using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FogSoft.WinForm.Classes;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Merlin.Classes.Documents
{
	/// <summary>Шаблон нельзя применить: неизвестное поле, незакрытый блок и т.п.</summary>
	public sealed class DocumentTemplateException : Exception
	{
		public DocumentTemplateException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// Документ Word из шаблона с метками <c>{{Поле}}</c>, <c>{{#Блок}}…{{/Блок}}</c>,
	/// <c>{{^Блок}}…{{/Блок}}</c> — синтаксис в docs/tasks/web-reports.md §8.1.
	///
	/// Word режет набранный текст на фрагменты (проверка орфографии, правки, смена
	/// шрифта), поэтому сначала каждая метка собирается в отдельный фрагмент с
	/// оформлением её первого символа, и дальше работа идёт с целыми метками.
	/// </summary>
	public static class DocxTemplate
	{
		private static readonly Regex TagRegex =
			new Regex(@"\{\{\s*([#^/]?)\s*([^{}]*?)\s*\}\}", RegexOptions.Compiled);

		private const char FieldTag = ' ';
		private const char OpenTag = '#';
		private const char InvertedTag = '^';
		private const char CloseTag = '/';

		/// <summary>Заполняет шаблон. Шаблон должен пройти <see cref="Validate"/> по тем же полям.</summary>
		public static byte[] Render(byte[] template, DocumentData data)
		{
			using (var stream = new MemoryStream())
			{
				stream.Write(template, 0, template.Length);
				using (WordprocessingDocument doc = WordprocessingDocument.Open(stream, true))
				{
					List<OpenXmlPart> parts = ContentParts(doc).ToList();
					var context = new RenderContext(MaxDrawingId(parts));
					foreach (OpenXmlPart part in parts)
					{
						OpenXmlPartRootElement root = part.RootElement;
						NormalizeTags(root);
						context.Part = part;
						var roots = new List<OpenXmlElement> { root };
						ProcessSections(roots, data, context);
						ReplaceFields(roots, data, context);
						root.Save();
					}
				}
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Ошибки шаблона по каталогу полей; пустой список — шаблон годен.
		/// Сообщения — для администратора, который правил шаблон в Word.
		/// </summary>
		public static IList<string> Validate(byte[] template, IEnumerable<DocumentField> fields)
		{
			var errors = new List<string>();
			var global = fields.ToDictionary(f => f.Name, DocumentFieldNameComparer.Instance);
			using (var stream = new MemoryStream())
			{
				stream.Write(template, 0, template.Length);
				WordprocessingDocument doc = TryOpen(stream);
				if (doc == null)
					return new List<string> { Tr.T("Файл не читается как документ Word (.docx).") };
				using (doc)
				{
					foreach (OpenXmlPart part in ContentParts(doc))
					{
						NormalizeTags(part.RootElement);
						CheckBrokenTags(part.RootElement, errors);
						CheckTags(Tags(new OpenXmlElement[] { part.RootElement }), global, errors);
					}
				}
			}
			return errors.Distinct().ToList();
		}

		private static WordprocessingDocument TryOpen(Stream stream)
		{
			WordprocessingDocument doc = null;
			try
			{
				doc = WordprocessingDocument.Open(stream, true);
				if (doc.MainDocumentPart != null && doc.MainDocumentPart.Document != null)
					return doc;
			}
			catch (Exception e) // любой сбой разбора = «не документ Word»; причину пишем в лог
			{
				ErrorManager.LogError("DocxTemplate: не удалось открыть шаблон", e); // i18n-ok: лог
			}
			if (doc != null)
				doc.Dispose();
			return null;
		}

		private static IEnumerable<OpenXmlPart> ContentParts(WordprocessingDocument doc)
		{
			MainDocumentPart main = doc.MainDocumentPart;
			yield return main;
			foreach (HeaderPart header in main.HeaderParts)
				yield return header;
			foreach (FooterPart footer in main.FooterParts)
				yield return footer;
		}

		#region Метки

		private sealed class Tag
		{
			public Run Run;
			public char Kind;
			public string Name;

			public string Display
			{
				get { return TagText(Kind, Name); }
			}
		}

		private static string TagText(char kind, string name)
		{
			return "{{" + (kind == FieldTag ? string.Empty : kind.ToString()) + name + "}}";
		}

		/// <summary>Фрагмент, целиком состоящий из одной метки (после <see cref="NormalizeTags"/>).</summary>
		private static Tag AsTag(Run run)
		{
			Text text = null;
			foreach (OpenXmlElement child in run.ChildElements)
			{
				if (child is RunProperties)
					continue;
				if (text != null || !(child is Text))
					return null;
				text = (Text)child;
			}
			if (text == null)
				return null;
			Match match = TagRegex.Match(text.Text);
			if (!match.Success || match.Index != 0 || match.Length != text.Text.Length)
				return null;
			string kind = match.Groups[1].Value;
			return new Tag
			{
				Run = run,
				Kind = kind.Length == 0 ? FieldTag : kind[0],
				Name = match.Groups[2].Value
			};
		}

		private static List<Tag> Tags(IEnumerable<OpenXmlElement> roots)
		{
			var result = new List<Tag>();
			foreach (OpenXmlElement root in roots)
			{
				// Корень, удалённый обработкой вложенного блока, уже не часть документа.
				if (root.Parent == null && !(root is OpenXmlPartRootElement))
					continue;
				IEnumerable<Run> runs = root is Run
					? new[] { (Run)root }.Concat(root.Descendants<Run>())
					: root.Descendants<Run>();
				foreach (Run run in runs)
				{
					Tag tag = AsTag(run);
					if (tag != null)
						result.Add(tag);
				}
			}
			return result;
		}

		private sealed class TextSpan
		{
			public Text Text;
			public int Start;

			public int End
			{
				get { return Start + Text.Text.Length; }
			}
		}

		/// <summary>Тексты абзаца по порядку (без текстов вложенных надписей).</summary>
		private static List<TextSpan> Spans(Paragraph paragraph)
		{
			var spans = new List<TextSpan>();
			int position = 0;
			foreach (Text text in paragraph.Descendants<Text>())
			{
				if (!(text.Parent is Run) || text.Ancestors<Paragraph>().First() != paragraph)
					continue;
				spans.Add(new TextSpan { Text = text, Start = position });
				position += text.Text.Length;
			}
			return spans;
		}

		private static string SpansText(IEnumerable<TextSpan> spans)
		{
			return string.Concat(spans.Select(s => s.Text.Text));
		}

		private static void NormalizeTags(OpenXmlElement root)
		{
			foreach (Paragraph paragraph in root.Descendants<Paragraph>().ToList())
			{
				string full = SpansText(Spans(paragraph));
				if (full.IndexOf("{{", StringComparison.Ordinal) < 0)
					continue;
				// С конца: выделение метки не меняет текст абзаца, позиции остальных не плывут.
				foreach (Match match in TagRegex.Matches(full).Cast<Match>().Reverse())
					IsolateTag(paragraph, match.Index, match.Length);
			}
		}

		private static void IsolateTag(Paragraph paragraph, int start, int length)
		{
			List<TextSpan> spans = Spans(paragraph);
			int end = start + length;
			TextSpan first = spans.First(s => s.Start <= start && start < s.End);
			TextSpan last = spans.First(s => s.Start < end && end <= s.End);
			Run firstRun = (Run)first.Text.Parent;
			if (first == last && AsTag(firstRun) != null)
				return;
			// Метка, начатая в ссылке и законченная вне её, не собирается — её покажет проверка.
			if (firstRun.Parent != last.Text.Parent.Parent)
				return;

			string tagText = SpansText(spans).Substring(start, length);
			Run after = SplitRunAt(last.Text, end - last.Start);
			Run at = SplitRunAt(first.Text, start - first.Start);

			var tagRun = new Run();
			if (at.RunProperties != null)
				tagRun.Append(at.RunProperties.CloneNode(true));
			tagRun.Append(new Text(tagText) { Space = SpaceProcessingModeValues.Preserve });
			at.InsertBeforeSelf(tagRun);

			for (OpenXmlElement node = at; node != null && node != after;)
			{
				OpenXmlElement next = node.NextSibling();
				if (node is Run)
					node.Remove();
				node = next;
			}
			RemoveIfEmpty(firstRun);
			RemoveIfEmpty(after);
		}

		/// <summary>
		/// Делит фрагмент на два перед символом <paramref name="offset"/> текста
		/// <paramref name="text"/>; возвращает правую часть.
		/// </summary>
		private static Run SplitRunAt(Text text, int offset)
		{
			var run = (Run)text.Parent;
			var right = new Run();
			if (run.RunProperties != null)
				right.Append(run.RunProperties.CloneNode(true));

			OpenXmlElement moveFrom;
			if (offset <= 0)
				moveFrom = text;
			else
			{
				if (offset < text.Text.Length)
				{
					var rest = new Text(text.Text.Substring(offset)) { Space = SpaceProcessingModeValues.Preserve };
					text.Text = text.Text.Substring(0, offset);
					text.Space = SpaceProcessingModeValues.Preserve;
					text.InsertAfterSelf(rest);
				}
				moveFrom = text.NextSibling();
			}

			var moving = new List<OpenXmlElement>();
			for (OpenXmlElement node = moveFrom; node != null; node = node.NextSibling())
				moving.Add(node);
			foreach (OpenXmlElement node in moving)
			{
				node.Remove();
				right.Append(node);
			}
			run.InsertAfterSelf(right);
			return right;
		}

		private static void RemoveIfEmpty(Run run)
		{
			if (run.Parent != null && run.ChildElements.All(c => c is RunProperties))
				run.Remove();
		}

		#endregion

		#region Блоки

		private enum SectionShape
		{
			Inline,
			Paragraphs,
			Rows
		}

		private sealed class Section
		{
			public SectionShape Shape;
			public Tag Open;
			public Tag Close;

			/// <summary>Повторяемое / удаляемое содержимое (для строк таблицы метки внутри него).</summary>
			public List<OpenXmlElement> Content = new List<OpenXmlElement>();

			/// <summary>Абзацы-метки для <see cref="SectionShape.Paragraphs"/>.</summary>
			public Paragraph OpenParagraph;
			public Paragraph CloseParagraph;
		}

		/// <summary>Форма блока по положению меток; null и текст ошибки, если форма недопустима.</summary>
		private static Section Classify(Tag open, Tag close, out string error)
		{
			error = null;
			var section = new Section { Open = open, Close = close };
			Paragraph openParagraph = open.Run.Ancestors<Paragraph>().First();
			Paragraph closeParagraph = close.Run.Ancestors<Paragraph>().First();

			if (openParagraph == closeParagraph)
			{
				if (open.Run.Parent != close.Run.Parent)
				{
					error = Tr.Format("Метки {0} и {1} должны стоять на одном уровне абзаца (не одна в ссылке или поле, а другая — вне).",
						open.Display, close.Display);
					return null;
				}
				section.Shape = SectionShape.Inline;
				section.Content = Between(open.Run, close.Run);
				return section;
			}

			if (openParagraph.Parent == closeParagraph.Parent)
			{
				if (!IsAlone(openParagraph, open.Run) || !IsAlone(closeParagraph, close.Run))
				{
					error = Tr.Format("Блок {0}…{1} занимает несколько абзацев — тогда каждая из этих меток должна стоять отдельным абзацем.",
						open.Display, close.Display);
					return null;
				}
				section.Shape = SectionShape.Paragraphs;
				section.OpenParagraph = openParagraph;
				section.CloseParagraph = closeParagraph;
				section.Content = Between(openParagraph, closeParagraph);
				return section;
			}

			TableRow openRow = openParagraph.Ancestors<TableRow>().FirstOrDefault();
			TableRow closeRow = closeParagraph.Ancestors<TableRow>().FirstOrDefault();
			if (openRow != null && closeRow != null && openRow.Parent == closeRow.Parent)
			{
				List<TableRow> rows = openRow.Parent.Elements<TableRow>().ToList();
				section.Shape = SectionShape.Rows;
				section.Content = rows.GetRange(rows.IndexOf(openRow), rows.IndexOf(closeRow) - rows.IndexOf(openRow) + 1)
					.Cast<OpenXmlElement>().ToList();
				return section;
			}

			error = Tr.Format("Метки {0} и {1} должны стоять в одном абзаце, отдельными абзацами в одном месте документа или в одной таблице.",
				open.Display, close.Display);
			return null;
		}

		private static List<OpenXmlElement> Between(OpenXmlElement from, OpenXmlElement to)
		{
			var result = new List<OpenXmlElement>();
			for (OpenXmlElement node = from.NextSibling(); node != null && node != to; node = node.NextSibling())
				result.Add(node);
			return result;
		}

		private static bool IsAlone(Paragraph paragraph, Run tagRun)
		{
			return Spans(paragraph).Where(s => s.Text.Parent != tagRun).All(s => string.IsNullOrWhiteSpace(s.Text.Text));
		}

		private static int FindClose(List<Tag> tags, int openIndex)
		{
			int depth = 0;
			for (int i = openIndex + 1; i < tags.Count; i++)
			{
				if (!DocumentFieldNameComparer.Instance.Equals(tags[i].Name, tags[openIndex].Name))
					continue;
				if (tags[i].Kind == OpenTag || tags[i].Kind == InvertedTag)
					depth++;
				else if (tags[i].Kind == CloseTag)
				{
					if (depth == 0)
						return i;
					depth--;
				}
			}
			return -1;
		}

		private static void ProcessSections(List<OpenXmlElement> roots, DocumentData data, RenderContext context)
		{
			while (true)
			{
				List<Tag> tags = Tags(roots);
				int openIndex = tags.FindIndex(t => t.Kind == OpenTag || t.Kind == InvertedTag);
				if (openIndex < 0)
					return;
				Tag open = tags[openIndex];
				int closeIndex = FindClose(tags, openIndex);
				if (closeIndex < 0)
					throw new DocumentTemplateException(Tr.Format("Блок {0} не закрыт меткой {1}.",
						open.Display, TagText(CloseTag, open.Name)));

				string error;
				Section section = Classify(open, tags[closeIndex], out error);
				if (section == null)
					throw new DocumentTemplateException(error);

				object value = Resolve(data, open);
				var list = value as List<DocumentData>;
				if (list != null && open.Kind == OpenTag)
				{
					Repeat(section, list, context);
					continue;
				}
				bool show = IsTrue(value) == (open.Kind == OpenTag);
				if (show)
					RemoveMarkers(section);
				else
					RemoveSection(section);
			}
		}

		private static object Resolve(DocumentData data, Tag tag)
		{
			object value;
			if (!data.TryGet(tag.Name, out value))
				throw new DocumentTemplateException(Tr.Format("Неизвестное поле {0}.", tag.Display));
			return value;
		}

		private static bool IsTrue(object value)
		{
			if (value == null)
				return false;
			if (value is bool)
				return (bool)value;
			var text = value as string;
			if (text != null)
				return !string.IsNullOrWhiteSpace(text);
			var list = value as List<DocumentData>;
			if (list != null)
				return list.Count > 0;
			return true;
		}

		private static void RemoveMarkers(Section section)
		{
			if (section.Shape == SectionShape.Paragraphs)
			{
				RemoveMarkerParagraph(section.OpenParagraph, section.Open.Run);
				RemoveMarkerParagraph(section.CloseParagraph, section.Close.Run);
			}
			else
			{
				section.Open.Run.Remove();
				section.Close.Run.Remove();
			}
		}

		private static void RemoveMarkerParagraph(Paragraph paragraph, Run tagRun)
		{
			// Абзац с разрывом раздела не удаляем — пропал бы раздел (ориентация, колонтитулы).
			if (paragraph.Descendants<SectionProperties>().Any())
			{
				tagRun.Remove();
				return;
			}
			OpenXmlElement parent = paragraph.Parent;
			paragraph.Remove();
			KeepCellValid(parent);
		}

		private static void RemoveSection(Section section)
		{
			OpenXmlElement parent = section.Content.Count > 0 ? section.Content[0].Parent : null;
			foreach (OpenXmlElement node in section.Content)
				node.Remove();
			if (section.Shape == SectionShape.Rows)
			{
				KeepTableValid(parent);
				return;
			}
			if (section.Shape == SectionShape.Paragraphs)
				RemoveMarkers(section);
			else
			{
				section.Open.Run.Remove();
				section.Close.Run.Remove();
			}
			if (parent != null)
				KeepCellValid(parent);
		}

		private static void Repeat(Section section, List<DocumentData> items, RenderContext context)
		{
			// Для строк таблицы метки лежат внутри повторяемого — находим их в копиях по номеру фрагмента.
			List<Run> originalRuns = section.Content.SelectMany(n => n.Descendants<Run>()).ToList();
			int openRunIndex = originalRuns.IndexOf(section.Open.Run);
			int closeRunIndex = originalRuns.IndexOf(section.Close.Run);

			OpenXmlElement anchor = section.Shape == SectionShape.Rows
				? section.Content[0]
				: section.Shape == SectionShape.Paragraphs ? (OpenXmlElement)section.OpenParagraph : section.Open.Run;

			foreach (DocumentData item in items)
			{
				List<OpenXmlElement> copies = section.Content.Select(n => n.CloneNode(true)).ToList();
				if (section.Shape == SectionShape.Rows)
				{
					List<Run> runs = copies.SelectMany(n => n.Descendants<Run>()).ToList();
					runs[closeRunIndex].Remove();
					runs[openRunIndex].Remove();
				}
				// Сначала в документ: вложенным блокам нужны соседи и предки (абзац, таблица).
				foreach (OpenXmlElement copy in copies)
					anchor.InsertBeforeSelf(copy);
				ProcessSections(copies, item, context);
				ReplaceFields(copies, item, context);
			}
			RemoveSection(section);
		}

		/// <summary>Ячейка таблицы обязана содержать абзац, иначе Word считает файл повреждённым.</summary>
		private static void KeepCellValid(OpenXmlElement container)
		{
			var cell = container as TableCell;
			if (cell != null && !cell.Elements<Paragraph>().Any() && !cell.Elements<Table>().Any())
				cell.Append(new Paragraph());
		}

		private static void KeepTableValid(OpenXmlElement container)
		{
			var table = container as Table;
			if (table == null || table.Elements<TableRow>().Any())
				return;
			OpenXmlElement parent = table.Parent;
			table.Remove();
			KeepCellValid(parent);
		}

		#endregion

		#region Значения

		private sealed class RenderContext
		{
			public RenderContext(uint lastDrawingId)
			{
				LastDrawingId = lastDrawingId;
			}

			public OpenXmlPart Part;
			public uint LastDrawingId;
		}

		private static uint MaxDrawingId(IEnumerable<OpenXmlPart> parts)
		{
			uint max = 0;
			foreach (OpenXmlPart part in parts)
				foreach (DW.DocProperties properties in part.RootElement.Descendants<DW.DocProperties>())
					if (properties.Id != null && properties.Id.Value > max)
						max = properties.Id.Value;
			return max;
		}

		private static void ReplaceFields(IEnumerable<OpenXmlElement> roots, DocumentData data, RenderContext context)
		{
			foreach (Tag tag in Tags(roots))
			{
				if (tag.Kind != FieldTag)
					continue;
				object value = Resolve(data, tag);
				if (value == null || value is string)
					SetText(tag.Run, (string)value);
				else if (value is DocumentImage)
					SetImage(tag.Run, (DocumentImage)value, context);
				else
					throw new DocumentTemplateException(Tr.Format("Поле {0} — условие или список, его нельзя вывести как текст.", tag.Display));
			}
		}

		private static void SetText(Run run, string value)
		{
			foreach (Text old in run.Elements<Text>().ToList())
				old.Remove();
			string[] lines = (value ?? string.Empty).Replace("\r\n", "\n").Split('\n');
			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0)
					run.Append(new Break());
				run.Append(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
			}
		}

		private static void SetImage(Run run, DocumentImage image, RenderContext context)
		{
			foreach (Text old in run.Elements<Text>().ToList())
				old.Remove();

			ImagePart imagePart = AddImagePart(context.Part);
			using (var stream = new MemoryStream(image.Png))
				imagePart.FeedData(stream);
			string relationId = context.Part.GetIdOfPart(imagePart);
			uint id = ++context.LastDrawingId;
			string name = "Picture " + id; // i18n-ok: служебное имя объекта в файле

			run.Append(new Drawing(
				new DW.Inline(
					new DW.Extent { Cx = image.WidthEmu, Cy = image.HeightEmu },
					new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
					new DW.DocProperties { Id = id, Name = name },
					new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
					new A.Graphic(
						new A.GraphicData(
							new PIC.Picture(
								new PIC.NonVisualPictureProperties(
									new PIC.NonVisualDrawingProperties { Id = 0U, Name = name + ".png" },
									new PIC.NonVisualPictureDrawingProperties()),
								new PIC.BlipFill(
									new A.Blip { Embed = relationId },
									new A.Stretch(new A.FillRectangle())),
								new PIC.ShapeProperties(
									new A.Transform2D(
										new A.Offset { X = 0L, Y = 0L },
										new A.Extents { Cx = image.WidthEmu, Cy = image.HeightEmu }),
									new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
						{ Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
				{
					DistanceFromTop = 0U,
					DistanceFromBottom = 0U,
					DistanceFromLeft = 0U,
					DistanceFromRight = 0U
				}));
		}

		private static ImagePart AddImagePart(OpenXmlPart part)
		{
			var main = part as MainDocumentPart;
			if (main != null)
				return main.AddImagePart(ImagePartType.Png);
			var header = part as HeaderPart;
			if (header != null)
				return header.AddImagePart(ImagePartType.Png);
			return ((FooterPart)part).AddImagePart(ImagePartType.Png);
		}

		#endregion

		#region Проверка

		private static void CheckBrokenTags(OpenXmlElement root, List<string> errors)
		{
			foreach (Paragraph paragraph in root.Descendants<Paragraph>())
			{
				List<TextSpan> spans = Spans(paragraph)
					.Where(s => AsTag((Run)s.Text.Parent) == null).ToList();
				string rest = SpansText(spans);
				if (rest.IndexOf("{{", StringComparison.Ordinal) < 0 && rest.IndexOf("}}", StringComparison.Ordinal) < 0)
					continue;
				string text = SpansText(Spans(paragraph)).Trim();
				if (text.Length > 80)
					text = text.Substring(0, 80) + "…";
				errors.Add(Tr.Format("Испорченная метка в абзаце «{0}»: проверьте парные скобки {1} или наберите метку заново одним куском.", text, "{{ }}"));
			}
		}

		private sealed class Frame
		{
			public Tag Open;
			public Dictionary<string, DocumentField> ItemFields;
		}

		private static void CheckTags(List<Tag> tags, Dictionary<string, DocumentField> global, List<string> errors)
		{
			var stack = new Stack<Frame>();
			foreach (Tag tag in tags)
			{
				if (tag.Kind == CloseTag)
				{
					if (stack.Count == 0 || !DocumentFieldNameComparer.Instance.Equals(stack.Peek().Open.Name, tag.Name))
					{
						errors.Add(stack.Count == 0
							? Tr.Format("Метка {0} закрывает блок, который не был открыт.", tag.Display)
							: Tr.Format("Метка {0} стоит раньше, чем закрыт блок {1}.", tag.Display, stack.Peek().Open.Display));
						continue;
					}
					string error;
					if (Classify(stack.Pop().Open, tag, out error) == null)
						errors.Add(error);
					continue;
				}

				DocumentField field = Find(tag.Name, stack, global);
				if (field == null)
				{
					errors.Add(Tr.Format("Неизвестное поле {0}.", tag.Display));
					if (tag.Kind != FieldTag)
						stack.Push(new Frame { Open = tag });
					continue;
				}

				if (tag.Kind == FieldTag)
				{
					if (field.Kind == DocumentFieldKind.Flag || field.Kind == DocumentFieldKind.List)
						errors.Add(Tr.Format("Поле {0} — условие или список: пишите {1}…{2}.",
							tag.Display, TagText(OpenTag, tag.Name), TagText(CloseTag, tag.Name)));
					continue;
				}

				var frame = new Frame { Open = tag };
				if (field.Kind == DocumentFieldKind.List && tag.Kind == OpenTag)
					frame.ItemFields = field.ItemFields.ToDictionary(f => f.Name, DocumentFieldNameComparer.Instance);
				stack.Push(frame);
			}
			foreach (Frame frame in stack)
				errors.Add(Tr.Format("Блок {0} не закрыт меткой {1}.", frame.Open.Display, TagText(CloseTag, frame.Open.Name)));
		}

		private static DocumentField Find(string name, IEnumerable<Frame> stack, Dictionary<string, DocumentField> global)
		{
			DocumentField field;
			foreach (Frame frame in stack)
				if (frame.ItemFields != null && frame.ItemFields.TryGetValue(name, out field))
					return field;
			return global.TryGetValue(name, out field) ? field : null;
		}

		#endregion
	}
}
