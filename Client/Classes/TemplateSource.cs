using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;

namespace Merlin.Classes
{
	/// <summary>
	/// Запуск Шаблонов 1–3 на вкладке «Рекламные окна» (docs/tasks/web-action-forms.md, этап 5) — то, что в
	/// десктопе делают FrmGenerator и FrmTemplate/2/3. Здесь — линейная кампания (одна станция); веер — 5.5.
	///
	/// Как устроен запуск (решения этапа 5): окна периода читаются заранее, одним вызовом на прайс-лист;
	/// выбор окон каждого дня — чистый <see cref="TemplatePlanner"/> с теми же правилами, что у десктопа;
	/// запись — по дням, каждый день в своей транзакции (дни независимы, как в десктопе, но блокировки
	/// держатся доли секунды). Бизнес-отказ шага — строка таблицы, запуск идёт дальше; любая другая
	/// ошибка (взаимоблокировка, таймаут) откатывает день и прерывает запуск: выпуски прежних дней остаются.
	/// Пересчёт акции — один, в конце. Права — как у остальных записей вкладки (LoadForWrite).
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class TemplateSource
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(TemplateSource));

		/// <summary>Итог запуска: поставленные выпуски (для «Отменить последнее»), счётчики и таблица замечаний.</summary>
		public sealed class TemplateResult
		{
			internal TemplateResult()
			{
				Errors = new DataTable();
				Errors.Columns.Add("name", typeof(string));
				Errors.Columns.Add("description", typeof(string));
			}

			/// <summary>Поставлено выпусков.</summary>
			public int Done { get; internal set; }

			/// <summary>Не поставлено: отказы записи плюс нехватка окон (сколько выпусков не нашло окна).</summary>
			public int NotAdded { get; internal set; }

			/// <summary>Сколько дней периода обработано (при сбое — до дня, на котором запуск прерван).</summary>
			public int Days { get; internal set; }

			public List<WindowsSource.PlacedIssue> Created { get; } = new List<WindowsSource.PlacedIssue>();

			/// <summary>name — день или окно, description — отказ, нехватка окон, прерывание запуска.</summary>
			public DataTable Errors { get; }

			internal void Note(string name, string description, int notAdded = 0)
			{
				Errors.Rows.Add(name, description);
				NotAdded += notAdded;
			}
		}

		/// <summary>
		/// Шаблон 2 («Шаблон для внесения роликов на интервале времени»): ролик кисти с позицией «Позиционирования»
		/// в окна интервала каждого дня периода — «в день» или отдельно в прайм и офф-прайм, самые свободные
		/// окна первыми (десктоп — FrmGenerator.AddSimpleIssues).
		/// </summary>
		public static TemplateResult RunInterval(WindowsSource.OpenCampaign c, PlacementTemplate template, int rollerId, int position)
		{
			Campaign campaign = WindowsSource.LoadForWrite(c);
			Roller roller = WindowsSource.BrushRoller(c, rollerId, campaign.Action.FirmID);
			List<DateTime> dates = template.Dates();
			TemplateResult result = new TemplateResult();
			if (dates.Count == 0)
				return result;

			using (OperationScope.Start($"TemplateInterval campaign={c.CampaignId} days={dates.Count}"))
			{
				Stopwatch sw = Stopwatch.StartNew();
				Random rnd = new Random();
				Period period = LoadPeriod(c, dates, template);
				int startMinute = (int)template.StartTime.TotalMinutes;
				int finishMinute = (int)template.FinishTime.TotalMinutes;

				// Подряд идущие дни вне прайс-листов станции — одна строка таблицы («дата – дата»), а не по строке на день.
				DateTime? missingFrom = null, missingTo = null;
				int missingDays = 0;
				void FlushMissing()
				{
					if (missingFrom == null)
						return;
					string range = missingFrom == missingTo ? missingFrom.Value.ToString("dd.MM.yyyy")
						: missingFrom.Value.ToString("dd.MM.yyyy") + " – " + missingTo.Value.ToString("dd.MM.yyyy");
					result.Note(range, MessageAccessor.GetMessage("PriceListDoesntExist"), WantedPerDay(template) * missingDays);
					missingFrom = missingTo = null;
					missingDays = 0;
				}

				for (int dayIndex = 0; dayIndex < dates.Count; dayIndex++)
				{
					DateTime day = dates[dayIndex];
					string dayText = day.ToString("dd.MM.yyyy");
					if (!period.Rows.TryGetValue(day, out List<DataRow> rows))
					{
						missingFrom = missingFrom ?? day;
						missingTo = day;
						missingDays++;
						continue;
					}
					FlushMissing();

					// Те же отборы, что у десктопа: интервал (у линейки конец включается), занятая подтверждённым выпуском
					// позиция (макеты не смотрятся), ролики этой фирмы в окне (с макетами, любой акции фирмы).
					List<TemplateWindow> candidates = TemplatePlanner.InTimeRange(
							rows.Select(r => ToWindow(r, day, period)), startMinute, finishMinute, finishInclusive: true)
						.Where(w => !(position != (int)RollerPositions.Undefined && PositionOccupied(period.RowOf(w.Key), (RollerPositions)position))
							&& !(template.IgnoreFirmWindows && period.FirmWindows.Contains(w.Key)))
						.ToList();

					List<TemplatePick> picks = TemplatePlanner.PickDay(candidates, template.Quantity, template.QuantityPrime, template.QuantityOffPrime, rnd);
					foreach (TemplatePick pick in picks.Where(p => p.IsShort))
						result.Note(dayText, ShortNote(pick), pick.Wanted - pick.Windows.Count);

					// По возрастанию номера окна — один порядок блокировок у всех пачек.
					List<TemplateWindow> toWrite = picks.SelectMany(p => p.Windows).OrderBy(w => w.Key).ToList();
					if (toWrite.Count == 0)
					{
						result.Days++;
						continue;
					}

					List<DayWrite> writes = toWrite
						.Select(w => new DayWrite(w.Key, w.Start, new TariffWindowWithRollerIssues(period.RowOf(w.Key), Entities.TariffWindow))).ToList();
					if (!WriteDay(c, campaign, roller, position, writes, dayText, dates.Count - dayIndex, result))
						break;
				}
				FlushMissing();

				Recalculate(c, campaign, result);

				// Параметры шаблона — в строке итога: по логу видно, что просили и что получилось (пользователь и сеанс — в шаблоне строки лога).
				string daysText = template.ByParity ? (template.Odd ? "odd" : "even") : "days=" + string.Concat(template.WeekDays.Select(on => on ? "1" : "0"));
				Log.Info($"Template interval completed in {sw.ElapsedMilliseconds}ms campaign={c.CampaignId} roller={rollerId} position={position}" +
					$" period={template.StartDate:yyyy-MM-dd}..{template.FinishDate:yyyy-MM-dd} {daysText}" +
					$" time={template.StartTime.ToString(@"hh\:mm")}-{template.FinishTime.ToString(@"hh\:mm")}" +
					$" quantity={template.Quantity}/{template.QuantityPrime}/{template.QuantityOffPrime} ignoreFirm={template.IgnoreFirmWindows}" +
					$" days={result.Days}/{dates.Count} added={result.Done} notAdded={result.NotAdded}");
			}
			return result;
		}

		/// <summary>Окно, в которое день пишет выпуск: номер, время (для записи «Отменить») и объект ядра.</summary>
		private sealed class DayWrite
		{
			public DayWrite(int windowId, DateTime start, TariffWindowWithRollerIssues window)
			{
				WindowId = windowId;
				Start = start;
				Window = window;
			}

			public int WindowId { get; }
			public DateTime Start { get; }
			public TariffWindowWithRollerIssues Window { get; }
		}

		/// <summary>
		/// Один день в одной транзакции: выпуски в окна по порядку, отказ шага — строка таблицы, остальные пишутся.
		/// Взаимоблокировка откатывает день целиком — он повторяется один раз; иной сбой откатывает день и прерывает
		/// запуск (false): выпуски прежних дней остаются, в строке — сколько дней не обработано (<paramref name="daysLeft"/>
		/// считает и этот).
		/// </summary>
		private static bool WriteDay(WindowsSource.OpenCampaign c, Campaign campaign, Roller roller, int position, List<DayWrite> writes,
			string dayText, int daysLeft, TemplateResult result)
		{
			WindowsSource.BatchResult dayResult = null;
			Exception failure = null;
			for (int attempt = 0; attempt < 2; attempt++)
			{
				dayResult = new WindowsSource.BatchResult();
				failure = null;
				try
				{
					WindowsSource.RunInTransaction(() =>
					{
						foreach (DayWrite w in writes)
							WindowsSource.Step(dayResult, w.Start.ToString("dd.MM.yyyy HH:mm"), () =>
							{
								dayResult.Created.Add(WindowsSource.Capture(c, campaign.AddIssue(roller, w.Window, (RollerPositions)position, null), w.WindowId, w.Start, roller));
								return null;
							});
					});
					break;
				}
				catch (SqlException ex) when (ex.Number == 1205 && attempt == 0)
				{
					// Жертва взаимоблокировки: день откатан целиком и ничего не оставил — день повторяется один раз.
					Log.Warn($"Template: deadlock on {dayText} (campaign {c.CampaignId}), retrying the day", ex);
				}
				catch (Exception ex)
				{
					failure = ex;
					break;
				}
			}
			if (failure != null)
			{
				// День откатан целиком; прежние дни остаются. Запуск прерывается — следующие дни, скорее всего,
				// упрутся в то же (сеть, таймаут), и ждать их по таймауту каждый нельзя.
				Log.Error($"Template interrupted on {dayText} (campaign {c.CampaignId})", failure);
				Interrupted(result, dayText, failure, daysLeft, writes.Count);
				return false;
			}

			result.Days++;
			result.Done += dayResult.Done;
			result.NotAdded += dayResult.Failed;
			result.Created.AddRange(dayResult.Created);
			foreach (DataRow refused in dayResult.Errors.Rows)
				result.Errors.Rows.Add(refused["name"], refused["description"]);
			return true;
		}

		private static void Interrupted(TemplateResult result, string name, Exception failure, int daysLeft, int notAdded) =>
			result.Note(name, Tr.Format("Запуск прерван: {0} Выпуски этого дня не добавлены, добавленные ранее — сохранены. Не обработано дней: {1}.",
				ErrorManager.GetErrorMessage(failure), daysLeft), notAdded);

		/// <summary>Один пересчёт акции после запуска, отдельной транзакцией; сбой — строка в таблице (выпуски уже записаны).</summary>
		private static void Recalculate(WindowsSource.OpenCampaign c, Campaign campaign, TemplateResult result)
		{
			if (result.Done == 0)
				return;
			try
			{
				WindowsSource.RunInTransaction(() => campaign.RecalculateAction(false));
			}
			catch (Exception ex)
			{
				Log.Error($"Template: recalculation failed (campaign {c.CampaignId})", ex);
				result.Note("", Tr.Format("Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.", ErrorManager.GetErrorMessage(ex)));
			}
		}

		/// <summary>
		/// Шаблон 1 («Шаблон для внесения роликов», десктоп — FrmTemplate): ролик кисти с позицией «Позиционирования» по
		/// одному выпуску в день периода в окно, накрывающее время <see cref="PlacementTemplate.StartTime"/> (десктоп —
		/// FrmGenerator.AddSimpleIssue: время даёт щелчок по окну, здесь оно в диалоге). Окно ищется так же, как у десктопа
		/// (TariffWindowRetrieve @actualDate: ближайшее по началу, которое начинается не позже времени и кончается не раньше),
		/// без отбора по прайс-листу и блокировке: заблокированное окно откажет сама запись. Режима «Удалить выбранные» нет.
		/// </summary>
		public static TemplateResult RunSingle(WindowsSource.OpenCampaign c, PlacementTemplate template, int rollerId, int position)
		{
			Campaign campaign = WindowsSource.LoadForWrite(c);
			Roller roller = WindowsSource.BrushRoller(c, rollerId, campaign.Action.FirmID);
			List<DateTime> dates = template.Dates();
			TemplateResult result = new TemplateResult();
			if (dates.Count == 0)
				return result;

			using (OperationScope.Start($"TemplateSingle campaign={c.CampaignId} days={dates.Count}"))
			{
				Stopwatch sw = Stopwatch.StartNew();
				Massmedia massmedia = Massmedia.GetMassmediaByID(c.MassmediaId);
				int firmId = campaign.Action.FirmID;
				for (int dayIndex = 0; dayIndex < dates.Count; dayIndex++)
				{
					DateTime moment = dates[dayIndex].Date + template.StartTime;
					string name = moment.ToString("dd.MM.yyyy HH:mm");

					// Чтение — вне транзакции дня; отказы по правилам десктопа (FrmGenerator.AddSimpleIssue) — строки таблицы.
					TariffWindowWithRollerIssues window;
					string refusal;
					try
					{
						window = TariffWindowWithRollerIssues.GetWindowByDate(moment, massmedia) as TariffWindowWithRollerIssues;
						refusal = window == null ? MessageAccessor.GetMessage("TariffWindowNotFound")
							: template.IgnoreFirmWindows && window.IsRollerOfTheFirmExist(firmId, true) ? MessageAccessor.GetMessage("IssueWithTheSameFirmExists")
							: position != (int)RollerPositions.Undefined && PositionOccupied(window, (RollerPositions)position) ? MessageAccessor.GetMessage("FirstLastIssueError")
							: null;
					}
					catch (Exception ex)
					{
						Log.Error($"Template single interrupted on {name} (campaign {c.CampaignId})", ex);
						Interrupted(result, name, ex, dates.Count - dayIndex, 1);
						break;
					}
					if (refusal != null)
					{
						result.Days++;
						result.Note(name, refusal, 1);
						continue;
					}

					List<DayWrite> writes = new List<DayWrite> { new DayWrite(window.WindowId, window.WindowDate, window) };
					if (!WriteDay(c, campaign, roller, position, writes, name, dates.Count - dayIndex, result))
						break;
				}

				Recalculate(c, campaign, result);

				string daysText = template.ByParity ? (template.Odd ? "odd" : "even") : "days=" + string.Concat(template.WeekDays.Select(on => on ? "1" : "0"));
				Log.Info($"Template single completed in {sw.ElapsedMilliseconds}ms campaign={c.CampaignId} roller={rollerId} position={position}" +
					$" period={template.StartDate:yyyy-MM-dd}..{template.FinishDate:yyyy-MM-dd} {daysText} time={template.StartTime.ToString(@"hh\:mm")}" +
					$" ignoreFirm={template.IgnoreFirmWindows} days={result.Days}/{dates.Count} added={result.Done} notAdded={result.NotAdded}");
			}
			return result;
		}

		/// <summary>Сколько выпусков в день просит шаблон.</summary>
		private static int WantedPerDay(PlacementTemplate t) => t.Quantity > 0 ? t.Quantity : t.QuantityPrime + t.QuantityOffPrime;

		/// <summary>Тексты десктопа (FrmGenerator.AddSimpleIssues): «Окон» — сколько окон нашлось, «выпусков» — сколько просили.</summary>
		private static string ShortNote(TemplatePick pick)
		{
			switch (pick.Group)
			{
				case TemplateGroup.Prime:
					return Tr.Format("Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков {1}.", pick.Windows.Count, pick.Wanted);
				case TemplateGroup.OffPrime:
					return Tr.Format("Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков для добавления {1}.", pick.Windows.Count, pick.Wanted);
				default:
					return Tr.Format("Недостаточно рекламных окон для размещения всех выпусков для добавления. Окон: {0}, выпусков {1}.", pick.Windows.Count, pick.Wanted);
			}
		}

		// ---------- Чтение периода ----------

		/// <summary>Окна периода по дням (TariffWindowRetrieve), окна фирмы и максимальные цены дней — всё, что нужно планировщику.</summary>
		private sealed class Period
		{
			/// <summary>День (dayOriginal) → окна дня, как их вернул TariffWindowRetrieve; только дни с прайс-листом.</summary>
			public readonly Dictionary<DateTime, List<DataRow>> Rows = new Dictionary<DateTime, List<DataRow>>();
			public readonly HashSet<int> FirmWindows = new HashSet<int>();
			public readonly Dictionary<DateTime, decimal> MaxPrice = new Dictionary<DateTime, decimal>();
			private readonly Dictionary<int, DataRow> _byId = new Dictionary<int, DataRow>();

			public void Add(DateTime day, DataRow row)
			{
				if (!Rows.TryGetValue(day, out List<DataRow> list))
					Rows[day] = list = new List<DataRow>();
				list.Add(row);
				_byId[Convert.ToInt32(row[TariffWindow.ParamNames.WindowId])] = row;
			}

			public DataRow RowOf(int windowId) => _byId[windowId];
		}

		private static Period LoadPeriod(WindowsSource.OpenCampaign c, List<DateTime> dates, PlacementTemplate template)
		{
			Massmedia massmedia = Massmedia.GetMassmediaByID(c.MassmediaId);
			Period period = new Period();

			// Даты по прайс-листам: GetPriceList даёт покрывающий прайс-лист или ближайший будущий — такой дату не покрывает
			// (окон на неё нет), день уйдёт отказом «На указанную дату отсутствует прайс-лист».
			Dictionary<int, (MassmediaPricelist List, List<DateTime> Days)> byList = new Dictionary<int, (MassmediaPricelist, List<DateTime>)>();
			MassmediaPricelist current = null;
			foreach (DateTime day in dates)
			{
				// Прайс-лист спрашивается, только когда прошлый кончился (даты идут по возрастанию): «ближайший будущий» ещё не
				// начался — дней до его начала в прайс-листах нет, повторно его не спрашиваем; «нет» — значит, дальше нет вовсе.
				if (current == null || day > current.FinishDate.Date)
				{
					current = massmedia.GetPriceList(day) as MassmediaPricelist;
					if (current == null)
						break;
				}
				if (day < current.StartDate.Date)
					continue;
				if (!byList.TryGetValue(current.PricelistId, out var entry))
					byList[current.PricelistId] = entry = (current, new List<DateTime>());
				entry.Days.Add(day);
			}

			foreach ((MassmediaPricelist list, List<DateTime> days) in byList.Values)
			{
				DateTime first = days[0], last = days[days.Count - 1];

				// Как RollerIssuesGrid3 и десктопный генератор: без особых окон и тарифов «только для модулей», без
				// заблокированных окон; время окна — фактическое.
				list.ExcludeSpecialWindows = true;
				list.ExcludeModuleTariffs = true;
				DataTable windows = list.GetTariffWindows(first, last, null, false, false, useActualTime: true).Tables[Constants.TableNames.Data];
				HashSet<DateTime> wanted = new HashSet<DateTime>(days);
				foreach (DataRow row in windows.Rows)
				{
					DateTime day = ((DateTime)row[TariffWindow.ParamNames.WindowDateOriginal]).Date;
					if (wanted.Contains(day))
						period.Add(day, row);
				}
				foreach (DateTime day in days)
					if (!period.Rows.ContainsKey(day))
						period.Rows[day] = new List<DataRow>();

				// «Ролики этой фирмы в окне»: окна, где у фирмы есть выпуски любой её акции, подтверждённые и макеты, включая эту
				// (десктоп — IsRollerOfTheFirmExist(firm, true) на каждое окно). Процедура Grid отдаёт их набором; месяцами —
				// как просит процедура на длинных периодах.
				if (template.IgnoreFirmWindows)
					for (DateTime from = first; from <= last; from = from.AddDays(31))
					{
						DateTime to = from.AddDays(30) < last ? from.AddDays(30) : last;
						DataSet cells = massmedia.GetRollerCells(list, from, to, null, true, WindowsSource.CampaignRef(c), RollerPositions.Undefined);
						DataTable firm = cells.Tables[Constants.TableNames.WindowsWithThisFirmIssue];
						if (firm != null)
							foreach (DataRow row in firm.Rows)
								period.FirmWindows.Add(ParseHelper.GetInt32FromObject(row[TariffWindow.ParamNames.WindowId], 0));
					}
			}

			// Прайм линейки — окно с максимальной ценой дня по ВСЕМ окнам станции (GetMaxPriceByDay), нужно только при делении на прайм.
			if (template.Quantity == 0 && dates.Count > 0)
				foreach (DataRow row in massmedia.GetMaxPriceByDay(dates[0], dates[dates.Count - 1]).Rows)
				{
					DateTime day = ParseHelper.GetDateTimeFromObject(row["Date"], DateTime.MinValue);
					if (day != DateTime.MinValue)
						period.MaxPrice[day.Date] = ParseHelper.GetDecimalFromObject(row["Price"], 0m);
				}
			return period;
		}

		private static TemplateWindow ToWindow(DataRow row, DateTime day, Period period) => new TemplateWindow
		{
			Key = Convert.ToInt32(row[TariffWindow.ParamNames.WindowId]),
			Start = (DateTime)row[TariffWindow.ParamNames.WindowDateActual],
			// Остаток с неподтверждёнными (TariffWindow.TimeLeftWithUnconfirmed) — независимо от «Учитывать макеты».
			FreeSeconds = WindowsSource.IntOrZero(row[TariffWindow.ParamNames.Duration])
				- WindowsSource.IntOrZero(row[TariffWindow.ParamNames.TimeInUseConfirmed])
				- WindowsSource.IntOrZero(row[TariffWindow.ParamNames.TimeInUseUnconfirmed]),
			IsPrime = period.MaxPrice.TryGetValue(day, out decimal max) && Convert.ToDecimal(row[TariffWindow.ParamNames.Price]) == max,
		};

		/// <summary>То же для окна ядра (FrmGenerator.IsPositionOccupied).</summary>
		private static bool PositionOccupied(TariffWindowWithRollerIssues window, RollerPositions position)
		{
			switch (position)
			{
				case RollerPositions.First: return window.IsFirstPositionOccupied;
				case RollerPositions.Second: return window.IsSecondPositionOccupied;
				case RollerPositions.Last: return window.IsLastPositionOccupied;
				default: return false;
			}
		}

		/// <summary>Позиция занята подтверждённым выпуском — флаги окна, как FrmGenerator.IsPositionOccupied.</summary>
		private static bool PositionOccupied(DataRow row, RollerPositions position)
		{
			switch (position)
			{
				case RollerPositions.First: return ParseHelper.GetBooleanFromObject(row["isFirstPositionOccupied"], false);
				case RollerPositions.Second: return ParseHelper.GetBooleanFromObject(row["isSecondPositionOccupied"], false);
				case RollerPositions.Last: return ParseHelper.GetBooleanFromObject(row["isLastPositionOccupied"], false);
				default: return false;
			}
		}
	}
}
