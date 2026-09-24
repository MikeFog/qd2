using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Вход пользователя: форма входа (Home) и автологин разработчика (MainLayout).
/// </summary>
public static class WebLogin
{
	/// <summary>Возвращает false при неверном логине или пароле.</summary>
	public static bool Login(UserSession session, string login, string password)
	{
		SecurityManager.Login(login, password);
		if (SecurityManager.LoggedUser == null)
			return false;

		// Словарь процедур обязателен до первого DoAction и не грузится
		// лениво — см. CoreBootstrap. В десктопе это делает
		// SplashLogginForm после входа, здесь — мы.
		CoreBootstrap.EnsureDictionariesLoaded();
		session.Language = WebLanguage.Normalize(UserSettings.Load(WebLanguage.SettingName));
		return true;
	}

	/// <summary>
	/// Автологин для отладки — аналог TestMode в SplashLogginForm десктопа.
	/// Работает только в Development. Пароли — в user-secrets (профиль Windows,
	/// в репозиторий не попадают), задаются скриптом FogSoft.Web/dev-autologin.ps1:
	/// <c>DevAutoLogin:Passwords:&lt;логин&gt;</c> и <c>DevAutoLogin:User</c> — кто
	/// входит по умолчанию. Другого пользователя выбирает параметр адреса
	/// <c>?devuser=fog</c>: переход по адресу создаёт новый circuit, и автологин
	/// входит заново — так проверяются права не-администратора.
	/// </summary>
	public static void TryDevAutoLogin(UserSession session, IConfiguration configuration,
		IWebHostEnvironment environment, string? requestedLogin)
	{
		if (session.User != null || !environment.IsDevelopment())
			return;
		string? login = string.IsNullOrEmpty(requestedLogin) ? configuration["DevAutoLogin:User"] : requestedLogin;
		if (string.IsNullOrEmpty(login))
			return;
		string? password = configuration["DevAutoLogin:Passwords:" + login];
		if (string.IsNullOrEmpty(password))
			return;
		Login(session, login, password);
	}
}
