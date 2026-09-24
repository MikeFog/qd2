using System.Collections.Concurrent;
using FogSoft.WinForm.Classes;
using log4net;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Языки интерфейса веба. docs/tasks/web-i18n.md.
///
/// Язык — свойство пользователя (<see cref="UserSession.Language"/>, хранится
/// в UserSetting), по умолчанию — язык установки (<c>Language</c> в App.config).
/// Формат дат, чисел и валюта от языка НЕ зависят: это свойство установки
/// (<c>Culture</c> в App.config), выставляется на весь процесс в Program.cs.
///
/// Почему язык в сеансе, а не в <c>CultureInfo.CurrentUICulture</c> circuit:
/// культуру circuit Blazor берёт из HTTP-запроса, с которого он начался, и
/// сменить её можно только перезагрузкой страницы — а перезагрузка в вебе
/// разлогинивает (пользователь живёт на circuit).
/// </summary>
public static class WebLanguage
{
	public const string Russian = "ru";
	public const string Spanish = "es";

	/// <summary>
	/// Псевдоязык для поиска непереведённого: всё, что прошло через перевод,
	/// обрамляется «[·…·]», значит, строка без рамки перевод обходит.
	/// Доступен только в Development.
	/// </summary>
	public const string Pseudo = "pseudo";

	/// <summary>Имя настройки в UserSetting.</summary>
	public const string SettingName = "Language";

	public static bool PseudoEnabled { get; set; }

	/// <summary>Язык установки.</summary>
	public static string Default { get; } =
		Normalize(System.Configuration.ConfigurationManager.AppSettings["Language"]) ?? Russian;

	public static IReadOnlyList<(string Code, string Name)> Available =>
		PseudoEnabled
			? new[] { (Russian, "Русский"), (Spanish, "Español"), (Pseudo, "[·Псевдо·]") }
			: new[] { (Russian, "Русский"), (Spanish, "Español") };

	/// <summary>Код языка, если он поддерживается; иначе null.</summary>
	public static string? Normalize(string? code)
	{
		code = code?.Trim().ToLowerInvariant();
		if (code == Russian || code == Spanish || (code == Pseudo && PseudoEnabled))
			return code;
		return null;
	}
}

/// <summary>
/// Переводчик ядра (<see cref="Tr"/>) для веба: язык берётся из сеанса
/// пользователя текущего circuit, вне circuit — язык установки.
/// </summary>
public sealed class WebTranslator : Tr.ITranslator
{
	private static readonly ILog Log = LogManager.GetLogger(typeof(WebTranslator));

	private readonly CircuitServicesAccessor _accessor;

	// Непереведённое пишется в лог один раз на строку, а не на каждую отрисовку.
	private readonly ConcurrentDictionary<string, byte> _reportedMissing = new();

	public WebTranslator(CircuitServicesAccessor accessor)
	{
		_accessor = accessor;
	}

	public string Current =>
		_accessor.Services?.GetService<UserSession>()?.Language ?? WebLanguage.Default;

	public string Translate(string source, string context)
	{
		string language = Current;
		if (language == WebLanguage.Russian)
			return source;
		// Разделители, «%», «$» и т. п. — переводить нечего.
		if (!source.Any(char.IsLetter))
			return source;
		if (language == WebLanguage.Pseudo)
			return "[·" + source + "·]";

		if (TranslationStore.Find(language, source, context) is { } text)
			return text;
		if (_reportedMissing.TryAdd(language + "\u0001" + context + "\u0001" + source, 0))
			Log.InfoFormat("Нет перевода [{0}{1}]: «{2}»", language,
				context == null ? "" : ", " + context, source);
		return source;
	}
}
