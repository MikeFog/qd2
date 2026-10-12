using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// «Сетка вещания» станции на день без UI — для веб-экрана (десктоп — FrmGridReport +
	/// GridReportCreator + Crystal-макет Grid.rpt). Данные — rpt_Grid_v3, раскладка по окнам —
	/// как в макете: группа по tariffTime, под окном его длительность (среднее cellRealDuration)
	/// и сумма роликов (rollerDurationSum — все ролики блока).
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
	}
}
