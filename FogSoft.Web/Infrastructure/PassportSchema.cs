using System.Xml;

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
/// Пока поддержан только <c>field</c> — этого хватает паспорту и фильтру
/// сущности 17. Остальные типы контролов (<c>lookup</c>, <c>objectPicker</c>,
/// <c>selector</c>, <c>treeselector</c>, <c>image</c>, <c>button</c>) —
/// работа этапа 2, см. docs/tasks/web-migration.md, раздел 4.2.
/// Неизвестный элемент не молчит, а превращается в <see cref="PassportField"/>
/// с <see cref="PassportField.Unsupported"/> — иначе поле тихо пропало бы из
/// карточки, а данные так же тихо не сохранились.
/// </summary>
public static class PassportSchema
{
	public static IReadOnlyList<PassportPage> Parse(string? xml)
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

				string? name = Attr(child, "name");
				if (string.IsNullOrEmpty(name))
					continue;

				page.Fields.Add(new PassportField(
					Name: name!,
					Caption: Attr(child, "caption") ?? name!,
					// type в метаданных указан не всегда: у паспорта сущности 17
					// его нет, тип берётся из атрибутов сущности. Здесь — только
					// то, что явно записано в XML.
					XmlType: Attr(child, "type"),
					Unsupported: child.Name != "field" ? child.Name : null));
			}
			pages.Add(page);
		}
		return pages;
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
/// <param name="Unsupported">Имя XML-элемента, если это не field. null — поддержано.</param>
public sealed record PassportField(
	string Name,
	string Caption,
	string? XmlType,
	string? Unsupported);
