using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;

namespace Merlin.Classes
{
	/// <summary>
	/// Вкладка «Рекламные окна» страницы акции, одна линейная кампания (docs/tasks/web-action-forms.md,
	/// этап 2) — то, что в десктопе делают CampaignForm и RollerIssuesGrid3 в линейном режиме: неделя
	/// окон станции с отметками кампании, «кисть» (ролик и позиция), постановка кликом, выпуски окна,
	/// массовые операции по выделенным окнам, перенос выпусков в другое окно.
	///
	/// Чтение — те же процедуры и параметры, что у десктопа (PricelistByDate, TariffWindowRetrieve с
	/// useActualTime, Grid, TariffWindowWithAdvertTypeRetrieve), отметка «свой выпуск» и счётчики — по
	/// окну выхода, как в десктопе (правило 07.10.2026, отменило В-10). Запись — IssueIUD, RollerSubstitute
	/// и ActionRecalculate: одно действие пользователя — одна транзакция и один пересчёт акции. В пачке
	/// отказ по бизнес-правилу (RAISERROR с ключом iMessage) уходит в итог, остальные места пишутся:
	/// у IssueIUD AddItem/DeleteItem и RollerSubstitute все отказы стоят до записи. Любая другая
	/// ошибка (взаимоблокировка, таймаут) откатывает действие целиком. Перенос — «всё или ничего»,
	/// как десктопное перетаскивание.
	///
	/// Права проверяет фасад перед каждой записью (план, §3.9): процедуры о «своей / чужой» акции не
	/// знают, а IssueIUD не проверяет ни станцию окна, ни фирму ролика — окно берётся только из модели
	/// недели, ролик — только из списка роликов фирмы.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class WindowsSource
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsSource));

		// ---------- Кампании вкладки ----------

		/// <summary>Линейная кампания — её размещение показывает вкладка «Рекламные окна».</summary>
		public static bool IsLinear(DataRow campaign) =>
			ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.CampaignTypeId], 0) == (int)Campaign.CampaignTypes.Simple;

		public static int CampaignIdOf(DataRow campaign) => Convert.ToInt32(campaign[Campaign.ParamNames.CampaignId]);

		/// <summary>
		/// Подпись кампании: радиостанция с группой и тип оплаты — в акции бывают две линейные
		/// кампании одной станции с разным типом оплаты.
		/// </summary>
		public static string CampaignCaption(DataRow campaign, bool withAgency = false)
		{
			// В базе у названий станций бывает пробел на конце («Русское радио ») — в подсказках он виден.
			string caption = string.Format("{0} ({1}), {2}", Convert.ToString(campaign[Campaign.ParamNames.MassmediaName]).Trim(),
				Convert.ToString(campaign[Campaign.ParamNames.GroupName]).Trim(), campaign[PaymentTypeNameColumn]);
			if (!withAgency || !campaign.Table.Columns.Contains(AgencyNameColumn))
				return caption;
			string agency = Convert.ToString(campaign[AgencyNameColumn]);
			return string.IsNullOrWhiteSpace(agency) ? caption : caption + ", " + agency.Trim();
		}

		private const string AgencyNameColumn = "agencyName";

		private const string PaymentTypeNameColumn = "paymentTypeName";

		/// <summary>
		/// Строки статистики кампании — как список под роликами в CampaignForm
		/// (Campaign.DisplayCampaignData), те же подписи.
		/// </summary>
		public static IReadOnlyList<KeyValuePair<string, string>> CampaignStats(DataRow campaign)
		{
			return new List<KeyValuePair<string, string>>
			{
				Stat(Tr.T("Начало"), DateText(campaign[Campaign.ParamNames.StartDate])),
				Stat(Tr.T("Окончание"), DateText(campaign[Campaign.ParamNames.FinishDate])),
				Stat(Tr.T("Выпусков"), ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.IssuesCount], 0).ToString()),
				Stat(Tr.T("Общее время"), DateTimeUtils.Time2String(ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.IssuesDuration], 0))),
				Stat(Tr.T("Цена по тарифам"), ParseHelper.GetDecimalFromObject(campaign[Campaign.ParamNames.TariffPrice], 0).ToString("C")),
				Stat(Tr.T("Объёмная скидка"), ParseHelper.GetDecimalFromObject(campaign[Campaign.ParamNames.Discount], 1).ToString("0.00")),
				Stat(Tr.T("Цена с учётом объёмной скидки"), ParseHelper.GetDecimalFromObject(campaign[Campaign.ParamNames.Price], 0).ToString("C")),
			};
		}

		private static KeyValuePair<string, string> Stat(string caption, string value) => new KeyValuePair<string, string>(caption, value);

		private static string DateText(object value) => value is DateTime d ? d.ToString("dd.MM.yyyy") : "";

		// ---------- Открытая кампания ----------

		/// <summary>
		/// То, с чем работает вкладка и чему нужны ролики фирмы (кисть): одна кампания (WindowsSource)
		/// или набор отмеченных кампаний веера (RangeSource). Ролики — фирмы акции, не станции.
		/// </summary>
		public interface IBrushScope
		{
			int FirmId { get; }

			/// <summary>Можно ли менять размещение (права на акцию и кампании).</summary>
			bool CanEdit { get; }

			/// <summary>Список роликов фирмы, показанный последним, — ролик кисти берётся только из него.</summary>
			DataTable Rollers { get; set; }
		}

		/// <summary>
		/// Кампания, открытая на вкладке. Сам Campaign — internal; для записи он каждый раз читается
		/// заново (акцию могли активировать или изменить в другом окне — от этого зависят проверки
		/// IssueIUD и урезание оплат после пересчёта).
		/// </summary>
		public sealed class OpenCampaign : IBrushScope
		{
			internal OpenCampaign() { }

			public int CampaignId { get; internal set; }
			public int ActionId { get; internal set; }
			public int MassmediaId { get; internal set; }
			public int FirmId { get; internal set; }

			/// <summary>Можно ли менять размещение: «Редактировать» у акции и у кампании.</summary>
			public bool CanEdit { get; internal set; }

			/// <summary>День первого выпуска; null — выпусков нет. С этой недели открывается сетка.</summary>
			public DateTime? FirstIssueDate { get; internal set; }

			public DataTable Rollers { get; set; }
		}

		/// <summary>Линейная кампания этой акции; null — нет такой (удалена, другая акция, не линейная).</summary>
		public static OpenCampaign Open(ActionOnMassmedia action, int campaignId)
		{
			Campaign campaign = Campaign.GetCampaignById(campaignId);
			if (campaign == null || campaign.ActionId != action.ActionId || campaign.CampaignType != Campaign.CampaignTypes.Simple)
				return null;

			return new OpenCampaign
			{
				CampaignId = campaignId,
				ActionId = action.ActionId,
				MassmediaId = ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.MassmediaId], 0),
				FirmId = action.FirmID,
				CanEdit = CanEdit(action, campaign),
				FirstIssueDate = campaign.StartDate == DateTime.MinValue ? (DateTime?)null : campaign.StartDate.Date,
			};
		}

		/// <summary>
		/// Править размещение — как вход в CampaignForm из ActionForm (campaign.IsActionEnabled(Edit):
		/// права группы и правило своих / чужих акций) плюс «Редактировать» самой акции (В-7).
		/// </summary>
		private static bool CanEdit(ActionOnMassmedia action, Campaign campaign) =>
			ActionWorkspace.CanEdit(action) && campaign.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal);

		/// <summary>Свежая кампания для записи; без права — отказ.</summary>
		internal static Campaign LoadForWrite(OpenCampaign c)
		{
			Campaign campaign = Campaign.GetCampaignById(c.CampaignId);
			if (campaign == null)
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			if (!CanEdit(campaign.Action, campaign))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			return campaign;
		}

		// ---------- Неделя ----------

		/// <summary>Переключатели вида, от которых зависит чтение недели.</summary>
		public sealed class WeekView
		{
			/// <summary>«Учитывать макеты»: остаток, позиции и выпуски фирмы — с неподтверждёнными.</summary>
			public bool ShowUnconfirmed { get; set; } = true;

			/// <summary>«Показать заблокированные окна»: без неё такие окна не читаются вовсе (как в десктопе).</summary>
			public bool ShowDisabled { get; set; }

			/// <summary>«Позиционирование»: 0 — «Показывать всё», иначе RollerPositions (-20, -10, 10).</summary>
			public int Position { get; set; }

			/// <summary>«Предметы рекламы»: null — «Показывать всё».</summary>
			public int? AdvertTypeId { get; set; }

			/// <summary>true — «где есть предмет рекламы», false — «где нет».</summary>
			public bool AdvertTypeExists { get; set; } = true;
		}

		/// <summary>
		/// Неделя, содержащая <paramref name="anyDate"/>, по прайс-листу станции на эту дату (или
		/// ближайшему будущему — PricelistByDate), обрезанная его сроком. Дата вне срока прижимается
		/// к ближайшей границе.
		/// </summary>
		public static PlacementWeek LoadWeek(OpenCampaign c, DateTime anyDate, WeekView view)
		{
			Massmedia massmedia = Massmedia.GetMassmediaByID(c.MassmediaId);
			DateTime date = anyDate.Date;
			MassmediaPricelist p = massmedia.GetPriceList(date) as MassmediaPricelist;
			if (p == null)
			{
				DateTime monday = MondayOf(date);
				PlacementWeek empty = new PlacementWeek { FirstDay = monday, StartDate = monday, FinishDate = monday.AddDays(PlacementWeek.DaysInWeek - 1), NoPricelist = true };
				return empty;
			}

			if (date < p.StartDate.Date) date = p.StartDate.Date;
			if (date > p.FinishDate.Date) date = p.FinishDate.Date;
			DateTime weekMonday = MondayOf(date);
			DateTime start = weekMonday < p.StartDate.Date ? p.StartDate.Date : weekMonday;
			DateTime finish = weekMonday.AddDays(PlacementWeek.DaysInWeek - 1);
			if (finish > p.FinishDate.Date) finish = p.FinishDate.Date;

			// Как RollerIssuesGrid3 у линейной кампании: без особых окон и модульных тарифов, время
			// строки — фактическое (гибридная раскладка, docs/tasks/tariff-window-actual-time.md).
			p.ExcludeSpecialWindows = true;
			p.ExcludeModuleTariffs = true;
			DataSet windows = p.GetTariffWindows(start, finish, null, false, view.ShowDisabled, useActualTime: true);
			DataSet issues = massmedia.GetRollerCells(p, start, finish, null, view.ShowUnconfirmed, CampaignRef(c),
				(RollerPositions)view.Position);
			HashSet<int> advertWindows = view.AdvertTypeId.HasValue
				? AdvertTypeWindows(view, startDate: start, finishDate: finish, pricelistId: p.PricelistId, windowId: null)
				: null;

			PlacementWeek week = new PlacementWeek
			{
				FirstDay = weekMonday,
				StartDate = start,
				FinishDate = finish,
				PricelistStart = p.StartDate.Date,
				PricelistFinish = p.FinishDate.Date,
			};
			WeekIssues marks = new WeekIssues(issues);
			week.Rows = BuildRows(week, windows.Tables["time"], windows.Tables[Constants.TableNames.Data], marks, view, advertWindows);

			for (int d = 0; d < PlacementWeek.DaysInWeek; d++)
				week.DayTotals[d] = week.IsInRange(d) ? marks.CountOfDay(d) : (int?)null;
			return week;
		}

		private static DateTime MondayOf(DateTime date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

		/// <summary>Кампания для процедуры Grid — нужен только её номер, читать её незачем.</summary>
		internal static Campaign CampaignRef(OpenCampaign c)
		{
			Campaign campaign = new Campaign();
			campaign[Campaign.ParamNames.CampaignId] = c.CampaignId;
			return campaign;
		}

		/// <summary>Наборы процедуры Grid: выпуски кампании, счётчики дней, окна фирмы, ролики фирмы.</summary>
		private sealed class WeekIssues
		{
			private readonly Dictionary<int, List<int>> _own = new Dictionary<int, List<int>>();
			private readonly HashSet<int> _firmWindows = new HashSet<int>();
			private readonly Dictionary<int, List<int>> _firmRollers = new Dictionary<int, List<int>>();
			private readonly int[] _perDay = new int[PlacementWeek.DaysInWeek];

			internal WeekIssues(DataSet ds)
			{
				// Выпуски кампании — по окну выхода (actualWindowID: куда выпуск перенёс трафик), с повторами,
				// в порядке набора. Старая процедура Grid колонку не отдаёт — тогда исходное окно, как раньше.
				DataTable own = ds.Tables[Constants.TableNames.Data];
				string windowColumn = own.Columns.Contains(TariffWindow.ParamNames.ActualWindowId)
					? TariffWindow.ParamNames.ActualWindowId
					: TariffWindow.ParamNames.OriginalWindowId;
				foreach (DataRow row in own.Rows)
				{
					int windowId = ParseHelper.GetInt32FromObject(row[windowColumn], 0);
					if (!_own.TryGetValue(windowId, out List<int> rollers))
						_own[windowId] = rollers = new List<int>();
					rollers.Add(ParseHelper.GetInt32FromObject(row[TariffWindow.ParamNames.RollerID], 0));
				}

				// Счётчик: weekday по DATEFIRST 1, Пн = 1 … Вс = 7 — все свои выпуски, с макетами.
				foreach (DataRow row in ds.Tables[1].Rows)
				{
					int weekday = ParseHelper.GetInt32FromObject(row["weekday"], 0);
					if (weekday >= 1 && weekday <= PlacementWeek.DaysInWeek)
						_perDay[weekday - 1] += ParseHelper.GetInt32FromObject(row["count"], 0);
				}

				DataTable firm = ds.Tables[Constants.TableNames.WindowsWithThisFirmIssue];
				if (firm != null)
					foreach (DataRow row in firm.Rows)
						_firmWindows.Add(ParseHelper.GetInt32FromObject(row[TariffWindow.ParamNames.WindowId], 0));

				// Четвёртый набор — ролики других кампаний фирмы, для номеров в бирюзовых окнах.
				if (ds.Tables.Count > 3)
					foreach (DataRow row in ds.Tables[3].Rows)
					{
						int windowId = ParseHelper.GetInt32FromObject(row[TariffWindow.ParamNames.WindowId], 0);
						int rollerId = ParseHelper.GetInt32FromObject(row[TariffWindow.ParamNames.RollerID], 0);
						if (!_firmRollers.TryGetValue(windowId, out List<int> rollers))
							_firmRollers[windowId] = rollers = new List<int>();
						if (!rollers.Contains(rollerId))
							rollers.Add(rollerId);
					}
			}

			internal IReadOnlyList<int> Own(int windowId) =>
				_own.TryGetValue(windowId, out List<int> rollers) ? rollers : (IReadOnlyList<int>)Array.Empty<int>();

			internal IReadOnlyList<int> FirmRollers(int windowId) =>
				_firmRollers.TryGetValue(windowId, out List<int> rollers) ? rollers : (IReadOnlyList<int>)Array.Empty<int>();

			/// <summary>
			/// Выпуски этой фирмы в окне при отсутствии своих (десктоп: бирюзовый цвет, синий его
			/// перекрывает). Набор Grid включает и текущую кампанию — свои отсекает отметка Mine.
			/// </summary>
			internal bool FirmHere(int windowId) => _firmWindows.Contains(windowId);

			internal int CountOfDay(int dayIndex) => _perDay[dayIndex];
		}

		private static List<PlacementRow> BuildRows(PlacementWeek week, DataTable times, DataTable windows, WeekIssues marks,
			WeekView view, HashSet<int> advertWindows)
		{
			List<PlacementRow> rows = new List<PlacementRow>(times.Rows.Count);
			Dictionary<string, PlacementRow> byKey = new Dictionary<string, PlacementRow>();
			foreach (DataRow t in times.Rows)
			{
				int hour = Convert.ToInt32(t["hour"]);
				int min = Convert.ToInt32(t["min"]);
				decimal price = Convert.ToDecimal(t[TariffWindow.ParamNames.Price]);
				string key = RowKey(hour, min, price);
				if (byKey.ContainsKey(key))
					continue;
				PlacementRow row = new PlacementRow(DateTimeUtils.Time2String(hour, min), price);
				byKey.Add(key, row);
				rows.Add(row);
			}

			// Прайм — окно с самой высокой ценой в своём дне (TariffWindowGrid.ProcessPrimeWindows).
			// Заблокированные окна не участвуют: иначе «Показать заблокированные окна» сдвигала бы прайм.
			decimal[] maxPrice = new decimal[PlacementWeek.DaysInWeek];
			foreach (DataRow w in windows.Rows)
			{
				int day = DayOf(week, w);
				if (day >= 0 && !IsDisabled(w))
					maxPrice[day] = Math.Max(maxPrice[day], Convert.ToDecimal(w[TariffWindow.ParamNames.Price]));
			}

			foreach (DataRow w in windows.Rows)
			{
				int day = DayOf(week, w);
				if (day < 0)
					continue;
				string key = RowKey(Convert.ToInt32(w["hour"]), Convert.ToInt32(w["min"]), Convert.ToDecimal(w[TariffWindow.ParamNames.Price]));
				if (!byKey.TryGetValue(key, out PlacementRow row))
					continue;

				int windowId = Convert.ToInt32(w[TariffWindow.ParamNames.WindowId]);
				PlacementCell cell = new PlacementCell
				{
					Key = windowId,
					DayIndex = day,
					OwnRollerIds = marks.Own(windowId),
					FirmRollerIds = marks.FirmRollers(windowId),
				};
				bool prime = !IsDisabled(w) && Convert.ToDecimal(w[TariffWindow.ParamNames.Price]) == maxPrice[day];
				Fill(cell, w, view, advertWindows, cell.OwnRollerIds.Count > 0, marks.FirmHere(windowId), prime);
				// Два окна в одной ячейке (на 2026 год — ни одного): как в десктопе, остаётся
				// последнее, набор идёт по убыванию windowDateOriginal.
				row.Cells[day] = cell;
			}
			return rows;
		}

		/// <summary>Колонка окна — день исходной даты (windowDateOriginal); -1 — вне недели.</summary>
		private static int DayOf(PlacementWeek week, DataRow w)
		{
			DateTime original = (DateTime)w[TariffWindow.ParamNames.WindowDateOriginal];
			int day = (int)(original.Date - week.FirstDay).TotalDays;
			return day >= 0 && day < PlacementWeek.DaysInWeek ? day : -1;
		}

		private static string RowKey(int hour, int min, decimal price) => hour + ":" + min + "|" + price;

		/// <summary>Текст, признаки и окно ядра — из строки TariffWindowRetrieve.</summary>
		private static void Fill(PlacementCell cell, DataRow w, WeekView view, HashSet<int> advertWindows, bool mine, bool firmHere, bool prime)
		{
			cell.Date = (DateTime)w[TariffWindow.ParamNames.WindowDateActual];
			cell.Text = CellText(w, view.ShowUnconfirmed);
			cell.Window = new TariffWindowWithRollerIssues(w, Entities.TariffWindow);

			PlacementFlags flags = PlacementFlags.None;
			if (mine) flags |= PlacementFlags.Mine;
			if (firmHere) flags |= PlacementFlags.FirmHere;
			if (prime) flags |= PlacementFlags.Prime;
			if (IsDisabled(w)) flags |= PlacementFlags.Disabled;
			if (w[TariffWindow.ParamNames.IsMarked] is bool marked && marked) flags |= PlacementFlags.Marked;
			if (Matches(w, view, advertWindows)) flags |= PlacementFlags.Match;
			cell.Flags = flags;
		}

		/// <summary>
		/// Остаток окна — как RollerIssuesGrid3.GetCellContent: длительность минус занятое
		/// подтверждёнными, при «Учитывать макеты» — и макетами; у штучного окна ещё «[осталось/всего]».
		/// Остаток бывает отрицательным.
		/// </summary>
		internal static string CellText(DataRow w, bool showUnconfirmed)
		{
			int timeLeft = IntOrZero(w[TariffWindow.ParamNames.Duration]) - IntOrZero(w[TariffWindow.ParamNames.TimeInUseConfirmed])
				- (showUnconfirmed ? IntOrZero(w[TariffWindow.ParamNames.TimeInUseUnconfirmed]) : 0);
			string text = DateTimeUtils.Time2String(timeLeft);

			int maxCapacity = IntOrZero(w[TariffWindow.ParamNames.MaxCapacity]);
			if (maxCapacity == 0)
				return text;
			int capacityLeft = maxCapacity - IntOrZero(w[TariffWindowWithRollerIssues.ParamNames.CapacityInUseConfirmed])
				- (showUnconfirmed ? IntOrZero(w[TariffWindowWithRollerIssues.ParamNames.CapacityInUseUnconfirmed]) : 0);
			return string.Format("{0} [{1}/{2}]", text, capacityLeft, maxCapacity);
		}

		/// <summary>
		/// Подходит ли окно под «Позиционирование» и «Предметы рекламы» (RollerIssuesGrid3.MarkCell):
		/// позиция свободна среди подтверждённых, при «Учитывать макеты» — и среди макетов; И окно
		/// есть (нет) в наборе предмета рекламы. Без обоих фильтров — не подходит ничто.
		/// </summary>
		private static bool Matches(DataRow w, WeekView view, HashSet<int> advertWindows)
		{
			if (view.Position == 0 && advertWindows == null)
				return false;
			if (view.Position != 0 && !IsPositionFree(w, view.Position, view.ShowUnconfirmed))
				return false;
			if (advertWindows != null)
				return advertWindows.Contains(Convert.ToInt32(w[TariffWindow.ParamNames.WindowId])) == view.AdvertTypeExists;
			return true;
		}

		private static bool IsPositionFree(DataRow w, int position, bool showUnconfirmed)
		{
			string occupied, unconfirmed;
			switch ((RollerPositions)position)
			{
				case RollerPositions.First: occupied = "isFirstPositionOccupied"; unconfirmed = "firstPositionsUnconfirmed"; break;
				case RollerPositions.Second: occupied = "isSecondPositionOccupied"; unconfirmed = "secondPositionsUnconfirmed"; break;
				case RollerPositions.Last: occupied = "isLastPositionOccupied"; unconfirmed = "lastPositionsUnconfirmed"; break;
				default: return true;
			}
			return !ParseHelper.GetBooleanFromObject(w[occupied], false)
				&& (!showUnconfirmed || IntOrZero(w[unconfirmed]) == 0);
		}

		/// <summary>
		/// Окна, где есть выпуск (любой фирмы) с роликом этого предмета рекламы или его дочернего —
		/// TariffWindowWithAdvertTypeRetrieve за неделю прайс-листа или по одному окну.
		/// </summary>
		private static HashSet<int> AdvertTypeWindows(WeekView view, DateTime startDate, DateTime finishDate, int pricelistId, int? windowId)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[AdvertType.ParamNames.AdvertTypeId] = view.AdvertTypeId.Value;
			parameters["showUnconfirmed"] = view.ShowUnconfirmed;
			if (windowId.HasValue)
				parameters[TariffWindow.ParamNames.WindowId] = windowId.Value;
			else
			{
				parameters[Pricelist.ParamNames.StartDate] = startDate;
				parameters[Pricelist.ParamNames.FinishDate] = finishDate;
				parameters[Pricelist.ParamNames.PricelistId] = pricelistId;
			}

			HashSet<int> result = new HashSet<int>();
			foreach (DataRow row in DataAccessor.LoadDataSet("TariffWindowWithAdvertTypeRetrieve", parameters).Tables[0].Rows)
				result.Add(Convert.ToInt32(row[TariffWindow.ParamNames.WindowId]));
			return result;
		}

		private static bool IsDisabled(DataRow w) => w[TariffWindow.ParamNames.IsDisabled] is bool disabled && disabled;

		internal static int IntOrZero(object value) => value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);

		// ---------- Кисть: ролики фирмы ----------

		/// <summary>Номер ролика в списке «Ролики» — колонка «№»; по нему же номера в ячейках и чек-листах.</summary>
		public const string RollerNumberColumn = "rollerNumber";

		/// <summary>
		/// Ролики фирмы, как список «Ролики» в CampaignForm: процедура Rollers (Firm.GetRollers) —
		/// активные ролики фирмы, пустышки тоже, по имени. Номер — позиция в списке.
		/// </summary>
		public static DataTable LoadRollers(IBrushScope c)
		{
			DataTable rollers = new Firm(c.FirmId).GetRollers();
			rollers.Columns.Add(RollerNumberColumn, typeof(int));
			for (int i = 0; i < rollers.Rows.Count; i++)
				rollers.Rows[i][RollerNumberColumn] = i + 1;
			c.Rollers = rollers;
			return rollers;
		}

		/// <summary>
		/// Ролики для чек-листа «Какие ролики …?» — как CampaignForm.SelectRollers: журнальная
		/// загрузка Rollers по каждому @rollerID (так видны и неактивные ролики, стоящие в выпусках),
		/// номер — из списка «Ролики» (у кого его там нет — пусто), по порядку номеров.
		/// </summary>
		public static DataTable RollerRows(IBrushScope c, IEnumerable<int> rollerIds)
		{
			Entity rollerEntity = EntityManager.GetEntity((int)Entities.Roller);
			DataTable table = new DataTable();
			foreach (int rollerId in rollerIds)
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				DataAccessor.PrepareParameters(parameters, rollerEntity, InterfaceObjects.SimpleJournal, Constants.Actions.Load);
				parameters[Roller.ParamNames.RollerId] = rollerId;
				table.Merge(((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data]);
			}
			return WithNumbers(c, table);
		}

		/// <summary>Номер из списка «Ролики» к каждой строке роликов; строки — по возрастанию номера.</summary>
		public static DataTable WithNumbers(IBrushScope c, DataTable rollers)
		{
			Dictionary<int, int> numbers = new Dictionary<int, int>();
			if (c.Rollers != null)
				foreach (DataRow row in c.Rollers.Rows)
					numbers[RollerIdOf(row)] = Convert.ToInt32(row[RollerNumberColumn]);

			if (!rollers.Columns.Contains(RollerNumberColumn))
				rollers.Columns.Add(RollerNumberColumn, typeof(int));
			foreach (DataRow row in rollers.Rows)
				row[RollerNumberColumn] = numbers.TryGetValue(RollerIdOf(row), out int number) ? (object)number : DBNull.Value;

			DataView view = rollers.DefaultView;
			view.Sort = RollerNumberColumn;
			return view.ToTable();
		}

		/// <summary>Сущность строк списка — «Ролик рекламной акции» (1244): колонки и меню как в десктопе.</summary>
		public static Entity RollerListEntity() => EntityManager.GetEntity((int)Entities.ActionRollers);

		public static int RollerIdOf(DataRow roller) => Convert.ToInt32(roller[Roller.ParamNames.RollerId]);

		/// <summary>Пустышка — ролик без файла (isMute): слушать нечего.</summary>
		public static bool IsDummy(DataRow roller) => ParseHelper.GetBooleanFromObject(roller[Roller.ParamNames.IsMute], false);

		public static string RollerName(DataRow roller) => Convert.ToString(roller[Constants.Parameters.Name]);

		public static string RollerDuration(DataRow roller) => Convert.ToString(roller[Roller.ParamNames.DurationString]);

		/// <summary>
		/// «Добавить ролик - пустышку»: пустышка фирмы этой длины (GetMuteRoller находит готовую или
		/// заводит новую, без предмета рекламы — как десктоп). Возвращает её номер ролика.
		/// </summary>
		public static int CreateDummyRoller(OpenCampaign c, int seconds)
		{
			Campaign campaign = LoadForWrite(c);
			return MuteRoller.GetRoller(seconds, campaign.Action.FirmID, null).RollerId;
		}

		/// <summary>
		/// Ролик кисти — только из показанного списка роликов и только фирмы акции на сейчас
		/// (IssueIUD фирму ролика не проверяет; фирму акции могли сменить, пока открыта вкладка).
		/// </summary>
		internal static Roller BrushRoller(IBrushScope c, int rollerId, int actionFirmId)
		{
			if (c.Rollers != null)
				foreach (DataRow row in c.Rollers.Rows)
					if (RollerIdOf(row) == rollerId
						&& ParseHelper.GetInt32FromObject(row[Firm.ParamNames.FirmId], 0) == actionFirmId)
						return new Roller(row);
			throw new InvalidOperationException(Tr.T("Ролика нет в списке роликов фирмы. Обновите страницу."));
		}

		// ---------- Постановка ----------

		/// <summary>
		/// Клик «Режима добавления» — как RollerIssuesGrid3.AddIssueTransaction: IssueIUD и пересчёт
		/// акции одной транзакцией. Затем окно перечитывается (TariffWindowRetrieve @windowId), ячейка
		/// и счётчик дня обновляются на месте — неделя целиком не перечитывается. <paramref name="onPlaced"/> получает
		/// поставленный выпуск сразу после коммита, до перечитывания окна: сбой чтения не должен оставить выпуск без записи
		/// для «Отменить последнее».
		/// </summary>
		public static void PlaceOne(OpenCampaign c, PlacementWeek week, PlacementCell cell, int rollerId, int position, WeekView view,
			System.Action<PlacedIssue> onPlaced)
		{
			Campaign campaign = LoadForWrite(c);
			Roller roller = BrushRoller(c, rollerId, campaign.Action.FirmID);
			PlacedIssue placed = null;
			RunInTransaction(() =>
			{
				placed = Capture(c, campaign.AddIssue(roller, cell.Window, (RollerPositions)position, null), cell, roller);
				campaign.RecalculateAction(false);
			});
			onPlaced(placed);

			DataRow w = LoadWindowRow(cell.Key);
			List<int> own = new List<int>(cell.OwnRollerIds) { rollerId };
			cell.OwnRollerIds = own;
			HashSet<int> advertWindows = view.AdvertTypeId.HasValue
				? AdvertTypeWindows(view, DateTime.MinValue, DateTime.MinValue, 0, cell.Key)
				: null;
			if (w != null)
				Fill(cell, w, view, advertWindows, mine: true, firmHere: cell.Has(PlacementFlags.FirmHere), prime: cell.Has(PlacementFlags.Prime));
			else
				cell.Flags |= PlacementFlags.Mine;
			if (week.DayTotals[cell.DayIndex].HasValue)
				week.DayTotals[cell.DayIndex]++;
		}

		/// <summary>Одно окно — TariffWindowRetrieve @windowId с флагами линейной сетки; null — окна больше нет.</summary>
		private static DataRow LoadWindowRow(int windowId)
		{
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.TariffWindow));
			parameters[TariffWindow.ParamNames.WindowId] = windowId;
			parameters[MassmediaPricelist.ParamNames.ExcludeSpecialWindows] = true;
			parameters[MassmediaPricelist.ParamNames.ExcludeModuleTariffs] = true;
			parameters["showDisabledWindows"] = true;
			parameters["useActualTime"] = true;
			DataTable data = ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
			return data.Rows.Count > 0 ? data.Rows[0] : null;
		}

		/// <summary>Итог действия над несколькими местами: сколько сделано и отказы «место — причина».</summary>
		public sealed class BatchResult
		{
			internal BatchResult()
			{
				Errors = new DataTable();
				Errors.Columns.Add("name", typeof(string));
				Errors.Columns.Add("description", typeof(string));
			}

			public int Done { get; internal set; }

			/// <summary>Поставленные выпуски (только у «Разместить ролик») — запись для «Отменить последнее».</summary>
			public List<PlacedIssue> Created { get; } = new List<PlacedIssue>();

			/// <summary>Выпуски записи, до которых отмена не дошла из-за сбоя (только у «Отменить последнее»): их можно отменить снова.</summary>
			public List<PlacedIssue> Unprocessed { get; } = new List<PlacedIssue>();

			/// <summary>
			/// Сколько не сделано: отказы плюс не обработанные из-за сбоя; строка «отмена прервана» — не отказ, а пояснение
			/// к <see cref="Unprocessed"/>.
			/// </summary>
			public int NotDone => Errors.Rows.Count - (Unprocessed.Count > 0 ? 1 : 0) + Unprocessed.Count;

			/// <summary>name — место или выпуск, description — причина отказа.</summary>
			public DataTable Errors { get; }

			public int Failed => Errors.Rows.Count;

			internal void Refuse(string name, string reason) => Errors.Rows.Add(name, reason);
		}

		/// <summary>
		/// «Разместить ролик» в выделенные окна (десктоп — Insert): ролик кисти с позицией кисти в
		/// каждое окно, одна транзакция и один пересчёт (десктоп пересчитывал на каждое окно, П-1).
		/// </summary>
		public static BatchResult Place(OpenCampaign c, IEnumerable<PlacementCell> cells, int rollerId, int position)
		{
			Campaign campaign = LoadForWrite(c);
			Roller roller = BrushRoller(c, rollerId, campaign.Action.FirmID);
			BatchResult result = new BatchResult();
			RunInTransaction(() =>
			{
				// По возрастанию номера окна — один порядок блокировок у всех пачек.
				foreach (PlacementCell cell in cells.OrderBy(x => x.Key))
					Step(result, cell.Date.ToString("dd.MM.yyyy HH:mm"), () =>
					{
						result.Created.Add(Capture(c, campaign.AddIssue(roller, cell.Window, (RollerPositions)position, null), cell, roller));
						return null;
					});
				if (result.Done > 0)
					campaign.RecalculateAction(false);
			});
			return result;
		}

		// ---------- «Отменить последнее» ----------

		/// <summary>
		/// Выпуск, который поставила вкладка: номер из ответа IssueIUD и то, каким он был поставлен (кампания, окно, ролик).
		/// По записи отмена находит именно этот выпуск, а не «какой-то такой же», и видит, что его уже изменили.
		/// </summary>
		public sealed class PlacedIssue
		{
			internal PlacedIssue() { }

			public int IssueId { get; internal set; }
			public int CampaignId { get; internal set; }
			public int WindowId { get; internal set; }
			public int RollerId { get; internal set; }

			/// <summary>Для строки отказа: «дата время — ролик».</summary>
			public string Name { get; internal set; }
		}

		private static PlacedIssue Capture(OpenCampaign c, Issue issue, PlacementCell cell, Roller roller) =>
			Capture(c, issue, cell.Key, cell.Date, roller);

		internal static PlacedIssue Capture(OpenCampaign c, Issue issue, int windowId, DateTime date, Roller roller) => new PlacedIssue
		{
			IssueId = Convert.ToInt32(issue[Issue.ParamNames.IssueId]),
			CampaignId = c.CampaignId,
			WindowId = windowId,
			RollerId = Convert.ToInt32(roller[Roller.ParamNames.RollerId]),
			Name = string.Format("{0:dd.MM.yyyy HH:mm} — {1}", date, roller.Name),
		};

		/// <summary>
		/// Сколько выпусков снимается в одной транзакции: удаление держит блокировку окна до коммита, а на проде
		/// <c>IssueIUD DeleteItem</c> — медиана 0,2 с, до 2 с на выпуск; запись шаблона — сотни выпусков.
		/// </summary>
		private const int UndoBatchSize = 10;

		/// <summary>
		/// «Отменить последнее» (десктоп — «Отменить» у шаблонов): удаляет выпуски, которые поставило последнее
		/// размещение, пачками по <see cref="UndoBatchSize"/> (каждая в своей транзакции, по возрастанию номера окна)
		/// и одним пересчётом в конце. Десктоп удалял объекты из списка формы; веб читает каждый выпуск заново: нет в
		/// базе — уже удалён, другой ролик, окно или кампания — его заменили или перенесли после размещения, такой не
		/// трогается. И то и другое — строкой отказа, остальные снимаются. Права и правила прошлого периода — те же,
		/// что у «Удалить выпуски» (IssueIUD DeleteItem). Сверка и удаление — два обращения в READ COMMITTED: если между
		/// ними другой сеанс заменит ролик или перенесёт выпуск, он будет удалён (миллисекунды; тот же приём у переноса,
		/// а десктоп не сверяет ничего).
		///
		/// Небизнес-ошибка (взаимоблокировка, таймаут) откатывает текущую пачку и прерывает отмену: снятые ранее пачки
		/// остаются, остальные — в <see cref="BatchResult.Unprocessed"/>, чтобы отменить их снова.
		/// </summary>
		public static BatchResult Undo(OpenCampaign c, IReadOnlyList<PlacedIssue> placed)
		{
			if (placed.Any(p => p.CampaignId != c.CampaignId))
				throw new InvalidOperationException(Tr.T("Отменять можно только выпуски этой кампании."));
			Campaign campaign = LoadForWrite(c);
			return UndoCore(placed, "campaign " + c.CampaignId, () => campaign.RecalculateAction(false));
		}

		/// <summary>
		/// Отмена по записи — общая у линейки и веера: пачки, сверка выпуска с записью, отказы, один пересчёт.
		/// <paramref name="recalculate"/> зовётся в своей транзакции, если хоть что-то снято; <paramref name="context"/> — для журнала.
		/// </summary>
		internal static BatchResult UndoCore(IReadOnlyList<PlacedIssue> placed, string context, System.Action recalculate)
		{
			BatchResult result = new BatchResult();
			List<PlacedIssue> ordered = placed.OrderBy(x => x.WindowId).ThenBy(x => x.IssueId).ToList();
			for (int from = 0; from < ordered.Count; from += UndoBatchSize)
			{
				List<PlacedIssue> batch = ordered.GetRange(from, Math.Min(UndoBatchSize, ordered.Count - from));
				BatchResult part = new BatchResult();
				try
				{
					RunInTransaction(() =>
					{
						foreach (PlacedIssue p in batch)
						{
							DataRow row = Issue.LoadRow(p.IssueId);
							if (row == null)
								part.Refuse(p.Name, Tr.T("Выпуска уже нет."));
							else if (Convert.ToInt32(row[Roller.ParamNames.RollerId]) != p.RollerId
								|| Convert.ToInt32(row[Issue.ActualWindowIdParam]) != p.WindowId
								|| Convert.ToInt32(row[Campaign.ParamNames.CampaignId]) != p.CampaignId)
								part.Refuse(p.Name, Tr.T("Выпуск изменили после размещения: другой ролик или окно."));
							else
								Step(part, p.Name, () =>
								{
									new RollerIssue(row).Delete(silenceFlag: true);
									return null;
								});
						}
					});
				}
				catch (Exception ex)
				{
					Log.Error($"Undo interrupted at {from}/{ordered.Count} ({context})", ex);
					List<PlacedIssue> left = ordered.GetRange(from, ordered.Count - from);
					result.Unprocessed.AddRange(left);
					result.Refuse(left[0].Name, Tr.Format("Отмена прервана: {0} Не снято выпусков: {1}.", ErrorManager.GetErrorMessage(ex), left.Count));
					break;
				}
				result.Done += part.Done;
				foreach (DataRow refused in part.Errors.Rows)
					result.Errors.Rows.Add(refused["name"], refused["description"]);
			}

			if (result.Done > 0)
			{
				try
				{
					RunInTransaction(recalculate);
				}
				catch (Exception ex)
				{
					Log.Error($"Undo: recalculation failed ({context})", ex);
					result.Refuse("", Tr.Format("Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.", ErrorManager.GetErrorMessage(ex)));
				}
			}
			return result;
		}

		// ---------- Выпуски кампании в окнах ----------

		/// <summary>
		/// Выпуски текущей кампании в окнах — как CampaignForm.LoadCurrentCampaignIssueRows:
		/// WindowIssuesRetrieve по каждому окну (по фактическому окну, с макетами), отбор по кампании.
		/// </summary>
		public sealed class CampaignIssues
		{
			internal CampaignIssues(List<DataRow> rows)
			{
				Rows = rows;
			}

			internal List<DataRow> Rows { get; }

			public int Count => Rows.Count;

			/// <summary>Ролики выпусков без повторов, в порядке выпусков.</summary>
			public IReadOnlyList<int> RollerIds => Rows.Select(r => RollerIdOf(r)).Distinct().ToList();

			public CampaignIssues Only(IEnumerable<int> rollerIds)
			{
				HashSet<int> keep = new HashSet<int>(rollerIds);
				return new CampaignIssues(Rows.Where(r => keep.Contains(RollerIdOf(r))).ToList());
			}

			public CampaignIssues Without(int rollerId) => new CampaignIssues(Rows.Where(r => RollerIdOf(r) != rollerId).ToList());
		}

		public static CampaignIssues IssuesInWindows(OpenCampaign c, IEnumerable<PlacementCell> cells)
		{
			List<DataRow> rows = new List<DataRow>();
			foreach (PlacementCell cell in cells)
				foreach (DataRow row in TariffWindowWithRollerIssues.LoadIssues(true, cell.Key).Rows)
					if (ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignId], 0) == c.CampaignId)
						rows.Add(row);
			return new CampaignIssues(rows);
		}

		private static string IssueName(DataRow row) =>
			string.Format("{0:dd.MM.yyyy HH:mm} — {1}", row[RollerIssue.ParamNames.IssueDate], row[Constants.Parameters.Name]);

		/// <summary>
		/// «Удалить выпуски» в выделенных окнах (десктоп — Del): IssueIUD DeleteItem на выпуск,
		/// одна транзакция и один пересчёт.
		/// </summary>
		public static BatchResult Remove(OpenCampaign c, CampaignIssues issues)
		{
			Campaign campaign = LoadForWrite(c);
			BatchResult result = new BatchResult();
			RunInTransaction(() =>
			{
				foreach (DataRow row in issues.Rows)
				{
					RollerIssue issue = new RollerIssue(row);
					Step(result, IssueName(row), () =>
					{
						issue.Delete(silenceFlag: true);
						return null;
					});
				}
				if (result.Done > 0)
					campaign.RecalculateAction(false);
			});
			return result;
		}

		/// <summary>
		/// «Заменить ролики» в выделенных окнах (десктоп — Ctrl+R): RollerSubstitute на каждый выпуск
		/// (@issueID + @originalWindowID, как CampaignForm.ReplaceRollerInSelectedWindows). Пересчёт —
		/// один и только если длина роликов разная: ролик той же длины цену не меняет. Незаменённые
		/// по правилам процедуры (прошлое, закрытый день…) — в отказах с её текстом.
		/// </summary>
		public static BatchResult Replace(OpenCampaign c, CampaignIssues issues, int newRollerId)
		{
			Campaign campaign = LoadForWrite(c);
			Roller newRoller = BrushRoller(c, newRollerId, campaign.Action.FirmID);
			Dictionary<int, Roller> oldRollers = issues.RollerIds.ToDictionary(id => id, id => new Roller(id));
			bool durationChanged = false;
			BatchResult result = new BatchResult();
			RunInTransaction(() =>
			{
				foreach (DataRow row in issues.Rows)
				{
					Roller oldRoller = oldRollers[RollerIdOf(row)];
					Step(result, IssueName(row), () =>
					{
						DataTable unsubstituted = CampaignRoller.ApplyRollerSubstitutionForIssue(campaign, oldRoller, newRoller,
							Convert.ToInt32(row[Issue.ParamNames.IssueId]), Convert.ToInt32(row[TariffWindow.ParamNames.OriginalWindowId]));
						if (unsubstituted != null && unsubstituted.Rows.Count > 0)
							return Convert.ToString(unsubstituted.Rows[0]["message"]);
						durationChanged |= oldRoller.Duration != newRoller.Duration;
						return null;
					});
				}
				if (durationChanged)
					campaign.RecalculateAction(false);
			});
			return result;
		}

		// ---------- Перенос ----------

		/// <summary>Перенос выпусков: окно-приёмник, выпуски и предупреждения для вопроса.</summary>
		public sealed class MovePlan
		{
			internal MovePlan(PlacementCell target, List<RollerIssue> issues, List<string> warnings)
			{
				Target = target;
				Issues = issues;
				Warnings = warnings;
			}

			public PlacementCell Target { get; }
			public int Count => Issues.Count;
			internal List<RollerIssue> Issues { get; }

			/// <summary>В окне уже есть тот же ролик или ролики фирмы — перенести можно, но стоит подтвердить.</summary>
			public IReadOnlyList<string> Warnings { get; }
		}

		/// <summary>
		/// План переноса отмеченных в панели выпусков в окно <paramref name="target"/>. Предупреждения
		/// — как у трафика (TrafficManagement.PlanTransfer): одно чтение окна-приёмника, только
		/// подтверждённые выпуски. В десктопном перетаскивании их нет.
		/// </summary>
		public static MovePlan PlanMove(OpenCampaign c, IEnumerable<PresentationObject> issues, PlacementCell target)
		{
			List<RollerIssue> list = new List<RollerIssue>();
			foreach (PresentationObject issue in issues)
			{
				if (!(issue is RollerIssue rollerIssue) || ParseHelper.GetInt32FromObject(issue[Campaign.ParamNames.CampaignId], 0) != c.CampaignId)
					throw new InvalidOperationException(Tr.T("Переносить можно только выпуски этой кампании."));
				list.Add(rollerIssue);
			}

			HashSet<int> rollers = new HashSet<int>();
			HashSet<int> firms = new HashSet<int>();
			foreach (DataRow r in TariffWindowWithRollerIssues.LoadIssues(false, target.Key).Rows)
			{
				rollers.Add(RollerIdOf(r));
				firms.Add(ParseHelper.GetInt32FromObject(r[Firm.ParamNames.FirmId], 0));
			}

			List<string> warnings = new List<string>();
			foreach (RollerIssue issue in list)
			{
				string warning = rollers.Contains(Convert.ToInt32(issue[Roller.ParamNames.RollerId]))
					? Tr.Format("В окне уже есть ролик «{0}».", issue[Constants.Parameters.Name])
					: firms.Contains(c.FirmId)
						? Tr.Format("В окне уже есть ролики фирмы «{0}».", issue["firmName"])
						: null;
				if (warning != null && !warnings.Contains(warning))
					warnings.Add(warning);
			}
			return new MovePlan(target, list, warnings);
		}

		/// <summary>
		/// Перенос — как десктопное перетаскивание (CampaignForm.MoveIssuesToWindow, решение
		/// 06.10.2026): каждый выпуск удаляется и ставится в окно-приёмник тем же роликом с той же
		/// позицией, затем один пересчёт — всё одной транзакцией, отказ любого выпуска отменяет всё.
		/// Новый выпуск получает цену окна-приёмника; в «Переносы» (TransferLog) это не пишется.
		///
		/// В отличие от перетаскивания, между выбором выпусков и окна-приёмника проходит время: выпуск
		/// могли удалить, заменить ролик или позицию. Поэтому выпуски перечитываются в транзакции, и
		/// изменённый отменяет перенос — иначе удалённый выпуск воскрес бы, а замена откатилась.
		///
		/// Перенесённый выпуск — новая строка с новым номером. Возвращаются пары «прежний номер → выпуск в окне-приёмнике»:
		/// по ним запись «Отменить последнее» переходит на новые выпуски, а не теряет их.
		/// </summary>
		public static IReadOnlyList<KeyValuePair<int, PlacedIssue>> Move(OpenCampaign c, MovePlan plan)
		{
			Campaign campaign = LoadForWrite(c);
			List<KeyValuePair<int, PlacedIssue>> moved = new List<KeyValuePair<int, PlacedIssue>>();
			RunInTransaction(() =>
			{
				moved.Clear();
				foreach (RollerIssue issue in plan.Issues)
				{
					if (!IsUnchanged(issue))
						throw new InvalidOperationException(Tr.T("Выпуск изменили или удалили, пока выбиралось окно. Выберите выпуски заново."));
					int oldId = Convert.ToInt32(issue[Issue.ParamNames.IssueId]);
					issue.Delete(silenceFlag: true);
					moved.Add(new KeyValuePair<int, PlacedIssue>(oldId,
						Capture(c, campaign.AddIssue(issue.Roller, plan.Target.Window, issue.Position, null), plan.Target, issue.Roller)));
				}
				campaign.RecalculateAction(false);
			});
			return moved;
		}

		/// <summary>Выпуск в базе такой же, как в строке панели: тот же ролик, позиция, окно и кампания.</summary>
		private static bool IsUnchanged(RollerIssue issue)
		{
			DataRow fresh = Issue.LoadRow(Convert.ToInt32(issue[Issue.ParamNames.IssueId]));
			return fresh != null
				&& Equals(fresh[Roller.ParamNames.RollerId], issue[Roller.ParamNames.RollerId])
				&& Equals(Convert.ToInt32(fresh[Issue.ParamNames.PositionId]), Convert.ToInt32(issue[Issue.ParamNames.PositionId]))
				&& Equals(fresh[Issue.ActualWindowIdParam], issue[Issue.ActualWindowIdParam])
				&& Equals(fresh[Campaign.ParamNames.CampaignId], issue[Campaign.ParamNames.CampaignId]);
		}

		// ---------- Выпуски окна ----------

		/// <summary>Выпуски окна для панели: «Выходы в эфир этой кампании» и «Все выходы в эфир».</summary>
		public sealed class WindowIssues
		{
			internal WindowIssues(DataTable campaign, DataTable all)
			{
				Campaign = campaign;
				All = all;
			}

			public DataTable Campaign { get; }
			public DataTable All { get; }
		}

		/// <summary>
		/// WindowIssuesRetrieve по фактическому окну — один вызов с макетами: выпуски этой кампании
		/// показываются всегда (как в десктопе), остальные — по «Учитывать макеты».
		/// </summary>
		public static WindowIssues LoadWindowIssues(OpenCampaign c, PlacementCell cell, bool showUnconfirmed) =>
			LoadWindowIssues(cell.Key, c.CampaignId, showUnconfirmed);

		/// <summary>То же по номеру окна и кампании — общее с вкладкой «Модули» (окна модуля в панели).</summary>
		internal static WindowIssues LoadWindowIssues(int windowId, int campaignId, bool showUnconfirmed)
		{
			DataTable all = TariffWindowWithRollerIssues.LoadIssues(true, windowId);
			DataTable campaign = all.Clone();
			DataTable shown = all.Clone();
			foreach (DataRow row in all.Rows)
			{
				if (ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignId], 0) == campaignId)
					campaign.ImportRow(row);
				if (showUnconfirmed || ParseHelper.GetBooleanFromObject(row[Action.ParamNames.IsConfirmed], false))
					shown.ImportRow(row);
			}
			return new WindowIssues(campaign, shown);
		}

		/// <summary>
		/// Сущность «Выпуск ролика» (98) с колонками панели: <paramref name="full"/> — «Все выходы в
		/// эфир», иначе «Выходы в эфир этой кампании».
		/// </summary>
		public static Entity IssueEntity(bool full)
		{
			Entity entity = (Entity)EntityManager.GetEntity((int)Entities.Issue).Clone();
			entity.AttributeSelector = full ? Issue.AttributeSelectorFull : Issue.AttributeSelectorShort;
			return entity;
		}

		// ---------- Транзакция ----------

		/// <summary>
		/// Одна транзакция на действие. Транзакция ядра живёт в AsyncLocal: начало, записи и конец —
		/// в одном синхронном вызове (образец — TrafficManagement.Apply).
		/// </summary>
		internal static void RunInTransaction(System.Action work)
		{
			DataAccessor.BeginTransaction();
			try
			{
				work();
				DataAccessor.CommitTransaction();
			}
			catch
			{
				try
				{
					DataAccessor.RollbackTransaction();
				}
				catch (Exception)
				{
					// Сервер мог откатить транзакцию сам (жертва взаимоблокировки) — Rollback тогда бросает,
					// и его ошибка заслонила бы настоящую причину; транзакция в любом случае закрыта.
				}
				throw;
			}
		}

		/// <summary>
		/// Одно место пачки. Отказ по бизнес-правилу (RAISERROR с ключом iMessage, у IssueIUD и
		/// RollerSubstitute — до записи) или причина, которую вернула запись, — в отказы, пачка идёт
		/// дальше. Любая другая ошибка рвёт пачку: транзакцию откатит вызывающий.
		/// </summary>
		internal static void Step(BatchResult result, string name, Func<string> write)
		{
			try
			{
				string refusal = write();
				if (refusal == null)
					result.Done++;
				else
					result.Refuse(name, refusal);
			}
			catch (SqlException ex) when (ex.Number == 50000 && ex.Class == 16)
			{
				result.Refuse(name, ErrorManager.GetErrorMessage(ex));
			}
		}
	}
}
