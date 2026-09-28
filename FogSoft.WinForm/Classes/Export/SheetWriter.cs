using System;
using System.Data;
using System.Globalization;

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

		public static object ConvertCellValue(object val)
		{
			if (val is string s && TryParseMonthYear(s, out DateTime dt))
				return dt;
			return val;
		}

		private static bool TryParseMonthYear(string s, out DateTime result)
		{
			result = default(DateTime);
			if (string.IsNullOrWhiteSpace(s)) return false;

			string cleaned = s.Trim();
			if (cleaned.EndsWith("г.", StringComparison.OrdinalIgnoreCase))
				cleaned = cleaned.Substring(0, cleaned.Length - 2).Trim();
			else if (cleaned.EndsWith("г", StringComparison.OrdinalIgnoreCase))
				cleaned = cleaned.Substring(0, cleaned.Length - 1).Trim();

			if (DateTime.TryParseExact(cleaned, new[] { "MMMM yyyy", "MMM yyyy" },
					new CultureInfo("ru-RU"), DateTimeStyles.None, out result))
			{
				result = new DateTime(result.Year, result.Month, 1);
				return true;
			}

			return false;
		}

		private static object[,] ProcessData(DataTable dt, bool rotate)
		{
			object[,] data = rotate ? new object[dt.Columns.Count, dt.Rows.Count] : new object[dt.Rows.Count, dt.Columns.Count];
			for (int i = 0; i < dt.Rows.Count; i++)
				for (int j = 0; j < dt.Columns.Count; j++)
				{
					object converted = ConvertCellValue(dt.Rows[i][j]);
					if (rotate)
						data[j, i] = converted;
					else
						data[i, j] = converted;
				}
			return data;
		}
	}
}
