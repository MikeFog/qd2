using System.Data;

namespace FogSoft.WinForm.Classes.Export
{
	/// <summary>
	/// Запись таблиц в <see cref="IDocumentSheet"/> без привязки к конкретному
	/// Excel (COM) — поэтому файл входит и в FogSoft.Core. Раньше эти методы жили
	/// в <see cref="ExportManager"/>; там остались обёртки с прежними именами.
	/// </summary>
	public static class SheetWriter
	{
		public static void CopyData2WorkSheet(IDocumentSheet sheet, DataTable dt, int left, int top, bool rotate = false)
		{
			object[,] data = ProcessData(dt, rotate);
			PopulateWorksheet(data, left, top, sheet);
		}

		public static void PopulateWorksheet(object[,] data, int left, int top, IDocumentSheet ws)
		{
			int height = data.GetLength(0);
			int width = data.GetLength(1);
			int bottom = top + height - 1;
			int right = left + width - 1;

			if (height == 0 && width == 0)
				return;

			ws.SetValuesForRange(top, left, bottom, right, data);
			ws.SetBordersStyles(top, left, bottom, right, height > 1);
		}

		// Строки «Октябрь 2026 г.» передаются как есть: лист (MSDocumentSheet, OpenXmlDocumentSheet)
		// сам узнаёт месяц-год и ставит формат «MMMM YYYY». Раньше их здесь заранее превращали в DateTime,
		// лист их уже не узнавал, и на Excel 2010 (прод) «Период» выходил числом (31.10.2026).
		private static object[,] ProcessData(DataTable dt, bool rotate)
		{
			object[,] data = rotate ? new object[dt.Columns.Count, dt.Rows.Count] : new object[dt.Rows.Count, dt.Columns.Count];
			for (int i = 0; i < dt.Rows.Count; i++)
				for (int j = 0; j < dt.Columns.Count; j++)
				{
					if (rotate)
						data[j, i] = dt.Rows[i][j];
					else
						data[i, j] = dt.Rows[i][j];
				}
			return data;
		}
	}
}
