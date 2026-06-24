using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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
			{
				Workbooks workbooks = app.Workbooks;
				wb = workbooks.Add(XlWBATemplate.xlWBATWorksheet);
				Marshal.ReleaseComObject(workbooks);
			}
			else
			{
				Sheets sheets = wb.Sheets;
				Worksheet activeForAdd = (Worksheet)wb.ActiveSheet;
				sheets.Add(Type.Missing, activeForAdd, 1, XlSheetType.xlWorksheet);
				Marshal.ReleaseComObject(activeForAdd);
				Marshal.ReleaseComObject(sheets);
			}
			Worksheet ws = (Worksheet)wb.ActiveSheet;

			if (!string.IsNullOrEmpty(name))
			{
				bool fFind = false;
				Sheets sheets = wb.Sheets;
				int count = sheets.Count;
				for (int i = 1; i <= count; i++)
				{
					Worksheet sheet = (Worksheet)sheets[i];
					if (sheet.Name == name)
						fFind = true;
					Marshal.ReleaseComObject(sheet);
				}
				Marshal.ReleaseComObject(sheets);
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

			// PID процесса EXCEL.EXE снимаем ДО Quit, пока app жив. Это headless-экземпляр
			// (Visible=false), используемый только для записи файла, поэтому если после Quit
			// процесс не завершился (висящие COM-ссылки), его можно безопасно убить по PID —
			// видимый путь (FinishExport) сюда не заходит.
			uint pid = 0;
			try
			{
				GetWindowThreadProcessId((IntPtr)app.Hwnd, out pid);
			}
			catch (Exception e)
			{
				Log.Error("SaveToDisk - get Excel PID", e);
			}

			try
			{
				app.Visible = false;
				app.ScreenUpdating = false;
				app.DisplayAlerts = false;

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

				KillIfAlive(pid);
			}
		}

		private static void KillIfAlive(uint pid)
		{
			if (pid == 0) return;
			try
			{
				Process proc = Process.GetProcessById((int)pid);
				try
				{
					if (!proc.HasExited)
					{
						proc.Kill();
						proc.WaitForExit(5000);
					}
				}
				finally
				{
					proc.Dispose();
				}
			}
			catch (ArgumentException)
			{
				// Процесса с таким PID уже нет — Excel завершился штатно, это и есть цель.
			}
			catch (Exception e)
			{
				Log.Error("SaveToDisk - kill Excel process", e);
			}
		}
	}
}
