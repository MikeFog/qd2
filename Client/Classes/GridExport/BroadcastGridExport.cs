using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes.GridExport
{
	/// <summary>
	/// Выгрузка сетки вещания станции на день без UI: файлы для эфира (DJin) и сетка в Word.
	/// Для веба («Экспорт» на сетке, «Экспорт сеток вещания»); десктоп (GridReportCreator,
	/// ExportGridForm) берёт отсюда раскладку позиций <see cref="AdjustIssuePositions"/>.
	/// </summary>
	public static class BroadcastGridExport
	{
		/// <summary>Файлы для эфира: до двух на станцию (до полуночи и после).</summary>
		public static IList<ExportFile> DJin(int massmediaId, DateTime date)
		{
			Massmedia massmedia = Massmedia.GetMassmediaByID(massmediaId);
			ExportDocument document = ExportDocument.GetDocument();
			if (document == null)
				return new List<ExportFile>();

			DataTable data = AdjustIssuePositions(LoadData(massmediaId, date));
			return document.ExportToMemory(data, massmedia, date.Date, ExportHelper.RemoveInvalidFileNameChars(massmedia.Name));
		}

		/// <summary>
		/// Сетка в Word — тот же лист, что экран «Сетка вещания», без отбора по менеджеру.
		/// Имя — как у десктопной выгрузки Crystal: название станции (расширение docx, а не doc).
		/// </summary>
		public static ExportFile Word(int massmediaId, DateTime date)
		{
			Massmedia massmedia = Massmedia.GetMassmediaByID(massmediaId);
			BroadcastGrid grid = BroadcastGrid.Load(massmediaId, date.Date, null);
			return new ExportFile
			{
				Name = ExportHelper.RemoveInvalidFileNameChars(massmedia.Name) + ".docx",
				Content = BroadcastGridDocx.Build(grid, massmedia.Name, date.Date, null)
			};
		}

		/// <summary>Название станции для отчёта о выгрузке.</summary>
		public static string StationName(int massmediaId)
		{
			return Massmedia.GetMassmediaByID(massmediaId).Name;
		}

		/// <summary>Строки сетки для выгрузки — rpt_Grid_v3 без менеджера: в эфир уходит вся сетка.</summary>
		private static DataTable LoadData(int massmediaId, DateTime date)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Massmedia.ParamNames.MassmediaId] = massmediaId;
			parameters["theDate"] = date.Date;
			return DataAccessor.LoadDataSet("rpt_Grid_v3", parameters, 120).Tables[0];
		}

		// ---------- Позиции выпусков в окне ----------
		// Перенесено без изменений из GridReportCreator (десктоп): первый и второй ролики окна,
		// затем остальные — поочерёдно по предметам рекламы (сначала самые многочисленные,
		// не больше двух подряд за проход), последний — в конце.

		private class Window
		{
			public DataRow FirstRow;
			public DataRow SecondRow;
			public DataRow LastRow;

			public class IssuesWithRoltype
			{
				public IList<DataRow> Issues = new List<DataRow>();
			}

			public IDictionary<string, IssuesWithRoltype> IssuesByRoltype = new Dictionary<string, IssuesWithRoltype>();

			public int IssuesUnprocessed
			{
				get
				{
					int res = 0;
					foreach (IssuesWithRoltype item in IssuesByRoltype.Values)
						res += item.Issues.Count;

					return res;
				}
			}
		}

		public static DataTable AdjustIssuePositions(DataTable dataTable)
		{
			DataTable dt = dataTable.Clone();
			string time = string.Empty;
			Window window = null;

			foreach (DataRow row in dataTable.Rows)
			{
				if (time != row[ExportParams.tariffTime].ToString())
				{
					// началось новое рекламное окно
					ProcessTariffWindow(dt, window);
					window = new Window();
					time = row[ExportParams.tariffTime].ToString();
				}

				string advertType = row[ExportParams.advertTypeId].ToString();
				int position = 0;

				if (row[ExportParams.positionId] != DBNull.Value)
					position = int.Parse(row[ExportParams.positionId].ToString());
				if (position == (int)RollerPositions.First)
					window.FirstRow = row;
				else if (position == (int)RollerPositions.Second)
					window.SecondRow = row;
				else if (position == (int)RollerPositions.Last)
					window.LastRow = row;
				else
				{
					if (!window.IssuesByRoltype.ContainsKey(advertType))
						window.IssuesByRoltype.Add(advertType, new Window.IssuesWithRoltype());

					Window.IssuesWithRoltype list = window.IssuesByRoltype[advertType];
					list.Issues.Add(row);
				}
			}
			ProcessTariffWindow(dt, window);
			return dt;
		}

		private static void ProcessTariffWindow(DataTable dt, Window window)
		{
			if (window == null) return;
			if (window.FirstRow != null)
				dt.Rows.Add(window.FirstRow.ItemArray);
			if (window.SecondRow != null)
				dt.Rows.Add(window.SecondRow.ItemArray);

			while (window.IssuesUnprocessed > 0)
			{
				int count = 0;
				foreach (Window.IssuesWithRoltype item in window.IssuesByRoltype.Values.OrderByDescending(val => val.Issues.Count))
				{
					if (item.Issues.Count > 0)
					{
						dt.Rows.Add(item.Issues[0].ItemArray);
						item.Issues.RemoveAt(0);
					}
					if (++count == 2) break;
				}
			}
			if (window.LastRow != null)
				dt.Rows.Add(window.LastRow.ItemArray);
		}
	}
}
