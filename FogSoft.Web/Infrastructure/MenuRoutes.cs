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
/// <c>miMassMedia</c> добавлен отдельно (2026-09-17) и в те 22 ветки не входит:
/// он ведёт в собственную форму <c>MassmediasJournal</c>. Разбор показал, что
/// форма — это <c>JournalForm</c> того же вида, что создаёт
/// <c>ShowSimpleJournal</c>, плюс перерисовка журнала после добавления,
/// изменения и удаления; своей разметки у неё нет (пустой
/// <c>InitializeComponent</c>). Веб перечитывает список после сохранения сам,
/// так что поведение совпадает. Если в эту таблицу попадёт ещё один экран не
/// из тех 22 — разбирать так же и писать почему, иначе карта перестанет быть
/// проверяемой.
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
			{ "miBank", (int)Entities.Bank },
			{ "miBonusesStat", (int)Entities.StatBonuses },
			{ "miConfirmationHistory", (int)Entities.ConfirmationHistory },
			{ "miFirm", (int)Entities.Firm },
			{ "miGroupMassmedia", (int)Entities.MassmediaGroup },
			{ "miLog", (int)Entities.LogDeletedIssue },
			{ "miManagerDiscountHistory", (int)Entities.ManagerDiscountHistory },
			{ "miManagerDiscountReason", (int)Entities.ManagerDiscountReason },
			{ "miMassMedia", (int)Entities.MassMedia },
			{ "miPaymentByManagerFromRSection", (int)Entities.PaymentCommonAction },
			{ "miPaymentType", (int)Entities.PaymentType },
			{ "miReportPartText", (int)Entities.ReportPartText },
			{ "miSpecialActions", (int)Entities.SpecialAction },
			{ "miTransferJournal", (int)Entities.TransferLog },
		};
}
