using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using log4net;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Вкладка «Рекламные окна» страницы акции, несколько отмеченных линейных кампаний — веер
	/// (docs/tasks/web-action-forms.md, этап 4): то, что в десктопе делают EditIssuesForm и
	/// TariffWithRangeGrid. Ячейка — получасовой слот: он есть только там, где окно есть у каждой
	/// отмеченной станции; клик «Режима добавления» ставит один ролик с одной позицией на всех
	/// станциях сразу (AddRangeIssues).
	///
	/// Чтение — те же процедуры и параметры, что у десктопа: TariffWindowWithRange (слоты, цвета,
	/// прайм, остаток), RangeSlotIssues (выпуски отмеченных кампаний в цветных слотах — число выпусков
	/// за день и подсказка «Отсутствуют»). Окно выхода везде фактическое (правило 07.10.2026).
	///
	/// Запись — AddRangeIssues и ActionRecalculate одной транзакцией со свежей акцией (платёж
	/// корректируется по сравнению старой и новой цены акции). Процедура ставит выпуски по станциям
	/// курсором: если на какой-то станции IssueIUD отказал, предыдущие уже записаны, поэтому любой
	/// отказ откатывает весь слот целиком (десктоп делал то же отдельной транзакцией на вызов) и
	/// уходит пользователю как есть; частичного слота такая запись не оставляет.
	///
	/// Права проверяет фасад перед записью: процедуры веера о «своей / чужой» акции не знают, а
	/// «Редактировать» нужно и акции, и каждой отмеченной кампании — иначе слот вышел бы красным.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class RangeSource
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RangeSource));

		// ---------- Открытый веер ----------

		/// <summary>Отмеченные линейные кампании акции, с которыми работает вкладка.</summary>
		public sealed class OpenRange : WindowsSource.IBrushScope
		{
			internal OpenRange() { }

			public int ActionId { get; internal set; }
			public int FirmId { get; internal set; }

			/// <summary>Отмеченные кампании — в порядке отметки.</summary>
			public IReadOnlyList<int> CampaignIds { get; internal set; }

			/// <summary>Можно ли менять размещение: «Редактировать» у акции и у кампаний (на момент открытия).</summary>
			public bool CanEdit { get; internal set; }

			/// <summary>Право «Редактировать» кампании на момент открытия — права группы на сущность, от строки кампании не зависят.</summary>
			internal bool CampaignRight { get; set; }

			/// <summary>День самого раннего выпуска отмеченных кампаний; null — выпусков нет. С этой недели открывается сетка.</summary>
			public DateTime? FirstIssueDate { get; internal set; }

			public DataTable Rollers { get; set; }

			/// <summary>Подпись кампании для подсказок («Отсутствуют: …») — задаёт экран.</summary>
			internal IReadOnlyDictionary<int, string> Titles { get; set; }

			internal IReadOnlyDictionary<int, int> MassmediaOf { get; set; }
		}

		/// <summary>
		/// Веер по отмеченным кампаниям: <paramref name="campaigns"/> — номер и подпись кампании. null — какой-то
		/// из номеров не годится (удалена, другая акция, не линейная).
		/// </summary>
		public static OpenRange Open(ActionOnMassmedia action, IReadOnlyList<KeyValuePair<int, string>> campaigns)
		{
			// Один свежий список кампаний акции вместо чтения каждой: на 18 станциях это 18 вызовов на каждое открытие
			// и на каждый клик.
			Dictionary<int, DataRow> rows = LinearCampaigns(ActionWorkspace.LoadCampaigns(action));
			Dictionary<int, string> titles = new Dictionary<int, string>();
			Dictionary<int, int> massmediaOf = new Dictionary<int, int>();
			DateTime? first = null;
			foreach (KeyValuePair<int, string> item in campaigns)
			{
				if (!rows.TryGetValue(item.Key, out DataRow row))
					return null;
				titles[item.Key] = item.Value;
				massmediaOf[item.Key] = ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.MassmediaId], 0);
				DateTime start = ParseHelper.GetDateTimeFromObject(row[Campaign.ParamNames.StartDate], DateTime.MinValue);
				if (start != DateTime.MinValue && (first == null || start.Date < first))
					first = start.Date;
			}

			bool campaignRight = CampaignRight(campaigns.First().Key);
			return new OpenRange
			{
				ActionId = action.ActionId,
				FirmId = action.FirmID,
				CampaignIds = campaigns.Select(c => c.Key).ToList(),
				CanEdit = ActionWorkspace.CanEdit(action) && campaignRight,
				CampaignRight = campaignRight,
				FirstIssueDate = first,
				Titles = titles,
				MassmediaOf = massmediaOf,
			};
		}

		/// <summary>Линейные кампании акции по номеру: удалённые и нелинейные в списке не бывают.</summary>
		private static Dictionary<int, DataRow> LinearCampaigns(DataTable campaigns)
		{
			Dictionary<int, DataRow> result = new Dictionary<int, DataRow>();
			foreach (DataRow row in campaigns.Rows)
				if (WindowsSource.IsLinear(row))
					result[WindowsSource.CampaignIdOf(row)] = row;
			return result;
		}

		/// <summary>
		/// «Редактировать» кампании (как вход в CampaignForm из ActionForm). Оно складывается из правила своих /
		/// чужих акций (по акции — его перепроверяет фасад на свежей акции перед записью) и прав группы на
		/// сущность — те не зависят от строки кампании, поэтому читается одна кампания и один раз, при открытии.
		/// </summary>
		private static bool CampaignRight(int campaignId)
		{
			Campaign campaign = Campaign.GetCampaignById(campaignId);
			return campaign != null && campaign.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal);
		}

		/// <summary>
		/// Свежая акция для записи; без права или с исчезнувшей отмеченной кампанией — отказ. Исчезнувшую
		/// кампанию процедура сама молча пропустила бы, и слот вышел бы красным.
		/// </summary>
		internal static ActionOnMassmedia LoadForWrite(OpenRange r)
		{
			ActionOnMassmedia action = ActionWorkspace.Load(r.ActionId);
			if (action == null)
				throw new InvalidOperationException(Tr.T("Рекламная акция удалена. Обновите страницу."));
			Dictionary<int, DataRow> rows = LinearCampaigns(ActionWorkspace.LoadCampaigns(action));
			if (r.CampaignIds.Any(id => !rows.ContainsKey(id)))
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			if (!ActionWorkspace.CanEdit(action) || !r.CampaignRight)
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			return action;
		}

		/// <summary>«Добавить ролик - пустышку» в веере: пустышка фирмы этой длины, как у одной кампании.</summary>
		public static int CreateDummyRoller(OpenRange r, int seconds)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			return MuteRoller.GetRoller(seconds, action.FirmID, null).RollerId;
		}

		// ---------- Неделя ----------

		/// <summary>Один выпуск отмеченной кампании в слоте.</summary>
		internal sealed class SlotIssue
		{
			internal int CampaignId { get; set; }
			internal int RollerId { get; set; }
			internal int PositionId { get; set; }
			internal string RollerName { get; set; }
			internal int IssueId { get; set; }
			internal int Duration { get; set; }

			/// <summary>Окно выхода выпуска (колонка originalWindowID у RangeSlotIssues — имя историческое).</summary>
			internal int WindowId { get; set; }
		}

		/// <summary>Ролик фирмы в слоте из чужой акции (или модульной кампании этой): подтверждён ли выпуск.</summary>
		internal sealed class FirmRoller
		{
			internal int RollerId { get; set; }
			internal int PositionId { get; set; }
			internal bool HasConfirmed { get; set; }
		}

		private static readonly DateTime SlotEpoch = new DateTime(2000, 1, 1);

		/// <summary>Ключ слота — номер получаса от 2000-01-01: слоты веера лежат на границах получасов.</summary>
		public static int SlotKey(DateTime slotStart) => (int)((slotStart - SlotEpoch).TotalMinutes / 30);

		private const string SqlDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

		/// <summary>
		/// Неделя, содержащая <paramref name="anyDate"/>: понедельник – воскресенье, получасовые слоты
		/// по всем отмеченным станциям. Слотов нет без прайс-листа или окон хотя бы у одной станции —
		/// в пустой неделе сказано, у каких станций нет прайс-листа.
		/// </summary>
		public static PlacementWeek LoadWeek(OpenRange r, DateTime anyDate, WindowsSource.WeekView view)
		{
			DateTime monday = MondayOf(anyDate.Date);
			PlacementWeek week = new PlacementWeek
			{
				FirstDay = monday,
				StartDate = monday,
				FinishDate = monday.AddDays(PlacementWeek.DaysInWeek - 1),
				HasPrices = false,
				HasPricelist = false,
			};

			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["dateStart"] = monday;
			parameters[Action.ParamNames.ActionId] = r.ActionId;
			parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(r.CampaignIds.ToList());
			if (view.AdvertTypeId.HasValue)
				parameters["advertTypeID"] = view.AdvertTypeId.Value;
			DataSet ds = DataAccessor.LoadDataSet("TariffWindowWithRange", parameters);

			object maxBroadcast = ds.Tables[1].Rows[0]["maxBroadcast"];
			if (maxBroadcast == null || maxBroadcast == DBNull.Value)
			{
				SetEmpty(week, r, monday);
				return week;
			}
			int broadcastHour = ((DateTime)maxBroadcast).Hour;

			// Строки — получасы по порядку вещания, как в десктопе: «23:30-00:00».
			List<PlacementRow> rows = new List<PlacementRow>();
			Dictionary<int, PlacementRow> rowOf = new Dictionary<int, PlacementRow>();
			foreach (DataRow t in ds.Tables[2].Rows)
			{
				int h = Convert.ToInt32(t["h"]);
				int m = Convert.ToInt32(t["m"]);
				DateTime end = new DateTime(1, 1, 1, h, m, 0).AddMinutes(30);
				PlacementRow row = new PlacementRow(string.Format("{0}-{1}", TimeText(h, m, broadcastHour), TimeText(end.Hour, end.Minute, broadcastHour)), 0m);
				rowOf[h * 60 + m] = row;
				rows.Add(row);
			}
			week.Rows = rows;

			Dictionary<DateTime, List<DataRow>> firmActions = new Dictionary<DateTime, List<DataRow>>();
			foreach (DataRow a in ds.Tables[3].Rows)
			{
				DateTime date = (DateTime)a["date"];
				if (!firmActions.TryGetValue(date, out List<DataRow> list))
					firmActions[date] = list = new List<DataRow>();
				list.Add(a);
			}

			Dictionary<DateTime, List<FirmRoller>> firmRollers = new Dictionary<DateTime, List<FirmRoller>>();
			foreach (DataRow a in ds.Tables[4].Rows)
			{
				DateTime date = (DateTime)a["date"];
				if (!firmRollers.TryGetValue(date, out List<FirmRoller> list))
					firmRollers[date] = list = new List<FirmRoller>();
				list.Add(new FirmRoller
				{
					RollerId = Convert.ToInt32(a[Roller.ParamNames.RollerId]),
					PositionId = IntOrZero(a[Issue.ParamNames.PositionId]),
					HasConfirmed = IntOrZero(a["hasConfirmed"]) == 1,
				});
			}

			List<PlacementCell> coloured = new List<PlacementCell>();
			foreach (DataRow d in ds.Tables[0].Rows)
			{
				int h = Convert.ToInt32(d["h"]);
				int m = Convert.ToInt32(d["m"]);
				if (!rowOf.TryGetValue(h * 60 + m, out PlacementRow row))
					continue;
				int day = Convert.ToInt32(d["col"]) - 1;
				if (day < 0 || day >= PlacementWeek.DaysInWeek)
					continue;

				DateTime slot = (DateTime)d["date"];
				PlacementCell cell = new PlacementCell
				{
					Key = SlotKey(slot),
					Date = slot,
					DayIndex = day,
					Text = DateTimeUtils.Time2String(IntOrZero(view.ShowUnconfirmed ? d["timeWithUnConfirmed"] : d["timeWithConfirmed"])),
					Flags = SlotFlags(d, view),
				};
				if (firmRollers.TryGetValue(slot, out List<FirmRoller> firm))
					cell.FirmSlotRollers = firm;
				if (cell.Has(PlacementFlags.Mine) || cell.Has(PlacementFlags.Partial))
					coloured.Add(cell);
				else if ((cell.Has(PlacementFlags.FirmHere) || cell.Has(PlacementFlags.FirmSome)) && firmActions.TryGetValue(slot, out List<DataRow> others))
					cell.Details = OtherActionsText(r, others);
				row.Cells[day] = cell;
			}

			// Выпуски отмеченных кампаний в цветных слотах: число выпусков за день и «Отсутствуют: …».
			int[] perDay = new int[PlacementWeek.DaysInWeek];
			if (coloured.Count > 0)
			{
				Dictionary<DateTime, List<SlotIssue>> bySlot = LoadSlotIssues(r, coloured.Select(c => c.Date));
				foreach (PlacementCell cell in coloured)
				{
					cell.SlotIssuesLoaded = true;
					if (!bySlot.TryGetValue(cell.Date, out List<SlotIssue> issues))
						continue;
					cell.SlotIssues = issues;
					perDay[cell.DayIndex] += issues.Count;
					if (cell.Has(PlacementFlags.Partial))
						cell.Details = MissingText(r, issues);
				}
			}
			for (int d = 0; d < PlacementWeek.DaysInWeek; d++)
				week.DayTotals[d] = perDay[d];

			if (rows.Count == 0)
				SetEmpty(week, r, monday);
			return week;
		}

		private static DateTime MondayOf(DateTime date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

		/// <summary>Как TariffWithRangeGrid.GetTimeString: часы до начала вещания — это уже следующие сутки.</summary>
		private static string TimeText(int hour, int minute, int broadcastHour) =>
			hour >= broadcastHour ? DateTimeUtils.Time2String(hour, minute) : DateTimeUtils.Time2String(hour + 24, minute);

		private static int IntOrZero(object value) => value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);

		private static bool Flag(DataRow row, string column) => ParseHelper.GetBooleanFromObject(row[column], false);

		/// <summary>
		/// Признаки слота — TariffWithRangeGrid.MarkCells: цвет текста по приоритету (свои у всех кампаний, свои
		/// не у всех, фирма на всех станциях, фирма на части), жирный — подходит под «Позиционирование» и
		/// «Предметы рекламы», фон — прайм.
		/// </summary>
		private static PlacementFlags SlotFlags(DataRow d, WindowsSource.WeekView view)
		{
			PlacementFlags flags = PlacementFlags.None;
			if (Flag(d, "isPrime"))
				flags |= PlacementFlags.Prime;
			if ((view.Position != 0 || view.AdvertTypeId.HasValue) && PositionFree(d, view) && AdvertTypeMatches(d, view))
				flags |= PlacementFlags.Match;

			if (Flag(d, "HasIssuesThisActionAllCampaigns"))
				return flags | PlacementFlags.Mine;
			if (Flag(d, "HasIssuesThisAction"))
				return flags | PlacementFlags.Partial;

			bool firmEverywhere = Flag(d, "HasIssuesAllMassmedia") || (view.ShowUnconfirmed && Flag(d, "HasIssuesUnconfirmedAllMassmedia"));
			bool firmAnywhere = firmEverywhere || Flag(d, "HasIssues") || (view.ShowUnconfirmed && Flag(d, "HasIssuesUnconfirmed"));
			if (firmEverywhere)
				return flags | PlacementFlags.FirmHere;
			return firmAnywhere ? flags | PlacementFlags.FirmSome : flags;
		}

		/// <summary>Позиция свободна на всех станциях слота среди подтверждённых, при «Учитывать макеты» — и среди макетов.</summary>
		private static bool PositionFree(DataRow d, WindowsSource.WeekView view) => PositionFree(d, view.Position, view.ShowUnconfirmed);

		private static bool PositionFree(DataRow d, int position, bool showUnconfirmed)
		{
			string occupied, unconfirmed;
			switch ((RollerPositions)position)
			{
				case RollerPositions.First: occupied = "isFirstPositionOccupied"; unconfirmed = "firstPositionsUnconfirmed"; break;
				case RollerPositions.Second: occupied = "isSecondPositionOccupied"; unconfirmed = "secondPositionsUnconfirmed"; break;
				case RollerPositions.Last: occupied = "isLastPositionOccupied"; unconfirmed = "lastPositionsUnconfirmed"; break;
				default: return true;
			}
			return !Flag(d, occupied) && (!showUnconfirmed || IntOrZero(d[unconfirmed]) == 0);
		}

		/// <summary>«Предметы рекламы»: флаг слота — предмет есть хотя бы на одной станции; «Есть» подсвечивает такой слот, «Нет» — остальные.</summary>
		private static bool AdvertTypeMatches(DataRow d, WindowsSource.WeekView view)
		{
			if (!view.AdvertTypeId.HasValue)
				return true;
			bool found = Flag(d, view.ShowUnconfirmed ? "HasAdvertTypeUnconfirmed" : "HasAdvertType");
			return found == view.AdvertTypeExists;
		}

		/// <summary>
		/// «В этом окне:» — акции этой же фирмы в слоте (подсказка бирюзовых и оранжевых ячеек, данные уже
		/// пришли вместе с раскраской). Своя акция здесь — выпуски её модульных кампаний.
		/// </summary>
		private static string OtherActionsText(OpenRange r, List<DataRow> actions)
		{
			List<string> lines = new List<string>();
			foreach (DataRow a in actions)
			{
				bool confirmed = IntOrZero(a["hasConfirmed"]) == 1;
				string status = confirmed ? Tr.T("подтверждена") : Tr.T("не подтверждена");
				int actionId = Convert.ToInt32(a["actionID"]);
				if (actionId == r.ActionId)
				{
					lines.Insert(0, Tr.Format("Модульная кампания этой акции ({0})", status));
					continue;
				}
				string owner = a["ownerName"] as string;
				lines.Add(Tr.Format("Акция №{0} ({1})", actionId, string.IsNullOrEmpty(owner) ? status : status + ", " + owner));
			}
			return lines.Count == 0 ? null : Tr.T("В этом окне:") + "\n" + string.Join("\n", lines);
		}

		/// <summary>«Отсутствуют:» — отмеченные кампании, у которых в красном слоте нет ни одного выпуска.</summary>
		private static string MissingText(OpenRange r, List<SlotIssue> issues)
		{
			HashSet<int> present = new HashSet<int>(issues.Select(i => i.CampaignId));
			List<string> missing = r.CampaignIds.Where(id => !present.Contains(id)).Select(id => r.Titles[id]).ToList();
			return missing.Count == 0 ? null : Tr.T("Отсутствуют:") + "\n" + string.Join("\n", missing);
		}

		/// <summary>
		/// Пустая неделя: слот есть только там, где окно есть у каждой отмеченной станции. Чаще всего у одной
		/// из станций на неделю нет прайс-листа — ей и скажем (по одной станции, не по кампании).
		/// </summary>
		private static void SetEmpty(PlacementWeek week, OpenRange r, DateTime monday)
		{
			week.EmptyTitle = Tr.T("На этой неделе нет слотов, общих для всех отмеченных станций");
			string hint = Tr.T("Слот есть только там, где окно есть у каждой отмеченной станции. Снимите галочку с лишней станции или перейдите к другой неделе.");

			Dictionary<int, bool> hasPricelist = new Dictionary<int, bool>();
			List<string> without = new List<string>();
			foreach (int campaignId in r.CampaignIds)
			{
				int massmediaId = r.MassmediaOf[campaignId];
				if (!hasPricelist.TryGetValue(massmediaId, out bool has))
				{
					MassmediaPricelist p = Massmedia.GetMassmediaByID(massmediaId).GetPriceList(monday) as MassmediaPricelist;
					has = p != null && p.StartDate.Date <= monday.AddDays(PlacementWeek.DaysInWeek - 1) && p.FinishDate.Date >= monday;
					hasPricelist[massmediaId] = has;
				}
				if (!has)
					without.Add(r.Titles[campaignId]);
			}
			week.EmptyHint = without.Count == 0 ? hint : hint + " " + Tr.Format("Нет прайс-листа на эту неделю: {0}.", string.Join("; ", without));
		}

		/// <summary>
		/// Выпуски отмеченных кампаний во ВСЕХ окнах недели, а не только в цветных. Цвет слота считает
		/// TariffWindowWithRange по включённым окнам без ёмкости (#tw), а RangeSlotIssues и десктопные номера роликов
		/// (RefreshPartialRollerGroups) читают любые: выпуск в окне, которое потом заблокировали, слот не красит, но номер
		/// в ячейке десктоп показывал. Нужно только при «Номерах роликов», как и там, — одним вызовом.
		/// </summary>
		public static void LoadAllSlotIssues(OpenRange r, PlacementWeek week)
		{
			List<PlacementCell> rest = week.Cells.Where(c => !c.SlotIssuesLoaded).ToList();
			if (rest.Count == 0)
				return;
			Dictionary<DateTime, List<SlotIssue>> bySlot = LoadSlotIssues(r, rest.Select(c => c.Date));
			foreach (PlacementCell cell in rest)
			{
				if (bySlot.TryGetValue(cell.Date, out List<SlotIssue> issues))
					cell.SlotIssues = issues;
				cell.SlotIssuesLoaded = true;
			}
		}

		/// <summary>RangeSlotIssues одним вызовом на все слоты: выпуски отмеченных кампаний по слотам.</summary>
		private static Dictionary<DateTime, List<SlotIssue>> LoadSlotIssues(OpenRange r, IEnumerable<DateTime> slots)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Action.ParamNames.ActionId] = r.ActionId;
			// ISO 8601 с «T»: на сервере с русским языком 'ГГГГ-ММ-ДД чч:мм:сс' читается как 'ГГГГ-ДД-ММ'.
			parameters["issueDates"] = string.Join(",", slots.Select(d => d.ToString(SqlDateTimeFormat, CultureInfo.InvariantCulture)));
			parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(r.CampaignIds.ToList());

			Dictionary<DateTime, List<SlotIssue>> result = new Dictionary<DateTime, List<SlotIssue>>();
			foreach (DataRow row in DataAccessor.LoadDataSet("RangeSlotIssues", parameters).Tables[0].Rows)
			{
				DateTime slot = (DateTime)row["requestedIssueDate"];
				if (!result.TryGetValue(slot, out List<SlotIssue> list))
					result[slot] = list = new List<SlotIssue>();
				list.Add(new SlotIssue
				{
					CampaignId = Convert.ToInt32(row[Campaign.ParamNames.CampaignId]),
					RollerId = Convert.ToInt32(row[Roller.ParamNames.RollerId]),
					PositionId = IntOrZero(row[Issue.ParamNames.PositionId]),
					RollerName = Convert.ToString(row["rollerName"]),
					IssueId = Convert.ToInt32(row["issueID"]),
					Duration = IntOrZero(row["duration"]),
					WindowId = Convert.ToInt32(row["originalWindowID"]),
				});
			}
			return result;
		}

		// ---------- Слоты недели для шаблонов ----------

		/// <summary>Получасовой слот недели веера — кандидат на выпуск шаблона (TariffWindowWithRange, как у TariffWithRangeGrid.GetSlotsForWeek).</summary>
		internal sealed class TemplateSlot
		{
			private readonly DataRow _row;

			internal TemplateSlot(DataRow row)
			{
				_row = row;
				Start = (DateTime)row["date"];
				FreeSeconds = IntOrZero(row["timeWithUnConfirmed"]);
				IsPrime = Flag(row, "isPrime");
			}

			internal DateTime Start { get; }

			/// <summary>Остаток слота с неподтверждёнными выпусками — по нему шаблон берёт самые свободные слоты первыми, независимо от «Учитывать макеты».</summary>
			internal int FreeSeconds { get; }

			internal bool IsPrime { get; }

			/// <summary>Позиция свободна на всех станциях слота (TariffWithRangeGrid.IsPositionAvailable); без позиции слот подходит всегда.</summary>
			internal bool PositionAvailable(int position, bool showUnconfirmed) => position == 0 || PositionFree(_row, position, showUnconfirmed);

			/// <summary>«Выпуски этой фирмы в слоте» (TariffWithRangeGrid.HasFirmIssuesFlags): любая её акция, в том числе эта.</summary>
			internal bool HasFirmIssues(bool showUnconfirmed) =>
				Flag(_row, "HasIssues") || Flag(_row, "HasIssuesThisAction") || (showUnconfirmed && Flag(_row, "HasIssuesUnconfirmed"));
		}

		/// <summary>Слоты недели с понедельником <paramref name="monday"/> — одним вызовом TariffWindowWithRange; слотов нет — пустой список.</summary>
		internal static List<TemplateSlot> LoadTemplateWeek(OpenRange r, DateTime monday)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["dateStart"] = monday;
			parameters[Action.ParamNames.ActionId] = r.ActionId;
			parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(r.CampaignIds.ToList());
			DataSet ds = DataAccessor.LoadDataSet("TariffWindowWithRange", parameters);
			return ds.Tables[0].Rows.Cast<DataRow>().Select(row => new TemplateSlot(row)).ToList();
		}

		internal static DateTime WeekMonday(DateTime date) => MondayOf(date.Date);

		// ---------- Номера роликов ----------

		/// <summary>
		/// «Номера роликов» в слоте: номер → кампания → сколько выпусков этого ролика (позиция в счёт не идёт: дубль с другой
		/// позицией — обычный дубль); ролики без номера (нет в списке «Ролики» фирмы) пропускаются — как
		/// TariffWithRangeGrid.CountRollersByNumber.
		/// </summary>
		private static SortedDictionary<int, Dictionary<int, int>> CountByNumber(PlacementCell cell, IReadOnlyDictionary<int, int> numbers)
		{
			SortedDictionary<int, Dictionary<int, int>> result = new SortedDictionary<int, Dictionary<int, int>>();
			foreach (SlotIssue issue in cell.SlotIssues)
			{
				if (!numbers.TryGetValue(issue.RollerId, out int number))
					continue;
				if (!result.TryGetValue(number, out Dictionary<int, int> byCampaign))
					result[number] = byCampaign = new Dictionary<int, int>();
				byCampaign.TryGetValue(issue.CampaignId, out int count);
				byCampaign[issue.CampaignId] = count + 1;
			}
			return result;
		}

		private static int CountOf(Dictionary<int, int> byCampaign, int campaignId) =>
			byCampaign.TryGetValue(campaignId, out int count) ? count : 0;

		/// <summary>
		/// Текст ячейки при «Номерах роликов» (TariffWithRangeGrid.GetRollerNumbersText): номера роликов акции по возрастанию с
		/// пометками «Н» — число этого ролика не одинаково у отмеченных кампаний (в том числе у кого-то его нет) и «Д» — у
		/// какой-то кампании он стоит больше одного раза; затем номера роликов этой фирмы из других акций (бирюзовые и оранжевые
		/// слоты) — неподтверждённые только при «Учитывать макеты». null — известных номеров нет, в ячейке остаётся время.
		/// </summary>
		public static string NumbersText(OpenRange r, PlacementCell cell, IReadOnlyDictionary<int, int> numbers, bool showUnconfirmed)
		{
			List<string> result = new List<string>();
			foreach (KeyValuePair<int, Dictionary<int, int>> roller in CountByNumber(cell, numbers))
			{
				List<int> counts = r.CampaignIds.Select(id => CountOf(roller.Value, id)).ToList();
				bool incomplete = counts.Any(count => count != counts[0]);
				bool duplicated = counts.Any(count => count > 1);
				result.Add(roller.Key + (incomplete ? "Н" : "") + (duplicated ? "Д" : ""));
			}

			HashSet<string> covered = new HashSet<string>(cell.SlotIssues.Select(i => i.RollerId + "/" + i.PositionId));
			foreach (FirmRoller firm in cell.FirmSlotRollers)
			{
				if (!firm.HasConfirmed && !showUnconfirmed)
					continue;
				if (!covered.Add(firm.RollerId + "/" + firm.PositionId))
					continue;
				if (numbers.TryGetValue(firm.RollerId, out int number))
					result.Add(number.ToString());
			}
			return result.Count > 0 ? string.Join(", ", result) : null;
		}

		/// <summary>
		/// Расшифровка значков Н/Д (EditIssuesForm.BuildRollerMismatchTooltip): «Отсутствуют:» — по каждой кампании, каких роликов
		/// и сколько не хватает до кампании с наибольшим числом этого ролика, «Дубли:» — что у кампании стоит больше одного раза.
		/// null — показывать нечего (в слоте нет выпусков этой акции, всё совпадает, дублей нет).
		/// </summary>
		public static string MismatchTooltip(OpenRange r, PlacementCell cell, IReadOnlyDictionary<int, int> numbers)
		{
			SortedDictionary<int, Dictionary<int, int>> counts = CountByNumber(cell, numbers);
			if (counts.Count == 0)
				return null;

			List<string> missingLines = new List<string>();
			List<string> duplicateLines = new List<string>();
			foreach (int campaignId in r.CampaignIds)
			{
				List<string> missing = new List<string>();
				List<string> duplicates = new List<string>();
				foreach (KeyValuePair<int, Dictionary<int, int>> roller in counts)
				{
					int own = CountOf(roller.Value, campaignId);
					int max = r.CampaignIds.Max(id => CountOf(roller.Value, id));
					if (own < max)
						missing.Add(RollerCountText(roller.Key, max - own, missing.Count == 0));
					if (own > 1)
						duplicates.Add(RollerCountText(roller.Key, own, duplicates.Count == 0));
				}
				if (missing.Count > 0)
					missingLines.Add(r.Titles[campaignId] + " – " + string.Join(", ", missing));
				if (duplicates.Count > 0)
					duplicateLines.Add(r.Titles[campaignId] + " – " + string.Join(", ", duplicates));
			}

			List<string> sections = new List<string>();
			if (missingLines.Count > 0)
				sections.Add(Tr.T("Отсутствуют:") + "\n" + string.Join("\n", missingLines));
			if (duplicateLines.Count > 0)
				sections.Add(Tr.T("Дубли:") + "\n" + string.Join("\n", duplicateLines));
			return sections.Count == 0 ? null : string.Join("\n\n", sections);
		}

		/// <summary>«ролик 5 (3 шт.)» у первого ролика строки, дальше без слова «ролик» — как в ТЗ десктопа.</summary>
		private static string RollerCountText(int number, int count, bool isFirst) =>
			isFirst ? Tr.Format("ролик {0} ({1} шт.)", number, count) : Tr.Format("{0} ({1} шт.)", number, count);

		// ---------- Постановка ----------

		/// <summary>
		/// Клик «Режима добавления» — как TariffWithRangeGrid.AddIssuesRange: один ролик с позицией
		/// «Позиционирования» на всех отмеченных станциях слота, затем пересчёт акции — одной транзакцией.
		/// Отказ любой станции (позиция занята, нет места, выпуск этой фирмы в окне…) откатывает слот
		/// целиком и показывается пользователю текстом процедуры. Неделя после записи перечитывается. Возвращает поставленные
		/// выпуски — запись для «Отменить последнее».
		/// </summary>
		public static IReadOnlyList<WindowsSource.PlacedIssue> PlaceSlot(OpenRange r, PlacementCell cell, int rollerId, int position,
			WindowsSource.WeekView view)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			Roller roller = WindowsSource.BrushRoller(r, rollerId, action.FirmID);

			DateTime[] slots = { cell.Date };
			HashSet<int> before = IssueIdsIn(r, slots);
			WindowsSource.RunInTransaction(() =>
			{
				AddRange(r, cell.Date, roller, position, view);
				action.Recalculate(false);
			});
			return TryCreatedSince(r, slots, before, (slot, issue) => issue.RollerId == roller.RollerId) ?? new List<WindowsSource.PlacedIssue>();
		}

		/// <summary>AddRangeIssues: ролик с позицией на всех отмеченных станциях получаса <paramref name="slot"/>.</summary>
		private static void AddRange(OpenRange r, DateTime slot, Roller roller, int position, WindowsSource.WeekView view) =>
			AddRangeFor(r, slot, roller.RollerId, roller.Duration, position, r.CampaignIds, view);

		/// <summary>
		/// AddRangeIssues по заданным кампаниям: «до полного пересечения» ставит выпуск одной кампании, а не всем.
		/// <paramref name="ignoreFirmWindows"/> — шаблоны («не использовать окна, где есть ролики данной фирмы»): процедура сама
		/// пропускает такие окна и, если других нет, отказывает.
		/// </summary>
		internal static void AddRangeFor(OpenRange r, DateTime slot, int rollerId, int duration, int position, IEnumerable<int> campaignIds,
			WindowsSource.WeekView view, bool ignoreFirmWindows = false)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Action.ParamNames.ActionId] = r.ActionId;
			parameters["issueDate"] = slot;
			parameters["rollerID"] = rollerId;
			parameters["rollerDuration"] = duration;
			parameters["positionId"] = position;
			parameters["considerUnconfirmed"] = view.ShowUnconfirmed ? 1 : 0;
			parameters["ignoreWindowsWithTheSameFirmIssue"] = ignoreFirmWindows ? 1 : 0;
			parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(campaignIds.ToList());
			DataAccessor.ExecuteNonQuery("AddRangeIssues", parameters);
		}

		// ---------- Выделенные окна: массовые операции ----------

		private const string StepPoint = "rangeStep";

		/// <summary>
		/// Один шаг пачки — один вызов процедуры веера. Отказ по бизнес-правилу (RAISERROR с ключом iMessage)
		/// откатывает ТОЛЬКО этот шаг к точке сохранения и уходит в отказы: процедуры веера пишут станции курсором, и без
		/// отката шага на отказе станции k станции 1…k−1 остались бы записанными (красное окно). Остальные шаги пачки
		/// идут дальше. Любая другая ошибка (взаимоблокировка, таймаут) рвёт пачку — транзакцию откатит вызывающий.
		/// </summary>
		internal static bool Step(WindowsSource.BatchResult result, string name, int done, System.Action write) =>
			Step(result, name, done, () =>
			{
				write();
				return null;
			});

		/// <summary>
		/// То же, но запись может сама вернуть причину отказа (RollerSubstitute отдаёт «незамененные» строкой, не ошибкой):
		/// такой шаг тоже откатывается к точке и уходит в отказы.
		/// </summary>
		internal static bool Step(WindowsSource.BatchResult result, string name, int done, Func<string> write)
		{
			DataAccessor.SaveTransactionPoint(StepPoint);
			try
			{
				string refusal = write();
				if (refusal == null)
				{
					result.Done += done;
					return true;
				}
				DataAccessor.RollbackToTransactionPoint(StepPoint);
				result.Refuse(name, refusal);
				return false;
			}
			catch (SqlException ex) when (ex.Number == 50000 && ex.Class == 16)
			{
				DataAccessor.RollbackToTransactionPoint(StepPoint);
				result.Refuse(name, ErrorManager.GetErrorMessage(ex));
				return false;
			}
		}

		internal static string SlotName(DateTime slot) => slot.ToString("dd.MM.yyyy HH:mm");

		/// <summary>
		/// «Разместить ролик» в выделенные окна (десктоп — Insert): ролик и позиция кисти — AddRangeIssues на каждое окно,
		/// один пересчёт акции на всю пачку (десктоп — тоже один, после пакета). Done — число окон. Окна идут по
		/// возрастанию времени — один порядок блокировок у всех пачек.
		/// </summary>
		public static WindowsSource.BatchResult PlaceSlots(OpenRange r, IEnumerable<PlacementCell> cells, int rollerId, int position,
			WindowsSource.WeekView view)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			Roller roller = WindowsSource.BrushRoller(r, rollerId, action.FirmID);
			WindowsSource.BatchResult result = new WindowsSource.BatchResult();
			List<DateTime> slots = cells.Select(c => c.Date).Distinct().ToList();
			HashSet<int> before = IssueIdsIn(r, slots);
			WindowsSource.RunInTransaction(() =>
			{
				foreach (PlacementCell cell in cells.OrderBy(c => c.Key))
					Step(result, SlotName(cell.Date), 1, () => AddRange(r, cell.Date, roller, position, view));
				if (result.Done > 0)
					action.Recalculate(false);
			});
			if (result.Done > 0)
				result.Created.AddRange(TryCreatedSince(r, slots, before, (slot, issue) => issue.RollerId == roller.RollerId) ?? new List<WindowsSource.PlacedIssue>());
			return result;
		}

		// ---------- «Отменить последнее» ----------

		/// <summary>
		/// Номера выпусков отмеченных кампаний в слотах (RangeSlotIssues) — снимок «до» записи. AddRangeIssues номера поставленных
		/// выпусков не возвращает, поэтому поставленное находится как разность снимков до и после записи; оба чтения — вне
		/// транзакции записи: процедура читает окна всех выпусков акции и внутри транзакции ждала бы чужую запись, удерживая
		/// блокировки своих окон (взаимоблокировка с чужим пакетом по тем же станциям).
		/// </summary>
		internal static HashSet<int> IssueIdsIn(OpenRange r, IReadOnlyCollection<DateTime> slots) =>
			slots.Count == 0
				? new HashSet<int>()
				: new HashSet<int>(LoadSlotIssues(r, slots).Values.SelectMany(list => list).Select(i => i.IssueId));

		/// <summary>
		/// <see cref="CreatedSince"/> после коммита записи: сбой чтения уже записанного не отменяет размещение — он пишется в журнал,
		/// запись для «Отменить последнее» тогда не заводится (null).
		/// </summary>
		internal static List<WindowsSource.PlacedIssue> TryCreatedSince(OpenRange r, IReadOnlyCollection<DateTime> slots, HashSet<int> before,
			Func<DateTime, SlotIssue, bool> own)
		{
			try
			{
				return CreatedSince(r, slots, before, own);
			}
			catch (Exception ex)
			{
				Log.Error($"Reading the placed issues failed (action {r.ActionId}, {slots.Count} slot(s)): «Undo» will not see them", ex);
				return null;
			}
		}

		/// <summary>
		/// Выпуски, появившиеся в слотах после снимка <paramref name="before"/> и подходящие под <paramref name="own"/> (слот и
		/// выпуск — что записывалось): по слотам, внутри слота — в порядке отметки кампаний. Выпуск другого сеанса, попавший в тот
		/// же слот с другим роликом, в запись не попадает.
		/// </summary>
		internal static List<WindowsSource.PlacedIssue> CreatedSince(OpenRange r, IReadOnlyCollection<DateTime> slots, HashSet<int> before,
			Func<DateTime, SlotIssue, bool> own)
		{
			List<WindowsSource.PlacedIssue> created = new List<WindowsSource.PlacedIssue>();
			foreach (KeyValuePair<DateTime, List<SlotIssue>> bySlot in LoadSlotIssues(r, slots).OrderBy(x => x.Key))
				foreach (SlotIssue issue in bySlot.Value.OrderBy(i => CampaignOrder(r, i.CampaignId)).ThenBy(i => i.IssueId))
					if (!before.Contains(issue.IssueId) && own(bySlot.Key, issue))
						created.Add(new WindowsSource.PlacedIssue
						{
							IssueId = issue.IssueId,
							CampaignId = issue.CampaignId,
							WindowId = issue.WindowId,
							RollerId = issue.RollerId,
							Name = SlotName(bySlot.Key) + " — " + r.Titles[issue.CampaignId] + " — " + issue.RollerName,
						});
			return created;
		}

		/// <summary>
		/// «Отменить последнее» у веера: выпуски последнего размещения (<see cref="WindowsSource.PlacedIssue"/> разных кампаний
		/// веера). Правила те же, что у линейки (<see cref="WindowsSource.UndoCore"/>): пачки, сверка выпуска с записью, отказы,
		/// один пересчёт акции в конце.
		/// </summary>
		public static WindowsSource.BatchResult Undo(OpenRange r, IReadOnlyList<WindowsSource.PlacedIssue> placed)
		{
			if (placed.Any(p => !r.CampaignIds.Contains(p.CampaignId)))
				throw new InvalidOperationException(Tr.T("Отменять можно только выпуски отмеченных кампаний."));
			ActionOnMassmedia action = LoadForWrite(r);
			return WindowsSource.UndoCore(placed, "action " + r.ActionId, () => action.Recalculate(false));
		}

		/// <summary>Выпуски одного окна с одним роликом и одной позицией: у каких кампаний и сколько копий.</summary>
		internal sealed class SlotGroup
		{
			internal DateTime Slot { get; set; }
			internal int RollerId { get; set; }
			internal string RollerName { get; set; }
			internal int PositionId { get; set; }

			/// <summary>Кампания → число выпусков этого ролика с этой позицией в окне (дубли — больше одного).</summary>
			internal Dictionary<int, int> Copies { get; } = new Dictionary<int, int>();

			/// <summary>Сами выпуски группы — для замены ролика, которая работает по выпуску.</summary>
			internal List<SlotIssue> Issues { get; } = new List<SlotIssue>();

			internal int Count => Copies.Values.Sum();

			/// <summary>
			/// MasterIssueDelete снимает по одному выпуску на кампанию за вызов: в проход k входят кампании, у которых
			/// копий не меньше k, — каждый проход удаляет ровно столько выпусков, сколько в нём кампаний.
			/// </summary>
			internal IEnumerable<List<int>> DeletePasses()
			{
				for (int pass = 1; ; pass++)
				{
					List<int> campaigns = Copies.Where(c => c.Value >= pass).Select(c => c.Key).OrderBy(id => id).ToList();
					if (campaigns.Count == 0)
						yield break;
					yield return campaigns;
				}
			}
		}

		/// <summary>Содержимое выделенных окон по отмеченным кампаниям, сгруппированное по «ролик + позиция».</summary>
		public sealed class SlotContent
		{
			internal SlotContent(List<SlotGroup> groups)
			{
				Groups = groups;
			}

			internal List<SlotGroup> Groups { get; }

			/// <summary>Выпусков в базе — столько снимет удаление (по одному на кампанию и копию).</summary>
			public int Count => Groups.Sum(g => g.Count);

			public IReadOnlyList<int> RollerIds => Groups.Select(g => g.RollerId).Distinct().ToList();

			public SlotContent Only(IEnumerable<int> rollerIds)
			{
				HashSet<int> keep = new HashSet<int>(rollerIds);
				return new SlotContent(Groups.Where(g => keep.Contains(g.RollerId)).ToList());
			}

			/// <summary>Без выпусков этого ролика — «Заменить ролики» не трогает выпуски нового ролика.</summary>
			public SlotContent Without(int rollerId) => new SlotContent(Groups.Where(g => g.RollerId != rollerId).ToList());
		}

		/// <summary>Содержимое выделенных окон — RangeSlotIssues одним вызовом, как делает десктоп перед Del и Ctrl+R.</summary>
		public static SlotContent ReadSlots(OpenRange r, IEnumerable<PlacementCell> cells)
		{
			List<DateTime> slots = cells.Select(c => c.Date).Distinct().OrderBy(d => d).ToList();
			List<SlotGroup> groups = new List<SlotGroup>();
			if (slots.Count == 0)
				return new SlotContent(groups);

			Dictionary<DateTime, List<SlotIssue>> bySlot = LoadSlotIssues(r, slots);
			foreach (DateTime slot in slots)
				if (bySlot.TryGetValue(slot, out List<SlotIssue> issues))
					groups.AddRange(GroupsOf(slot, issues));
			return new SlotContent(groups);
		}

		/// <summary>Выпуски одного окна — группами «ролик + позиция» с числом копий у каждой кампании.</summary>
		private static List<SlotGroup> GroupsOf(DateTime slot, IEnumerable<SlotIssue> issues)
		{
			List<SlotGroup> groups = new List<SlotGroup>();
			foreach (IGrouping<(int RollerId, int PositionId), SlotIssue> g in issues.GroupBy(i => (i.RollerId, i.PositionId)))
			{
				SlotGroup group = new SlotGroup { Slot = slot, RollerId = g.Key.RollerId, PositionId = g.Key.PositionId, RollerName = g.First().RollerName };
				foreach (SlotIssue issue in g)
				{
					group.Copies.TryGetValue(issue.CampaignId, out int copies);
					group.Copies[issue.CampaignId] = copies + 1;
					group.Issues.Add(issue);
				}
				groups.Add(group);
			}
			return groups;
		}

		/// <summary>
		/// Содержимое окна для панели: уже прочитанные вместе с неделей выпуски (у цветных окон они есть всегда, у остальных — при «Номерах
		/// роликов»), иначе — RangeSlotIssues по этому окну. Перед записью состав всё равно перечитывается в транзакции.
		/// </summary>
		public static SlotContent ContentOf(OpenRange r, PlacementCell cell) =>
			cell.SlotIssuesLoaded ? new SlotContent(GroupsOf(cell.Date, cell.SlotIssues)) : ReadSlots(r, new[] { cell });

		/// <summary>Строка панели окна: группа «ролик + позиция» — сколько выпусков и у скольких кампаний.</summary>
		public sealed class SlotGroupInfo
		{
			internal SlotGroupInfo() { }

			/// <summary>Номер группы в <see cref="SlotContent"/> — по нему отмечаются группы для переноса.</summary>
			public int Index { get; internal set; }
			public int RollerId { get; internal set; }
			public string RollerName { get; internal set; }
			public int PositionId { get; internal set; }

			/// <summary>«Первый» / «Второй» / «Последний»; без позиции — пусто.</summary>
			public string PositionText { get; internal set; }
			public int Issues { get; internal set; }
			public int Campaigns { get; internal set; }

			/// <summary>Кампании, у которых группа стоит, — подписи по строке на кампанию (подсказка).</summary>
			public string CampaignTitles { get; internal set; }
		}

		public static IReadOnlyList<SlotGroupInfo> Describe(OpenRange r, SlotContent content)
		{
			List<SlotGroupInfo> items = new List<SlotGroupInfo>();
			for (int i = 0; i < content.Groups.Count; i++)
			{
				SlotGroup g = content.Groups[i];
				items.Add(new SlotGroupInfo
				{
					Index = i,
					RollerId = g.RollerId,
					RollerName = g.RollerName,
					PositionId = g.PositionId,
					PositionText = g.PositionId == 0 ? "" : Issue.GetPositionDisplayName((RollerPositions)g.PositionId),
					Issues = g.Count,
					Campaigns = g.Copies.Count,
					CampaignTitles = string.Join("\n", r.CampaignIds.Where(g.Copies.ContainsKey).Select(id => r.Titles[id])),
				});
			}
			return items;
		}

		// ---------- Перенос выпусков окна в другое окно ----------

		/// <summary>План переноса: какие группы окна-источника, куда, и есть ли в окне-приёмнике выпуск этой же фирмы.</summary>
		public sealed class MovePlan
		{
			internal MovePlan() { }

			internal DateTime Source { get; set; }
			internal DateTime Target { get; set; }
			internal List<SlotGroup> Groups { get; } = new List<SlotGroup>();

			/// <summary>Групп «ролик + позиция» к переносу.</summary>
			public int Count => Groups.Count;

			/// <summary>В окне-приёмнике на станциях переносимых кампаний уже есть выпуск этой фирмы (из любой акции) — предупреждение, не запрет.</summary>
			public bool FirmConflict { get; internal set; }

			/// <summary>Среди этих выпусков есть выпуск подтверждённой акции — для текста предупреждения.</summary>
			public bool AnyConfirmed { get; internal set; }
		}

		/// <summary>
		/// План переноса отмеченных групп окна <paramref name="source"/> в окно <paramref name="target"/>; группа переезжает в том составе
		/// кампаний, в котором стояла (иначе перенос размножил бы выпуск на остальные кампании). Проверка фирмы — RangeSlotFirmConflict,
		/// как в десктопном перетаскивании: только сигнал для диалога, реальную проверку при записи делает AddRangeIssues.
		/// </summary>
		public static MovePlan PlanMove(OpenRange r, SlotContent content, IEnumerable<int> groupIndexes, DateTime source, DateTime target)
		{
			MovePlan plan = new MovePlan { Source = source, Target = target };
			HashSet<int> wanted = new HashSet<int>(groupIndexes);
			for (int i = 0; i < content.Groups.Count; i++)
				if (wanted.Contains(i))
					plan.Groups.Add(content.Groups[i]);

			HashSet<int> campaigns = new HashSet<int>(plan.Groups.SelectMany(g => g.Copies.Keys));
			if (campaigns.Count == 0)
				return plan;

			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Action.ParamNames.ActionId] = r.ActionId;
			parameters["issueDate"] = target;
			parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(campaigns.ToList());
			DataTable table = DataAccessor.LoadDataSet("RangeSlotFirmConflict", parameters).Tables[0];
			if (table.Rows.Count > 0)
			{
				plan.FirmConflict = ParseHelper.GetBooleanFromObject(table.Rows[0]["hasConflict"], false);
				plan.AnyConfirmed = ParseHelper.GetBooleanFromObject(table.Rows[0]["anyConfirmed"], false);
			}
			return plan;
		}

		/// <summary>Состав группы для сравнения «не изменилась ли»: ролик, позиция, кампании и число копий.</summary>
		private static string Signature(SlotGroup g) =>
			g.RollerId + "/" + g.PositionId + ":" + string.Join(",", g.Copies.OrderBy(c => c.Key).Select(c => c.Key + "x" + c.Value));

		/// <summary>
		/// Перенос — как десктопное MoveRangeIssuesToSlot, всё или ничего одной транзакцией: сначала удаляются все группы окна-источника
		/// (MasterIssueDelete по кампаниям группы, столько проходов, сколько копий), затем те же ролики с теми же позициями и в тех же
		/// кампаниях ставятся в окно-приёмник (AddRangeIssues), один пересчёт. Отказ любой станции — откат всего переноса, сообщение
		/// процедуры. Десктопный путь по группам теряет дубли (за вызов снимается и ставится по одному выпуску на кампанию) — здесь
		/// копии переносятся все. Между выбором групп и окна-приёмника проходит время, поэтому состав окна-источника перечитывается в
		/// транзакции: изменившаяся группа отменяет перенос — иначе удалённый выпуск воскрес бы.
		/// </summary>
		public static void MoveGroups(OpenRange r, MovePlan plan, WindowsSource.WeekView view)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			WindowsSource.RunInTransaction(() =>
			{
				Dictionary<DateTime, List<SlotIssue>> now = LoadSlotIssues(r, new[] { plan.Source });
				Dictionary<string, string> current = GroupsOf(plan.Source, now.TryGetValue(plan.Source, out List<SlotIssue> issues) ? issues : new List<SlotIssue>())
					.ToDictionary(g => g.RollerId + "/" + g.PositionId, Signature);
				foreach (SlotGroup group in plan.Groups)
					if (!current.TryGetValue(group.RollerId + "/" + group.PositionId, out string signature) || signature != Signature(group))
						throw new InvalidOperationException(Tr.T("Выпуск изменили или удалили, пока выбиралось окно. Выберите выпуски заново."));

				foreach (SlotGroup group in plan.Groups)
					foreach (List<int> campaigns in group.DeletePasses())
					{
						Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
						parameters[Action.ParamNames.ActionId] = r.ActionId;
						parameters["issueDate"] = plan.Source;
						parameters["rollerID"] = group.RollerId;
						parameters["positionId"] = group.PositionId;
						parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(campaigns);
						DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters);
					}

				foreach (SlotGroup group in plan.Groups)
				{
					int duration = group.Issues[0].Duration;
					foreach (List<int> campaigns in group.DeletePasses())
						AddRangeFor(r, plan.Target, group.RollerId, duration, group.PositionId, campaigns, view);
				}
				action.Recalculate(false);
			});
		}

		// ---------- Дубли и «до полного пересечения» ----------

		private static IEnumerable<(DateTime Slot, SlotIssue Issue)> AllIssues(SlotContent content) =>
			content.Groups.SelectMany(g => g.Issues.Select(i => (g.Slot, i)));

		/// <summary>Номер из списка «Ролики», как в ячейке; ролика в списке нет — по названию.</summary>
		private static string RollerLabel(int rollerId, string rollerName, IReadOnlyDictionary<int, int> numbers) =>
			numbers.TryGetValue(rollerId, out int number) ? Tr.Format("ролик {0}", number) : Tr.Format("ролик «{0}»", rollerName);

		private static string IssueTarget(OpenRange r, DateTime slot, int campaignId, int rollerId, string rollerName, IReadOnlyDictionary<int, int> numbers) =>
			SlotName(slot) + " — " + r.Titles[campaignId] + " — " + RollerLabel(rollerId, rollerName, numbers);

		private static int CampaignOrder(OpenRange r, int campaignId)
		{
			for (int i = 0; i < r.CampaignIds.Count; i++)
				if (r.CampaignIds[i] == campaignId)
					return i;
			return int.MaxValue;
		}

		/// <summary>Сколько лишних выпусков одного ролика снять у одной кампании в одном окне.</summary>
		internal sealed class Removal
		{
			internal DateTime Slot { get; set; }
			internal int CampaignId { get; set; }
			internal int RollerId { get; set; }
			internal string RollerName { get; set; }
			internal int PositionId { get; set; }
			internal int Extra { get; set; }
		}

		/// <summary>План удаления дублей и окна, пропущенные из-за разного позиционирования дублей (<see cref="Problems"/>).</summary>
		public sealed class DuplicatePlan
		{
			internal DuplicatePlan() { }

			internal List<Removal> Removals { get; } = new List<Removal>();

			/// <summary>Окна, где у дублей одного ролика одной кампании разная позиция — неизвестно, какой оставить.</summary>
			public WindowsSource.BatchResult Problems { get; } = new WindowsSource.BatchResult();

			public int SkippedWindows { get; internal set; }

			/// <summary>Выпусков к удалению.</summary>
			public int Count => Removals.Sum(x => x.Extra);
		}

		/// <summary>
		/// План удаления дублей (EditIssuesForm.PlanDuplicateRemovals): в каждом окне у каждой кампании каждый ролик — не больше
		/// одного выпуска. Окно, где у дублей одного ролика одной кампании разные позиции, не трогается целиком и попадает в
		/// <see cref="DuplicatePlan.Problems"/>. Позиции роликов без дублей на решение не влияют.
		/// </summary>
		public static DuplicatePlan PlanDuplicates(OpenRange r, SlotContent content, IReadOnlyDictionary<int, int> numbers)
		{
			DuplicatePlan plan = new DuplicatePlan();
			foreach (IGrouping<DateTime, (DateTime Slot, SlotIssue Issue)> window in AllIssues(content).GroupBy(x => x.Slot).OrderBy(g => g.Key))
			{
				List<Removal> removals = new List<Removal>();
				List<string> conflicts = new List<string>();
				foreach (IGrouping<(int CampaignId, int RollerId), (DateTime Slot, SlotIssue Issue)> pair in window
					.GroupBy(x => (x.Issue.CampaignId, x.Issue.RollerId))
					.OrderBy(g => CampaignOrder(r, g.Key.CampaignId)).ThenBy(g => g.Key.RollerId))
				{
					List<SlotIssue> issues = pair.Select(x => x.Issue).ToList();
					if (issues.Count < 2)
						continue;
					if (issues.Select(i => i.PositionId).Distinct().Count() > 1)
					{
						conflicts.Add(r.Titles[pair.Key.CampaignId] + " – " + RollerLabel(pair.Key.RollerId, issues[0].RollerName, numbers));
						continue;
					}
					removals.Add(new Removal
					{
						Slot = window.Key,
						CampaignId = pair.Key.CampaignId,
						RollerId = pair.Key.RollerId,
						RollerName = issues[0].RollerName,
						PositionId = issues[0].PositionId,
						Extra = issues.Count - 1,
					});
				}

				if (conflicts.Count > 0)
				{
					plan.SkippedWindows++;
					plan.Problems.Refuse(SlotName(window.Key), Tr.Format("Окно пропущено: у дублей разное позиционирование — {0}", string.Join("; ", conflicts)));
				}
				else
					plan.Removals.AddRange(removals);
			}
			return plan;
		}

		/// <summary>
		/// «Удалить дубли» (EditIssuesForm.DeleteDuplicatesInSelectedWindows): каждый лишний выпуск — отдельный вызов MasterIssueDelete по
		/// одной кампании (шаг с откатом к точке); отказ (прошлое, закрытый день) — в отказы, остальные идут дальше, повторы того же
		/// выпуска пропускаются (процедура выберет тот же выпуск — отказ будет тот же), в строке отказа — сколько не удалено. Один пересчёт.
		/// Done — число удалённых.
		///
		/// План строился до диалога подтверждения, а пока он открыт, дубль могли снять в другом окне: MasterIssueDelete не знает «оставить
		/// один» и снял бы единственный выпуск. Поэтому внутри транзакции состав окон читается заново, и снимается не больше «есть − 1».
		/// </summary>
		public static WindowsSource.BatchResult RemoveDuplicates(OpenRange r, DuplicatePlan plan, IReadOnlyDictionary<int, int> numbers)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			WindowsSource.BatchResult result = plan.Problems;
			WindowsSource.RunInTransaction(() =>
			{
				Dictionary<DateTime, List<SlotIssue>> actual = plan.Removals.Count == 0
					? new Dictionary<DateTime, List<SlotIssue>>()
					: LoadSlotIssues(r, plan.Removals.Select(x => x.Slot).Distinct());
				foreach (Removal removal in plan.Removals)
				{
					int have = actual.TryGetValue(removal.Slot, out List<SlotIssue> inSlot)
						? inSlot.Count(x => x.CampaignId == removal.CampaignId && x.RollerId == removal.RollerId)
						: 0;
					int extra = Math.Min(removal.Extra, have - 1);
					for (int i = 0; i < extra; i++)
					{
						Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
						parameters[Action.ParamNames.ActionId] = r.ActionId;
						parameters["issueDate"] = removal.Slot;
						parameters["rollerID"] = removal.RollerId;
						parameters["positionId"] = removal.PositionId;
						parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(new[] { removal.CampaignId });
						if (!Step(result, IssueTarget(r, removal.Slot, removal.CampaignId, removal.RollerId, removal.RollerName, numbers), 1,
							() => DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters)))
						{
							DataRow refused = result.Errors.Rows[result.Errors.Rows.Count - 1];
							refused["description"] = Tr.Format("Не удалено {0} шт.: {1}", extra - i, refused["description"]);
							break;
						}
					}
				}
				if (result.Done > 0)
					action.Recalculate(false);
			});
			return result;
		}

		/// <summary>Один недостающий выпуск: куда, какой ролик и с какой позицией ставить.</summary>
		internal sealed class Addition
		{
			internal DateTime Slot { get; set; }
			internal int CampaignId { get; set; }
			internal int RollerId { get; set; }
			internal string RollerName { get; set; }
			internal int Duration { get; set; }
			internal int PositionId { get; set; }
		}

		public sealed class AdditionPlan
		{
			internal AdditionPlan() { }

			internal List<Addition> Additions { get; } = new List<Addition>();

			public int Count => Additions.Count;
		}

		/// <summary>
		/// План выравнивания (EditIssuesForm.PlanRollerAdditions): в каждом окне по каждому ролику цель — наибольшее число среди
		/// отмеченных кампаний (нуль считается). Лидер — первая в порядке отметки кампания с наибольшим числом; недостающим достаются
		/// позиции лидера за вычетом тех, что у них уже есть; если их больше дефицита — сначала особые (первая, вторая, последняя), потом
		/// без позиции (решение владельца).
		/// </summary>
		public static AdditionPlan PlanAdditions(OpenRange r, SlotContent content)
		{
			AdditionPlan plan = new AdditionPlan();
			foreach (IGrouping<DateTime, (DateTime Slot, SlotIssue Issue)> window in AllIssues(content).GroupBy(x => x.Slot).OrderBy(g => g.Key))
				foreach (IGrouping<int, (DateTime Slot, SlotIssue Issue)> roller in window.GroupBy(x => x.Issue.RollerId).OrderBy(g => g.Key))
				{
					Dictionary<int, List<int>> positions = r.CampaignIds.ToDictionary(id => id, id => new List<int>());
					foreach ((DateTime Slot, SlotIssue Issue) row in roller)
						if (positions.TryGetValue(row.Issue.CampaignId, out List<int> own))
							own.Add(row.Issue.PositionId);

					int max = positions.Values.Max(p => p.Count);
					List<int> leader = positions[r.CampaignIds.First(id => positions[id].Count == max)];
					SlotIssue sample = roller.First().Issue;

					foreach (int campaignId in r.CampaignIds)
					{
						List<int> own = positions[campaignId];
						int deficit = max - own.Count;
						if (deficit == 0)
							continue;

						List<int> missing = new List<int>(leader);
						foreach (int position in own)
							missing.Remove(position);
						foreach (int position in missing.OrderBy(p => p == 0 ? 1 : 0).ThenBy(p => p).Take(deficit))
							plan.Additions.Add(new Addition
							{
								Slot = window.Key,
								CampaignId = campaignId,
								RollerId = roller.Key,
								RollerName = sample.RollerName,
								Duration = sample.Duration,
								PositionId = position,
							});
					}
				}
			return plan;
		}

		// Отказы IssueIUD «позиция в окне занята»: подтверждённым выпуском (hlp_IssueVerify) или выпуском этой же акции — в черновике
		// занятость своей акцией приходит только вторым ключом.
		private static readonly HashSet<string> PositionBusyErrors = new HashSet<string> { "FirstLastIssueError", "PositionErrorForTheSameAction" };

		/// <summary>
		/// «Добавить до полного пересечения» (EditIssuesForm.FillRollersToIntersectionInSelectedWindows): best-effort, каждый выпуск —
		/// отдельный вызов AddRangeIssues по одной кампании (шаг с откатом к точке); занятая позиция — повтор без позиции
		/// (<paramref name="withoutPosition"/> — сколько их); любой другой отказ — в отказы. Один пересчёт. Done — число добавленных.
		/// </summary>
		public static WindowsSource.BatchResult FillToIntersection(OpenRange r, AdditionPlan plan, WindowsSource.WeekView view,
			IReadOnlyDictionary<int, int> numbers, out int withoutPosition)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			WindowsSource.BatchResult result = new WindowsSource.BatchResult();
			int without = 0;
			List<DateTime> slots = plan.Additions.Select(a => a.Slot).Distinct().ToList();
			HashSet<(DateTime, int, int)> targets = new HashSet<(DateTime, int, int)>(plan.Additions.Select(a => (a.Slot, a.CampaignId, a.RollerId)));
			HashSet<int> before = IssueIdsIn(r, slots);
			WindowsSource.RunInTransaction(() =>
			{
				foreach (Addition a in plan.Additions)
					Step(result, IssueTarget(r, a.Slot, a.CampaignId, a.RollerId, a.RollerName, numbers), 1, () =>
					{
						try
						{
							AddRangeFor(r, a.Slot, a.RollerId, a.Duration, a.PositionId, new[] { a.CampaignId }, view);
						}
						catch (SqlException ex) when (a.PositionId != 0 && PositionBusyErrors.Contains(ex.Message))
						{
							// Откат к точке шага: курсор мог успеть записать часть; повтор — без позиции.
							DataAccessor.RollbackToTransactionPoint(StepPoint);
							AddRangeFor(r, a.Slot, a.RollerId, a.Duration, 0, new[] { a.CampaignId }, view);
							without++;
						}
					});
				if (result.Done > 0)
					action.Recalculate(false);
			});
			if (result.Done > 0)
				result.Created.AddRange(TryCreatedSince(r, slots, before, (slot, issue) => targets.Contains((slot, issue.CampaignId, issue.RollerId)))
					?? new List<WindowsSource.PlacedIssue>());
			withoutPosition = without;
			return result;
		}

		/// <summary>
		/// «Заменить ролики» в выделенных окнах (десктоп — Ctrl+R): RollerSubstitute на каждый выпуск (@issueID + окно выхода;
		/// та же процедура, что у замены в сетке одной кампании). Десктоп зовёт её по паре «кампания + старый ролик» с таблицей
		/// дней, но та загрузка идёт мимо транзакции ядра (свои соединение и транзакция) — в одной транзакции пачки веба
		/// годится только по выпуску; итог тот же, замена и так идёт по выпускам окон. Отказы правил процедуры («незамененные»:
		/// прошлое, закрытый день, переполнение окна, позиция…) — в отказы с её текстом; пересчёт акции — один, и только если
		/// длина ролика изменилась (ролик той же длины цену не меняет). Done — число замен.
		/// </summary>
		public static WindowsSource.BatchResult ReplaceSlots(OpenRange r, SlotContent content, int newRollerId)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			Roller newRoller = WindowsSource.BrushRoller(r, newRollerId, action.FirmID);
			Dictionary<int, Roller> oldRollers = content.RollerIds.ToDictionary(id => id, id => new Roller(id));
			Dictionary<int, Campaign> campaigns = new Dictionary<int, Campaign>();
			foreach (int campaignId in content.Groups.SelectMany(g => g.Copies.Keys).Distinct())
				campaigns[campaignId] = Campaign.GetCampaignById(campaignId);

			bool durationChanged = false;
			WindowsSource.BatchResult result = new WindowsSource.BatchResult();
			WindowsSource.RunInTransaction(() =>
			{
				foreach (SlotGroup group in content.Groups.OrderBy(g => g.Slot).ThenBy(g => g.RollerId).ThenBy(g => g.PositionId))
				{
					Roller oldRoller = oldRollers[group.RollerId];
					foreach (SlotIssue issue in group.Issues)
					{
						string name = SlotName(group.Slot) + " — " + r.Titles[issue.CampaignId] + " — " + group.RollerName;
						Step(result, name, 1, () =>
						{
							DataTable unsubstituted = CampaignRoller.ApplyRollerSubstitutionForIssue(campaigns[issue.CampaignId], oldRoller,
								newRoller, issue.IssueId, issue.WindowId);
							if (unsubstituted != null && unsubstituted.Rows.Count > 0)
								return Convert.ToString(unsubstituted.Rows[0]["message"]);
							durationChanged |= oldRoller.Duration != newRoller.Duration;
							return null;
						});
					}
				}
				if (durationChanged)
					action.Recalculate(false);
			});
			return result;
		}

		/// <summary>
		/// «Удалить выпуски» в выделенных окнах (десктоп — Del): MasterIssueDelete по каждой группе «окно + ролик + позиция»
		/// и только по тем кампаниям, где она стоит (частичное окно не размножается на остальные), столько проходов, сколько
		/// копий у кампании с наибольшим их числом. Каждый вызов — шаг с откатом к точке (как отдельная транзакция на вызов
		/// в десктопе); отказ (прошлое, закрытый день у подтверждённой акции) — в отказы, остальное удаляется. Один пересчёт.
		/// Done — число удалённых выпусков.
		/// </summary>
		public static WindowsSource.BatchResult RemoveSlots(OpenRange r, SlotContent content)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			WindowsSource.BatchResult result = new WindowsSource.BatchResult();
			WindowsSource.RunInTransaction(() =>
			{
				foreach (SlotGroup group in content.Groups.OrderBy(g => g.Slot).ThenBy(g => g.RollerId).ThenBy(g => g.PositionId))
					foreach (List<int> campaigns in group.DeletePasses())
					{
						Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
						parameters[Action.ParamNames.ActionId] = r.ActionId;
						parameters["issueDate"] = group.Slot;
						parameters["rollerID"] = group.RollerId;
						parameters["positionId"] = group.PositionId;
						parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(campaigns);
						Step(result, SlotName(group.Slot) + " — " + group.RollerName, campaigns.Count,
							() => DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters));
					}
				if (result.Done > 0)
					action.Recalculate(false);
			});
			return result;
		}
	}
}
