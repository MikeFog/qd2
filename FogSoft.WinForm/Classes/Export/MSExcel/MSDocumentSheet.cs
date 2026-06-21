using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;

namespace FogSoft.WinForm.Classes.Export.MSExcel
{
	class MSDocumentSheet : IDocumentSheet
	{
		private readonly Worksheet worksheet = null;

		public MSDocumentSheet(Worksheet worksheet)
		{
			this.worksheet = worksheet;
		}

		private Range GetRange(int top, int left, int bottom, int right)
		{
			Range topLeft = (Range)worksheet.Cells[top, left];
			Range bottomRight = (Range)worksheet.Cells[bottom, right];
			Range rg = worksheet.get_Range(topLeft, bottomRight);
			Marshal.ReleaseComObject(topLeft);
			Marshal.ReleaseComObject(bottomRight);
			return rg;
		}

		private static void SetBorder(Borders borders, XlBordersIndex index, XlLineStyle style)
		{
			Border b = borders[index];
			b.LineStyle = style;
			Marshal.ReleaseComObject(b);
		}

		public void SetValuesForRange(int top, int left, int bottom, int right, object[,] data)
		{
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            // Запоминаем позиции DateTime-ячеек ДО конвертации в OADate
            var monthYearCells = new System.Collections.Generic.List<(int r, int c)>();
            var russianMonths = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "январь",  1 }, { "февраль",  2 }, { "март",     3 },
                { "апрель",  4 }, { "май",      5 }, { "июнь",     6 },
                { "июль",    7 }, { "август",   8 }, { "сентябрь", 9 },
                { "октябрь", 10 }, { "ноябрь", 11 }, { "декабрь", 12 }
            };

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (data[r, c] is string str)
                    {
                        string[] parts = str.Trim().Split(' ');
                        bool hasYearSuffix = parts.Length == 3
                            && string.Equals(parts[2].TrimEnd('.'), "г", System.StringComparison.OrdinalIgnoreCase);
                        if ((parts.Length == 2 || hasYearSuffix)
                            && russianMonths.TryGetValue(parts[0], out int month)
                            && int.TryParse(parts[1], out int year))
                        {
                            monthYearCells.Add((r, c));
                            data[r, c] = new DateTime(year, month, 1).ToOADate();
                        }
                    }

            Range rg = GetRange(top, left, bottom, right);
            rg.set_Value(XlRangeValueDataType.xlRangeValueDefault, data);
            Marshal.ReleaseComObject(rg);

            // Применяем формат "Март 2026" (русский) к ячейкам, которые были DateTime
            foreach (var (r, c) in monthYearCells)
            {
                Range cell = (Range)worksheet.Cells[top + r, left + c];
                cell.NumberFormat = "[$-419]MMMM YYYY";
                Marshal.ReleaseComObject(cell);
            }
		}

		public void SetBordersStyles(int top, int left, int bottom, int right, bool fNeedInsideVertical)
		{
			Range rg = GetRange(top, left, bottom, right);
			Borders borders = rg.Borders;
			SetBorder(borders, XlBordersIndex.xlEdgeTop, XlLineStyle.xlContinuous);
			SetBorder(borders, XlBordersIndex.xlEdgeRight, XlLineStyle.xlContinuous);
			SetBorder(borders, XlBordersIndex.xlEdgeBottom, XlLineStyle.xlContinuous);
			SetBorder(borders, XlBordersIndex.xlEdgeLeft, XlLineStyle.xlContinuous);
			if (fNeedInsideVertical && left != right)
				SetBorder(borders, XlBordersIndex.xlInsideVertical, XlLineStyle.xlContinuous);
			if (top != bottom)
				SetBorder(borders, XlBordersIndex.xlInsideHorizontal, XlLineStyle.xlContinuous);
			Marshal.ReleaseComObject(borders);
			Marshal.ReleaseComObject(rg);
		}

		public void SetBoldForRange(int top, int left, int bottom, int right)
		{
			Range rg = GetRange(top, left, bottom, right);
			var font = rg.Font;
			font.Bold = true;
			Marshal.ReleaseComObject(font);
			Marshal.ReleaseComObject(rg);
		}

		public void SetCellValue(int y, int x, object val)
		{
			Range cells = worksheet.Cells;
			Range cell = (Range)cells[y, x];
			cell.Value2 = val;
			Marshal.ReleaseComObject(cell);
			Marshal.ReleaseComObject(cells);
		}

		public void SetAutoFitCells()
		{
			Range range = worksheet.Cells;
			Range cols = range.EntireColumn;
			cols.AutoFit();
			Marshal.ReleaseComObject(cols);
			Marshal.ReleaseComObject(range);
		}

        public void SetAutoFitRows(int top, int bottom)
        {
            Range usedRange = worksheet.UsedRange;
            Range usedCols = usedRange.Columns;
            int lastColumn = usedCols.Count;
            Marshal.ReleaseComObject(usedCols);
            Marshal.ReleaseComObject(usedRange);
            if (lastColumn < 1) lastColumn = 1;

            Range range = GetRange(top, 1, bottom, lastColumn);
            Range rows = range.Rows;
            rows.AutoFit();
            Marshal.ReleaseComObject(rows);
            Marshal.ReleaseComObject(range);
        }

		public void SetAutoFitCells(int left, int right)
        {
            Range usedRange = worksheet.UsedRange;
            Range usedRows = usedRange.Rows;
            int lastRow = usedRows.Count;
            Marshal.ReleaseComObject(usedRows);
            Marshal.ReleaseComObject(usedRange);
            if (lastRow < 1) lastRow = 1;
            int topRow = lastRow > 1 ? 2 : 1;

            Range range = GetRange(topRow, left, lastRow, right);
            Range cols = range.EntireColumn;
            cols.AutoFit();
            Marshal.ReleaseComObject(cols);
            Marshal.ReleaseComObject(range);
        }

		public void SetFormatForCell(int top, int left, int bottom, int right, Type type)
		{
			if (type == typeof(Int16))
				SetRangeFormat(top, left, bottom, right, "0");
			else if (type == typeof(Money))
				SetRangeFormat(top, left, bottom, right, "#,##0.00 p");
			else if(type == typeof(Time))
                SetRangeFormat(top, left, bottom, right, "hh:mm:ss");
            else if (type == typeof(DateTime))
                SetRangeFormat(top, left, bottom, right, "dd/mm/yyyy");
            else
                SetRangeFormat(top, left, bottom, right, "@");
		}

        public void SetFormatForCell(int top, int left, int bottom, int right, string type)
        {
            if (type == CustomType.Time)
                SetRangeFormat(top, left, bottom, right, "h:mm");
        }

        public void SetColumnWidth(int columnIndex, double width)
        {
            Range rg = (Range)worksheet.Columns[columnIndex];
            rg.ColumnWidth = width;
            Marshal.ReleaseComObject(rg);
        }

        public double GetColumnWidth(int columnIndex)
        {
            Range rg = (Range)worksheet.Columns[columnIndex];
            double res = (double)rg.ColumnWidth;
            Marshal.ReleaseComObject(rg);
            return res;
        }

        public void SetColumnNumberFormat(int columnIndex, string format)
        {
            Range rg = (Range)worksheet.Columns[columnIndex];
            rg.NumberFormat = format;
            Marshal.ReleaseComObject(rg);
        }

        public void SetWrapText(int top, int left, int bottom, int right, bool wrap)
        {
            Range rg = GetRange(top, left, bottom, right);
            rg.WrapText = wrap;
            Marshal.ReleaseComObject(rg);
        }

        private void SetRangeFormat(int top, int left, int bottom, int right, string format)
		{
            Range rg = GetRange(top, left, bottom, right);
            rg.NumberFormat = format;
            Marshal.ReleaseComObject(rg);
        }

        public void SetOrientationForCells(int x, int y, int gr)
		{
			Range rg = (Range)worksheet.Cells[x, y];
			rg.Orientation = gr;
			Marshal.ReleaseComObject(rg);
		}

		public void SetLandscapeOrientation()
		{
			var pageSetup = worksheet.PageSetup;
			pageSetup.Orientation = XlPageOrientation.xlLandscape;
			Marshal.ReleaseComObject(pageSetup);
		}

		public void SetStyleForRange(int top, int left, int bottom, int right, bool fBold, bool fItalic, int fontSize)
		{
			Range rg = GetRange(top, left, bottom, right);
			var font = rg.Font;
			font.Bold = fBold;
			font.Italic = fItalic;
			font.Size = fontSize;
			Marshal.ReleaseComObject(font);
			Marshal.ReleaseComObject(rg);
		}

		public void SetBackground(int top, int left, int bottom, int right, int r, int g, int b)
		{
			Range rg = GetRange(top, left, bottom, right);
			var interior = rg.Interior;
			interior.Color = b * (int)Math.Pow(16, 4) + g * (int)Math.Pow(16, 2) + r;
			Marshal.ReleaseComObject(interior);
			Marshal.ReleaseComObject(rg);
		}

		public void InsertImage(int top, int left, byte[] image)
		{
			string fileName = TempFileWorker.GetTemplateFile();
			using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
				fs.Write(image, 0, image.Length);

			// Считываем размеры и освобождаем Image (а с ним GDI+-lock файла) ДО AddPicture,
			// иначе temp-файл остаётся залоченным и TempFileWorker.Clear() не может его удалить.
			int imgWidth, imgHeight;
			float horizRes;
			using (Image img = Image.FromFile(fileName))
			{
				imgWidth = img.Width;
				imgHeight = img.Height;
				horizRes = img.HorizontalResolution;
			}

			Range rg = GetRange(top, left, top, left);
			object[] args = new object[] { GetPoint(imgHeight, horizRes) };
			rg.GetType().InvokeMember("RowHeight", BindingFlags.SetProperty, null, rg, args);

			var shapes = worksheet.Shapes;
			var shape = shapes.AddPicture(fileName, MsoTriState.msoFalse, MsoTriState.msoTrue
				, ParseHelper.GetInt32FromObject(rg.Left, 0), ParseHelper.GetInt32FromObject(rg.Top, 0)
				, GetPoint(imgWidth, horizRes), GetPoint(imgHeight, horizRes));
			Marshal.ReleaseComObject(shape);
			Marshal.ReleaseComObject(shapes);
			Marshal.ReleaseComObject(rg);
		}

		/// <summary>
		/// Convert Image Pixels with resolution to Office Point (1/72 inch)
		/// </summary>
		private static float GetPoint(int value, double resolution)
		{
			return (float) (((double) value/resolution)*72);
		}
    }
}
