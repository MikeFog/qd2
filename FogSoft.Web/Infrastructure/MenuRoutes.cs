using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Classes;
using Merlin;
using Merlin.Classes.FakeContainers;

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
/// <c>miStats.*</c> (2026-09-18) — тоже не из тех 22. Инвентаризация считала
/// ветки верхнего if/else в <c>MenuItemClick</c>, а все пункты
/// <c>miStats.*</c> уходят туда в одну ветку <c>StartsWith("miStats.")</c> →
/// <c>ShowStatsJournal</c>, у которой внутри свой switch на 15 пунктов. Из них
/// 14 — тот же <c>ShowSimpleJournal</c> (см. записи ниже); пятнадцатый,
/// <c>miStats.Balance</c>, ведёт в <c>StatBalanceJournalForm</c> — это простой
/// журнал с одной подменой сущности при включённой группировке
/// (<see cref="JournalRoute.EntitySwitch"/>). Итого к тем 22 веткам добавляются
/// miMassMedia и 14 пунктов статистики, плюс miStats.Balance.
///
/// Правило <c>ManagerFilter</c> привязано к пункту меню, а не к сущности
/// (десктоп передаёт <c>ManagerFilter.FilterClick</c> в <c>ShowSimpleJournal</c>
/// только из части веток), поэтому признак лежит здесь, в
/// <see cref="JournalRoute.ManagerFilter"/>, рядом с маршрутом.
///
/// Ссылки на <c>Entities.X</c> — по имени, а не голым числом: если когда-то
/// понадобится сменить нумерацию, компилятор укажет на это место, а не
/// уронит меню в рантайме на непонятной сущности.
/// </summary>
public static class MenuRoutes
{
	/// <summary>Пункт «Выход»: в вебе — выход из сеанса, как кнопка «Выйти» в верхней полосе.</summary>
	public const string Exit = "miExit";

	public static readonly IReadOnlyDictionary<string, JournalRoute> SimpleJournal =
		new Dictionary<string, JournalRoute>(StringComparer.OrdinalIgnoreCase)
		{
			{ "miBalance", new JournalRoute(Entities.BalanceIssues, ManagerFilter: true) },
			{ "miBalanceFromRSection", new JournalRoute(Entities.BalanceIssues, ManagerFilter: true) },
			{ "miAnnouncements", new JournalRoute(Entities.Announcement,
				BulkAction: new BulkAction("MarkAsRead", "Пометить все как прочтенное")) }, // i18n-ok: подпись кнопки, переводить при показе (Journal.razor, bulk.Caption)
			{ "miBank", new JournalRoute(Entities.Bank) },
			{ "miBonusesStat", new JournalRoute(Entities.StatBonuses) },
			{ "miFirm", new JournalRoute(Entities.Firm) },
			{ "miGroupMassmedia", new JournalRoute(Entities.MassmediaGroup) },
			{ "miLog", new JournalRoute(Entities.LogDeletedIssue) },
			{ "miManagerDiscountHistory", new JournalRoute(Entities.ManagerDiscountHistory) },
			{ "miManagerDiscountReason", new JournalRoute(Entities.ManagerDiscountReason) },
			{ "miMassMedia", new JournalRoute(Entities.MassMedia) },
			{ "miPaymentByManagerFromRSection", new JournalRoute(Entities.PaymentCommonAction, ManagerFilter: true) },
			{ "miPaymentType", new JournalRoute(Entities.PaymentType) },
			{ "miReportPartText", new JournalRoute(Entities.ReportPartText) },
			{ "miSpecialActions", new JournalRoute(Entities.SpecialAction, ManagerFilter: true) },
			// StatBalanceJournalForm: при включённой «С разбивкой по агентствам» таблица
			// берёт сущность StatsBalanceGroup, иначе StatsBalance; фильтр общий.
			// Без ManagerFilter (MDIForm.ShowStatBalance открывает форму без него).
			{ "miStats.Balance", new JournalRoute(Entities.StatsBalance,
				EntitySwitch: new EntitySwitch("IsGroupByAgency", Entities.StatsBalanceGroup)) },
			{ "miStats.AvgDiscount", new JournalRoute(Entities.StatAvgDiscount, ManagerFilter: true) },
			{ "miStats.BalanceAgency", new JournalRoute(Entities.StatsBalanceAgency, ManagerFilter: true) },
			{ "miStats.BalanceManager", new JournalRoute(Entities.StatsBalanceManager, ManagerFilter: true) },
			{ "miStats.FactorAnalysis", new JournalRoute(Entities.StatsFactorAnalysis, ManagerFilter: true) },
			// Единственный из miStats.*, который десктоп открывает без ManagerFilter.
			{ "miStats.FillPercentage", new JournalRoute(Entities.StatsFillPercentage) },
			{ "miStats.ModuleFinancy", new JournalRoute(Entities.StatModuleFinancy, ManagerFilter: true) },
			{ "miStats.ModuleLoading", new JournalRoute(Entities.StatModuleLoading, ManagerFilter: true,
				Caption: "Фактическое размещение рекламных модулей") }, // i18n-ok: переводится при показе (Journal.razor, Title)
			{ "miStats.PackModuleFinancy", new JournalRoute(Entities.StatPackModuleFinancy, ManagerFilter: true) },
			{ "miStats.PackModuleLoading", new JournalRoute(Entities.StatPackModuleLoading, ManagerFilter: true,
				Caption: "Фактическое размещение пакетных рекламных модулей") }, // i18n-ok: переводится при показе (Journal.razor, Title)
			{ "miStats.SponsorBusiness", new JournalRoute(Entities.StatsSponsorBusiness, ManagerFilter: true,
				Caption: "Фактическое размещение спонсорских программ") }, // i18n-ok: переводится при показе (Journal.razor, Title)
			{ "miStats.VolumeByPaymentType", new JournalRoute(Entities.StatVolumeByPaymentType, ManagerFilter: true) },
			// Действующий код десктопа — простой журнал; рядом закомментирован вариант с графиком (GraphForm).
			{ "miStats.VolumeOfRealization", new JournalRoute(Entities.StatsVolumeofRealization, ManagerFilter: true) },
			{ "miStats.VolumeOfRealizationSec", new JournalRoute(Entities.StatVolumeOfRealizationSec, ManagerFilter: true) },
			{ "miStats.VolumeRealizationByMonth", new JournalRoute(Entities.StatVolumeOfRealiztionByMonth, ManagerFilter: true) },
			{ "miTransferJournal", new JournalRoute(Entities.TransferLog) },
			// MDIForm.ShowGraphVolumeOfRealizationByPerson: те же данные, что у miStats.VolumeOfRealization,
			// но менеджер по умолчанию — текущий пользователь. Диаграммы десктопа (GraphForm)
			// отложены (решение владельца, 2026-09-21), пока показываем таблицу.
			// Стоит после miStats.VolumeOfRealization: запасной поиск по сущности берёт первый маршрут.
			{ "VolumeOfRealizationByManager", new JournalRoute(Entities.StatsVolumeofRealization, ManagerFilter: true,
				FilterDefault: new FilterDefault("managerID", PageControl.InitialValueAbbreviations.LoggedUser)) },
		};

	/// <summary>
	/// Соответствие <c>codeName</c> пункта меню → древовидный экран.
	///
	/// Извлечено из тех же веток <c>MDIForm.MenuItemClick</c>, что ведут в
	/// <c>Globals.ShowBrowser(new FakeContainer(имя, действия, сценарий))</c> —
	/// то есть в дерево по сценарию связей, без единой строки кода на экран.
	/// Семь веток создают «голый» <c>FakeContainer</c>, восьмая
	/// (<c>miAdvertSubject</c>) — свой <c>AdvertTypeContainer</c> через
	/// <see cref="BrowserRoute.Factory"/>. Остальные пять пунктов — журналы
	/// рекламных акций на <c>ActionContainer</c>, тоже через фабрику; экрана там
	/// три, а пунктов пять, потому что права выдаются на пункт меню
	/// (инвентаризация, §3). Ветки <c>miDisabledWindows</c> и
	/// <c>miPrintInquire</c> удалены как мёртвые (2026-09-19).
	///
	/// Имя — подпись корневого узла дерева, тот же первый аргумент
	/// конструктора, что в десктопе.
	/// </summary>
	/// <summary>
	/// Журнал рекламных акций: те же аргументы, что у десктопного
	/// <c>MDIForm.ShowMassmediaActions</c> — сценарий, подпись корня и три
	/// сущности разбивки.
	///
	/// <c>ManagerFilter</c>: в десктопе поле «Менеджер» гасит не
	/// <c>ManagerFilter.FilterClick</c>, как у журналов, а собственный диалог
	/// отбора <c>ActionJournalFilter</c>. Правило в нём то же самое (право на
	/// чужие или групповые акции), поэтому в вебе это тот же признак.
	/// </summary>
	private static BrowserRoute ActionJournal(string scenario, string caption,
		Entities firm, Entities action, Entities headCompany) =>
		new(scenario, caption,
			() => new ActionContainer(RelationManager.GetScenario(scenario), Tr.T(caption), firm, action, headCompany),
			ManagerFilter: true);

	/// <summary>Подтверждённые акции — один экран за тремя пунктами меню.</summary>
	private static readonly BrowserRoute ConfirmedActions = ActionJournal(
		RelationScenarios.ConfirmedAction, "Подтверждённые рекламные акции", // i18n-ok: переводится при открытии (ActionJournal)
		Entities.FirmWithConfirmedActions, Entities.Action, Entities.HeadCompanyWithConfirmedActions);

	public static readonly IReadOnlyDictionary<string, BrowserRoute> Browser =
		new Dictionary<string, BrowserRoute>(StringComparer.OrdinalIgnoreCase)
		{
			{ "miTariff", new BrowserRoute(RelationScenarios.Tariff, "Радиостанция") }, // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
			// Десктоп — отдельная форма TariffWindowGenerationForm (своё дерево «Tariff
			// Windows» + сетка окон). В вебе это то же дерево «Рекламные тарифы», сразу на
			// вкладке окон у прайс-листа (решение 2026-09-23, docs/tasks/web-tariffgrid.md).
			{ "miTariffWindow", new BrowserRoute(RelationScenarios.Tariff, "Радиостанция", OpenWindowsTab: true) }, // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
			{ "miModules", new BrowserRoute(RelationScenarios.Module, "Радиостанция") }, // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
			{ "miSponsorTariff", new BrowserRoute(RelationScenarios.SponsorProgramm, "Радиостанция") }, // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
			{ "miDiscount", new BrowserRoute(RelationScenarios.Discount, "Скидки") }, // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
			{ "miPackageDiscounts", new BrowserRoute(RelationScenarios.PackageDiscount, "Скидки") }, // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
			{ "miPackModules", new BrowserRoute(RelationScenarios.PackModules, "Пакетные модули") }, // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
			{ "miComboModules", new BrowserRoute(RelationScenarios.ComboModules, "Комбо-модули") }, // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
			{ "miAdvertSubject", new BrowserRoute(RelationScenarios.AdvertTypes, "Предметы рекламы", // i18n-ok: подпись корня, переводить при показе (Browser.razor, route.RootName)
				() => new AdvertTypeContainer()) },
			// Десктоп открывает их через MasterDetailForm; веб — деревом по
			// сценарию из кода (CodeScenario), метаданных сценарий не требует.
			// Подпись корня — имя мастера, как у соседних маршрутов.
			{ "miAgencyTax", new BrowserRoute("Агентства и налоги", "Агентство", // i18n-ok: ключ — имя сценария связей; подпись корня переводить при показе (Browser.razor)
				ScenarioFactory: () => CodeScenario.MasterDetail("Агентства и налоги", Entities.Agency, Entities.AgencyTax)) }, // i18n-ok: ключ — имя сценария связей
			{ "miHeadOrganizations", new BrowserRoute("Группа компаний", "Группа компаний", // i18n-ok: ключ — имя сценария связей; подпись корня переводить при показе (Browser.razor)
				ScenarioFactory: () => CodeScenario.MasterDetail("Группа компаний", Entities.HeadCompany, Entities.Firm)) }, // i18n-ok: ключ — имя сценария связей
			{ "miActionJournal", ConfirmedActions },
			{ "miActionJournalBuh", ConfirmedActions },
			{ "miActionJournalTraffic", ConfirmedActions },
			{ "miActionJournalUnconfirmed", ActionJournal(
				RelationScenarios.UnconfirmedAction, "Макеты рекламных акций", // i18n-ok: переводится при открытии (ActionJournal)
				Entities.FirmWithUnconfirmedActions, Entities.Action, Entities.HeadCompanyWithUnconfirmedActions) },
			{ "miActionJournalDeleted", ActionJournal(
				RelationScenarios.DeletedAction, "Удалённые рекламные акции", // i18n-ok: переводится при открытии (ActionJournal)
				Entities.FirmWithDeletedActions, Entities.ActionDeleted, Entities.HeadCompanyWithDeletedActions) },
		};
}

/// <summary>
/// Пункты меню, у которых в вебе свой экран — не журнал и не дерево: codeName → адрес
/// страницы. Права — те же, что у остальных пунктов (MenuAccess.CheckScreen).
/// </summary>
public static class ScreenRoutes
{
	public const string TrafficManagement = "miTrafficManagement";

	public static readonly IReadOnlyDictionary<string, string> Screens =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			// Десктоп — TrafficManagementForm + TrafficGrid; в вебе — свой экран по решениям
			// владельца 2026-09-23 (docs/tasks/web-tariffgrid.md, §9).
			{ TrafficManagement, "/traffic" },
		};
}

/// <summary>Маршрут простого журнала: сущность плюс то, что десктоп задаёт на уровне пункта меню.</summary>
/// <param name="Entity">Сущность журнала.</param>
/// <param name="ManagerFilter">
/// Десктоп открывает журнал через <c>ManagerFilter.FilterClick</c>: поле
/// «Менеджер» в отборе заблокировано у пользователя без прав на чужие и
/// групповые акции. Признак пункта меню, а не сущности: одна и та же сущность
/// может открываться и с этим правилом, и без него.
/// </param>
/// <param name="Caption">
/// Заголовок, который десктоп задаёт в коде вместо текста пункта меню.
/// <c>null</c> — заголовок по-прежнему имя сущности, как у остальных журналов.
/// </param>
/// <param name="EntitySwitch">
/// Подмена сущности, из которой берутся данные, по значению поля отбора
/// (<c>StatBalanceJournalForm.LoadData</c>). Отбор и добавление остаются за
/// <paramref name="Entity"/>; сменяется только сущность списка.
/// </param>
/// <param name="BulkAction">
/// Кнопка тулбара «сделать действие над всеми строками» (десктопный
/// <c>AnnouncementJournalForm</c>). <c>null</c> — кнопки на экране нет: она есть
/// только у журналов, чья форма в десктопе её добавляет.
/// </param>
/// <param name="FilterDefault">
/// Начальное значение поля отбора, которое пункт меню задаёт поверх метаданных
/// (<c>MDIForm.ShowGraphVolumeOfRealizationByPerson</c>: <c>managerID</c> = текущий
/// пользователь). Свойство пункта, а не сущности: у сущности в <c>XmlFilter</c> этого
/// поля значения по умолчанию нет, и второй пункт на ту же сущность
/// (<c>miStats.VolumeOfRealization</c>) должен открываться с пустым отбором.
/// <c>null</c> — отбор только из метаданных.
/// </param>
public sealed record JournalRoute(Entities Entity, bool ManagerFilter = false, string? Caption = null,
	EntitySwitch? EntitySwitch = null, BulkAction? BulkAction = null, FilterDefault? FilterDefault = null)
{
	public int EntityId => (int)Entity;
}

/// <param name="ActionName">
/// Действие сущности (<c>iEntityAction</c>), которое выполняется у каждой строки
/// списка, где оно доступно: тот же путь, что у пункта меню «⋯», с теми же
/// проверками доступности и прав.
/// </param>
/// <param name="Caption">Подпись кнопки.</param>
public sealed record BulkAction(string ActionName, string Caption);

/// <param name="Field">Поле отбора.</param>
/// <param name="Value">
/// Значение в том же виде, что в метаданных фильтра (атрибут <c>value</c>): сокращение из
/// <see cref="PageControl.InitialValueAbbreviations"/> или готовое значение. Разбирается
/// <see cref="Globals.ResolveInitialValue"/> — тем же кодом, что и значения из метаданных.
/// </param>
public sealed record FilterDefault(string Field, string Value);

/// <param name="FilterField">Булево поле отбора.</param>
/// <param name="WhenTrue">Сущность данных, когда поле включено; иначе — сущность маршрута.</param>
public sealed record EntitySwitch(string FilterField, Entities WhenTrue);

/// <param name="Scenario">
/// Имя сценария связей (as_relationScenarios). У маршрута с
/// <paramref name="ScenarioFactory"/> — только имя для людей: сценария в
/// метаданных нет.
/// </param>
/// <param name="RootName">Подпись корневого узла дерева.</param>
/// <param name="Factory">
/// Свой контейнер корня, если десктоп создаёт не «голый» <c>FakeContainer</c>.
/// <c>null</c> — «голый» с двумя действиями «Обновить» и «Добавить».
/// </param>
/// <param name="ManagerFilter">
/// Поле «Менеджер» в отборе доступно только пользователю с правом на чужие или
/// групповые акции. У деревьев это даёт не <c>ManagerFilter.FilterClick</c>, а
/// свой диалог отбора контейнера; правило то же, что у журналов.
/// </param>
/// <param name="ScenarioFactory">
/// Готовый сценарий, объявленный в коде (<see cref="CodeScenario"/>), вместо
/// поиска по имени в метаданных. Вызывается при каждом открытии экрана и не
/// кэшируется: сценарий держит сущности из кэша circuit, а они персональные.
/// </param>
/// <param name="OpenWindowsTab">
/// У прайс-листа радиостанции открывать сразу вкладку «Рекламные окна», а не «Тарифы».
/// </param>
public sealed record BrowserRoute(string Scenario, string RootName, Func<FakeContainer>? Factory = null,
	bool ManagerFilter = false, Func<RelationScenario>? ScenarioFactory = null, bool OpenWindowsTab = false);
