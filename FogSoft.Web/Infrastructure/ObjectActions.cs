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
/// Scoped — пользуется диалогами circuit.
/// </summary>
public sealed class ObjectActions
{
	private readonly PassportDialog _passports;
	private readonly DialogService _dialogs;
	private readonly TableDialog _tables;

	public ObjectActions(PassportDialog passports, DialogService dialogs, TableDialog tables)
	{
		_passports = passports;
		_dialogs = dialogs;
		_tables = tables;
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
			[AdvertTypeContainer.ActionNames.ShowTree] = (_, t) => Changed(((AdvertTypeContainer)t).ShowTree),
			[AdvertTypeContainer.ActionNames.ShowFlat] = (_, t) => Changed(((AdvertTypeContainer)t).ShowFlat),
		},
		// ActionContainer.DoAction: подмена ChildEntity корня — разбивка акций по
		// группам компаний, по фирмам или без разбивки. Какой пункт сейчас
		// недоступен, решает сам контейнер (IsActionEnabled): серым гасится
		// текущий вид, как в десктопе.
		["ActionContainer"] = new()
		{
			[ActionContainer.ActionNames.ShowHeadCompanies] = (_, t) => Changed(((ActionContainer)t).ShowHeadCompanies),
			[ActionContainer.ActionNames.ShowFirms] = (_, t) => Changed(((ActionContainer)t).ShowFirms),
			[ActionContainer.ActionNames.ShowActions] = (_, t) => Changed(((ActionContainer)t).ShowActions),
		},
		// Announcement.DoAction: «Пометить как прочтенное». Доступность гасит
		// Announcement.IsActionEnabled (у прочитанного — серый).
		["Announcement"] = new()
		{
			[Merlin.Classes.Announcement.ActionNames.MarkAsRead] = (_, t) => Changed(((Merlin.Classes.Announcement)t).MarkAsRead),
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
	};

	private static Task<ActionEffect> Changed(Action apply)
	{
		apply();
		return Task.FromResult(ActionEffect.Changed);
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

		return new ActionMenuItem
		{
			Name = action.Name,
			Text = isProperties ? "Открыть карточку" : action.Alias,
			// В списке карточку открывает клик по строке, в дереве клик выбирает узел.
			Hint = isProperties && view == ViewType.Journal ? "клик" : null,
			Icon = ActionIcons.For(action.Name, action.ImgResourceName),
			Enabled = IsEnabled(target, action.Name, view),
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
				Text = action.Alias,
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
				"Удаление",
				builder => builder.AddContent(0, obj.DeleteConfirmationText),
				okText: "Удалить") != DialogOutcome.Ok)
			return ActionEffect.None;

		return obj.Delete(silenceFlag: true) ? ActionEffect.Deleted : ActionEffect.None;
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

		string question = string.Format(
			"Вы действительно хотите удалить выбранные объекты? ({0} шт.)", objects.Count);
		if (await _dialogs.ShowAsync("Удаление", builder => builder.AddContent(0, question), okText: "Удалить") != DialogOutcome.Ok)
			return null;

		DataTable errors = new();
		errors.Columns.Add("objectName", typeof(string));
		errors.Columns.Add("errorText", typeof(string));

		foreach (PresentationObject obj in objects)
		{
			string objectName = string.IsNullOrEmpty(obj.Name) ? "<без названия>" : obj.Name;

			try
			{
				if (!obj.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal))
				{
					AddDeleteError(errors, objectName, string.Format("Удаление недоступно для объекта '{0}'.", objectName));
					continue;
				}

				if (!obj.Delete(silenceFlag: true))
					AddDeleteError(errors, objectName, string.Format("Не удалось удалить объект '{0}'.", objectName));
			}
			catch (Exception ex)
			{
				AddDeleteError(errors, objectName, ErrorPresenter.Describe(ex));
			}
		}

		if (errors.Rows.Count > 0)
			await _tables.ShowAsync("Ошибки массового удаления", errors,
				new Entity.Attribute("objectName", "Объект", "nvarchar"),
				new Entity.Attribute("errorText", "Ошибка", "nvarchar"));

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
		PresentationObject? newObject = container.CreateNewChild();
		if (newObject == null || !await _passports.ShowAsync(newObject, isNew: true))
			return ActionEffect.None;

		container.CompleteNewChild(newObject);
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
		DateTime startDate = DateTime.Today;
		DateTime finishDate = DateTime.Today;
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

			if (await _dialogs.ShowAsync("Клонирование прайс-листа", body) != DialogOutcome.Ok)
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
				pricelist.ApplyClone(startDate, finishDate, appliedMode);
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

			if (await _dialogs.ShowAsync("Радиостанции", body, okText: "Клонировать") != DialogOutcome.Ok)
				return ActionEffect.None;

			IReadOnlyList<DataRow> selected = selector!.SelectedRows;
			previouslySelected = selected;
			if (!pricelist.IsMassCloneSelectionValid(selected.Count))
			{
				// Тот же текст, что десктопный Properties.Resources.NoRadiostationSelected
				// (CheckSelectionResult) — это не бизнес-ошибка процедуры, MessageAccessor
				// такого ключа не знает.
				message = "Необходимо выбрать хотя бы одну радиостанцию.";
				continue;
			}

			Entity massmedia = EntityManager.GetEntity((int)Merlin.Entities.MassMedia);
			List<PresentationObject> radioStations = selected.Select(massmedia.CreateObject).ToList();

			DataTable errors = pricelist.ApplyMassClone(startDate, finishDate, mode, radioStations);
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
		_tables.ShowAsync("Ошибки клонирования", errors,
			new Entity.Attribute("description", "Ошибка", "nvarchar"));

	/// <summary>FakeContainer, ветка AddNew — то же для корня древовидного экрана.</summary>
	private async Task<ActionEffect> AddNew(object target)
	{
		var container = (FakeContainer)target;
		PresentationObject newObject = container.CreateNewObject();
		if (!await _passports.ShowAsync(newObject, isNew: true))
			return ActionEffect.None;

		container.CompleteNewObject(newObject);
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
