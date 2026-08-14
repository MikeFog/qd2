using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Controls
{
	/// <summary>Модуль комбо-модуля в конкретном дне - то, что стоит за ячейкой грида.</summary>
	internal class ComboModuleDay
	{
		public int ModuleID;
		public int MassmediaID;
		public string MassmediaName;
		public string ModuleName;
		public int ModulePriceListID;
		public decimal Price;
		public DateTime Date;
		/// <summary>Остаток времени в самом заполненном окне модуля, сек.</summary>
		public int? FreeTime;
		/// <summary>Остаток по количеству в самом заполненном штучном окне. Null - штучных окон нет.</summary>
		public int? FreeCapacity;
		/// <summary>Вместимость того самого штучного окна.</summary>
		public int? MaxCapacity;

		/// <summary>Текст ячейки - как в обычном гриде: «02:30» либо «02:30 [3/5]».</summary>
		public string CellText
		{
			get
			{
				string text = FreeTime.HasValue ? DateTimeUtils.Time2String(FreeTime.Value) : string.Empty;
				if (FreeCapacity.HasValue)
					text = string.Format("{0} [{1}/{2}]", text, FreeCapacity, MaxCapacity).TrimStart();
				return text;
			}
		}
	}

	internal delegate void ComboModuleDayDelegate(ComboModuleDay day);

	public enum ComboModulePeriodMode
	{
		Week,
		Month
	}

	/// <summary>
	/// Грид размещения комбо-модулями: строка - модуль комбо-модуля, колонка - день,
	/// в ячейке - остаток времени в самом заполненном окне модуля за этот день.
	///
	/// От TariffGrid отличается тем, что колонок не всегда семь (режим месяца) и строка -
	/// не тарифное окно, а модуль, поэтому наследоваться от него смысла нет: там и сетка
	/// колонок, и адресация ячеек жёстко недельные.
	/// </summary>
	internal partial class ComboModuleGrid : UserControl
	{
		#region Constants -------------------------------------

		private const int FIXED_COLS = 2;   // радиостанция, модуль
		private const int FIXED_ROWS = 2;   // даты, число выпусков
		private const int ROW_DATE = 0;
		private const int ROW_ISSUES_COUNT = 1;

		private const string COLUMN_MASSMEDIA = "massmedia";
		private const string COLUMN_MODULE = "module";
		private const string COLUMN_DAY_PREFIX = "day";

		#endregion

		#region Members ---------------------------------------

		private int _comboModuleID;
		private DateTime _currentDate = DateTime.Today;
		private DateTime _startDate, _finishDate;
		private ComboModulePeriodMode _periodMode = ComboModulePeriodMode.Week;
		private bool _showUnconfirmed = true;

		private DataTable _modules;
		private DataTable _dtGrid;
		private ComboModuleDay[,] _days;

		#endregion

		public event ComboModuleDayDelegate CellClicked;

		public ComboModuleGrid()
		{
			InitializeComponent();
			RawDataGridView.CellClick += OnGridCellClick;
			RawDataGridView.AutoGenerateColumns = false;
		}

		#region Properties ------------------------------------

		/// <summary>Комбо-модуль, размещение по которому идёт. Смена сбрасывает список модулей.</summary>
		public int ComboModuleID
		{
			get { return _comboModuleID; }
			set
			{
				_comboModuleID = value;
				_modules = null;
			}
		}

		public ComboModulePeriodMode PeriodMode
		{
			get { return _periodMode; }
			set { _periodMode = value; }
		}

		public bool ShowUnconfirmed
		{
			get { return _showUnconfirmed; }
			set { _showUnconfirmed = value; }
		}

		public DateTime CurrentDate
		{
			get { return _currentDate; }
			set { _currentDate = value; }
		}

		public DateTime StartDate
		{
			get { return _startDate; }
		}

		public DateTime FinishDate
		{
			get { return _finishDate; }
		}

		public DataGridView RawDataGridView
		{
			get { return grid; }
		}

		#endregion

		public void RefreshGrid()
		{
			ResolvePeriod();
			LoadModules();

			CreateColumns();
			CreateRows();
			FillFreeTime();

			RawDataGridView.DataSource = _dtGrid.DefaultView;
			SetFrozenRowsAndColumns();
			SetColumnWidths();
			SetNavigationCaption();
		}

		#region Период ----------------------------------------

		private void ResolvePeriod()
		{
			if (_periodMode == ComboModulePeriodMode.Week)
			{
				_startDate = _currentDate.Date;
				while (_startDate.DayOfWeek != DayOfWeek.Monday)
					_startDate = _startDate.AddDays(-1);
				_finishDate = _startDate.AddDays(6);
			}
			else
			{
				_startDate = new DateTime(_currentDate.Year, _currentDate.Month, 1);
				_finishDate = _startDate.AddMonths(1).AddDays(-1);
			}
		}

		private void SetNavigationCaption()
		{
			Caption.Caption = string.Format("{0} - {1}",
				_startDate.ToShortDateString(), _finishDate.ToShortDateString());
		}

		private void Caption_GoNext()
		{
			GoToPeriod(_finishDate.AddDays(1));
		}

		private void Caption_GoPrevious()
		{
			GoToPeriod(_startDate.AddDays(-1));
		}

		private void GoToPeriod(DateTime date)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				_currentDate = date;
				RefreshGrid();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private int DayCount
		{
			get { return (int)(_finishDate - _startDate).TotalDays + 1; }
		}

		#endregion

		#region Построение сетки ------------------------------

		private void LoadModules()
		{
			if (_modules == null)
				_modules = ComboModule.LoadContent(_comboModuleID);
		}

		private void CreateColumns()
		{
			RawDataGridView.Columns.Clear();

			AddColumn(COLUMN_MASSMEDIA, "Радиостанция");
			AddColumn(COLUMN_MODULE, "Модуль");

			for (int i = 0; i < DayCount; i++)
			{
				DateTime date = _startDate.AddDays(i);
				AddColumn(COLUMN_DAY_PREFIX + i,
					DateTimeUtils.ResolveWeekDayName(date.DayOfWeek, DateTimeUtils.WeekDayNameFormat.Short));
			}
		}

		private void AddColumn(string columnName, string headerText)
		{
			DataGridViewColumn column = new DataGridViewTextBoxColumn
			{
				DataPropertyName = columnName,
				HeaderText = headerText,
				SortMode = DataGridViewColumnSortMode.NotSortable
			};
			RawDataGridView.Columns.Add(column);
		}

		private void CreateRows()
		{
			_dtGrid = new DataSet().Tables.Add();
			_dtGrid.Columns.Add(COLUMN_MASSMEDIA);
			_dtGrid.Columns.Add(COLUMN_MODULE);
			for (int i = 0; i < DayCount; i++)
				_dtGrid.Columns.Add(COLUMN_DAY_PREFIX + i);

			// строка дат
			DataRow row = _dtGrid.NewRow();
			for (int i = 0; i < DayCount; i++)
				row[FIXED_COLS + i] = _startDate.AddDays(i).ToString("dd.MM");
			_dtGrid.Rows.Add(row);

			// строка количества выпусков - заполняется формой размещения
			row = _dtGrid.NewRow();
			for (int i = 0; i < DayCount; i++)
				row[FIXED_COLS + i] = 0;
			_dtGrid.Rows.Add(row);

			foreach (DataRow moduleRow in _modules.Rows)
			{
				row = _dtGrid.NewRow();
				row[COLUMN_MASSMEDIA] = moduleRow["massmediaName"];
				row[COLUMN_MODULE] = moduleRow["moduleName"];
				_dtGrid.Rows.Add(row);
			}

			_days = new ComboModuleDay[_modules.Rows.Count, DayCount];
		}

		private void FillFreeTime()
		{
			DataTable freeTime = ComboModule.LoadFreeTime(_comboModuleID, _startDate, _finishDate, _showUnconfirmed);

			Dictionary<string, DataRow> byModuleAndDay = new Dictionary<string, DataRow>();
			foreach (DataRow row in freeTime.Rows)
				byModuleAndDay[MakeKey((int)Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]),
									   Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]))] = row;

			for (int moduleIndex = 0; moduleIndex < _modules.Rows.Count; moduleIndex++)
			{
				DataRow moduleRow = _modules.Rows[moduleIndex];
				int moduleID = Convert.ToInt32(moduleRow[ComboModule.ParamNames.ModuleId]);

				for (int dayIndex = 0; dayIndex < DayCount; dayIndex++)
				{
					DateTime date = _startDate.AddDays(dayIndex);
					DataRow row;
					if (!byModuleAndDay.TryGetValue(MakeKey(moduleID, date), out row))
						continue;

					ComboModuleDay day = new ComboModuleDay
					{
						ModuleID = moduleID,
						MassmediaID = Convert.ToInt32(moduleRow["massmediaID"]),
						MassmediaName = moduleRow["massmediaName"].ToString(),
						ModuleName = moduleRow["moduleName"].ToString(),
						ModulePriceListID = Convert.ToInt32(row[ComboModule.ParamNames.ModulePriceListId]),
						Price = Convert.ToDecimal(row[ComboModule.ParamNames.Price]),
						Date = date,
						FreeTime = GetNullableInt(row, ComboModule.ParamNames.FreeTime),
						FreeCapacity = GetNullableInt(row, ComboModule.ParamNames.FreeCapacity),
						MaxCapacity = GetNullableInt(row, ComboModule.ParamNames.MaxCapacity)
					};

					_days[moduleIndex, dayIndex] = day;
					_dtGrid.Rows[FIXED_ROWS + moduleIndex][FIXED_COLS + dayIndex] = day.CellText;
				}
			}
		}

		private static int? GetNullableInt(DataRow row, string columnName)
		{
			return row[columnName] == DBNull.Value ? (int?)null : Convert.ToInt32(row[columnName]);
		}

		private static string MakeKey(int moduleID, DateTime date)
		{
			return string.Format("{0}|{1:yyyyMMdd}", moduleID, date);
		}

		private void SetFrozenRowsAndColumns()
		{
			if (RawDataGridView.RowCount == 0) return;

			for (int i = 0; i < FIXED_ROWS && i < RawDataGridView.RowCount; i++)
				RawDataGridView.Rows[i].Frozen = true;

			for (int i = 0; i < FIXED_COLS; i++)
				RawDataGridView.Columns[i].Frozen = true;

			for (int row = 0; row < FIXED_ROWS && row < RawDataGridView.RowCount; row++)
				for (int col = 0; col < RawDataGridView.Columns.Count; col++)
					CopyColumnHeaderCellStyle(GetCell(row, col), DataGridViewContentAlignment.MiddleCenter);

			for (int row = 0; row < RawDataGridView.RowCount; row++)
				for (int col = 0; col < FIXED_COLS; col++)
					CopyColumnHeaderCellStyle(GetCell(row, col), DataGridViewContentAlignment.MiddleLeft);
		}

		/// <summary>Колонки дней делаем одинаковой ширины - по самой широкой из них.</summary>
		private void SetColumnWidths()
		{
			for (int i = 0; i < FIXED_COLS; i++)
				RawDataGridView.AutoResizeColumn(i);

			int dayWidth = 0;
			for (int i = FIXED_COLS; i < RawDataGridView.Columns.Count; i++)
			{
				RawDataGridView.AutoResizeColumn(i);
				dayWidth = Math.Max(dayWidth, RawDataGridView.Columns[i].Width);
			}

			for (int i = FIXED_COLS; i < RawDataGridView.Columns.Count; i++)
				RawDataGridView.Columns[i].Width = dayWidth;
		}

		private void CopyColumnHeaderCellStyle(DataGridViewCell cell, DataGridViewContentAlignment alignment)
		{
			cell.Style.BackColor = RawDataGridView.ColumnHeadersDefaultCellStyle.BackColor;
			cell.Style.SelectionBackColor = RawDataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor;
			cell.Style.SelectionForeColor = RawDataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor;
			cell.Style.Alignment = alignment;
		}

		private DataGridViewCell GetCell(int rowIndex, int columnIndex)
		{
			return RawDataGridView[columnIndex, rowIndex];
		}

		#endregion

		/// <summary>Число выпусков за день - строка под датами. Заполняет форма размещения.</summary>
		public void SetIssuesCount(DateTime date, int count)
		{
			int dayIndex = (int)(date.Date - _startDate).TotalDays;
			if (dayIndex < 0 || dayIndex >= DayCount) return;
			_dtGrid.Rows[ROW_ISSUES_COUNT][FIXED_COLS + dayIndex] = count;
		}

		private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
		{
			try
			{
				if (e.RowIndex < FIXED_ROWS || e.ColumnIndex < FIXED_COLS) return;

				ComboModuleDay day = _days[e.RowIndex - FIXED_ROWS, e.ColumnIndex - FIXED_COLS];
				if (day == null) return;   // модуля в этот день нет - клик ничего не делает

				if (CellClicked != null) CellClicked(day);
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
	}
}
