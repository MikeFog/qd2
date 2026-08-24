using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Состояние сеанса одного пользователя. Регистрируется как Scoped, то есть в
/// Blazor Server живёт ровно столько, сколько живёт circuit — вкладка браузера.
/// </summary>
public sealed class UserSession
{
	public SecurityManager.User? User { get; set; }
}

/// <summary>
/// Реализация <see cref="SecurityManager.ILoggedUserStorage"/> для веба.
///
/// Шов был подготовлен этапом 0.2 специально ради этого места: десктопу оставили
/// обычное статическое поле (SingleUserStorage), а веб подставляет своё
/// хранилище при старте. Публичный API SecurityManager при этом не менялся —
/// значит, все доменные классы, которые спрашивают SecurityManager.LoggedUser,
/// работают в вебе без единой правки.
///
/// Само значение лежит в <see cref="UserSession"/> внутри scope circuit;
/// добраться до нужного scope помогает <see cref="CircuitServicesAccessor"/>.
/// </summary>
public sealed class WebLoggedUserStorage : SecurityManager.ILoggedUserStorage
{
	private readonly CircuitServicesAccessor _accessor;

	public WebLoggedUserStorage(CircuitServicesAccessor accessor)
	{
		_accessor = accessor;
	}

	public SecurityManager.User User
	{
		get => Session?.User!;
		set
		{
			UserSession? session = Session;
			if (session != null)
				session.User = value;
		}
	}

	/// <summary>
	/// Сеанс текущего circuit. null означает, что обращение произошло вне
	/// обработки действия пользователя — например, из фонового кода. Молча
	/// возвращаем null: для чтения это «пользователь неизвестен», а запись в
	/// никуда безопаснее, чем запись в чужой сеанс.
	/// </summary>
	private UserSession? Session =>
		_accessor.Services?.GetService<UserSession>();
}
