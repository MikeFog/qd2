namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Значки действий из Bootstrap Icons (wwwroot/lib/bootstrap-icons).
///
/// В десктопе значок — картинка из ресурсов, имя которой лежит в
/// iEntityAction.imgResourceName. Картинки есть не у всех действий: на
/// ArtvisDev их 13 разных, и почти все — у общих действий. Поэтому сначала
/// значок по имени действия (так общие выглядят одинаково во всех сущностях),
/// потом по имени ресурса, иначе — без значка, одним текстом, как и в десктопе.
/// </summary>
public static class ActionIcons
{
	private static readonly Dictionary<string, string> ByAction = new()
	{
		["Properties"] = "bi-card-text",
		["DeleteItem"] = "bi-trash3",
		["RefreshItem"] = "bi-arrow-clockwise",
		["AddItem"] = "bi-plus-lg",
		["AssignNew"] = "bi-plus-lg",
		["AssignExisting"] = "bi-link-45deg",
		["ShowFilters"] = "bi-funnel",
		["Clone"] = "bi-copy",
		["Edit"] = "bi-pencil",
		["PlayRoller"] = "bi-play",
	};

	private static readonly Dictionary<string, string> ByResource = new()
	{
		["Icons.RefreshItem.png"] = "bi-arrow-clockwise",
		["Icons.DeleteItem.png"] = "bi-trash3",
		["Icons.Properties.png"] = "bi-card-text",
		["Icons.AddItem.png"] = "bi-plus-lg",
		["Icons.AddAction.png"] = "bi-plus-lg",
		["Icons.Filter.png"] = "bi-funnel",
		["Icons.Play.png"] = "bi-play",
		["Icons.Stop.png"] = "bi-stop",
		["Icons.Save.png"] = "bi-floppy",
		["Icons.ExportExcel.png"] = "bi-file-earmark-spreadsheet",
		["Icons.Day.png"] = "bi-calendar-day",
		["Icons.Campaign.png"] = "bi-megaphone",
		["Icons.Roller.png"] = "bi-music-note-beamed",
		["Icons.Group.png"] = "bi-collection",
		["Icons.Module.png"] = "bi-grid",
		["Icons.PackModule.png"] = "bi-grid-3x3-gap",
		["Icons.SponsorProgram.png"] = "bi-award",
		["Icons.User.png"] = "bi-person",
		["Icons.Firm.png"] = "bi-building",
		["Icons.Issue.png"] = "bi-broadcast",
	};

	public static string? For(string actionName, string? imgResourceName)
	{
		if (ByAction.TryGetValue(actionName, out string? byName))
			return byName;

		if (actionName.StartsWith("Add", StringComparison.Ordinal))
			return "bi-plus-lg";
		if (actionName.StartsWith("Print", StringComparison.Ordinal))
			return "bi-printer";

		return imgResourceName != null && ByResource.TryGetValue(imgResourceName, out string? byResource)
			? byResource
			: null;
	}
}
