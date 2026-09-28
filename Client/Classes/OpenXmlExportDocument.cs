using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using S = DocumentFormat.OpenXml.Spreadsheet;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;
using A = DocumentFormat.OpenXml.Drawing;

namespace Merlin.Classes
{
	/// <summary>
	/// Книга Excel в памяти без Excel: <see cref="IExportDocument"/> поверх OpenXml.
	/// Повторяет то, как пишет в лист Excel через COM (MSDocumentSheet), чтобы
	/// медиаплан выглядел так же: блок из одних строк получает текстовый формат,
	/// в смешанном блоке строки вроде «08:15» становятся временем; автоподбор
	/// ширины колонок и высоты строк с повёрнутым текстом считается по Tahoma.
	/// Работает и в десктопе, и в вебе (System.Drawing — только Windows).
	/// </summary>
	internal sealed class OpenXmlExportDocument : IExportDocument
	{
		private readonly List<OpenXmlDocumentSheet> _sheets = new List<OpenXmlDocumentSheet>();

		public bool HasSheets => _sheets.Count > 0;

		public IDocumentSheet GetNewSheet(string name, string fontName, int fontSize)
		{
			// Как MSExportDocument: пустое или занятое имя — лист остаётся «ЛистN».
			string sheetName = string.IsNullOrEmpty(name) || _sheets.Any(s => s.Name == name)
				? Tr.T("Лист") + (_sheets.Count + 1)
				: name;
			var sheet = new OpenXmlDocumentSheet(sheetName, fontName, fontSize);
			_sheets.Add(sheet);
			return sheet;
		}

		public void StartExport() { }
		public void FinishExport() { }
		public void OnAppQuit() { }
		public bool Visible() => false;

		public void SaveToDisk(string filePath) => File.WriteAllBytes(filePath, ToArray());

		public byte[] ToArray()
		{
			using (var ms = new MemoryStream())
			{
				using (SpreadsheetDocument doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
				{
					WorkbookPart wbPart = doc.AddWorkbookPart();
					// Без bookViews/sheetViews Excel масштабирует заданные высоты строк
					// (строка с подписью выходит в 2/3 высоты) — пишем как Excel.
					wbPart.Workbook = new S.Workbook(new S.BookViews(new S.WorkbookView()));
					var styles = new StyleTable();
					var strings = new SharedStrings();
					// Подпись агентства повторяется на каждом листе — в книге одна копия
					// картинки на все листы, как у Excel (иначе файл больше в разы).
					var images = new Dictionary<string, ImagePart>();
					var sheets = new S.Sheets();
					uint sheetId = 1;
					foreach (OpenXmlDocumentSheet sheet in _sheets)
					{
						WorksheetPart wsPart = wbPart.AddNewPart<WorksheetPart>();
						sheet.Write(wsPart, styles, strings, images);
						sheets.Append(new S.Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = sheetId++, Name = sheet.Name });
					}
					wbPart.Workbook.Append(sheets);
					wbPart.AddNewPart<WorkbookStylesPart>().Stylesheet = styles.Build();
					wbPart.AddNewPart<SharedStringTablePart>().SharedStringTable = strings.Build();
					wbPart.Workbook.Save();
				}
				return ms.ToArray();
			}
		}

		#region Стили и строки книги

		internal sealed class StyleTable
		{
			private readonly List<string> _fonts = new List<string>();
			private readonly List<string> _fills = new List<string>();
			private readonly List<string> _borders = new List<string>();
			private readonly List<string> _numFmts = new List<string>();
			private readonly List<string> _xfs = new List<string>();

			public StyleTable()
			{
				// «Обычный» — Calibri 11, как у Excel по умолчанию: от его ширины цифры
				// считаются ширины колонок (стандартная колонка — 64 пикселя).
				_fonts.Add("Calibri|11|0|0");
				_fills.Add("none");
				_fills.Add("gray125");
				_borders.Add("0000");
				_xfs.Add("0|0|0|0|0|0");
			}

			public uint GetStyle(XlsxCellFormat f)
			{
				int font = IndexOf(_fonts, $"{f.FontName}|{f.FontSize.ToString(CultureInfo.InvariantCulture)}|{(f.Bold ? 1 : 0)}|{(f.Italic ? 1 : 0)}");
				int fill = f.Fill == null ? 0 : IndexOf(_fills, f.Fill);
				int border = IndexOf(_borders, $"{B(f.Left)}{B(f.Right)}{B(f.Top)}{B(f.Bottom)}");
				int numFmt = NumFmtId(f.NumFmt);
				return (uint)IndexOf(_xfs, $"{numFmt}|{font}|{fill}|{border}|{f.Rotation}|{(f.Wrap ? 1 : 0)}");
			}

			private static char B(bool b) => b ? '1' : '0';

			private static int IndexOf(List<string> list, string key)
			{
				int i = list.IndexOf(key);
				if (i >= 0) return i;
				list.Add(key);
				return list.Count - 1;
			}

			private static readonly Dictionary<string, int> BuiltIn = new Dictionary<string, int>
			{
				{ "General", 0 }, { "0", 1 }, { "0.00", 2 }, { "@", 49 },
				{ "h:mm", 20 }, { "h:mm:ss", 21 }, { "dd.mm.yyyy", 14 },
			};

			private int NumFmtId(string code)
			{
				if (string.IsNullOrEmpty(code)) return 0;
				if (BuiltIn.TryGetValue(code, out int id)) return id;
				return 164 + IndexOf(_numFmts, code);
			}

			public S.Stylesheet Build()
			{
				var ss = new S.Stylesheet();
				if (_numFmts.Count > 0)
					ss.Append(new S.NumberingFormats(_numFmts.Select((c, i) =>
						new S.NumberingFormat { NumberFormatId = (uint)(164 + i), FormatCode = c })) { Count = (uint)_numFmts.Count });

				ss.Append(new S.Fonts(_fonts.Select(k =>
				{
					string[] p = k.Split('|');
					var font = new S.Font();
					if (p[2] == "1") font.Append(new S.Bold());
					if (p[3] == "1") font.Append(new S.Italic());
					font.Append(new S.FontSize { Val = double.Parse(p[1], CultureInfo.InvariantCulture) });
					font.Append(new S.FontName { Val = p[0] });
					font.Append(new S.FontCharSet { Val = 204 });
					return font;
				})) { Count = (uint)_fonts.Count });

				ss.Append(new S.Fills(_fills.Select(k =>
				{
					if (k == "none" || k == "gray125")
						return new S.Fill(new S.PatternFill { PatternType = k == "none" ? S.PatternValues.None : S.PatternValues.Gray125 });
					return new S.Fill(new S.PatternFill(
						new S.ForegroundColor { Rgb = "FF" + k },
						new S.BackgroundColor { Indexed = 64 }) { PatternType = S.PatternValues.Solid });
				})) { Count = (uint)_fills.Count });

				ss.Append(new S.Borders(_borders.Select(k => new S.Border(
					Side<S.LeftBorder>(k[0]), Side<S.RightBorder>(k[1]), Side<S.TopBorder>(k[2]),
					Side<S.BottomBorder>(k[3]), new S.DiagonalBorder()))) { Count = (uint)_borders.Count });

				ss.Append(new S.CellStyleFormats(new S.CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 }) { Count = 1 });

				ss.Append(new S.CellFormats(_xfs.Select(k =>
				{
					string[] p = k.Split('|');
					var xf = new S.CellFormat
					{
						NumberFormatId = uint.Parse(p[0]), FontId = uint.Parse(p[1]), FillId = uint.Parse(p[2]),
						BorderId = uint.Parse(p[3]), FormatId = 0,
					};
					if (p[0] != "0") xf.ApplyNumberFormat = true;
					if (p[1] != "0") xf.ApplyFont = true;
					if (p[2] != "0") xf.ApplyFill = true;
					if (p[3] != "0") xf.ApplyBorder = true;
					if (p[4] != "0" || p[5] != "0")
					{
						xf.ApplyAlignment = true;
						var al = new S.Alignment();
						if (p[4] != "0") al.TextRotation = uint.Parse(p[4]);
						if (p[5] != "0") al.WrapText = true;
						xf.Append(al);
					}
					return xf;
				})) { Count = (uint)_xfs.Count });

				ss.Append(new S.CellStyles(new S.CellStyle { Name = "Normal", FormatId = 0, BuiltinId = 0 }) { Count = 1 });
				return ss;
			}

			private static T Side<T>(char on) where T : S.BorderPropertiesType, new()
			{
				var side = new T();
				if (on == '1')
				{
					side.Style = S.BorderStyleValues.Thin;
					side.Append(new S.Color { Indexed = 64 });
				}
				return side;
			}
		}

		internal sealed class SharedStrings
		{
			private readonly Dictionary<string, int> _index = new Dictionary<string, int>();
			private readonly List<string> _list = new List<string>();

			public int Get(string s)
			{
				if (!_index.TryGetValue(s, out int i))
				{
					i = _list.Count;
					_list.Add(s);
					_index[s] = i;
				}
				return i;
			}

			public S.SharedStringTable Build() =>
				new S.SharedStringTable(_list.Select(s => new S.SharedStringItem(new S.Text(s) { Space = SpaceProcessingModeValues.Preserve })))
				{ Count = (uint)_list.Count, UniqueCount = (uint)_list.Count };
		}

		#endregion
	}

	/// <summary>Формат ячейки: шрифт, заливка, рамки, числовой формат, поворот.</summary>
	internal sealed class XlsxCellFormat
	{
		public string FontName;
		public double FontSize;
		public bool Bold, Italic;
		public string Fill;          // RRGGBB или null
		public bool Left, Right, Top, Bottom;
		public string NumFmt;        // код формата Excel или null = «Общий»
		public int Rotation;
		public bool Wrap;

		public XlsxCellFormat Clone() => (XlsxCellFormat)MemberwiseClone();
	}

	internal sealed class OpenXmlDocumentSheet : IDocumentSheet
	{
		private enum Kind { Empty, Text, Number, Boolean }

		private sealed class Cell
		{
			public Kind Kind;
			public string Text;
			public double Number;
			public XlsxCellFormat Format;
		}

		private sealed class Image
		{
			public int Row, Col;
			public byte[] Png;
			public long WidthEmu, HeightEmu;
		}

		// Стандартная ширина колонки Excel в символах (Calibri 11 — 64 пикселя);
		// в файл не пишется — у колонок без заданной ширины Excel берёт свою.
		private const double DefaultColumnWidth = 8.43;
		private const double MaxDigitWidth = 7;
		// Высота строки по умолчанию для Tahoma 8 (как у листа из COM).
		private const double DefaultRowHeight = 10;

		private readonly Dictionary<(int row, int col), Cell> _cells = new Dictionary<(int, int), Cell>();
		private readonly Dictionary<int, double> _columnWidths = new Dictionary<int, double>();
		private readonly Dictionary<int, double> _rowHeights = new Dictionary<int, double>();
		private readonly List<Image> _images = new List<Image>();
		private readonly XlsxCellFormat _baseFormat;
		private bool _landscape;

		public string Name { get; }

		public OpenXmlDocumentSheet(string name, string fontName, int fontSize)
		{
			Name = name;
			_baseFormat = new XlsxCellFormat { FontName = fontName, FontSize = fontSize };
		}

		#region IDocumentSheet

		public void SetValuesForRange(int top, int left, int bottom, int right, object[,] data)
		{
			int rows = data.GetLength(0);
			int cols = data.GetLength(1);

			// Как MSDocumentSheet: блок из одних строк сначала получает текстовый
			// формат «@», поэтому Excel не превращает в нём «02:29» во время.
			bool allText = true;
			for (int r = 0; r < rows && allText; r++)
				for (int c = 0; c < cols && allText; c++)
					if (data[r, c] != null && !(data[r, c] is string) && !Convert.IsDBNull(data[r, c]))
						allText = false;

			var monthYear = new List<(int r, int c, DateTime date)>();
			for (int r = 0; r < rows; r++)
				for (int c = 0; c < cols; c++)
					if (data[r, c] is string s && TryParseMonthYear(s, out DateTime my))
						monthYear.Add((r, c, my));

			bool textRange = allText && monthYear.Count == 0;
			for (int r = top; r <= bottom; r++)
				for (int c = left; c <= right; c++)
				{
					Cell cell = GetCell(r, c);
					if (textRange)
						cell.Format.NumFmt = "@";
					object value = r - top < rows && c - left < cols ? data[r - top, c - left] : null;
					SetValue(cell, value);
				}

			foreach (var (r, c, date) in monthYear)
			{
				Cell cell = GetCell(top + r, left + c);
				cell.Kind = Kind.Number;
				cell.Number = date.ToOADate();
				cell.Format.NumFmt = "[$-419]MMMM\\ YYYY";
			}
		}

		public void SetBordersStyles(int top, int left, int bottom, int right, bool fNeedInsideVertical)
		{
			bool insideVertical = fNeedInsideVertical && left != right;
			bool insideHorizontal = top != bottom;
			for (int r = top; r <= bottom; r++)
				for (int c = left; c <= right; c++)
				{
					XlsxCellFormat f = GetCell(r, c).Format;
					if (r == top || insideHorizontal) f.Top = true;
					if (r == bottom || insideHorizontal) f.Bottom = true;
					if (c == left || insideVertical) f.Left = true;
					if (c == right || insideVertical) f.Right = true;
				}
		}

		public void SetBoldForRange(int top, int left, int bottom, int right)
		{
			ForRange(top, left, bottom, right, f => f.Bold = true);
		}

		public void SetCellValue(int y, int x, object val)
		{
			SetValue(GetCell(y, x), val);
		}

		public void SetAutoFitCells()
		{
			if (_cells.Count == 0) return;
			SetAutoFitCells(_cells.Keys.Min(k => k.col), _cells.Keys.Max(k => k.col));
		}

		public void SetAutoFitCells(int left, int right)
		{
			// Как MSDocumentSheet: автоподбор по строкам со 2-й до последней
			// использованной (первая строка — заголовок листа).
			int lastRow = _cells.Count == 0 ? 1 : _cells.Keys.Max(k => k.row);
			int topRow = lastRow > 1 ? 2 : 1;
			for (int c = left; c <= right; c++)
			{
				double maxPx = 0;
				foreach (var kv in _cells)
				{
					if (kv.Key.col != c || kv.Key.row < topRow || kv.Key.row > lastRow || kv.Value.Kind == Kind.Empty)
						continue;
					maxPx = Math.Max(maxPx, TextMeasure.ContentWidthPx(DisplayText(kv.Value), kv.Value.Format));
				}
				// Пустую колонку Excel при автоподборе не трогает.
				if (maxPx > 0)
					_columnWidths[c] = PixelsToWidth(maxPx);
			}
		}

		public void SetFormatForCell(int top, int left, int bottom, int right, Type type)
		{
			string format;
			if (type == typeof(short))
				format = "0";
			else if (type == typeof(Money))
				format = MoneyFormat();
			else if (type == typeof(Time))
				format = "hh:mm:ss";
			else if (type == typeof(DateTime))
				format = "dd/mm/yyyy";
			else
				format = "@";
			ForRange(top, left, bottom, right, f => f.NumFmt = format);
		}

		// При русской культуре — прежний формат листа из COM («1 234,00 p»);
		// иначе знак валюты по культуре установки, как в GridExcelExport веба.
		private static string MoneyFormat()
		{
			NumberFormatInfo nf = CultureInfo.CurrentCulture.NumberFormat;
			if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ru")
				return "#,##0.00\\ \\p";
			string symbol = "\"" + nf.CurrencySymbol + "\"";
			switch (nf.CurrencyPositivePattern)
			{
				case 0: return symbol + "#,##0.00";
				case 1: return "#,##0.00" + symbol;
				case 2: return symbol + "\\ #,##0.00";
				default: return "#,##0.00\\ " + symbol;
			}
		}

		public void SetFormatForCell(int top, int left, int bottom, int right, string type)
		{
			if (type == CustomType.Time)
				ForRange(top, left, bottom, right, f => f.NumFmt = "h:mm");
		}

		public void SetOrientationForCells(int x, int y, int gr)
		{
			GetCell(x, y).Format.Rotation = gr;
		}

		public void SetLandscapeOrientation()
		{
			_landscape = true;
		}

		public void SetStyleForRange(int top, int left, int bottom, int right, bool fBold, bool fItalic, int fontSize)
		{
			ForRange(top, left, bottom, right, f =>
			{
				f.Bold = fBold;
				f.Italic = fItalic;
				f.FontSize = fontSize;
			});
		}

		public void SetBackground(int top, int left, int bottom, int right, int r, int g, int b)
		{
			string rgb = $"{r:X2}{g:X2}{b:X2}";
			ForRange(top, left, bottom, right, f => f.Fill = rgb);
		}

		public void InsertImage(int top, int left, byte[] image)
		{
			// Как MSDocumentSheet: строка — по высоте картинки, картинка — в левый
			// верхний угол ячейки, размер в пунктах по разрешению картинки. Подписи
			// агентств хранятся в BMP — перекодируем в PNG, как делает Excel.
			using (var input = new MemoryStream(image))
			using (var img = System.Drawing.Image.FromStream(input))
			using (var png = new MemoryStream())
			{
				double dpi = img.HorizontalResolution > 0 ? img.HorizontalResolution : 96;
				double widthPt = img.Width / dpi * 72;
				double heightPt = img.Height / dpi * 72;
				img.Save(png, System.Drawing.Imaging.ImageFormat.Png);
				_rowHeights[top] = heightPt;
				_images.Add(new Image
				{
					Row = top, Col = left, Png = png.ToArray(),
					WidthEmu = (long)Math.Round(widthPt * 12700), HeightEmu = (long)Math.Round(heightPt * 12700),
				});
			}
		}

		public void SetColumnWidth(int columnIndex, double width)
		{
			_columnWidths[columnIndex] = width;
		}

		public double GetColumnWidth(int columnIndex)
		{
			return _columnWidths.TryGetValue(columnIndex, out double w) ? w : DefaultColumnWidth;
		}

		public void SetColumnNumberFormat(int columnIndex, string format)
		{
			throw new NotSupportedException("OpenXmlDocumentSheet: SetColumnNumberFormat не реализован — медиаплан его не использует.");
		}

		public void SetWrapText(int top, int left, int bottom, int right, bool wrap)
		{
			ForRange(top, left, bottom, right, f => f.Wrap = wrap);
		}

		public void SetAutoFitRows(int top, int bottom)
		{
			throw new NotSupportedException("OpenXmlDocumentSheet: SetAutoFitRows не реализован — медиаплан его не использует.");
		}

		#endregion

		#region Значения

		private Cell GetCell(int row, int col)
		{
			if (!_cells.TryGetValue((row, col), out Cell cell))
			{
				cell = new Cell { Kind = Kind.Empty, Format = _baseFormat.Clone() };
				_cells[(row, col)] = cell;
			}
			return cell;
		}

		private void ForRange(int top, int left, int bottom, int right, Action<XlsxCellFormat> apply)
		{
			for (int r = top; r <= bottom; r++)
				for (int c = left; c <= right; c++)
					apply(GetCell(r, c).Format);
		}

		private static void SetValue(Cell cell, object value)
		{
			cell.Text = null;
			cell.Number = 0;
			switch (value)
			{
				case null:
				case DBNull _:
					cell.Kind = Kind.Empty;
					return;
				case string s:
					SetString(cell, s);
					return;
				case bool b:
					cell.Kind = Kind.Boolean;
					cell.Number = b ? 1 : 0;
					return;
				case DateTime d:
					cell.Kind = Kind.Number;
					cell.Number = d.ToOADate();
					if (cell.Format.NumFmt == null || cell.Format.NumFmt == "@")
						cell.Format.NumFmt = d.TimeOfDay == TimeSpan.Zero ? "dd.mm.yyyy" : "dd.mm.yyyy h:mm";
					return;
				case byte _: case sbyte _: case short _: case ushort _: case int _: case uint _:
				case long _: case ulong _: case float _: case double _: case decimal _:
					cell.Kind = Kind.Number;
					cell.Number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
					return;
				default:
					SetString(cell, value.ToString());
					return;
			}
		}

		private static readonly Regex TimeRx = new Regex(@"^(\d{1,2}):(\d{2})(?::(\d{2}))?$", RegexOptions.Compiled);
		private static readonly Regex NumberRx = new Regex(@"^-?\d+(,\d+)?$", RegexOptions.Compiled);
		private static readonly Regex DateRx = new Regex(@"^(\d{1,2})\.(\d{1,2})\.(\d{4})$", RegexOptions.Compiled);

		// Строка в ячейку: в текстовой ячейке («@») — как есть; иначе, как Excel
		// (русские настройки), число, время и дата распознаются и хранятся числом.
		private static void SetString(Cell cell, string s)
		{
			if (s.Length == 0)
			{
				cell.Kind = Kind.Empty;
				return;
			}
			if (cell.Format.NumFmt != "@")
			{
				string t = s.Trim();
				Match m;
				if (NumberRx.IsMatch(t))
				{
					cell.Kind = Kind.Number;
					cell.Number = double.Parse(t, CultureInfo.GetCultureInfo("ru-RU"));
					return;
				}
				if ((m = TimeRx.Match(t)).Success && int.Parse(m.Groups[2].Value) < 60)
				{
					cell.Kind = Kind.Number;
					int sec = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
					cell.Number = (int.Parse(m.Groups[1].Value) * 3600 + int.Parse(m.Groups[2].Value) * 60 + sec) / 86400.0;
					if (cell.Format.NumFmt == null)
						cell.Format.NumFmt = m.Groups[3].Success ? "h:mm:ss" : "h:mm";
					return;
				}
				if ((m = DateRx.Match(t)).Success
					&& DateTime.TryParseExact(t, "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
				{
					cell.Kind = Kind.Number;
					cell.Number = date.ToOADate();
					if (cell.Format.NumFmt == null)
						cell.Format.NumFmt = "dd.mm.yyyy";
					return;
				}
			}
			cell.Kind = Kind.Text;
			cell.Text = s;
		}

		private static readonly Dictionary<string, int> RussianMonths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			{ "январь", 1 }, { "февраль", 2 }, { "март", 3 }, { "апрель", 4 }, { "май", 5 }, { "июнь", 6 },
			{ "июль", 7 }, { "август", 8 }, { "сентябрь", 9 }, { "октябрь", 10 }, { "ноябрь", 11 }, { "декабрь", 12 },
		};

		private static bool TryParseMonthYear(string str, out DateTime date)
		{
			date = default(DateTime);
			string[] parts = str.Trim().Split(' ');
			bool hasYearSuffix = parts.Length == 3
				&& string.Equals(parts[2].TrimEnd('.'), "г", StringComparison.OrdinalIgnoreCase);
			if ((parts.Length == 2 || hasYearSuffix)
				&& RussianMonths.TryGetValue(parts[0], out int month)
				&& int.TryParse(parts[1], out int year))
			{
				date = new DateTime(year, month, 1);
				return true;
			}
			return false;
		}

		// Текст ячейки, как его покажет Excel, — для автоподбора ширины.
		private static string DisplayText(Cell cell)
		{
			if (cell.Kind == Kind.Text) return cell.Text;
			if (cell.Kind == Kind.Boolean) return cell.Number != 0 ? "ИСТИНА" : "ЛОЖЬ";
			CultureInfo ru = CultureInfo.GetCultureInfo("ru-RU");
			switch (cell.Format.NumFmt)
			{
				case "h:mm": return DateTime.FromOADate(cell.Number).ToString("H:mm", ru);
				case "h:mm:ss": case "hh:mm:ss": return DateTime.FromOADate(cell.Number).ToString("H:mm:ss", ru);
				case "dd.mm.yyyy": case "dd/mm/yyyy": return DateTime.FromOADate(cell.Number).ToString("dd.MM.yyyy", ru);
				case "#,##0.00\\ \\p": return cell.Number.ToString("#,##0.00", ru) + " p";
				default: return cell.Number.ToString(ru);
			}
		}

		// Ширина колонки в файле (в ширинах цифры «Обычного» шрифта, с полями) по
		// ширине в пикселях: Excel показывает колонку шириной px = W * 7.
		private static double PixelsToWidth(double px)
		{
			return Math.Ceiling(px / MaxDigitWidth * 256) / 256;
		}

		#endregion

		#region Запись листа

		public void Write(WorksheetPart part, OpenXmlExportDocument.StyleTable styles, OpenXmlExportDocument.SharedStrings strings,
			Dictionary<string, ImagePart> images)
		{
			var ws = new S.Worksheet();
			uint baseStyle = styles.GetStyle(_baseFormat);

			ws.Append(new S.SheetViews(new S.SheetView { WorkbookViewId = 0 }));
			ws.Append(new S.SheetFormatProperties { DefaultRowHeight = DefaultRowHeight, CustomHeight = true });
			if (_columnWidths.Count > 0)
				ws.Append(BuildColumns(baseStyle));

			var sheetData = new S.SheetData();
			foreach (var rowGroup in _cells.GroupBy(kv => kv.Key.row).OrderBy(g => g.Key))
			{
				// Высоту пишем у каждой строки явно: иначе Excel при открытии сам
				// подгоняет строку и учитывает пустые ячейки «Обычного» шрифта (14,5 пт).
				var row = new S.Row
				{
					RowIndex = (uint)rowGroup.Key,
					Height = RowHeight(rowGroup.Key, rowGroup.Select(kv => kv.Value)),
					CustomHeight = true,
				};
				foreach (var kv in rowGroup.OrderBy(kv => kv.Key.col))
					row.Append(BuildCell(kv.Key.row, kv.Key.col, kv.Value, styles, strings));
				sheetData.Append(row);
			}
			// Строки только с картинкой (без ячеек) тоже должны получить высоту.
			foreach (int r in _rowHeights.Keys.Where(r => !_cells.Keys.Any(k => k.row == r)))
				InsertRow(sheetData, new S.Row { RowIndex = (uint)r, Height = _rowHeights[r], CustomHeight = true });
			ws.Append(sheetData);

			ws.Append(new S.PageMargins { Left = 0.7, Right = 0.7, Top = 0.75, Bottom = 0.75, Header = 0.3, Footer = 0.3 });
			ws.Append(new S.PageSetup { PaperSize = 9, Orientation = _landscape ? S.OrientationValues.Landscape : S.OrientationValues.Portrait });

			if (_images.Count > 0)
			{
				DrawingsPart drawings = part.AddNewPart<DrawingsPart>();
				ws.Append(new S.Drawing { Id = part.GetIdOfPart(drawings) });
				WriteDrawings(drawings, images);
			}
			part.Worksheet = ws;
		}

		private static void InsertRow(S.SheetData sheetData, S.Row row)
		{
			S.Row after = sheetData.Elements<S.Row>().LastOrDefault(r => r.RowIndex.Value < row.RowIndex.Value);
			if (after == null)
				sheetData.InsertAt(row, 0);
			else
				after.InsertAfterSelf(row);
		}

		private S.Columns BuildColumns(uint baseStyle)
		{
			// Только колонки с заданной шириной: у остальных Excel сам берёт свою
			// стандартную ширину (как в листе из COM); <col> без ширины он показывает
			// нулевой шириной, поэтому их не пишем.
			var columns = new S.Columns();
			foreach (int c in _columnWidths.Keys.OrderBy(c => c))
				columns.Append(new S.Column { Min = (uint)c, Max = (uint)c, Width = _columnWidths[c], CustomWidth = true, Style = baseStyle });
			return columns;
		}

		// Высота строки: заданная (картинка) или как у Excel при автоподборе — по
		// крупному шрифту и по повёрнутому тексту, но не меньше высоты по умолчанию.
		private double RowHeight(int row, IEnumerable<Cell> cells)
		{
			if (_rowHeights.TryGetValue(row, out double custom))
				return custom;
			double height = DefaultRowHeight;
			foreach (Cell cell in cells)
			{
				if (cell.Kind == Kind.Empty) continue;
				double h = cell.Format.Rotation != 0
					? TextMeasure.RotatedHeightPt(DisplayText(cell), cell.Format)
					: TextMeasure.LineHeightPt(cell.Format);
				height = Math.Max(height, h);
			}
			return height;
		}

		private static S.Cell BuildCell(int row, int col, Cell cell, OpenXmlExportDocument.StyleTable styles, OpenXmlExportDocument.SharedStrings strings)
		{
			var c = new S.Cell { CellReference = ColumnName(col) + row, StyleIndex = styles.GetStyle(cell.Format) };
			switch (cell.Kind)
			{
				case Kind.Text:
					c.DataType = S.CellValues.SharedString;
					c.CellValue = new S.CellValue(strings.Get(cell.Text));
					break;
				case Kind.Number:
					c.CellValue = new S.CellValue(cell.Number.ToString("R", CultureInfo.InvariantCulture));
					break;
				case Kind.Boolean:
					c.DataType = S.CellValues.Boolean;
					c.CellValue = new S.CellValue(cell.Number != 0 ? "1" : "0");
					break;
			}
			return c;
		}

		private static string ColumnName(int col)
		{
			string name = string.Empty;
			while (col > 0)
			{
				int m = (col - 1) % 26;
				name = (char)('A' + m) + name;
				col = (col - m) / 26;
			}
			return name;
		}

		private void WriteDrawings(DrawingsPart drawings, Dictionary<string, ImagePart> images)
		{
			var wsDr = new Xdr.WorksheetDrawing();
			uint id = 1;
			foreach (Image img in _images)
			{
				string key = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(img.Png));
				string relId;
				if (images.TryGetValue(key, out ImagePart shared))
				{
					// Та же картинка уже на этом листе (подпись у каждого блока) — та же связь.
					IdPartPair linked = drawings.Parts.FirstOrDefault(pp => pp.OpenXmlPart == shared);
					relId = linked.OpenXmlPart != null ? linked.RelationshipId : drawings.GetIdOfPart(drawings.AddPart(shared));
				}
				else
				{
					ImagePart imagePart = drawings.AddImagePart(ImagePartType.Png);
					using (var ms = new MemoryStream(img.Png))
						imagePart.FeedData(ms);
					images[key] = imagePart;
					relId = drawings.GetIdOfPart(imagePart);
				}

				wsDr.Append(new Xdr.OneCellAnchor(
					new Xdr.FromMarker(
						new Xdr.ColumnId((img.Col - 1).ToString(CultureInfo.InvariantCulture)),
						new Xdr.ColumnOffset("0"),
						new Xdr.RowId((img.Row - 1).ToString(CultureInfo.InvariantCulture)),
						new Xdr.RowOffset("0")),
					new Xdr.Extent { Cx = img.WidthEmu, Cy = img.HeightEmu },
					new Xdr.Picture(
						new Xdr.NonVisualPictureProperties(
							new Xdr.NonVisualDrawingProperties { Id = ++id, Name = "Рисунок " + (id - 1) },
							new Xdr.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = true })),
						new Xdr.BlipFill(
							new A.Blip { Embed = relId },
							new A.Stretch(new A.FillRectangle())),
						new Xdr.ShapeProperties(
							new A.Transform2D(new A.Offset { X = 0, Y = 0 }, new A.Extents { Cx = img.WidthEmu, Cy = img.HeightEmu }),
							new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })),
					new Xdr.ClientData()));
			}
			drawings.WorksheetDrawing = wsDr;
		}

		#endregion
	}

	/// <summary>
	/// Замер текста для автоподбора ширины колонок и высоты строк — вместо
	/// Excel, которого здесь нет. Шрифт и отступы подобраны по листам, которые
	/// строит Excel через COM (стенд tools/mediaplan-compare).
	/// </summary>
	internal static class TextMeasure
	{
		private static readonly object Lock = new object();
		private static System.Drawing.Graphics _graphics;

		// Поля, которые Excel добавляет к тексту при автоподборе колонки.
		private const double ColumnPaddingPx = 6;
		// Ширина повёрнутого текста на пункт размера шрифта (подобрано по Excel).
		private const double RotatedWidthPxPerPt = 2.35;
		// Поле при автоподборе высоты строки с повёрнутым на 90° текстом.
		private const double RotatedPaddingPt = 5.5;

		public static double TextWidthPx(string text, XlsxCellFormat f)
		{
			if (string.IsNullOrEmpty(text)) return 0;
			lock (Lock)
			{
				if (_graphics == null)
				{
					var bmp = new System.Drawing.Bitmap(1, 1);
					bmp.SetResolution(96, 96);
					_graphics = System.Drawing.Graphics.FromImage(bmp);
					_graphics.PageUnit = System.Drawing.GraphicsUnit.Pixel;
				}
				var style = (f.Bold ? System.Drawing.FontStyle.Bold : 0) | (f.Italic ? System.Drawing.FontStyle.Italic : 0);
				using (var font = new System.Drawing.Font(f.FontName, (float)f.FontSize, style, System.Drawing.GraphicsUnit.Point))
					return _graphics.MeasureString(text, font, int.MaxValue, System.Drawing.StringFormat.GenericTypographic).Width;
			}
		}

		public static double ContentWidthPx(string text, XlsxCellFormat f)
		{
			// Повёрнутый на 90° текст занимает по ширине строку шрифта с межстрочным
			// интервалом: у Excel колонка дат в сетке — 26 пикселей при Tahoma 8.
			double px = f.Rotation != 0 ? f.FontSize * RotatedWidthPxPerPt : TextWidthPx(text, f);
			return px + ColumnPaddingPx;
		}

		public static double LineHeightPt(XlsxCellFormat f) => Math.Ceiling(f.FontSize * 1.25 * 4) / 4;

		public static double RotatedHeightPt(string text, XlsxCellFormat f) =>
			Math.Ceiling((TextWidthPx(text, f) * 72 / 96 + RotatedPaddingPt) * 4) / 4;
	}
}
