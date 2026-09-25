using System.Xml;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Разбор XML паспорта и фильтра из метаданных (<c>iEntity.passport</c> /
/// <c>iEntity.filter</c>) в структуру, независимую от способа отрисовки.
///
/// Это веб-аналог диспетчеризации из <c>PageControl.CreateInstance</c>: там по
/// имени XML-элемента создавался WinForms-контрол, здесь — описание поля,
/// которое компонент превращает в разметку. Разделение на «разобрать» и
/// «нарисовать» сделано намеренно: разбор не зависит от фронтенда и переживёт
/// смену способа отрисовки.
///
/// Поддержаны <c>field</c>, <c>lookup</c>, <c>objectPicker</c>,
/// <c>selector</c>, <c>treeselector</c>, <c>image</c> и <c>label</c>. Не поддержан
/// <c>button</c> (переносить нечего: сам контрол пустой, а
/// поведение живёт в форме паспорта ролика — этап 3, см.
/// docs/tasks/web-migration.md). Неизвестный элемент не молчит, а превращается в
/// <see cref="PassportField"/> с <see cref="PassportField.Unsupported"/> —
/// иначе поле тихо пропало бы из карточки, а данные так же тихо не сохранились.
/// </summary>
public static class PassportSchema
{
	/// <param name="entity">
	/// Нужна только для обязательности поля: она берётся из nullability колонки
	/// (<c>Entity.ColumnsInfo</c>), ровно как в <c>PageField</c>. null — правило
	/// сводится к атрибутам XML.
	/// </param>
	/// <param name="isNew">
	/// Новый объект или существующий: влияет на атрибут <c>isMandatoryOnCreate</c>.
	/// </param>
	/// <param name="pageType">
	/// Passport или Filter — тот же контекст, что <c>PageContext.PageType</c> в
	/// десктопе. Влияет только на <c>lookup</c> без <c>source</c>: запасной путь
	/// по атрибуту <c>entity</c> десктоп не различает по контексту (см.
	/// <c>PageFieldLookUp</c>), но в реальных метаданных он встречается только в
	/// фильтрах — в паспортах такой lookup не с чем сверить живьём, поэтому там
	/// он остаётся <see cref="PassportField.Unsupported"/>, а не молча
	/// притворяется рабочим.
	/// </param>
	public static IReadOnlyList<PassportPage> Parse(
		string? xml, Entity? entity = null, bool isNew = false, PageTypes pageType = PageTypes.Passport)
	{
		var pages = new List<PassportPage>();
		if (string.IsNullOrWhiteSpace(xml))
			return pages;

		var doc = new XmlDocument();
		doc.LoadXml(xml);

		foreach (XmlNode pageNode in doc.SelectNodes("//page")!)
		{
			var page = new PassportPage(Tr.T(Attr(pageNode, "caption")) ?? "");
			foreach (XmlNode child in pageNode.ChildNodes)
			{
				if (child.NodeType != XmlNodeType.Element)
					continue;

				// Разделитель — единственный элемент, который действительно нечего
				// показывать. Всё остальное без имени пропускать нельзя: у
				// selector-а в паспорте радиостанции атрибута name нет вовсе
				// (значение он пишет не по имени поля, а по сущности, см.
				// ObjectsSelector.ApplyChanges), и страница «Агентства»
				// получалась пустой — ровно то молчание, которого этот разбор
				// должен избегать.
				if (child.Name == "separator")
					continue;

				string name = Attr(child, PageControl.Attributes.Name) ?? string.Empty;

				// selector в фильтре — выбор нескольких объектов по сущности (станции журнала
				// использования роликов): тот же выбор, что у objectPicker, только с галочками.
				bool multiPick = pageType == PageTypes.Filter && child.Name == "selector";

				page.Fields.Add(new PassportField(
					Name: name,
					Caption: Tr.T(Attr(child, PageControl.Attributes.Caption)) ?? name,
					// type в метаданных указан не всегда: у паспорта сущности 17
					// его нет, тип берётся из атрибутов сущности. Здесь — только
					// то, что явно записано в XML.
					XmlType: Attr(child, PageControl.Attributes.Type),
					Unsupported: Unsupported(child, pageType),
					Required: IsRequired(child, name, entity, isNew),
					Lookup: ParseLookup(child),
					Picker: multiPick ? ParseMultiPicker(child) : ParsePicker(child),
					Selector: multiPick ? null : ParseSelector(child),
					MultiPick: multiPick,
					Image: ParseImage(child),
					Tree: ParseTree(child),
					Type: ResolveType(child, name, entity),
					IsLabel: child.Name == "label",
					Locked: IsLocked(child)));
			}
			pages.Add(page);
		}
		return pages;
	}

	/// <summary>
	/// Поле только для чтения по метаданным — дословно PageControl.SetControlLockedFlag:
	/// <c>disabled="true"</c> — для всех, <c>locked="true"</c> — для всех, кроме
	/// администратора. До этого веб атрибуты не читал, и такие поля (цена и исходное
	/// время в карточке рекламного окна, занятость и т.п.) были редактируемыми.
	/// </summary>
	private static bool IsLocked(XmlNode node)
	{
		if (IsTrue(Attr(node, PageControl.Attributes.Disabled)))
			return true;
		return IsTrue(Attr(node, PageControl.Attributes.Locked))
			&& SecurityManager.LoggedUser?.IsAdmin != true;
	}

	private static bool IsTrue(string? value) =>
		!string.IsNullOrEmpty(value) && ParseHelper.ParseToBoolean(value);

	private static string? Unsupported(XmlNode node, PageTypes pageType)
	{
		// Поле, список и выбор объекта пишут значение по имени параметра. Без
		// имени писать некуда, и рисовать ввод нельзя: он выглядел бы рабочим,
		// а значение пропадало бы.
		if ((node.Name == "field" || node.Name == "lookup" || node.Name == "objectPicker")
			&& string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Name)))
			return Tr.Format("{0} без атрибута name", node.Name);

		if (node.Name == "field" || node.Name == "label")
			return null;

		if (node.Name == "lookup")
		{
			if (!string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Source)))
				return null;

			// Без source список брать неоткуда готовым набором из процедуры
			// паспорта/фильтра. Запасной источник по атрибуту entity
			// (PageFieldLookUp грузит его сам через GetContent) в десктопе не
			// различает контекст, но живьём встречается только в фильтрах
			// (сущности 146, 1266, 1269) — в паспортах ни одного такого lookup
			// нет, сверить нечем, поэтому там он остаётся непереведённым, а не
			// молча притворяется рабочим.
			if (pageType == PageTypes.Filter && !string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Entity)))
				return null;

			return Tr.T("lookup без источника");
		}

		if (node.Name == "image")
			return null;

		if (node.Name == "treeselector")
		{
			// Все три колонки обязательны: десктопный TreeView2.InitFixedTree без
			// них строит дерево по пустым именам и падает на первой строке.
			// Набора по source может не оказаться — это видно уже при отрисовке.
			if (string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Source)))
				return Tr.T("treeselector без source");
			return string.IsNullOrEmpty(Attr(node, PageControl.Attributes.ColumnId))
				|| string.IsNullOrEmpty(Attr(node, PageControl.Attributes.ColumnParentid))
				|| string.IsNullOrEmpty(Attr(node, PageControl.Attributes.ColumnName))
				? Tr.T("treeselector без columnid/columnparentid/columnname")
				: null;
		}

		if (node.Name == "selector" && pageType == PageTypes.Filter)
		{
			// В фильтре selector — это выбор нескольких значений одного параметра: имя
			// параметра обязательно, набор строк — готовый по source или сущностью по entity.
			if (string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Name)))
				return Tr.T("selector в фильтре без атрибута name");
			if (string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Entity)))
				return Tr.T("selector без entity");
			return ParseHelper.ParseToBoolean(Attr(node, PageControl.Attributes.Multiselect) ?? string.Empty, false)
				? null
				: Tr.T("selector в фильтре без multiselect");
		}

		if (node.Name == "selector")
		{
			// Набор строк selector берёт только готовым, из процедуры паспорта:
			// в ObjectsSelector запасного пути по entity нет вовсе, в отличие
			// от objectPicker.
			if (string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Source)))
				return Tr.T("selector без source");
			return string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Entity))
				? Tr.T("selector без entity")
				: null;
		}

		if (node.Name == "objectPicker")
		{
			if (string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Entity)))
				return Tr.T("objectPicker без entity");

			// relationScenario уводит выбор в TreeViewSelector — дерево по
			// сценарию связей (веб: ScenarioTreePicker). На ArtvisDev такие
			// objectPicker есть только в фильтрах — «Предмет рекламы» у
			// статистики 158 и 201; в паспортах ни одного, поэтому там выбор
			// из дерева не подключался и поле остаётся непереведённым.
			return string.IsNullOrEmpty(Attr(node, PageControl.Attributes.RelationScenario))
				|| pageType == PageTypes.Filter
				? null
				: Tr.T("objectPicker с relationScenario");
		}

		return node.Name;
	}

	private static PassportLookup? ParseLookup(XmlNode node)
	{
		if (node.Name != "lookup")
			return null;

		string? source = Attr(node, PageControl.Attributes.Source);
		string? entityName = Attr(node, PageControl.Attributes.Entity);
		string? parentName = Attr(node, PageControl.Attributes.ParentLookupName);
		return new PassportLookup(
			Source: string.IsNullOrEmpty(source) ? null : source,
			// Запасной путь: сущность, из которой список грузится сам, когда
			// готового набора по source нет (PageFieldLookUp.GetEntity).
			EntityName: string.IsNullOrEmpty(entityName) ? null : entityName,
			// Умолчания те же, что у контрола LookUp: колонка значения — id,
			// колонка подписи — name. columnWithName в метаданных не встречается,
			// такого атрибута нет и в PageControl.Attributes.
			ColumnWithId: Attr(node, PageControl.Attributes.ColumnWithId) ?? Constants.Parameters.Id,
			ParentLookupName: string.IsNullOrEmpty(parentName) ? null : parentName,
			ParentFilter: Attr(node, PageControl.Attributes.Filter),
			// Вложенные <filter> — параметры для запасного пути по entity, тот
			// же разбор, что и у objectPicker (PageFieldSelector.GetFilters).
			Filters: ParseFilters(node));
	}

	/// <summary>
	/// Тип значения — тем же способом, что <c>PageField.CreateInstance</c>:
	/// сначала атрибут <c>type</c>, затем тип колонки сущности. Разбор отдан
	/// ядровому <see cref="FieldTypeResolver"/>, чтобы правила не разъехались.
	/// </summary>
	private static FieldTypeResolver ResolveType(XmlNode node, string name, Entity? entity)
	{
		ColumnInfo? columnInfo = null;
		entity?.ColumnsInfo.TryGetValue(name, out columnInfo);
		return new FieldTypeResolver(Attr(node, PageControl.Attributes.Type) ?? string.Empty, columnInfo);
	}

	private static PassportPicker? ParsePicker(XmlNode node)
	{
		if (node.Name != "objectPicker")
			return null;

		string? entityName = Attr(node, PageControl.Attributes.Entity);
		if (string.IsNullOrEmpty(entityName))
			return null;

		string? source = Attr(node, PageControl.Attributes.Source);
		string? scenario = Attr(node, PageControl.Attributes.RelationScenario);
		return new PassportPicker(
			EntityName: entityName!,
			Source: string.IsNullOrEmpty(source) ? null : source,
			// Умолчание то же, что в PageFieldObjectPicker.IsCreateNewAllowed:
			// нет атрибута — кнопки нет.
			IsCreateNewAllowed: ParseHelper.ParseToBoolean(
				Attr(node, PageControl.Attributes.IsCreateNewAllowed) ?? string.Empty, false),
			Filters: ParseFilters(node),
			Scenario: string.IsNullOrEmpty(scenario) ? null : scenario);
	}

	private static PassportImage? ParseImage(XmlNode node)
	{
		if (node.Name != "image")
			return null;

		// Умолчание то же, что в PageFieldImage: 60 точек, если высота не задана.
		int height = ParseHelper.ParseToInt32(
			Attr(node, PageControl.Attributes.Height) ?? string.Empty, 0);
		return new PassportImage(height > 0 ? height : 60);
	}

	/// <summary>
	/// <c>treeselector</c> — дерево с галочками из плоской таблицы (десктопный
	/// <c>TreeObjectsSelector</c>). Встречается только в именованных паспортах
	/// (iPassport) и везде объявлен одинаково: source="days", колонки id /
	/// parentID / name. Имена берутся из атрибутов, а не зашиты.
	/// </summary>
	private static PassportTree? ParseTree(XmlNode node)
	{
		if (node.Name != "treeselector")
			return null;

		string? source = Attr(node, PageControl.Attributes.Source);
		string? columnId = Attr(node, PageControl.Attributes.ColumnId);
		string? columnParentId = Attr(node, PageControl.Attributes.ColumnParentid);
		string? columnName = Attr(node, PageControl.Attributes.ColumnName);
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(columnId)
			|| string.IsNullOrEmpty(columnParentId) || string.IsNullOrEmpty(columnName))
			return null;

		return new PassportTree(source!, columnId!, columnParentId!, columnName!);
	}

	/// <summary>selector в фильтре: выбирают из той же сущности, что objectPicker.</summary>
	private static PassportPicker? ParseMultiPicker(XmlNode node)
	{
		string? entityName = Attr(node, PageControl.Attributes.Entity);
		if (string.IsNullOrEmpty(entityName))
			return null;
		return new PassportPicker(entityName!, Attr(node, PageControl.Attributes.Source), false,
			Array.Empty<PassportFilterValue>());
	}

	private static PassportSelector? ParseSelector(XmlNode node)
	{
		if (node.Name != "selector")
			return null;

		string? source = Attr(node, PageControl.Attributes.Source);
		string? entityName = Attr(node, PageControl.Attributes.Entity);
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(entityName))
			return null;

		return new PassportSelector(
			Source: source!,
			EntityName: entityName!,
			// Без multiselect грид в десктопе рисуется без колонки с галочками
			// (ObjectsSelector.HasCheckBox), то есть список только для чтения.
			Multiselect: ParseHelper.ParseToBoolean(
				Attr(node, PageControl.Attributes.Multiselect) ?? string.Empty, false));
	}

	/// <summary>
	/// Вложенные <c>&lt;filter&gt;</c> — то же, что читает
	/// <c>PageFieldSelector.GetFilters</c>. Значения без типа там молча
	/// пропускаются, здесь так же.
	/// </summary>
	private static IReadOnlyList<PassportFilterValue> ParseFilters(XmlNode node)
	{
		var filters = new List<PassportFilterValue>();
		foreach (XmlNode child in node.ChildNodes)
		{
			if (child.NodeType != XmlNodeType.Element || child.Name != "filter")
				continue;

			string? type = Attr(child, PageControl.Attributes.Type);
			string? name = Attr(child, PageControl.Attributes.Name);
			if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(name))
				continue;

			filters.Add(new PassportFilterValue(
				name!, Attr(child, PageControl.Attributes.Value) ?? string.Empty, type!));
		}
		return filters;
	}

	/// <summary>
	/// Повторяет разрешение обязательности из <c>PageField</c>: атрибут
	/// <c>required</c> сильнее всего, иначе — nullability колонки, иначе —
	/// атрибут <c>mandatory</c>; у нового объекта <c>isMandatoryOnCreate="false"</c>
	/// снимает обязательность.
	/// </summary>
	private static bool IsRequired(XmlNode node, string name, Entity? entity, bool isNew)
	{
		string? required = Attr(node, PageControl.Attributes.Required);
		if (!string.IsNullOrEmpty(required))
			return ParseHelper.ParseToBoolean(required, false);

		bool isNullable;
		if (entity != null && entity.ColumnsInfo.TryGetValue(name, out ColumnInfo? columnInfo))
			isNullable = columnInfo.IsNullable;
		else
			isNullable = !ParseHelper.ParseToBoolean(
				Attr(node, PageControl.Attributes.Mandatory) ?? string.Empty, false);

		if (isNew && !isNullable)
		{
			string? onCreate = Attr(node, PageControl.Attributes.IsMandatoryOnCreate);
			if (!string.IsNullOrEmpty(onCreate) && !ParseHelper.ParseToBoolean(onCreate))
				isNullable = true;
		}

		return !isNullable;
	}

	private static string? Attr(XmlNode node, string name) =>
		node.Attributes?[name]?.Value;
}

public sealed class PassportPage
{
	public PassportPage(string caption) => Caption = caption;

	public string Caption { get; }
	public List<PassportField> Fields { get; } = new();
}

/// <param name="Name">Имя параметра — ключ в PresentationObject и в процедуре.</param>
/// <param name="Caption">Подпись из метаданных.</param>
/// <param name="XmlType">Тип, явно указанный в XML (например, boolean); null, если не указан.</param>
/// <param name="Unsupported">Причина, по которой контрол не рисуется; null — поддержан.</param>
/// <param name="Required">Значение обязательно — пустым сохранять нельзя.</param>
/// <param name="Lookup">Описание выпадающего списка; null — это не lookup.</param>
/// <param name="Picker">Описание выбора объекта; null — это не objectPicker.</param>
/// <param name="Type">Разрешённый тип значения; null у полей, где он не нужен.</param>
/// <param name="Selector">Описание набора дочерних объектов; null — это не selector.</param>
/// <param name="Image">Описание картинки; null — это не image.</param>
/// <param name="Tree">Описание дерева с галочками; null — это не treeselector.</param>
/// <param name="MultiPick">
/// selector в фильтре: выбор нескольких объектов из <see cref="Picker"/>. Значение —
/// ключи через запятую с запятой в конце («12,15,»), как ждут процедуры
/// (<c>massmediaString</c> → <c>fn_CreateTableFromString</c>).
/// </param>
/// <param name="IsLabel">
/// Элемент &lt;label&gt;: десктопный PageFieldLabel — только показ, WinForms
/// Label без ApplyChanges. Значение не редактируется и не уходит в процедуру.
/// </param>
public sealed record PassportField(
	string Name,
	string Caption,
	string? XmlType,
	string? Unsupported,
	bool Required = false,
	PassportLookup? Lookup = null,
	PassportPicker? Picker = null,
	FieldTypeResolver? Type = null,
	PassportSelector? Selector = null,
	PassportImage? Image = null,
	bool IsLabel = false,
	PassportTree? Tree = null,
	bool Locked = false,
	bool MultiPick = false);

/// <param name="Source">Псевдоним набора строк из процедуры паспорта/фильтра (iTableAlias); null — набора нет, список берётся сущностью по <paramref name="EntityName"/>.</param>
/// <param name="EntityName">Сущность запасного пути, когда готового набора по source нет; null — запасного пути нет.</param>
/// <param name="ColumnWithId">Колонка со значением, которое уходит в процедуру.</param>
/// <param name="ParentLookupName">Имя lookup-а, от которого зависит этот; null — независимый.</param>
/// <param name="ParentFilter">Выражение RowFilter с {0} вместо значения родителя.</param>
/// <param name="Filters">Значения вложенного &lt;filter&gt; для запасного пути по entity.</param>
public sealed record PassportLookup(
	string? Source,
	string? EntityName,
	string ColumnWithId,
	string? ParentLookupName,
	string? ParentFilter,
	IReadOnlyList<PassportFilterValue> Filters);

/// <param name="EntityName">Имя сущности, из которой выбирают (iEntity.name).</param>
/// <param name="Source">Псевдоним готового набора строк; null — грузить сущностью по требованию.</param>
/// <param name="IsCreateNewAllowed">Разрешено ли создавать новый объект прямо из карточки.</param>
/// <param name="Filters">Значения фильтра из вложенных &lt;filter&gt;.</param>
/// <param name="Scenario">Сценарий связей (relationScenario): выбор из дерева, а не из списка; null — из списка.</param>
public sealed record PassportPicker(
	string EntityName,
	string? Source,
	bool IsCreateNewAllowed,
	IReadOnlyList<PassportFilterValue> Filters,
	string? Scenario = null);

/// <param name="Name">Имя параметра процедуры выборки.</param>
/// <param name="Value">Значение как записано в XML.</param>
/// <param name="Type">Тип для FieldTypeResolver либо parameter / parameter_isnew.</param>
public sealed record PassportFilterValue(string Name, string Value, string Type);

/// <param name="Source">Псевдоним набора строк из процедуры паспорта (iTableAlias).</param>
/// <param name="EntityName">Сущность строк набора: из неё берутся колонки и объекты.</param>
/// <param name="Multiselect">Есть ли галочки; без них список только для чтения.</param>
public sealed record PassportSelector(
	string Source,
	string EntityName,
	bool Multiselect);

/// <param name="Height">Высота показа в точках; 60, если в метаданных не задана.</param>
public sealed record PassportImage(int Height);

/// <param name="Source">Псевдоним набора строк из процедуры паспорта (iTableAlias).</param>
/// <param name="ColumnId">Колонка идентификатора узла; её значения — результат выбора.</param>
/// <param name="ColumnParentId">Колонка идентификатора родителя; пусто — узел верхнего уровня.</param>
/// <param name="ColumnName">Колонка подписи узла.</param>
public sealed record PassportTree(
	string Source,
	string ColumnId,
	string ColumnParentId,
	string ColumnName);
