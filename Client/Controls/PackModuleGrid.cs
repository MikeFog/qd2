using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Controls
{
	internal partial class PackModuleGrid : TariffGridWithCampaignIssues, IRollerGrid
	{
		private PackModule module;
		private Roller roller;

        public PackModuleGrid()
		{
			InitializeComponent();
			InitializeDelegates();
			FixedCols = 1;
		}
                
		private void InitializeDelegates()
		{
			loadPricelist = delegate
			                {
							if(module != null) pricelist = module.GetPriceList(currentDate);
			                };

			populateGrid = delegate
			               {
			               	DataSet dsWindows =	PackModulePricelist.GetTariffWindows(startDate, finishDate, ShowUnconfirmed);
			               	DataRow gridRow = dtGrid.NewRow();
							tariffWindows = new ITariffWindow[1, 7];

			               	gridRow[ColumnNames.Price] = PackModulePricelist.Price;

			               	foreach(DataRow row in dsWindows.Tables[0].Rows)
			               	{
								ProcessDay(gridRow, row);
			               	}
							dtGrid.Rows.Add(gridRow);
			               };

			updateDB = delegate (DataGridViewCell cell)
					   {
						   TariffWindowPackModule tariffWindow = (TariffWindowPackModule)GetTariffWindow(cell);
						   try
						   {
							   DataAccessor.BeginTransaction();
							   tariffWindow.AddIssue(PackModulePricelist, roller, (CampaignPackModule)campaign, rollerPosition, Grantor == null ? null : (int?)Grantor.Id);
							   campaign.RecalculateAction(false);
                               DataAccessor.CommitTransaction();
                           }
                           catch
                           {
                               DataAccessor.RollbackTransaction();
                               throw;
                           }

                           ChangeIssuesCounter(cell.ColumnIndex, 1);
						   MarkCellAsHavingCurrentCampaignIssues(cell.RowIndex, cell.ColumnIndex);
						   FireCampaignStatusChanged();
						   RefreshCurrentCell(tariffWindow.WindowDate);
					   };

			onGridPopulated = delegate
			                  {
			                  	foreach(DataRow row in LoadIssues().Rows)
			                  	{
			                  		int columnIndex = DateTimeUtils.ResolveDayOfWeekNumber(
			                  			((DateTime)row[RollerIssue.ParamNames.IssueDate]).DayOfWeek);
                                                        ChangeIssuesCounter(int.Parse(row["weekday"].ToString()), 1);
														MarkCellAsHavingCurrentCampaignIssues(2, columnIndex);
			                  	}

													for(int i = 0; i < tariffWindows.Length; i++)
													{
														TariffWindowPackModule tariffWindow = tariffWindows[0, i] as TariffWindowPackModule;
														if (tariffWindow != null && ((rollerPosition == RollerPositions.First && !tariffWindow.isFirstBusy) || (rollerPosition == RollerPositions.Second && !tariffWindow.isSecondBusy) || (rollerPosition == RollerPositions.Last && !tariffWindow.isLastBusy)))
															MarkCellAsNotOccupied(2, i + 1);
													}
			                  };
		}

		private void ProcessDay(DataRow gridRow, DataRow row)
		{
			DayOfWeek dayOfWeek = ((DateTime)row["date"]).DayOfWeek;
												
			if(!row.IsNull("duration"))
			{
				int columnIndex = DateTimeUtils.ResolveDayOfWeekNumber(dayOfWeek);
				int maxCapacity = int.Parse(row["maxCapacity"].ToString());
				string timeString = DateTimeUtils.Time2String(int.Parse(row["duration"].ToString()));
				if (maxCapacity == 0)
					gridRow[columnIndex] = timeString;
				else
					gridRow[columnIndex] = string.Format("{0} [{1}]", timeString, row["freeCapacity"]);

				tariffWindows[0, columnIndex - 1] =
					new TariffWindowPackModule(row, PackModulePricelist.Price, module.PackModuleId);
			}
		}

		private void RefreshCurrentCell(DateTime date)
		{
			DataSet dsWindows = PackModulePricelist.GetTariffWindows(date, date, ShowUnconfirmed);
			ProcessDay(dtGrid.Rows[2], dsWindows.Tables[0].Rows[0]);	
		}

		protected override void InitializeGridColumns()
		{
			gridColumns = new GridColumn[]
				{
					new GridColumn("Öåíà", ColumnNames.Price, "c", Type.GetType("System.Decimal")),
					new GridColumn("Ïí.", ColumnNames.Monday),
					new GridColumn("Âò.", ColumnNames.Tuesday),
					new GridColumn("Ñð.", ColumnNames.Wednesday),
					new GridColumn("×ò.", ColumnNames.Thursday),
					new GridColumn("Ïò.", ColumnNames.Friday),
					new GridColumn("Ñá.", ColumnNames.Saturday),
					new GridColumn("Âñ.", ColumnNames.Sunday),
					new GridColumn(ColumnNames.Time, ColumnNames.Time, true)
				};			
		}

		public PresentationObject Module
		{
			get{ return module;}
			set { module = (PackModule)value; }
		}

		public void RefreshCurrentCell(bool hasCurrentCampaignIssues, TariffGridRefreshMode mode)
		{
			RefreshCurrentCell(CurrentTariffWindow.WindowDate);
			if(mode == TariffGridRefreshMode.WithDelete)
				ChangeIssuesCounter(RawDataGridView.CurrentCell.ColumnIndex, -1);

			if (hasCurrentCampaignIssues)
				MarkCellAsHavingCurrentCampaignIssues(RawDataGridView.CurrentCell);
			else
				MarkCellAsNotHavingCurrentCampaignIssues(RawDataGridView.CurrentCell);			
		}

		public Roller Roller
		{
			get { return roller; }
			set { roller = value; }
		}

		protected override void SetNavigationCaption()
		{
			if(pricelist == null) return;
			Caption.Caption = string.Format("'{0}' Ïðàéñ-ëèñò: {1} - {2}",
			                                module.Name,
			                                pricelist.StartDate.ToShortDateString(),
			                                pricelist.FinishDate.ToShortDateString());
		}

		private PackModulePricelist PackModulePricelist
		{
			get { return pricelist as PackModulePricelist; }
		}

		public override Campaign Campaign
		{
			get { return campaign; }
			set { campaign = value; }
		}

		public override Entity IssueEntity
		{
			get 
			{
				//return PackModuleIssue.GetEntity(); 
				return EntityManager.GetEntity((int)Entities.PackModuleIssueInCampaignForm);
			}
		}

		private DataTable LoadIssues()
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters.Add(Classes.Pricelist.ParamNames.PricelistId, pricelist.PricelistId);
			parameters.Add(Classes.Campaign.ParamNames.StartDate, startDate);
			parameters.Add(Classes.Campaign.ParamNames.FinishDate, finishDate);			
			
			campaign.ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleIssue);

			DataTable issues = campaign.GetFilteredContent(parameters);			
			return issues;
		}

		#region IRollerGrid Members

		public SecurityManager.User Grantor { get; set; }

		#endregion
	}
}