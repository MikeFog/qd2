using Merlin;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Соответствие <c>codeName</c> пункта меню → сущность простого журнала.
///
/// Извлечено из <c>Client/Forms/MDIForm.cs:MenuItemClick</c> — там огромный
/// if/else по <c>codeName</c>, ведущий в конкретный обработчик. Из 70 веток
/// этого switch 22 сводятся ровно к <c>Globals.ShowSimpleJournal(entity, ...)</c>
/// — то есть к тому же самому <c>/journal/{id}</c>, что уже работает в срезе.
/// Остальные ветки — либо <c>FakeContainer</c>/<c>MasterDetail</c> (свои
/// движки, этап 2), либо самостоятельные экраны (этап 3, раздел 3 плана) —
/// см. решение по объёму этапа 1 в docs/tasks/web-migration.md, раздел 6.
///
/// Ссылки на <c>Entities.X</c> — по имени, а не голым числом: если когда-то
/// понадобится сменить нумерацию, компилятор укажет на это место, а не
/// уронит меню в рантайме на непонятной сущности.
/// </summary>
public static class MenuRoutes
{
	public static readonly IReadOnlyDictionary<string, int> SimpleJournal =
		new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			{ "miBalance", (int)Entities.BalanceIssues },
			{ "miBalanceFromRSection", (int)Entities.BalanceIssues },
			{ "miBalanceStudioOrder", (int)Entities.BalanceStudioOrder },
			{ "miBalanceStudioOrderFromRSection", (int)Entities.BalanceStudioOrder },
			{ "miBank", (int)Entities.Bank },
			{ "miBonusesStat", (int)Entities.StatBonuses },
			{ "miConfirmationHistory", (int)Entities.ConfirmationHistory },
			{ "miFirm", (int)Entities.Firm },
			{ "miGroupMassmedia", (int)Entities.MassmediaGroup },
			{ "miLog", (int)Entities.LogDeletedIssue },
			{ "miManagerDiscountHistory", (int)Entities.ManagerDiscountHistory },
			{ "miManagerDiscountReason", (int)Entities.ManagerDiscountReason },
			{ "miPaymentByManagerFromRSection", (int)Entities.PaymentCommonAction },
			{ "miPaymentStudioOrderByManagerFRS", (int)Entities.PaymentStudioOrderAction },
			{ "miPaymentType", (int)Entities.PaymentType },
			{ "miProductionStudio", (int)Entities.ProductionStudio },
			{ "miReportPartText", (int)Entities.ReportPartText },
			{ "miRolStyle", (int)Entities.RolStyle },
			{ "miSpecialActions", (int)Entities.SpecialAction },
			{ "miSpecialStudioOrderActions", (int)Entities.SpecialStudioOrderAction },
			{ "miStudioOrderActPrint", (int)Entities.StudioOrderActJournal },
			{ "miTransferJournal", (int)Entities.TransferLog },
		};
}
