namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Значки сущностей из Bootstrap Icons (wwwroot/lib/bootstrap-icons).
///
/// В десктопе значок сущности — картинка из ресурсов, имя лежит в
/// iEntity.iconName. На ArtvisDev там 23 разных имени (у 32 сущностей значка
/// нет, у 19 — default.png), поэтому словарь «имя картинки → bi-…» короткий.
/// Он же — единственное место, где значки веба правятся: ни БД, ни десктоп
/// для этого трогать не нужно. Когда десктоп перейдёт на те же значки, в
/// iconName окажутся имена Bootstrap и словарь уйдёт.
///
/// Регистр имён в iEntity не выдержан (module.png / Module.png), поэтому
/// сравнение без учёта регистра.
/// </summary>
public static class EntityIcons
{
	/// <summary>Значок для сущностей без картинки и для неизвестных имён: колонка значков остаётся ровной.</summary>
	public const string Fallback = "bi-dot";

	private static readonly Dictionary<string, string> ByName = new(StringComparer.OrdinalIgnoreCase)
	{
		["Action.png"] = "bi-bullseye",
		["bank.png"] = "bi-bank",
		["Campaign.png"] = "bi-megaphone",
		["combo-module.png"] = "bi-boxes",
		["Day.png"] = "bi-calendar-day",
		["DeletedIssues.png"] = "bi-trash3",
		["error.png"] = "bi-exclamation-triangle",
		["Firm.png"] = "bi-building",
		["folder.png"] = "bi-folder2",
		["Group.png"] = "bi-people",
		["headcompany.png"] = "bi-buildings",
		["Issue.png"] = "bi-broadcast",
		["Massmedia.png"] = "bi-boombox",
		["MassmediaGroup.png"] = "bi-collection",
		["Message.png"] = "bi-chat-left-text",
		["Module.png"] = "bi-grid",
		["PackModule.png"] = "bi-grid-3x3-gap",
		["payment.png"] = "bi-cash",
		["PriceList.png"] = "bi-card-list",
		["Roller.png"] = "bi-music-note-beamed",
		["SponsorProgram.png"] = "bi-award",
		["tariff.ico"] = "bi-tag",
		["User.png"] = "bi-person",
	};

	/// <summary>Класс значка для iEntity.iconName; никогда не пустой.</summary>
	public static string For(string? iconName) =>
		iconName != null && ByName.TryGetValue(iconName, out string? icon) ? icon : Fallback;
}
