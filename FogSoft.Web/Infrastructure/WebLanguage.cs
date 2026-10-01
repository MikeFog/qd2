using System.Collections.Concurrent;
using FogSoft.WinForm.Classes;
using log4net;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Языки интерфейса веба. docs/tasks/web-i18n.md.
///
/// Язык — свойство пользователя (<see cref="UserSession.Language"/>, хранится
/// в UserSetting), по умолчанию — язык установки (<c>Language</c> в App.config).
/// Выбор — из языков установки (<c>Languages</c>), переключатель только при двух и более.
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

	// Названия языков — каждое на своём языке, не переводятся.
	private static readonly (string Code, string Name)[] Known =
		{ (Russian, "Русский"), (Spanish, "Español") }; // i18n-ok

	/// <summary>Язык установки.</summary>
	public static string Default { get; } =
		KnownCode(System.Configuration.ConfigurationManager.AppSettings["Language"]) ?? Russian;

	/// <summary>
	/// Языки установки: <c>Languages</c> в App.config через запятую. Нет
	/// настройки — только язык установки. Установка работает в одной стране,
	/// поэтому на проде настройки обычно нет, и переключателя не видно.
	/// </summary>
	private static readonly (string Code, string Name)[] Installed = LoadInstalled();

	/// <summary>Показывать ли переключатель языка: выбирать есть из чего.</summary>
	public static bool CanSwitch => Installed.Length > 1;

	// Псевдоязык — только при переключателе, иначе с него не уйти.
	public static IReadOnlyList<(string Code, string Name)> Available =>
		PseudoEnabled && CanSwitch
			? Installed.Append((Pseudo, "[·Псевдо·]")).ToArray() // i18n-ok
			: Installed;

	/// <summary>Код языка, если он доступен в установке; иначе null.</summary>
	public static string? Normalize(string? code)
	{
		code = code?.Trim().ToLowerInvariant();
		return Available.Any(l => l.Code == code) ? code : null;
	}

	private static string? KnownCode(string? code)
	{
		code = code?.Trim().ToLowerInvariant();
		return Known.Any(l => l.Code == code) ? code : null;
	}

	private static (string Code, string Name)[] LoadInstalled()
	{
		var codes = (System.Configuration.ConfigurationManager.AppSettings["Languages"] ?? "")
			.Split(',')
			.Select(KnownCode)
			.Where(c => c != null)
			.ToList();
		// Язык установки доступен всегда, даже если в списке его забыли.
		if (!codes.Contains(Default))
			codes.Insert(0, Default);
		return Known.Where(l => codes.Contains(l.Code)).ToArray();
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
