using System;
using System.Globalization;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	/// <summary>
	/// Сетка вещания в Word (docx) — замена выгрузки Crystal-макета Grid.rpt в .doc
	/// (десктоп: ExportGridForm, вторая папка). Тот же лист, что экран «Сетка вещания»:
	/// A4 книжная; шапка (заголовок, дата, станция, менеджер) и подвал (заполняемость,
	/// фактическое время рекламы) повторяются на каждой странице, как «шапка/подвал страницы»
	/// у Crystal; окна — в три колонки раздела, текст перетекает сам. Под окном жирно его
	/// длительность и сумма роликов, снизу черта.
	/// </summary>
	public static class BroadcastGridDocx
	{
		// Размеры — в двадцатых долях пункта (twip): A4 = 11906 × 16838.
		private const int PageWidth = 11906;
		private const int PageHeight = 16838;
		private const int MarginSide = 850;
		private const int ColumnGap = 340;
		private const int Columns = 3;
		private const int ColumnWidth = (PageWidth - 2 * MarginSide - (Columns - 1) * ColumnGap) / Columns;

		// Колонки окна (таблица): время, ролик (переносится в своей колонке, как у Crystal),
		// длительность — ширина под «00:00:00» шрифтом Verdana 8.
		private const int TimeWidth = 620;
		private const int DurationWidth = 820;
		private const int NameWidth = ColumnWidth - TimeWidth - DurationWidth;

		public static byte[] Build(BroadcastGrid grid, string station, DateTime date, string manager)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (WordprocessingDocument document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
				{
					MainDocumentPart main = document.AddMainDocumentPart();
					AddStyles(main);

					HeaderPart header = main.AddNewPart<HeaderPart>();
					header.Header = BuildHeader(station, date, manager);
					FooterPart footer = main.AddNewPart<FooterPart>();
					footer.Footer = BuildFooter(grid);

					Body body = new Body();
					if (grid.Windows.Count == 0)
						body.Append(Line(Tr.T("На этот день у станции нет рекламных окон."), false));

					foreach (BroadcastGrid.Window window in grid.Windows)
						AppendWindow(body, window);

					body.Append(SectionProperties(main.GetIdOfPart(header), main.GetIdOfPart(footer)));
					main.Document = new Document(body);
				}
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Окно — таблица на ширину колонки: строки выпусков и итог (жирно: длительность окна
		/// слева, сумма роликов справа, черта снизу). Строки не рвутся, а KeepNext держит окно
		/// целиком в колонке, если оно помещается.
		/// </summary>
		private static void AppendWindow(Body body, BroadcastGrid.Window window)
		{
			Table table = new Table(new TableProperties(
				new TableWidth { Width = ColumnWidth.ToString(CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa },
				new TableLayout { Type = TableLayoutValues.Fixed },
				new TableCellMarginDefault(
					new TopMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
					new TableCellLeftMargin { Width = 0, Type = TableWidthValues.Dxa },
					new BottomMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
					new TableCellRightMargin { Width = 0, Type = TableWidthValues.Dxa })),
				new TableGrid(
					new GridColumn { Width = TimeWidth.ToString(CultureInfo.InvariantCulture) },
					new GridColumn { Width = NameWidth.ToString(CultureInfo.InvariantCulture) },
					new GridColumn { Width = DurationWidth.ToString(CultureInfo.InvariantCulture) }));

			if (window.Rows.Count == 0)
				table.Append(Row(window.Time, string.Empty, string.Empty, false, false));
			for (int i = 0; i < window.Rows.Count; i++)
				table.Append(Row(i == 0 ? window.Time : string.Empty, window.Rows[i].Description, window.Rows[i].Duration, false, false));
			table.Append(Row(window.WindowDuration ?? string.Empty, string.Empty, window.AdsDuration ?? string.Empty, true, true));

			body.Append(table);
			// Таблицы подряд Word склеил бы в одну — между окнами пустой абзац высотой в зазор.
			body.Append(new Paragraph(new ParagraphProperties(
				new SpacingBetweenLines { Line = "80", LineRule = LineSpacingRuleValues.Exact })));
		}

		private static TableRow Row(string time, string name, string duration, bool total, bool last)
		{
			TableRow row = new TableRow(new TableRowProperties(new CantSplit()));
			row.Append(Cell(TimeWidth, time, total, false, last));
			row.Append(Cell(NameWidth, name, false, false, last));
			row.Append(Cell(DurationWidth, duration, total, true, last));
			return row;
		}

		private static TableCell Cell(int width, string text, bool bold, bool right, bool underline)
		{
			TableCellProperties properties = new TableCellProperties(
				new TableCellWidth { Width = width.ToString(CultureInfo.InvariantCulture), Type = TableWidthUnitValues.Dxa });
			if (underline)
				properties.Append(new TableCellBorders(new BottomBorder { Val = BorderValues.Single, Size = 8, Space = 0, Color = "000000" }));

			ParagraphProperties paragraph = new ParagraphProperties();
			if (!underline)
				paragraph.Append(new KeepNext());
			if (right)
				paragraph.Append(new Justification { Val = JustificationValues.Right });
			return new TableCell(properties, new Paragraph(paragraph, TextRun(text, bold)));
		}

		private static Header BuildHeader(string station, DateTime date, string manager)
		{
			Paragraph title = new Paragraph(
				new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { After = "160" }));
			Run titleRun = TextRun(Tr.T("Сетка вещания"), true);
			titleRun.RunProperties.Append(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman", ComplexScript = "Times New Roman" });
			titleRun.RunProperties.Append(new FontSize { Val = "28" });
			title.Append(titleRun);

			Header header = new Header(title,
				HeadLine(Tr.T("Дата:"), string.Format("{0} ({1})", date.ToString("d"), CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(date.DayOfWeek))),
				HeadLine(Tr.T("Радиостанция:"), station));
			if (manager != null)
				header.Append(HeadLine(Tr.T("Менеджер:"), manager));
			header.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "0" })));
			return header;
		}

		private static Footer BuildFooter(BroadcastGrid grid)
		{
			return new Footer(
				HeadLine(Tr.T("Заполняемость:"), grid.Fill == null ? "—" : grid.Fill + " %"),
				HeadLine(Tr.T("Фактическое время рекламы:"), grid.RealTime ?? "—"));
		}

		/// <summary>Строка шапки/подвала: подпись и значение с отступом, как в макете Crystal.</summary>
		private static Paragraph HeadLine(string caption, string value)
		{
			Paragraph line = new Paragraph(
				new ParagraphProperties(
					new Tabs(new TabStop { Val = TabStopValues.Left, Position = 4320 }),
					new Indentation { Left = "567" }));
			Run captionRun = TextRun(caption, false);
			captionRun.RunProperties.Append(new FontSize { Val = "20" });
			line.Append(captionRun);
			line.Append(new Run(new TabChar()));
			Run valueRun = TextRun(value ?? string.Empty, false);
			valueRun.RunProperties.Append(new FontSize { Val = "20" });
			line.Append(valueRun);
			return line;
		}

		private static Paragraph Line(string text, bool bold)
		{
			return new Paragraph(TextRun(text, bold));
		}

		private static Run TextRun(string text, bool bold)
		{
			RunProperties properties = new RunProperties();
			if (bold)
				properties.Append(new Bold());
			return new Run(properties, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
		}

		private static SectionProperties SectionProperties(string headerId, string footerId)
		{
			return new SectionProperties(
				new HeaderReference { Type = HeaderFooterValues.Default, Id = headerId },
				new FooterReference { Type = HeaderFooterValues.Default, Id = footerId },
				new PageSize { Width = PageWidth, Height = PageHeight },
				new PageMargin
				{
					Top = 2300, Bottom = 1300, Left = MarginSide, Right = MarginSide,
					Header = 567, Footer = 567, Gutter = 0
				},
				new Columns { ColumnCount = Columns, Space = ColumnGap.ToString(CultureInfo.InvariantCulture), EqualWidth = true });
		}

		/// <summary>Шрифт документа — Verdana 8 pt, как строки макета Crystal; без интервалов между абзацами.</summary>
		private static void AddStyles(MainDocumentPart main)
		{
			StyleDefinitionsPart part = main.AddNewPart<StyleDefinitionsPart>();
			part.Styles = new Styles(
				new DocDefaults(
					new RunPropertiesDefault(new RunPropertiesBaseStyle(
						new RunFonts { Ascii = "Verdana", HighAnsi = "Verdana", ComplexScript = "Verdana", EastAsia = "Verdana" },
						new FontSize { Val = "16" },
						new Languages { Val = "ru-RU" })),
					new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(
						new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }))));
		}
	}
}
