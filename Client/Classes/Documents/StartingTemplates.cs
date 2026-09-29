using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using F = Merlin.Classes.Documents.DocumentFields.Names;

namespace Merlin.Classes.Documents
{
	/// <summary>
	/// Начальные Word-шаблоны из нынешних текстов «Текст отчётов» (<c>ReportPartText</c>) и вёрстки
	/// Crystal-макетов (<c>Client/Reports/*.rpt</c>): текст документов у заказчика не меняется
	/// (docs/tasks/web-reports.md §6.0, §8 этап 4). Дальше шаблоны правит администратор в Word.
	/// </summary>
	public static class StartingTemplates
	{
		public static IDictionary<string, string> LoadReportParts()
		{
			DataTable table = DataAccessor.LoadDataSet("ReportPartTexts", DataAccessor.CreateParametersDictionary()).Tables[0];
			var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataRow row in table.Rows)
				parts[row["codeName"].ToString()] = row["reportText"].ToString();
			return parts;
		}

		/// <summary>
		/// Шаблон для печати: загруженный администратором (действующий на дату документа), а если
		/// у агентства его нет — начальный, собранный из «Текста отчётов» на лету. Так документы
		/// печатаются сразу после наката, тем же текстом, что у десктопа, без отдельного заполнения базы.
		/// </summary>
		public static byte[] ForPrint(int agencyId, DocumentKind kind, DateTime date)
		{
			DocumentTemplateFile stored = DocumentTemplateStore.ForDate(agencyId, kind, date);
			return stored != null ? stored.Content : Build(kind, LoadReportParts());
		}

		public static string FileName(DocumentKind kind)
		{
			switch (kind)
			{
				case DocumentKind.Bill: return Tr.T("Счёт") + ".docx";
				case DocumentKind.OnAirInquire: return Tr.T("Эфирная справка") + ".docx";
				case DocumentKind.BillContract: return Tr.T("Счёт-договор") + ".docx";
				case DocumentKind.Contract: return Tr.T("Договор") + ".docx";
				default: return Tr.T("Спонсорский договор") + ".docx";
			}
		}

		public static byte[] Build(DocumentKind kind, IDictionary<string, string> parts)
		{
			switch (kind)
			{
				case DocumentKind.Contract:
					return Contract(parts, "Contract", "ContractTitle", "ContractTitle2", "ContractHeader", "Contract.ContractSubject");
				case DocumentKind.SponsorContract:
					return Contract(parts, "SponsorContract", "SponsorContractTitle", "SponsorContractTitle2", "SponsorContractHeader",
						"SponsorContract.ContractSubject");
				case DocumentKind.BillContract:
					return BillContract(parts);
				case DocumentKind.Bill:
					return Bill(parts);
				default:
					return OnAirInquire(parts);
			}
		}

		#region Документы

		// Contract.rpt: Georgia 11, поля слева 2 см, остальные 1 см.
		private static byte[] Contract(IDictionary<string, string> parts, string prefix, string title, string titleWithoutAction,
			string header, string subject)
		{
			var doc = new DocBuilder("Georgia", 11, 1134, 567);
			doc.Add(Marker("#" + F.ByAction));
			doc.AddRange(Html(Part(parts, title)));
			doc.Add(Marker("/" + F.ByAction));
			doc.Add(Marker("^" + F.ByAction));
			doc.AddRange(Html(Part(parts, titleWithoutAction)));
			doc.Add(Marker("/" + F.ByAction));
			doc.Add(DatePlace(doc.Width));
			doc.AddRange(Html(Part(parts, header)));
			doc.AddRange(Html(TaxClause(Part(parts, subject))));
			doc.AddRange(Html(Part(parts, prefix + "LegalInfoTitle")));
			doc.Add(Requisites(doc.Width, parts, prefix));
			return doc.Save();
		}

		// BillContract.rpt: как договор, между частями — таблица счёта (Calibri 11).
		private static byte[] BillContract(IDictionary<string, string> parts)
		{
			var doc = new DocBuilder("Georgia", 11, 1134, 567);
			doc.AddRange(Html(Part(parts, "billContractTitle")));
			doc.Add(DatePlace(doc.Width));
			doc.AddRange(Html(Part(parts, "BillContractHeader")));
			doc.AddRange(Html(Part(parts, "billContract1")));
			doc.Add(BillTable(doc.Width, "Calibri", 11));
			doc.Add(P(null, R(Tr.T("Сумма прописью:") + " ", font: "Calibri"), R(Tag(F.TotalInWords), font: "Calibri")));
			doc.Add(NoTaxText(parts, "Calibri", 11, JustificationValues.Left));
			doc.AddRange(Html(Part(parts, "billContract2")));
			doc.AddRange(Html(Part(parts, "BillContractLegalInfoTitle")));
			doc.Add(Requisites(doc.Width, parts, "BillContract"));
			doc.Add(P(null, R(Tr.T("Контактное лицо:") + " " + Tag(F.ManagerContacts), bold: true, size: 10)));
			return doc.Save();
		}

		// GenericBill.rpt: Tahoma 9, реквизиты подписями слева, QR справа, таблица с рамками.
		private static byte[] Bill(IDictionary<string, string> parts)
		{
			var doc = new DocBuilder("Tahoma", 9, 1134, 850);
			doc.Add(P(JustificationValues.Center, R(
				Tr.T("Счёт №") + " " + Tag(F.Number) + " " + Tr.T("от") + " " + Tag(F.Date)
				+ Tag("#" + F.ForMonth) + " " + Tr.T("за месяц") + " " + Tag(F.Month) + " " + Tr.T("года") + Tag("/" + F.ForMonth)
				+ " " + Tr.T("к акции №") + " " + Tag(F.ActionNumber), bold: true, size: 12)));
			doc.Add(Empty());

			var rows = new List<string[]>
			{
				new[] { "!" + Tr.T("Исполнитель:"), Tag(F.AgencyName) },
				new[] { Tr.T("ИНН:"), Tag(F.AgencyInn) },
				new[] { Tr.T("КПП:"), Tag(F.AgencyKpp) },
				new[] { Tr.T("Адрес:"), Tag(F.AgencyAddress) },
				new[] { Tr.T("Телефон / Email:"), Tag(F.AgencyPhone) + ", " + Tag(F.AgencyEmail) },
				new[] { Tr.T("Р/сч.:"), Tag(F.AgencyAccount) },
				new[] { "!" + Tr.T("Банк:"), Tag(F.AgencyBank) },
				new[] { Tr.T("К/сч.:"), Tag(F.AgencyCorAccount) },
				new[] { Tr.T("БИК:"), Tag(F.AgencyBik) },
				new[] { "!" + Tr.T("Заказчик:"), Tag(F.FirmName) },
				new[] { Tr.T("ИНН:"), Tag(F.FirmInn) },
				new[] { Tr.T("Адрес:"), Tag(F.FirmAddress) }
			};
			int qrWidth = 1800;
			var table = PlainTable(doc.Width - qrWidth - 2300, 2300, qrWidth);
			for (int i = 0; i < rows.Count; i++)
			{
				string label = rows[i][0];
				bool bold = label.StartsWith("!");
				var qrCell = i == 0
					? Cell(qrWidth, P(JustificationValues.Right, R(Tag(F.Qr))))
					: Cell(qrWidth, new Paragraph());
				// QR — в правой колонке на высоту реквизитов агентства.
				if (i < 9)
					qrCell.TableCellProperties.Append(new VerticalMerge { Val = i == 0 ? MergedCellValues.Restart : MergedCellValues.Continue });
				table.Append(new TableRow(
					Cell(2300, P(null, R(bold ? label.Substring(1) : label, bold: bold))),
					Cell(doc.Width - qrWidth - 2300, P(null, R(rows[i][1], italic: true))),
					qrCell));
			}
			doc.Add(table);
			doc.Add(Empty());
			doc.Add(BillTable(doc.Width, null, 8));
			doc.Add(NoTaxText(parts, "Verdana", 8, JustificationValues.Right));
			doc.Add(Empty());
			doc.Add(P(null, R(Tr.T("Сумма прописью:") + " "), R(Tag(F.TotalInWords), italic: true)));
			doc.Add(Empty());
			doc.Add(P(null, R(Tag(F.AgencySignature))));
			doc.Add(SignLine(doc.Width, Tr.T("Руководитель:"), Tag(F.AgencyDirector)));
			doc.Add(Empty());
			doc.Add(P(null, R(Tr.T("Контактное лицо:") + " " + Tag(F.ManagerContacts))));
			return doc.Save();
		}

		// OnAirInquire.rpt: Tahoma 9, шапка подписями слева, выходы таблицей без рамок.
		private static byte[] OnAirInquire(IDictionary<string, string> parts)
		{
			var doc = new DocBuilder("Tahoma", 9, 1134, 850);
			doc.Add(P(JustificationValues.Center, R(Tr.T("Эфирная справка для акции №") + Tag(F.ActionNumber), bold: true, size: 10, font: "Verdana")));
			doc.Add(Empty());

			var head = PlainTable(2700, doc.Width - 2700);
			foreach (string[] row in new[]
			{
				new[] { Tr.T("Заказчик:"), Tag(F.FirmName) },
				new[] { Tr.T("ИНН:"), Tag(F.FirmInn) },
				new[] { Tr.T("Адрес:"), Tag(F.FirmAddress) },
				new[] { Tr.T("Лицензиар:"), Tag(F.StationFounder) },
				new[] { Tr.T("СМИ:"), Tag(F.StationName) },
				new[] { Tr.T("Радиостанция:"), Tag(F.StationRadio) },
				new[] { Tr.T("Территория распространения:"), Tag(F.StationGroup) }
			})
				head.Append(new TableRow(Cell(2700, P(null, R(row[0]))), Cell(doc.Width - 2700, P(null, R(row[1], bold: true, italic: true)))));
			doc.Add(head);
			doc.Add(Empty());
			doc.AddRange(Html(Part(parts, "efir1")));
			doc.Add(Empty());

			int[] widths = { 1150, 1000, 1100, doc.Width - 3250 };
			var issues = PlainTable(widths);
			issues.Append(new TableRow(
				Cell(widths[0], P(null, R(Tr.T("Дата"), bold: true))),
				Cell(widths[1], P(null, R(Tr.T("Время"), bold: true))),
				Cell(widths[2], P(null, R(Tr.T("Прод-ть"), bold: true))),
				Cell(widths[3], P(null, R(Tr.T("Название ролика или программы"), bold: true)))));
			issues.Append(new TableRow(SpanCell(doc.Width, 4, P(null, R(Tr.T("Рекламные выпуски"), bold: true, italic: true)))));
			issues.Append(IssueRow(widths, F.Issues));
			issues.Append(new TableRow(SpanCell(doc.Width, 4, P(null, R(Tr.T("Всего выходов:") + " "), R(Tag(F.IssueCount), bold: true)))));
			issues.Append(new TableRow(
				SpanCell(widths[0] + widths[1] + widths[2], 3, P(null, R(Tag("#" + F.HasSponsorIssues) + Tr.T("Спонсорские выпуски"), bold: true, italic: true))),
				Cell(widths[3], P(null, R(Tag("/" + F.HasSponsorIssues))))));
			issues.Append(IssueRow(widths, F.SponsorIssues));
			doc.Add(issues);
			doc.Add(Empty());

			doc.Add(Marker("#" + F.WithPrice));
			doc.Add(P(null, R(Tr.T("Сумма:") + " " + Tag(F.Total) + " " + Tr.T("руб.") + " (" + Tag(F.TotalInWords) + ")")));
			doc.Add(P(null, R(Tag("#" + F.WithTax) + Tr.T("В том числе НДС") + " (" + Tag(F.TaxRate) + "%) - " + Tag(F.TaxSum) + " " + Tr.T("руб.") + Tag("/" + F.WithTax), italic: true, size: 8)));
			doc.Add(Marker("/" + F.WithPrice));
			doc.Add(Empty());
			doc.Add(P(null, R(Tr.T("Справка выдана:") + " " + Tag(F.StationCertificate))));
			doc.Add(P(null, R(Tag(F.StationSignature))));
			doc.Add(SignLine(doc.Width, Tr.T("Руководитель:"), Tag(F.StationDirector)));
			return doc.Save();
		}

		private static TableRow IssueRow(int[] widths, string list)
		{
			return new TableRow(
				Cell(widths[0], P(null, R(Tag("#" + list) + Tag(F.IssueDate), size: 10))),
				Cell(widths[1], P(null, R(Tag(F.IssueTime), size: 10))),
				Cell(widths[2], P(null, R(Tag(F.IssueDuration), size: 10))),
				Cell(widths[3], P(null, R(Tag(F.IssueName) + Tag("/" + list), size: 10))));
		}

		/// <summary>Таблица счёта: предмет, сумма без НДС, НДС, сумма с НДС; строки по списку, итог.</summary>
		private static Table BillTable(int width, string font, int size)
		{
			int[] widths = { width - 4500, 1500, 1400, 1600 };
			var table = BorderedTable(widths);
			table.Append(new TableRow(
				Cell(widths[0], P(JustificationValues.Center, R(Tr.T("Предмет счёта"), bold: true, font: font, size: size))),
				Cell(widths[1], P(JustificationValues.Center, R(Tr.T("Сумма"), bold: true, font: font, size: size))),
				Cell(widths[2], P(JustificationValues.Center, R(Tr.T("НДС") + Tag("#" + F.WithTax) + " (" + Tag(F.TaxRate) + "%)" + Tag("/" + F.WithTax), bold: true, font: font, size: size))),
				Cell(widths[3], P(JustificationValues.Center, R(Tr.T("Сумма с НДС"), bold: true, font: font, size: size)))));
			table.Append(new TableRow(
				Cell(widths[0], P(null, R(Tag("#" + F.Rows) + Tag(F.RowName), font: font, size: size))),
				Cell(widths[1], P(JustificationValues.Right, R(Tag(F.RowSumWithoutTax), font: font, size: size))),
				Cell(widths[2], P(JustificationValues.Right, R(Tag(F.RowTax), font: font, size: size))),
				Cell(widths[3], P(JustificationValues.Right, R(Tag(F.RowSum) + Tag("/" + F.Rows), font: font, size: size)))));
			table.Append(new TableRow(
				Cell(widths[0], P(JustificationValues.Right, R(Tr.T("Итого:"), bold: true, font: font, size: size))),
				Cell(widths[1], P(JustificationValues.Right, R(Tag(F.TotalWithoutTax), bold: true, font: font, size: size))),
				Cell(widths[2], P(JustificationValues.Right, R(Tag(F.TaxSum), bold: true, font: font, size: size))),
				Cell(widths[3], P(JustificationValues.Right, R(Tag(F.Total), bold: true, font: font, size: size)))));
			return table;
		}

		/// <summary>
		/// «НДС не облагается…» (блок <c>NoNDSText</c>). Десктоп печатал его в каждом счёте, даже
		/// с колонкой «НДС (5%)»; здесь — только когда НДС нет (§8.5).
		/// </summary>
		private static Paragraph NoTaxText(IDictionary<string, string> parts, string font, int size, JustificationValues align)
		{
			return P(align, R(Tag("^" + F.WithTax) + Plain(Part(parts, "NoNDSText")) + Tag("/" + F.WithTax), font: font, size: size));
		}

		/// <summary>Дата слева, город справа (<c>txtContractDate</c> / <c>txtAgencyContractPlace</c>).</summary>
		private static Table DatePlace(int width)
		{
			var table = PlainTable(width / 2, width - width / 2);
			table.Append(new TableRow(
				Cell(width / 2, P(null, R(Tag(F.Date)))),
				Cell(width - width / 2, P(JustificationValues.Right, R(Tag(F.AgencyPlace))))));
			return table;
		}

		/// <summary>Реквизиты сторон в две колонки и места подписей; подпись агентства — картинкой.</summary>
		private static Table Requisites(int width, IDictionary<string, string> parts, string prefix)
		{
			int half = width / 2;
			var table = PlainTable(half, width - half);
			var left = Cell(half, Html(Part(parts, prefix + "FooterLeftPart")).ToArray());
			var right = Cell(width - half, Html(Part(parts, prefix + "FooterRightPart")).ToArray());
			table.Append(new TableRow(left, right));

			// Как в Crystal: с подписью место печати слева не печатается.
			var seal = new List<OpenXmlElement> { P(null, R(Tag(F.AgencySignature))), Marker("^" + F.AgencySignature) };
			seal.AddRange(Html(Part(parts, prefix + "SealPlaceLeft")));
			seal.Add(Marker("/" + F.AgencySignature));
			table.Append(new TableRow(
				Cell(half, seal.ToArray()),
				Cell(width - half, Html(Part(parts, prefix + "SealPlaceRight")).ToArray())));
			return table;
		}

		private static Table SignLine(int width, string label, string value)
		{
			var table = PlainTable(width / 2, width - width / 2);
			table.Append(new TableRow(
				Cell(width / 2, P(null, R(label))),
				Cell(width - width / 2, P(JustificationValues.Right, R(value, italic: true)))));
			return table;
		}

		#endregion

		#region Тексты «Текст отчётов» → Word

		/// <summary>@-метки десктопа (<c>GenericReport.GetTextPart</c>) → поля шаблона.</summary>
		private static readonly Dictionary<string, string> LegacyTokens = new Dictionary<string, string>
		{
			{ "agencyName", F.AgencyShortName }, { "agencyFullPrefix", F.AgencyFullForm },
			{ "agencyRegistration", F.AgencyRegistration }, { "agencyBossText", F.AgencyActingBy },
			{ "agencyAddress", F.AgencyAddress }, { "agencyINN", F.AgencyInn }, { "agencyKPP", F.AgencyKpp },
			{ "agencyAccount", F.AgencyAccount }, { "agencyPrefixWithName", F.AgencyName },
			{ "agencyOGRN", F.AgencyOgrn }, { "agencyDirector", F.AgencyDirector },
			{ "agencyBankName", F.AgencyBank }, { "agencyBankAccount", F.AgencyCorAccount }, { "agencyBankBIK", F.AgencyBik },
			{ "firmPrefixWithName", F.FirmName }, { "firmOGRN", F.FirmOgrn }, { "firmRegistration", F.FirmRegistration },
			{ "firmBossText", F.FirmActingBy }, { "firmDirector", F.FirmDirector }, { "firmAddress", F.FirmAddress },
			{ "firmINN", F.FirmInn }, { "firmKPP", F.FirmKpp }, { "firmAccount", F.FirmAccount },
			{ "firmBankName", F.FirmBank }, { "firmBankAccount", F.FirmCorAccount }, { "firmBankBIK", F.FirmBik },
			{ "actionID", F.ActionNumber }, { "billNo", F.Number }
		};

		private static string Part(IDictionary<string, string> parts, string codeName)
		{
			string text;
			if (!parts.TryGetValue(codeName, out text) || text == null)
				return string.Empty;
			text = Regex.Replace(text, @"@(\w+)", m =>
			{
				string field;
				return LegacyTokens.TryGetValue(m.Groups[1].Value, out field) ? Tag(field) : m.Value;
			});
			return text.Replace("{tax}", Tag(F.TaxRate));
		}

		/// <summary>
		/// Пункт о ставке НДС — условный: без ставки у агентства печатается «НДС не облагается»,
		/// остальной договор остаётся (вместо блока <c>…ContractSubjectNoNDS</c>, §6.2). Ставка,
		/// записанная в тексте числом (спонсорский договор: «по ставке 5%»), становится полем (§3.2.5).
		/// </summary>
		private static string TaxClause(string subject)
		{
			subject = Regex.Replace(subject, @"по\s+ставке\s+\d+([,.]\d+)?\s*%", "по ставке " + Tag(F.TaxRate) + "%");
			return Regex.Replace(subject, @"^(\s*\d+(?:\.\d+)*\.?\s*)(.*" + Regex.Escape(Tag(F.TaxRate)) + @".*?)\s*$",
				m => m.Groups[1].Value + Tag("#" + F.WithTax) + m.Groups[2].Value + Tag("/" + F.WithTax)
					+ Tag("^" + F.WithTax) + Tr.T("Услуги, оказываемые по данному договору, НДС не облагаются.") + Tag("/" + F.WithTax),
				RegexOptions.Multiline);
		}

		private static string Plain(string html)
		{
			return WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")).Replace("\r", " ").Replace("\n", " ").Trim();
		}

		/// <summary>
		/// Разметка блоков — то подмножество HTML, что понимал Crystal: абзацы, перенос строки,
		/// жирный, по центру. Каждая строка становится абзацем Word — юристу так удобнее править.
		/// </summary>
		private static IEnumerable<Paragraph> Html(string html)
		{
			var result = new List<Paragraph>();
			html = html.Replace("\r\n", "<br>").Replace("\r", "<br>").Replace("\n", "<br>");
			bool bold = false, paragraphCenter = false;
			int centerDepth = 0;
			var runs = new List<Run>();

			System.Action flush = () =>
			{
				result.Add(P(centerDepth > 0 || paragraphCenter ? JustificationValues.Center : (JustificationValues?)null, runs.ToArray()));
				runs.Clear();
			};

			foreach (Match token in Regex.Matches(html, "<[^>]*>|[^<]+"))
			{
				string value = token.Value;
				if (!value.StartsWith("<"))
				{
					runs.Add(R(WebUtility.HtmlDecode(value), bold: bold));
					continue;
				}
				string tag = Regex.Match(value, @"^</?\s*(\w+)").Groups[1].Value.ToLowerInvariant();
				bool closing = value.StartsWith("</");
				switch (tag)
				{
					case "b":
					case "strong":
						bold = !closing;
						break;
					case "br":
						flush();
						break;
					case "p":
						if (runs.Count > 0)
							flush();
						paragraphCenter = !closing && Regex.IsMatch(value, @"align\s*=\s*""?center", RegexOptions.IgnoreCase);
						break;
					case "center":
						if (runs.Count > 0)
							flush();
						centerDepth = Math.Max(0, centerDepth + (closing ? -1 : 1));
						break;
				}
			}
			if (runs.Count > 0)
				flush();
			// Хвостовые пустые абзацы от завершающих переводов строки не нужны.
			while (result.Count > 0 && string.IsNullOrWhiteSpace(result[result.Count - 1].InnerText))
				result.RemoveAt(result.Count - 1);
			return result;
		}

		#endregion

		#region Построение Word

		private static string Tag(string name)
		{
			return "{{" + name + "}}";
		}

		/// <summary>Абзац с одной меткой блока — сама метка удаляется при печати.</summary>
		private static Paragraph Marker(string tag)
		{
			return P(null, R(Tag(tag)));
		}

		private static Paragraph Empty()
		{
			return new Paragraph();
		}

		private static Paragraph P(JustificationValues? align, params Run[] runs)
		{
			var paragraph = new Paragraph();
			if (align.HasValue)
				paragraph.Append(new ParagraphProperties(new Justification { Val = align.Value }));
			paragraph.Append(runs);
			return paragraph;
		}

		private static Run R(string text, bool bold = false, bool italic = false, int? size = null, string font = null)
		{
			var properties = new RunProperties();
			if (font != null)
				properties.Append(new RunFonts { Ascii = font, HighAnsi = font, ComplexScript = font });
			if (bold)
				properties.Append(new Bold());
			if (italic)
				properties.Append(new Italic());
			if (size.HasValue)
				properties.Append(new FontSize { Val = (size.Value * 2).ToString() });
			var run = new Run();
			if (properties.HasChildren)
				run.Append(properties);
			run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
			return run;
		}

		private static Table PlainTable(params int[] widths)
		{
			return NewTable(widths, BorderValues.None);
		}

		private static Table BorderedTable(int[] widths)
		{
			return NewTable(widths, BorderValues.Single);
		}

		private static Table NewTable(int[] widths, BorderValues border)
		{
			return new Table(
				new TableProperties(
					new TableWidth { Width = widths.Sum().ToString(), Type = TableWidthUnitValues.Dxa },
					new TableBorders(
						Border(new TopBorder(), border), Border(new LeftBorder(), border),
						Border(new BottomBorder(), border), Border(new RightBorder(), border),
						Border(new InsideHorizontalBorder(), border), Border(new InsideVerticalBorder(), border)),
					new TableLayout { Type = TableLayoutValues.Fixed },
					new TableCellMarginDefault(
						new TopMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
						new TableCellLeftMargin { Width = 60, Type = TableWidthValues.Dxa },
						new BottomMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
						new TableCellRightMargin { Width = 60, Type = TableWidthValues.Dxa })),
				new TableGrid(widths.Select(w => new GridColumn { Width = w.ToString() })));
		}

		/// <summary>Цвет — только у настоящих рамок: docx-preview рисует «none» с цветом как линию.</summary>
		private static T Border<T>(T element, BorderValues border) where T : BorderType
		{
			element.Val = border;
			element.Size = 4;
			if (border != BorderValues.None)
				element.Color = "000000";
			return element;
		}

		private static TableCell Cell(int width, params OpenXmlElement[] content)
		{
			var cell = new TableCell(new TableCellProperties(new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa }));
			if (content.Length == 0)
				cell.Append(new Paragraph());
			else
				cell.Append(content);
			return cell;
		}

		private static TableCell SpanCell(int width, int span, params OpenXmlElement[] content)
		{
			TableCell cell = Cell(width, content);
			cell.TableCellProperties.Append(new GridSpan { Val = span });
			return cell;
		}

		private sealed class DocBuilder
		{
			private const int PageWidth = 11906;
			private const int PageHeight = 16838;
			private readonly List<OpenXmlElement> body = new List<OpenXmlElement>();
			private readonly string font;
			private readonly int size;
			private readonly int leftMargin;
			private readonly int rightMargin;

			public DocBuilder(string font, int size, int leftMargin, int rightMargin)
			{
				this.font = font;
				this.size = size;
				this.leftMargin = leftMargin;
				this.rightMargin = rightMargin;
			}

			public int Width
			{
				get { return PageWidth - leftMargin - rightMargin; }
			}

			public void Add(OpenXmlElement element)
			{
				body.Add(element);
			}

			public void AddRange(IEnumerable<OpenXmlElement> elements)
			{
				body.AddRange(elements);
			}

			public byte[] Save()
			{
				using (var stream = new MemoryStream())
				{
					using (WordprocessingDocument doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
					{
						MainDocumentPart main = doc.AddMainDocumentPart();
						StyleDefinitionsPart styles = main.AddNewPart<StyleDefinitionsPart>();
						styles.Styles = new Styles(new DocDefaults(
							new RunPropertiesDefault(new RunPropertiesBaseStyle(
								new RunFonts { Ascii = font, HighAnsi = font, ComplexScript = font, EastAsia = font },
								new FontSize { Val = (size * 2).ToString() },
								new FontSizeComplexScript { Val = (size * 2).ToString() },
								new Languages { Val = "ru-RU" })),
							new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
								new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }))));
						var documentBody = new Body();
						documentBody.Append(body);
						documentBody.Append(new SectionProperties(
							new PageSize { Width = PageWidth, Height = PageHeight },
							new PageMargin
							{
								Top = 567, Bottom = 567, Left = (uint)leftMargin, Right = (uint)rightMargin,
								Header = 284, Footer = 284, Gutter = 0
							}));
						main.Document = new Document(documentBody);
					}
					return stream.ToArray();
				}
			}
		}

		#endregion
	}
}
