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
			try
			{
				await next(context);
			}
			finally
			{
				// Снимаем, чтобы ссылка на чужой circuit не утекла в код,
				// который выполняется вне обработки действия пользователя.
				_accessor.Services = null;
			}
		};
	}
}
