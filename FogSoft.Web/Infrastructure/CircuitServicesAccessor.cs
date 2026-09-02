using log4net;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Даёт статическому коду ядра (FogSoft.Core) доступ к сервисам текущего circuit.
///
/// Зачем это нужно. В десктопе на процесс приходится один пользователь, поэтому
/// SecurityManager хранит его в статическом поле. В вебе пользователей много, а
/// API ядра остался статическим — менять его было бы правкой сотни мест вызова
/// (этап 0, п. 2). Значит, статике нужно уметь спросить: «а кто сейчас?».
///
/// Почему именно так, а не проще:
///
/// - <b>Просто AsyncLocal с пользователем не работает.</b> Каждое действие
///   пользователя в Blazor Server — отдельная порция работы, приходящая по
///   SignalR. Значение, положенное в AsyncLocal при входе в систему, до
///   следующего клика не доживёт: у него будет свой ExecutionContext.
/// - <b>Scoped-сервис сам по себе тоже не решает.</b> В Blazor Server scope
///   живёт ровно столько, сколько circuit, — это правильное время жизни. Но
///   статический SecurityManager не умеет внедрять зависимости.
///
/// Отсюда связка: scope хранит состояние (правильное время жизни), а AsyncLocal
/// хранит лишь <i>ссылку</i> на этот scope и переустанавливается перед каждой
/// порцией работы circuit — за это отвечает <see cref="CircuitServicesHandler"/>.
/// Тот же приём, которым в ASP.NET Core сделан IHttpContextAccessor.
/// </summary>
public sealed class CircuitServicesAccessor
{
	private static readonly AsyncLocal<IServiceProvider?> _current = new();

	public IServiceProvider? Services
	{
		get => _current.Value;
		internal set => _current.Value = value;
	}
}

/// <summary>
/// Переустанавливает <see cref="CircuitServicesAccessor"/> перед каждой порцией
/// работы circuit и снимает после.
///
/// <see cref="CircuitHandler.CreateInboundActivityHandler"/> появился в .NET 8
/// ровно для этой задачи: он оборачивает каждое входящее действие пользователя,
/// а не только создание circuit, — поэтому ссылка на scope актуальна на каждом
/// клике, а не только в момент подключения.
///
/// Тут же, в этой обёртке над каждой входящей активностью, — единственно
/// правильное место проставлять и лог-контекст (<c>%property{user}</c>,
/// <c>%property{cid}</c>): десктоп кладёт <c>user</c> один раз при входе через
/// <c>GlobalContext.Properties</c> (один процесс — один пользователь на всё
/// время жизни), в вебе так нельзя — тот же класс бага, что чинили для
/// <c>SecurityManager.loggedUser</c> в этапе 0.2. Простой <c>AsyncLocal</c>,
/// выставленный один раз при логине, тоже не подошёл бы: до следующего клика
/// он не доживёт (см. комментарий класса выше про отдельный
/// <c>ExecutionContext</c> на каждую порцию работы circuit) — значит,
/// перечитывать пользователя из <see cref="UserSession"/> нужно на каждую
/// активность, а не один раз. <c>cid</c> в десктопе объявлен в паттерне, но
/// нигде не проставлялся (docs/ARCHITECTURE.md, «Открытые вопросы» №3) —
/// здесь заполняется впервые, как идентификатор одной входящей активности
/// circuit (один клик/JS-interop-вызов), чтобы в логе можно было отличить
/// параллельные действия разных пользователей и разных вкладок одного
/// пользователя друг от друга.
/// </summary>
public sealed class CircuitServicesHandler : CircuitHandler
{
	private readonly CircuitServicesAccessor _accessor;
	private readonly IServiceProvider _services;

	public CircuitServicesHandler(CircuitServicesAccessor accessor, IServiceProvider services)
	{
		_accessor = accessor;
		_services = services;
	}

	public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
		Func<CircuitInboundActivityContext, Task> next)
	{
		return async context =>
		{
			_accessor.Services = _services;
			LogicalThreadContext.Properties["user"] = _services.GetService<UserSession>()?.User?.LoginName ?? "";
			LogicalThreadContext.Properties["cid"] = Guid.NewGuid().ToString("N").Substring(0, 8);
			try
			{
				await next(context);
			}
			finally
			{
				// Снимаем, чтобы ссылка на чужой circuit (и его лог-контекст)
				// не утекла в код, который выполняется вне обработки действия
				// пользователя.
				_accessor.Services = null;
				LogicalThreadContext.Properties.Remove("user");
				LogicalThreadContext.Properties.Remove("cid");
			}
		};
	}
}
