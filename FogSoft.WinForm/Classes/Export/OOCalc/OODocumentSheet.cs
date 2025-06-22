using System;
using System.Drawing;
using System.IO;
using uno;
using unoidl.com.sun.star.awt;
using unoidl.com.sun.star.beans;
using unoidl.com.sun.star.drawing;
using unoidl.com.sun.star.lang;
using unoidl.com.sun.star.sheet;
using unoidl.com.sun.star.table;
using unoidl.com.sun.star.text;
using Point=unoidl.com.sun.star.awt.Point;
using Size=unoidl.com.sun.star.awt.Size;

namespace FogSoft.WinForm.Classes.Export.OOCalc
{
	class OODocumentSheet : IDocumentSheet
	{
		private readonly XSpreadsheet sheet;
		private readonly XSpreadsheetDocument xComponent;
		private int rightColumn = 0;

		public OODocumentSheet(XSpreadsheetDocument xComponent, XSpreadsheet sheet)
		{
			this.xComponent = xComponent;
			this.sheet = sheet;
		}

		public void SetValuesForRange(int top, int left, int bottom, int right, object[,] data)
		{
			if (data == null || data.Length == 0)
				return;

			SetCorrectSize(ref bottom, ref top, ref right, ref left);
			XCellRangeData rangeData = (XCellRangeData)sheet.getCellRangeByPosition(left - 1, top - 1, right - 1, bottom - 1);
			int yCount = (bottom - top + 1);
			int xCount = (right - left + 1);
			Any[][] aData = new Any[yCount][];

			for (int j = top; j <= bottom; j++)
			{
				aData[j - top] = new Any[xCount];
				for (int i = left; i <= right; i++)
				{
					object val = data[j - top, i - left];
					aData[j - top][i-left] = (val == null || val == DBNull.Value) ? new Any(string.Empty) 
						: (val is bool) ? new Any((bool)val)
						: (val is double) ? new Any((double)val)
						: (val is float) ? new Any((float)val)
						: (val is decimal) ? new Any(decimal.ToDouble((decimal)val))
						: new Any(val.ToString());
				}
			}

			rangeData.setDataArray(aData);
		}

		public void SetBordersStyles(int top, int left, int bottom, int right, bool fNeedInsideVertical)
		{
			SetCorrectSize(ref bottom, ref top, ref right, ref left);

			XCellRange range = sheet.getCellRangeByPosition(left - 1, top - 1, right - 1, bottom - 1);
			
			XPropertySet property = (XPropertySet) range;
			BorderLine line = new BorderLine {InnerLineWidth = 1};
			property.setPropertyValue("TopBorder", new Any(typeof(BorderLine), line));
			property.setPropertyValue("BottomBorder", new Any(typeof(BorderLine), line));
			property.setPropertyValue("LeftBorder", new Any(typeof(BorderLine), line));
			property.setPropertyValue("RightBorder", new Any(typeof(BorderLine), line));
		}

		private static void SetCorrectSize(ref int bottom, ref int top, ref int right, ref int left)
		{
			int t;
			if (bottom < top)
			{
				t = top;
				top = bottom;
				bottom = t;
			}

			if (right < left)
			{
				t = left;
				left = right;
				right = t;
			}
		}

		public void SetBoldForRange(int top, int left, int bottom, int right)
		{
			SetCorrectSize(ref bottom, ref top, ref right, ref left);
			XPropertySet set = (XPropertySet)sheet.getCellRangeByPosition(left - 1, top - 1, right - 1, bottom - 1);
			set.setPropertyValue("CharWeight", new Any(FontWeight.BOLD));
		}

		public void SetCellValue(int y, int x, object val)
		{
			rightColumn = x;
			XCell xCell = sheet.getCellByPosition(x - 1, y - 1);
            if (val is double || val is float)
            {
                xCell.setValue((double)val);
            }
			else if (val is decimal)
			{
				xCell.setValue(decimal.ToDouble((decimal)val));
			}
            else
            {
                XText xCellText = (XText) xCell;
                XTextCursor xTextCursor = xCellText.createTextCursor();
                if (val != null)
                    xCellText.insertString(xTextCursor, val.ToString(), false);
            }
		}

		public void InsertImage(int y, int x, byte[] image)
		{
			XCell cell = sheet.getCellByPosition(x - 1, y - 1);
			XPropertySet propertySetCell = cell as XPropertySet;
			Point point = propertySetCell.getPropertyValue("Position").Value as Point;

			string fileName = TempFileWorker.GetTemplateFile();
			using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
				fs.Write(image, 0, image.Length);

			Image img = Image.FromFile(fileName);

			XDrawPageSupplier supplier = sheet as XDrawPageSupplier;
			XDrawPage page = supplier.getDrawPage();
			
			XColumnRowRange range = sheet as XColumnRowRange;
			XPropertySet row = range.getRows().getByIndex(y - 1).Value as XPropertySet;
			row.setPropertyValue("Height", new Any(ToOpenOfficeFormat(img.Height, img.VerticalResolution)));

			XMultiServiceFactory factory = xComponent as XMultiServiceFactory;
			XShape shape = factory.createInstance("com.sun.star.drawing.GraphicObjectShape") as XShape;
			shape.setPosition(point);
			shape.setSize(new Size(ToOpenOfficeFormat(img.Width, img.HorizontalResolution), ToOpenOfficeFormat(img.Height, img.VerticalResolution)));
			XPropertySet propertySet = shape as XPropertySet;
			
			propertySet.setPropertyValue("GraphicURL", new Any(string.Format("file:///{0}", fileName)));
			propertySet.setPropertyValue("FilterName", new Any("BMP - Windows Bitmap"));
			propertySet.setPropertyValue("AsLink", new Any(false));
			
			page.add(shape);
		}

		private static int ToOpenOfficeFormat(int pixels, float resolution)
		{
			return (int)(((float)pixels / resolution) * 1000 * 2.54);
		}

		public void SetAutoFitCells()
		{
            SetAutoFitCells(0, rightColumn);
		}

        public void SetAutoFitCells(int left, int right)
        {
            XColumnRowRange columns = (XColumnRowRange)sheet;
            for (int i = left; i <= right; i++)
            {
                XPropertySet column = (XPropertySet)columns.getColumns().getByIndex(i).Value;
                column.setPropertyValue("OptimalWidth", new Any(true));
            }
        }

		public void SetFormatForCell(int top, int left, int bottom, int right, Type type)
		{
			SetCorrectSize(ref bottom, ref top, ref right, ref left);
			XPropertySet set = (XPropertySet) sheet.getCellRangeByPosition(left - 1, top - 1, right - 1, bottom - 1);
			if (type == typeof(Money))
				set.setPropertyValue("NumberFormat", new Any(102));
			else if (type == typeof(float))
			    set.setPropertyValue("NumberFormat", new Any(2));
		}

        public void SetFormatForCell(int top, int left, int bottom, int right, string type)
        {
        
        }

        public void SetOrientationForCells(int x, int y, int gr)
		{
			XCell xCell = sheet.getCellByPosition(y - 1, x - 1);
			XPropertySet propertyCell = (XPropertySet)xCell;
			propertyCell.setPropertyValue("RotateAngle", new Any(gr*100));
		}

		public void SetLandscapeOrientation()
		{
			// already set in OOExportDocument.FinishExport()
		}

		public void SetStyleForRange(int top, int left, int bottom, int right, bool fBold, bool fItalic, int fontSize)
		{
			SetCorrectSize(ref bottom, ref top, ref right, ref left);
			XPropertySet set = (XPropertySet)sheet.getCellRangeByPosition(left - 1, top - 1, right - 1, bottom - 1);
			if (fBold)
				set.setPropertyValue("CharWeight", new Any(FontWeight.BOLD));
			if (fItalic)
				set.setPropertyValue("CharPosture", new Any(typeof(FontSlant), FontSlant.ITALIC));
			set.setPropertyValue("CharHeight", new Any((float)fontSize));
		}

		public void SetBackground(int top, int left, int bottom, int right, int r, int g, int b)
		{
			SetCorrectSize(ref bottom, ref top, ref right, ref left);
			XPropertySet set = (XPropertySet)sheet.getCellRangeByPosition(left - 1, top - 1, right - 1, bottom - 1);
			set.setPropertyValue("CellBackColor", new Any(r * (int)Math.Pow(16, 4) + g * (int)Math.Pow(16, 2) + b));
		}

		public void SetColumnWidth(int columnIndex, double width)
		{

		}

        public double GetColumnWidth(int columnIndex)
        {
			return 0;
        }
    }
}