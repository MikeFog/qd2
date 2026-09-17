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
/// Поддержаны <c>field</c> и <c>lookup</c>. Остальные типы контролов
/// (<c>objectPicker</c>, <c>selector</c>, <c>treeselector</c>, <c>image</c>,
/// <c>button</c>) — продолжение этапа 2, см. docs/tasks/web-migration.md,
/// раздел 4.2. Неизвестный элемент не молчит, а превращается в
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
	public static IReadOnlyList<PassportPage> Parse(string? xml, Entity? entity = null, bool isNew = false)
	{
		var pages = new List<PassportPage>();
		if (string.IsNullOrWhiteSpace(xml))
			return pages;

		var doc = new XmlDocument();
		doc.LoadXml(xml);

		foreach (XmlNode pageNode in doc.SelectNodes("//page")!)
		{
			var page = new PassportPage(Attr(pageNode, "caption") ?? "");
			foreach (XmlNode child in pageNode.ChildNodes)
			{
				if (child.NodeType != XmlNodeType.Element)
					continue;

				string? name = Attr(child, PageControl.Attributes.Name);
				if (string.IsNullOrEmpty(name))
					continue;

				page.Fields.Add(new PassportField(
					Name: name!,
					Caption: Attr(child, PageControl.Attributes.Caption) ?? name!,
					// type в метаданных указан не всегда: у паспорта сущности 17
					// его нет, тип берётся из атрибутов сущности. Здесь — только
					// то, что явно записано в XML.
					XmlType: Attr(child, PageControl.Attributes.Type),
					Unsupported: Unsupported(child),
					Required: IsRequired(child, name!, entity, isNew),
					Lookup: ParseLookup(child)));
			}
			pages.Add(page);
		}
		return pages;
	}

	private static string? Unsupported(XmlNode node)
	{
		if (node.Name == "field")
			return null;

		// lookup без source нарисовать не из чего: данные берутся из набора
		// строк, который вернула процедура паспорта. Атрибут entity как
		// запасной источник (PageFieldLookUp) не поддержан — в метаданных
		// ArtvisDev и прода нет ни одного lookup-а, где source не указан.
		if (node.Name == "lookup")
			return string.IsNullOrEmpty(Attr(node, PageControl.Attributes.Source))
				? "lookup без source"
				: null;

		return node.Name;
	}

	private static PassportLookup? ParseLookup(XmlNode node)
	{
		if (node.Name != "lookup")
			return null;

		string? source = Attr(node, PageControl.Attributes.Source);
		if (string.IsNullOrEmpty(source))
			return null;

		string? parentName = Attr(node, PageControl.Attributes.ParentLookupName);
		return new PassportLookup(
			Source: source!,
			// Умолчания те же, что у контрола LookUp: колонка значения — id,
			// колонка подписи — name. columnWithName в метаданных не встречается,
			// такого атрибута нет и в PageControl.Attributes.
			ColumnWithId: Attr(node, PageControl.Attributes.ColumnWithId) ?? Constants.Parameters.Id,
			ParentLookupName: string.IsNullOrEmpty(parentName) ? null : parentName,
			ParentFilter: Attr(node, PageControl.Attributes.Filter));
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
public sealed record PassportField(
	string Name,
	string Caption,
	string? XmlType,
	string? Unsupported,
	bool Required = false,
	PassportLookup? Lookup = null);

/// <param name="Source">Псевдоним набора строк из процедуры паспорта (iTableAlias).</param>
/// <param name="ColumnWithId">Колонка со значением, которое уходит в процедуру.</param>
/// <param name="ParentLookupName">Имя lookup-а, от которого зависит этот; null — независимый.</param>
/// <param name="ParentFilter">Выражение RowFilter с {0} вместо значения родителя.</param>
public sealed record PassportLookup(
	string Source,
	string ColumnWithId,
	string? ParentLookupName,
	string? ParentFilter);
