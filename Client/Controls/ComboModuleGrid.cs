using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
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
		/// <summary>Выбранная позиция свободна во всех окнах модуля - ячейка рисуется жирным.</summary>
		public bool PositionFree;
		/// <summary>Выбранный предмет рекламы (есть/нет) выполняется во всех окнах модуля.</summary>
		public bool AdvertTypeFree;

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
		private int _actionID;
		private DateTime _currentDate = DateTime.Today;
		private DateTime _startDate, _finishDate;
		private ComboModulePeriodMode _periodMode = ComboModulePeriodMode.Week;
		private bool _showUnconfirmed = true;

		private DataTable _modules;
		private DataTable _dtGrid;
		private ComboModuleDay[,] _days;
		private readonly Dictionary<int, int> _rowByModule = new Dictionary<int, int>();
		private bool _editMode;
		private RollerPositions _rollerPosition = RollerPositions.Undefined;
		private PresentationObject _advertType;
		private AdvertTypePresences _advertTypePresence = AdvertTypePresences.Undefined;

		// Режим "Номера роликов": в ячейке вместо остатка времени - номера роликов акции,
		// размещённых в этом модуле в этот день. Карту (rollerID -> номер в списке роликов)
		// строит форма и передаёт в SetRollerNumbersMode; выпуски берутся из последней
		// таблицы, переданной в MarkIssues.
		private bool _showRollerNumbers;
		private Dictionary<int, int> _rollerNumbers;
		private DataTable _markedIssues;

		#endregion

		public event ComboModuleDayDelegate CellClicked;

		/// <summary>
		/// Грид перестроен - в том числе при листании стрелками. Форма по этому событию
		/// заново раскрашивает ячейки с выпусками и заполняет счётчик по дням.
		/// </summary>
		public event EmptyDelegate GridRefreshed;

		public ComboModuleGrid()
		{
			InitializeComponent();
			Caption.Caption = string.Empty;   // у NavigationCaption по умолчанию текст про прайс-лист
			EditMode = false;                 // заодно красит шапку в цвет режима просмотра
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

		/// <summary>
		/// Акция, модули которой показывать, - режим редактирования готовой акции.
		/// Используется, когда комбо-модуль не задан.
		/// </summary>
		public int ActionID
		{
			get { return _actionID; }
			set
			{
				_actionID = value;
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

		/// <summary>
		/// Режим добавления выпусков. Пока выключен, клик по ячейке ничего не создаёт -
		/// так же ведёт себя тарифная сетка обычной кампании (EditMode.Edit).
		/// </summary>
		public bool EditMode
		{
			get { return _editMode; }
			set
			{
				_editMode = value;
				Cursor = _editMode ? Cursors.Hand : Cursors.Default;
				RawDataGridView.Cursor = Cursor;
				// выделение прямоугольником нужно для Del по окнам, но в режиме
				// добавления оно мешает кликать - так же поступает CampaignForm
				RawDataGridView.MultiSelect = !_editMode;
				// шапка синеет в режиме добавления - как у тарифной сетки обычной кампании
				Caption.CaptionBackColor = _editMode
					? Color.Blue
					: Color.FromName(KnownColor.GrayText.ToString());
			}
		}

		/// <summary>
		/// Выбранное позиционирование. Модули, у которых эта позиция свободна во всех окнах,
		/// грид рисует жирным - как тарифная сетка обычной кампании помечает свободные окна.
		/// </summary>
		public RollerPositions RollerPosition
		{
			get { return _rollerPosition; }
			set { _rollerPosition = value; }
		}

		/// <summary>
		/// Выбранный предмет рекламы. Модули, у которых во всех окнах выполняется условие
		/// «есть»/«нет» этого предмета, грид рисует жирным - тот же принцип, что и у
		/// позиционирования, оба условия комбинируются (нужно и то, и другое, если выбрано).
		/// </summary>
		public PresentationObject AdvertType
		{
			get { return _advertType; }
			set { _advertType = value; }
		}

		public AdvertTypePresences AdvertTypePresence
		{
			get { return _advertTypePresence; }
			set { _advertTypePresence = value; }
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
			StripeMassmediaGroups();
			ShadeWeekends();
			MarkFilteredCells();
			SetColumnWidths();
			SetNavigationCaption();

			if (GridRefreshed != null) GridRefreshed();
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

		/// <summary>Тот же переход, что по стрелке "вперёд" в подписи над сеткой -
		/// нужен снаружи для горячей клавиши (PageDown) на ComboModulePlacementForm.</summary>
		public void GoToNextPeriod()
		{
			Caption_GoNext();
		}

		/// <summary>Тот же переход, что по стрелке "назад" в подписи над сеткой -
		/// нужен снаружи для горячей клавиши (PageUp) на ComboModulePlacementForm.</summary>
		public void GoToPreviousPeriod()
		{
			Caption_GoPrevious();
		}

		/// <summary>
		/// Переход к выбранной дате - тот же диалог, что и у тарифной сетки кампании.
		/// Грид после этого надо обновить: делает форма, чтобы заодно перечитать выпуски.
		/// </summary>
		public bool SelectDate2Jump()
		{
			FrmDateSelector fSelector = new FrmDateSelector("Выбор даты") { Mode = FrmDateSelector.SelectorMode.SelectOne };
			if (fSelector.ShowDialog(this) != DialogResult.OK) return false;

			_currentDate = fSelector.StartDate;
			return true;
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

		// Список модулей перечитываем на каждое обновление, а не кэшируем: в режиме готовой
		// акции он выводится из выпусков, и после удаления последнего выпуска модуля строка
		// должна пропасть. Запрос дешёвый - модулей единицы.
		private void LoadModules()
		{
			_modules = _comboModuleID > 0
				? ComboModule.LoadContent(_comboModuleID)
				: ComboModule.LoadActionModules(_actionID);
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
				row[FIXED_COLS + i] = _startDate.AddDays(i).ToString("dd.MM.yy");
			_dtGrid.Rows.Add(row);

			// строка количества выпусков - заполняется формой размещения
			row = _dtGrid.NewRow();
			for (int i = 0; i < DayCount; i++)
				row[FIXED_COLS + i] = 0;
			_dtGrid.Rows.Add(row);

			_rowByModule.Clear();
			for (int moduleIndex = 0; moduleIndex < _modules.Rows.Count; moduleIndex++)
			{
				DataRow moduleRow = _modules.Rows[moduleIndex];
				row = _dtGrid.NewRow();
				row[COLUMN_MASSMEDIA] = moduleRow[ComboModule.ParamNames.MassmediaName];
				row[COLUMN_MODULE] = moduleRow[ComboModule.ParamNames.ModuleName];
				_dtGrid.Rows.Add(row);
				_rowByModule[Convert.ToInt32(moduleRow[ComboModule.ParamNames.ModuleId])] = moduleIndex;
			}

			_days = new ComboModuleDay[_modules.Rows.Count, DayCount];
		}

		private void FillFreeTime()
		{
			DataTable freeTime = ComboModule.LoadFreeTime(
				_comboModuleID, _actionID, _startDate, _finishDate, _showUnconfirmed, _rollerPosition,
				_advertType, _advertTypePresence);

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
						MassmediaID = Convert.ToInt32(moduleRow[ComboModule.ParamNames.MassmediaId]),
						MassmediaName = moduleRow[ComboModule.ParamNames.MassmediaName].ToString(),
						ModuleName = moduleRow[ComboModule.ParamNames.ModuleName].ToString(),
						ModulePriceListID = Convert.ToInt32(row[ComboModule.ParamNames.ModulePriceListId]),
						Price = Convert.ToDecimal(row[ComboModule.ParamNames.Price]),
						Date = date,
						FreeTime = GetNullableInt(row, ComboModule.ParamNames.FreeTime),
						FreeCapacity = GetNullableInt(row, ComboModule.ParamNames.FreeCapacity),
						MaxCapacity = GetNullableInt(row, ComboModule.ParamNames.MaxCapacity),
						PositionFree = Convert.ToInt32(row[ComboModule.ParamNames.PositionFree]) == 1,
						AdvertTypeFree = Convert.ToInt32(row[ComboModule.ParamNames.AdvertTypeFree]) == 1
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

		/// <summary>
		/// Жирным - модули, у которых во всех окнах выполняются выбранные фильтры
		/// (позиционирование и/или предмет рекламы). Если выбрано и то, и другое - нужны оба;
		/// если не выбрано ничего - не помечаем, как и тарифная сетка обычной кампании
		/// (RollerIssuesGrid3.MarkCell).
		/// </summary>
		private void MarkFilteredCells()
		{
			bool positionSelected = _rollerPosition != RollerPositions.Undefined;
			bool advertTypeSelected = _advertTypePresence != AdvertTypePresences.Undefined;

			for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.RowCount; rowIndex++)
				for (int columnIndex = FIXED_COLS; columnIndex < RawDataGridView.Columns.Count; columnIndex++)
				{
					ComboModuleDay day = _days[rowIndex - FIXED_ROWS, columnIndex - FIXED_COLS];
					bool bold = (positionSelected || advertTypeSelected) && day != null
						&& (!positionSelected || day.PositionFree)
						&& (!advertTypeSelected || day.AdvertTypeFree);

					GetCell(rowIndex, columnIndex).Style.Font = bold
						? new Font(RawDataGridView.DefaultCellStyle.Font, FontStyle.Bold)
						: RawDataGridView.DefaultCellStyle.Font;
				}
		}

		/// <summary>
		/// "Зебра" по радиостанциям: колонки "Радиостанция" и "Модуль" заливаются
		/// чередующимся фоном - все строки одной станции одним цветом, следующая станция
		/// другим. У комбо-модулей на проде десятки модулей подряд, и без этого не видно,
		/// где кончается блок одной станции и начинается следующий. Модули идут группами
		/// по станции (ComboModuleContentRetrieve / ...ActionModulesRetrieve), поэтому
		/// достаточно менять цвет при смене massmediaID у соседних строк.
		/// </summary>
		private void StripeMassmediaGroups()
		{
			if (RawDataGridView.RowCount == 0 || _modules == null) return;

			int lastMassmediaID = -1;
			bool useAlt = false;
			for (int moduleIndex = 0; moduleIndex < _modules.Rows.Count; moduleIndex++)
			{
				int massmediaID = Convert.ToInt32(_modules.Rows[moduleIndex][ComboModule.ParamNames.MassmediaId]);
				if (massmediaID != lastMassmediaID)
				{
					useAlt = !useAlt;
					lastMassmediaID = massmediaID;
				}

				Color color = useAlt ? Color.WhiteSmoke : Color.Gainsboro;
				int rowIndex = FIXED_ROWS + moduleIndex;
				for (int columnIndex = 0; columnIndex < FIXED_COLS; columnIndex++)
					SetCellBackColor(rowIndex, columnIndex, color);
			}
		}

		/// <summary>
		/// Бледно-серым - колонки субботы и воскресенья, но только в месячном режиме: в
		/// недельном все семь дней и так видны целиком, выделять нечего.
		/// </summary>
		private void ShadeWeekends()
		{
			if (RawDataGridView.RowCount == 0) return;

			if (_periodMode != ComboModulePeriodMode.Month) return;   // в неделе и так все дни на виду

			for (int dayIndex = 0; dayIndex < DayCount; dayIndex++)
			{
				DayOfWeek dayOfWeek = _startDate.AddDays(dayIndex).DayOfWeek;
				if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday) continue;

				// колонки только что созданы заново (CreateColumns/CreateRows), поэтому
				// сбрасывать окраску будних дней не нужно - у них и так цвет по умолчанию
				for (int rowIndex = 0; rowIndex < RawDataGridView.RowCount; rowIndex++)
					SetCellBackColor(rowIndex, FIXED_COLS + dayIndex, Color.Gainsboro);
			}
		}

		// SelectionBackColor намеренно не трогаем - как в TariffGrid.SetCellBackColor: иначе
		// выделенная ячейка теряла бы обычную синюю подсветку выбора.
		private void SetCellBackColor(int rowIndex, int columnIndex, Color color)
		{
			GetCell(rowIndex, columnIndex).Style.BackColor = color;
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

		/// <summary>
		/// Синим отмечаются ячейки, где у текущей акции уже есть выпуски, - тем же цветом,
		/// что и в тарифной сетке обычной кампании. Таблица выпусков приходит из
		/// ComboModuleIssuesRetrieve; null - отмечать нечего.
		/// </summary>
		public void MarkIssues(DataTable issues)
		{
			_markedIssues = issues;
			ClearIssueMarks();

			if (issues != null)
				foreach (DataRow row in issues.Rows)
				{
					int moduleIndex;
					if (!_rowByModule.TryGetValue(
							Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]), out moduleIndex))
						continue;

					int dayIndex = (int)(Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]).Date - _startDate).TotalDays;
					if (dayIndex < 0 || dayIndex >= DayCount) continue;

					SetCellForeColor(FIXED_ROWS + moduleIndex, FIXED_COLS + dayIndex, Color.Blue);
				}

			// выпуски только что перечитаны - обновляем тексты ячеек (в режиме "Номера
			// роликов" они зависят от состава выпусков, поэтому за каждым MarkIssues)
			ApplyCellTexts();
		}

		/// <summary>
		/// Режим ячейки: остаток времени (<see cref="ComboModuleDay.CellText"/>) ↔ номера
		/// роликов акции, размещённых в модуле. Карту номеров (rollerID → номер строки в
		/// списке роликов) строит форма; сам грид её не пересчитывает. Тексты
		/// перерисовываются сразу, без похода в БД.
		/// </summary>
		public void SetRollerNumbersMode(bool on, Dictionary<int, int> rollerNumbers)
		{
			_showRollerNumbers = on;
			_rollerNumbers = rollerNumbers;
			ApplyCellTexts();
		}

		// Заполняет тексты ячеек модулей: обычно остаток времени, а в режиме "Номера
		// роликов" - номера роликов из этого модуля в этот день (через запятую).
		private void ApplyCellTexts()
		{
			if (_dtGrid == null || _days == null) return;

			for (int moduleIndex = 0; moduleIndex < _days.GetLength(0); moduleIndex++)
				for (int dayIndex = 0; dayIndex < _days.GetLength(1); dayIndex++)
				{
					ComboModuleDay day = _days[moduleIndex, dayIndex];
					if (day == null) continue;

					string text = day.CellText;
					if (_showRollerNumbers)
					{
						string numbers = GetRollerNumbersText(day.ModuleID, day.Date);
						if (numbers != null) text = numbers;
					}

					_dtGrid.Rows[FIXED_ROWS + moduleIndex][FIXED_COLS + dayIndex] = text;
				}
		}

		// Номера роликов акции в этом модуле за этот день (через запятую, в порядке из
		// таблицы выпусков), или null - выпусков нет / номер ролика не известен.
		private string GetRollerNumbersText(int moduleID, DateTime date)
		{
			if (_markedIssues == null || _rollerNumbers == null) return null;

			List<string> numbers = new List<string>();
			foreach (DataRow row in _markedIssues.Rows)
			{
				if (Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]) != moduleID) continue;
				if (Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]).Date != date.Date) continue;

				int rollerId = ParseHelper.GetInt32FromObject(row[Roller.ParamNames.RollerId], 0);
				if (_rollerNumbers.TryGetValue(rollerId, out int number))
					numbers.Add(number.ToString());
			}
			return numbers.Count > 0 ? string.Join(", ", numbers) : null;
		}

		private void ClearIssueMarks()
		{
			for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.RowCount; rowIndex++)
				for (int columnIndex = FIXED_COLS; columnIndex < RawDataGridView.Columns.Count; columnIndex++)
					SetCellForeColor(rowIndex, columnIndex, RawDataGridView.DefaultCellStyle.ForeColor);
		}

		private void SetCellForeColor(int rowIndex, int columnIndex, Color color)
		{
			DataGridViewCell cell = GetCell(rowIndex, columnIndex);
			cell.Style.ForeColor = cell.Style.SelectionForeColor = color;
		}

		/// <summary>
		/// Модули и дни выделенных ячеек - для массового удаления выпусков по Del.
		/// Аналог TariffGrid.GetSelectedTariffWindows.
		/// </summary>
		public IList<ComboModuleDay> GetSelectedDays()
		{
			List<ComboModuleDay> days = new List<ComboModuleDay>();
			foreach (DataGridViewCell cell in RawDataGridView.SelectedCells)
			{
				if (cell.RowIndex < FIXED_ROWS || cell.ColumnIndex < FIXED_COLS) continue;

				ComboModuleDay day = _days[cell.RowIndex - FIXED_ROWS, cell.ColumnIndex - FIXED_COLS];
				if (day != null) days.Add(day);
			}
			return days;
		}

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
				if (!_editMode) return;   // режим добавления выключен
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
