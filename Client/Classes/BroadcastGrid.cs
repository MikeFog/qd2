using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// «Сетка вещания» станции на день без UI — для веб-экрана (десктоп — FrmGridReport +
	/// GridReportCreator + Crystal-макет Grid.rpt). Данные — rpt_Grid_v3, итоги дня —
	/// stat_FillPercentage, раскладка по окнам — как в макете: группа по tariffTime, под окном
	/// его длительность (среднее cellRealDuration) и сумма роликов (rollerDurationSum).
	/// </summary>
	public sealed class BroadcastGrid
	{
		public sealed class Row
		{
			public string Description;
			public string Duration;
		}

		public sealed class Window
		{
			public string Time;
			public readonly List<Row> Rows = new List<Row>();

			/// <summary>Длительность окна, «мм:сс».</summary>
			public string WindowDuration;

			/// <summary>Сумма роликов, «мм:сс»; null — в окне ничего нет (в макете пусто).</summary>
			public string AdsDuration;
		}

		public readonly List<Window> Windows = new List<Window>();

		/// <summary>Заполняемость и фактическое время рекламы за день (подвал макета); null — нет данных.</summary>
		public string Fill;
		public string RealTime;

		/// <param name="userId">Менеджер: только его выпуски; null — все.</param>
		public static BroadcastGrid Load(int massmediaId, DateTime date, int? userId)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Massmedia.ParamNames.MassmediaId] = massmediaId;
			parameters["theDate"] = date.Date;
			if (userId != null)
				parameters[SecurityManager.ParamNames.UserId] = userId.Value;

			BroadcastGrid grid = new BroadcastGrid();
			grid.Build(DataAccessor.LoadDataSet("rpt_Grid_v3", parameters, 120).Tables[0]);
			grid.LoadFill(massmediaId, date.Date);
			return grid;
		}

		private void Build(DataTable data)
		{
			int i = 0;
			while (i < data.Rows.Count)
			{
				string time = data.Rows[i]["tariffTime"].ToString();
				Window window = new Window { Time = time };
				double windowSum = 0;
				int windowCount = 0;
				int adsSum = 0;
				bool hasAds = false;

				// Группа Crystal по tariffTime без сортировки — подряд идущие строки одного времени.
				for (; i < data.Rows.Count && data.Rows[i]["tariffTime"].ToString() == time; i++)
				{
					DataRow row = data.Rows[i];
					if (row["cellRealDuration"] != DBNull.Value)
					{
						windowSum += Convert.ToDouble(row["cellRealDuration"]);
						windowCount++;
					}
					if (row["rollerDurationSum"] != DBNull.Value)
					{
						adsSum += Convert.ToInt32(row["rollerDurationSum"]);
						hasAds = true;
					}
					if (row["Description"] != DBNull.Value)
						window.Rows.Add(new Row
						{
							Description = row["Description"].ToString(),
							Duration = row["rollerDurationString"].ToString()
						});
				}

				window.WindowDuration = windowCount == 0 ? null : MinutesSeconds(windowSum / windowCount);
				window.AdsDuration = hasAds ? MinutesSeconds(adsSum) : null;
				Windows.Add(window);
			}
		}

		/// <summary>Формулы макета fWindowDuration/fSumDuration: «мм:сс», минуты не ограничены.</summary>
		private static string MinutesSeconds(double seconds)
		{
			int total = (int)Math.Round(seconds, MidpointRounding.AwayFromZero);
			return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
		}

		private void LoadFill(int massmediaId, DateTime date)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["StartDay"] = date;
			parameters["FinishDay"] = date;
			parameters[Massmedia.ParamNames.MassmediaId] = massmediaId;

			// Итоги — довесок к сетке: как в десктопе, их сбой пишется в лог, а сетка показывается.
			try
			{
				DataSet ds = DataAccessor.LoadDataSet("stat_FillPercentage", parameters, 60);
				if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
					return;
				DataRow row = ds.Tables[0].Rows[0];
				Fill = row["fill"].ToString();
				RealTime = row["realTime"].ToString();
			}
			catch (Exception e)
			{
				ErrorManager.LogError("Cannot stat_FillPercentage", e); // i18n-ok: текст для лога
			}
		}
	}
}
