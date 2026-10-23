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
	/// Пересчёт акции — один, в конце; у бонусных роликов спонсорской кампании (этап 7) — ещё и после каждого выпуска
	/// (В-33, см. WriteDay). Права — как у остальных записей вкладки (LoadForWrite).
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
			return RunIntervalCore(c, campaign, template, position, "interval", roller, null, "roller=" + rollerId);
		}

		/// <summary>
		/// Шаблон 3 («Шаблон с несколькими роликами», десктоп — FrmTemplate3 и FrmGenerator.AddMultiRollerIssues): те же окна и
		/// те же правила выбора, что у Шаблона 2, но ролики берутся из очереди «ролик × количество» (RollerAllocationQueue: по
		/// кругам, в круге каждый ролик по разу, внутри порции подряд не повторяются). На группу окон дня берётся ровно столько
		/// роликов, сколько нашлось окон (как у веера десктопа; у десктопной линейки — дневная норма с возвратом остатка:
		/// квоты и «не подряд» те же; при нехватке окон состав порции и редкие соседние повторы могут отличаться). Дневная норма
		/// не растёт: не поставленное из-за нехватки окон остаётся в очереди, к следующим дням не добавляется и входит в строку
		/// «Недостаточно рекламных окон за весь период шаблона…». Отказ записи ролик расходует. <paramref name="quotas"/> — «номер ролика → сколько выходов», ролики только из списка роликов фирмы.
		/// </summary>
		public static TemplateResult RunMulti(WindowsSource.OpenCampaign c, PlacementTemplate template, IReadOnlyList<KeyValuePair<int, int>> quotas, int position)
		{
			Campaign campaign = WindowsSource.LoadForWrite(c);
			List<(Roller Roller, int Quantity)> chosen = quotas.Where(q => q.Value > 0)
				.Select(q => (WindowsSource.BrushRoller(c, q.Key, campaign.Action.FirmID), q.Value)).ToList();
			if (chosen.Count == 0)
				return new TemplateResult();
			return RunIntervalCore(c, campaign, template, position, "multi", null, new RollerAllocationQueue(chosen),
				"rollers=" + string.Join(",", quotas.Where(q => q.Value > 0).Select(q => q.Key + "x" + q.Value)));
		}

		/// <summary>Общий ход Шаблонов 2 и 3: окна периода читаются заранее, день за днём — выбор окон, ролики, запись.</summary>
		private static TemplateResult RunIntervalCore(WindowsSource.OpenCampaign c, Campaign campaign, PlacementTemplate template, int position,
			string kind, Roller roller, RollerAllocationQueue queue, string rollersText)
		{
			List<DateTime> dates = template.Dates();
			TemplateResult result = new TemplateResult();
			if (dates.Count == 0)
				return result;

			using (OperationScope.Start($"Template{char.ToUpperInvariant(kind[0])}{kind.Substring(1)} campaign={c.CampaignId} days={dates.Count}"))
			{
				Stopwatch sw = Stopwatch.StartNew();
				Random rnd = new Random();
				Period period = LoadPeriod(c, dates, template, showDisabled: false);
				int startMinute = (int)template.StartTime.TotalMinutes;
				int finishMinute = (int)template.FinishTime.TotalMinutes;
				bool interrupted = false;

				// Подряд идущие дни вне прайс-листов станции — одна строка таблицы («дата – дата»), а не по строке на день.
				// У Шаблона 3 «не добавлено» считает остаток очереди в конце, поэтому на день ничего не прибавляется.
				RunOfDays missing = new RunOfDays(result, queue == null ? WantedPerDay(template) : 0);

				for (int dayIndex = 0; dayIndex < dates.Count; dayIndex++)
				{
					DateTime day = dates[dayIndex];
					string dayText = day.ToString("dd.MM.yyyy");
					if (!period.Rows.TryGetValue(day, out List<DataRow> rows))
					{
						missing.Add(day, MessageAccessor.GetMessage("PriceListDoesntExist"));
						continue;
					}
					missing.Flush();

					// Те же отборы, что у десктопа: интервал (у линейки конец включается), занятая подтверждённым выпуском
					// позиция (макеты не смотрятся), ролики этой фирмы в окне (с макетами, любой акции фирмы).
					List<TemplateWindow> candidates = TemplatePlanner.InTimeRange(
							rows.Select(r => ToWindow(r, day, period)), startMinute, finishMinute, finishInclusive: true)
						.Where(w => !(position != (int)RollerPositions.Undefined && PositionOccupied(period.RowOf(w.Key), (RollerPositions)position))
							&& !(template.IgnoreFirmWindows && period.FirmWindows.Contains(w.Key)))
						.ToList();

					List<TemplatePick> picks = TemplatePlanner.PickDay(candidates, template.Quantity, template.QuantityPrime, template.QuantityOffPrime, rnd);
					foreach (TemplatePick pick in picks.Where(p => p.IsShort))
						// Шаблон 3: нехватка окон в день ещё не потеря — ролики остаются в очереди; итог — строка в конце
						result.Note(dayText, ShortNote(pick, queue != null), queue == null ? pick.Wanted - pick.Windows.Count : 0);

					// Ролик каждому окну: один ролик кисти или из очереди (группа за группой, как у десктопа).
					List<DayWrite> writes = new List<DayWrite>();
					foreach (TemplatePick pick in picks)
					{
						List<Roller> rollers = queue?.TakeForToday(pick.Windows.Count);
						for (int k = 0; k < pick.Windows.Count; k++)
						{
							Roller forWindow = queue == null ? roller : (k < rollers.Count ? rollers[k] : null);
							if (forWindow == null)
								break;
							TemplateWindow w = pick.Windows[k];
							writes.Add(new DayWrite(w.Key, w.Start, new TariffWindowWithRollerIssues(period.RowOf(w.Key), Entities.TariffWindow), forWindow));
						}
					}
					if (writes.Count == 0)
					{
						result.Days++;
						continue;
					}

					// По возрастанию номера окна — один порядок блокировок у всех пачек.
					writes = writes.OrderBy(w => w.WindowId).ToList();
					if (!WriteDay(c, campaign, position, writes, dayText, dates.Count - dayIndex, result, kind))
					{
						interrupted = true;
						break;
					}
				}
				missing.Flush();

				// Не хватило окон на весь период: что осталось в очереди, поставить некуда (при сбое запуска — не говорим «нет окон»).
				if (queue != null && !interrupted && queue.Count > 0)
					result.Note(dates[dates.Count - 1].ToString("dd.MM.yyyy"),
						Tr.Format("Недостаточно рекламных окон за весь период шаблона — не удалось разместить {0} выход(ов) из выбранных роликов.", queue.Count), queue.Count);

				Recalculate(c, campaign, result, kind);

				// Параметры шаблона — в строке итога: по логу видно, что просили и что получилось (пользователь и сеанс — в шаблоне строки лога).
				string daysText = template.ByParity ? (template.Odd ? "odd" : "even") : "days=" + string.Concat(template.WeekDays.Select(on => on ? "1" : "0"));
				Log.Info($"Template {kind} completed in {sw.ElapsedMilliseconds}ms campaign={c.CampaignId} {rollersText} position={position}" +
					$" period={template.StartDate:yyyy-MM-dd}..{template.FinishDate:yyyy-MM-dd} {daysText}" +
					$" time={template.StartTime.ToString(@"hh\:mm")}-{template.FinishTime.ToString(@"hh\:mm")}" +
					$" quantity={template.Quantity}/{template.QuantityPrime}/{template.QuantityOffPrime} ignoreFirm={template.IgnoreFirmWindows}" +
					$" days={result.Days}/{dates.Count} added={result.Done} notAdded={result.NotAdded}");
			}
			return result;
		}

		/// <summary>Окно, в которое день пишет выпуск: номер, время (для записи «Отменить»), объект ядра и ролик.</summary>
		private sealed class DayWrite
		{
			public DayWrite(int windowId, DateTime start, TariffWindowWithRollerIssues window, Roller roller)
			{
				WindowId = windowId;
				Start = start;
				Window = window;
				Roller = roller;
			}

			public int WindowId { get; }
			public DateTime Start { get; }
			public TariffWindowWithRollerIssues Window { get; }
			public Roller Roller { get; }
		}

		/// <summary>
		/// Один день линейки: выпуски в окна по порядку, каждое — шаг <see cref="WindowsSource.Step"/>. У спонсорской кампании
		/// после каждого поставленного выпуска — пересчёт акции в транзакции дня (<see cref="WindowsSource.RecalculatesPerStep"/>):
		/// иначе внутри дня бонус сверяется с расходом до дня и превышается — так превышает десктопный генератор (Д-27).
		/// </summary>
		private static bool WriteDay(WindowsSource.OpenCampaign c, Campaign campaign, int position, List<DayWrite> writes,
			string dayText, int daysLeft, TemplateResult result, string kind)
		{
			bool perStep = WindowsSource.RecalculatesPerStep(campaign);
			return WriteDayCore(kind, "campaign " + c.CampaignId, dayText, daysLeft, writes.Count, result, () =>
			{
				WindowsSource.BatchResult dayResult = new WindowsSource.BatchResult();
				WindowsSource.RunInTransaction(() =>
				{
					foreach (DayWrite w in writes)
						if (WindowsSource.Step(dayResult, w.Start.ToString("dd.MM.yyyy HH:mm"), () =>
							{
								dayResult.Created.Add(WindowsSource.Capture(c, campaign.AddIssue(w.Roller, w.Window, (RollerPositions)position, null), w.WindowId, w.Start, w.Roller));
								return null;
							}) && perStep)
							campaign.RecalculateAction(false);
				});
				return dayResult;
			});
		}

		/// <summary>
		/// Один день в одной транзакции (её открывает <paramref name="attempt"/>): отказ шага — строка таблицы, остальные пишутся.
		/// Взаимоблокировка откатывает день целиком — он повторяется один раз; иной сбой откатывает день и прерывает
		/// запуск (false): выпуски прежних дней остаются, в строке — сколько дней не обработано (<paramref name="daysLeft"/>
		/// считает и этот), <paramref name="notAdded"/> — сколько выпусков дня пропало. Общее у линейки и веера.
		/// </summary>
		internal static bool WriteDayCore(string kind, string context, string dayText, int daysLeft, int notAdded, TemplateResult result,
			Func<WindowsSource.BatchResult> attempt)
		{
			WindowsSource.BatchResult dayResult = null;
			Exception failure = null;
			for (int n = 0; n < 2; n++)
			{
				failure = null;
				try
				{
					dayResult = attempt();
					break;
				}
				catch (SqlException ex) when (ex.Number == 1205 && n == 0)
				{
					// Жертва взаимоблокировки: день откатан целиком и ничего не оставил — день повторяется один раз.
					Log.Warn($"Template {kind}: deadlock on {dayText} ({context}), retrying the day", ex);
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
				Log.Error($"Template {kind} interrupted on {dayText} ({context})", failure);
				Interrupted(result, dayText, failure, daysLeft, notAdded);
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

		internal static void Interrupted(TemplateResult result, string name, Exception failure, int daysLeft, int notAdded) =>
			result.Note(name, Tr.Format("Запуск прерван: {0} Выпуски этого дня не добавлены, добавленные ранее — сохранены. Не обработано дней: {1}.",
				ErrorManager.GetErrorMessage(failure), daysLeft), notAdded);

		private static void Recalculate(WindowsSource.OpenCampaign c, Campaign campaign, TemplateResult result, string kind) =>
			RecalculateCore("campaign " + c.CampaignId, () => campaign.RecalculateAction(false), result, kind);

		/// <summary>Один пересчёт акции после запуска, отдельной транзакцией; сбой — строка в таблице (выпуски уже записаны).</summary>
		internal static void RecalculateCore(string context, System.Action recalculate, TemplateResult result, string kind)
		{
			if (result.Done == 0)
				return;
			try
			{
				WindowsSource.RunInTransaction(recalculate);
			}
			catch (Exception ex)
			{
				Log.Error($"Template {kind}: recalculation failed ({context})", ex);
				result.Note("", Tr.Format("Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.", ErrorManager.GetErrorMessage(ex)));
			}
		}

		/// <summary>
		/// Шаблон 1 («Шаблон для внесения роликов», десктоп — FrmTemplate): ролик кисти с позицией «Позиционирования» по
		/// одному выпуску в день периода в окно, накрывающее время <see cref="PlacementTemplate.StartTime"/>. Десктоп берёт
		/// время из щелчка по окну сетки (всегда начало видимого окна) и ищет окно запросом TariffWindowRetrieve @actualDate
		/// на каждый день; в вебе время вводится вручную, поэтому окно ищется среди окон, которые видит сетка (период читается
		/// заранее, как у Шаблона 2: без особых окон и тарифов «только для модулей»; заблокированные окна остаются — отказ
		/// скажет запись), и введённое время не может попасть в окно, которого в сетке нет. Окно накрывает время, если
		/// начинается не позже и кончается не раньше (границы включены, из нескольких — начавшееся позже), как в запросе
		/// десктопа. Отказы — по правилам десктопа (FrmGenerator.AddSimpleIssue), тексты из iMessage, строка — время найденного
		/// окна. Режима «Удалить выбранные» нет.
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
				Period period = LoadPeriod(c, dates, template, showDisabled: true);

				// Дни вне прайс-листов и дни без окна на это время — по одной строке на подряд идущие дни.
				RunOfDays nothing = new RunOfDays(result, 1);
				for (int dayIndex = 0; dayIndex < dates.Count; dayIndex++)
				{
					DateTime day = dates[dayIndex];
					if (!period.Rows.TryGetValue(day, out List<DataRow> rows))
					{
						nothing.Add(day, MessageAccessor.GetMessage("PriceListDoesntExist"));
						continue;
					}
					DataRow row = CoveringWindow(rows, day + template.StartTime);
					if (row == null)
					{
						nothing.Add(day, MessageAccessor.GetMessage("TariffWindowNotFound"));
						continue;
					}
					nothing.Flush();

					int windowId = Convert.ToInt32(row[TariffWindow.ParamNames.WindowId]);
					DateTime start = (DateTime)row[TariffWindow.ParamNames.WindowDateActual];
					string name = start.ToString("dd.MM.yyyy HH:mm");
					string refusal = template.IgnoreFirmWindows && period.FirmWindows.Contains(windowId) ? MessageAccessor.GetMessage("IssueWithTheSameFirmExists")
						: position != (int)RollerPositions.Undefined && PositionOccupied(row, (RollerPositions)position) ? MessageAccessor.GetMessage("FirstLastIssueError")
						: null;
					if (refusal != null)
					{
						result.Days++;
						result.Note(name, refusal, 1);
						continue;
					}

					List<DayWrite> writes = new List<DayWrite> { new DayWrite(windowId, start, new TariffWindowWithRollerIssues(row, Entities.TariffWindow), roller) };
					if (!WriteDay(c, campaign, position, writes, name, dates.Count - dayIndex, result, "single"))
						break;
				}
				nothing.Flush();

				Recalculate(c, campaign, result, "single");

				string daysText = template.ByParity ? (template.Odd ? "odd" : "even") : "days=" + string.Concat(template.WeekDays.Select(on => on ? "1" : "0"));
				Log.Info($"Template single completed in {sw.ElapsedMilliseconds}ms campaign={c.CampaignId} roller={rollerId} position={position}" +
					$" period={template.StartDate:yyyy-MM-dd}..{template.FinishDate:yyyy-MM-dd} {daysText} time={template.StartTime.ToString(@"hh\:mm")}" +
					$" ignoreFirm={template.IgnoreFirmWindows} days={result.Days}/{dates.Count} added={result.Done} notAdded={result.NotAdded}");
			}
			return result;
		}

		/// <summary>Окно дня, накрывающее момент: начинается не позже и кончается не раньше (границы включены); из нескольких — начавшееся позже.</summary>
		private static DataRow CoveringWindow(List<DataRow> rows, DateTime moment)
		{
			DataRow best = null;
			DateTime bestStart = DateTime.MinValue;
			foreach (DataRow row in rows)
			{
				DateTime start = (DateTime)row[TariffWindow.ParamNames.WindowDateActual];
				if (start <= moment && start.AddSeconds(WindowsSource.IntOrZero(row[TariffWindow.ParamNames.Duration])) >= moment && start >= bestStart)
				{
					best = row;
					bestStart = start;
				}
			}
			return best;
		}

		/// <summary>
		/// Подряд идущие дни с одной и той же причиной «ничего не нашлось» (нет прайс-листа, нет окна) — одна строка таблицы
		/// «дата – дата», а не по строке на день; не добавлено — по <c>wantedPerDay</c> на день.
		/// </summary>
		internal sealed class RunOfDays
		{
			private readonly TemplateResult _result;
			private readonly int _wantedPerDay;
			private string _reason;
			private DateTime _from, _to;
			private int _days;

			public RunOfDays(TemplateResult result, int wantedPerDay)
			{
				_result = result;
				_wantedPerDay = wantedPerDay;
			}

			public void Add(DateTime day, string reason)
			{
				if (_reason != null && _reason != reason)
					Flush();
				if (_reason == null)
				{
					_reason = reason;
					_from = day;
				}
				_to = day;
				_days++;
			}

			public void Flush()
			{
				if (_reason == null)
					return;
				string range = _from == _to ? _from.ToString("dd.MM.yyyy") : _from.ToString("dd.MM.yyyy") + " – " + _to.ToString("dd.MM.yyyy");
				_result.Note(range, _reason, _wantedPerDay * _days);
				_reason = null;
				_days = 0;
			}
		}

		/// <summary>Сколько выпусков в день просит шаблон.</summary>
		internal static int WantedPerDay(PlacementTemplate t) => t.Quantity > 0 ? t.Quantity : t.QuantityPrime + t.QuantityOffPrime;

		/// <summary>
		/// Тексты десктопа: у Шаблона 2 — FrmGenerator.AddSimpleIssues, у Шаблона 3 — AddMultiRollerIssues (свои три строки: «выпусков: {1}»,
		/// без «для добавления»). «Окон» — сколько окон нашлось (у десктопа — сколько выпусков поставлено), «выпусков» — сколько просили.
		/// </summary>
		private static string ShortNote(TemplatePick pick, bool multi)
		{
			switch (pick.Group)
			{
				case TemplateGroup.Prime:
					return multi
						? Tr.Format("Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков: {1}.", pick.Windows.Count, pick.Wanted)
						: Tr.Format("Недостаточно рекламных окон прайм для размещения всех выпусков. Окон: {0}, выпусков {1}.", pick.Windows.Count, pick.Wanted);
				case TemplateGroup.OffPrime:
					return multi
						? Tr.Format("Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков: {1}.", pick.Windows.Count, pick.Wanted)
						: Tr.Format("Недостаточно рекламных окон офф прайм для размещения всех выпусков. Окон: {0}, выпусков для добавления {1}.", pick.Windows.Count, pick.Wanted);
				default:
					return multi
						? Tr.Format("Недостаточно рекламных окон для размещения всех выпусков. Окон: {0}, выпусков: {1}.", pick.Windows.Count, pick.Wanted)
						: Tr.Format("Недостаточно рекламных окон для размещения всех выпусков для добавления. Окон: {0}, выпусков {1}.", pick.Windows.Count, pick.Wanted);
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

		private static Period LoadPeriod(WindowsSource.OpenCampaign c, List<DateTime> dates, PlacementTemplate template, bool showDisabled)
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

				// Как RollerIssuesGrid3 и десктопный генератор: без особых окон и тарифов «только для модулей»; время окна —
				// фактическое. Заблокированные окна нужны только Шаблону 1 (в окно на заданное время запись откажет сама).
				list.ExcludeSpecialWindows = true;
				list.ExcludeModuleTariffs = true;
				DataTable windows = list.GetTariffWindows(first, last, null, false, showDisabled, useActualTime: true).Tables[Constants.TableNames.Data];
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
