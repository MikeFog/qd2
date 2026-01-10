using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Forms;

namespace Merlin.Controls
{
	internal partial class ProgramIssuesGrid2 : TariffGridWithIssuesOnSingleMassmedia
	{
		private DataTable dtTariff;
		private SponsorProgram sponsorProgram;

		public ProgramIssuesGrid2()
		{
			InitializeComponent();
			InitializeDelegates();
			gridCellType = GridCellTypes.CheckBoxes;
			RawDataGridView.ReadOnly = false;
		}

		public SponsorProgram SponsorProgram
		{
			set { sponsorProgram = value; }
			get { return sponsorProgram; }
		}

		public override Entity IssueEntity
		{
			get { return ProgramIssue.GetEntity(); }
		}

		private void InitializeDelegates()
		{
			loadPricelist = LoadPricelist;
			populateGrid = PopulateGrid;
			updateDB = UpdateDB;
			GridRefreshed += ProgramIssuesGrid2_GridRefreshed;
		}

		void ProgramIssuesGrid2_GridRefreshed()
		{
			if (selectedWindow != null)
				onCellClicked(selectedWindow);
		}

		protected override DataGridViewCell ProcessBooleanCell(DataGridViewCell cell)
		{
			cell = base.ProcessBooleanCell(cell);

			TariffWindowWithProgramIssue window = GetTariffWindow(cell) as TariffWindowWithProgramIssue;
			if (window != null) cell.ToolTipText = string.Format("[{0}]", ShowUnconfirmed ? window.IssuesCount : window.IssuesCountConfirmed);

			return cell;
		}

		private void UpdateDB(DataGridViewCell cell)
		{
			if (cell is DataGridViewCheckBoxCell)
			{
				int bonus = ((SponsorPricelist) pricelist).Bonus;
				try
				{
					DataAccessor.BeginTransaction();
					if (IsCellChecked(cell))
						AddProgramIssue(bonus, cell, GetTariffWindow(cell) as TariffWindowWithProgramIssue);
					else
						DeleteProgramIssue(bonus, cell, GetTariffWindow(cell) as TariffWindowWithProgramIssue);
					campaign.RecalculateAction(false);
                    DataAccessor.CommitTransaction();
                }
                catch
                {
                    DataAccessor.RollbackTransaction();
                    throw;
                }

                Refresh();
				FireCampaignStatusChanged();
			}
		}

		private void PopulateGrid()
		{
			int rowCount = 0;
			LoadData();

			dtTariff = pricelist.GetTariffList();
			tariffWindows = new TariffWindowWithProgramIssue[dtTariff.Rows.Count,7];

			foreach (DataRow tariffRow in dtTariff.Rows)
				ProcessTariffRow(rowCount++, tariffRow);
		}

		private void ProcessTariffRow(int rowCount, DataRow tariffRow)
		{
			DataRow row = dtGrid.NewRow();
			SetFixedColumnsValues(row, tariffRow);

			for (int dayNum = 0; dayNum < 7; dayNum++)
			{
				DateTime theDate = monday.AddDays(dayNum);

				if (((dayNum == 0 && bool.Parse(tariffRow[ColumnNames.Monday].ToString()))
				     || (dayNum == 1 && bool.Parse(tariffRow[ColumnNames.Tuesday].ToString()))
				     || (dayNum == 2 && bool.Parse(tariffRow[ColumnNames.Wednesday].ToString()))
				     || (dayNum == 3 && bool.Parse(tariffRow[ColumnNames.Thursday].ToString()))
				     || (dayNum == 4 && bool.Parse(tariffRow[ColumnNames.Friday].ToString()))
				     || (dayNum == 5 && bool.Parse(tariffRow[ColumnNames.Saturday].ToString()))
				     || (dayNum == 6 && bool.Parse(tariffRow[ColumnNames.Sunday].ToString())))
				    && theDate >= startDate && theDate <= finishDate)
				{
					TariffWindowWithProgramIssue tariffWindow =
						new TariffWindowWithProgramIssue(new Tariff(tariffRow), theDate, _dtIssue, sponsorProgram, Campaign.CampaignId);
					tariffWindows[rowCount, dayNum] = tariffWindow;

					if (tariffWindow.Issue == null)
						row[dayNum + FixedCols] = false;
					else if (tariffWindow.Issue.CampaignId == campaign.CampaignId)
					{
						row[dayNum + FixedCols] = true;
						ChangeIssuesCounter(dayNum + FixedCols, 1);
					}
					else
						row[dayNum + FixedCols] = tariffWindow.Issue.Campaign.Action.FirmName;
				}
			}
			dtGrid.Rows.Add(row);
		}

		private void LoadData()
		{
			_dtIssue = _massmedia.
				GetProgramIssues(pricelist, sponsorProgram, startDate, finishDate);
		}

		private void LoadPricelist()
		{
			if(sponsorProgram != null)
				pricelist = sponsorProgram.GetPricelist(currentDate);
		}

		private void DeleteProgramIssue(int bonus, DataGridViewCell cell, TariffWindowWithProgramIssue tariffWindow)
		{
			tariffWindow.Issue.Bonus = bonus;
			tariffWindow.Issue.TariffPrice = tariffWindow.Price;
			tariffWindow.Issue.Delete(true);
			tariffWindow.Issue = null;
			ChangeIssuesCounter(cell.ColumnIndex, -1);
			ChangeSumCount(cell, -1);
		}

		private void AddProgramIssue(int bonus, DataGridViewCell cell, TariffWindowWithProgramIssue tariffWindow)
		{
			tariffWindow.Issue =
				campaign.AddProgramIssue(
					sponsorProgram, GetTariffWindow(cell).TariffId,
					GetTariffWindow(cell).WindowDate, GetTariffWindow(cell).Price,
					bonus, campaign.Action.IsConfirmed, _advertType);
			ChangeIssuesCounter(cell.ColumnIndex, 1);
			ChangeSumCount(cell, 1);
		}

		private static void ChangeSumCount(DataGridViewCell cell, int change)
		{
			int count = ParseHelper.ParseToInt32(cell.ToolTipText.Trim('[', ']'));
			cell.ToolTipText = string.Format("[{0}]", count + change);
		}

		public PresentationObject AdvertType
		{
			set { _advertType = value; }
		}
	}
}