using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;
using Merlin.Classes;

namespace Merlin.Controls
{
	internal partial class RollerIssuesGrid3 : TariffWindowGrid, IRollerGrid
	{
        private static readonly ILog Log = LogManager.GetLogger(typeof(RollerIssuesGrid3));

		private Roller roller;
		private DataTable _dtWindowsWithAdvertType;
		private bool showRollerNumbers;
		private Dictionary<int, int> rollerNumbers;

        public RollerIssuesGrid3()
			: this(true)
		{
		}

		public RollerIssuesGrid3(bool initdelegates)
		{
			InitializeComponent();
			if (initdelegates)
				InitializeDelegates();
			excludeModuleTariffs = true;
		}

        protected override void InitializePopupMenus()
        {
        }

        // Актуальное время — только для линейной расстановки роликов (module == null).
        // Модульные кампании (module != null) остаются на оригинальном времени — вне scope.
        // TrafficGrid (наследник) переопределяет это в false: его операции (перенос/длительность/
        // удаление окон) ищут в SQL по windowDateOriginal и должны видеть оригинальную подпись.
        protected override bool UseActualTime => module == null;

        [Browsable(false)]
		public Roller Roller
		{
			get { return roller; }
			set { roller = value; }
		}

        // Режим ячейки: вместо остатка свободного времени — номера роликов текущей кампании
        // (см. RollerNumbers), размещённых в этом окне. Карта номеров фиксируется снаружи
        // (CampaignForm) на момент включения режима, сама повторно не пересчитывается.
        [Browsable(false)]
		public bool ShowRollerNumbers
		{
			get { return showRollerNumbers; }
			set { showRollerNumbers = value; }
		}

        // rollerID -> номер ролика (как в колонке "№" grdRollers на момент включения ShowRollerNumbers)
        [Browsable(false)]
		public Dictionary<int, int> RollerNumbers
		{
			get { return rollerNumbers; }
			set { rollerNumbers = value; }
		}

        public override Entity IssueEntity
		{
			get { return module == null ? RollerIssue.GetEntity() : ModuleIssue.GetEntity(); }
		}

		protected override void SetNavigationCaption()
		{
			if (module == null || pricelist == null)
				base.SetNavigationCaption();
			else if (module != null)
			{
				Caption.Caption = string.Format("'{0}' Прайс-лист: {1} - {2}",
				                                module.Name,
				                                pricelist.StartDate.ToShortDateString(),
				                                pricelist.FinishDate.ToShortDateString());
			}
		}

		private void InitializeDelegates()
		{
			loadPricelist = LoadPricelist;
			updateDB = UpdateDB;
			onGridPopulated += OnGridPopulated;
		}

		protected void LoadPricelist()
		{
			if (_massmedia == null) return;

			if (module == null)
				pricelist = _massmedia.GetPriceList(_currentDate);
			else
				pricelist = module.GetPriceList(_currentDate);

			if (pricelist != null)
			{
				if (startDate < pricelist.StartDate)
					startDate = pricelist.StartDate;

				if (finishDate > pricelist.FinishDate)
					finishDate = pricelist.FinishDate;

				PricelistOnMassmedia.ExcludeSpecialWindows = excludeSpecialTariffs;
			}
		}

		private void UpdateDB(DataGridViewCell cell)
		{
			if (campaign == null) return;
			using (OperationScope.Start($"UpdateDB cell={cell.RowIndex},{cell.ColumnIndex}"))
			{

            if (!(GetTariffWindow(cell) is TariffWindowWithRollerIssues tariffWindow)) return;

            if (module == null)
				AddIssue(cell, tariffWindow);
			else
			{
				if (!IsFullColumn(cell.ColumnIndex))
					return;

				AddModuleIssue(cell, tariffWindow);
			}

            campaign.Action.Refresh();

			Refresh();
			FireCampaignStatusChanged();
		}
		}

		private void AddModuleIssue(DataGridViewCell cell, TariffWindowWithRollerIssues tariffWindow)
		{
            try
            {
                DataAccessor.BeginTransaction();
                CampaignOnSingleMassmedia.AddModuleIssue(module, roller,
									(ModulePricelist)pricelist, tariffWindow.WindowDateBroadcast, rollerPosition, Grantor == null ? null : (int?)Grantor.Id);
				CampaignOnSingleMassmedia.RecalculateAction(false);
                DataAccessor.CommitTransaction();
            }
            catch
            {
                DataAccessor.RollbackTransaction();
                throw;
            }

            for (int i = 0; i < dtTime.Rows.Count; i++)
			{
				tariffWindow = (TariffWindowWithRollerIssues) GetTariffWindow(i + FIXED_ROWS, cell.ColumnIndex);
				RefreshSingleCell(i + FIXED_ROWS, cell.ColumnIndex, tariffWindow, TariffGridRefreshMode.WithAdd, true);
			}
		}

		private void AddIssue(DataGridViewCell cell, TariffWindowWithRollerIssues tariffWindow)
		{
            using (OperationScope.Start($"AddIssue date={tariffWindow.WindowDate:yyyy-MM-dd HH:mm}"))
            {
                try
                {
                    DataAccessor.BeginTransaction();
                    CampaignOnSingleMassmedia.AddIssue(roller, tariffWindow, rollerPosition, Grantor == null ? null : (int?)Grantor.Id);
                    CampaignOnSingleMassmedia.RecalculateAction(false);
                    DataAccessor.CommitTransaction();
                }
                catch
                {
                    DataAccessor.RollbackTransaction();
                    throw;
                }

                RefreshSingleCell(cell.RowIndex, cell.ColumnIndex, tariffWindow, TariffGridRefreshMode.WithAdd, true);
            }
		}

		private void OnGridPopulated()
		{
			AddIssues2Grid();
			if (module != null) 
				MarkFullColumns();
			else
                RefreshWindowsColors();

            if (rollerPosition != RollerPositions.Undefined || _advertTypePresence != AdvertTypePresences.Undefined)
                MarkCellsWithPositionAndAdvType();

            if (showRollerNumbers)
                RefreshCellTexts();
		}

        private void MarkCellsWithPositionAndAdvType()
		{
			if (_advertTypePresence != AdvertTypePresences.Undefined)
				LoadTariffWindowsWithAdvertType();

			for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.RowCount; rowIndex++)
				for (int columnIndex = FixedCols; columnIndex < RawDataGridView.ColumnCount; columnIndex++)
                {
                    if (GetTariffWindow(rowIndex, columnIndex) is TariffWindowWithRollerIssues window)
                        MarkCell(window, rowIndex, columnIndex);
                }
        }

        private void MarkCell(TariffWindowWithRollerIssues window, int rowIndex, int columnIndex)
        {
            bool markFlag = false;
			

			if (rollerPosition != RollerPositions.Undefined)
			{
				if (!ShowUnconfirmed)
					markFlag = IsConfirmedPositionNotOccupied(window);
				else
					markFlag = IsConfirmedPositionNotOccupied(window) && IsUnconfirmedPositionsNotOccupied(window);
			}
			if (!(_advertTypePresence == AdvertTypePresences.Undefined || (rollerPosition != RollerPositions.Undefined && !markFlag)))
			{
				bool advertTypeFound = _dtWindowsWithAdvertType.Select("windowID = " + window.WindowId).Length > 0;
				markFlag = (advertTypeFound && _advertTypePresence == AdvertTypePresences.Exist) || (!advertTypeFound && _advertTypePresence == AdvertTypePresences.NotExist);
			}

			if (markFlag)
				MarkCellAsNotOccupied(rowIndex, columnIndex);            
			else
                MarkCellAsOccupied(rowIndex, columnIndex);
        }

        private bool IsUnconfirmedPositionsNotOccupied(TariffWindowWithRollerIssues window)
		{
			return (rollerPosition == RollerPositions.First && window.FirstPositionsUnconfirmed == 0) ||
			       (rollerPosition == RollerPositions.Second && window.SecondPositionsUnconfirmed == 0) ||
			       (rollerPosition == RollerPositions.Last && window.LastPositionsUnconfirmed == 0);
		}

		private bool IsConfirmedPositionNotOccupied(TariffWindowWithRollerIssues window)
		{
			return (rollerPosition == RollerPositions.First && !window.IsFirstPositionOccupied) ||
			       (rollerPosition == RollerPositions.Second && !window.IsSecondPositionOccupied) ||
			       (rollerPosition == RollerPositions.Last && !window.IsLastPositionOccupied);
		}

		private void MarkFullColumns()
		{
			// if this week hasn't windows
			if (RawDataGridView.RowCount > FIXED_ROWS)
			{
				for (int columnIndex = FixedCols; columnIndex < RawDataGridView.ColumnCount; columnIndex++)
				{
					if (IsFullColumn(columnIndex) &&
					    Campaign.IsModuleExists((ModulePricelist) pricelist, GetTariffWindow(FIXED_ROWS, columnIndex).WindowDate))
						MarkColumnCells(columnIndex, Color.PeachPuff);
				}
			}
		}

		private bool IsFullColumn(int columnIndex)
		{
			for (int rowIndex = 0; rowIndex < RawDataGridView.RowCount; rowIndex++)
				if (StringUtil.IsDBNullOrEmpty(GetCell(rowIndex, columnIndex).Value))
					return false;
			return true;
		}

		private void AddIssues2Grid()
		{
			DataSet ds = _massmedia.GetRollerCells(pricelist,
			                                      startDate, finishDate, module,
												  ShowUnconfirmed,
			                                      campaign, rollerPosition);
			_dtIssue = ds.Tables[Constants.TableNames.Data];
            _dtWindoesWithCurrentFirmIssue = ds.Tables[Constants.TableNames.WindowsWithThisFirmIssue];
            foreach (DataRow row in _dtWindoesWithCurrentFirmIssue.Rows)
            {
                DataGridViewCell cell =
                    GetCell(TariffWindowWithRollerIssues.CreateTariffWindowById(
                                int.Parse(row[TariffWindow.ParamNames.WindowId].ToString())));
                if (cell != null)
                    MarkCellAsHavingCurrentFirmIssues(cell);
            }

            foreach (DataRow row in _dtIssue.Rows)
			{
				DataGridViewCell cell =
					GetCell(TariffWindowWithRollerIssues.CreateTariffWindowById(
								int.Parse(row[TariffWindow.ParamNames.OriginalWindowId].ToString())));
				if (cell != null)
					MarkCellAsHavingCurrentCampaignIssues(cell);
			}

            DataTable dtCounts = ds.Tables[1];
			foreach (DataRow row in dtCounts.Rows)
			{
				ChangeIssuesCounter(ParseHelper.GetInt32FromObject(row["weekday"], -1) + 1, ParseHelper.GetInt32FromObject(row["count"], 0));
			}
		}

		protected override string GetCellContent(DataRow row)
		{
			PresentationObject obj = new PresentationObject((int)Entities.TariffWindow, row);
			return GetCellContent(obj);
		}

		private string GetCellContent(PresentationObject obj)
		{
			if (showRollerNumbers)
			{
				string rollerNumbersText = GetRollerNumbersText(obj);
				if (rollerNumbersText != null)
					return rollerNumbersText;
			}

			DateTime dtOriginal = (DateTime)obj[ColumnNames.WindowDateOriginal];
			DateTime dtActual = (DateTime)obj[ColumnNames.WindowDateActual];
			dtOriginal = dtOriginal.Date.AddHours(dtOriginal.Hour).AddMinutes(dtOriginal.Minute);
			dtActual = dtActual.Date.AddHours(dtActual.Hour).AddMinutes(dtActual.Minute);
			string postfix = (DateTime.Compare((DateTime)obj[ColumnNames.WindowDateActualBroadcast], (DateTime)obj[ColumnNames.WindowDateBroadcast]) != 0) ? string.Format(" ({0})", dtActual.ToString("dd.MM.yy HH:mm"))
				: (DateTime.Compare(dtOriginal, dtActual) != 0) ? string.Format(" ({0})", dtActual.ToString("HH:mm"))
				: string.Empty;

			int timeLeft = int.Parse(obj[Tariff.ParamNames.Duration].ToString()) -
			               int.Parse(obj[TariffWindow.ParamNames.TimeInUseConfirmed].ToString());
			if (ShowUnconfirmed)
				timeLeft -= int.Parse(obj[TariffWindow.ParamNames.TimeInUseUnconfirmed].ToString());

			string timeString = DateTimeUtils.Time2String(timeLeft);
			if (obj[TariffWindowWithRollerIssues.ParamNames.MaxCapacity] == null || obj[TariffWindowWithRollerIssues.ParamNames.MaxCapacity].ToString() == "0")
				return string.Format("{0}{1}", timeString, showTrafficWindows ? postfix : string.Empty); 
			int maxCapacity = int.Parse(obj[TariffWindowWithRollerIssues.ParamNames.MaxCapacity].ToString());
			int capacityLeft = maxCapacity - int.Parse(obj[TariffWindowWithRollerIssues.ParamNames.CapacityInUseConfirmed].ToString());
			if (ShowUnconfirmed)
				capacityLeft -= int.Parse(obj[TariffWindowWithRollerIssues.ParamNames.CapacityInUseUnconfirmed].ToString());
			return string.Format("{0} [{1}/{2}]{3}", timeString, capacityLeft, maxCapacity, showTrafficWindows ? postfix : string.Empty);
		}

		// Номера роликов текущей кампании в этом окне (через запятую, в порядке размещения),
		// или null, если у окна нет выпусков текущей кампании / номер ролика не известен.
		private string GetRollerNumbersText(PresentationObject obj)
		{
			if (_dtIssue == null || rollerNumbers == null) return null;

			int windowId = int.Parse(obj[TariffWindow.ParamNames.WindowId].ToString());
			DataRow[] issueRows = _dtIssue.Select(TariffWindow.ParamNames.OriginalWindowId + " = " + windowId);
			if (issueRows.Length == 0) return null;

			List<string> numbers = new List<string>();
			foreach (DataRow issueRow in issueRows)
			{
				int rollerId = ParseHelper.GetInt32FromObject(issueRow[TariffWindow.ParamNames.RollerID], 0);
				if (rollerNumbers.TryGetValue(rollerId, out int number))
					numbers.Add(number.ToString());
			}
			return numbers.Count > 0 ? string.Join(", ", numbers) : null;
		}

		// Ручное добавление выпуска (не через шаблон/генератор) не перестраивает _dtIssue
		// целиком — без этой правки только что добавленный ролик не появлялся бы в номерах
		// ячейки (GetRollerNumbersText) до следующей полной перезагрузки грида.
		private void AddIssueToCache(int windowId)
		{
			if (_dtIssue == null || roller == null) return;

			DataRow row = _dtIssue.NewRow();
			row[TariffWindow.ParamNames.OriginalWindowId] = windowId;
			row[TariffWindow.ParamNames.RollerID] = roller.RollerId;
			_dtIssue.Rows.Add(row);
		}

		// Симметрично AddIssueToCache — ручное удаление одного выпуска (не через шаблон)
		// тоже не перестраивает _dtIssue целиком; без этого удалённый номер держался бы
		// в ячейке до следующей полной перезагрузки грида. Вызывается снаружи (CampaignForm),
		// так как только там известно, какой именно выпуск/ролик был удалён.
		public void RemoveIssueFromCache(int windowId, int rollerId)
		{
			if (_dtIssue == null) return;

			DataRow[] rows = _dtIssue.Select(string.Format("{0} = {1} AND {2} = {3}",
				TariffWindow.ParamNames.OriginalWindowId, windowId,
				TariffWindow.ParamNames.RollerID, rollerId));
			if (rows.Length > 0)
				_dtIssue.Rows.Remove(rows[0]);
		}

		// Перерисовать текст всех ячеек грида (без похода в БД) — нужно при включении/
		// выключении ShowRollerNumbers и при перезагрузке недели, пока режим включён.
		public void RefreshCellTexts()
		{
			int rowCount = _tariffWindows.GetLength(0);
			int columnCount = _tariffWindows.GetLength(1);

			for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
				for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
					if (_tariffWindows[rowIndex, columnIndex] is TariffWindowWithRollerIssues window)
						UpdateGridCell(rowIndex + FIXED_ROWS, columnIndex + FixedCols, window);
		}

		protected void UpdateGridCell(int rowIndex, int columnIndex, TariffWindowWithRollerIssues tariffWindow)
		{
			dtGrid.Rows[rowIndex][columnIndex] = GetCellContent(tariffWindow);//DateTimeUtils.Time2String(tariffWindow.GetFreeTime(showUnconfirmed));
		}

		protected void UpdateGridCell(DataGridViewCell cell, TariffWindowWithRollerIssues tariffWindow)
		{
			UpdateGridCell(cell.RowIndex, cell.ColumnIndex, tariffWindow);
		}

		public void RefreshCurrentCell(bool hasCurrentCampaignIssues, TariffGridRefreshMode mode)
		{
			if(module == null)
				RefreshSingleCell(CurrentCell.RowIndex, CurrentCell.ColumnIndex,
				                  (TariffWindowWithRollerIssues)CurrentTariffWindow, mode,
				                  hasCurrentCampaignIssues);
			else
			{
				for(int i = 0; i < dtTime.Rows.Count; i++)
				{
					TariffWindowWithRollerIssues tariffWindow =
						(TariffWindowWithRollerIssues) GetTariffWindow(i + FIXED_ROWS, CurrentCell.ColumnIndex);
					RefreshSingleCell(i + FIXED_ROWS, CurrentCell.ColumnIndex, tariffWindow, mode, hasCurrentCampaignIssues);
				}				
			}
        }

        private void RefreshSingleCell(int rowIndex, int columnIndex, TariffWindowWithRollerIssues tariffWindow,
            TariffGridRefreshMode mode, bool hasCurrentCampaignIssues)
		{
			tariffWindow.Refresh();

			if (mode == TariffGridRefreshMode.WithAdd && hasCurrentCampaignIssues)
				AddIssueToCache(tariffWindow.WindowId);

			UpdateGridCell(rowIndex, columnIndex, tariffWindow);
			if(mode == TariffGridRefreshMode.WithAdd)
				ChangeIssuesCounter(columnIndex, 1);
			else if(mode == TariffGridRefreshMode.WithDelete)
                ChangeIssuesCounter(columnIndex, -1);

            if (hasCurrentCampaignIssues)
				MarkCellAsHavingCurrentCampaignIssues(rowIndex, columnIndex);
			else
				MarkCellAsNotHavingCurrentCampaignIssues(rowIndex, columnIndex);
			
			if(mode == TariffGridRefreshMode.WithAdd && rollerPosition != RollerPositions.Undefined)
				MarkCellAsOccupied(rowIndex, columnIndex);
			
			if (mode == TariffGridRefreshMode.WithAdd && (_advertTypePresence != AdvertTypePresences.Undefined || rollerPosition != RollerPositions.Undefined))
			{
				if (_advertTypePresence != AdvertTypePresences.Undefined)
					LoadTariffWindowsWithAdvertType(tariffWindow);
                MarkCell(tariffWindow, rowIndex, columnIndex);
			}
		}

		private void LoadTariffWindowsWithAdvertType(TariffWindowWithRollerIssues tariffWindow = null)
		{
            MassmediaPricelist massmediaPriceList = PricelistOnMassmedia;
            //massmediaPriceList.ExcludeModuleTariffs = excludeModuleTariffs;

            DataSet dsWindows = massmediaPriceList.GetTariffWindowsWithAdvertType(startDate, finishDate, _advertType, ShowUnconfirmed, tariffWindow);
            _dtWindowsWithAdvertType = dsWindows.Tables[0];
        }

        #region IRollerGrid Members

        public SecurityManager.User Grantor { get; set; }

	    #endregion
	}
}