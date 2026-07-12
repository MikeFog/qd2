using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;
using Merlin.Classes;
using Merlin.Classes.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Controls
{
	internal partial class TariffWithRangeGrid : TariffGrid, IRollerGrid
	{
        private static readonly ILog Log = LogManager.GetLogger(typeof(TariffWithRangeGrid));
        private static int _tempIssueId = 0;

        private const string MinBroadcastColumnName = "minBroadcast";
		private const string MaxBroadcastColumnName = "maxBroadcast";
        private readonly ActionOnMassmedia _action;
        private readonly int _massmediasCount;
		private Dictionary<string, string> _timeResolver;
        private bool showRollerNumbers;
        private Dictionary<int, int> rollerNumbers;

        public ActionOnMassmedia Action
        {
			get => _action;
        }

        public TariffWithRangeGrid(ActionOnMassmedia action, int massmediasCount)
		{
			InitializeComponent();
			InitializeDelegates();
			FixedCols = 1;
			DateTime date = DateTime.Today.Date;
			monday = startDate = date.AddDays(date.DayOfWeek - DayOfWeek.Monday);
			finishDate = startDate.AddDays(7);
            _action = action;
            _massmediasCount = massmediasCount;
            InitAddedIssuesData();
		}

	    private void InitAddedIssuesData()
	    {
			AddedIssues = _action.BuildAddedIssuesTable();
        }

	    private DataTable Data { get; set; }
        public DataTable AddedIssues { get; set; }

        // Режим ячейки: вместо остатка свободного времени — номера роликов текущей акции
        // (см. RollerNumbers), размещённых в этом слоте. Карта номеров фиксируется снаружи
        // (CampaignForm) на момент включения режима, сама повторно не пересчитывается.
        public bool ShowRollerNumbers
        {
            get { return showRollerNumbers; }
            set { showRollerNumbers = value; }
        }

        // rollerID -> номер ролика (как в колонке "№" grdRollers на момент включения ShowRollerNumbers)
        public Dictionary<int, int> RollerNumbers
        {
            get { return rollerNumbers; }
            set { rollerNumbers = value; }
        }

		private DateTime? MinBroadCast { get; set; }
		private DateTime? MaxBroadCast { get; set; }

		private void InitializeDelegates()
		{
			loadPricelist = delegate
			{
				pricelist = new MassmediaPricelist();
				pricelist[Pricelist.ParamNames.StartDate] = DateTime.MinValue;
				pricelist[Pricelist.ParamNames.FinishDate] = DateTime.MaxValue;
			};

			populateGrid = delegate
			{
				Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
				dictionary.Add("dateStart", StartDate);
				dictionary.Add("actionID", _action.ActionId);
				DataSet dataSet = DataAccessor.LoadDataSet("TariffWindowWithRange", dictionary);
				Data = dataSet.Tables[0];

				object oMaxBroadCast = dataSet.Tables[1].Rows[0][MaxBroadcastColumnName];
				MaxBroadCast = StringUtil.IsDBNullOrNull(oMaxBroadCast)
				               	? null : (DateTime?)ParseHelper.GetDateTimeFromObject(oMaxBroadCast, DateTime.Now);
				object oMinBroadCast = dataSet.Tables[1].Rows[0][MinBroadcastColumnName];
				MinBroadCast = StringUtil.IsDBNullOrNull(oMinBroadCast)
								? null : (DateTime?)ParseHelper.GetDateTimeFromObject(oMinBroadCast, DateTime.Now);

				if (MaxBroadCast.HasValue && MinBroadCast.HasValue)
					_tariffWindows = new ITariffWindow[(int)(MaxBroadCast.Value.AddDays(1) - MinBroadCast.Value).TotalHours * 2, 7];
				else
					_tariffWindows = null;

                PopulateGridTable(dataSet.Tables[2]);
            };

			updateDB = delegate (DataGridViewCell cell)
			{
			   AddIssuesRange(cell);
			};

			onGridPopulated = delegate
			{
                MarkCells();
                RefreshWindowsColors();
                if (MinBroadCast.HasValue)
                {
                    foreach (DataRow row in AddedIssues.Rows)
                    {
                        DateTime time = ParseHelper.GetDateTimeFromObject(row["issueDate"], DateTime.MinValue);
                        if (time != DateTime.MinValue)
                        {
                            int index = (time.Date - StartDate).Days;
                            if (time.Hour < MinBroadCast.Value.Hour ||
                                (time.Hour < MinBroadCast.Value.Hour && time.Minute < MinBroadCast.Value.Minute))
                                index--;
                            if (index >= 0 && index < 7)
                                ChangeIssuesCounter(index + FixedCols, _massmediasCount);
                        }
                    }
                }

                if (showRollerNumbers)
                    RefreshCellTexts();
			};
		}

		public DataRow AddIssuesRange(DateTime windowDate)
		{
			return AddIssuesRange(windowDate, false);
		}

		public DataRow AddIssuesRange(DateTime windowDate, bool ignoreWindowsWithTheSameFirmIssue,
			bool recalculate = true)
		{
			return AddIssuesRange(windowDate, Roller, ignoreWindowsWithTheSameFirmIssue, recalculate);
		}

		// Ролик передаётся явно — для генерации по Шаблону 3, где на разные слоты может
		// попасть разный ролик (в отличие от свойства Roller — единого выбора на форме).
		public DataRow AddIssuesRange(DateTime windowDate, Roller roller, bool ignoreWindowsWithTheSameFirmIssue,
			bool recalculate = true)
		{
            using (OperationScope.Start($"AddIssuesRange date={windowDate:yyyy-MM-dd HH:mm} recalc={recalculate}"))
            {
                Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
                parameters[ActionOnMassmedia.ParamNames.ActionId] = _action.ActionId;
                parameters["issueDate"] = windowDate;
                parameters["rollerID"] = roller.RollerId;
                parameters["rollerDuration"] = roller.Duration;
                parameters["positionId"] = (int)RollerPosition;
                parameters["considerUnconfirmed"] = ShowUnconfirmed ? 1 : 0;
                parameters["ignoreWindowsWithTheSameFirmIssue"] = ignoreWindowsWithTheSameFirmIssue ? 1 : 0;
                if (Grantor != null)
                    parameters["grantorID"] = Grantor.Id;

                DataAccessor.ExecuteNonQuery("AddRangeIssues", parameters);
                if (recalculate)
                    _action.Recalculate();

                DataRow row = AddedIssues.NewRow();
                row[Issue.ParamNames.IssueId] = System.Threading.Interlocked.Decrement(ref _tempIssueId);
                Debug.WriteLine(row[Issue.ParamNames.IssueId]);
                row["issueDate"] = windowDate;
                row[Entity.ParamNames.NAME] = roller.Name;
                row[Roller.ParamNames.RollerId] = roller.RollerId;
                row["durationString"] = roller.DurationString;
                row["RowNum"] = Guid.NewGuid();
                row[Issue.ParamNames.PositionName] = Issue.GetPositionDisplayName(RollerPosition);
                row[Issue.ParamNames.PositionId] = (int)RollerPosition;
                row[ActionOnMassmedia.ParamNames.ActionId] = _action.ActionId;

                // replace AddedIssues.Rows.Add(row); with sorted insert
                InsertIssueRowSorted(row);

                return row;
            }
		}

		public List<PresentationObject> DeleteIssuesRange(DateTime windowDate)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Merlin.Classes.Action.ParamNames.ActionId] = _action.ActionId;
			parameters["issueDate"] = windowDate;
			parameters["rollerID"] = Roller.RollerId;
			parameters["positionId"] = (int)RollerPosition;
			if (Grantor != null)
				parameters["grantorID"] = Grantor.Id;
			DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters);
			_action.Recalculate();

			DataRow rowToDelete = null;
			foreach (DataRow row in AddedIssues.Rows)
			{
				DateTime issueDate = ParseHelper.GetDateTimeFromObject(row["issueDate"], DateTime.MinValue);
				int rollerId = ParseHelper.GetInt32FromObject(row["rollerID"], 0);
				int positionId = ParseHelper.GetInt32FromObject(row["positionID"], 0);
				if (issueDate == windowDate && rollerId == Roller.RollerId && positionId == (int)RollerPosition)
				{
					rowToDelete = row;
					break;
				}
			}

			if (rowToDelete == null)
				return new List<PresentationObject>();

			PresentationObject issue = new RollerIssue(rowToDelete);
			AddedIssues.Rows.Remove(rowToDelete);
			return new List<PresentationObject> { issue };
		}

        private void AddIssuesRange(DataGridViewCell cell)
	    {
			try
			{
				AddIssuesRange(GetTariffWindow(cell).WindowDate);
				MarkCellAsHavingCurrentCampaignIssues(cell.RowIndex, cell.ColumnIndex);
				ChangeIssuesCounter(cell.ColumnIndex, _massmediasCount);
				RefreshGrid();
			}
			catch (Exception e)
			{
				ErrorManager.PublishError(e);
			}
        }

	    private void MarkCells()
	    {
            for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.RowCount; rowIndex++)
                for (int columnIndex = FixedCols; columnIndex < RawDataGridView.ColumnCount; columnIndex++)
                {
                    TariffWindowWithRange window = GetTariffWindow(rowIndex, columnIndex) as TariffWindowWithRange;
                    if (window == null)
                        continue;

                    if (RollerPosition != RollerPositions.Undefined)
                        MarkCellWithRollerPosition(window, rowIndex, columnIndex);

					if (AddedIssues.Select(string.Format("[issueDate] = '{0}'", window.WindowDate)).Length > 0)
					{
						MarkCellAsHavingCurrentCampaignIssues(rowIndex, columnIndex);
                        continue;
                    }

                    var cell = GetCell(rowIndex, columnIndex);
                    if (window.HasCurrentActionIssues)
                    {
                        MarkCellAsHavingCurrentActionIssues(cell);
                        continue;
                    }

                    bool hasAllMassmediaIssues = HasAllMassmediaIssuesFlags(window);
					bool hasAnyIssues = hasAllMassmediaIssues || HasFirmIssuesFlags(window);

                    if (!hasAnyIssues)
                        continue;

                    
                    if (hasAllMassmediaIssues)
                        MarkCellAsHavingCurrentFirmIssues(cell);
                    else if (hasAnyIssues)
                        MarkCellAsHavingCurrentFirmIssuesAnyMassmedia(cell);
                }
	    }

	    private bool HasFirmIssuesFlags(TariffWindowWithRange window)
	    {
	        if (window == null) return false;

	        return window.HasIssues || window.HasCurrentActionIssues
	               || (ShowUnconfirmed && window.HasIssuesUnconfirmed);
	    }

	    private bool HasAllMassmediaIssuesFlags(TariffWindowWithRange window)
	    {
	        if (window == null) return false;

	        return window.HasIssuesAllMassmedia
	               || (ShowUnconfirmed && window.HasIssuesUnconfirmedAllMassmedia);
	    }

        /// <summary>
        /// Проверяет, доступна ли текущая позиция ролика (RollerPosition) в окне.
        /// Если позиция не определена (Undefined) — всегда доступно.
        /// </summary>
        private bool IsPositionAvailable(TariffWindowWithRange window)
        {
            if (_rollerPosition == RollerPositions.Undefined)
                return true;

            if (!ShowUnconfirmed)
                return IsConfirmedPositionNotOccupied(window);

            return IsConfirmedPositionNotOccupied(window)
                && IsUnconfirmedPositionsNotOccupied(window);
        }

        private void MarkCellWithRollerPosition(TariffWindowWithRange window, int rowIndex, int columnIndex)
        {
            if (window == null) return;
            if (!ShowUnconfirmed)
            {
                if (IsConfirmedPositionNotOccupied(window))
                    MarkCellAsNotOccupied(rowIndex, columnIndex);
            }
            else if (IsConfirmedPositionNotOccupied(window) && IsUnconfirmedPositionsNotOccupied(window))
                MarkCellAsNotOccupied(rowIndex, columnIndex);
        }

        private bool IsUnconfirmedPositionsNotOccupied(TariffWindowWithRange window)
        {
            return (RollerPosition == RollerPositions.First && window.FirstPositionsUnconfirmed == 0) ||
                   (RollerPosition == RollerPositions.Second && window.SecondPositionsUnconfirmed == 0) ||
                   (RollerPosition == RollerPositions.Last && window.LastPositionsUnconfirmed == 0);
        }

        private bool IsConfirmedPositionNotOccupied(TariffWindowWithRange window)
        {
            return (RollerPosition == RollerPositions.First && !window.IsFirstPositionOccupied) ||
                   (RollerPosition == RollerPositions.Second && !window.IsSecondPositionOccupied) ||
                   (RollerPosition == RollerPositions.Last && !window.IsLastPositionOccupied);
        }

	    private void PopulateGridTable(DataTable dt)
		{
			_timeResolver = new Dictionary<string, string>();
			if (_tariffWindows != null && MaxBroadCast.HasValue)
			{
                foreach (DataRow row in dt.Rows)
                {
                    DataRow gridRow = dtGrid.NewRow();
                    int h = ParseHelper.GetInt32FromObject(row["h"], 0);
                    int m = ParseHelper.GetInt32FromObject(row["m"], 0);
					DateTime t = new DateTime(1, 1, 1, h, m, 0);
					t = t.AddMinutes(30);
                    gridRow[ColumnNames.TimeString] = string.Format("{0}-{1}", GetTimeString(h, m), GetTimeString(t.Hour, t.Minute));
                    _timeResolver.Add(gridRow[ColumnNames.TimeString].ToString(), GetTimeString(h, m));
                    dtGrid.Rows.Add(gridRow);
                }

				foreach (DataRow row in Data.Rows)
				{
					int h = ParseHelper.GetInt32FromObject(row["h"], 0);
                    int m = ParseHelper.GetInt32FromObject(row["m"], 0);
				    int? rowIndex = GetRow(h, m);
                    if (rowIndex.HasValue)
                    {
                        int iCol = ParseHelper.GetInt32FromObject(row["col"], 1) - 1;
                        dtGrid.Rows[rowIndex.Value][iCol + FixedCols] = GetCellContent(row);
                        _tariffWindows[rowIndex.Value - FIXED_ROWS, iCol] = new TariffWindowWithRange(row);
                    }
				}
			}
		}

	    private string GetTimeString(int h, int m)
	    {
	        return h >= MaxBroadCast.Value.Hour
	                   ? DateTimeUtils.Time2String(h, m)
	                   : DateTimeUtils.Time2String(h + 24, m);
	    }

	    private int? GetRow(int h, int m)
        {
	        int index = 0;
            foreach (DataRow row in dtGrid.Rows)
            {
				string key = row[ColumnNames.TimeString].ToString();
				if (_timeResolver.ContainsKey(key) && _timeResolver[key].Equals(GetTimeString(h, m)))
					return index;
                index++;
            }
	        return null;
        }

		protected virtual string GetCellContent(DataRow row)
		{
			DateTime windowDate = ParseHelper.GetDateTimeFromObject(row["date"], DateTime.MinValue);
			int timeWithConfirmed = ParseHelper.GetInt32FromObject(row["timeWithConfirmed"], 0);
			int timeWithUnConfirmed = ParseHelper.GetInt32FromObject(row["timeWithUnConfirmed"], 0);
			return BuildCellContent(windowDate, timeWithConfirmed, timeWithUnConfirmed);
		}

		private string BuildCellContent(DateTime windowDate, int timeWithConfirmed, int timeWithUnConfirmed)
		{
			if (showRollerNumbers)
			{
				string rollerNumbersText = GetRollerNumbersText(windowDate);
				if (rollerNumbersText != null)
					return rollerNumbersText;
			}

			return DateTimeUtils.Time2String(ShowUnconfirmed ? timeWithUnConfirmed : timeWithConfirmed);
		}

		// Номера роликов, размещённых в этом слоте (через запятую, в порядке размещения — как
		// они лежат в AddedIssues), или null, если слот свободен / номер ролика не известен.
		// AddedIssues уже фильтрует "свою" акцию и, что важно, один клик AddRangeIssues атомарно
		// пишет ОДИН И ТОТ ЖЕ ролик на все станции акции сразу (см. AddRangeIssues.sql — @rollerID
		// один параметр на весь курсор по станциям) — поэтому потеря привязки к конкретной
		// станции в AddedIssues не теряет сам номер ролика.
		private string GetRollerNumbersText(DateTime windowDate)
		{
			if (AddedIssues == null || rollerNumbers == null || windowDate == DateTime.MinValue) return null;

			DataRow[] issueRows = AddedIssues.Select(string.Format("[issueDate] = '{0}'", windowDate));
			if (issueRows.Length == 0) return null;

			List<string> numbers = new List<string>();
			foreach (DataRow issueRow in issueRows)
			{
				int rollerId = ParseHelper.GetInt32FromObject(issueRow[Roller.ParamNames.RollerId], 0);
				if (rollerNumbers.TryGetValue(rollerId, out int number))
					numbers.Add(number.ToString());
			}
			return numbers.Count > 0 ? string.Join(", ", numbers) : null;
		}

		// Перерисовать текст всех ячеек грида (без похода в БД) — нужно при включении/выключении
		// ShowRollerNumbers. При обычном RefreshGrid() (после клика добавления/удаления) тоже
		// вызывается — см. onGridPopulated — но там AddedIssues уже свежий на момент вызова.
		public void RefreshCellTexts()
		{
			int rowCount = _tariffWindows.GetLength(0);
			int columnCount = _tariffWindows.GetLength(1);

			for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
				for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
					if (_tariffWindows[rowIndex, columnIndex] is TariffWindowWithRange window)
						UpdateGridCell(rowIndex + FIXED_ROWS, columnIndex + FixedCols, window);
		}

		private void UpdateGridCell(int rowIndex, int columnIndex, TariffWindowWithRange window)
		{
			dtGrid.Rows[rowIndex][columnIndex] = BuildCellContent(window.WindowDate, window.TimeWithConfirmed, window.TimeWithUnConfirmed);
		}

		protected override void SetNavigationCaption()
		{
			Caption.Caption = string.Format("{0} - {1}", StartDate.ToString("dd.MM.yyyy"), FinishDate.ToString("dd.MM.yyyy"));
		}

		protected override void InitializeGridColumns()
		{
			gridColumns = new[]
				{
					new GridColumn("Время", ColumnNames.TimeString),
					new GridColumn("Пн.", ColumnNames.Monday),
					new GridColumn("Вт.", ColumnNames.Tuesday),
					new GridColumn("Ср.", ColumnNames.Wednesday),
					new GridColumn("Чт.", ColumnNames.Thursday),
					new GridColumn("Пт.", ColumnNames.Friday),
					new GridColumn("Сб.", ColumnNames.Saturday),
					new GridColumn("Вс.", ColumnNames.Sunday),
					new GridColumn(ColumnNames.Time, ColumnNames.Time, true)
				};
		}

		public override Entity IssueEntity
		{
			get { return null; }
		}

		public Roller Roller { get; set; }

	    private RollerPositions _rollerPosition;

		public RollerPositions RollerPosition { get => _rollerPosition; set { _rollerPosition = value; RefreshGrid();} }
				
		public PresentationObject Module 
		{ 
			get { return null;} set { }
		}

		public void RefreshCurrentCell(bool hasCurrentCampaignIssues, TariffGridRefreshMode mode)
		{
			throw new NotImplementedException();
		}

        public SecurityManager.User Grantor { get; set; }

        public void SetAdvertTypePresence(AdvertTypePresences advertTypePresence, PresentationObject advertType)
        {
            RefreshGrid();
        }

        // ---------------------------------------------------------------
        // TimePeriod range generation support
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns the Monday that starts the ISO week containing <paramref name="date"/>.
        /// Handles Sunday correctly (DayOfWeek.Sunday == 0).
        /// </summary>
        private static DateTime GetWeekMonday(DateTime date)
        {
            int diff = (7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return date.Date.AddDays(-diff);
        }

        /// <summary>
        /// Loads all 30-min slots for the ISO week that contains <paramref name="weekMonday"/>
        /// from the TariffWindowWithRange stored procedure and caches them in
        /// <paramref name="slotsCache"/> (keyed by Monday date).
        /// </summary>
        private List<TariffWindowWithRange> GetSlotsForWeek(
            DateTime weekMonday,
            Dictionary<DateTime, List<TariffWindowWithRange>> slotsCache)
        {
            if (!slotsCache.TryGetValue(weekMonday, out List<TariffWindowWithRange> slots))
            {
                Dictionary<string, object> dict = DataAccessor.CreateParametersDictionary();
                dict.Add("dateStart", weekMonday);
                dict.Add("actionID", _action.ActionId);
                DataSet ds = DataAccessor.LoadDataSet("TariffWindowWithRange", dict);
                slots = ds.Tables[0].Rows
                          .Cast<DataRow>()
                          .Select(r => new TariffWindowWithRange(r))
                          .ToList();
                slotsCache[weekMonday] = slots;
            }
            return slots;
        }

        /// <summary>
        /// Adds issues for all selected slots within the <paramref name="startTime"/>–
        /// <paramref name="finishTime"/> window on <paramref name="date"/> (both bounds
        /// inclusive, matching the Simple TimePeriod behaviour).
        ///
        /// When <paramref name="quantity"/> &gt; 0: takes the first N available slots
        /// chronologically regardless of prime.
        /// When <paramref name="quantity"/> == 0: fills <paramref name="quantityPrime"/>
        /// prime slots and <paramref name="quantityNonPrime"/> non-prime slots.
        ///
        /// Does NOT call _action.Recalculate() — the caller (FrmGenerator.finally) is
        /// responsible for the single end-of-generation recalculate.
        /// </summary>
        public TimePeriodAddResult AddIssuesRangeTimePeriod(
            DateTime date,
            DateTime startTime,
            DateTime finishTime,
            int quantity,
            int quantityPrime,
            int quantityNonPrime,
            bool ignoreWindowsWithTheSameFirmIssue,
            Dictionary<DateTime, List<TariffWindowWithRange>> slotsCache)
        {
            using (OperationScope.Start(
                $"AddIssuesRangeTimePeriod date={date:yyyy-MM-dd} " +
                $"q={quantity}/{quantityPrime}p/{quantityNonPrime}np"))
            {
                DateTime weekMonday = GetWeekMonday(date);
                List<TariffWindowWithRange> weekSlots = GetSlotsForWeek(weekMonday, slotsCache);

                // Filter: same calendar date, within the time window (both bounds inclusive)
                TimeSpan tsStart  = startTime.TimeOfDay;
                TimeSpan tsFinish = finishTime.TimeOfDay;

                var rnd = new Random();

                List<TariffWindowWithRange> slotsForDate = weekSlots
                    .Where(s => s.WindowDate.Date == date.Date
                             && s.WindowDate.TimeOfDay >= tsStart
                             && s.WindowDate.TimeOfDay < tsFinish
                             && (_rollerPosition == RollerPositions.Undefined || IsPositionAvailable(s))
                             && (!ignoreWindowsWithTheSameFirmIssue || !HasFirmIssuesFlags(s)))
                    .Select(s => new { w = s, rand = rnd.Next() })
                    .OrderBy(x => x.w)       // IComparable — сначала самые свободные (max TimeWithUnConfirmed)
                    .ThenBy(x => x.rand)     // среди одинаковых — рандом
                    .Select(x => x.w)
                    .ToList();

                var result = new TimePeriodAddResult();

                IEnumerable<TariffWindowWithRange> selectedSlots;
                if (quantity > 0)
                {
                    selectedSlots = slotsForDate.Take(quantity);
                    result.ExpectedCount = quantity;
                }
                else
                {
                    IEnumerable<TariffWindowWithRange> primeSlots    = slotsForDate.Where(s =>  s.IsPrime).Take(quantityPrime);
                    IEnumerable<TariffWindowWithRange> nonPrimeSlots = slotsForDate.Where(s => !s.IsPrime).Take(quantityNonPrime);
                    selectedSlots = primeSlots.Concat(nonPrimeSlots);
                    result.ExpectedCount = quantityPrime + quantityNonPrime;
                }

                foreach (TariffWindowWithRange slot in selectedSlots)
                {
                    try
                    {
                        DataRow row = AddIssuesRange(slot.WindowDate,
                            ignoreWindowsWithTheSameFirmIssue,
                            recalculate: false);
                        result.Rows.Add(row);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(ex);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Аналог <see cref="AddIssuesRangeTimePeriod"/>, но роликов несколько: вместо единого Roller
        /// на каждый выбранный слот берёт следующий ролик из <paramref name="takeRollersForToday"/> —
        /// колбэк получает "сколько слотов реально нашлось" и возвращает столько же роликов
        /// из общей случайно перемешанной очереди (отсортированных по убыванию длительности,
        /// см. RollerAllocationQueue). Если слотов сегодня меньше, чем нужно по дневной норме,
        /// колбэк заберёт из очереди меньше — остаток естественным образом уйдёт на следующий день.
        /// </summary>
        public TimePeriodAddResult AddIssuesRangeTimePeriodMultiRoller(
            DateTime date,
            DateTime startTime,
            DateTime finishTime,
            int quantity,
            int quantityPrime,
            int quantityNonPrime,
            bool ignoreWindowsWithTheSameFirmIssue,
            Dictionary<DateTime, List<TariffWindowWithRange>> slotsCache,
            Func<int, List<Roller>> takeRollersForToday)
        {
            using (OperationScope.Start(
                $"AddIssuesRangeTimePeriodMultiRoller date={date:yyyy-MM-dd} " +
                $"q={quantity}/{quantityPrime}p/{quantityNonPrime}np"))
            {
                DateTime weekMonday = GetWeekMonday(date);
                List<TariffWindowWithRange> weekSlots = GetSlotsForWeek(weekMonday, slotsCache);

                TimeSpan tsStart = startTime.TimeOfDay;
                TimeSpan tsFinish = finishTime.TimeOfDay;

                var rnd = new Random();

                List<TariffWindowWithRange> slotsForDate = weekSlots
                    .Where(s => s.WindowDate.Date == date.Date
                             && s.WindowDate.TimeOfDay >= tsStart
                             && s.WindowDate.TimeOfDay < tsFinish
                             && (_rollerPosition == RollerPositions.Undefined || IsPositionAvailable(s))
                             && (!ignoreWindowsWithTheSameFirmIssue || !HasFirmIssuesFlags(s)))
                    .Select(s => new { w = s, rand = rnd.Next() })
                    .OrderBy(x => x.w)
                    .ThenBy(x => x.rand)
                    .Select(x => x.w)
                    .ToList();

                var result = new TimePeriodAddResult();

                if (quantity > 0)
                {
                    List<TariffWindowWithRange> selectedSlots = slotsForDate.Take(quantity).ToList();
                    result.ExpectedCount = quantity;
                    PlaceRollersInSlots(selectedSlots, takeRollersForToday, ignoreWindowsWithTheSameFirmIssue, result);
                }
                else
                {
                    List<TariffWindowWithRange> primeSlots = slotsForDate.Where(s => s.IsPrime).Take(quantityPrime).ToList();
                    List<TariffWindowWithRange> nonPrimeSlots = slotsForDate.Where(s => !s.IsPrime).Take(quantityNonPrime).ToList();
                    result.ExpectedCount = quantityPrime + quantityNonPrime;
                    PlaceRollersInSlots(primeSlots, takeRollersForToday, ignoreWindowsWithTheSameFirmIssue, result);
                    PlaceRollersInSlots(nonPrimeSlots, takeRollersForToday, ignoreWindowsWithTheSameFirmIssue, result);
                }

                return result;
            }
        }

        private void PlaceRollersInSlots(
            List<TariffWindowWithRange> slots,
            Func<int, List<Roller>> takeRollersForToday,
            bool ignoreWindowsWithTheSameFirmIssue,
            TimePeriodAddResult result)
        {
            List<Roller> rollers = takeRollersForToday(slots.Count);
            for (int i = 0; i < rollers.Count; i++)
            {
                try
                {
                    DataRow row = AddIssuesRange(slots[i].WindowDate, rollers[i],
                        ignoreWindowsWithTheSameFirmIssue, recalculate: false);
                    result.Rows.Add(row);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex);
                }
            }
        }

        // helper to keep AddedIssues sorted by issueDate
        private void InsertIssueRowSorted(DataRow row)
        {
            DateTime newIssueDate = ParseHelper.GetDateTimeFromObject(row["issueDate"], DateTime.MinValue);
            int insertIndex = AddedIssues.Rows.Count;

            for (int i = 0; i < AddedIssues.Rows.Count; i++)
            {
                DateTime existingDate = ParseHelper.GetDateTimeFromObject(AddedIssues.Rows[i]["issueDate"], DateTime.MaxValue);
                if (existingDate > newIssueDate)
                {
                    insertIndex = i;
                    break;
                }
            }

            AddedIssues.Rows.InsertAt(row, insertIndex);
        }
	}
}
