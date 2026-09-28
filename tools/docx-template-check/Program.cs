using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using Merlin.Classes.Documents;

// Проверки движка Word-шаблонов: шаблон собирается кодом, метки специально
// режутся на фрагменты, как это делает Word; сверяется текст результата и
// валидность файла. Файлы результата — в папку из аргумента (для просмотра в Word).
internal static class Program
{
	private static int failed;
	private static string outDir;

	private static int Main(string[] args)
	{
		outDir = args.Length > 0 ? args[0] : null;
		if (outDir != null)
			Directory.CreateDirectory(outDir);

		RenderCases();
		ValidateCases();

		Console.WriteLine(failed == 0 ? "OK: все проверки прошли" : $"ОШИБОК: {failed}");
		return failed == 0 ? 0 : 1;
	}

	private static readonly DocumentField[] Catalog =
	{
		new DocumentField("Счёт.Номер", DocumentFieldKind.Text, "номер счёта"),
		new DocumentField("Счёт.Дата", DocumentFieldKind.Text, "дата счёта"),
		new DocumentField("Агентство.Реквизиты", DocumentFieldKind.Text, "многострочный текст"),
		new DocumentField("Агентство.Подпись", DocumentFieldKind.Image, "подпись и печать"),
		new DocumentField("СНДС", DocumentFieldKind.Flag, "облагается НДС"),
		new DocumentField("Ставка", DocumentFieldKind.Text, "ставка НДС"),
		new DocumentField("Строки", DocumentFieldKind.List, "строки счёта", new[]
		{
			new DocumentField("Наименование", DocumentFieldKind.Text, ""),
			new DocumentField("Цена", DocumentFieldKind.Text, ""),
			new DocumentField("Спонсорская", DocumentFieldKind.Flag, "")
		}),
	};

	private static DocumentData Data(bool withTax = true, int rows = 3)
	{
		var data = new DocumentData()
			.Set("Счёт.Номер", "17")
			.Set("Счёт.Дата", "01.02.2026")
			.Set("Агентство.Реквизиты", "ИНН 123\nКПП 456")
			.SetImage("Агентство.Подпись", TestImage())
			.SetFlag("СНДС", withTax)
			.Set("Ставка", "5")
			.SetEmptyList("Строки");
		for (int i = 1; i <= rows; i++)
			data.AddItem("Строки").Set("Наименование", "Эфир " + i).Set("Цена", (i * 100).ToString())
				.SetFlag("Спонсорская", i == 2);
		return data;
	}

	private static void RenderCases()
	{
		// Метка разрезана на три фрагмента, первый фрагмент жирный.
		Check("метка из кусков", Render("pieces",
			P(R("Договор № "), R("{{Счё", bold: true), R("т.Ном"), R("ер}}"), R(" от "), R("{{Счёт.Дата}}"))),
			body => Text(body) == "Договор № 17 от 01.02.2026"
				&& body.Descendants<Run>().Any(r => r.InnerText == "17" && r.RunProperties?.Bold != null));

		Check("две метки в одном фрагменте, регистр и пробелы",
			Render("two", P(R("{{счёт.номер}}-{{ Счёт.Дата }}"))),
			body => Text(body) == "17-01.02.2026");

		Check("перевод строки в значении",
			Render("lines", P(R("{{Агентство.Реквизиты}}"))),
			body => Text(body) == "ИНН 123КПП 456" && body.Descendants<Break>().Count() == 1);

		string inline = "Цена {{#СНДС}}с НДС {{Ставка}}%{{/СНДС}}{{^СНДС}}без НДС{{/СНДС}}.";
		Check("условие внутри абзаца: да", Render("inline-yes", P(R(inline))), body => Text(body) == "Цена с НДС 5%.");
		Check("условие внутри абзаца: нет", Render("inline-no", Data(withTax: false), P(R(inline))),
			body => Text(body) == "Цена без НДС.");

		Check("блок абзацев убран", Render("block-no", Data(withTax: false),
				P(R("До")), P(R("{{#С"), R("НДС}}")), P(R("Пункт про НДС")), P(R("{{/СНДС}}")), P(R("После"))),
			body => Paragraphs(body) == "До|После");
		Check("блок абзацев оставлен", Render("block-yes",
				P(R("До")), P(R("{{#СНДС}}")), P(R("Пункт про НДС")), P(R("{{/СНДС}}")), P(R("После"))),
			body => Paragraphs(body) == "До|Пункт про НДС|После");

		Table RowsTable() => T(
			Row("Наименование", "Цена"),
			Row("{{#Строки}}{{Наименование}}", "{{Цена}}{{/Строки}}"),
			Row("Итого", "600"));
		Check("строка таблицы на каждый элемент", Render("rows", RowsTable()),
			body => Rows(body) == "Наименование;Цена|Эфир 1;100|Эфир 2;200|Эфир 3;300|Итого;600");
		Check("пустой список — строк нет", Render("rows-empty", Data(rows: 0), RowsTable()),
			body => Rows(body) == "Наименование;Цена|Итого;600");

		Check("вложенное условие по полю элемента", Render("nested",
				P(R("{{#Строки}}")),
				P(R("{{Наименование}}{{#Спонсорская}} (спонсорская){{/Спонсорская}}, НДС {{Ставка}}%")),
				P(R("{{/Строки}}"))),
			body => Paragraphs(body) == "Эфир 1, НДС 5%|Эфир 2 (спонсорская), НДС 5%|Эфир 3, НДС 5%");

		Check("картинка", Render("image", P(R("Подпись: "), R("{{Агентство.Подпись}}"))),
			body => body.Descendants<Drawing>().Count() == 1 && Text(body) == "Подпись: ");

		Check("разрыв раздела в абзаце-метке сохраняется", Render("sectpr",
				P(R("{{#СНДС}}")), P(R("Текст")),
				new Paragraph(new ParagraphProperties(new SectionProperties()), new Run(new Text("{{/СНДС}}")))),
			body => body.Descendants<SectionProperties>().Count() == 1 && Text(body) == "Текст");

		// Колонтитул.
		byte[] withFooter = Build(doc =>
		{
			doc.MainDocumentPart.Document.Body.Append(P(R("Тело")));
			FooterPart footer = doc.MainDocumentPart.AddNewPart<FooterPart>();
			footer.Footer = new Footer(P(R("Счёт {{Счёт"), R(".Номер}}")));
		});
		byte[] footerResult = DocxTemplate.Render(withFooter, Data());
		using (var doc = WordprocessingDocument.Open(new MemoryStream(footerResult), false))
			Check("колонтитул", doc.MainDocumentPart.FooterParts.Single().Footer.InnerText == "Счёт 17");
		Save("footer", footerResult);
	}

	private static void ValidateCases()
	{
		Errors("годный шаблон", 0, P(R("{{Счёт.Номер}} {{#СНДС}}{{Ставка}}{{/СНДС}}")),
			T(Row("{{#Строки}}{{Наименование}}", "{{Цена}}{{/Строки}}")));
		Errors("неизвестное поле", 1, P(R("{{Счёт.Номр}}")));
		Errors("е вместо ё", 0, P(R("{{Счет.Номер}}")));
		Errors("поле элемента вне списка", 1, P(R("{{Наименование}}")));
		Errors("блок не закрыт", 1, P(R("{{#СНДС}} текст")));
		Errors("лишнее закрытие", 1, P(R("текст {{/СНДС}}")));
		Errors("условие как значение", 1, P(R("{{СНДС}}")));
		Errors("метки блока не отдельными абзацами", 1, P(R("Начало {{#СНДС}}")), P(R("{{/СНДС}}")));
		Errors("испорченная метка", 1, P(R("ИНН {{Счёт.Номер}")));

		IList<string> garbage = DocxTemplate.Validate(new byte[] { 1, 2, 3 }, Catalog);
		Check("не docx", garbage.Count == 1, string.Join(" / ", garbage));
	}

	#region Построение шаблонов

	private static Run R(string text, bool bold = false)
	{
		var run = new Run();
		if (bold)
			run.Append(new RunProperties(new Bold()));
		run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
		return run;
	}

	private static Paragraph P(params Run[] runs) => new Paragraph(runs);

	private static TableRow Row(params string[] cells) =>
		new TableRow(cells.Select(c => new TableCell(P(R(c)))));

	private static Table T(params TableRow[] rows)
	{
		var table = new Table(new TableProperties(), new TableGrid(
			Enumerable.Range(0, rows[0].Elements<TableCell>().Count()).Select(_ => new GridColumn { Width = "2000" })));
		table.Append(rows);
		return table;
	}

	private static byte[] Build(Action<WordprocessingDocument> fill)
	{
		using (var stream = new MemoryStream())
		{
			using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
			{
				doc.AddMainDocumentPart().Document = new Document(new Body());
				fill(doc);
			}
			return stream.ToArray();
		}
	}

	private static byte[] Template(OpenXmlElement[] content) =>
		Build(doc => doc.MainDocumentPart.Document.Body.Append(content.Select(c => c.CloneNode(true))));

	private static DocumentImage TestImage()
	{
		using (var bitmap = new Bitmap(120, 60))
		using (var stream = new MemoryStream())
		{
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.Clear(System.Drawing.Color.White);
				g.DrawEllipse(Pens.Blue, 5, 5, 110, 50);
			}
			bitmap.Save(stream, ImageFormat.Png);
			return DocumentImage.FromBytes(stream.ToArray());
		}
	}

	#endregion

	#region Проверки

	private static Body Render(string name, params OpenXmlElement[] content) => Render(name, Data(), content);

	private static Body Render(string name, DocumentData data, params OpenXmlElement[] content)
	{
		try
		{
			byte[] result = DocxTemplate.Render(Template(content), data);
			Save(name, result);
			using (var doc = WordprocessingDocument.Open(new MemoryStream(result), false))
			{
				List<ValidationErrorInfo> invalid = new OpenXmlValidator().Validate(doc).ToList();
				if (invalid.Count > 0)
				{
					failed++;
					Console.WriteLine($"FAIL {name}: файл невалиден: {invalid[0].Description} ({invalid[0].Path?.XPath})");
				}
				return (Body)doc.MainDocumentPart.Document.Body.CloneNode(true);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine($"FAIL {name}: {e.GetType().Name}: {e.Message}");
			failed++;
			return new Body();
		}
	}

	private static void Check(string name, Body body, Func<Body, bool> condition)
	{
		Check(name, condition(body), $"текст «{Paragraphs(body)}», строки «{Rows(body)}»");
	}

	private static void Check(string name, bool ok, string details = null)
	{
		Console.WriteLine((ok ? "ok   " : "FAIL ") + name + (ok || details == null ? "" : ": " + details));
		if (!ok)
			failed++;
	}

	private static void Errors(string name, int expected, params OpenXmlElement[] content)
	{
		IList<string> errors = DocxTemplate.Validate(Template(content), Catalog);
		Check("проверка: " + name, errors.Count == expected, string.Join(" / ", errors));
		foreach (string error in errors)
			Console.WriteLine("       " + error);
	}

	private static void Save(string name, byte[] content)
	{
		if (outDir != null)
			File.WriteAllBytes(Path.Combine(outDir, name + ".docx"), content);
	}

	private static string Text(Body body) => string.Concat(body.Descendants<Text>().Select(t => t.Text));

	private static string Paragraphs(Body body) =>
		string.Join("|", body.Elements<Paragraph>().Select(p => p.InnerText));

	private static string Rows(Body body) =>
		string.Join("|", body.Descendants<TableRow>().Select(r => string.Join(";", r.Elements<TableCell>().Select(c => c.InnerText))));

	#endregion
}
