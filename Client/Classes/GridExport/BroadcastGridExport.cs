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
		// Первый и второй ролики окна, затем остальные — так, чтобы два ролика одного предмета
		// рекламы (подпредмета, advertTypeId) не стояли рядом, в том числе с первым/вторым
		// и с последним; последний — в конце.

		// Типы, которые BlockManager (DJin) всегда выносит из середины блока на свои места:
		// идентификаторы СМИ 4/5/44/55, анонс агитации 7, агитация 6, локальное промо 8/9.
		// Соседями рекламе в эфире они не будут, поэтому в чередовании не участвуют.
		private static readonly HashSet<string> TypesOutOfBlockBody =
			new HashSet<string> { "4", "5", "6", "7", "8", "9", "44", "55" };

		private static bool IsOutOfBlockBody(DataRow row)
		{
			return TypesOutOfBlockBody.Contains(row[ExportParams.rolActionTypeID].ToString());
		}

		private class Window
		{
			public DataRow FirstRow;
			public DataRow SecondRow;
			public DataRow LastRow;
			public IList<DataRow> OutOfBlockBody = new List<DataRow>();

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
				else if (IsOutOfBlockBody(row))
					window.OutOfBlockBody.Add(row);
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

			// Предмет соседа слева — второй (или первый) ролик, справа — последний,
			// если они остаются в середине блока
			string prevType = null;
			foreach (DataRow row in new[] { window.FirstRow, window.SecondRow })
				if (row != null && !IsOutOfBlockBody(row))
					prevType = row[ExportParams.advertTypeId].ToString();
			string lastType = window.LastRow != null && !IsOutOfBlockBody(window.LastRow)
				? window.LastRow[ExportParams.advertTypeId].ToString()
				: null;

			// Каждый раз — самый многочисленный предмет, не совпадающий с предыдущим роликом.
			// Предмет последнего ролика считается на один больше, чтобы закончиться раньше него
			while (window.IssuesUnprocessed > 0)
			{
				KeyValuePair<string, Window.IssuesWithRoltype> next = window.IssuesByRoltype
					.Where(item => item.Value.Issues.Count > 0)
					.OrderBy(item => item.Key == prevType)
					.ThenByDescending(item => item.Value.Issues.Count + (item.Key == lastType ? 1 : 0))
					.ThenByDescending(item => item.Value.Issues.Count)
					.First();
				dt.Rows.Add(next.Value.Issues[0].ItemArray);
				next.Value.Issues.RemoveAt(0);
				prevType = next.Key;
			}
			foreach (DataRow row in window.OutOfBlockBody)
				dt.Rows.Add(row.ItemArray);
			if (window.LastRow != null)
				dt.Rows.Add(window.LastRow.ItemArray);
		}
	}
}
