using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Classes.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Merlin.Controls
{
	internal partial class TariffWithRangeGrid : TariffGrid, IRollerGrid
	{
		private const string MinBroadcastColumnName = "minBroadcast";
		private const string MaxBroadcastColumnName = "maxBroadcast";
        private readonly ActionOnMassmedia _action;
        private readonly int _massmediasCount;
		private Dictionary<string, string> _timeResolver;

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
					tariffWindows = new ITariffWindow[(int)(MaxBroadCast.Value.AddDays(1) - MinBroadCast.Value).TotalHours * 2, 7];
				else
					tariffWindows = null;

                PopulateGridTable(dataSet.Tables[2]);
			};

			updateDB = delegate (DataGridViewCell cell)
			{
			   AddIssuesRange(cell);
			};

			onGridPopulated = delegate
			{
                MarkCells();
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
			};
		}

		public DataRow AddIssuesRange(DateTime windowDate)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["actionID"] = _action.ActionId;
			parameters["issueDate"] = windowDate;
			parameters["rollerID"] = Roller.RollerId;
			parameters["rollerDuration"] = Roller.Duration;
			parameters["positionId"] = (int)RollerPosition;
			parameters["considerUnconfirmed"] = ShowUnconfirmed ? 1 : 0;
            if (Grantor != null)
				parameters["grantorID"] = Grantor.Id;
			DataAccessor.ExecuteNonQuery("AddRangeIssues", parameters);
			_action.Recalculate();

			DataRow row = AddedIssues.NewRow();
			row[Issue.ParamNames.IssueId] = (new Random()).Next();
			row["issueDate"] = windowDate;
			row[Entity.ParamNames.NAME] = Roller.Name;
			row["rollerID"] = Roller.RollerId;
			row["durationString"] = Roller.DurationString;
			row["RowNum"] = Guid.NewGuid();
			row["position"] = RollerPosition == RollerPositions.Last ? "Последний" : RollerPosition == RollerPositions.First ? "Первый" : RollerPosition == RollerPositions.Second ? "Второй" : "Неопределена";
			row["positionID"] = (int)RollerPosition;
			
			// replace AddedIssues.Rows.Add(row); with sorted insert
			InsertIssueRowSorted(row);
			
			return row;
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
                    if (RollerPosition != RollerPositions.Undefined) 
                        MarkCellWithRollerPosition(window, rowIndex, columnIndex);
                    if (window != null && AddedIssues.Select(string.Format("[issueDate] = '{0}'", window.WindowDate)).Length > 0)
                        MarkCellAsHavingCurrentCampaignIssues(rowIndex, columnIndex);
                }
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
			if (tariffWindows != null && MaxBroadCast.HasValue)
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
                        tariffWindows[rowIndex.Value - FIXED_ROWS, iCol] = new TariffWindowWithRange(row);
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
			return DateTimeUtils.Time2String(ParseHelper.GetInt32FromObject(row[ShowUnconfirmed ? "timeWithUnConfirmed" : "timeWithConfirmed"], 0));
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

        public void DeleteIssue(MasterIssue issue)
		{
			try
			{
				Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
				parameters[Merlin.Classes.Action.ParamNames.ActionId] = _action.ActionId;
				parameters["issueDate"] = issue["issueDate"];
				parameters["rollerID"] = issue["rollerID"];
				parameters["positionId"] = issue["positionID"];
				if (Grantor != null)
					parameters["grantorID"] = Grantor.Id;
				DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters);
				_action.Recalculate();

				foreach (DataRow row in AddedIssues.Select(string.Format("RowNum = '{0}'", issue["RowNum"])))
					AddedIssues.Rows.Remove(row);
			}
			catch(Exception e)
			{
				ErrorManager.PublishError(e);
			}
			finally
			{
				RefreshGrid();
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
