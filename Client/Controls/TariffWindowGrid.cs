using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Forms;

namespace Merlin.Controls
{
	internal partial class TariffWindowGrid : TariffGridWithIssuesOnSingleMassmedia, IObjectControl
	{
		protected DataTable dtTariffWindow;
		protected DataTable dtTime;
        protected DataTable dtTimeLookup; // таблица вида  Id, Name для показа в виде выпадающего списка
		protected Module module;
		protected bool excludeModuleTariffs = false;
        protected bool showTrafficWindows = false;
        protected Point mouseClickPoint;
        private readonly ToolStripMenuItem miChangePrice = new ToolStripMenuItem();
        private readonly ContextMenuStrip cmTimeColumn = new ContextMenuStrip();
		private const string NEW_PRICE = "newPrice";

        public TariffWindowGrid()
		{
			InitializeComponent();
			InitializeDelegates();
			InitializePopupMenus();
            RawDataGridView.MouseDown += delegate (object sender, MouseEventArgs e)
            {
                mouseClickPoint = new Point(e.X, e.Y);
            };

            dtTimeLookup = new DataTable("windowTime");
            dtTimeLookup.Columns.Add("id", typeof(int));
            dtTimeLookup.Columns.Add("name", typeof(string));
        }

        public TariffWindowGrid(bool showTrafficWindows)
            :this ()
        {
            this.showTrafficWindows = showTrafficWindows;
        }

		private DateTime CurrentWindowDate
		{
			get
			{
				string currentDateTime = string.Format("{0} {1}", dtGrid.Rows[ROW_DATE][RawDataGridView.CurrentCell.ColumnIndex], 
					GetNormalTimeString(dtGrid.Rows[RawDataGridView.CurrentCell.RowIndex][ColTime].ToString()));
				return DateTime.Parse(currentDateTime);
			}
		}

        protected int CurrentColumnIndex
        {
            get
            {
                DataGridView.HitTestInfo hti =
                    RawDataGridView.HitTest(mouseClickPoint.X, mouseClickPoint.Y);
                return hti.ColumnIndex;
            }
        }

        protected int CurrentRowIndex
        {
            get
            {
                DataGridView.HitTestInfo hti =
                    RawDataGridView.HitTest(mouseClickPoint.X, mouseClickPoint.Y);
                return hti.RowIndex;
            }
        }

        protected string GetNormalTimeString(string time)
		{
			string[] times = time.Split(':');
			int hour = ParseHelper.ParseToInt32(times[0]);
			int minute = ParseHelper.ParseToInt32(times[1]);
			return string.Format("{0}:{1}", hour >= 24 ? hour - 24 : hour, minute);
		}

		[Browsable(false)]
		public PresentationObject Module
		{
			get { return module; }
			set
			{
				excludeSpecialTariffs = false;
				module = (Module)value;
				if (module != null)
					_massmedia = module.Massmedia;
			}
		}

		[Browsable(false)]
		public Massmedia Massmedia
		{
			get { return _massmedia; }
			set { _massmedia = value; }
		}

        protected virtual void InitializePopupMenus()
		{
            miChangePrice.Text = "Изменить цену...";
            miChangePrice.Click += ChangePrice;
			cmTimeColumn.Items.AddRange(new ToolStripItem[] { miChangePrice });
        }

        private void SetContextMenu()
        {
			for (int i = FIXED_ROWS; i < RawDataGridView.Rows.Count; i++)
			{
				RawDataGridView.Rows[i].Cells[ColTime].ContextMenuStrip = cmTimeColumn;
                RawDataGridView.Rows[i].Cells[ColPrice].ContextMenuStrip = cmTimeColumn;
            }
        }

        private void ChangePrice(object sender, EventArgs e)
		{
            string currentDateTime = GetNormalTimeString(dtGrid.Rows[CurrentRowIndex][ColTime].ToString());
			string price = dtGrid.Rows[CurrentRowIndex][ColPrice].ToString();
            Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
            dictionary.Add("time", currentDateTime);
            dictionary.Add(Tariff.ParamNames.Price, price);
            dictionary.Add(NEW_PRICE, price);
            dictionary.Add(Pricelist.ParamNames.StartDate, DateTime.Today);
            dictionary.Add(Pricelist.ParamNames.FinishDate, Pricelist.FinishDate);
            dictionary.Add(Pricelist.ParamNames.PricelistId, Pricelist.PricelistId);

            UniversalPassportForm frm = new UniversalPassportForm(dictionary, UniversalPassportForm.PassportNames.ChangeTariffWindowsPrice,
                UniversalPassportForm.ProcedureNames.ChangePrice, "Изменение цены", ValidatePassportData);

            if (frm.ShowDialog() == DialogResult.OK)
                RefreshGrid();
        }


        private bool ValidatePassportData(Dictionary<string, object> parameters)
        {
            DateTime startDate = (DateTime)parameters[Pricelist.ParamNames.StartDate];
            DateTime finishDate = (DateTime)parameters[Pricelist.ParamNames.FinishDate];

			decimal price = (decimal)parameters[Tariff.ParamNames.Price];
            decimal newPrice = (decimal)parameters[NEW_PRICE];

			if(price == newPrice)
			{
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.NewPriceShouldBeDifferent);
                return false;
            }

            if (startDate > finishDate)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("StartFinishWindowTimeError"));
                return false;
            }

			if(startDate < Pricelist.StartDate)
			{
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(string.Format(Properties.Resources.StartDateShouldBeInsidePricelistDates, Pricelist.StartDate.ToShortDateString()));
                return false;
            }

            if (finishDate > Pricelist.FinishDate)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(string.Format(Properties.Resources.FinishDateShouldBeInsidePricelistDates, Pricelist.FinishDate.ToShortDateString()));
                return false;
            }

            return true;
        }

        private void InitializeDelegates()
		{
			populateGrid = delegate
			{
				MassmediaPricelist massmediaPriceList = PricelistOnMassmedia;
				massmediaPriceList.ExcludeModuleTariffs = excludeModuleTariffs;

				DataSet dsWindows = massmediaPriceList.GetTariffWindows(startDate, finishDate, module, showTrafficWindows);

				dtTime = dsWindows.Tables["time"];
				tariffWindows = new ITariffWindow[dtTime.Rows.Count, 7];
				dtTariffWindow = dsWindows.Tables[Constants.TableNames.Data];
				if (dtTime.Rows.Count > 0)
					PopulateGridTable();
				else if(ShowMessages)
					FogSoft.WinForm.Forms.MessageBox.ShowInformation(Properties.Resources.NoTariffWindowForGivenDate);
			};

			onGridPopulated += delegate 
			{
                SetContextMenu();
            };

			RawDataGridView.MouseDown += delegate(object sender, MouseEventArgs e)
																	 {
																		 if (isPopUpMenuAllowed && e.Button == MouseButtons.Right &&
																			 GetTariffWindow(RawDataGridView.CurrentCell) != null)
																		 {
																			 RawDataGridView.CurrentCell.ContextMenuStrip =
																				 MenuManager.CreatePopupMenu(
																					 GetTariffWindow(RawDataGridView.CurrentCell) as
																					 TariffWindowWithRollerIssues,
																					 MenuItemClick, ViewType.Journal);
																		 }
																	 };
		}

	    private void MenuItemClick(object sender, EventArgs e)
		{
			try
			{
				ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
				TariffWindowWithRollerIssues tw =
					(TariffWindowWithRollerIssues)GetTariffWindow(RawDataGridView.CurrentCell);

				tw.ObjectDeleted -= OnTariffWindowDeleted;
				tw.ObjectChanged -= OnTariffWindowChanged;
				tw.TariffExtend -= OnTariffWindowExtend;

				tw.ObjectDeleted += OnTariffWindowDeleted;
				tw.ObjectChanged += OnTariffWindowChanged;
				tw.TariffExtend += OnTariffWindowExtend;

				tw.DoAction(menuItem.Name, ParentForm, InterfaceObjects.SimpleJournal);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				RawDataGridView.CurrentCell.ContextMenuStrip = null;
			}
		}

		private void OnTariffWindowExtend(PresentationObject presentationObject)
		{
			RefreshGrid();
		}

		private void OnTariffWindowChanged(PresentationObject presentationObject)
		{
			TariffWindowWithRollerIssues window = (TariffWindowWithRollerIssues)presentationObject;
			if (window.Price != CurrentRowPrice || window.WindowDate != CurrentWindowDate)
			{
				selectedWindow = window;
				RefreshGrid();
			}
			else
				SetDataSourceValue(RawDataGridView.CurrentCell,
													 DateTimeUtils.Time2String(window.GetFreeTime(false)));
		}

		private void OnTariffWindowDeleted(PresentationObject presentationObject)
		{
			SetDataSourceValue(RawDataGridView.CurrentCell, string.Empty);
			tariffWindows[
				RawDataGridView.CurrentCell.RowIndex - FIXED_ROWS,
				RawDataGridView.CurrentCell.ColumnIndex - FixedCols
				] = null;
		}

		private void PopulateGridTable()
		{
			int rowIndex = 0;
			dtTimeLookup.Rows.Clear();
			foreach (DataRow row in dtTime.Rows)
				AddGridRow(row, dtTariffWindow, rowIndex++);
        }

		private void AddGridRow(DataRow timeAndPriceRow, DataTable dtData, int rowIndex)
		{
			DataRow gridRow = dtGrid.NewRow();
			gridRow[ColumnNames.Price] = timeAndPriceRow[ColumnNames.Price];
			string time = AddTimeString(gridRow, timeAndPriceRow);
			dtTimeLookup.Rows.Add(rowIndex, time);

			AddWindowCells(gridRow, dtData, timeAndPriceRow, rowIndex);
			dtGrid.Rows.Add(gridRow);
		}

		private string AddTimeString(DataRow gridRow, DataRow row)
		{
			int hour = int.Parse(row[ColumnNames.Hour].ToString());
			int min = int.Parse(row[ColumnNames.Min].ToString());

			if (hour >= PricelistOnMassmedia.BroadcastStart.Hour)
				gridRow[ColumnNames.TimeString] = DateTimeUtils.Time2String(hour, min);
			else
				gridRow[ColumnNames.TimeString] = DateTimeUtils.Time2String(hour + 24, min);

			return gridRow[ColumnNames.TimeString].ToString();
        }

		protected static string CreateFilterExpression(DataRow timeAndPriceRow)
		{
			return string.Format("price = {0} And hour = {1} And min= {2}",
													 timeAndPriceRow[ColumnNames.Price].ToString().Replace(",", "."),
													 timeAndPriceRow[ColumnNames.Hour],
													 timeAndPriceRow[ColumnNames.Min]);
		}

		private void AddWindowCells(DataRow gridRow, DataTable dtData, DataRow timeAndPriceRow, int rowIndex)
		{
			foreach (DataRow row in dtData.Select(CreateFilterExpression(timeAndPriceRow)))
			{
				DateTime dtOriginal = (DateTime)row[ColumnNames.WindowDateOriginal];
				DayOfWeek dayOfWeek = dtOriginal.DayOfWeek;

				int columnIndex = DefineColumnIndex(timeAndPriceRow, dayOfWeek);
				TariffWindowWithRollerIssues window = new TariffWindowWithRollerIssues(row, showTrafficWindows ? Entities.TariffWindowTM : Entities.TariffWindow);
				
                tariffWindows[rowIndex, columnIndex] = window;
				gridRow[FixedCols + columnIndex] = GetCellContent(row);
			}
		}

		private int DefineColumnIndex(DataRow timeAndPriceRow, DayOfWeek dayOfWeek)
		{
			if (int.Parse(timeAndPriceRow[ColumnNames.Hour].ToString()) >= PricelistOnMassmedia.BroadcastStart.Hour)
				return DateTimeUtils.ResolveDayOfWeekNumber(dayOfWeek) - 1;
			if (dayOfWeek == DayOfWeek.Monday)
				return 6;
			return
				DateTimeUtils.ResolveDayOfWeekNumber(dayOfWeek) - 2;
		}

		protected virtual string GetCellContent(DataRow row)
		{
			int maxCapacity = int.Parse(row[TariffWindowWithRollerIssues.ParamNames.MaxCapacity].ToString());
			string duration = DateTimeUtils.Time2String(int.Parse(row[Tariff.ParamNames.Duration].ToString()));
			if(maxCapacity == 0)
				return duration;
			return string.Format("{0} [{1}]", duration, maxCapacity);
		}

		public void DeleteCurrentObject()
		{
			if (CurrentTariffWindow != null && CurrentTariffWindowWithRollerIssues.Delete())
				OnTariffWindowDeleted(CurrentTariffWindowWithRollerIssues);
		}

		public void EditCurrentObject()
		{
			if (CurrentTariffWindowWithRollerIssues != null &&
					CurrentTariffWindowWithRollerIssues.ShowPassport(ParentForm))
				OnTariffWindowChanged(CurrentTariffWindowWithRollerIssues);
		}

		private TariffWindowWithRollerIssues CurrentTariffWindowWithRollerIssues
		{
			get { return CurrentTariffWindow as TariffWindowWithRollerIssues; }
		}

		public override Entity IssueEntity
		{
			get { return null; }
		}
	}
}