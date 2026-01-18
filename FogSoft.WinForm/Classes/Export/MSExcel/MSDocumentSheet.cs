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

		public void SetValuesForRange(int top, int left, int bottom, int right, object[,] data)
		{
			Range rg = worksheet.get_Range(worksheet.Cells[top, left], worksheet.Cells[bottom, right]);
			rg.set_Value(XlRangeValueDataType.xlRangeValueDefault, data);
			Marshal.ReleaseComObject(rg);
		}

		public void SetBordersStyles(int top, int left, int bottom, int right, bool fNeedInsideVertical)
		{
			Range rg = worksheet.get_Range(worksheet.Cells[top, left], worksheet.Cells[bottom, right]);
			rg.Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
			rg.Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
			rg.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
			rg.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
			if (fNeedInsideVertical && left != right)
				rg.Borders[XlBordersIndex.xlInsideVertical].LineStyle = XlLineStyle.xlContinuous;
			if (top != bottom)
				rg.Borders[XlBordersIndex.xlInsideHorizontal].LineStyle = XlLineStyle.xlContinuous;
			Marshal.ReleaseComObject(rg);
		}

		public void SetBoldForRange(int top, int left, int bottom, int right)
		{
			Range rg = worksheet.get_Range(worksheet.Cells[top, left], worksheet.Cells[bottom, right]);
			rg.Font.Bold = true;
			Marshal.ReleaseComObject(rg);
		}

		public void SetCellValue(int y, int x, object val)
		{
			worksheet.Cells[y, x] = val;
		}

		public void SetAutoFitCells()
		{
			Range range = worksheet.Cells;
			range.EntireColumn.AutoFit();
			Marshal.ReleaseComObject(range);
		}

		public void SetAutoFitCells(int left, int right)
        {
            int lastRow = worksheet.UsedRange.Rows.Count;
            if (lastRow < 1) lastRow = 1;
            int topRow = lastRow > 1 ? 2 : 1;

            Range range = worksheet.get_Range(worksheet.Cells[topRow, left], worksheet.Cells[lastRow, right]);
            range.EntireColumn.AutoFit();
            Marshal.ReleaseComObject(range);
        }

		public void SetFormatForCell(int top, int left, int bottom, int right, Type type)
		{
			if (type == typeof(Int16))
				SetRangeFormat(top, left, bottom, right, "0");
			else if (type == typeof(Money))
				// rg.NumberFormat = "# ##0,00ð";
				//rg.NumberFormat = "#,##0.00 p";
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
            Range rg = worksheet.get_Range(worksheet.Cells[top, left], worksheet.Cells[bottom, right]);
            rg.WrapText = wrap;
            Marshal.ReleaseComObject(rg);
        }

        private void SetRangeFormat(int top, int left, int bottom, int right, string format)
		{
            Range rg = worksheet.get_Range(worksheet.Cells[top, left], worksheet.Cells[bottom, right]);
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
			worksheet.PageSetup.Orientation = XlPageOrientation.xlLandscape;
		}

		public void SetStyleForRange(int top, int left, int bottom, int right, bool fBold, bool fItalic, int fontSize)
		{
			Range rg = worksheet.get_Range(worksheet.Cells[top, left], worksheet.Cells[bottom, right]);
			rg.Font.Bold = fBold;
			rg.Font.Italic = fItalic;
			rg.Font.Size = fontSize;
			Marshal.ReleaseComObject(rg);
		}

		public void SetBackground(int top, int left, int bottom, int right, int r, int g, int b)
		{
			Range rg = worksheet.get_Range(worksheet.Cells[top, left], worksheet.Cells[bottom, right]);
			rg.Interior.Color = b * (int)Math.Pow(16, 4) + g * (int)Math.Pow(16, 2) + r;
			Marshal.ReleaseComObject(rg);
		}

		public void InsertImage(int top, int left, byte[] image)
		{
			string fileName = TempFileWorker.GetTemplateFile();
			using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
				fs.Write(image, 0, image.Length);

			Image img = Image.FromFile(fileName);

			Range rg = worksheet.get_Range(worksheet.Cells[top, left], worksheet.Cells[top, left]);
			object[] args = new object[] { GetPoint(img.Height, img.HorizontalResolution) };
			rg.GetType().InvokeMember("RowHeight", BindingFlags.SetProperty, null, rg, args);
			
			worksheet.Shapes.AddPicture(fileName, MsoTriState.msoFalse, MsoTriState.msoTrue
				, ParseHelper.GetInt32FromObject(rg.Left, 0), ParseHelper.GetInt32FromObject(rg.Top, 0)
				, GetPoint(img.Width, img.HorizontalResolution), GetPoint(img.Height, img.HorizontalResolution));
			Marshal.ReleaseComObject(rg);
		}

		/// <summary>
		/// Convert Image Pixels with resolution to Office Point (1/72 inch)
		/// </summary>
		/// <param name="value"></param>
		/// <param name="resolution"></param>
		/// <returns></returns>
		private static float GetPoint(int value, double resolution)
		{
			return (float) (((double) value/resolution)*72);
		}
    }
}
