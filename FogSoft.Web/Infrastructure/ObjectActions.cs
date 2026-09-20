using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
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

	public ObjectActions(PassportDialog passports, DialogService dialogs)
	{
		_passports = passports;
		_dialogs = dialogs;
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
	/// Механизм общий, не под один класс: следующим клиентом будет журнал
	/// рекламных акций — ActionContainer (ShowFirms / ShowActions /
	/// ShowHeadCompanies) переключает ChildEntity корня так же, как
	/// AdvertTypeContainer.
	/// </summary>
	private static readonly Dictionary<string, Dictionary<string, Handler>> ClassActions = new()
	{
		// AdvertTypeContainer.DoAction: подмена ChildEntity корня (AdvertType ↔ AdvertTypeChild).
		["AdvertTypeContainer"] = new()
		{
			[AdvertTypeContainer.ActionNames.ShowTree] = (_, t) => Changed(((AdvertTypeContainer)t).ShowTree),
			[AdvertTypeContainer.ActionNames.ShowFlat] = (_, t) => Changed(((AdvertTypeContainer)t).ShowFlat),
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

	private static bool IsHidden(object target, string actionName, ViewType view) => target switch
	{
		PresentationObject po => po.IsActionHidden(actionName, view),
		FakeContainer fc => fc.IsActionHidden(actionName, view),
		_ => true
	};
}
