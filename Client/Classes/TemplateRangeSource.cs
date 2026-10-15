using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using log4net;

namespace Merlin.Classes
{
	/// <summary>
	/// Запуск Шаблонов 1–3 на веере — вкладка «Рекламные окна» с несколькими отмеченными станциями
	/// (docs/tasks/web-action-forms.md, этап 5.5). Десктоп — TariffWithRangeGrid.AddIssuesRange (Шаблон 1),
	/// AddIssuesRangeTimePeriod (Шаблон 2) и AddIssuesRangeTimePeriodMultiRoller (Шаблон 3) под FrmGenerator.
	///
	/// Устройство то же, что у линейки (<see cref="TemplateSource"/>): день — одна транзакция, шаг — одно размещение, бизнес-отказ —
	/// строка таблицы, взаимоблокировка — один повтор дня, иной сбой прерывает запуск, один пересчёт акции в конце. Отличия
	/// веера: единица размещения — получасовой слот (TariffWindowWithRange: он есть у каждой отмеченной станции), одно размещение —
	/// AddRangeIssues, то есть выпуск на каждой станции сразу (<see cref="TemplateSource.TemplateResult.Done"/> — число слотов);
	/// слоты недели читаются одним вызовом на неделю; конец интервала исключается (у линейки включается); прайм — признак слота;
	/// номера поставленных выпусков процедура не возвращает — «Отменить последнее» берёт их разностью RangeSlotIssues до и
	/// после записи дня, в той же транзакции.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class TemplateRangeSource
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(TemplateRangeSource));

		/// <summary>
		/// Шаблон 2: ролик кисти с позицией «Позиционирования» в слоты интервала каждого дня — «в день» или отдельно в прайм и
		/// офф-прайм, самые свободные слоты первыми.
		/// </summary>
		public static TemplateSource.TemplateResult RunInterval(RangeSource.OpenRange r, PlacementTemplate template, int rollerId, int position,
			WindowsSource.WeekView view)
		{
			ActionOnMassmedia action = RangeSource.LoadForWrite(r);
			Roller roller = WindowsSource.BrushRoller(r, rollerId, action.FirmID);
			return RunIntervalCore(r, action, template, position, view, "interval", roller, null, "roller=" + rollerId);
		}

		/// <summary>
		/// Шаблон 3: те же слоты и правила выбора, ролики — из очереди «ролик × количество» (<see cref="RollerAllocationQueue"/>), как у
		/// линейки: на группу слотов дня берётся столько роликов, сколько нашлось слотов; не поставленное из-за нехватки слотов
		/// остаётся в очереди и входит в итоговую строку «Недостаточно окон за весь период шаблона…». <paramref name="quotas"/> —
		/// «номер ролика → сколько выходов на одной станции».
		/// </summary>
		public static TemplateSource.TemplateResult RunMulti(RangeSource.OpenRange r, PlacementTemplate template, IReadOnlyList<KeyValuePair<int, int>> quotas,
			int position, WindowsSource.WeekView view)
		{
			ActionOnMassmedia action = RangeSource.LoadForWrite(r);
			List<(Roller Roller, int Quantity)> chosen = quotas.Where(q => q.Value > 0)
				.Select(q => (WindowsSource.BrushRoller(r, q.Key, action.FirmID), q.Value)).ToList();
			if (chosen.Count == 0)
				return new TemplateSource.TemplateResult();
			return RunIntervalCore(r, action, template, position, view, "multi", null, new RollerAllocationQueue(chosen),
				"rollers=" + string.Join(",", quotas.Where(q => q.Value > 0).Select(q => q.Key + "x" + q.Value)));
		}

		/// <summary>
		/// Шаблон 1: ролик кисти с позицией «Позиционирования» по одному размещению в день периода в слот, накрывающий время
		/// <see cref="PlacementTemplate.StartTime"/>. Десктоп берёт время из щелчка по слоту (всегда начало получаса) и просит
		/// AddRangeIssues поставить выпуск в этот получас на каждый день; в вебе время вводится вручную и округляется вниз до
		/// получаса. Слоты заранее не читаются — как у десктопа, окно ищет и отказывает сама процедура; отказы — её тексты.
		/// </summary>
		public static TemplateSource.TemplateResult RunSingle(RangeSource.OpenRange r, PlacementTemplate template, int rollerId, int position,
			WindowsSource.WeekView view)
		{
			ActionOnMassmedia action = RangeSource.LoadForWrite(r);
			Roller roller = WindowsSource.BrushRoller(r, rollerId, action.FirmID);
			List<DateTime> dates = template.Dates();
			TemplateSource.TemplateResult result = new TemplateSource.TemplateResult();
			if (dates.Count == 0)
				return result;

			using (OperationScope.Start($"TemplateRangeSingle action={r.ActionId} stations={r.CampaignIds.Count} days={dates.Count}"))
			{
				Stopwatch sw = Stopwatch.StartNew();
				TimeSpan time = TimeSpan.FromMinutes((int)template.StartTime.TotalMinutes / 30 * 30);
				for (int dayIndex = 0; dayIndex < dates.Count; dayIndex++)
				{
					DateTime slot = dates[dayIndex] + time;
					if (!WriteDay(r, position, view, template.IgnoreFirmWindows, new List<DayWrite> { new DayWrite(slot, roller) },
							slot.ToString("dd.MM.yyyy"), dates.Count - dayIndex, result, "single"))
						break;
				}

				TemplateSource.RecalculateCore("action " + r.ActionId, () => action.Recalculate(false), result, "single");
				Log.Info($"Template range single completed in {sw.ElapsedMilliseconds}ms action={r.ActionId} stations={r.CampaignIds.Count} roller={rollerId} position={position}" +
					$" {Describe(template)} ignoreFirm={template.IgnoreFirmWindows} days={result.Days}/{dates.Count} added={result.Done} notAdded={result.NotAdded}");
			}
			return result;
		}

		/// <summary>Общий ход Шаблонов 2 и 3: слоты недели читаются по мере продвижения, день за днём — выбор слотов, ролики, запись.</summary>
		private static TemplateSource.TemplateResult RunIntervalCore(RangeSource.OpenRange r, ActionOnMassmedia action, PlacementTemplate template,
			int position, WindowsSource.WeekView view, string kind, Roller roller, RollerAllocationQueue queue, string rollersText)
		{
			List<DateTime> dates = template.Dates();
			TemplateSource.TemplateResult result = new TemplateSource.TemplateResult();
			if (dates.Count == 0)
				return result;

			using (OperationScope.Start($"TemplateRange{char.ToUpperInvariant(kind[0])}{kind.Substring(1)} action={r.ActionId} stations={r.CampaignIds.Count} days={dates.Count}"))
			{
				Stopwatch sw = Stopwatch.StartNew();
				Random rnd = new Random();
				int startMinute = (int)template.StartTime.TotalMinutes;
				int finishMinute = (int)template.FinishTime.TotalMinutes;
				int wanted = TemplateSource.WantedPerDay(template);
				bool interrupted = false;

				// Даты идут по возрастанию — достаточно держать одну неделю.
				DateTime weekMonday = DateTime.MinValue;
				List<RangeSource.TemplateSlot> week = null;

				// Подряд идущие дни без единого слота (нет прайс-листа, нет общего окна станций) — одна строка «дата – дата».
				// У Шаблона 3 «не добавлено» считает остаток очереди в конце, поэтому на день ничего не прибавляется.
				TemplateSource.RunOfDays missing = new TemplateSource.RunOfDays(result, queue == null ? wanted : 0);

				for (int dayIndex = 0; dayIndex < dates.Count; dayIndex++)
				{
					DateTime day = dates[dayIndex];
					string dayText = day.ToString("dd.MM.yyyy");
					DateTime monday = RangeSource.WeekMonday(day);
					if (monday != weekMonday)
					{
						week = RangeSource.LoadTemplateWeek(r, monday);
						weekMonday = monday;
					}
					List<RangeSource.TemplateSlot> daySlots = week.Where(s => s.Start.Date == day).ToList();
					if (daySlots.Count == 0)
					{
						missing.Add(day, ShortNote(0, wanted));
						continue;
					}
					missing.Flush();

					// Те же отборы, что у десктопа: интервал (конец исключается), позиция свободна на всех станциях слота
					// (с неподтверждёнными при «Учитывать макеты»), без выпусков этой фирмы в слоте.
					List<TemplateWindow> candidates = TemplatePlanner.InTimeRange(
							daySlots
								.Where(s => s.PositionAvailable(position, view.ShowUnconfirmed)
									&& !(template.IgnoreFirmWindows && s.HasFirmIssues(view.ShowUnconfirmed)))
								.Select(s => new TemplateWindow { Key = RangeSource.SlotKey(s.Start), Start = s.Start, FreeSeconds = s.FreeSeconds, IsPrime = s.IsPrime }),
							startMinute, finishMinute, finishInclusive: false)
						.ToList();

					List<TemplatePick> picks = TemplatePlanner.PickDay(candidates, template.Quantity, template.QuantityPrime, template.QuantityOffPrime, rnd);

					// Ролик каждому слоту: один ролик кисти или из очереди (группа за группой, как у десктопа).
					List<DayWrite> writes = new List<DayWrite>();
					foreach (TemplatePick pick in picks)
					{
						List<Roller> rollers = queue?.TakeForToday(pick.Windows.Count);
						for (int k = 0; k < pick.Windows.Count; k++)
						{
							Roller forSlot = queue == null ? roller : (k < rollers.Count ? rollers[k] : null);
							if (forSlot == null)
								break;
							writes.Add(new DayWrite(pick.Windows[k].Start, forSlot));
						}
					}

					int addedBefore = result.Done;
					if (writes.Count > 0)
					{
						// По возрастанию времени слота — один порядок блокировок у всех пачек.
						writes = writes.OrderBy(w => w.Start).ToList();
						if (!WriteDay(r, position, view, template.IgnoreFirmWindows, writes, dayText, dates.Count - dayIndex, result, kind))
						{
							interrupted = true;
							break;
						}
					}
					else
						result.Days++;

					// Нашлось меньше слотов, чем просили: десктоп пишет «Недостаточно слотов» после отказов дня; у Шаблона 3 нехватка
					// слотов в день ещё не потеря — ролики остаются в очереди, итог — строка в конце.
					if (picks.Any(p => p.IsShort))
						result.Note(dayText, ShortNote(result.Done - addedBefore, picks.Sum(p => p.Wanted)),
							queue == null ? picks.Sum(p => Math.Max(0, p.Wanted - p.Windows.Count)) : 0);
				}
				missing.Flush();

				// Не хватило слотов на весь период: что осталось в очереди, поставить некуда (при сбое запуска — не говорим «нет окон»).
				if (queue != null && !interrupted && queue.Count > 0)
					result.Note(dates[dates.Count - 1].ToString("dd.MM.yyyy"),
						Tr.Format("Недостаточно окон за весь период шаблона — не удалось разместить {0} выход(ов) из выбранных роликов.", queue.Count), queue.Count);

				TemplateSource.RecalculateCore("action " + r.ActionId, () => action.Recalculate(false), result, kind);

				Log.Info($"Template range {kind} completed in {sw.ElapsedMilliseconds}ms action={r.ActionId} stations={r.CampaignIds.Count} {rollersText} position={position}" +
					$" {Describe(template)} time={template.StartTime.ToString(@"hh\:mm")}-{template.FinishTime.ToString(@"hh\:mm")}" +
					$" quantity={template.Quantity}/{template.QuantityPrime}/{template.QuantityOffPrime} ignoreFirm={template.IgnoreFirmWindows}" +
					$" days={result.Days}/{dates.Count} added={result.Done} notAdded={result.NotAdded}");
			}
			return result;
		}

		private static string Describe(PlacementTemplate template) =>
			$"period={template.StartDate:yyyy-MM-dd}..{template.FinishDate:yyyy-MM-dd} " +
			(template.ByParity ? (template.Odd ? "odd" : "even") : "days=" + string.Concat(template.WeekDays.Select(on => on ? "1" : "0")));

		/// <summary>Десктопный текст веера (FrmGenerator.AddIssues): «Добавлено» — сколько слотов занято, «требовалось» — сколько просили.</summary>
		private static string ShortNote(int added, int expected) =>
			Tr.Format("Недостаточно слотов для размещения. Добавлено: {0}, требовалось: {1}.", added, expected);

		/// <summary>Слот дня и ролик, который в него ставится.</summary>
		private sealed class DayWrite
		{
			public DayWrite(DateTime start, Roller roller)
			{
				Start = start;
				Roller = roller;
			}

			public DateTime Start { get; }
			public Roller Roller { get; }
		}

		/// <summary>
		/// Один день в одной транзакции: AddRangeIssues на каждый слот по порядку (шаг с точкой сохранения — отказ любой станции
		/// откатывает слот целиком), затем поставленные выпуски — разностью RangeSlotIssues до и после. Повтор после
		/// взаимоблокировки и прерывание запуска — как у линейки (<see cref="TemplateSource.WriteDayCore"/>).
		/// </summary>
		private static bool WriteDay(RangeSource.OpenRange r, int position, WindowsSource.WeekView view, bool ignoreFirmWindows, List<DayWrite> writes,
			string dayText, int daysLeft, TemplateSource.TemplateResult result, string kind)
		{
			List<DateTime> slots = writes.Select(w => w.Start).Distinct().ToList();
			Dictionary<DateTime, int> rollerOf = writes.ToDictionary(w => w.Start, w => w.Roller.RollerId);
			return TemplateSource.WriteDayCore(kind, "action " + r.ActionId, dayText, daysLeft, writes.Count, result, () =>
			{
				WindowsSource.BatchResult dayResult = new WindowsSource.BatchResult();
				WindowsSource.RunInTransaction(() =>
				{
					HashSet<int> before = RangeSource.IssueIdsIn(r, slots);
					foreach (DayWrite w in writes)
						RangeSource.Step(dayResult, RangeSource.SlotName(w.Start), 1,
							() => RangeSource.AddRangeFor(r, w.Start, w.Roller.RollerId, w.Roller.Duration, position, r.CampaignIds, view, ignoreFirmWindows));
					if (dayResult.Done > 0)
						dayResult.Created.AddRange(RangeSource.CreatedSince(r, slots, before,
							(slot, issue) => rollerOf.TryGetValue(slot, out int rollerId) && rollerId == issue.RollerId));
				});
				return dayResult;
			});
		}
	}
}
