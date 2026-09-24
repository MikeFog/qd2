using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Трафик-менеджмент без UI — для веб-экрана «Трафик» (десктоп — TrafficManagementForm +
	/// TrafficGrid). Сетка окон — TariffWindowWeek.LoadForTraffic; здесь — то, что вокруг неё.
	/// Решения владельца — docs/tasks/web-tariffgrid.md, §9 «Трафик-менеджмент».
	/// </summary>
	public static class TrafficManagement
	{
		/// <summary>Группы станций с пунктом «Показать все» (id 0) — как фильтр десктопной формы.</summary>
		public static DataView Groups()
		{
			return Massmedia.LoadGroupsWithShowAllOption();
		}

		/// <summary>
		/// Станции группы (0 — все) с датой «обработано по» — massmediaList с набором колонок
		/// трафика (Massmedia.WinForms.LoadRadiostationsByGroup). Неактивные станции не
		/// показываем: окон по ним не ведут (десктоп показывает все).
		/// </summary>
		public static DataTable Stations(int groupId)
		{
			Entity entity = (Entity)Massmedia.GetEntity().Clone();
			entity.AttributeSelector = (int)Massmedia.AttributeSelectors.TrafficDeadLine;
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(entity);
			if (groupId > 0)
				parameters.Add(Massmedia.ParamNames.GroupId, groupId);

			DataTable table = ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
			DataView active = new DataView(table) { RowFilter = "isActive = true", Sort = "name" };
			return active.ToTable();
		}

		/// <summary>
		/// Отметить станцию «обработанной по» дату (Massmedia.deadLine): выпуски этих дней после
		/// этого может менять только трафик-менеджер и администратор (hlp_IssueVerify). Тот же
		/// путь, что у десктопа (Massmedia.SetDeadLine: перечитать станцию, поставить дату,
		/// сохранить — MassmediaIUD). Более ранняя дата снимает отметку с последующих дней.
		/// </summary>
		public static void SetClosedThrough(int massmediaId, DateTime date)
		{
			Massmedia.GetMassmediaByID(massmediaId).SetDeadLine(date.Date);
		}

		// ---------- «Изменить окна…» ----------

		/// <summary>
		/// Цель правки — ровно эти окна (выделение в сетке). Процедуры вызываются по окну, на
		/// один его день.
		/// </summary>
		public static WindowChangeTarget ForCells(TariffWindowWeek week, IEnumerable<TariffWindowCell> cells)
		{
			List<DataRow> rows = new List<DataRow>();
			foreach (TariffWindowCell cell in cells)
				rows.Add(cell.Row);
			return new WindowChangeTarget(week.PricelistId, rows, null);
		}

		/// <summary>
		/// Цель правки — «повторить на период»: окна прайс-листа недели этих тарифных времён в эти
		/// дни недели за период (как десктопные «Перенос времени выхода» / «Изменить
		/// продолжительность» у строки времени). Окна периода читаются одним TariffWindowRetrieve.
		/// </summary>
		/// <param name="days">Пн…Вс.</param>
		public static WindowChangeTarget ForPeriod(TariffWindowWeek week, IEnumerable<TimeSpan> times,
			DateTime start, DateTime finish, bool[] days)
		{
			List<TimeSpan> timeList = new List<TimeSpan>(times);
			List<DataRow> rows = new List<DataRow>();
			if (start <= finish && week.PricelistId > 0)
			{
				MassmediaPricelist p = (MassmediaPricelist)Massmedia.GetMassmediaByID(week.MassmediaId).GetPriceList(week.StartDate);
				p.ExcludeSpecialWindows = false;
				p.ExcludeModuleTariffs = false;
				DataTable windows = p.GetTariffWindows(start.Date, finish.Date, null, true).Tables[Constants.TableNames.Data];
				foreach (DataRow w in windows.Rows)
				{
					DateTime original = (DateTime)w[TariffWindow.ParamNames.WindowDateOriginal];
					int day = ((int)original.DayOfWeek + 6) % 7;
					if (days[day] && timeList.Contains(new TimeSpan(original.Hour, original.Minute, 0)))
						rows.Add(w);
				}
			}
			return new WindowChangeTarget(week.PricelistId, rows,
				new WindowChangePeriod(timeList, start.Date, finish.Date, (bool[])days.Clone()));
		}

		/// <summary>Период вне срока прайс-листа недели — текст ошибки, иначе null.</summary>
		public static string ValidatePeriod(TariffWindowWeek week, DateTime start, DateTime finish, bool[] days)
		{
			if (start > finish)
				return MessageAccessor.GetMessage("StartFinishDateError2");
			if (start < week.PricelistStart || finish > week.PricelistFinish)
				return string.Format("Период должен быть внутри срока прайс-листа: {0:dd.MM.yyyy} – {1:dd.MM.yyyy}.",
					week.PricelistStart, week.PricelistFinish);
			foreach (bool d in days)
				if (d) return null;
			return "Отметьте хотя бы один день недели.";
		}

		/// <summary>
		/// Проверка правки до записи: что-то меняется, время — только у одной строки времени,
		/// продолжительность не больше полной у каждого окна (как DurationExceedsTotal процедуры,
		/// но заранее и со списком). null — всё верно.
		/// </summary>
		public static string Validate(WindowChangeTarget target, WindowChange change)
		{
			if (!change.NewTime.HasValue && !change.NewDuration.HasValue && !change.NewTotal.HasValue)
				return "Заполните хотя бы одно: время выхода, продолжительность или полную продолжительность.";
			if (target.Count == 0)
				return "Под условие не попало ни одного окна.";
			if (change.NewTime.HasValue && target.Times.Count > 1)
				return "Время выхода меняется только для одной строки времени — выделите окна одного времени.";

			int bad = 0;
			string first = null;
			foreach (DataRow w in target.Rows)
			{
				int duration = change.NewDuration ?? Seconds(w, TariffWindow.ParamNames.Duration);
				int total = change.NewTotal ?? Seconds(w, TariffWindow.ParamNames.DurationTotal);
				if (total > 0 && duration > total)
				{
					bad++;
					if (first == null)
						first = string.Format("{0:dd.MM.yyyy HH:mm}: продолжительность {1} больше полной {2}",
							(DateTime)w[TariffWindow.ParamNames.WindowDateOriginal],
							DateTimeUtils.Time2String(duration), DateTimeUtils.Time2String(total));
				}
			}
			if (bad > 0)
				return string.Format("Продолжительность не может быть больше полной — окон с нарушением: {0}. Например, {1}.", bad, first);
			return null;
		}

		/// <summary>
		/// Применить правку в одной транзакции: TariffWindowMoveTime (время выхода) и
		/// TariffWindowChangeDuration (продолжительность и полная — процедура пишет обе сразу,
		/// поэтому незаполненная берётся у окна своя). У «повторить на период» процедура
		/// вызывается одна на время, если у всех его окон получаются одинаковые значения; иначе —
		/// по окну. Ошибка любой записи откатывает всё.
		/// </summary>
		public static void Apply(WindowChangeTarget target, WindowChange change)
		{
			string error = Validate(target, change);
			if (error != null)
				throw new InvalidOperationException(error);

			DataAccessor.BeginTransaction();
			try
			{
				if (target.Period != null)
					ApplyPeriod(target, change);
				else
					foreach (DataRow w in target.Rows)
						ApplyWindow(target.PricelistId, w, change);

				DataAccessor.CommitTransaction();
			}
			catch
			{
				DataAccessor.RollbackTransaction();
				throw;
			}
		}

		private static void ApplyPeriod(WindowChangeTarget target, WindowChange change)
		{
			WindowChangePeriod period = target.Period;
			foreach (TimeSpan time in period.Times)
			{
				List<DataRow> ofTime = target.Rows.FindAll(w => OriginalTime(w) == time);
				if (ofTime.Count == 0)
					continue;

				if (change.NewTime.HasValue)
					MoveTime(target.PricelistId, time, change.NewTime.Value, period.Start, period.Finish, period.Days);

				if (!change.NewDuration.HasValue && !change.NewTotal.HasValue)
					continue;

				HashSet<string> targets = new HashSet<string>();
				foreach (DataRow w in ofTime)
					targets.Add(NewDuration(w, change) + "/" + NewTotal(w, change));

				if (targets.Count == 1)
					ChangeDuration(target.PricelistId, time, NewDuration(ofTime[0], change), NewTotal(ofTime[0], change),
						period.Start, period.Finish, period.Days);
				else
					foreach (DataRow w in ofTime)
						ChangeDuration(target.PricelistId, time, NewDuration(w, change), NewTotal(w, change),
							OriginalDay(w), OriginalDay(w), AllDays);
			}
		}

		private static void ApplyWindow(int pricelistId, DataRow w, WindowChange change)
		{
			TimeSpan time = OriginalTime(w);
			DateTime day = OriginalDay(w);
			if (change.NewTime.HasValue)
				MoveTime(pricelistId, time, change.NewTime.Value, day, day, AllDays);
			if (change.NewDuration.HasValue || change.NewTotal.HasValue)
				ChangeDuration(pricelistId, time, NewDuration(w, change), NewTotal(w, change), day, day, AllDays);
		}

		private static readonly bool[] AllDays = { true, true, true, true, true, true, true };
		private static readonly string[] DayParams = { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };

		private static void MoveTime(int pricelistId, TimeSpan time, TimeSpan newTime, DateTime start, DateTime finish, bool[] days)
		{
			Dictionary<string, object> parameters = PeriodParameters(pricelistId, time, start, finish, days);
			parameters["newtime"] = new DateTime(1900, 1, 1).Add(newTime);
			DataAccessor.ExecuteNonQuery("TariffWindowMoveTime", parameters);
		}

		private static void ChangeDuration(int pricelistId, TimeSpan time, int duration, int total,
			DateTime start, DateTime finish, bool[] days)
		{
			Dictionary<string, object> parameters = PeriodParameters(pricelistId, time, start, finish, days);
			parameters["newDuration"] = duration;
			parameters["newDuration_total"] = total;
			DataAccessor.ExecuteNonQuery("TariffWindowChangeDuration", parameters);
		}

		private static Dictionary<string, object> PeriodParameters(int pricelistId, TimeSpan time, DateTime start,
			DateTime finish, bool[] days)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["time"] = new DateTime(1900, 1, 1).Add(time);
			parameters["startdate"] = start.Date;
			parameters["finishdate"] = finish.Date;
			parameters["pricelistid"] = pricelistId;
			for (int i = 0; i < DayParams.Length; i++)
				parameters[DayParams[i]] = days[i];
			return parameters;
		}

		private static int NewDuration(DataRow w, WindowChange change) =>
			change.NewDuration ?? Seconds(w, TariffWindow.ParamNames.Duration);

		private static int NewTotal(DataRow w, WindowChange change) =>
			change.NewTotal ?? Seconds(w, TariffWindow.ParamNames.DurationTotal);

		internal static int Seconds(DataRow w, string column) =>
			w[column] == DBNull.Value ? 0 : Convert.ToInt32(w[column]);

		internal static TimeSpan OriginalTime(DataRow w)
		{
			DateTime original = (DateTime)w[TariffWindow.ParamNames.WindowDateOriginal];
			return new TimeSpan(original.Hour, original.Minute, 0);
		}

		internal static DateTime OriginalDay(DataRow w) =>
			((DateTime)w[TariffWindow.ParamNames.WindowDateOriginal]).Date;
	}

	/// <summary>Что меняем в окнах: null — не менять.</summary>
	public sealed class WindowChange
	{
		public TimeSpan? NewTime { get; set; }
		public int? NewDuration { get; set; }
		public int? NewTotal { get; set; }
	}

	/// <summary>«Повторить на период»: времена × дни недели × период.</summary>
	public sealed class WindowChangePeriod
	{
		internal WindowChangePeriod(IReadOnlyList<TimeSpan> times, DateTime start, DateTime finish, bool[] days)
		{
			Times = times;
			Start = start;
			Finish = finish;
			Days = days;
		}

		public IReadOnlyList<TimeSpan> Times { get; }
		public DateTime Start { get; }
		public DateTime Finish { get; }
		public bool[] Days { get; }
	}

	/// <summary>
	/// Окна, которые изменит правка, и сводка их текущих значений — вместо десктопных полей
	/// «по тарифу» (тариф — шаблон, у окон значения свои: docs/business-logic.md).
	/// </summary>
	public sealed class WindowChangeTarget
	{
		internal WindowChangeTarget(int pricelistId, List<DataRow> rows, WindowChangePeriod period)
		{
			PricelistId = pricelistId;
			Rows = rows;
			Period = period;

			SortedSet<TimeSpan> times = new SortedSet<TimeSpan>();
			foreach (DataRow w in rows)
				times.Add(TrafficManagement.OriginalTime(w));
			Times = period != null ? period.Times : new List<TimeSpan>(times);
		}

		internal int PricelistId { get; }
		internal List<DataRow> Rows { get; }

		/// <summary>null — ровно выделенные окна.</summary>
		public WindowChangePeriod Period { get; }

		/// <summary>Тарифные времена, которых касается правка.</summary>
		public IReadOnlyList<TimeSpan> Times { get; }

		public int Count => Rows.Count;

		/// <summary>«02:56 — 40, 03:11 — 5» — текущая продолжительность окон по значениям.</summary>
		public string DurationSummary => Summary(TariffWindow.ParamNames.Duration);

		/// <summary>То же для полной продолжительности.</summary>
		public string TotalSummary => Summary(TariffWindow.ParamNames.DurationTotal);

		/// <summary>Сколько окон уже перенесено по времени (фактическое время не как по расписанию).</summary>
		public int MovedCount
		{
			get
			{
				int moved = 0;
				foreach (DataRow w in Rows)
					if (!Equals(w[TariffWindow.ParamNames.WindowDateActual], w[TariffWindow.ParamNames.WindowDateOriginal]))
						moved++;
				return moved;
			}
		}

		private string Summary(string column)
		{
			SortedDictionary<int, int> byValue = new SortedDictionary<int, int>();
			foreach (DataRow w in Rows)
			{
				int value = TrafficManagement.Seconds(w, column);
				byValue.TryGetValue(value, out int n);
				byValue[value] = n + 1;
			}

			List<string> parts = new List<string>();
			foreach (KeyValuePair<int, int> kv in byValue)
				parts.Add(string.Format("{0} — {1}", DateTimeUtils.Time2String(kv.Key), kv.Value));
			return string.Join(", ", parts);
		}
	}
}
