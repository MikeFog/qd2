using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Forms;
using static Merlin.Forms.UniversalPassportForm;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Controls
{
	internal class TrafficGrid : RollerIssuesGrid3
	{
		#region Members ---------------------------------------

		private readonly ContextMenuStrip cmColumnHeader = new ContextMenuStrip();
		private readonly ContextMenuStrip cmTimeColumn = new ContextMenuStrip();

		private readonly ToolStripMenuItem miSetDeadLine = new ToolStripMenuItem();
		private readonly ToolStripMenuItem miCreateNewWindow = new ToolStripMenuItem();
		private readonly ToolStripMenuItem miCreateNewWindowTemplate = new ToolStripMenuItem();
        private readonly ToolStripMenuItem miCorrectDurationInDay = new ToolStripMenuItem();

        private readonly ToolStripMenuItem miCorrectTime = new ToolStripMenuItem();
        private readonly ToolStripMenuItem miCorrectDuration = new ToolStripMenuItem();
        private readonly ToolStripMenuItem miDeleteWindows = new ToolStripMenuItem();

        private readonly IContainer components = null;
		
		private TariffWindowWithRollerIssues sourceTariffWindow;
		private SmartGrid grdMassmedia;
		private SmartGrid grdIssue;
		private CheckBox chkDate;
		private Color sourceCellBackColor;
		private readonly Color defaultSelectionBackColor;

        #endregion

        public TrafficGrid() : base(false)
		{
			InitializeComponent();
			//InitializePopupMenus();

			excludeModuleTariffs = false;
			loadPricelist = LoadPricelist;
			defaultSelectionBackColor = InternalGrid.DefaultCellStyle.SelectionBackColor;
        }

        public TrafficGrid(bool showTrafficWindows)
            :this()
        {
			ShowTrafficWindows = showTrafficWindows;
        }

		public bool ShowTrafficWindows
		{
			get { return showTrafficWindows; }
			set { showTrafficWindows = value; }
		}

        
		protected override void InitializePopupMenus()
		{
			miSetDeadLine.Text = "Пометить как обработанный";
			miSetDeadLine.Click += SetDeadLine;
			miCreateNewWindow.Text = "Создать новое окно...";
			miCreateNewWindow.Click += CreateNewTariffWindow;
			miCreateNewWindowTemplate.Text = "Создать новое окно по шаблону...";
			miCreateNewWindowTemplate.Click += CreateNewTariffWindowTemplate;
            miCorrectDurationInDay.Text = "Изменить продолжительность...";
            miCorrectDurationInDay.Click += MiCorrectDurationInDay_Click;
            miCorrectTime.Text = "Отредактировать время выхода...";
			miCorrectTime.Click += MiCorrectTime_Click;
            miCorrectDuration.Text = "Изменить продолжительность...";
            miCorrectDuration.Click += MiCorrectDuration_Click;
            miDeleteWindows.Text = "Удалить...";
            miDeleteWindows.Click += MiDeleteWindows_Click;

			cmColumnHeader.Items.AddRange(new ToolStripItem[]
											  {
												  miSetDeadLine,
												  miCreateNewWindow,
												  miCreateNewWindowTemplate,
												miCorrectDurationInDay
											  });

			cmTimeColumn.Items.AddRange(new ToolStripItem[]
											  {
												  miCorrectTime,
												miCorrectDuration,
												miDeleteWindows
											  });
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// grid
			// 
			this.grid.Location = new System.Drawing.Point(0, 0);
			this.grid.Size = new System.Drawing.Size(390, 339);
			// 
			// miSetDeadLine
			// 
			/*
			this.miSetDeadLine.Name = "miSetDeadLine";
			this.miSetDeadLine.Size = new System.Drawing.Size(32, 19);
			// 
			// miCreateNewWindow
			// 
			this.miCreateNewWindow.Name = "miCreateNewWindow";
			this.miCreateNewWindow.Size = new System.Drawing.Size(32, 19);
			*/
			// 
			// TrafficGrid
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.Name = "TrafficGrid";
			this.ResumeLayout(false);
		}

		#endregion

		public override Cursor Cursor
		{
			get { return base.Cursor; }
			set
			{
				base.Cursor = value;
				RawDataGridView.Cursor = value;
			}
		}

		public override void RefreshGrid()
		{
			Cursor currneCursor= Cursor.Current;

            try
			{
                Cursor.Current = Cursors.WaitCursor;

                base.RefreshGrid();
				if (pricelist != null)
				{
					SetContextMenu();
					MarkSourceCell(Color.BlueViolet);
					RefreshActiveWindowContent(CurrentTariffWindow);
					SetWindowGroupProcedureAndMarkCells();
                    MarkDatesBeforeDeadLine(Massmedia.DeadLine == null ? DateTime.MinValue : (DateTime)Massmedia.DeadLine);
                    MarkCellsWithOverflow();
                }
			}
			finally { Cursor.Current = currneCursor; }
		}

        private void SetWindowGroupProcedureAndMarkCells()
        {
            for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.RowCount; rowIndex++)
                for (int columnIndex = FixedCols; columnIndex < RawDataGridView.ColumnCount; columnIndex++)
                {
					if (GetTariffWindow(GetCell(rowIndex, columnIndex)) is TariffWindowWithRollerIssues window)
					{
						window.GetTariffWindow2Group = GetTariffWindow2Group;
						window.TariffWindowUngrouped = TariffWindowUngrouped;

						window.IsGroupWithPrevEnabled = rowIndex > FIXED_ROWS;
						window.IsGroupWithNextEnabled = rowIndex < RawDataGridView.RowCount - 1;

                        if (window.IsInGroup) MarkCellAsGroup(rowIndex, columnIndex);
                        if (window.IsTariffUnited) MarkCellAsUnited(rowIndex, columnIndex);
                    }
                }
        }

        protected void MarkCellAsGroup(int rowIndex, int columnIndex)
        {
            SetCellBackColor(rowIndex, columnIndex, Color.LightSeaGreen);
        }

        protected void MarkCellAsUnited(int rowIndex, int columnIndex)
        {
            SetCellBackColor(rowIndex, columnIndex, Color.FromArgb(217, 242, 208));
        }

        void TariffWindowUngrouped(bool isWithPrev, bool isUngroup)
		{
			TariffWindow window = isWithPrev ? FindPrevWindowInDay() : FindNextWindowInDay();
            int rowIndex2 = GetCell(window).RowIndex;

            if (isUngroup)
			{
                SetCellBackColor(CurrentRowIndex, CurrentColumnIndex, SystemColors.Window);
                SetCellBackColor(rowIndex2, CurrentColumnIndex, SystemColors.Window);
            }
			else
			{
				MarkCellAsGroup(CurrentRowIndex, CurrentColumnIndex);
                MarkCellAsGroup(rowIndex2, CurrentColumnIndex);
            }

			MarkCellsWithOverflow();
		}

        private TariffWindowWithRollerIssues GetTariffWindow2Group(bool isWithPrev)
		{
            TariffWindowWithRollerIssues window = isWithPrev ? FindPrevWindowInDay(): FindNextWindowInDay();

            if (window == null)
            {
                MessageBox.ShowInformation(string.Format("Не могу найти {0} окно для рекламного окна '{1}'. Операция прервана!", isWithPrev ? "предыдущее" : "последующее", CurrentTariffWindow.WindowDate.ToString("g")));
                return null;
            }

            if (MessageBox.ShowQuestion(string.Format("Хотите объединить рекламные окна '{0}' и '{1}'?", CurrentTariffWindow.WindowDate.ToString("g"), window.WindowDate.ToString("g"))) == DialogResult.Yes)
				return window;
			return null;
		}

		private TariffWindowWithRollerIssues FindPrevWindowInDay()
		{
			int rowIndex = RawDataGridView.CurrentCell.RowIndex;
			TariffWindowWithRollerIssues window = null;

            while (rowIndex-- > FIXED_ROWS)
			{
				window = GetTariffWindow(rowIndex, RawDataGridView.CurrentCell.ColumnIndex) as TariffWindowWithRollerIssues;
				if (window != null) break;

            }
			return window;
		}

        private TariffWindowWithRollerIssues FindNextWindowInDay()
        {
            int rowIndex = RawDataGridView.CurrentCell.RowIndex;
            TariffWindowWithRollerIssues window = null;

            while (rowIndex++ < RawDataGridView.RowCount - 1)
            {
                window = GetTariffWindow(rowIndex, RawDataGridView.CurrentCell.ColumnIndex) as TariffWindowWithRollerIssues;
                if (window != null) break;

            }
            return window;
        }

        private void MarkCellsWithOverflow()
		{
			for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.Rows.Count; rowIndex++)
			{
				for (int columnIndex = FixedCols; columnIndex < RawDataGridView.Columns.Count; columnIndex++)
				{
					DataGridViewCell cell = RawDataGridView.Rows[rowIndex].Cells[columnIndex];
					if (CheckWindowOverflow(cell))
						SetCellBackColor(rowIndex, columnIndex, Color.Red);
					else if(cell.Style.BackColor == Color.Red)
						SetCellBackColor(rowIndex, columnIndex, Color.White);
				}
			}
		}

		private bool CheckWindowOverflow(DataGridViewCell cell)
		{
			if (cell == null)
				return false;

            return (GetTariffWindow(cell) is TariffWindow window
                    && ((window.CapacityInUseConfirmed > window.MaxCapacity) || (window.TimeInUseConfirmed > window.Duration)));
        }

		public void BindControls(SmartGrid massmediaGrid, SmartGrid issuesGrid, CheckBox chkDate)
		{
			grdMassmedia = massmediaGrid;
			grdMassmedia.ObjectSelected += GrdMassmedia_ObjectSelected;

			grdIssue = issuesGrid;
			this.chkDate = chkDate;
			this.chkDate.CheckedChanged += ChkDate_CheckedChanged;
		}

		private void ChkDate_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				if (chkDate.Checked)
				{
					if (CurrentCell != null && CurrentCell.ColumnIndex >= FixedCols && CurrentCell.RowIndex >= FIXED_ROWS)
					{
						sourceTariffWindow = (TariffWindowWithRollerIssues)GetTariffWindow(CurrentCell);
						sourceCellBackColor = CurrentCell.Style.BackColor;
						EditMode = EditMode.TransferIssue;
						MarkSourceCell(Color.BlueViolet);
						InternalGrid.DefaultCellStyle.SelectionBackColor = Color.Yellow;
					}
					else
					{
						chkDate.Checked = false;
                    }
				}
				else
				{
					EditMode = EditMode.View;
                    MarkSourceCell(sourceCellBackColor);
                    InternalGrid.DefaultCellStyle.SelectionBackColor = defaultSelectionBackColor;
					if (sourceTariffWindow != null && sourceTariffWindow.WindowId != ((TariffWindowWithRollerIssues)GetTariffWindow(CurrentCell)).WindowId)
					{
                        grdIssue.Clear();
                        chkDate.Text = " ";
                    }
						
                    sourceTariffWindow = null;
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void GrdMassmedia_ObjectSelected(PresentationObject presentationObject)
		{
			try
			{
				ParentForm.Cursor = Cursor = Cursors.WaitCursor;
                sourceTariffWindow = null;
                EditMode = EditMode.View;
                Massmedia = presentationObject as Massmedia;
				RefreshGrid();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
				throw;
			}
			finally
			{
				ParentForm.Cursor = Cursor = Cursors.Default;
			}
		}

		private void SetContextMenu()
		{
			foreach (DataGridViewColumn column in RawDataGridView.Columns)
				if (!column.Frozen)
					column.HeaderCell.ContextMenuStrip = cmColumnHeader;

			for (int i = FIXED_ROWS; i < RawDataGridView.Rows.Count; i++)
				RawDataGridView.Rows[i].Cells[ColTime].ContextMenuStrip = cmTimeColumn;
		}

		private void SetDeadLine(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				if (MessageBox.ShowQuestion(string.Format("Вы действительно хотите пометить день '{0}' как обработанный на радиостанции '{1}'?"
						, CurrentColumnDate.ToString("dd.MM.yyyy"), Massmedia.Name)) == DialogResult.Yes)
				{
					Cursor = Cursors.WaitCursor;
					Massmedia.SetDeadLine(CurrentColumnDate);
					MarkDatesBeforeDeadLine(Massmedia.DeadLine == null ? DateTime.MinValue : (DateTime) Massmedia.DeadLine);
					grdMassmedia.UpdateRow(Massmedia);

                    SetWindowGroupProcedureAndMarkCells();
                    MarkCellsWithOverflow();
				}
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

		private void MarkDatesBeforeDeadLine(DateTime deadLine)
		{
			for (int i = FixedCols; i < RawDataGridView.Columns.Count; i++)
			{
				DateTime dt = weekDates[i - FixedCols];
				if (deadLine >= dt && dt != DateTime.MinValue)
					MarkColumnCells(i, Color.PeachPuff);
			}
		}

		private DateTime CurrentColumnDate
		{
			get { return weekDates[CurrentColumnIndex - FixedCols]; }
		}

		private void MiCorrectDurationInDay_Click(object sender, EventArgs e)
		{
			try
			{
				Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
				dictionary.Add("currentDate", CurrentColumnDate);
				dictionary.Add(Massmedia.ParamNames.MassmediaId, Massmedia.MassmediaId);
				if (dtTimeLookup.DataSet == null)
				{
					DataSet ds = new DataSet();
					ds.Tables.Add(dtTimeLookup);
				}

                ShowUniversalPassport(dictionary, UniversalPassportForm.PassportNames.ChangeDurationInDay, UniversalPassportForm.ProcedureNames.ChangeDurationInDay,
					"Изменение продолжительности", ValidatePassportDatetimes, dtTimeLookup.DataSet);
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


        private void CreateNewTariffWindowTemplate(object sender, EventArgs e)
		{
			try
			{
				if (Massmedia != null)
				{
					FrmWindowTariffTemplate frm = new FrmWindowTariffTemplate(CurrentColumnDate, Massmedia);
					if (frm.ShowDialog(this) == DialogResult.OK)
					{
						Application.DoEvents();
						Cursor.Current = Cursors.WaitCursor;
						RefreshGrid();
					}
				}
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

        private void MiDeleteWindows_Click(object sender, EventArgs e)
		{
			try
			{
				string currentDateTime = GetNormalTimeString(dtGrid.Rows[CurrentRowIndex][ColTime].ToString());
				Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
				dictionary.Add("time", currentDateTime);

				dictionary.Add("startDate", CurrentDate);
				dictionary.Add("finishDate", CurrentDate);

				UniversalPassportForm frm = new UniversalPassportForm(dictionary, UniversalPassportForm.PassportNames.DeleteWindows, DeleteWindows, "Удалить окна", ValidatePassportDates);
				if (frm.ShowDialog(this) == DialogResult.OK)
					RefreshGrid();
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

        private void MiCorrectDuration_Click(object sender, EventArgs e)
		{
			try
			{
				DataRow row = dtGrid.Rows[CurrentRowIndex];
				// строки типа "25:15" надо привести к нормальному виду
				string time = row[ColTime].ToString();
				string[] parts = time.Split(':');
				int hour = int.Parse(parts[0].ToString());
				if (hour > 23)
					time = string.Format("{0}:{1}", hour - 24, parts[1]);

				string filter = string.Format("price = {0} And time = '1900-01-01 {1}'", row[ColPrice].ToString().Replace(",", "."), time);
				DataRow[] rows = Pricelist.GetTariffList().Select(filter);
				if (rows.Length == 0)
				{
					MessageBox.ShowExclamation(Properties.Resources.TariffNotFound);
					return;
				}

				Tariff tariff = new Tariff(rows[0]);

				string currentDateTime = GetNormalTimeString(dtGrid.Rows[CurrentRowIndex][ColTime].ToString());
				Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
				dictionary.Add("time", currentDateTime);
				dictionary.Add("duration", tariff.Duration);
				dictionary.Add("newduration", tariff.Duration);
                dictionary.Add("duration_total", tariff.DurationTotal);
                dictionary.Add("newduration_total", tariff.DurationTotal);
                dictionary.Add("startDate", CurrentDate);
				dictionary.Add("finishDate", CurrentDate);
				dictionary.Add("pricelistid", Pricelist.PricelistId);
				ShowUniversalPassport(dictionary, UniversalPassportForm.PassportNames.ChangeDuration, UniversalPassportForm.ProcedureNames.ChangeDuration,
					"Изменение продолжительности", ValidatePassportDates);
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


        private void MiCorrectTime_Click(object sender, EventArgs e)
		{
			try
			{
				string currentDateTime = GetNormalTimeString(dtGrid.Rows[CurrentRowIndex][ColTime].ToString());

				Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
				dictionary.Add("time", currentDateTime);
				dictionary.Add("newtime", currentDateTime);
				dictionary.Add("startDate", CurrentDate);
				dictionary.Add("finishDate", CurrentDate);
				dictionary.Add("pricelistid", Pricelist.PricelistId);

				ShowUniversalPassport(dictionary, UniversalPassportForm.PassportNames.MoveTime, UniversalPassportForm.ProcedureNames.MoveTime,
					"Перенос времени выхода", ValidatePassportDates);
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

		private bool ValidatePassportDates(Dictionary<string, object> parameters)
		{
			if (PassportStartDate(parameters) > PassportFinishDate(parameters))
			{
                MessageBox.ShowExclamation(MessageAccessor.GetMessage("StartFinishDateError2"));
                return false;
			}
			return true;
		}

        private bool ValidatePassportDatetimes(Dictionary<string, object> parameters)
        {
			int index1 = int.Parse(parameters["startDate"].ToString());
            int index2 = int.Parse(parameters["finishDate"].ToString());
			if (index1 > index2)
			{
				MessageBox.ShowExclamation(MessageAccessor.GetMessage("StartFinishWindowTimeError"));
				return false;
			}

            string time1 = dtTimeLookup.Select("id = " + index1)[0]["name"].ToString();
            string time2 = dtTimeLookup.Select("id = " + index2)[0]["name"].ToString();
			DateTime theDate = (DateTime)parameters["currentDate"];

            parameters["startDate"] = CreateWindowDateTime(theDate, time1);
            parameters["finishDate"] = CreateWindowDateTime(theDate, time2);
            return true;
;       }

		private DateTime CreateWindowDateTime(DateTime theDate, string time)
		{
            string[] parts = time.Split(':');
            int hour = int.Parse(parts[0]);
			int min = ParseHelper.ParseToInt32(parts[1]);
			if (hour < 24)
				return new DateTime(theDate.Year, theDate.Month, theDate.Day, hour, min, 0);
            return new DateTime(theDate.Year, theDate.Month, theDate.Day, hour-24, min, 0).AddDays(1);
        }

        private void DeleteWindows(Dictionary<string, object> parameters)
		{
			try
			{
                Application.DoEvents();
                Cursor.Current = Cursors.WaitCursor;

                DateTime time = (DateTime)parameters["time"];
				DateTime startDate = PassportStartDate(parameters);
				DateTime finishDate = PassportFinishDate(parameters);

                DataTable tableErrors = Campaign.CreateErrorsTable();

                while (startDate <= finishDate)
				{

					DateTime date = new DateTime(startDate.Year, startDate.Month, startDate.Day, time.Hour, time.Minute, time.Second);
					TariffWindow window = _massmedia.GetTariffWindow(date);
					try
					{
						window?.Delete(true);
					}
					catch(Exception ex) 
					{ 
						Campaign.AddErrorRow(tableErrors, date, MessageAccessor.GetMessage(ex.Message));
                    }

					startDate = startDate.AddDays(1);
				}
                if (tableErrors.Rows.Count > 0)
                {
                    Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки удаления", tableErrors);
                }
                RefreshGrid();
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

		private static DateTime PassportStartDate(Dictionary<string, object> parameters)
		{
			return (DateTime)parameters["startDate"];
        }

        private static DateTime PassportFinishDate(Dictionary<string, object> parameters)
        {
            return (DateTime)parameters["finishDate"];
        }

        private void CreateNewTariffWindow(object sender, EventArgs e)
		{
			try
			{
				TariffWindow tariff = PricelistOnMassmedia.CreateSpecialTariffWindow(CurrentColumnDate, ParentForm);
				if (tariff != null)
					RefreshGrid();
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

        private void ShowUniversalPassport(Dictionary<string, object> parameters, string passportName, string procedureName, 
			string caption, ValidateDataDelegate validateData, DataSet dataSet = null)
        {
            UniversalPassportForm frm = new UniversalPassportForm(parameters, passportName, procedureName, caption, validateData, dataSet);
            if (frm.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        private void MarkSourceCell(Color color)
		{
			if (sourceTariffWindow == null) return;
			DataGridViewCell cell = GetCell(sourceTariffWindow);
			if (cell != null)
				cell.Style.BackColor = cell.Style.SelectionBackColor = color;
		}



		protected override void onCellClicked(ITariffWindow tariffWindow)
		{
			if (IsTransferMode && SelectedIssue != null)
			{
				Color tmp = CurrentCell.Style.BackColor;
				SetCellBackColor(CurrentCell.RowIndex, CurrentCell.ColumnIndex, Color.Yellow);
				TransferIssue(tariffWindow as TariffWindowWithRollerIssues);
                SetCellBackColor(CurrentCell.RowIndex, CurrentCell.ColumnIndex, tmp);
            }

			RefreshActiveWindowContent(tariffWindow);
			base.onCellClicked(tariffWindow);
		}

		private void RefreshActiveWindowContent(ITariffWindow tariffWindow)
		{
			if (IsTransferMode)	return;

			if (tariffWindow != null && IsActiveCellSelected && !IsTransferMode)
			{
				RefreshWindowContent(tariffWindow as TariffWindowWithRollerIssues);
				SetSelectedDate(tariffWindow.WindowDate);
			}
			else if (tariffWindow == null || !IsActiveCellSelected)
			{
				grdIssue.DataSource = null;
				chkDate.Text = " ";
				chkDate.Checked = false;
			}
		}

		private void RefreshWindowContent(ITariffWindow tariffWindow)
		{
			if (tariffWindow != null)
				grdIssue.DataSource = tariffWindow.LoadIssues(ShowUnconfirmed, grdIssue.Entity).DefaultView;
		}

		private void SetSelectedDate(DateTime selectedDate)
		{
			chkDate.Text = DateTimeUtils.ToDateTimeString(selectedDate);
		}

		private bool IsTransferMode
		{
			get { return sourceTariffWindow != null; }
		}

		private RollerIssue SelectedIssue
		{
			get { return grdIssue.SelectedObject as RollerIssue; }
		}

		private void TransferIssue(TariffWindowWithRollerIssues destinationWindow)
		{
			IList<PresentationObject> list = new List<PresentationObject>(grdIssue.Added2Checked);
			if (list.Count > 0)
			{
				Dictionary<string, object> msgParameters = new Dictionary<string, object>(3, StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "sourceDate", DateTimeUtils.ToDateTimeString(((RollerIssue)list[0]).IssueDate) },
                        { "destinationDate", DateTimeUtils.ToDateTimeString(destinationWindow.WindowDate) }
                    };
				if (Globals.ShowQuestion("IssuesTransferConfirmation", msgParameters) == DialogResult.Yes)
				{
					foreach (PresentationObject presentationObject in list)
					{
						SimpleTransfer issueTransfer = new SimpleTransfer(_massmedia);
						RollerIssue selectedIssue = presentationObject as RollerIssue;
						Roller addedRoller = issueTransfer.Transfer(selectedIssue, destinationWindow, false);
						if (addedRoller != null)
						{
							UpdateDestinationCell(addedRoller.Duration, destinationWindow);
							UpdateSourceCell(SelectedIssue.Roller.Duration);
							grdIssue.DeleteRow(SelectedIssue);
						}
					}
				}
				MarkCellsWithOverflow();
			}
		}

		private void UpdateSourceCell(int rollerDuration)
		{
			if (sourceTariffWindow.MaxCapacity > 0)
				sourceTariffWindow.CapacityInUseConfirmed--;
			else
				sourceTariffWindow.TimeInUseConfirmed -= rollerDuration;

			DataGridViewCell cell = GetCell(sourceTariffWindow);
			if (cell != null)
			{
				// если уже сохранили какой-то цвет, то затирать его не надо
				if(sourceCellBackColor == Color.White)
					sourceCellBackColor = CheckWindowOverflow(cell) ? Color.Red : Color.White;
				UpdateGridCell(cell, sourceTariffWindow);
			}
		}

		private void UpdateDestinationCell(int rollerDuration, TariffWindowWithRollerIssues destinationWindow)
		{
			if (sourceTariffWindow.MaxCapacity > 0)
				destinationWindow.CapacityInUseConfirmed++;
			else
				destinationWindow.TimeInUseConfirmed += rollerDuration;

			UpdateGridCell(RawDataGridView.CurrentCell, destinationWindow);
		}
	}
}