using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Controls
{
	internal readonly struct GridColumn
	{
		public readonly string HeaderText;
		public readonly string ColumnName;
		public readonly bool IsHidden;
		public readonly string FormatString;
		public readonly Type DataType;

		public GridColumn(string headerText, string columnName, bool isHidden, string formatString, Type dataType)
		{
			HeaderText = headerText;
			ColumnName = columnName;
			IsHidden = isHidden;
			FormatString = formatString;
			DataType = dataType;
		}

		public GridColumn(string headerText, string columnName, bool isHidden)
			: this(headerText, columnName, isHidden, string.Empty, typeof(string))
		{
		}

		public GridColumn(string headerText, string columnName)
			: this(headerText, columnName, false)
		{
		}

		public GridColumn(string headerText, string columnName, string formatString, Type dataType)
			: this(headerText, columnName, false, formatString, dataType)
		{
		}
	}

	public enum EditMode
	{
		View,
		Edit,
		Template,
		TransferIssue
	}

	public delegate void TariffWindowDelegate(ITariffWindow tariffWindow);

	internal abstract partial class TariffGrid : UserControl
	{
		protected delegate void LoadPricelistDelegate();
		protected delegate void PopulateGridDelegate();
		protected delegate void OnGridPopulatedDelegate();
		protected delegate void UpdateDBDelegate(DataGridViewCell cell);

		#region Constants -------------------------------------

		protected short FixedCols = 2;
		protected const short FIXED_ROWS = 2;
		protected const short ROW_DATE = 0;
		protected const short ROW_ISSUES_COUNT = 1;

		protected struct ColumnNames
		{
			public const string TariffId = "tariffId";
			public const string Time = "time";
			public const string Comment = "comment";
			public const string TimeString = "timeString";
			public const string Hour = "hour";
			public const string Min = "min";
			public const string Price = "price";
			public const string Duration = "duration";
			public const string Monday = "monday";
			public const string Tuesday = "tuesday";
			public const string Wednesday = "wednesday";
			public const string Thursday = "thursday";
			public const string Friday = "friday";
			public const string Saturday = "saturday";
			public const string Sunday = "sunday";
			public const string WindowDateOriginal = "windowDateOriginal";
			public const string WindowDateActual = "windowDateActual";
			public const string WindowDateBroadcast = "windowDateBroadcast";
			public const string WindowDateActualBroadcast = "windowDateActualBroadcast";
		}

		private enum Directions
		{
			Forward,
			Back
		}

		public enum GridCellTypes
		{
			Generic,
			CheckBoxes
		}

		#endregion

		#region Members ---------------------------------------

		protected EditMode editMode;
		protected Campaign campaign;
		protected Pricelist pricelist;
		protected DateTime currentDate = DateTime.Today.AddDays(1);
		protected DateTime monday, startDate, finishDate;
		protected DataTable dtGrid;

		protected DataTable dtIssue;
		protected ITariffWindow[,] tariffWindows;
		protected DateTime[] weekDates;
		protected GridColumn[] gridColumns;
		protected GridCellTypes gridCellType = GridCellTypes.Generic;
		protected ITariffWindow selectedWindow;

		protected LoadPricelistDelegate loadPricelist;
		protected PopulateGridDelegate populateGrid;
		protected OnGridPopulatedDelegate onGridPopulated;
		protected UpdateDBDelegate updateDB;

		#endregion

		protected TariffGrid()
		{
			ShowUnconfirmed = true;
			InitializeComponent();
			InitGridHeaders();
			SetEventHandlers();
		}

		public virtual EditMode EditMode
		{
			get { return editMode; }
			set
			{
				editMode = value;
				Cursor = editMode == EditMode.View ? Cursors.Default : Cursors.Hand;
				switch (editMode)
				{
					case EditMode.View:
						Caption.CaptionBackColor = Color.FromName(KnownColor.GrayText.ToString());
						break;
					case EditMode.TransferIssue:
                        Caption.CaptionBackColor = Color.BlueViolet;
						break;
					case EditMode.Edit:
                        Caption.CaptionBackColor = Color.Blue;
                        break;
                    case EditMode.Template:
                        Caption.CaptionBackColor = Color.DarkBlue;
                        break;
                }

				grid.Cursor = Cursor;

				if (gridCellType == GridCellTypes.CheckBoxes)
					SetCheckBoxCells();
			}
		}

		[Browsable(false)]
		public bool IsActiveCellSelected
		{
			get
			{
				if(RawDataGridView.CurrentCell == null) return false;

				return !(RawDataGridView.CurrentCell.RowIndex < FIXED_ROWS ||
				         RawDataGridView.CurrentCell.ColumnIndex < FixedCols);
			}
		}

		[Browsable(false)]
		public Pricelist Pricelist
		{
			get { return pricelist; }
			set
			{
				pricelist = value;
				if (pricelist != null) currentDate = pricelist.StartDate;
			}
		}

		public DateTime StartDate
		{
			get { return startDate; }
		}

		public DateTime FinishDate
		{
			get { return finishDate; }
		}

		public bool ShowMessages
		{
			get;set;
		}

		private bool _showUnconfirmed;

		public bool ShowUnconfirmed
		{
			[DebuggerStepThrough]
			get { return _showUnconfirmed; }
			[DebuggerStepThrough]
			set
			{
				_showUnconfirmed = value;
				if (gridCellType == GridCellTypes.CheckBoxes)
					SetCheckBoxCells();
			}
		}

		public event EmptyDelegate CampaignStatusChanged;
		public event TariffWindowDelegate CellClicked;
		public event EmptyDelegate GridRefreshed;

		protected abstract void InitializeGridColumns();
		public abstract Entity IssueEntity { get;}		

		public virtual void Clear()
		{
			RawDataGridView.DataSource = null;
			Caption.Caption = string.Empty;
		}

		public virtual void RefreshGrid()
		{
			Clear();
			if (loadPricelist != null) loadPricelist();
			DisplayGridData();
			grid.SetColumnsWidth();
			if(GridRefreshed != null) GridRefreshed();
		}

		public DataGridView InternalGrid
		{
			get { return grid.RawDataGridView; }
		}

		private void SetEventHandlers()
		{
			RawDataGridView.CellClick += OnGridCellClick;
			RawDataGridView.CurrentCellDirtyStateChanged += OnCurrentCellDirtyStateChanged;
		}

		private void OnCurrentCellDirtyStateChanged(object sender, EventArgs e)
		{
			try
			{
				if (gridCellType == GridCellTypes.CheckBoxes && RawDataGridView.IsCurrentCellDirty)
				{
					updateDB(RawDataGridView.CurrentCell);
					RawDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
					FireCellClicked(GetTariffWindow(RawDataGridView.CurrentCell));
				}
			}
			catch (Exception ex)
			{
				RawDataGridView.CancelEdit();
				ErrorManager.PublishError(ex);
			}
		}

		private void DisplayGridData()
		{
			CreateGridTable();
			if (pricelist != null)
			{
				SetGridCaptions();
				SetNavigationCaption();

				populateGrid();
				RawDataGridView.AutoGenerateColumns = false;
				RawDataGridView.DataSource = dtGrid.DefaultView;
				if (onGridPopulated != null) onGridPopulated();
				SetFrozenRowsAndColumns();

				if (gridCellType == GridCellTypes.CheckBoxes)
					SetCheckBoxCells();
				if (selectedWindow != null)
					RestoreCurrentWindow();
			}
			else if(ShowMessages && grid is IRollerGrid) // пока проверка сделана только для обычных тарифов 
				FogSoft.WinForm.Forms.MessageBox.ShowInformation(Properties.Resources.NoPricelistForGivenDate);
		}

		private void RestoreCurrentWindow()
		{
			RawDataGridView.CurrentCell = GetCell(selectedWindow);
		}

		private void SetFrozenRowsAndColumns()
		{
			if (RawDataGridView.RowCount == 0) return;

			for (int i = 0; i < FIXED_ROWS; i++)
				RawDataGridView.Rows[i].Frozen = true;

			for (int i = 0; i < FixedCols; i++)
				RawDataGridView.Columns[i].Frozen = true;

			// Paint fixed cells
			for (int row = 0; row < FIXED_ROWS; row++)
				for (int col = 0; col < RawDataGridView.Columns.Count; col++)
					CopyColumnHeaderCellStyle(GetCell(row, col), DataGridViewContentAlignment.MiddleCenter);

			for (int row = 0; row < RawDataGridView.RowCount; row++)
				for (int col = 0; col < FixedCols; col++)
					CopyColumnHeaderCellStyle(GetCell(row, col), DataGridViewContentAlignment.MiddleRight);
		}

		private void CopyColumnHeaderCellStyle(DataGridViewCell cell, DataGridViewContentAlignment alignment)
		{
			cell.Style.BackColor = RawDataGridView.ColumnHeadersDefaultCellStyle.BackColor;
			cell.Style.SelectionBackColor = RawDataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor;
			cell.Style.SelectionForeColor = RawDataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor;
			cell.Style.Alignment = alignment;
		}

		public DataGridView RawDataGridView
		{
			get { return grid.RawDataGridView; }
		}

		protected void CreateGridTable()
		{
			dtGrid = new DataSet().Tables.Add();
		}

		protected void MarkColumnCells(int columnIndex, Color backColor)
		{
			for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.RowCount; rowIndex++)
				SetCellBackColor(rowIndex, columnIndex, backColor);
		}

		protected virtual void SetNavigationCaption()
		{
			Caption.Caption = pricelist != null ? string.Format("Прайс-лист: {0} - {1}",
			                                                    pricelist.StartDate.ToShortDateString(),
			                                                    pricelist.FinishDate.ToShortDateString()) : string.Empty;
		}

        protected void ChangeIssuesCounter(int column, int changes)
		{
			int val = int.Parse(dtGrid.Rows[ROW_ISSUES_COUNT][column].ToString());
			dtGrid.Rows[ROW_ISSUES_COUNT][column] = val + changes;
		}

		protected void InitGridHeaders()
		{
			InitializeGridColumns();
			foreach (GridColumn gridColumn in gridColumns)
				if (!gridColumn.IsHidden)
					AddColumn(gridColumn);
		}

		private void AddColumn(GridColumn gridColumn)
		{
			DataGridViewColumn column = CreateDataGridColumn(gridColumn.ColumnName);
			column.DataPropertyName = gridColumn.ColumnName;
			column.HeaderText = gridColumn.HeaderText;
			column.CellTemplate.Style.Format = gridColumn.FormatString;
			column.SortMode = DataGridViewColumnSortMode.NotSortable;

			RawDataGridView.Columns.Add(column);
		}

		protected virtual DataGridViewColumn CreateDataGridColumn(string mappingName)
		{
			return new DataGridViewTextBoxColumn();
		}

		protected void SetGridCaptions()
		{
			foreach (GridColumn gridColumn in gridColumns)
			{
				DataColumn dataColumn = dtGrid.Columns.Add(gridColumn.ColumnName);
				dataColumn.DataType = gridColumn.DataType;
			}

			// add first row with dates
			DataRow row = dtGrid.NewRow();
			DateTime theDate = currentDate;

			while (theDate.DayOfWeek != DayOfWeek.Monday)
				theDate = theDate.AddDays(-1);

			monday = theDate;
			while (monday.DayOfWeek != DayOfWeek.Monday)
				monday = monday.AddDays(-1);

			startDate = theDate;

			if (pricelist != null && startDate < pricelist.StartDate)
				startDate = pricelist.StartDate;

			// Init array with Min date just to have all 7 days in any case
			weekDates = new DateTime[7];
			for (int i = 0; i < weekDates.Length; i++)
				weekDates[i] = DateTime.MinValue;

			do
			{
				row[FixedCols - 1 + DateTimeUtils.ResolveDayOfWeekNumber(theDate.DayOfWeek)] =
					theDate.ToShortDateString();
				weekDates[DateTimeUtils.ResolveDayOfWeekNumber(theDate.DayOfWeek) - 1] = theDate;
				theDate = theDate.AddDays(1);
			} while (theDate.DayOfWeek != DayOfWeek.Monday && pricelist != null && theDate <= pricelist.FinishDate);

			finishDate = theDate.AddDays(-1);

			if (pricelist != null && finishDate > pricelist.FinishDate)
				finishDate = pricelist.FinishDate;

			dtGrid.Rows.Add(row);

			//Add row for amount of issues 
			row = dtGrid.NewRow();
			for (int i = 0; i < weekDates.Length; i++)
				if (monday.AddDays(i) >= startDate)
					row[FixedCols + i] = 0;
			dtGrid.Rows.Add(row);
		}

		protected DataGridViewCell CurrentCell
		{
			get { return RawDataGridView.CurrentCell; }
		}

		private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
		{
			try
			{
				if (e.ColumnIndex >= FixedCols && e.RowIndex >= FIXED_ROWS)
					FireCellClicked(GetTariffWindow(e.RowIndex, e.ColumnIndex));
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void FireCellClicked(ITariffWindow tariffWindow)
		{
			Application.DoEvents();
			Cursor.Current = Cursors.WaitCursor;

			if (tariffWindow != null)
			{
				selectedWindow = tariffWindow;
				if (editMode == EditMode.Edit && gridCellType == GridCellTypes.Generic && updateDB != null)
					updateDB(GetCell(tariffWindow));
			}
			onCellClicked(tariffWindow);
		}

		protected virtual void onCellClicked(ITariffWindow tariffWindow)
		{
			if (CellClicked != null) CellClicked(tariffWindow);
		}

		protected ITariffWindow GetTariffWindow(DataGridViewCell cell)
		{
			return cell == null ? null : GetTariffWindow(cell.RowIndex, cell.ColumnIndex);
		}

		protected ITariffWindow GetTariffWindow(int rowIndex, int columnIndex)
		{
			if (rowIndex < FIXED_ROWS || columnIndex < FixedCols) return null;
			return tariffWindows[rowIndex - FIXED_ROWS, columnIndex - FixedCols];
		}

		protected DataGridViewCell GetCell(ITariffWindow tariffWindow)
		{
			for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.RowCount; rowIndex++)
				for (int columnIndex = FixedCols; columnIndex < RawDataGridView.ColumnCount; columnIndex++)
				{
					ITariffWindow currentWindow = GetTariffWindow(GetCell(rowIndex, columnIndex));
					if (tariffWindow.Equals(currentWindow))
						return GetCell(rowIndex, columnIndex);
				}
			return null;
		}

		private void Caption_GoNext()
		{
			GoToNextWeek(Directions.Forward);
		}

		private void Caption_GoPrevious()
		{
			GoToNextWeek(Directions.Back);
		}

		private void GoToNextWeek(Directions direction)
		{
			try
			{
				if (pricelist == null)
					return;

				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				if (direction == Directions.Forward)
					currentDate = finishDate.AddDays(1);
				else if (direction == Directions.Back)
					currentDate = startDate.AddDays(-1);

				RefreshGrid();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = editMode == EditMode.View ? Cursors.Default : Cursors.Hand;
            }
		}

		protected void FireCampaignStatusChanged()
		{
			if (CampaignStatusChanged != null) CampaignStatusChanged();
		}

		protected void SetCellBackColor(int rowIndex, int columnIndex, Color color)
		{
			GetCell(rowIndex, columnIndex).Style.BackColor =
				/*
      GetCell(rowIndex, columnIndex).Style.SelectionBackColor =*/ color;
		}

		protected void SetCellForeColor(int rowIndex, int columnIndex, Color color)
		{
			DataGridViewCell cell = GetCell(rowIndex, columnIndex);		
			cell.Style.ForeColor = cell.Style.SelectionForeColor = color;
		}

		protected DataGridViewCell GetCell(int rowIndex, int columnIndex)
		{
			return RawDataGridView[columnIndex, rowIndex];
		}

		[Browsable(false)]
		public DateTime CurrentDate
		{
			get { return currentDate; }
			set { currentDate = value; }
		}

		public ITariffWindow CurrentTariffWindow
		{
			get { return selectedWindow; }
		}

		private void SetCheckBoxCells()
		{
			for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.Rows.Count; rowIndex++)
				for (int columnIndex = FixedCols; columnIndex < RawDataGridView.Columns.Count; columnIndex++)
					ProcessBooleanCell(GetCell(rowIndex, columnIndex));
		}

		protected virtual DataGridViewCell ProcessBooleanCell(DataGridViewCell cell)
		{
			if (!IsBooleanValue(cell))
			{
				cell.ReadOnly = true;
				cell.Style.WrapMode = DataGridViewTriState.True;
				cell.ToolTipText = cell.Value.ToString();
			}
			else
			{			
				DataGridViewTextCheckBoxCell checkboxCell = new DataGridViewTextCheckBoxCell(false);
				RawDataGridView[cell.ColumnIndex, cell.RowIndex] = checkboxCell;
				checkboxCell.ReadOnly = !(editMode == EditMode.Edit);
				checkboxCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
				return checkboxCell;
			}
			return cell;
		}

		private static bool IsBooleanValue(DataGridViewCell cell)
		{
			bool res;
			return bool.TryParse(cell.Value.ToString(), out res);
		}

		protected void SetDataSourceValue(DataGridViewCell cell, object value)
		{
			dtGrid.Rows[cell.RowIndex][cell.ColumnIndex] = value;
		}

		protected DataRowView CurrentRowView
		{
			get
			{
				BindingManagerBase bm = BindingContext[RawDataGridView.DataSource];
				if (bm == null || bm.Position < 0) return null;
				return bm.Current as DataRowView;
			}
		}

		protected decimal CurrentRowPrice
		{
			get { return decimal.Parse(CurrentRowView[ColumnNames.Price].ToString()); }
		}

		protected void MarkCellAsNotHavingCurrentCampaignIssues(DataGridViewCell cell)
		{
			MarkCellAsNotHavingCurrentCampaignIssues(cell.RowIndex, cell.ColumnIndex);
		}

		protected void MarkCellAsNotHavingCurrentCampaignIssues(int rowIndex, int columnIndex)
		{
			SetCellForeColor(rowIndex, columnIndex, RawDataGridView.DefaultCellStyle.ForeColor);
		}

		protected void MarkCellAsHavingCurrentCampaignIssues(DataGridViewCell cell)
		{
			MarkCellAsHavingCurrentCampaignIssues(cell.RowIndex, cell.ColumnIndex);
		}

		protected void MarkCellAsHavingCurrentCampaignIssues(int rowIndex, int columnIndex)
		{
			SetCellForeColor(rowIndex, columnIndex, Color.Blue);
		}

		public bool Jump2Date()
		{
			FrmDateSelector fSelector = new FrmDateSelector("Выбор даты");
			fSelector.Mode = FrmDateSelector.SelectorMode.SelectOne;
			if(fSelector.ShowDialog(this) == DialogResult.OK)
			{
				CurrentDate = fSelector.StartDate;
				return true;
			}
			return false;
		}

		protected void MarkCellAsNotOccupied(int rowIndex, int columnIndex)
		{
			GetCell(rowIndex, columnIndex).Style.Font = new Font(RawDataGridView.DefaultCellStyle.Font, FontStyle.Bold);
		}

		private void MarkCellAsOccupied(DataGridViewCell cell)
		{
			cell.Style.Font = RawDataGridView.DefaultCellStyle.Font;
		}

		protected void MarkCellAsOccupied(int rowIndex, int columnIndex)
		{
			MarkCellAsOccupied(GetCell(rowIndex, columnIndex));
		}

        protected void MarkCellAsDisabled(int rowIndex, int columnIndex)
        {
            
            //SetCellBackColor(rowIndex, columnIndex, Color.LightPink);
            SetCellBackColor(rowIndex, columnIndex, Color.FromArgb(255, 231, 234));
        }
        protected void MarkCellAsMarked(int rowIndex, int columnIndex)
        {
            SetCellBackColor(rowIndex, columnIndex, Color.LightSteelBlue); 
        }
    }
}