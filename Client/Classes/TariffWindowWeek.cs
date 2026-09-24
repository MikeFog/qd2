using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>Что показывает неделя окон.</summary>
	public enum TariffWindowWeekMode
	{
		/// <summary>Вкладка «Рекламные окна» у прайс-листа: в ячейке длительность окна.</summary>
		Generation,

		/// <summary>
		/// Трафик-менеджмент: в ячейке остаток свободного времени, «[осталось/всего]» у
		/// штучных окон, фактическое время перенесённого окна; видны особые окна, переполнение,
		/// склейка и закрытые («обработанные») дни станции.
		/// </summary>
		Traffic
	}

	/// <summary>
	/// Рекламные окна за одну неделю — данные веб-сетки окон: вкладки «Окна» у прайс-листа
	/// (веб-аналог TariffWindowGrid на форме «Генерация рекламных окон») и экрана «Трафик»
	/// (веб-аналог TrafficGrid). Публичный фасад: MassmediaPricelist internal, веб видит
	/// прайс-лист только как object узла дерева, станцию — по id.
	///
	/// Раскладка — как у TariffWindowGrid: строка = тарифное время + цена (набор «time»
	/// процедуры TariffWindowRetrieve), колонка = день по windowDateOriginal, в ячейке —
	/// длительность окна и вместимость штучного окна. Сутки календарные: broadcastStart у всех
	/// прайс-листов 00:00 (docs/broadcast-start.md), сдвиг ночных часов не повторяем.
	/// Проект — docs/tasks/web-tariffgrid.md.
	/// </summary>
	public sealed class TariffWindowWeek
	{
		public const int DaysInWeek = 7;

		/// <summary>Понедельник недели.</summary>
		public DateTime Monday { get; private set; }

		/// <summary>Первый и последний показанный день — неделя, обрезанная границами прайс-листа.</summary>
		public DateTime StartDate { get; private set; }
		public DateTime FinishDate { get; private set; }

		/// <summary>Есть ли у прайс-листа неделя до/после этой — для стрелок навигации.</summary>
		public bool HasPrevious { get; private set; }
		public bool HasNext { get; private set; }

		public IReadOnlyList<TariffWindowRow> Rows { get; private set; }

		public TariffWindowWeekMode Mode { get; private set; }

		/// <summary>Прайс-лист, по которому построена неделя (0 — прайс-листа нет), и его срок.</summary>
		public int PricelistId { get; private set; }
		public DateTime PricelistStart { get; private set; }
		public DateTime PricelistFinish { get; private set; }

		/// <summary>Станция недели (трафик).</summary>
		public int MassmediaId { get; private set; }

		/// <summary>Трафик: на эту дату у станции нет прайс-листа — окон нет и быть не может.</summary>
		public bool NoPricelist { get; private set; }

		/// <summary>
		/// Трафик: станция «обработана по» эту дату (Massmedia.deadLine) — выпуски этих дней
		/// менять нельзя всем, кроме трафик-менеджера и администратора (hlp_IssueVerify,
		/// DeadLineViolation). null — не закрывалась.
		/// </summary>
		public DateTime? ClosedThrough { get; private set; }

		/// <summary>День закрыт: не позже даты «обработано по».</summary>
		public bool IsClosed(int dayIndex)
		{
			return ClosedThrough.HasValue && Monday.AddDays(dayIndex) <= ClosedThrough.Value.Date;
		}

		/// <summary>Число окон за неделю по дням (Пн…Вс).</summary>
		public int[] WindowsPerDay { get; private set; }

		/// <summary>День недели попадает в срок прайс-листа (иначе колонка пустая и серая).</summary>
		public bool IsInRange(int dayIndex)
		{
			DateTime day = Monday.AddDays(dayIndex);
			return day >= StartDate && day <= FinishDate;
		}

		/// <summary>Узел дерева — прайс-лист радиостанции (у него есть рекламные окна).</summary>
		public static bool Supports(object container)
		{
			return container is MassmediaPricelist;
		}

		/// <summary>
		/// Дата, с которой открывать вкладку: сегодня, если прайс-лист действует, иначе его
		/// начало. Десктоп всегда открывает первую неделю прайс-листа.
		/// </summary>
		public static DateTime DefaultDate(object pricelist)
		{
			Pricelist p = (Pricelist)pricelist;
			DateTime today = DateTime.Today;
			return today >= p.StartDate && today <= p.FinishDate ? today : p.StartDate;
		}

		/// <summary>
		/// Неделя, содержащая <paramref name="anyDate"/>, обрезанная сроком прайс-листа. Дата вне
		/// срока прижимается к ближайшей границе. Один вызов TariffWindowRetrieve с теми же
		/// параметрами, что у формы генерации окон: модульные тарифы показываются, особые окна и
		/// окна трафика — нет, недоступные — да.
		/// </summary>
		public static TariffWindowWeek Load(object pricelist, DateTime anyDate)
		{
			return LoadCore((MassmediaPricelist)pricelist, anyDate, TariffWindowWeekMode.Generation, null);
		}

		/// <summary>
		/// Трафик: неделя окон станции, содержащая <paramref name="anyDate"/>, по прайс-листу,
		/// действующему в эту дату (как TrafficGrid: прайс-лист ищется заново на каждую неделю, и
		/// неделя на стыке прайс-листов обрезается его сроком). Особые окна и окна трафика видны,
		/// модульные тарифы — тоже; время строк — оригинальное (операции трафика ищут окна по
		/// windowDateOriginal, TrafficGrid.UseActualTime = false).
		/// </summary>
		public static TariffWindowWeek LoadForTraffic(int massmediaId, DateTime anyDate)
		{
			Massmedia massmedia = Massmedia.GetMassmediaByID(massmediaId);
			MassmediaPricelist p = massmedia.GetPriceList(anyDate.Date) as MassmediaPricelist;
			if (p == null)
			{
				DateTime monday = anyDate.Date.AddDays(-(((int)anyDate.DayOfWeek + 6) % 7));
				return new TariffWindowWeek
				{
					Mode = TariffWindowWeekMode.Traffic,
					NoPricelist = true,
					Monday = monday,
					StartDate = monday,
					FinishDate = monday.AddDays(DaysInWeek - 1),
					HasPrevious = true,
					HasNext = true,
					ClosedThrough = massmedia.DeadLine,
					Rows = new List<TariffWindowRow>(),
					WindowsPerDay = new int[DaysInWeek]
				};
			}

			p.ExcludeSpecialWindows = false;
			return LoadCore(p, anyDate, TariffWindowWeekMode.Traffic, massmedia.DeadLine);
		}

		private static TariffWindowWeek LoadCore(MassmediaPricelist p, DateTime anyDate, TariffWindowWeekMode mode,
			DateTime? closedThrough)
		{
			DateTime date = anyDate.Date;
			if (date < p.StartDate.Date) date = p.StartDate.Date;
			if (date > p.FinishDate.Date) date = p.FinishDate.Date;

			DateTime monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
			DateTime start = monday < p.StartDate.Date ? p.StartDate.Date : monday;
			DateTime finish = monday.AddDays(DaysInWeek - 1);
			if (finish > p.FinishDate.Date) finish = p.FinishDate.Date;

			// Модульные тарифы на форме генерации окон видны (TariffWindowGrid.excludeModuleTariffs =
			// false). Флаг живёт в объекте узла дерева, поэтому возвращаем значение по умолчанию.
			DataSet ds;
			DataTable tariffs;
			p.ExcludeModuleTariffs = false;
			try
			{
				ds = p.GetTariffWindows(start, finish, null, mode == TariffWindowWeekMode.Traffic);
				// Тарифы — для метки «окно изменено относительно тарифа»: тариф — шаблон,
				// окно — экземпляр со своими правками (docs/business-logic.md, «Тариф и
				// рекламное окно»). Десятки строк, один лёгкий запрос на неделю.
				tariffs = p.GetTariffList();
			}
			finally
			{
				p.ExcludeModuleTariffs = true;
			}

			bool traffic = mode == TariffWindowWeekMode.Traffic;
			TariffWindowWeek week = new TariffWindowWeek
			{
				Mode = mode,
				PricelistId = p.PricelistId,
				PricelistStart = p.StartDate.Date,
				PricelistFinish = p.FinishDate.Date,
				MassmediaId = p.MassmediaId,
				Monday = monday,
				StartDate = start,
				FinishDate = finish,
				// Трафик листает станцию, а не прайс-лист: за границей срока — следующий прайс-лист
				// (или «нет прайс-листа»).
				HasPrevious = traffic || start > p.StartDate.Date,
				HasNext = traffic || finish < p.FinishDate.Date,
				ClosedThrough = closedThrough,
				WindowsPerDay = new int[DaysInWeek]
			};
			week.Rows = BuildRows(ds.Tables["time"], ds.Tables[Constants.TableNames.Data], IndexTariffs(tariffs),
				monday, week.WindowsPerDay, mode);
			return week;
		}

		private static Dictionary<int, DataRow> IndexTariffs(DataTable tariffs)
		{
			Dictionary<int, DataRow> byId = new Dictionary<int, DataRow>();
			foreach (DataRow t in tariffs.Rows)
				byId[Convert.ToInt32(t[Tariff.ParamNames.TariffId])] = t;
			return byId;
		}

		private static List<TariffWindowRow> BuildRows(DataTable times, DataTable windows, Dictionary<int, DataRow> tariffs,
			DateTime monday, int[] perDay, TariffWindowWeekMode mode)
		{
			List<TariffWindowRow> rows = new List<TariffWindowRow>(times.Rows.Count);
			Dictionary<string, TariffWindowRow> byKey = new Dictionary<string, TariffWindowRow>();

			foreach (DataRow t in times.Rows)
			{
				int hour = Convert.ToInt32(t["hour"]);
				int min = Convert.ToInt32(t["min"]);
				decimal price = Convert.ToDecimal(t[TariffWindow.ParamNames.Price]);

				TariffWindowRow row = new TariffWindowRow(new TimeSpan(hour, min, 0), price);
				string key = RowKey(hour, min, price);
				// Коллизий (одно время + цена дважды) набор «time» не даёт: там DISTINCT.
				if (byKey.ContainsKey(key)) continue;
				byKey.Add(key, row);
				rows.Add(row);
			}

			foreach (DataRow w in windows.Rows)
			{
				string key = RowKey(Convert.ToInt32(w["hour"]), Convert.ToInt32(w["min"]),
					Convert.ToDecimal(w[TariffWindow.ParamNames.Price]));
				if (!byKey.TryGetValue(key, out TariffWindowRow row)) continue;

				DateTime original = (DateTime)w[TariffWindow.ParamNames.WindowDateOriginal];
				int day = (int)(original.Date - monday).TotalDays;
				if (day < 0 || day >= DaysInWeek) continue;

				// Дубль окна (LEFT JOIN TariffUnion в процедуре размножает строки) — пропускаем,
				// десктоп в этом случае просто перезаписывает ячейку тем же окном.
				if (row.Cells[day] != null) continue;

				row.Cells[day] = new TariffWindowCell(
					Convert.ToInt32(w[TariffWindow.ParamNames.WindowId]),
					(DateTime)w[TariffWindow.ParamNames.WindowDateActual],
					mode == TariffWindowWeekMode.Traffic ? TrafficText(w) : CellText(w),
					w[TariffWindow.ParamNames.IsDisabled] is bool disabled && disabled,
					w[TariffWindow.ParamNames.IsMarked] is bool marked && marked,
					Deviations(w, tariffs),
					w)
				{
					IsOverflow = mode == TariffWindowWeekMode.Traffic && IsOverflow(w),
					LinkNote = mode == TariffWindowWeekMode.Traffic ? LinkNote(w) : null
				};
				perDay[day]++;
			}

			return rows;
		}

		/// <summary>
		/// Текст ячейки трафика — как RollerIssuesGrid3.GetCellContent у TrafficGrid (неподтверждённые
		/// учитываются): остаток времени, у штучного окна «[осталось/всего]», у перенесённого окна —
		/// фактическое время в скобках (с датой, если перенесено на другой день).
		/// </summary>
		private static string TrafficText(DataRow w)
		{
			int timeLeft = IntOrZero(w[TariffWindow.ParamNames.Duration])
				- IntOrZero(w[TariffWindow.ParamNames.TimeInUseConfirmed])
				- IntOrZero(w[TariffWindow.ParamNames.TimeInUseUnconfirmed]);
			string text = DateTimeUtils.Time2String(timeLeft);

			int maxCapacity = IntOrZero(w[TariffWindow.ParamNames.MaxCapacity]);
			if (maxCapacity > 0)
			{
				int capacityLeft = maxCapacity
					- IntOrZero(w[TariffWindowWithRollerIssues.ParamNames.CapacityInUseConfirmed])
					- IntOrZero(w[TariffWindowWithRollerIssues.ParamNames.CapacityInUseUnconfirmed]);
				text = string.Format("{0} [{1}/{2}]", text, capacityLeft, maxCapacity);
			}

			DateTime original = (DateTime)w[TariffWindow.ParamNames.WindowDateOriginal];
			DateTime actual = (DateTime)w[TariffWindow.ParamNames.WindowDateActual];
			if (actual.Date != original.Date)
				text += actual.ToString(" (dd.MM.yy HH:mm)");
			else if (actual.Hour != original.Hour || actual.Minute != original.Minute)
				text += actual.ToString(" (HH:mm)");
			return text;
		}

		/// <summary>
		/// Переполнение — занято подтверждёнными больше, чем есть (TrafficGrid.CheckWindowOverflow):
		/// окно укоротили или перенесли в него выпуски сверх длительности/вместимости.
		/// </summary>
		private static bool IsOverflow(DataRow w)
		{
			int maxCapacity = IntOrZero(w[TariffWindow.ParamNames.MaxCapacity]);
			return (maxCapacity > 0 && IntOrZero(w[TariffWindowWithRollerIssues.ParamNames.CapacityInUseConfirmed]) > maxCapacity)
				|| IntOrZero(w[TariffWindow.ParamNames.TimeInUseConfirmed]) > IntOrZero(w[TariffWindow.ParamNames.Duration]);
		}

		/// <summary>Склейка с соседним окном (windowPrevId / windowNextId, docs/window-merging.md).</summary>
		private static string LinkNote(DataRow w)
		{
			bool prev = w.Table.Columns.Contains(TariffWindowWithRollerIssues.ParamNames.WindowPrevId)
				&& w[TariffWindowWithRollerIssues.ParamNames.WindowPrevId] != DBNull.Value;
			bool next = w.Table.Columns.Contains(TariffWindowWithRollerIssues.ParamNames.WindowNextId)
				&& w[TariffWindowWithRollerIssues.ParamNames.WindowNextId] != DBNull.Value;
			if (prev && next) return Tr.T("склеено с предыдущим и следующим окном");
			if (prev) return Tr.T("склеено с предыдущим окном");
			if (next) return Tr.T("склеено со следующим окном");
			return null;
		}

		/// <summary>Выпуски окна для панели трафика — WindowIssuesRetrieve, неподтверждённые тоже.</summary>
		public static DataTable LoadIssues(TariffWindowCell cell)
		{
			return TariffWindowWithRollerIssues.LoadIssues(true, cell.WindowId);
		}

		/// <summary>Сущность «Выпуск» с набором колонок трафик-менеджера (как grdSelectedCellIssues).</summary>
		public static Entity TrafficIssueEntity()
		{
			Entity entity = (Entity)EntityManager.GetEntity((int)Entities.Issue).Clone();
			entity.AttributeSelector = (int)RollerIssue.AttributeSelectors.TrafficManager;
			return entity;
		}

		/// <summary>
		/// Чем окно отличается от своего тарифа — точечные правки окна (карточка окна, «Изменить
		/// цену», трафик-менеджмент). Время выхода сравнивается не с тарифом, а с расписанием
		/// самого окна: оригинальное время и есть время тарифа.
		/// </summary>
		private static List<string> Deviations(DataRow w, Dictionary<int, DataRow> tariffs)
		{
			List<string> result = new List<string>();

			DateTime original = (DateTime)w[TariffWindow.ParamNames.WindowDateOriginal];
			DateTime actual = (DateTime)w[TariffWindow.ParamNames.WindowDateActual];
			if (actual != original)
				result.Add(Tr.Format("время выхода {0:HH:mm}{1}, по расписанию {2:HH:mm}",
					actual, actual.Date != original.Date ? actual.ToString(" (dd.MM)") : "", original));

			if (w[Tariff.ParamNames.TariffId] == DBNull.Value
				|| !tariffs.TryGetValue(Convert.ToInt32(w[Tariff.ParamNames.TariffId]), out DataRow t))
				return result;

			CompareSeconds(result, Tr.T("продолжительность"), w, t, TariffWindow.ParamNames.Duration);
			CompareSeconds(result, Tr.T("полная продолжительность"), w, t, TariffWindow.ParamNames.DurationTotal);

			decimal windowPrice = Convert.ToDecimal(w[TariffWindow.ParamNames.Price]);
			decimal tariffPrice = Convert.ToDecimal(t[TariffWindow.ParamNames.Price]);
			if (windowPrice != tariffPrice)
				result.Add(Tr.Format("цена {0:C}, по тарифу {1:C}", windowPrice, tariffPrice));

			int windowCapacity = IntOrZero(w[TariffWindow.ParamNames.MaxCapacity]);
			int tariffCapacity = IntOrZero(t[TariffWindow.ParamNames.MaxCapacity]);
			if (windowCapacity != tariffCapacity)
				result.Add(Tr.Format("вместимость {0}, по тарифу {1}", windowCapacity, tariffCapacity));

			return result;
		}

		private static void CompareSeconds(List<string> result, string caption, DataRow w, DataRow t, string column)
		{
			if (!w.Table.Columns.Contains(column) || !t.Table.Columns.Contains(column)
				|| w[column] == DBNull.Value || t[column] == DBNull.Value)
				return;

			int windowValue = Convert.ToInt32(w[column]);
			int tariffValue = Convert.ToInt32(t[column]);
			if (windowValue != tariffValue)
				result.Add(Tr.Format("{0} {1}, по тарифу {2}", caption,
					DateTimeUtils.Time2String(windowValue), DateTimeUtils.Time2String(tariffValue)));
		}

		private static int IntOrZero(object value)
		{
			return value == DBNull.Value ? 0 : Convert.ToInt32(value);
		}

		/// <summary>
		/// Объект окна для карточки и меню действий — из строки, которую уже вернула
		/// TariffWindowRetrieve (как ObjectList поднимает объект из строки списка), без
		/// отдельного чтения.
		/// </summary>
		public static PresentationObject CreateWindowObject(TariffWindowCell cell)
		{
			return EntityManager.GetEntity((int)Entities.TariffWindow).CreateObject(cell.Row);
		}

		// ---------- «Изменить цену» у строки времени ----------

		/// <summary>Именованный паспорт (iPassport) — тот же, что у TariffWindowGrid.ChangePrice.</summary>
		public const string ChangePricePassport = "ChangeTariffWindowsPrice";
		private const string ChangePriceProcedure = "TariffWindowChangePrice";
		private const string NewPriceParam = "newPrice";
		private const string TimeParam = "time";

		/// <summary>
		/// Черновик для паспорта «Изменить цену»: время и цена строки, интервал — от сегодня (не
		/// раньше начала прайс-листа) до конца прайс-листа. Десктоп всегда ставит сегодня и
		/// потом сам же отказывает, если прайс-лист ещё не начался.
		/// </summary>
		public static PresentationObject CreateChangePriceDraft(object pricelist, TariffWindowRow row)
		{
			Pricelist p = (Pricelist)pricelist;
			PresentationObject draft = EntityManager.GetEntity((int)Entities.TariffWindow).NewObject;
			draft[TimeParam] = new DateTime(1900, 1, 1).Add(row.TimeOfDay);
			draft[TariffWindow.ParamNames.Price] = row.Price;
			draft[NewPriceParam] = row.Price;
			draft[Pricelist.ParamNames.StartDate] = DateTime.Today < p.StartDate ? p.StartDate : DateTime.Today;
			draft[Pricelist.ParamNames.FinishDate] = p.FinishDate;
			draft[Pricelist.ParamNames.PricelistId] = p.PricelistId;
			return draft;
		}

		/// <summary>Проверки десктопного TariffWindowGrid.ValidatePassportData, те же тексты. null — всё верно.</summary>
		public static string ValidateChangePrice(object pricelist, Dictionary<string, object> values)
		{
			Pricelist p = (Pricelist)pricelist;
			DateTime startDate = Convert.ToDateTime(values[Pricelist.ParamNames.StartDate]);
			DateTime finishDate = Convert.ToDateTime(values[Pricelist.ParamNames.FinishDate]);

			if (Convert.ToDecimal(values[TariffWindow.ParamNames.Price]) == Convert.ToDecimal(values[NewPriceParam]))
				return Tr.T(Properties.Resources.NewPriceShouldBeDifferent);
			if (startDate > finishDate)
				return MessageAccessor.GetMessage("StartFinishWindowTimeError");
			if (startDate < p.StartDate)
				return Tr.Format(Properties.Resources.StartDateShouldBeInsidePricelistDates, p.StartDate.ToShortDateString());
			if (finishDate > p.FinishDate)
				return Tr.Format(Properties.Resources.FinishDateShouldBeInsidePricelistDates, p.FinishDate.ToShortDateString());
			return null;
		}

		/// <summary>Меняет цену окон строки в интервале (TariffWindowChangePrice — по оригинальному времени окна).</summary>
		public static void ChangePrice(Dictionary<string, object> values)
		{
			DataAccessor.ExecuteNonQuery(ChangePriceProcedure, values);
		}

		private static string RowKey(int hour, int min, decimal price)
		{
			return hour + ":" + min + "|" + price;
		}

		// То же, что TariffWindowGrid.GetCellContent: длительность окна, у штучного — [вместимость].
		private static string CellText(DataRow w)
		{
			string duration = DateTimeUtils.Time2String(Convert.ToInt32(w[TariffWindow.ParamNames.Duration]));
			int maxCapacity = w[TariffWindow.ParamNames.MaxCapacity] == DBNull.Value
				? 0 : Convert.ToInt32(w[TariffWindow.ParamNames.MaxCapacity]);
			return maxCapacity == 0 ? duration : string.Format("{0} [{1}]", duration, maxCapacity);
		}
	}

	/// <summary>Строка недели: тарифное время и цена, семь ячеек (null — окна в этот день нет).</summary>
	public sealed class TariffWindowRow
	{
		public TariffWindowRow(TimeSpan timeOfDay, decimal price)
		{
			TimeOfDay = timeOfDay;
			Time = DateTimeUtils.Time2String(timeOfDay.Hours, timeOfDay.Minutes);
			Price = price;
			Cells = new TariffWindowCell[TariffWindowWeek.DaysInWeek];
		}

		/// <summary>Тарифное (оригинальное) время строки.</summary>
		public TimeSpan TimeOfDay { get; }
		public string Time { get; }
		public decimal Price { get; }
		public TariffWindowCell[] Cells { get; }
	}

	/// <summary>Одно рекламное окно в сетке недели.</summary>
	public sealed class TariffWindowCell
	{
		internal TariffWindowCell(int windowId, DateTime windowDate, string text, bool isDisabled, bool isMarked,
			IReadOnlyList<string> deviations, DataRow row)
		{
			WindowId = windowId;
			WindowDate = windowDate;
			Text = text;
			IsDisabled = isDisabled;
			IsMarked = isMarked;
			Deviations = deviations;
			Row = row;
		}

		/// <summary>Чем окно отличается от своего тарифа; пусто — совпадает.</summary>
		public IReadOnlyList<string> Deviations { get; }
		public bool IsModified => Deviations.Count > 0;

		/// <summary>Трафик: занято подтверждёнными больше, чем есть.</summary>
		public bool IsOverflow { get; internal set; }

		/// <summary>Трафик: склеено с соседним окном — пояснение; null — не склеено.</summary>
		public string LinkNote { get; internal set; }

		/// <summary>Строка TariffWindowRetrieve — из неё поднимается объект окна.</summary>
		internal DataRow Row { get; }

		/// <summary>Тарифное (оригинальное) время окна — строка сетки.</summary>
		public TimeSpan OriginalTime
		{
			get
			{
				DateTime original = (DateTime)Row[TariffWindow.ParamNames.WindowDateOriginal];
				return new TimeSpan(original.Hour, original.Minute, 0);
			}
		}

		/// <summary>День окна по расписанию (dayOriginal) — колонка сетки.</summary>
		public DateTime OriginalDay => ((DateTime)Row[TariffWindow.ParamNames.WindowDateOriginal]).Date;

		public int WindowId { get; }
		public DateTime WindowDate { get; }
		public string Text { get; }
		public bool IsDisabled { get; }
		public bool IsMarked { get; }
	}
}
