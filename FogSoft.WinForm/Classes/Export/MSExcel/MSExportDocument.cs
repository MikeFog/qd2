using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using log4net;
using Microsoft.Office.Interop.Excel;

namespace FogSoft.WinForm.Classes.Export.MSExcel
{
	public class MSExportDocument : IExportDocument
	{
		private static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private Application app = null;
		private Workbook wb = null;

		private IList<Worksheet> worksheets = new List<Worksheet>();

		public MSExportDocument()
		{
			try
			{
                //app = (Application)Marshal.GetActiveObject("Excel.Application");
                app = new ApplicationClass
                {
                    Visible = true
                };
            }
			catch(Exception e)
			{
				Log.Error("Error - MSExportDocument()", e);
				app = new ApplicationClass();
			}
		}

		public void StartExport()
		{
			//app.ScreenUpdating = false;
		}

		public void FinishExport()
		{
			//app.ScreenUpdating = true;
			app.Visible = true;
			if(wb != null)
				wb.Saved = true;
		}

		public IDocumentSheet GetNewSheet(string name, string fontName, int fontSize)
		{
            CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;

            //Log.Info("IDocumentSheet GetNewSheet(string name, string fontName, int fontSize)");
			if (wb == null)
				wb = app.Workbooks.Add(XlWBATemplate.xlWBATWorksheet);
			else
				wb.Sheets.Add(Type.Missing, wb.ActiveSheet, 1, XlSheetType.xlWorksheet);
			Worksheet ws = (Worksheet)wb.ActiveSheet;
			
			if (!string.IsNullOrEmpty(name))
			{
				bool fFind = false;
				foreach (Worksheet sheet in wb.Sheets)
					if (sheet.Name == name)
						fFind = true;
				if (!fFind)
					ws.Name = name;
			}
			
			Range wsCells = ws.Cells;
			var wsFont = wsCells.Font;
			wsFont.Name = fontName;
			wsFont.Size = fontSize;
			Marshal.ReleaseComObject(wsFont);
			Marshal.ReleaseComObject(wsCells);

			worksheets.Add(ws);

			return new MSDocumentSheet(ws);
		}

		public void OnAppQuit()
		{
            CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            try
			{
				foreach (Worksheet worksheet in worksheets)
				{
					if (worksheet != null)
						Marshal.ReleaseComObject(worksheet);
				}
				if (app == null) return;
				foreach (Workbook workbook in app.Workbooks)
				{
					if (workbook != null)
					{
						if (!Visible())
							workbook.Close(false, false, null);
						Marshal.ReleaseComObject(workbook);
					}
				}
				if (!Visible())
					app.Quit();
				Marshal.ReleaseComObject(app);
				app = null;
			}
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }

		public bool Visible()
		{
			return (app != null && (app.Visible || !app.ScreenUpdating));
		}

		public void SaveToDisk(string filePath)
		{
			if (wb == null || app == null) return;

			try
			{
				app.Visible = false;
				app.ScreenUpdating = false;

				object format = 51; // XlFileFormat.xlOpenXMLWorkbook
				object mis = Type.Missing;
				wb.SaveAs(filePath, format, mis, mis, mis, mis,
					XlSaveAsAccessMode.xlNoChange, mis, mis, mis, mis, mis);

				wb.Close(false, mis, mis);
			}
			finally
			{
				foreach (Worksheet ws in worksheets)
					if (ws != null) Marshal.ReleaseComObject(ws);
				worksheets.Clear();

				if (wb != null) { Marshal.ReleaseComObject(wb); wb = null; }

				app.Quit();
				Marshal.ReleaseComObject(app);
				app = null;
			}
		}
	}
}
