using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
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
		private static ActionOnMassmedia LoadForWrite(OpenRange r)
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
				Monday = monday,
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
		private static bool PositionFree(DataRow d, WindowsSource.WeekView view)
		{
			string occupied, unconfirmed;
			switch ((RollerPositions)view.Position)
			{
				case RollerPositions.First: occupied = "isFirstPositionOccupied"; unconfirmed = "firstPositionsUnconfirmed"; break;
				case RollerPositions.Second: occupied = "isSecondPositionOccupied"; unconfirmed = "secondPositionsUnconfirmed"; break;
				case RollerPositions.Last: occupied = "isLastPositionOccupied"; unconfirmed = "lastPositionsUnconfirmed"; break;
				default: return true;
			}
			return !Flag(d, occupied) && (!view.ShowUnconfirmed || IntOrZero(d[unconfirmed]) == 0);
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
				});
			}
			return result;
		}

		// ---------- Постановка ----------

		/// <summary>
		/// Клик «Режима добавления» — как TariffWithRangeGrid.AddIssuesRange: один ролик с позицией
		/// «Позиционирования» на всех отмеченных станциях слота, затем пересчёт акции — одной транзакцией.
		/// Отказ любой станции (позиция занята, нет места, выпуск этой фирмы в окне…) откатывает слот
		/// целиком и показывается пользователю текстом процедуры. Неделя после записи перечитывается.
		/// </summary>
		public static void PlaceSlot(OpenRange r, PlacementCell cell, int rollerId, int position, WindowsSource.WeekView view)
		{
			ActionOnMassmedia action = LoadForWrite(r);
			Roller roller = WindowsSource.BrushRoller(r, rollerId, action.FirmID);

			WindowsSource.RunInTransaction(() =>
			{
				AddRange(r, cell.Date, roller, position, view);
				action.Recalculate(false);
			});
		}

		/// <summary>AddRangeIssues: ролик с позицией на всех отмеченных станциях получаса <paramref name="slot"/>.</summary>
		private static void AddRange(OpenRange r, DateTime slot, Roller roller, int position, WindowsSource.WeekView view)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Action.ParamNames.ActionId] = r.ActionId;
			parameters["issueDate"] = slot;
			parameters["rollerID"] = roller.RollerId;
			parameters["rollerDuration"] = roller.Duration;
			parameters["positionId"] = position;
			parameters["considerUnconfirmed"] = view.ShowUnconfirmed ? 1 : 0;
			parameters["ignoreWindowsWithTheSameFirmIssue"] = 0;
			parameters[Campaign.ParamNames.CampaignIds] = Action.BuildCampaignIdsCsv(r.CampaignIds.ToList());
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
		private static void Step(WindowsSource.BatchResult result, string name, int done, System.Action write)
		{
			DataAccessor.SaveTransactionPoint(StepPoint);
			try
			{
				write();
				result.Done += done;
			}
			catch (SqlException ex) when (ex.Number == 50000 && ex.Class == 16)
			{
				DataAccessor.RollbackToTransactionPoint(StepPoint);
				result.Refuse(name, ErrorManager.GetErrorMessage(ex));
			}
		}

		private static string SlotName(DateTime slot) => slot.ToString("dd.MM.yyyy HH:mm");

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
			WindowsSource.RunInTransaction(() =>
			{
				foreach (PlacementCell cell in cells.OrderBy(c => c.Key))
					Step(result, SlotName(cell.Date), 1, () => AddRange(r, cell.Date, roller, position, view));
				if (result.Done > 0)
					action.Recalculate(false);
			});
			return result;
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
			{
				if (!bySlot.TryGetValue(slot, out List<SlotIssue> issues))
					continue;
				foreach (IGrouping<(int RollerId, int PositionId), SlotIssue> g in issues.GroupBy(i => (i.RollerId, i.PositionId)))
				{
					SlotGroup group = new SlotGroup { Slot = slot, RollerId = g.Key.RollerId, PositionId = g.Key.PositionId, RollerName = g.First().RollerName };
					foreach (SlotIssue issue in g)
					{
						group.Copies.TryGetValue(issue.CampaignId, out int copies);
						group.Copies[issue.CampaignId] = copies + 1;
					}
					groups.Add(group);
				}
			}
			return new SlotContent(groups);
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
