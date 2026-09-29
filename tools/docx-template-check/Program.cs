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
		bool db = args.Contains("--db");
		outDir = args.FirstOrDefault(a => a != "--db");
		if (outDir != null)
			Directory.CreateDirectory(outDir);

		RenderCases();
		ValidateCases();
		if (db)
		{
			StoreCases();
			DocumentCases();
		}

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

	private sealed class TestUserStorage : FogSoft.WinForm.Classes.SecurityManager.ILoggedUserStorage
	{
		public FogSoft.WinForm.Classes.SecurityManager.User User { get; set; }
	}

	// Хранилище на ArtvisDev: версии по дате, действующая сегодня, удаление. Свои строки за собой убирает.
	private static void StoreCases()
	{
		var storage = new TestUserStorage();
		FogSoft.WinForm.Classes.SecurityManager.SetLoggedUserStorage(storage);
		storage.User = FogSoft.WinForm.Classes.SecurityManager.GetUser(3);
		const int agencyId = 135;
		var kind = DocumentKind.Contract;
		byte[] v1 = Template(new OpenXmlElement[] { P(R("версия 1")) });
		byte[] v2 = Template(new OpenXmlElement[] { P(R("версия 2")) });

		var ids = new List<int>();
		try
		{
			ids.Add(DocumentTemplateStore.Add(agencyId, kind, new DateTime(2026, 1, 1), v1, "Договор 2026.docx", " первая "));
			ids.Add(DocumentTemplateStore.Add(agencyId, kind, new DateTime(2026, 7, 1), v2, "Договор июль.docx", null));
			ids.Add(DocumentTemplateStore.Add(agencyId, kind, new DateTime(2026, 7, 1), v2, "Договор июль исправл.docx", ""));

			Check("БД: до первой версии шаблона нет", DocumentTemplateStore.ForDate(agencyId, kind, new DateTime(2025, 12, 31)) == null);
			DocumentTemplateFile march = DocumentTemplateStore.ForDate(agencyId, kind, new DateTime(2026, 3, 15, 14, 0, 0));
			Check("БД: на 15.03 действует версия от 01.01", march?.Id == ids[0] && march.Content.SequenceEqual(v1));
			Check("БД: на 01.07 — загруженная последней из двух", DocumentTemplateStore.ForDate(agencyId, kind, new DateTime(2026, 7, 1))?.Id == ids[2]);
			Check("БД: другой вид документа пуст", DocumentTemplateStore.ForDate(agencyId, DocumentKind.Bill, new DateTime(2026, 7, 1)) == null);

			List<DocumentTemplateVersion> list = DocumentTemplateStore.List(agencyId, kind);
			Check("БД: список версий", list.Select(v => v.Id).SequenceEqual(new[] { ids[2], ids[1], ids[0] }),
				string.Join(",", list.Select(v => v.Id)));
			Check("БД: сегодня действует последняя", list.Where(v => v.IsCurrent).Select(v => v.Id).SequenceEqual(new[] { ids[2] }));
			DocumentTemplateVersion first = list.Single(v => v.Id == ids[0]);
			Check("БД: поля версии", first.Comment == "первая" && list.Single(v => v.Id == ids[1]).Comment == null
				&& first.Size == v1.Length && !string.IsNullOrEmpty(first.CreatedByName) && first.KindName.Length > 0,
				$"{first.Comment}|{first.Size}|{first.CreatedByName}|{first.KindName}");
			Check("БД: файл по ID", DocumentTemplateStore.Load(ids[1])?.FileName == "Договор июль.docx");
		}
		finally
		{
			foreach (int id in ids)
				DocumentTemplateStore.Delete(id);
		}
		Check("БД: удалено", DocumentTemplateStore.List(agencyId, kind).Count == 0);
	}

	// Сборка значений на ArtvisDev: шаблон «все поля каталога» должен проходить проверку и
	// заполняться без ошибок; суммы счёта сверяются с rpt_GenericBill (как печатает Crystal).
	private static void DocumentCases()
	{
		// Как веб при старте (DomainAssemblyResolver): метаданные ссылаются на сборки десктопа.
		AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
			new[] { "Merlin", "FogSoft.WinForm" }.Contains(new System.Reflection.AssemblyName(e.Name).Name)
				? typeof(FogSoft.WinForm.Classes.PresentationObject).Assembly
				: null;
		FogSoft.WinForm.DataAccess.DataAccessor.LoadProcedureConfig();
		var action = Merlin.Classes.ActionOnMassmedia.GetActionById(185313);
		IList<Merlin.Classes.Agency> agencies = ClientDocuments.AgenciesOf(action);
		Check("документы: агентство акции", agencies.Count == 1 && agencies[0].AgencyId == 201,
			string.Join(",", agencies.Select(a => a.AgencyId)));
		Merlin.Classes.Agency agency = agencies[0];
		var billDate = new DateTime(2026, 7, 28);

		DocumentData contract = ClientDocuments.Contract(action, null, agency, billDate, "411", true);
		Dump(DocumentKind.Contract, "contract", contract);

		DocumentData bill = ClientDocuments.Bill(action, agency, "411", billDate, null, true);
		Body billBody = Dump(DocumentKind.Bill, "bill", bill);
		System.Data.DataTable rpt = FogSoft.WinForm.DataAccess.DataAccessor.LoadDataSet("rpt_GenericBill",
			new Dictionary<string, object> { { "actionId", 185313 }, { "agencyId", 201 } }).Tables[0];
		decimal total = rpt.Rows.Cast<System.Data.DataRow>().Sum(r => (decimal)r["price"]);
		Check("документы: строк счёта как в rpt_GenericBill", billBody.Descendants<Paragraph>()
			.Count(p => p.InnerText.StartsWith("  Строки: ")) == rpt.Rows.Count);
		Check("документы: сумма счёта", Paragraphs(billBody).Contains("Сумма: " + Math.Round(total, 2).ToString("N2")));

		DocumentData byMonth = ClientDocuments.Bill(action, agency, "411", billDate, new DateTime(2026, 8, 1), false);
		Dump(DocumentKind.Bill, "bill-month", byMonth);

		Type campaignType = typeof(ClientDocuments).Assembly.GetType("Merlin.Classes.Campaign");
		var campaign = (FogSoft.WinForm.Classes.PresentationObject)campaignType.GetMethod("GetCampaignById").Invoke(null, new object[] { 408900 });
		var massmedia = (Merlin.Classes.Massmedia)typeof(Merlin.Classes.Massmedia).GetMethod("GetMassmediaByID",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { 235 });
		DocumentData onAir = ClientDocuments.OnAirInquire(campaign, agency, massmedia, new DateTime(2026, 8, 1), true, true);
		Body onAirBody = Dump(DocumentKind.OnAirInquire, "on-air", onAir);
		Check("документы: выходы эфирной справки", onAirBody.Descendants<Paragraph>().Count(p => p.InnerText.StartsWith("  Выходы: ")) == 431);
	}

	/// <summary>Шаблон из всех полей каталога вида: проверка, заполнение, печать значений.</summary>
	private static Body Dump(DocumentKind kind, string name, DocumentData data)
	{
		var content = new List<OpenXmlElement>();
		foreach (DocumentField field in DocumentFields.For(kind))
		{
			string tag = "{{" + field.Name + "}}";
			switch (field.Kind)
			{
				case DocumentFieldKind.Flag:
					content.Add(P(R(field.Name + ": "), R("{{#" + field.Name + "}}да{{/" + field.Name + "}}{{^" + field.Name + "}}нет{{/" + field.Name + "}}")));
					break;
				case DocumentFieldKind.List:
					content.Add(P(R("{{#" + field.Name + "}}")));
					content.Add(P(R("  " + field.Name + ": " + string.Join(" | ", field.ItemFields.Select(i => "{{" + i.Name + "}}")))));
					content.Add(P(R("{{/" + field.Name + "}}")));
					break;
				case DocumentFieldKind.Image:
					content.Add(P(R(field.Name + ": "), R("{{#" + field.Name + "}}картинка {{/" + field.Name + "}}"), R(tag)));
					break;
				default:
					content.Add(P(R(field.Name + ": " + tag)));
					break;
			}
		}
		byte[] template = Template(content.ToArray());
		IList<string> errors = DocxTemplate.Validate(template, DocumentFields.For(kind));
		Check($"документы: шаблон всех полей «{kind}» годен", errors.Count == 0, string.Join(" / ", errors));
		try
		{
			byte[] result = DocxTemplate.Render(template, data);
			Save("fields-" + name, result);
			using (var doc = WordprocessingDocument.Open(new MemoryStream(result), false))
			{
				var body = (Body)doc.MainDocumentPart.Document.Body.CloneNode(true);
				Check($"документы: «{name}» заполнен", true);
				int listLines = 0;
				foreach (Paragraph p in body.Elements<Paragraph>())
				{
					bool listLine = p.InnerText.StartsWith("  ");
					if (listLine && ++listLines > 3)
						continue;
					if (!listLine)
						listLines = 0;
					Console.WriteLine("       " + p.InnerText + (p.Descendants<Drawing>().Any() ? " [картинка]" : ""));
				}
				return body;
			}
		}
		catch (Exception e)
		{
			Check($"документы: «{name}» заполнен", false, e.Message);
			return new Body();
		}
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
