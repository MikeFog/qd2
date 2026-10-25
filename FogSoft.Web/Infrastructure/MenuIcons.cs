namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Значки пунктов меню — Bootstrap Icons, как все значки веба. Ключ — код пункта (<c>iMenu.codeName</c>).
/// Колонку <c>iMenu.imgResourcePath</c> не используем: в ней имена десктопных ресурсов (<c>Icons.payment.png</c>), их читает
/// <c>MenuManager</c>, и чужое значение десктоп не поймёт.
///
/// Правило Миши 25.10.2026: у сущности один значок на всю систему. Поэтому у пункта-сущности (фирмы, банки, радиостанции,
/// оплаты, журналы акций…) в значении стоит имя картинки сущности (<c>Firm.png</c>) и значок берётся из <see cref="EntityIcons"/> —
/// им же рисуются дерево и списки; правится значок сущности в одном месте. Значение без точки — собственное имя Bootstrap Icons
/// для экрана, который сущностью не является (реестр счетов, статистика роликов, скидки…).
/// Пункт без значка рисуется с пустым местом под значок, чтобы тексты не прыгали.
/// </summary>
public static class MenuIcons
{
	private static readonly Dictionary<string, string> Icons = new(StringComparer.OrdinalIgnoreCase)
	{
		// Журналы рекламных акций (подтверждённые — в том числе у бухгалтерии и трафика, макеты, удалённые): сущность «акция».
		["miActionJournal"] = "Action.png",
		["miActionJournalBuh"] = "Action.png",
		["miActionJournalTraffic"] = "Action.png",
		["miActionJournalUnconfirmed"] = "Action.png",
		["miActionJournalDeleted"] = "Action.png",

		// Деньги: оплаты — сущность «оплата»; реестр счетов — свой значок.
		["miBillRegister"] = "receipt",
		["miPaymentFRS"] = "payment.png",
		["miPaymentCommon"] = "payment.png",
		["miPaymentByManagerFromRSection"] = "payment.png",
		["miPaymentByManager"] = "payment.png",

		// Эфир: ролики, бонусы, переносы, удалённые выпуски, сообщения.
		["miRoller"] = "Roller.png",
		["miRollerStatistic"] = "bar-chart-line",
		["miRollerStatisticWithFilter"] = "bar-chart-line",
		["miBonusesStat"] = "gift",
		["miTransferJournal"] = "arrow-left-right",
		["miLog"] = "DeletedIssues.png",
		["miAnnouncements"] = "Message.png",

		// Справочники заказчиков.
		["miFirm"] = "Firm.png",
		["miHeadOrganizations"] = "headcompany.png",
		["miAdvertSubject"] = "bookmark",
		["miBank"] = "bank.png",

		// Справочники эфира: радиостанции, тарифы, модули, скидки.
		["miMassMedia"] = "Massmedia.png",
		["miGroupMassmedia"] = "MassmediaGroup.png",
		["miTariff"] = "tariff.ico",
		["miSponsorTariff"] = "SponsorProgram.png",
		["miModules"] = "Module.png",
		["miPackModules"] = "PackModule.png",
		["miComboModules"] = "combo-module.png",
		["miDiscount"] = "percent",
		["miPackageDiscounts"] = "bag",
		["miManagerDiscountHistory"] = "person-badge",
		["miManagerDiscountReason"] = "chat-square-text",

		// Справочники расчётов и документов.
		["miAgencyTax"] = "briefcase",
		["miPaymentType"] = "credit-card",
		["miReportPartText"] = "file-earmark-word",
	};

	/// <summary>Класс значка Bootstrap Icons (<c>bi bi-…</c>) для пункта меню; null — значка нет.</summary>
	public static string? For(string? codeName)
	{
		if (codeName == null || !Icons.TryGetValue(codeName, out string? value))
			return null;
		// Имя картинки сущности (Firm.png, tariff.ico) — значок из реестра сущностей, иначе — собственное имя Bootstrap Icons.
		return "bi " + (value.Contains('.') ? EntityIcons.For(value) : "bi-" + value);
	}
}
