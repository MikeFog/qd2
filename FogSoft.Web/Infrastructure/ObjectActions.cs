using System.Data;
using FogSoft.Web.Components;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes.FakeContainers;
using Microsoft.AspNetCore.Components;

namespace FogSoft.Web.Infrastructure;

/// <summary>Пункт меню действий над объектом.</summary>
public sealed class ActionMenuItem
{
	public static readonly ActionMenuItem Separator = new() { IsSeparator = true };

	public string Name { get; init; } = "";
	public string Text { get; init; } = "";

	/// <summary>Класс значка Bootstrap Icons, без префикса «bi »; null — без значка.</summary>
	public string? Icon { get; init; }

	/// <summary>Пояснение справа от текста, например «клик».</summary>
	public string? Hint { get; init; }

	/// <summary>Почему пункт погашен — подсказка при наведении; null — причину объяснить нечем.</summary>
	public string? DisabledReason { get; init; }

	/// <summary>Право и состояние объекта: IsActionEnabled, как в десктопе.</summary>
	public bool Enabled { get; init; }

	/// <summary>Есть ли у веба обработчик. Неперенесённое рисуется серым.</summary>
	public bool Ported { get; init; }

	public bool Danger { get; init; }
	public bool IsSeparator { get; private init; }
	public IReadOnlyList<ActionMenuItem> Children { get; init; } = Array.Empty<ActionMenuItem>();

	public bool Clickable => Enabled && Ported && Children.Count == 0;
}

/// <summary>Что изменилось после действия — чтобы владелец экрана знал, что перечитать.</summary>
public enum ActionEffect
{
	/// <summary>Ничего: отмена, действие недоступно.</summary>
	None,
	/// <summary>Сам объект изменился — перечитать его строку или узел.</summary>
	Changed,
	/// <summary>У объекта-контейнера появился новый дочерний объект.</summary>
	ChildAdded,
	/// <summary>
	/// Рядом с объектом появился новый объект того же уровня — клон. Перечитывать
	/// надо родителя, а не сам объект: в дереве новый узел встанет братом.
	/// Десктоп делает ровно это (TreeView2.OnObjectCloned: встать на родителя и
	/// перечитать; OnParentChanged(po, 1) — то же самое другим событием).
	/// </summary>
	SiblingAdded,
	/// <summary>Объект удалён.</summary>
	Deleted
}

/// <summary>
/// Действия над объектами — веб-замена контекстного меню десктопа.
///
/// <b>Меню</b> строится тем же, чем MenuManager.CreatePopupMenu: список
/// ActionList сущности (строки iEntityAction, уже без isHidden и без вложенных),
/// IsActionHidden и IsActionEnabled объекта, вложенные ChildActions, схлопнутые
/// разделители. Отличия от десктопа — только согласованные с владельцем
/// продукта (2026-09-18): «Свойства» наверху под именем «Открыть карточку»,
/// «Удалить» последним и красным, неперенесённые пункты видны серыми.
///
/// <b>Исполнение</b> в десктопе — DoAction объекта, а он живёт в UI-половине
/// (*.WinForms.cs) и почти всегда открывает форму. Здесь вместо него реестр
/// веб-обработчиков: общие действия, которые десктоп выполняет в базовых
/// классах (PresentationObject, ObjectContainer, FakeContainer), плюс
/// псевдонимы — предметные действия, которые доменный класс сам сводит к
/// общему (Massmedia: «Добавить прайс-лист» → AssignNew).
///
/// <b>Защита от тихого расхождения.</b> Если доменный класс в десктопе
/// переопределяет общее действие (свой AssignNew, свой ShowPassport),
/// общий веб-обработчик сделал бы не то, что десктоп. Такие пары перечислены в
/// <see cref="DesktopOverrides"/> и показываются серыми, пока для них нет
/// своего веб-обработчика. Узнать это отражением нельзя: переопределения лежат
/// в UI-половинах, которых в веб-сборке нет вообще.
///
/// <b>Ожидание.</b> Обращения к базе внутри действия (удаление, клон, пересчёт)
/// идут под <see cref="BusyService"/>, показ диалогов — нет: ожидание ответа
/// пользователя не должно выглядеть как «система занята». Загрузку и сохранение
/// карточек оборачивают сами PassportDialog и NamedPassportDialog.
///
/// Scoped — пользуется диалогами circuit.
/// </summary>
public sealed partial class ObjectActions
{
	private readonly PassportDialog _passports;
	private readonly NamedPassportDialog _namedPassports;
	private readonly DialogService _dialogs;
	private readonly TableDialog _tables;
	private readonly BusyService _busy;
	private readonly PeriodDialog _periods;
	private readonly ProgressDialog _progress;
	private readonly FileSaver _saver;

	private readonly FilterMemory _filters;
	private readonly MenuAccess _menuAccess;
	private readonly NavigationManager _navigation;

	public ObjectActions(PassportDialog passports, NamedPassportDialog namedPassports, DialogService dialogs, TableDialog tables,
		BusyService busy, PeriodDialog periods, ProgressDialog progress, FileSaver saver,
		FilterMemory filters, MenuAccess menuAccess, NavigationManager navigation)
	{
		_filters = filters;
		_menuAccess = menuAccess;
		_navigation = navigation;
		_passports = passports;
		_namedPassports = namedPassports;
		_dialogs = dialogs;
		_tables = tables;
		_busy = busy;
		_periods = periods;
		_progress = progress;
		_saver = saver;
	}

	private delegate Task<ActionEffect> Handler(ObjectActions self, object target);

	/// <summary>
	/// Общие действия. Каждое повторяет свою ветку DoAction базового класса
	/// десктопа, но с веб-диалогом вместо формы.
	/// </summary>
	private static readonly Dictionary<string, Handler> Generic = new()
	{
		[Constants.EntityActions.ShowPassport] = (s, t) => s.OpenPassport(t),
		[Constants.EntityActions.Delete] = (s, t) => s.Delete(t),
		[Constants.EntityActions.Refresh] = (_, t) => Task.FromResult(Refresh(t)),
		[Constants.EntityActions.AssignNew] = (s, t) => s.AssignNew(t),
		[Constants.EntityActions.AddNew] = (s, t) => s.AddNew(t),
		[Constants.EntityActions.Clone] = (s, t) => s.Clone(t),
	};

	/// <summary>
	/// Собственные действия доменного класса: в десктопе это ветки его DoAction,
	/// которых нет ни у одного базового класса. Ключ — имя класса, затем имя
	/// действия; действие ищется у самого производного класса и выше, раньше
	/// общих и раньше <see cref="DesktopOverrides"/>, поэтому этим же способом
	/// закрывается и переопределённое общее действие.
	///
	/// Обработчик вызывает метод, который ядро вынесло из DoAction (по
	/// конвенции этапа 0 логика — в ядре, форма — в UI-половине), и возвращает
	/// <see cref="ActionEffect.Changed"/>: владелец экрана перечитывает узел.
	///
	/// Механизм общий, не под один класс: вторым клиентом стал журнал рекламных
	/// акций — ActionContainer (ShowHeadCompanies / ShowFirms / ShowActions)
	/// переключает ChildEntity корня так же, как AdvertTypeContainer.
	/// </summary>
	private static readonly Dictionary<string, Dictionary<string, Handler>> ClassActions = new()
	{
		// AdvertTypeContainer.DoAction: подмена ChildEntity корня (AdvertType ↔ AdvertTypeChild).
		["AdvertTypeContainer"] = new()
		{
			[AdvertTypeContainer.ActionNames.ShowTree] = (s, t) => s.Changed(((AdvertTypeContainer)t).ShowTree),
			[AdvertTypeContainer.ActionNames.ShowFlat] = (s, t) => s.Changed(((AdvertTypeContainer)t).ShowFlat),
		},
		// ActionContainer.DoAction: подмена ChildEntity корня — разбивка акций по
		// группам компаний, по фирмам или без разбивки. Какой пункт сейчас
		// недоступен, решает сам контейнер (IsActionEnabled): серым гасится
		// текущий вид, как в десктопе.
		["ActionContainer"] = new()
		{
			[ActionContainer.ActionNames.ShowHeadCompanies] = (s, t) => s.Changed(((ActionContainer)t).ShowHeadCompanies),
			[ActionContainer.ActionNames.ShowFirms] = (s, t) => s.Changed(((ActionContainer)t).ShowFirms),
			[ActionContainer.ActionNames.ShowActions] = (s, t) => s.Changed(((ActionContainer)t).ShowActions),
		},
		// HeadCompanyWithActions.DoAction: то же у узла группы компаний — дети фирмы или
		// акции (в журнале удалённых — удалённые). Погашен текущий вид (IsActionEnabled).
		// BalanceStatRow.DoAction: из строки журнала бонусов — журнал подтверждённых акций с
		// отбором по строке (в десктопе — новое окно, здесь — переход на экран).
		["BalanceStatRow"] = new()
		{
			[Merlin.Classes.BonusStatRow.OpenActionJournalAction] = (s, t) => s.OpenActionJournal((PresentationObject)t),
		},
		// PackModulePricelist.AssignExisting: «Добавить модуль в пакет» — новая строка
		// «Модули пакетного модуля» (135) с прайс-листом пакета, карточкой.
		["PackModulePricelist"] = new()
		{
			[Constants.EntityActions.AssignExisting] = (s, t) => s.AddModuleToPack((Merlin.Classes.PackModulePricelist)t),
		},
		// HeadCompany.DoAction: «Редактировать дочерние фирмы» — добавить фирмы в группу
		// компаний (выбранные уходят из своих групп; опустевшая группа исчезает).
		["HeadCompany"] = new()
		{
			["EditFirms"] = (s, t) => s.EditHeadCompanyFirms((Merlin.Classes.HeadCompany)t),
		},
		["HeadCompanyWithActions"] = new()
		{
			[Merlin.Classes.HeadCompanyView.ShowFirmsAction] = (s, t) => s.Changed(() => Merlin.Classes.HeadCompanyView.ShowFirms((PresentationObject)t)),
			[Merlin.Classes.HeadCompanyView.ShowActionsAction] = (s, t) => s.Changed(() => Merlin.Classes.HeadCompanyView.ShowActions((PresentationObject)t)),
		},
		// Announcement.DoAction: «Пометить как прочтенное». Доступность гасит
		// Announcement.IsActionEnabled (у прочитанного — серый).
		["Announcement"] = new()
		{
			[Merlin.Classes.Announcement.ActionNames.MarkAsRead] = (s, t) => s.Changed(((Merlin.Classes.Announcement)t).MarkAsRead),
		},
		// PackageDiscount.DoAction: «Добавить прайс-лист» — это AssignNew с временно
		// подменённой дочерней сущностью (у пакетной скидки их две: прайс-листы и
		// радиостанции). Подмена возвращается назад и при отказе от карточки — в
		// десктопе это следующая строка после base.DoAction, здесь finally.
		["PackageDiscount"] = new()
		{
			[Merlin.Classes.PackageDiscount.ActionNames.AssignPriceList] = async (s, t) =>
			{
				var discount = (Merlin.Classes.PackageDiscount)t;
				Entity previous = discount.ChildEntity;
				discount.ChildEntity = EntityManager.GetEntity((int)Merlin.Entities.PackageDiscountPriceLists);
				try
				{
					return await s.AssignNew(discount);
				}
				finally
				{
					discount.ChildEntity = previous;
				}
			},
		},
		// Pricelist.DoAction: диалог дат/режима, затем ApplyClone/ApplyMassClone —
		// не тот же жест, что общий Clone (PresentationObject.CreateCloneDraft +
		// карточка). Регистрация под именем базового класса "Pricelist" ловит и
		// MassmediaPricelist (80), и SponsorPricelist (12): у обоих в метаданных
		// объявлен Clone, а MassClone есть только у 80 — второе имя для 12 в
		// ActionList просто не появится. Перекрывает собой общий Generic[Clone]
		// (ClassActions проверяется раньше него в FindHandler), CreateCloneDraft
		// у Pricelist поэтому не трогаем.
		["Pricelist"] = new()
		{
			[Constants.EntityActions.Clone] = (s, t) => s.ClonePricelist((Merlin.Classes.Pricelist)t, massFlag: false),
			[Merlin.Classes.Pricelist.ActionNames.MassClone] = (s, t) => s.ClonePricelist((Merlin.Classes.Pricelist)t, massFlag: true),
		},
		// MassmediaPricelist.WinForms.cs, DoAction: «Добавить тариф массово» — именованный
		// паспорт TariffMass (см. NamedPassportDialog), не CreateCloneDraft. Класс
		// MassmediaPricelist сам internal (см. комментарий у ["Pricelist"] выше про
		// internal-наследников Pricelist), поэтому цель — публичный базовый тип.
		// Работа с рекламными окнами — остальные ветки того же DoAction. Цель — копия
		// прайс-листа с дочерней сущностью «Рекламное окно» (PricelistWindows.ForWindows):
		// только в таком виде MassmediaPricelist.IsActionEnabled эти пункты разрешает.
		// Вызываются с вкладки «Рекламные окна» (TariffWindowsView).
		["MassmediaPricelist"] = new()
		{
			["AddTariffsMass"] = (s, t) => s.AddTariffsMass((Merlin.Classes.Pricelist)t),
			[Merlin.Classes.PricelistWindows.ActionNames.GenerateWindows] = (s, t) => s.GenerateWindows(t),
			[Merlin.Classes.PricelistWindows.ActionNames.DeleteGeneratedWindows] = (s, t) => s.DeleteGeneratedWindows(t, null),
			[Merlin.Classes.PricelistWindows.ActionNames.DisabledTariffWindows] = (s, t) =>
				s.ChangeWindowsStatus(t, Merlin.Classes.PricelistWindows.ActionNames.DisabledTariffWindows, Tr.T("Запретить вносить выпуски в окна")),
			[Merlin.Classes.PricelistWindows.ActionNames.EnabledTariffWindows] = (s, t) =>
				s.ChangeWindowsStatus(t, Merlin.Classes.PricelistWindows.ActionNames.EnabledTariffWindows, Tr.T("Разрешить вносить выпуски в окна")),
			[Merlin.Classes.PricelistWindows.ActionNames.MarkWindows] = (s, t) =>
				s.ChangeWindowsStatus(t, Merlin.Classes.PricelistWindows.ActionNames.MarkWindows, Tr.T("Пометить окна цветом")),
			[Merlin.Classes.PricelistWindows.ActionNames.UnmarkWindows] = (s, t) =>
				s.ChangeWindowsStatus(t, Merlin.Classes.PricelistWindows.ActionNames.UnmarkWindows, Tr.T("Снять пометку окон цветом")),
			[Merlin.Classes.PricelistWindows.ActionNames.ShowDisabledWindows] = (s, t) => s.ShowDisabledWindows(t),
		},
		// Tariff.WinForms.cs, DoAction: «Изменить похожие тарифы» — второй именованный
		// паспорт (TariffMassEdit). "Clone" у Tariff не задет: ClassActions проверяется по
		// имени действия, а не класса целиком, поэтому общий Generic[Clone] (CreateCloneDraft)
		// по-прежнему обслуживает «Клонировать» этого же класса.
		["Tariff"] = new()
		{
			["EditSimilarTariffs"] = (s, t) => s.EditSimilarTariffs((Merlin.Classes.Tariff)t),
		},
		// CampaignRoller.WinForms.cs, DoAction: «Заменить рекламный ролик» — именованный
		// паспорт RollerSubstitute (дерево выпусков — treeselector). Ловит и
		// CampaignRollerInsideDay (97, ролик под датой в журнале акций): поиск идёт по
		// базовым классам. Выпуск ролика (98) и выпуск модуля (130) — другие классы со
		// своей заменой одного выпуска, сюда не попадают. Сам класс internal, поэтому
		// цель — PresentationObject, а операция — публичный RollerSubstitution.
		["CampaignRoller"] = new()
		{
			[Constants.Actions.Substitute] = (s, t) => s.SubstituteRoller((PresentationObject)t),
		},
		// ActionRollerInStatJournal.WinForms.cs, DoAction: «Назначить предмет рекламы» у
		// строки «Журнала использования роликов» (139). Ролик акции (ActionRoller) с тем же
		// именем действия — другой класс со своим диалогом, сюда не попадает и остаётся
		// серым. Класс internal — операция в публичном RollerStatisticQuery.
		["ActionRollerInStatJournal"] = new()
		{
			[Merlin.Classes.Action.ActionNames.SetAdvertType] = (s, t) => s.SetRollerAdvertType((PresentationObject)t),
		},
		// PaymentCommon.WinForms.cs, DoAction: «Выбрать акции для оплаты» (PaymentCandidatesForm) —
		// в «Журнале оплат». Доступность — PaymentCommon.IsActionEnabled (платёж не распределён).
		["PaymentCommon"] = new()
		{
			[Merlin.Classes.PaymentCommon.ActionNames.SelectActionsToPay] = (s, t) => s.SelectActionsToPay((Merlin.Classes.PaymentCommon)t),
		},
		// Action.WinForms.cs, DoAction: смена фирмы-заказчика и создателя акции.
		["Action"] = new()
		{
			[Merlin.Classes.Action.ActionNames.ChangeFirm] = (s, t) => s.ChangeFirm((Merlin.Classes.Action)t),
			[Merlin.Classes.Action.ActionNames.ChangeCreator] = (s, t) => s.ChangeCreator((Merlin.Classes.Action)t),
			// «Назначить предмет рекламы или заменить ролик» — окно роликов акции (журнал 1244).
			[Merlin.Classes.Action.ActionNames.SetAdvertType] = (s, t) => s.EditActionRollers((Merlin.Classes.Action)t),
			// Документы из Word-шаблонов агентства (docs/tasks/web-reports.md §8), ObjectActions.Documents.cs.
			[Merlin.Classes.Action.ActionNames.PrintContract] = (s, t) => s.PrintActionDocument((Merlin.Classes.Action)t, Merlin.Classes.Documents.DocumentKind.Contract),
			[Merlin.Classes.Action.ActionNames.PrintSponsorContract] = (s, t) => s.PrintActionDocument((Merlin.Classes.Action)t, Merlin.Classes.Documents.DocumentKind.SponsorContract),
			[Merlin.Classes.Action.ActionNames.PrintBillContract] = (s, t) => s.PrintActionDocument((Merlin.Classes.Action)t, Merlin.Classes.Documents.DocumentKind.BillContract),
			[Merlin.Classes.Action.ActionNames.PrintBill] = (s, t) => s.PrintActionDocument((Merlin.Classes.Action)t, Merlin.Classes.Documents.DocumentKind.Bill),
			[Merlin.Classes.Action.ActionNames.PrintBillByMounth] = (s, t) => s.PrintActionDocument((Merlin.Classes.Action)t, Merlin.Classes.Documents.DocumentKind.Bill, byMonth: true),
		},
		// Firm.WinForms.cs, DoAction: договор из карточки фирмы, без акции (ObjectActions.Documents.cs).
		["Firm"] = new()
		{
			[Merlin.Classes.Action.ActionNames.PrintContract] = (s, t) => s.PrintFirmContract((Merlin.Classes.Firm)t, Merlin.Classes.Documents.DocumentKind.Contract),
			[Merlin.Classes.Action.ActionNames.PrintSponsorContract] = (s, t) => s.PrintFirmContract((Merlin.Classes.Firm)t, Merlin.Classes.Documents.DocumentKind.SponsorContract),
		},
		// ActionOnMassmedia.WinForms.cs, DoAction: операции журнала акций, которые решаются
		// вопросом, выбором из списка или небольшим окном. «Восстановить» ловит и удалённую акцию
		// (ActionDeleted — наследник). «Разделить кампании» и клон — следующая партия.
		["ActionOnMassmedia"] = new()
		{
			[Merlin.Classes.Action.ActionNames.Recalculate] = (s, t) => s.RecalculateAction((Merlin.Classes.ActionOnMassmedia)t),
			[Merlin.Classes.Action.ActionNames.Deactivate] = (s, t) => s.DeactivateAction((Merlin.Classes.ActionOnMassmedia)t),
			[Merlin.Classes.Action.ActionNames.Merge] = (s, t) => s.MergeActions((Merlin.Classes.ActionOnMassmedia)t),
			[Merlin.Classes.Action.ActionNames.SplitAction] = (s, t) => s.SplitAction((Merlin.Classes.ActionOnMassmedia)t),
			[Merlin.Classes.Action.ActionNames.ActionRollers] = (s, t) => s.ShowActionRollers((Merlin.Classes.ActionOnMassmedia)t),
			[Merlin.Classes.Action.ActionNames.Restore] = (s, t) => s.RestoreAction((Merlin.Classes.ActionOnMassmedia)t),
			[Merlin.Classes.Action.ActionNames.ChangePaymentTypeMass] = (s, t) => s.ChangePaymentTypeMass((Merlin.Classes.ActionOnMassmedia)t),
			[Merlin.Classes.Action.ActionNames.Activate] = (s, t) => s.ActivateAction((Merlin.Classes.ActionOnMassmedia)t, isTest: false),
			[Merlin.Classes.Action.ActionNames.ActivateTest] = (s, t) => s.ActivateAction((Merlin.Classes.ActionOnMassmedia)t, isTest: true),
		},
		// Campaign.WinForms.cs, DoAction: смена агентства и типа оплаты кампании — у всех
		// видов кампаний (линейная, модульная, спонсорская, пакетная). Класс internal —
		// вход через CampaignChange.
		["Campaign"] = new()
		{
			[Merlin.Classes.CampaignChange.ChangeAgencyAction] = (s, t) => s.ChangeCampaignAgency((PresentationObject)t),
			[Merlin.Classes.CampaignChange.ChangePaymentTypeAction] = (s, t) => s.ChangeCampaignPaymentType((PresentationObject)t),
			[Merlin.Classes.CampaignChange.PrintTransfersAction] = (s, t) => s.ShowCampaignTransfers((PresentationObject)t),
			// Эфирная справка из Word-шаблона агентства (ObjectActions.Documents.cs): кампания на станции и пакетный модуль.
			[Merlin.Classes.Documents.ClientDocuments.PrintOnAirInquireAction] = (s, t) => s.PrintOnAirInquire((PresentationObject)t),
			// Переключение узла кампании в дереве (дни / ролики / пакетные модули) — как у
			// ActionContainer: сменилась дочерняя сущность, узел перечитывается. Доступность
			// (только дерево, не текущий вид) решает IsActionEnabled кампании.
			[Merlin.Classes.CampaignChange.ShowDaysAction] = (s, t) => s.Changed(() => Merlin.Classes.CampaignChange.ShowDays((PresentationObject)t)),
			[Merlin.Classes.CampaignChange.ShowRollersAction] = (s, t) => s.Changed(() => Merlin.Classes.CampaignChange.ShowRollers((PresentationObject)t)),
			[Merlin.Classes.CampaignChange.ShowPackModulesAction] = (s, t) => s.Changed(() => Merlin.Classes.CampaignChange.ShowPackModules((PresentationObject)t)),
		},
		// CampaignDay.WinForms.cs, DoAction: «Перенос дня» — дни линейной, модульной и пакетной
		// кампании (ModuleCampaignDay, CampaignPackDay — наследники). Вход — CampaignDayTransfer.
		["CampaignDay"] = new()
		{
			[Merlin.Classes.CampaignDayTransfer.TransferAction] = (s, t) => s.TransferDay((PresentationObject)t),
		},
		// ActionRoller.WinForms.cs, DoAction: строки окна роликов акции. CommonRoller (ролик
		// «для всех фирм») — наследник, отличие внутри ActionRollerChange.SetAdvertType.
		["ActionRoller"] = new()
		{
			[Merlin.Classes.ActionRollerChange.SetAdvertTypeAction] = (s, t) => s.SetActionRollerAdvertType((PresentationObject)t),
			[Merlin.Classes.ActionRollerChange.SubstituteAction] = (s, t) => s.SubstituteActionRoller((PresentationObject)t),
		},
	};

	private async Task<ActionEffect> Changed(Action apply)
	{
		await _busy.RunAsync(apply);
		return ActionEffect.Changed;
	}

	/// <summary>
	/// Предметные действия, которые десктопный DoAction класса передаёт общему.
	/// Ключ — имя класса, как и в <see cref="DesktopOverrides"/>.
	/// </summary>
	private static readonly Dictionary<string, Dictionary<string, string>> Aliases = new()
	{
		// Massmedia.WinForms.cs, DoAction: четыре пункта «Добавить …» — это
		// base.DoAction(AssignNew). Какую сущность добавлять, решает сценарий
		// дерева (ChildEntity), и доступен только пункт, совпадающий с ней
		// (Massmedia.IsActionEnabled).
		["Massmedia"] = new()
		{
			["AddSponsorProgram"] = Constants.EntityActions.AssignNew,
			["AddPriceList"] = Constants.EntityActions.AssignNew,
			["AddModule"] = Constants.EntityActions.AssignNew,
			["AssignRelease"] = Constants.EntityActions.AssignNew,
		},
	};

	/// <summary>
	/// Доменные классы, которые в десктопе переопределяют общее действие своим
	/// поведением (поиск по «override … AssignNew/ShowPassport/Delete» и по
	/// веткам DoAction на 2026-09-18). Действие у них серое, пока нет своего
	/// веб-обработчика.
	///
	/// Не включены Agency, Roller, SponsorTariff: их ShowPassport — копия
	/// базового с другим набором данных карточки, а веб грузит данные той же
	/// процедурой (LoadPassportData). Карточка SponsorTariff сверена с
	/// десктопом поле в поле (сверка десяти сущностей, этап 2).
	/// </summary>
	private static readonly Dictionary<string, string[]> DesktopOverrides = new()
	{
		// «Свойства» открывают форму редактирования акции (ActionForm) — этап 3.
		["ActionOnMassmedia"] = new[] { Constants.EntityActions.ShowPassport },
		// Свой AssignNew: выбор из списка, мастер, набор галочками.
		["AdvertType"] = new[] { Constants.EntityActions.AssignNew },
		["ComboModuleContainer"] = new[] { Constants.EntityActions.AssignNew },
		["PackageDiscountPriceList"] = new[] { Constants.EntityActions.AssignNew },
		// Своё удаление: пересчёт, каскад, подтверждение другим текстом.
		["MasterIssue"] = new[] { Constants.EntityActions.Delete },
		["ModuleIssue"] = new[] { Constants.EntityActions.Delete },
		["CampaignPart"] = new[] { Constants.EntityActions.Delete },
		// Своё обновление в DoAction.
		["ProgramPartOfSponsorCampaign"] = new[] { Constants.EntityActions.Refresh },
		["RollerPartOfSponsorCampaign"] = new[] { Constants.EntityActions.Refresh },
		["SponsorProgramPart"] = new[] { Constants.EntityActions.Refresh },
	};

	/// <summary>Действия, которые выносятся иконкой прямо в строку списка.</summary>
	public static readonly string[] QuickActionNames = { Constants.EntityActions.Delete };

	// Имена полей паспортов TariffMass/TariffMassEdit (AddTariffsMass/EditSimilarTariffs
	// ниже) — те же строки, что в десктопных приватных константах
	// MassmediaPricelist.WinForms.TariffMassHourFromParam и Tariff.WinForms.MassHourFromParam
	// (и родня); своя копия здесь по той же причине, что и там — UI-половины десктопа
	// в веб-сборку не попадают.
	private const string MassMinuteParam = "tariffMinute";
	private const string MassHourFromParam = "hourFrom";
	private const string MassHourToParam = "hourTo";
	private const string MassHintParam = "massEditHint";
	private const string MassDaysHintParam = "massDaysHint";

	// ---------- Меню ----------

	/// <summary>
	/// Меню для объекта: строки списка (PresentationObject) или узла дерева
	/// (PresentationObject или корневой FakeContainer).
	/// </summary>
	public IReadOnlyList<ActionMenuItem> BuildMenu(object target, ViewType view)
	{
		Entity.Action[]? actions = ActionList(target);
		if (actions == null)
			return Array.Empty<ActionMenuItem>();

		ActionMenuItem? properties = null;
		ActionMenuItem? delete = null;
		var middle = new List<ActionMenuItem>();

		foreach (Entity.Action action in actions)
		{
			if (action.Alias == "-")
			{
				middle.Add(ActionMenuItem.Separator);
				continue;
			}

			if (IsHidden(target, action.Name, view))
				continue;

			ActionMenuItem item = CreateItem(target, action, view);
			if (action.Name == Constants.EntityActions.ShowPassport)
				properties = item;
			else if (action.Name == Constants.EntityActions.Delete)
				delete = item;
			else
				middle.Add(item);
		}

		var result = new List<ActionMenuItem>();
		if (properties != null)
			result.Add(properties);
		result.AddRange(middle);
		if (delete != null)
		{
			result.Add(ActionMenuItem.Separator);
			result.Add(delete);
		}

		return CollapseSeparators(result);
	}

	private ActionMenuItem CreateItem(object target, Entity.Action action, ViewType view)
	{
		var children = new List<ActionMenuItem>();
		foreach (Entity.Action child in action.ChildActions)
			children.Add(child.Alias == "-" ? ActionMenuItem.Separator : CreateItem(target, child, view));

		bool isProperties = action.Name == Constants.EntityActions.ShowPassport;
		IReadOnlyList<ActionMenuItem> collapsed = CollapseSeparators(children);
		bool enabled = IsEnabled(target, action.Name, view);

		return new ActionMenuItem
		{
			Name = action.Name,
			Text = isProperties ? Tr.T("Открыть карточку") : Tr.T(action.Alias),
			// В списке карточку открывает клик по строке, в дереве клик выбирает узел.
			Hint = isProperties && view == ViewType.Journal ? Tr.T("клик") : null,
			Icon = ActionIcons.For(action.Name, action.ImgResourceName),
			Enabled = enabled,
			DisabledReason = enabled ? null : DisabledReason(target, action.Name),
			Ported = collapsed.Count > 0 ? collapsed.Any(c => c.Ported) : IsPorted(target, action.Name),
			Danger = action.Name == Constants.EntityActions.Delete,
			Children = collapsed,
		};
	}

	/// <summary>Без разделителей в начале, в конце и подряд — как в MenuManager.</summary>
	private static IReadOnlyList<ActionMenuItem> CollapseSeparators(List<ActionMenuItem> items)
	{
		var result = new List<ActionMenuItem>();
		foreach (ActionMenuItem item in items)
		{
			if (item.IsSeparator && (result.Count == 0 || result[^1].IsSeparator))
				continue;
			result.Add(item);
		}
		if (result.Count > 0 && result[^1].IsSeparator)
			result.RemoveAt(result.Count - 1);
		return result;
	}

	// ---------- Быстрые действия строки ----------

	/// <summary>
	/// Какие быстрые действия показывать у строк сущности. Решается по
	/// сущности, а не по каждой строке: иконки видны всегда, доступность
	/// проверяется при нажатии (решение 2026-09-18) — иначе пришлось бы
	/// поднимать доменный объект на каждую видимую строку при каждой отрисовке.
	/// </summary>
	public IReadOnlyList<ActionMenuItem> QuickActions(Entity entity, PresentationObject sample)
	{
		var result = new List<ActionMenuItem>();
		foreach (string name in QuickActionNames)
		{
			Entity.Action? action = entity.ActionList?.FirstOrDefault(a => a.Name == name);
			if (action == null || !IsPorted(sample, name))
				continue;

			result.Add(new ActionMenuItem
			{
				Name = name,
				Text = Tr.T(action.Alias),
				Icon = ActionIcons.For(action.Name, action.ImgResourceName),
				Enabled = true,
				Ported = true,
				Danger = name == Constants.EntityActions.Delete,
			});
		}
		return result;
	}

	// ---------- Исполнение ----------

	/// <summary>
	/// Выполнить действие. Доступность проверяется здесь ещё раз: быстрые
	/// иконки её заранее не проверяют. Права на уровне процедур дополнительно
	/// проверяет WebActionAuthorization.
	/// </summary>
	/// <returns>Что изменилось; null — действие недоступно или не перенесено.</returns>
	public async Task<ActionEffect?> ExecuteAsync(object target, string actionName, ViewType view)
	{
		if (!IsEnabled(target, actionName, view) || IsHidden(target, actionName, view))
			return null;

		Handler? handler = FindHandler(target, actionName);
		if (handler == null)
			return null;

		return await handler(this, target);
	}

	/// <summary>
	/// Выполнится ли <see cref="ExecuteAsync"/> для этого объекта: то же условие,
	/// что проверяет само исполнение. Нужно кнопке «для всех строк», чтобы знать
	/// заранее, есть ли что делать.
	/// </summary>
	public bool CanExecute(object target, string actionName, ViewType view) =>
		IsEnabled(target, actionName, view) && !IsHidden(target, actionName, view) && IsPorted(target, actionName);

	/// <summary>Есть ли у веба обработчик этого действия для этого объекта.</summary>
	public bool IsPorted(object target, string actionName) => FindHandler(target, actionName) != null;

	private static Handler? FindHandler(object target, string actionName)
	{
		foreach (string cls in ClassNames(target))
			if (ClassActions.TryGetValue(cls, out var own) && own.TryGetValue(actionName, out Handler? ownHandler))
				return ownHandler;

		string effective = actionName;
		foreach (string cls in ClassNames(target))
			if (Aliases.TryGetValue(cls, out var map) && map.TryGetValue(actionName, out string? generic))
			{
				effective = generic;
				break;
			}

		foreach (string cls in ClassNames(target))
			if (DesktopOverrides.TryGetValue(cls, out string[]? overridden) && overridden.Contains(effective))
				return null;

		if (!Generic.TryGetValue(effective, out Handler? handler))
			return null;

		// Общее действие существует только у того базового класса, чья ветка
		// DoAction его выполняет.
		bool applicable = effective switch
		{
			Constants.EntityActions.AssignNew => target is ObjectContainer,
			Constants.EntityActions.AddNew => target is FakeContainer,
			Constants.EntityActions.Refresh => target is PresentationObject or FakeContainer,
			// Умеет ли класс клонироваться, отвечает он сам: нет черновика — нет и
			// веб-обработчика, пункт серый. Черновик — копия словаря параметров, без
			// обращения к базе, поэтому спросить можно и при построении меню.
			Constants.EntityActions.Clone => target is PresentationObject clonable && clonable.CreateCloneDraft() != null,
			_ => target is PresentationObject,
		};
		return applicable ? handler : null;
	}

	/// <summary>Имена класса объекта и всех его базовых — переопределение наследуется.</summary>
	private static IEnumerable<string> ClassNames(object target)
	{
		for (Type? t = target.GetType(); t != null && t != typeof(object); t = t.BaseType)
			yield return t.Name;
	}

	private async Task<ActionEffect> OpenPassport(object target)
	{
		var obj = (PresentationObject)target;
		return await _passports.ShowAsync(obj, isNew: false) ? ActionEffect.Changed : ActionEffect.None;
	}

	/// <summary>
	/// Спрашиваем сами и зовём Delete(silenceFlag: true), а не Delete():
	/// десктоп спрашивает изнутри Delete() синхронно, а circuit нельзя
	/// заблокировать в ожидании ответа. Разрез «спросить / сделать», конвенция
	/// этапа 0; текст вопроса — тот же DeleteConfirmationText.
	/// </summary>
	private async Task<ActionEffect> Delete(object target)
	{
		var obj = (PresentationObject)target;

		if (await _dialogs.ShowAsync(
				Tr.T("Удаление"),
				builder => builder.AddContent(0, obj.DeleteConfirmationText),
				okText: Tr.T("Удалить")) != DialogOutcome.Ok)
			return ActionEffect.None;

		return await _busy.RunAsync(() => obj.Delete(silenceFlag: true)) ? ActionEffect.Deleted : ActionEffect.None;
	}

	/// <summary>
	/// Массовое удаление отмеченных чекбоксами объектов — веб-аналог
	/// SmartGrid.DeleteSelectedObjects. Вызывается кнопкой «Удалить (N)»
	/// тулбара экрана (ObjectList только собирает отметки, само удаление —
	/// здесь же, где и одиночное).
	///
	/// Один вопрос на всю пачку, затем по каждому объекту — ровно десктопная
	/// логика: IsActionEnabled → Delete(silenceFlag: true) → сбор ошибок,
	/// исключение тоже уходит строкой в тот же список. <b>Не повторяем</b>
	/// десктопный дефект: там при недоступном удалении у ПЕРВОГО объекта метод
	/// молча выходит и не делает ничего (SmartGrid.cs, проверка firstPo перед
	/// вопросом). Здесь такой объект просто попадает в список ошибок наравне с
	/// остальными, а доступные всё равно удаляются.
	///
	/// Итоги — как и у остальных десяти мест десктопа (сравнение с ошибками
	/// клонирования): без ошибок ничего не показываем, список у владельца
	/// экрана просто перечитывается; с ошибками — TableDialog с виртуальной
	/// сущностью и двумя колонками. Решение владельца продукта 2026-09-22:
	/// модальное окно про успех в вебе лишнее — строки и так исчезли из списка.
	/// </summary>
	/// <returns>
	/// null — пользователь отменил вопрос или отмеченных объектов нет: ничего
	/// не делать. ActionEffect.Deleted — операция прошла (возможно, частично, с
	/// ошибками): владелец экрана перечитывает список, как и после одиночного
	/// удаления.
	/// </returns>
	public async Task<ActionEffect?> DeleteSelectedAsync(IReadOnlyList<PresentationObject> objects)
	{
		if (objects.Count == 0)
			return null;

		string question = Tr.Format("Вы действительно хотите удалить выбранные объекты? ({0} шт.)", objects.Count);
		if (await _dialogs.ShowAsync(Tr.T("Удаление"), builder => builder.AddContent(0, question), okText: Tr.T("Удалить")) != DialogOutcome.Ok)
			return null;

		DataTable errors = new();
		errors.Columns.Add("objectName", typeof(string));
		errors.Columns.Add("errorText", typeof(string));

		await _busy.RunAsync(() =>
		{
			foreach (PresentationObject obj in objects)
			{
				string objectName = string.IsNullOrEmpty(obj.Name) ? Tr.T("<без названия>") : obj.Name;

				try
				{
					if (!obj.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal))
					{
						AddDeleteError(errors, objectName, Tr.Format("Удаление недоступно для объекта '{0}'.", objectName));
						continue;
					}

					if (!obj.Delete(silenceFlag: true))
						AddDeleteError(errors, objectName, Tr.Format("Не удалось удалить объект '{0}'.", objectName));
				}
				catch (Exception ex)
				{
					AddDeleteError(errors, objectName, ErrorPresenter.Describe(ex));
				}
			}
		});

		if (errors.Rows.Count > 0)
			await _tables.ShowAsync(Tr.T("Ошибки массового удаления"), errors,
				new Entity.Attribute("objectName", "Объект", "nvarchar"), // i18n-ok: Alias переводится при показе (ObjectList)
				new Entity.Attribute("errorText", "Ошибка", "nvarchar")); // i18n-ok: Alias переводится при показе (ObjectList)

		return ActionEffect.Deleted;
	}

	private static void AddDeleteError(DataTable table, string objectName, string errorText)
	{
		DataRow row = table.NewRow();
		row["objectName"] = objectName;
		row["errorText"] = errorText;
		table.Rows.Add(row);
	}

	/// <summary>
	/// Сам перечёт делает владелец экрана — он знает, что показывает. Здесь
	/// только сброс кэша контейнера, как в ветке Refresh у ObjectContainer.
	/// </summary>
	private static ActionEffect Refresh(object target)
	{
		if (target is IObjectContainer container)
			container.ClearCache();
		return ActionEffect.Changed;
	}

	/// <summary>ObjectContainer.AssignNew: подготовка и завершение — ядро, карточка — веб.</summary>
	private async Task<ActionEffect> AssignNew(object target)
	{
		var container = (ObjectContainer)target;
		PresentationObject? newObject = await _busy.RunAsync(container.CreateNewChild);
		if (newObject == null || !await _passports.ShowAsync(newObject, isNew: true))
			return ActionEffect.None;

		await _busy.RunAsync(() => container.CompleteNewChild(newObject));
		return ActionEffect.ChildAdded;
	}

	/// <summary>
	/// «Клонировать» — одно на все сущности: черновик со всеми посеянными значениями
	/// собирает сам класс (PresentationObject.CreateCloneDraft), веб только показывает
	/// его карточку и сохраняет, как у нового объекта. Шестая сущность с клонированием
	/// заработает здесь без единой строки веб-кода.
	///
	/// Записывает черновик обычный Update() внутри PassportDialog — тот же путь, что у
	/// десктопного ShowPassport. Доступность пункта решает IsActionEnabled класса
	/// (например ModulePricelist разрешает администратору) — здесь ничего не проверяем.
	/// </summary>
	private async Task<ActionEffect> Clone(object target)
	{
		PresentationObject? draft = ((PresentationObject)target).CreateCloneDraft();
		if (draft == null || !await _passports.ShowAsync(draft, isNew: true))
			return ActionEffect.None;

		return ActionEffect.SiblingAdded;
	}

	/// <summary>
	/// «Клонировать» / «Клонировать на несколько радиостанций» у прайс-листа —
	/// не общий жест Clone (CreateCloneDraft + карточка): диалог собирает даты и
	/// режим клонирования тарифов, ядро зовёт Pricelist.ApplyClone/ApplyMassClone
	/// напрямую. Веб-аналог Pricelist.WinForms.cs (ClonePriceList): показ
	/// PricelistCloneForm, затем либо сразу применение, либо SelectionForm по
	/// радиостанциям. Проверка "начало позже окончания" — как в
	/// PricelistCloneForm.btnOk_Click, до применения; диалог при ошибке
	/// показывается заново с сообщением, приём — как в PassportDialog.ShowAsync.
	/// </summary>
	private async Task<ActionEffect> ClonePricelist(Merlin.Classes.Pricelist pricelist, bool massFlag)
	{
		PricelistCloneDialog? dialog = null;
		string? message = null;

		// Введённые значения переживают повторный показ диалога (см. комментарий
		// у PricelistCloneDialog.StartDate) — на каждом обороте цикла заново
		// засеиваем компонент тем, что пользователь уже ввёл, а не значениями по
		// умолчанию.
		pricelist.GetClonePeriod(out DateTime startDate, out DateTime finishDate);
		Merlin.Classes.PricelistCloneMode mode = Merlin.Classes.PricelistCloneMode.WithWindowChanges;

		while (true)
		{
			RenderFragment body = builder =>
			{
				if (message != null)
				{
					builder.OpenElement(0, "div");
					builder.AddAttribute(1, "class", "alert alert-danger");
					builder.AddContent(2, message);
					builder.CloseElement();
				}

				builder.OpenComponent<PricelistCloneDialog>(3);
				builder.AddComponentParameter(4, nameof(PricelistCloneDialog.SupportsCloneModes), pricelist.SupportsCloneModes);
				builder.AddComponentParameter(5, nameof(PricelistCloneDialog.StartDate), startDate);
				builder.AddComponentParameter(6, nameof(PricelistCloneDialog.FinishDate), finishDate);
				builder.AddComponentParameter(7, nameof(PricelistCloneDialog.Mode), mode);
				builder.AddComponentReferenceCapture(8, c => dialog = (PricelistCloneDialog)c);
				builder.CloseComponent();
			};

			if (await _dialogs.ShowAsync(Tr.T("Клонирование прайс-листа"), body) != DialogOutcome.Ok)
				return ActionEffect.None;

			startDate = dialog!.StartDate;
			finishDate = dialog.FinishDate;
			mode = dialog.Mode;

			if (startDate > finishDate)
			{
				message = MessageAccessor.GetMessage("StartFinishDateError");
				continue;
			}

			Merlin.Classes.PricelistCloneMode? appliedMode = pricelist.SupportsCloneModes ? mode : null;

			if (!massFlag)
			{
				await _busy.RunAsync(() => pricelist.ApplyClone(startDate, finishDate, appliedMode));
				return ActionEffect.SiblingAdded;
			}

			return await CloneToSelectedRadioStations(pricelist, startDate, finishDate, appliedMode);
		}
	}

	/// <summary>
	/// Выбор радиостанций для массового клонирования — веб-аналог
	/// <c>SelectionForm(EntityManager.GetEntity(MassMedia), "Радиостанции", true, CheckSelectionResult)</c>.
	/// Таблицу ошибок <c>Pricelist.ApplyMassClone</c> десктоп показывает
	/// <c>Globals.ShowSimpleJournal</c> — у веб-журнала нет режима "готовая
	/// таблица" (движок строит список сам, не принимает чужой DataTable), поэтому
	/// здесь просто список ошибок в диалоге, а не журналом.
	/// </summary>
	private async Task<ActionEffect> CloneToSelectedRadioStations(Merlin.Classes.Pricelist pricelist, DateTime startDate, DateTime finishDate, Merlin.Classes.PricelistCloneMode? mode)
	{
		var picker = new PassportPicker("massmedia", null, false, Array.Empty<PassportFilterValue>());
		ObjectSelector? selector = null;
		string? message = null;
		IReadOnlyList<DataRow>? previouslySelected = null;

		while (true)
		{
			RenderFragment body = builder =>
			{
				if (message != null)
				{
					builder.OpenElement(0, "div");
					builder.AddAttribute(1, "class", "alert alert-danger");
					builder.AddContent(2, message);
					builder.CloseElement();
				}

				builder.OpenComponent<ObjectSelector>(3);
				builder.AddComponentParameter(4, nameof(ObjectSelector.Picker), picker);
				builder.AddComponentParameter(5, nameof(ObjectSelector.Multiselect), true);
				builder.AddComponentParameter(6, nameof(ObjectSelector.InitialSelectedRows), previouslySelected);
				builder.AddComponentReferenceCapture(7, c => selector = (ObjectSelector)c);
				builder.CloseComponent();
			};

			if (await _dialogs.ShowAsync(Tr.T("Радиостанции"), body, okText: Tr.T("Клонировать")) != DialogOutcome.Ok)
				return ActionEffect.None;

			IReadOnlyList<DataRow> selected = selector!.SelectedRows;
			previouslySelected = selected;
			if (!pricelist.IsMassCloneSelectionValid(selected.Count))
			{
				// Тот же текст, что десктопный Properties.Resources.NoRadiostationSelected
				// (CheckSelectionResult) — это не бизнес-ошибка процедуры, MessageAccessor
				// такого ключа не знает.
				message = Tr.T("Необходимо выбрать хотя бы одну радиостанцию.");
				continue;
			}

			Entity massmedia = EntityManager.GetEntity((int)Merlin.Entities.MassMedia);
			List<PresentationObject> radioStations = selected.Select(massmedia.CreateObject).ToList();

			DataTable errors = await _busy.RunAsync(() => pricelist.ApplyMassClone(startDate, finishDate, mode, radioStations));
			if (errors.Rows.Count > 0)
				await ShowCloneErrors(errors);

			return ActionEffect.SiblingAdded;
		}
	}

	/// <summary>
	/// Итоги массового клонирования — общим показом готовой таблицы
	/// (<see cref="TableDialog"/>), как это делает десктоп через
	/// Globals.ShowSimpleJournal. Колонка одна: Pricelist.CreateErrorTable
	/// складывает имя станции и текст отказа в одну строку description.
	/// </summary>
	private Task ShowCloneErrors(DataTable errors) =>
		_tables.ShowAsync(Tr.T("Ошибки клонирования"), errors,
			new Entity.Attribute("description", "Ошибка", "nvarchar")); // i18n-ok: Alias переводится при показе (ObjectList)

	/// <summary>
	/// «Добавить тариф массово» — веб-аналог MassmediaPricelist.WinForms.AddTariffsMass:
	/// шаблон-тариф с посеянным pricelistID → именованный паспорт TariffMass →
	/// по «ОК» Tariff.CreateMass ставит по тарифу в каждый час интервала (один
	/// TariffIUD на час, best-effort). Данные паспорта (справочник типов блока)
	/// грузятся той же процедурой, что у обычной карточки тарифа — тем же путём,
	/// каким это делает сам PresentationObject.LoadPassportData() у шаблона.
	///
	/// Итог — короткая сводка (счётчики иначе не узнать), в отличие от массового
	/// удаления, где список сам показывает результат исчезновением строк.
	/// Ошибки — TableDialog, тот же общий механизм, что у ошибок клонирования
	/// прайс-листа чуть выше.
	///
	/// Эффект — ChildAdded, а не SiblingAdded: действие вызвано на самом
	/// прайс-листе (контейнере), и десктоп после операции зовёт
	/// FireContainerRefreshed() — «перечитай СВОИХ детей», а не «перечитай
	/// родителя», как у «Изменить похожие тарифы» ниже (там действие на тарифе,
	/// и новый тариф встаёт рядом с ним, братом, а не под ним).
	/// </summary>
	private async Task<ActionEffect> AddTariffsMass(Merlin.Classes.Pricelist pricelist)
	{
		Entity tariffEntity = EntityManager.GetEntity((int)Merlin.Entities.Tariff);
		PresentationObject template = tariffEntity.NewObject;
		template[Merlin.Classes.Pricelist.ParamNames.PricelistId] = pricelist.PricelistId;

		int created = 0;
		DataTable? tableErrors = null;

		bool ok = await _namedPassports.ShowAsync(template, "TariffMass", Tr.T("Добавить тариф массово"), isNew: true,
			edited => Merlin.Classes.Tariff.ValidateMassCreateHours(
				Convert.ToInt32(edited[MassHourFromParam]), Convert.ToInt32(edited[MassHourToParam])),
			edited => created = Merlin.Classes.Tariff.CreateMass(edited,
				Convert.ToInt32(edited[MassHourFromParam]), Convert.ToInt32(edited[MassHourToParam]),
				Convert.ToInt32(edited[MassMinuteParam]), out tableErrors));

		if (!ok || tableErrors == null)
			return ActionEffect.None;

		if (tableErrors.Rows.Count > 0)
			await _tables.ShowAsync(
				Tr.Format("Создано тарифов: {0}, не создано: {1}", created, tableErrors.Rows.Count),
				tableErrors, new Entity.Attribute("description", "Ошибка", "nvarchar")); // i18n-ok: Alias переводится при показе (ObjectList)
		else
			await ShowInfo(Tr.T("Готово"), Tr.Format("Создано тарифов: {0}", created));

		return ActionEffect.ChildAdded;
	}

	/// <summary>
	/// «Изменить похожие тарифы» — веб-аналог Tariff.WinForms.EditSimilarTariffs:
	/// диапазон часов «похожих» тарифов (Tariff.LoadSimilarTariffs — тот же
	/// прайс-лист, та же минута, совпадение остальных атрибутов) подсказкой в
	/// заголовке и в паспорте, дни недели в форме — область применения
	/// (Tariff.ApplyMassEdit: все дни исходного отмечены — тариф правится на
	/// месте, часть — делится на два). Именованный паспорт TariffMassEdit,
	/// шаблон засеян значениями этого тарифа (isNew: false — это не мастер
	/// создания, а форма редактирования, как и в десктопе).
	///
	/// Эффект — SiblingAdded: действие вызвано на самом тарифе, и делёж создаёт
	/// новый тариф РЯДОМ с ним, а не под ним (десктоп — OnParentChanged(this,
	/// Pricelist), «перечитать родителя»). Перечитываем только если реально
	/// что-то изменилось или добавилось — как и десктопный
	/// `if (changed.Count + added.Count > 0)` перед OnParentChanged.
	/// </summary>
	private async Task<ActionEffect> EditSimilarTariffs(Merlin.Classes.Tariff tariff)
	{
		DataTable similar = await _busy.RunAsync(tariff.LoadSimilarTariffs);
		if (similar.Rows.Count == 0)
			return ActionEffect.None;

		int minHour = int.MaxValue, maxHour = int.MinValue;
		foreach (DataRow row in similar.Rows)
		{
			int hour = Convert.ToInt32(row["hour"]);
			minHour = Math.Min(minHour, hour);
			maxHour = Math.Max(maxHour, hour);
		}

		Dictionary<string, object> original = tariff.Parameters;
		int minute = Convert.ToDateTime(original[Merlin.Classes.Tariff.ParamNames.Time]).Minute;

		Merlin.Classes.Tariff template = new() { Parameters = tariff.Parameters };
		template[MassMinuteParam] = minute;
		template[MassHourFromParam] = minHour;
		template[MassHourToParam] = maxHour;
		template[MassHintParam] = Tr.Format("{0} шт., минута :{1:00}, часы {2}-{3}",
			similar.Rows.Count, minute, minHour, maxHour);
		template[MassDaysHintParam] = Tr.T("снимите дни, которые менять не нужно");

		DataTable? tableErrors = null;
		List<Merlin.Classes.Tariff>? changed = null;
		List<Merlin.Classes.Tariff>? added = null;

		bool ok = await _namedPassports.ShowAsync(template, "TariffMassEdit",
			Tr.Format("Изменить похожие тарифы ({0} шт.)", similar.Rows.Count), isNew: false,
			edited => Merlin.Classes.Tariff.ValidateMassEdit(original, edited,
				Convert.ToInt32(edited[MassHourFromParam]), Convert.ToInt32(edited[MassHourToParam]), Convert.ToInt32(edited[MassMinuteParam])),
			edited => Merlin.Classes.Tariff.ApplyMassEdit(original, edited, similar,
				Convert.ToInt32(edited[MassHourFromParam]), Convert.ToInt32(edited[MassHourToParam]), Convert.ToInt32(edited[MassMinuteParam]),
				out tableErrors, out changed, out added));

		if (!ok || tableErrors == null)
			return ActionEffect.None;

		if (tableErrors.Rows.Count > 0)
			await _tables.ShowAsync(
				Tr.Format("Изменено тарифов: {0}, создано новых: {1}, не обработано: {2}",
					changed!.Count, added!.Count, tableErrors.Rows.Count),
				tableErrors, new Entity.Attribute("description", "Ошибка", "nvarchar")); // i18n-ok: Alias переводится при показе (ObjectList)
		else
			await ShowInfo(Tr.T("Готово"), Tr.Format("Изменено тарифов: {0}, создано новых: {1}", changed!.Count, added!.Count));

		return changed!.Count + added!.Count > 0 ? ActionEffect.SiblingAdded : ActionEffect.None;
	}

	/// <summary>
	/// «Заменить рекламный ролик» — веб-аналог CampaignRoller.WinForms.SubstituteRoller +
	/// RollerSubstitutionForm: именованный паспорт RollerSubstitute с данными своей
	/// процедуры (не карточки), по «ОК» — проверки формы в её порядке, запись
	/// RollerSubstitution.Apply, таблица незаменённых роликов, пересчёт акции.
	///
	/// <b>Пересчёт — только если длина нового ролика другая</b> (PriceMayChange):
	/// процедура RollerSubstitute при равной длине не трогает ни цену выпусков, ни
	/// занятость окон, и ActionRecalculate был бы пустой тратой — та же оптимизация,
	/// что в десктопе.
	///
	/// Эффект — SiblingAdded: десктоп зовёт OnParentChanged(.., GeneralCampaign), под
	/// датой меняется состав роликов, перечитывать надо родителя.
	/// </summary>
	private async Task<ActionEffect> SubstituteRoller(PresentationObject campaignRoller)
	{
		var substitution = Merlin.Classes.RollerSubstitution.ForCampaignRoller(campaignRoller);
		DataSet data = await _busy.RunAsync(substitution.LoadPassportData);
		bool hasRollers = Merlin.Classes.RollerSubstitution.HasRollers(data);

		// Черновик — носитель значений паспорта, как PageContext.Parameters у формы;
		// сам ролик кампании не трогаем: поле rollerID паспорта — его же ключ.
		PresentationObject template = EntityManager.GetEntity((int)Merlin.Entities.CampaignRoller).NewObject;
		foreach (KeyValuePair<string, object> p in substitution.CreatePassportParameters(data))
			template[p.Key] = p.Value;
		// OnLoad + UpdateControlsStatus: галочка «молчание» снята, а если менять не на
		// что — включена принудительно (и недоступна, см. SubstitutionFieldDisabled).
		template[SubstituteParams.SubstituteMute] = !hasRollers;

		Merlin.Classes.Roller? newRoller = null;
		DataTable? selectedDays = null;
		DataTable? unsubstituted = null;

		bool ok = await _namedPassports.ShowAsync(template, Merlin.Classes.RollerSubstitution.PassportName,
			Tr.T("Замена ролика"), isNew: false,
			values => ValidateSubstitution(substitution, values, out selectedDays, out newRoller),
			_ => unsubstituted = substitution.Apply(newRoller!, selectedDays!),
			data, name => SubstitutionFieldDisabled(name, template, hasRollers));

		if (!ok)
			return ActionEffect.None;

		if (unsubstituted != null && unsubstituted.Rows.Count > 0)
			await _tables.ShowAsync(Tr.T("Незамененные ролики"), unsubstituted,
				new Entity.Attribute("windowDateOriginal", "Дата выпуска", "datetime"), // i18n-ok: Alias переводится при показе (ObjectList)
				new Entity.Attribute("message", "Ошибка", "nvarchar")); // i18n-ok: Alias переводится при показе (ObjectList)

		if (substitution.PriceMayChange(newRoller!))
			await ShowInfo(Tr.T("Замена ролика"), await _busy.RunAsync(substitution.RecalculateAction));

		return ActionEffect.SiblingAdded;
	}

	private struct SubstituteParams
	{
		public const string SubstituteMute = Merlin.Classes.RollerSubstitution.ParamNames.SubstituteMute;
		public const string MuteDuration = Merlin.Classes.RollerSubstitution.ParamNames.MuteDuration;
		public const string RollerId = Merlin.Classes.RollerSubstitution.ParamNames.RollerId;
		public const string AdvertTypeId = Merlin.Classes.RollerSubstitution.ParamNames.AdvertTypeId;
		public const string Days = Merlin.Classes.RollerSubstitution.ParamNames.Days;
	}

	/// <summary>
	/// RollerSubstitutionForm.UpdateControlsStatus: галочка «молчание» доступна, только
	/// если есть на что менять; длительность молчания — только при галочке; список
	/// роликов — только без неё.
	/// </summary>
	private static bool SubstitutionFieldDisabled(string name, PresentationObject template, bool hasRollers)
	{
		bool mute = template[SubstituteParams.SubstituteMute] is true;
		return name switch
		{
			SubstituteParams.SubstituteMute => !hasRollers,
			SubstituteParams.MuteDuration => !mute,
			SubstituteParams.RollerId => mute || !hasRollers,
			_ => false,
		};
	}

	/// <summary>
	/// Проверки RollerSubstitutionForm.ApplyChanges в том же порядке: выбраны ли
	/// выпуски → новый ролик (молчание с проверками либо выбранный в списке) →
	/// предмет рекламы в подтверждённой акции. Сами правила — в ядре
	/// (RollerSubstitution), здесь только последовательность.
	/// </summary>
	private static string? ValidateSubstitution(Merlin.Classes.RollerSubstitution substitution,
		Dictionary<string, object> values, out DataTable? selectedDays, out Merlin.Classes.Roller? newRoller)
	{
		selectedDays = null;
		newRoller = null;

		if (values.TryGetValue(SubstituteParams.Days, out object? days) && days is TreeSelection tree)
			selectedDays = Merlin.Classes.RollerSubstitution.SelectDays(tree.Table, tree.AddedIDs.ToList());
		if (selectedDays == null || selectedDays.Rows.Count == 0)
			return Tr.T(Merlin.Properties.Resources.NoIssueSelected);

		if (values.TryGetValue(SubstituteParams.SubstituteMute, out object? mute) && mute is true)
		{
			int duration = values.TryGetValue(SubstituteParams.MuteDuration, out object? d) && d != null && d != DBNull.Value
				? Convert.ToInt32(d) : 0;
			int? advertTypeId = values.TryGetValue(SubstituteParams.AdvertTypeId, out object? a) && a != null && a != DBNull.Value
				? Convert.ToInt32(a) : null;

			string? message = substitution.ValidateMuteRoller(advertTypeId, duration);
			if (message != null)
				return message;

			newRoller = substitution.CreateMuteRoller(duration, advertTypeId);
		}
		else
		{
			// Недостижимо: список роликов обязателен и проверен паспортом, а без
			// роликов «молчание» включено принудительно. Десктоп в этом случае
			// молча оставляет форму открытой.
			if (!values.TryGetValue(SubstituteParams.RollerId, out object? id) || id == null || id == DBNull.Value)
				return Tr.T("Не выбран ролик для замены.");

			newRoller = new Merlin.Classes.Roller(Convert.ToInt32(id));
		}

		return substitution.ValidateNewRoller(newRoller);
	}

	/// <summary>Простое информационное сообщение — тот же диалог, что и у остальных
	/// действий (TableDialog, подтверждение удаления), только с текстом вместо списка.</summary>
	// ---------- Рекламные окна прайс-листа ----------

	/// <summary>
	/// «Сгенерировать рекламные окна» — MassmediaPricelist.WinForms.GenerateTariffWindows:
	/// интервал (по умолчанию — срок прайс-листа), генерация по неделям с прогрессом, затем
	/// проверка склеенных окон и перечитывание прайс-листа — и после остановки тоже, для
	/// уже сгенерированной части.
	/// </summary>
	private async Task<ActionEffect> GenerateWindows(object pricelist)
	{
		var period = await _periods.ShowAsync(Tr.T("Интервал генерации окон"),
			Merlin.Classes.PricelistWindows.StartDate(pricelist), Merlin.Classes.PricelistWindows.FinishDate(pricelist),
			Tr.T("Сгенерировать"), (a, b) => Merlin.Classes.PricelistWindows.ValidatePeriod(pricelist, a, b),
			Tr.T("Окна строятся по тарифам прайс-листа. Уже сгенерированные окна не меняются."));
		if (period is not { } p)
			return ActionEffect.None;

		var weeks = Merlin.Classes.PricelistWindows.Weeks(p.Start, p.Finish);
		ProgressOutcome outcome = await _progress.RunAsync(Tr.T("Генерация рекламных окон"), weeks, DescribeWeek,
			w => Merlin.Classes.PricelistWindows.Generate(pricelist, w.Item1, w.Item2));

		if (outcome.Done > 0)
			await _busy.RunAsync(() => Merlin.Classes.PricelistWindows.AfterGenerate(
				pricelist, p.Start, weeks[outcome.Done - 1].Item2));
		return outcome.Done > 0 ? ActionEffect.Changed : ActionEffect.None;
	}

	/// <summary>
	/// «Удалить сгенерированные рекламные окна» у прайс-листа (<paramref name="time"/> = null)
	/// и у строки времени сетки (только это время) — MassmediaPricelist.WinForms.
	/// DeleteGeneratedTariffWindows и TariffWindowGrid.DeleteGeneratedTariffWindows.
	/// Окна с выпусками процедура не трогает.
	/// </summary>
	public async Task<ActionEffect> DeleteGeneratedWindows(object pricelist, TimeSpan? time)
	{
		DateTime start = Merlin.Classes.PricelistWindows.StartDate(pricelist);
		// Для одного времени — с сегодняшнего дня: прошлые окна удалять незачем (десктоп
		// предлагает весь срок прайс-листа).
		if (time.HasValue && DateTime.Today > start)
			start = DateTime.Today;
		DateTime finish = Merlin.Classes.PricelistWindows.FinishDate(pricelist);
		if (start > finish)
			start = finish;

		string? timeText = time?.ToString(@"hh\:mm");
		var period = await _periods.ShowAsync(timeText != null
				? Tr.Format("Интервал удаления сгенерированных окон {0}", timeText)
				: Tr.T("Интервал удаления сгенерированных окон"), start, finish,
			Tr.T("Удалить"), (a, b) => Merlin.Classes.PricelistWindows.ValidatePeriod(pricelist, a, b),
			timeText != null
				? Tr.Format("Удаляются окна времени {0} в выбранном интервале. Окна, в которых уже есть выпуски, остаются.", timeText)
				: Tr.T("Удаляются все окна прайс-листа в выбранном интервале. Окна, в которых уже есть выпуски, остаются."));
		if (period is not { } p)
			return ActionEffect.None;

		var weeks = Merlin.Classes.PricelistWindows.Weeks(p.Start, p.Finish);
		ProgressOutcome outcome = await _progress.RunAsync(Tr.T("Удаление сгенерированных окон"), weeks, DescribeWeek,
			w => Merlin.Classes.PricelistWindows.DeleteGenerated(pricelist, w.Item1, w.Item2, time));

		if (outcome.Done > 0)
			await _busy.RunAsync(() => Merlin.Classes.PricelistWindows.Refresh(pricelist));
		return outcome.Done > 0 ? ActionEffect.Changed : ActionEffect.None;
	}

	private static string DescribeWeek(Tuple<DateTime, DateTime> week) =>
		$"{DisplayFormat.Date(week.Item1)} – {DisplayFormat.Date(week.Item2)}";

	/// <summary>
	/// Запретить/разрешить внесение, пометить/снять пометку — TariffWindowsDisabledStatusForm:
	/// именованный паспорт TariffWindowsStatusChange (время, интервал, дни недели), один
	/// вызов процедуры.
	/// </summary>
	private async Task<ActionEffect> ChangeWindowsStatus(object pricelist, string actionName, string caption)
	{
		PresentationObject draft = Merlin.Classes.PricelistWindows.CreateStatusChangeDraft(pricelist);
		bool ok = await _namedPassports.ShowAsync(draft, Merlin.Classes.PricelistWindows.StatusChangePassport, caption,
			isNew: false,
			values => Merlin.Classes.PricelistWindows.ValidateStatusChange(pricelist, values),
			values => Merlin.Classes.PricelistWindows.ChangeStatus(pricelist, actionName, values),
			data: new DataSet());
		return ok ? ActionEffect.Changed : ActionEffect.None;
	}

	/// <summary>«Показать заблокированные окна» — MassmediaPricelist.WinForms.ShowDisabledWindows.</summary>
	private async Task<ActionEffect> ShowDisabledWindows(object pricelist)
	{
		var period = await _periods.ShowAsync(Tr.T("Выбрать период отчёта"),
			Merlin.Classes.PricelistWindows.StartDate(pricelist), Merlin.Classes.PricelistWindows.FinishDate(pricelist),
			Tr.T("Показать"), (a, b) => a > b ? MessageAccessor.GetMessage("StartFinishWindowTimeError") : null);
		if (period is not { } p)
			return ActionEffect.None;

		DataTable table = await _busy.RunAsync(() => Merlin.Classes.PricelistWindows.DisabledWindows(pricelist, p.Start, p.Finish));
		if (table.Rows.Count == 0)
		{
			await ShowInfo(Tr.T("Заблокированные окна"), Tr.T("Недоступных для внесения окон за этот период нет."));
			return ActionEffect.None;
		}

		await _tables.ShowAsync(Tr.Format("Заблокированные окна: {0}", table.Rows.Count), table,
			new Entity.Attribute(Merlin.Classes.TariffWindow.ParamNames.WindowDateOriginal, "Время выхода", "datetime"), // i18n-ok: Alias переводится при показе (ObjectList)
			new Entity.Attribute(Merlin.Classes.TariffWindow.ParamNames.WindowDateActual, "Время выхода реальное", "datetime"), // i18n-ok: Alias переводится при показе (ObjectList)
			new Entity.Attribute("durationString", "Продолжительность", "nvarchar"), // i18n-ok: Alias переводится при показе (ObjectList)
			new Entity.Attribute(Merlin.Classes.TariffWindow.ParamNames.Price, "Цена", "money")); // i18n-ok: Alias переводится при показе (ObjectList)
		return ActionEffect.None;
	}

	/// <summary>
	/// ActionRollerInStatJournal.SetAdvertType: ролику «для всех фирм» и копии — отказ с
	/// причиной, иначе выбор из предметов рекламы 2-го уровня (десктоп — SelectionForm по
	/// сущности AdvertTypeChild) и ActionRollerSetAdvertType.
	/// </summary>
	private async Task<ActionEffect> SetRollerAdvertType(PresentationObject roller)
	{
		string? reason = Merlin.Classes.RollerStatisticQuery.CannotSetAdvertType(roller);
		if (reason != null)
		{
			await ShowInfo(Tr.T("Назначить предмет рекламы"), reason);
			return ActionEffect.None;
		}

		Entity entity = EntityManager.GetEntity((int)Merlin.Entities.AdvertTypeChild);
		var picker = new PassportPicker(entity.CodeName, null, false, Array.Empty<PassportFilterValue>());
		ObjectSelector? selector = null;
		RenderFragment body = builder =>
		{
			builder.OpenComponent<ObjectSelector>(0);
			builder.AddComponentParameter(1, nameof(ObjectSelector.Picker), picker);
			builder.AddComponentReferenceCapture(2, c => selector = (ObjectSelector)c);
			builder.CloseComponent();
		};

		if (await _dialogs.ShowAsync(Tr.T("Выбор предмета рекламы"), body, okText: Tr.T("Назначить")) != DialogOutcome.Ok
			|| selector?.SelectedRow == null)
			return ActionEffect.None;

		PresentationObject advertType = entity.CreateObject(selector.SelectedRow);
		await _busy.RunAsync(() => Merlin.Classes.RollerStatisticQuery.SetAdvertType(roller, advertType));
		return ActionEffect.Changed;
	}

	/// <summary>
	/// PaymentCommon.SelectActions: кандидаты, распределение галочками, запись по «ОК».
	/// Десктоп после этого делает Refresh + FireContainerRefreshed — здесь экран
	/// перечитывает платежи и оплаты по <see cref="ActionEffect.Changed"/>.
	/// </summary>
	private async Task<ActionEffect> SelectActionsToPay(Merlin.Classes.PaymentCommon payment)
	{
		DataTable candidates = await _busy.RunAsync(payment.GetPaymentCandidates);
		var model = new PaymentCandidatesForm.Model(payment.Summa, payment.Consumed, candidates);

		if (await _dialogs.ShowAsync(Tr.T("Акции на оплату"), builder =>
			{
				builder.OpenComponent<PaymentCandidatesForm>(0);
				builder.AddComponentParameter(1, nameof(PaymentCandidatesForm.Value), model);
				builder.CloseComponent();
			}, okText: Tr.T("Оплатить")) != DialogOutcome.Ok || model.Allocated.Count == 0)
			return ActionEffect.None;

		await _busy.RunAsync(() => payment.PayActions(model.Allocated));
		return ActionEffect.Changed;
	}

	// ---------- Рекламная акция и кампания: операции журнала ----------
	//
	// Веб-аналоги веток DoAction из Action.WinForms.cs, ActionOnMassmedia.WinForms.cs и
	// Campaign.WinForms.cs. Проверки и запись — методы ядра, здесь только диалоги. Сообщений
	// «успешно» нет (решение 2026-09-22: результат виден в списке), кроме «Восстановить» —
	// акция уходит в другой журнал, и сообщение говорит, в какой.
	//
	// Эффект SiblingAdded там, где объект уходит из-под своего родителя (другая фирма,
	// объединение, новая акция при делении): перечитывается родитель, как и в десктопе
	// (OnParentChanged / FireContainerRefreshed по родителю).

	/// <summary>Action.ChangeFirm: правило «можно ли», выбор фирмы, запись.</summary>
	private async Task<ActionEffect> ChangeFirm(Merlin.Classes.Action action)
	{
		if (!action.IsChangeFirmPossible)
		{
			await ShowInfo(Tr.T("Сменить фирму-заказчика"), MessageAccessor.GetMessage("ChangeFirmIsForbidden"));
			return ActionEffect.None;
		}

		Entity firms = EntityManager.GetEntity((int)Merlin.Entities.Firm);
		DataTable candidates = await _busy.RunAsync(Merlin.Classes.Firm.GetFirmCandidates);
		DataRow? firm = (await PickAsync(Tr.T("Фирма-заказчик"), firms, candidates, Tr.T("Сменить")))?[0];
		if (firm == null)
			return ActionEffect.None;

		await _busy.RunAsync(() => action.ApplyFirmChange(Convert.ToInt32(PickedId(firms, firm))));
		return ActionEffect.SiblingAdded;
	}

	/// <summary>Action.ChangeCreator: выбор менеджера (Utils.SelectManager), запись.</summary>
	private async Task<ActionEffect> ChangeCreator(Merlin.Classes.Action action)
	{
		Entity users = EntityManager.GetEntity((int)Merlin.Entities.User);
		DataRow? manager = (await PickAsync(Tr.T("Менеджер"), users, null, Tr.T("Сменить")))?[0];
		if (manager == null)
			return ActionEffect.None;

		await _busy.RunAsync(() => action.ApplyCreatorChange(PickedId(users, manager)));
		return ActionEffect.Changed;
	}

	private async Task<ActionEffect> RecalculateAction(Merlin.Classes.ActionOnMassmedia action)
	{
		await _busy.RunAsync(() => action.Recalculate(true));
		return ActionEffect.Changed;
	}

	/// <summary>
	/// ActionOnMassmedia.DeactivateAction: запрет по дате начала, вопрос, деактивация.
	/// Акция уходит в журнал макетов — для этого экрана это удаление.
	/// </summary>
	private async Task<ActionEffect> DeactivateAction(Merlin.Classes.ActionOnMassmedia action)
	{
		string caption = Tr.T("Деактивировать");
		if (!action.CanDeactivate(out string error))
		{
			await ShowInfo(caption, error);
			return ActionEffect.None;
		}

		if (await _dialogs.ShowAsync(caption, builder => builder.AddContent(0, MessageAccessor.GetMessage("ConfirmActionDeactivate")),
				okText: caption) != DialogOutcome.Ok)
			return ActionEffect.None;

		await _busy.RunAsync(action.ApplyDeactivate);
		return ActionEffect.Deleted;
	}

	/// <summary>
	/// ActionOnMassmedia.Merge: запрет для начавшейся подтверждённой, выбор второй акции той же
	/// фирмы (кандидатов даёт ядро), тот же запрет для неё, объединение.
	/// </summary>
	private async Task<ActionEffect> MergeActions(Merlin.Classes.ActionOnMassmedia action)
	{
		string caption = Tr.T("Объединить с ...");
		if (!action.CanSplitOrMerge(action.StartDate.Date, out string messageKey))
		{
			await ShowInfo(caption, MessageAccessor.GetMessage(messageKey));
			return ActionEffect.None;
		}

		DataTable? candidates = await _busy.RunAsync(action.GetActionsForMerge);
		if (candidates == null)
			return ActionEffect.None;

		Entity actions = EntityManager.GetEntity((int)Merlin.Entities.Action);
		DataRow? picked = (await PickAsync(caption, actions, candidates, Tr.T("Объединить")))?[0];
		if (picked == null || actions.CreateObject(picked) is not Merlin.Classes.ActionOnMassmedia second)
			return ActionEffect.None;

		if (!second.CanSplitOrMerge(second.StartDate.Date, out messageKey))
		{
			await ShowInfo(caption, MessageAccessor.GetMessage(messageKey));
			return ActionEffect.None;
		}

		await _busy.RunAsync(() => action.ApplyMerge(second));
		return ActionEffect.SiblingAdded;
	}

	/// <summary>
	/// ActionOnMassmedia.SplitAction: отмеченные кампании уходят в новую акцию. Нельзя
	/// ни ни одной, ни все сразу — проверка ядра, окно выбора остаётся открытым.
	/// </summary>
	private async Task<ActionEffect> SplitAction(Merlin.Classes.ActionOnMassmedia action)
	{
		string caption = Tr.T("Разделить рекламную акцию");
		if (!action.CanSplitOrMerge(action.StartDate.Date, out string messageKey))
		{
			await ShowInfo(caption, MessageAccessor.GetMessage(messageKey));
			return ActionEffect.None;
		}

		DataTable? campaigns = null;
		string? reasonKey = null;
		await _busy.RunAsync(() => campaigns = action.GetCampaignsForSplit(out reasonKey));
		if (campaigns == null)
		{
			await ShowInfo(caption, MessageAccessor.GetMessage(reasonKey!));
			return ActionEffect.None;
		}

		Entity entity = EntityManager.GetEntity((int)Merlin.Entities.CampaignOnMassmedia);
		IReadOnlyList<DataRow>? picked = await PickAsync(
			Tr.T("Выберите рекламные кампании, которые хотите перенести в новую акцию"), entity, campaigns,
			Tr.T("Разделить"), multiselect: true,
			validate: rows => action.IsSplitSelectionValid(rows.Count, out string key) ? null : MessageAccessor.GetMessage(key));
		if (picked == null)
			return ActionEffect.None;

		List<PresentationObject> toMove = picked.Select(entity.CreateObject).ToList();
		await _busy.RunAsync(() => action.ApplySplitAction(toMove));
		return ActionEffect.SiblingAdded;
	}

	/// <summary>ActionOnMassmedia.ShowRollers: журнал «Статистика по роликам» под одну акцию.</summary>
	private async Task<ActionEffect> ShowActionRollers(Merlin.Classes.ActionOnMassmedia action)
	{
		Entity entity = EntityManager.GetEntity((int)Merlin.Entities.ActionRollersStat);
		var filter = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
		{
			[Merlin.Classes.Action.ParamNames.ActionId] = action.ActionId,
		};
		DataTable rows = await _busy.RunAsync(() => entity.GetContent(filter));
		await _tables.ShowAsync(Tr.Format("Статистика по роликам для акции №{0}", action.ActionId), entity, rows);
		return ActionEffect.None;
	}

	/// <summary>
	/// Campaign.PrintTransfers: перенесённые трафиком выпуски кампании (журнал 206); нет —
	/// сообщение CampaignHaveNotTransfers, как в десктопе.
	/// </summary>
	private async Task<ActionEffect> ShowCampaignTransfers(PresentationObject campaign)
	{
		DataTable rows = await _busy.RunAsync(() => Merlin.Classes.CampaignChange.Transfers(campaign));
		string caption = Tr.T(Merlin.Properties.Resources.CampaignIssuesTransfersTitle);
		if (rows.Rows.Count == 0)
			await ShowInfo(caption, MessageAccessor.GetMessage("CampaignHaveNotTransfers"));
		else
			await _tables.ShowAsync(caption, EntityManager.GetEntity((int)Merlin.Entities.CampaignIssuesTransfers), rows, wide: true);
		return ActionEffect.None;
	}

	/// <summary>
	/// BalanceStatRow «Открыть журнал акций»: переход на журнал подтверждённых акций с отбором
	/// из строки (фирма или группа компаний, группа станций, менеджер, период). Пунктов журнала
	/// три (Рекламный отдел, Бухгалтерия, Трафик) и права на них отдельные — берём первый
	/// доступный пользователю.
	/// </summary>
	private async Task<ActionEffect> OpenActionJournal(PresentationObject row)
	{
		string? code = new[] { "miActionJournal", "miActionJournalBuh", "miActionJournalTraffic" }
			.FirstOrDefault(c => _menuAccess.CheckBrowser(c) == JournalAccess.Allowed);
		if (code == null)
		{
			await ShowInfo(Tr.T("Открыть журнал акций"), Tr.T("Нет доступа к журналу подтверждённых рекламных акций."));
			return ActionEffect.None;
		}

		_filters.Preset("browser/" + code, Merlin.Classes.BonusStatRow.ActionJournalFilter(row));
		_navigation.NavigateTo("/browser/" + code);
		return ActionEffect.None;
	}

	/// <summary>
	/// HeadCompany.EditFirms: фирмы галочками из всех (SelectionForm с чекбоксами), отмеченные
	/// переносятся в эту группу. Перечитывается сама группа — видно добавленные фирмы; если
	/// какая-то прежняя группа опустела и исчезла — дерево от родителя.
	/// </summary>
	private async Task<ActionEffect> EditHeadCompanyFirms(Merlin.Classes.HeadCompany headCompany)
	{
		Entity firms = EntityManager.GetEntity((int)Merlin.Entities.Firm);
		DataTable candidates = await _busy.RunAsync(headCompany.GetFirmsForReassign);
		IReadOnlyList<DataRow>? picked = await PickAsync(Tr.T("Фирмы-заказчики"), firms, candidates, Tr.T("Добавить в группу"),
			multiselect: true);
		if (picked == null || picked.Count == 0)
			return ActionEffect.None;

		List<PresentationObject> items = picked.Select(firms.CreateObject).ToList();
		bool groupRemoved = await _busy.RunAsync(() => headCompany.ApplyFirmsReassign(items));
		return groupRemoved ? ActionEffect.SiblingAdded : ActionEffect.Changed;
	}

	/// <summary>
	/// PackModulePricelist.AssignExisting: новая строка содержимого пакета с прайс-листом
	/// пакета — карточка (станция → модуль → прайс-лист модуля, зависимые списки); сохраняет
	/// карточка, как в десктопе (ShowPassport).
	/// </summary>
	private async Task<ActionEffect> AddModuleToPack(Merlin.Classes.PackModulePricelist pricelist)
	{
		PresentationObject content = EntityManager.GetEntity((int)Merlin.Entities.PackModuleContent).NewObject;
		content[Merlin.Classes.Pricelist.ParamNames.PricelistId] = pricelist.PricelistId;
		return await _passports.ShowAsync(content, isNew: true) ? ActionEffect.ChildAdded : ActionEffect.None;
	}

	/// <summary>ActionOnMassmedia.Restore: акция возвращается в журнал макетов.</summary>
	private async Task<ActionEffect> RestoreAction(Merlin.Classes.ActionOnMassmedia action)
	{
		await _busy.RunAsync(action.ApplyRestore);
		await ShowInfo(Tr.T("Восстановить рекламную акцию"), MessageAccessor.GetMessage("ActionRestored"));
		return ActionEffect.Deleted;
	}

	// ---------- Партия 2: небольшие окна ----------

	/// <summary>
	/// ActionOnMassmedia.ChangePaymentTypeMass: тип оплаты из действующих и кампании
	/// галочками (ChangePaymentTypeMassForm), затем по одной кампании best-effort; отказы —
	/// таблицей. «ОК» без типа или без кампаний — сообщение, окно остаётся (в десктопе
	/// кнопка просто погашена).
	/// </summary>
	private async Task<ActionEffect> ChangePaymentTypeMass(Merlin.Classes.ActionOnMassmedia action)
	{
		var model = await _busy.RunAsync(() => new PaymentTypeMassForm.Model
		{
			PaymentTypes = Merlin.Classes.ActionOnMassmedia.LoadActivePaymentTypes(),
			CampaignEntity = Merlin.Classes.ActionOnMassmedia.CampaignListEntity(),
			Campaigns = action.Campaigns(),
		});

		while (true)
		{
			if (await _dialogs.ShowAsync(Tr.T("Сменить тип оплаты"), builder =>
				{
					builder.OpenComponent<PaymentTypeMassForm>(0);
					builder.AddComponentParameter(1, nameof(PaymentTypeMassForm.Value), model);
					builder.CloseComponent();
				}, okText: Tr.T("Сменить"), wide: true) != DialogOutcome.Ok)
				return ActionEffect.None;

			model.Message = model.PaymentTypeId == null ? Tr.T("Выберите тип оплаты.")
				: model.Selected.Count == 0 ? MessageAccessor.GetMessage("NoCampaignSelected")
				: null;
			if (model.Message == null)
				break;
		}

		DataTable? errors = null;
		await _busy.RunAsync(() => action.ApplyPaymentTypeChangeMass(model.PaymentTypeId!.Value, model.Selected, out errors));
		if (errors is { Rows.Count: > 0 })
			await _tables.ShowAsync(Tr.T("Ошибки смены типа оплаты"), errors,
				new Entity.Attribute("description", "Ошибка", "nvarchar")); // i18n-ok: Alias переводится при показе (ObjectList)
		return ActionEffect.Changed;
	}

	/// <summary>
	/// CampaignDay.TransferDay: исходный день, срок прайс-листа, новая дата; после
	/// переноса — сообщение о цене акции, как RecalculateAndShowPriceChange десктопа.
	/// Отказ процедуры (занятое окно и т.п.) — обычная ошибка действия.
	/// </summary>
	private async Task<ActionEffect> TransferDay(PresentationObject day)
	{
		var model = await _busy.RunAsync(() =>
		{
			DateTime source = Merlin.Classes.CampaignDayTransfer.Day(day);
			return new DayTransferForm.Model
			{
				Source = source,
				Target = source,
				Pricelist = Merlin.Classes.CampaignDayTransfer.PricelistPeriod(day),
			};
		});

		while (true)
		{
			if (await _dialogs.ShowAsync(Tr.T("Перенос дня"), builder =>
				{
					builder.OpenComponent<DayTransferForm>(0);
					builder.AddComponentParameter(1, nameof(DayTransferForm.Value), model);
					builder.CloseComponent();
				}, okText: Tr.T("Перенести")) != DialogOutcome.Ok)
				return ActionEffect.None;

			// В десктопе «ОК» с той же датой уходит в процедуру; здесь — просто нечего делать.
			model.Message = model.Target.Date == model.Source.Date ? Tr.T("Выберите другую дату.") : null;
			if (model.Message == null)
				break;
		}

		string priceMessage = await _busy.RunAsync(() => Merlin.Classes.CampaignDayTransfer.Apply(day, model.Target.Date));
		await ShowInfo(Tr.T("Перенос дня"), priceMessage);
		return ActionEffect.SiblingAdded;
	}

	/// <summary>
	/// Action.SetAdvertTypeOrSubstituteRoller: окно со списком роликов акции (журнал 1244),
	/// у строк — «Назначить предмет рекламы» и «Заменить рекламный ролик». Список
	/// перечитывается после каждого действия; по закрытии перечитывается и сама акция.
	/// </summary>
	/// <param name="title">Заголовок окна; null — «Ролики рекламной акции № N».</param>
	private async Task<ActionEffect> EditActionRollers(Merlin.Classes.Action action, string? title = null)
	{
		Entity entity = EntityManager.GetEntity((int)Merlin.Entities.ActionRollers);
		int actionId = action.ActionId;
		string caption = title ?? Tr.Format("Ролики рекламной акции № {0}", actionId);
		await _dialogs.ShowAsync(caption, builder =>
		{
			builder.OpenComponent<LiveObjectList>(0);
			builder.AddComponentParameter(1, nameof(LiveObjectList.Entity), entity);
			builder.AddComponentParameter(2, nameof(LiveObjectList.Load), (Func<DataTable>)(() => Merlin.Classes.ActionRollerChange.Load(actionId)));
			builder.AddComponentParameter(3, nameof(LiveObjectList.Title), caption);
			builder.CloseComponent();
		}, okText: Tr.T("Закрыть"), wide: true, showCancel: false);
		return ActionEffect.Changed;
	}

	// ---------- Партия 3: активация ----------

	/// <summary>
	/// ActionOnMassmedia.ActivateAction. Предпросмотр — сразу прогон без переноса и показ
	/// результата. Активация: ролики без предмета рекламы — окно роликов акции (назначить),
	/// повторная проверка, если остались — отказ; выпуски программ без предмета — только
	/// предупреждение; затем «Параметры активации», прогон и результат одним окном
	/// (ActivationResultView) вместо трёх журналов десктопа. Акция без фатальной ошибки
	/// уходит из журнала макетов — для этого экрана это удаление.
	/// </summary>
	private async Task<ActionEffect> ActivateAction(Merlin.Classes.ActionOnMassmedia action, bool isTest)
	{
		string activate = Tr.T("Активировать");
		var settings = Merlin.Classes.ActionOnMassmedia.ActivationSettings.NoTransfer;

		if (!isTest)
		{
			bool rollersWithout = false, programIssuesWithout = false;
			await _busy.RunAsync(() => action.CheckAdvertTypes(out rollersWithout, out programIssuesWithout));
			if (rollersWithout)
			{
				await EditActionRollers(action, Tr.Format("Назначьте предмет рекламы роликам акции № {0}", action.ActionId));
				await _busy.RunAsync(() => action.CheckAdvertTypes(out rollersWithout, out _));
				if (rollersWithout)
				{
					await ShowInfo(activate, MessageAccessor.GetMessage("ActivationWithRollersWithoutAdvType"));
					return ActionEffect.Changed;
				}
			}
			if (programIssuesWithout)
				await ShowInfo(activate, Tr.T(Merlin.Properties.Resources.ActivationWithProgramIssuesWithoutAdvType));

			settings = new Merlin.Classes.ActionOnMassmedia.ActivationSettings { TransferAttemptCount = 1 };
			if (await _dialogs.ShowAsync(Tr.T("Параметры активации"), builder =>
				{
					builder.OpenComponent<ActivationSettingsForm>(0);
					builder.AddComponentParameter(1, nameof(ActivationSettingsForm.Value), settings);
					builder.CloseComponent();
				}, okText: activate) != DialogOutcome.Ok)
				return ActionEffect.None;
		}

		var result = await _busy.RunAsync(() => action.RunActivation(isTest, settings));
		await _dialogs.ShowAsync(
			isTest ? Tr.T("Предварительный просмотр результатов активации") : Tr.T("Результаты активации"),
			builder =>
			{
				builder.OpenComponent<ActivationResultView>(0);
				builder.AddComponentParameter(1, nameof(ActivationResultView.Value), result);
				builder.CloseComponent();
			}, okText: Tr.T("Закрыть"), wide: true, showCancel: false);

		return !isTest && result.FatalError == null ? ActionEffect.Deleted : ActionEffect.None;
	}

	/// <summary>ActionRoller.SetAdvertType: выбор предмета рекламы, запись.</summary>
	private async Task<ActionEffect> SetActionRollerAdvertType(PresentationObject roller)
	{
		Entity entity = EntityManager.GetEntity((int)Merlin.Entities.AdvertTypeChild);
		DataRow? advertType = (await PickAsync(Tr.T("Выбор предмета рекламы"), entity, null, Tr.T("Назначить")))?[0];
		if (advertType == null)
			return ActionEffect.None;

		await _busy.RunAsync(() => Merlin.Classes.ActionRollerChange.SetAdvertType(roller, PickedId(entity, advertType)));
		return ActionEffect.Changed;
	}

	/// <summary>
	/// ActionRoller.SubstituteRoller: новый ролик из роликов фирмы — во всех кампаниях
	/// акции сразу. Незаменённые — одной таблицей (десктоп показывает журнал на каждую
	/// кампанию), затем сообщение о цене акции.
	/// </summary>
	private async Task<ActionEffect> SubstituteActionRoller(PresentationObject roller)
	{
		Entity rollers = EntityManager.GetEntity((int)Merlin.Entities.Roller);
		DataTable candidates = await _busy.RunAsync(() => Merlin.Classes.ActionRollerChange.SubstituteCandidates(roller));
		DataRow? picked = (await PickAsync(Tr.T("Замена ролика"), rollers, candidates, Tr.T("Заменить")))?[0];
		if (picked == null)
			return ActionEffect.None;

		var (unsubstituted, priceMessage) = await _busy.RunAsync(() =>
			Merlin.Classes.ActionRollerChange.Substitute(roller, Convert.ToInt32(PickedId(rollers, picked))));

		if (unsubstituted != null)
			await _tables.ShowAsync(Tr.T("Незамененные ролики"), unsubstituted,
				new Entity.Attribute("windowDateOriginal", "Дата выпуска", "datetime"), // i18n-ok: Alias переводится при показе (ObjectList)
				new Entity.Attribute("message", "Ошибка", "nvarchar")); // i18n-ok: Alias переводится при показе (ObjectList)
		await ShowInfo(Tr.T("Замена ролика"), priceMessage);
		return ActionEffect.Changed;
	}

	/// <summary>Campaign.ChangeAgency: правило «можно ли», агентства по правам, запись.</summary>
	private async Task<ActionEffect> ChangeCampaignAgency(PresentationObject campaign)
	{
		if (!Merlin.Classes.CampaignChange.IsPossible(campaign))
		{
			await ShowInfo(Tr.T("Сменить рекламное агентство"), Tr.T(Merlin.Properties.Resources.ChangeAgencyIsForbidden));
			return ActionEffect.None;
		}

		Entity agencies = EntityManager.GetEntity((int)Merlin.Entities.Agency);
		DataTable? candidates = Merlin.Classes.CampaignChange.AgencyCandidates(campaign)?.ToTable();
		DataRow? agency = (await PickAsync(Tr.T("Рекламное агентство"), agencies, candidates, Tr.T("Сменить")))?[0];
		if (agency == null)
			return ActionEffect.None;

		await _busy.RunAsync(() => Merlin.Classes.CampaignChange.ApplyAgency(campaign, Convert.ToInt32(PickedId(agencies, agency))));
		return ActionEffect.Changed;
	}

	/// <summary>Campaign.ChangePaymentType: правило «можно ли», выбор типа оплаты, запись.</summary>
	private async Task<ActionEffect> ChangeCampaignPaymentType(PresentationObject campaign)
	{
		if (!Merlin.Classes.CampaignChange.IsPossible(campaign))
		{
			await ShowInfo(Tr.T("Сменить тип оплаты"), Tr.T(Merlin.Properties.Resources.ChangePaymentTypeIsForbidden));
			return ActionEffect.None;
		}

		Entity types = EntityManager.GetEntity((int)Merlin.Entities.PaymentType);
		DataRow? type = (await PickAsync(Tr.T("Типы оплаты"), types, null, Tr.T("Сменить")))?[0];
		if (type == null)
			return ActionEffect.None;

		await _busy.RunAsync(() => Merlin.Classes.CampaignChange.ApplyPaymentType(campaign, Convert.ToInt32(PickedId(types, type))));
		return ActionEffect.Changed;
	}

	/// <summary>
	/// Выбор из списка — веб-аналог SelectionForm(entity, dataView, caption[, showCheckboxes,
	/// проверка]). <paramref name="rows"/> null — все строки сущности. Проверка выбора, как
	/// делегат SelectionForm: текст ошибки над списком, окно открыто снова с теми же отметками.
	/// </summary>
	/// <returns>Выбранные строки (одна без <paramref name="multiselect"/>); null — отказ.</returns>
	private async Task<IReadOnlyList<DataRow>?> PickAsync(string caption, Entity entity, DataTable? rows, string okText,
		bool multiselect = false, Func<IReadOnlyList<DataRow>, string?>? validate = null)
	{
		ObjectSelector? selector = null;
		string? message = null;
		IReadOnlyList<DataRow>? previouslySelected = null;

		while (true)
		{
			RenderFragment body = builder =>
			{
				if (message != null)
				{
					builder.OpenElement(0, "div");
					builder.AddAttribute(1, "class", "alert alert-danger");
					builder.AddContent(2, message);
					builder.CloseElement();
				}

				builder.OpenComponent<ObjectSelector>(3);
				builder.AddComponentParameter(4, nameof(ObjectSelector.SourceEntity), entity);
				builder.AddComponentParameter(5, nameof(ObjectSelector.SourceRows), rows);
				builder.AddComponentParameter(6, nameof(ObjectSelector.Multiselect), multiselect);
				builder.AddComponentParameter(7, nameof(ObjectSelector.InitialSelectedRows), previouslySelected);
				builder.AddComponentReferenceCapture(8, c => selector = (ObjectSelector)c);
				builder.CloseComponent();
			};

			if (await _dialogs.ShowAsync(caption, body, okText: okText) != DialogOutcome.Ok || selector == null)
				return null;

			IReadOnlyList<DataRow> selected = multiselect
				? selector.SelectedRows
				: selector.SelectedRow is { } row ? new[] { row } : Array.Empty<DataRow>();
			if (!multiselect && selected.Count == 0)
				return null;

			message = validate?.Invoke(selected);
			if (message == null)
				return selected;

			previouslySelected = selected;
		}
	}

	/// <summary>Ключ выбранной строки — то же, что SelectedObject.IDs[0] у SelectionForm.</summary>
	private static object PickedId(Entity entity, DataRow row) => row[entity.PKColumns[0]];

	private Task ShowInfo(string caption, string text) =>
		_dialogs.ShowAsync(caption, builder => builder.AddContent(0, text), okText: Tr.T("Ок"), showCancel: false);

	/// <summary>FakeContainer, ветка AddNew — то же для корня древовидного экрана.</summary>
	private async Task<ActionEffect> AddNew(object target)
	{
		var container = (FakeContainer)target;
		PresentationObject newObject = await _busy.RunAsync(container.CreateNewObject);
		if (!await _passports.ShowAsync(newObject, isNew: true))
			return ActionEffect.None;

		await _busy.RunAsync(() => container.CompleteNewObject(newObject));
		return ActionEffect.ChildAdded;
	}

	// ---------- Объект как обработчик действий ----------

	// IActionHandler в ядре нет — он объявлен в UI-половине, потому что его
	// DoAction принимает IWin32Window. Нужные три члена есть у обоих классов,
	// которые бывают целью меню, но общего интерфейса у них в вебе нет.

	private static Entity.Action[]? ActionList(object target) => target switch
	{
		PresentationObject po => po.ActionList,
		FakeContainer fc => fc.ActionList,
		_ => null
	};

	private static bool IsEnabled(object target, string actionName, ViewType view) => target switch
	{
		PresentationObject po => po.IsActionEnabled(actionName, view),
		FakeContainer fc => fc.IsActionEnabled(actionName, view),
		_ => false
	};

	/// <summary>
	/// Почему погашенное действие недоступно — там, где доменный класс умеет это
	/// объяснить. Десктоп причин не показывает; добавляются по мере вопросов.
	/// </summary>
	private static string? DisabledReason(object target, string actionName) => target switch
	{
		Merlin.Classes.PaymentCommon payment when actionName == Merlin.Classes.PaymentCommon.ActionNames.SelectActionsToPay
			=> payment.SelectActionsToPayUnavailableReason(),
		_ => null
	};

	/// <summary>
	/// Действия, которым в вебе отвечает не пункт меню, а элемент экрана. Они
	/// не «не перенесены» — их серый пункт врал бы, — поэтому прячутся совсем.
	///
	/// <c>ShowFilters</c>: в десктопе модальный диалог отбора, в вебе —
	/// постоянная панель на том же экране (FilterPanel). Объявлен он у корня
	/// ActionContainer (журналы рекламных акций) и у сущности 9 в метаданных;
	/// панель есть и там, и там.
	/// </summary>
	private static readonly string[] ReplacedByScreen = { "ShowFilters" };

	private static bool IsHidden(object target, string actionName, ViewType view) =>
		ReplacedByScreen.Contains(actionName) || target switch
		{
			PresentationObject po => po.IsActionHidden(actionName, view),
			FakeContainer fc => fc.IsActionHidden(actionName, view),
			_ => true
		};
}
