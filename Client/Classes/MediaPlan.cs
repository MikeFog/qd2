using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DataTable = System.Data.DataTable;

namespace Merlin.Classes
{
    internal class MediaPlan
	{
		private int currentY;
		protected IDocumentSheet activeSheet;
		protected IList<Campaign> campaigns;
		private Dictionary<int, int> colRollers;
		private Dictionary<string, int> colTimeWindows;
		private readonly IList<DateTime> monthes;
		private readonly Action action;
		private readonly DateTime? _dateFrom;
		private readonly DateTime? _dateTo;
		private bool _isFact;
        private readonly bool _selectively;
        private bool exportStarted = false;

        private string _selectedRollers = null;
		private int _columnWithRollerName;
		private PrintSettings _printSettings = new PrintSettings() { PrintWithSignatures = false };

		#region Singleton

        private MediaPlan(Action action, IList<Campaign> campaigns, IList<DateTime> monthes, DateTime? from, DateTime? to, bool selectively)
		{
			this.campaigns = campaigns;
			this.monthes = monthes;
			this.action = action;
			_dateFrom = from;
			_dateTo = to;
            _selectively = selectively;
		}

        public static MediaPlan CreateInstance(Campaign campaign, IList<DateTime> monthes, bool selectively)
		{
			IList<Campaign> campaigns = new List<Campaign> {campaign};
            return new MediaPlan(null, campaigns, monthes, null, null, selectively);
		}

        public static MediaPlan CreateInstance(Campaign campaign, bool selectively)
		{
			IList<Campaign> campaigns = new List<Campaign> {campaign};
            return new MediaPlan(null, campaigns, null, null, null, selectively);
		}

        public static MediaPlan CreateInstance(Campaign campaign, DateTime dtFrom, DateTime dtTo, bool selectively)
		{
			IList<Campaign> campaigns = new List<Campaign> {campaign};
            return new MediaPlan(null, campaigns, null, dtFrom, dtTo, selectively);
		}

        public static MediaPlan CreateInstance(IList<Campaign> campaigns, IList<DateTime> monthes, bool selectively)
		{
            return new MediaPlan(null, campaigns, monthes, null, null, selectively);
		}

        public static MediaPlan CreateInstance(IList<Campaign> campaigns, bool selectively)
		{
            return new MediaPlan(null, campaigns, null, null, null, selectively);
		}

        public static MediaPlan CreateInstance(IList<Campaign> campaigns, DateTime dtFrom, DateTime dtTo, bool selectively)
		{
            return new MediaPlan(null, campaigns, null, dtFrom, dtTo, selectively);
		}

        public static MediaPlan CreateInstance(Action action, bool selectively)
		{
            return new MediaPlan(action, null, null, null, null, selectively);
		}

		#endregion
		
		public void Show(bool isFact)
		{
			_isFact = isFact;
            CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;
            Globals.SetWaitCursor(Globals.MdiParent);
            try
			{
                if (action == null)
				{
					var frmSettings = new Forms.PrintMediaPlanSettings();
					if(frmSettings.ShowDialog(Globals.MdiParent) == DialogResult.Cancel) return;
					_printSettings = frmSettings.Settings;
                }

                //Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

                ProgressForm.Show(Globals.MdiParent, worker_DoWork, "Экспортируется график размещения...", null);
			}
			catch(Exception e)
			{
				ErrorManager.LogError("Error to show media plan", e);
			}
			finally
			{
                Thread.CurrentThread.CurrentCulture = oldCulture;
                Globals.SetDefaultCursor(Globals.MdiParent);
			}
		}

		private void worker_DoWork(object sender, DoWorkEventArgs e)
		{
            CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;
            try
			{
                //Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

                ExportMediaPlan();
			}
			catch (Exception exp)
			{
				ErrorManager.LogError("Error to show media plan", exp);
			}
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }

        }

        private void ExportMediaPlan()
		{
			PrintMediaPlan(_isFact);
            if (exportStarted)
            {
                ExportManager.Application.FinishExport();
            }
		}

		private bool SelectRollers()
        {
            Dictionary<int, string> allRollers = new Dictionary<int, string>();

            if (action != null)
            {
                CombineRollers(allRollers, action, null, null, null);
            }
            else
            {
                foreach (Campaign campaign in campaigns)
                {
                    if (monthes != null)
                    {
                        foreach (DateTime time in monthes)
                        {
                            if ((time.Year > campaign.StartDate.Year ||
                                 (time.Year == campaign.StartDate.Year && time.Month >= campaign.StartDate.Month))
                                &&
                                (time.Year < campaign.FinishDate.Year ||
                                 (time.Year == campaign.FinishDate.Year && time.Month <= campaign.FinishDate.Month)))
                            {
                                CombineRollers(allRollers, null, campaign, time.Year, time.Month);
                            }
                        }
                    }
                    else
                    {
                        CombineRollers(allRollers, null, campaign, null, null);
                    }
                }
            }

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("rollerID", typeof(int));
            dataTable.Columns.Add("name", typeof(string));
            dataTable.DefaultView.Sort = "name asc";
            foreach (var roller in allRollers)
            {
                dataTable.Rows.Add(roller.Key, roller.Value);
            }

            bool cancelled = true;

            var selectRollersAction = new System.Action(() =>
            {
                SelectionForm selectRollers = new SelectionForm(EntityManager.GetEntity((int)Entities.Roller), dataTable.DefaultView, "Выберите ролики", true);
                if (selectRollers.ShowDialog(Globals.MdiParent) == DialogResult.OK && selectRollers.AddedItems.Count > 0)
                {
                    string[] rollerIDs = new string[selectRollers.AddedItems.Count];
                    for (int i = 0; i < selectRollers.AddedItems.Count; i++)
                    {
                        rollerIDs[i] = selectRollers.AddedItems[i].Key;
                    }
                    _selectedRollers = string.Join(",", rollerIDs) + ",";
                    cancelled = false;
                }
            });

            if (Globals.MdiParent.InvokeRequired)
            {
                Globals.MdiParent.Invoke(selectRollersAction);
            }
            else
            {
                selectRollersAction();
            }

            return !cancelled;
        }

        private void CombineRollers(IDictionary<int, string> allRollers, Action action, Campaign campaign, int? year, int? month)
        {
            Dictionary<string, object> procParameters = new Dictionary<string, object>();
            if (campaign != null)
            {
                procParameters.Add("campaignId", campaign.CampaignId);
                procParameters.Add("campaignTypeId", (int)campaign.CampaignType);
            }
            else
            {
                procParameters.Add("actionId", action.ActionId);
            }
            procParameters.Add("isFact", _isFact);

            if (year.HasValue && month.HasValue)
            {
                procParameters.Add("year", year);
                procParameters.Add("month", month);
            }

            if (_dateFrom.HasValue && _dateTo.HasValue)
            {
                procParameters.Add("startDate", _dateFrom.Value);
                procParameters.Add("finishDate", _dateTo.Value);
            }
            procParameters.Add("onlyRollers", true);

            DataSet ds = DataAccessor.LoadDataSet("MediaPlanRetrieve_v2", procParameters);
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                int rollerID = ParseHelper.GetInt32FromObject(row["rollerID"], 0);
                if (rollerID > 0)
                {
                    if (!allRollers.ContainsKey(rollerID))
                    {
                        allRollers.Add(rollerID, ParseHelper.GetStringFromObject(row["name"], string.Empty));
                    }
                }
            }
        }

		private void PrintMediaPlan(bool isFact)
		{
            if (_selectively)
            {
                if (!SelectRollers())
                {
                    return;
                }
            }

			if (action != null)
			{
				PrintActionInfo(isFact);
			}
			else
			{
				foreach (Campaign campaign in campaigns)
				{
					if (monthes != null)
					{
						foreach (DateTime time in monthes)
						{
							if ((time.Year > campaign.StartDate.Year ||
							     (time.Year == campaign.StartDate.Year && time.Month >= campaign.StartDate.Month))
							    &&
							    (time.Year < campaign.FinishDate.Year ||
							     (time.Year == campaign.FinishDate.Year && time.Month <= campaign.FinishDate.Month)))
								PrintCampaignInfo(campaign, isFact, time.Year, time.Month);
						}
					}
					else
						PrintCampaignInfo(campaign, isFact, null, null);
				}
			}
		}

		private void PrintActionInfo(bool isFact)
		{
            bool printedHeader = false;
			IDictionary<String, String> mms = action.GetUniqueMassmedias(isFact);
			foreach (KeyValuePair<string, string> mm in mms)
			{
                DataSet ds;
                if (LoadData(null, mm.Key, isFact, null, null, out ds))
                {
                    if (!printedHeader)
                    {
                        currentY = 2;
                        VerifyExportManager();
                        activeSheet = ExportManager.Application.GetNewSheet(action.ActionId.ToString(), "Tahoma", 8);
                        SetPageOrientation();

                        printedHeader = true;
                    }

                    PrintCaption(action.ActionId, 3, currentY);
                    currentY++;
                    PrintHeader(action, null, mm.Value, mm.Key);
                    PrintContent(ds, null, mm.Key, isFact, null, null);
                    currentY += 3;
                }
			}
		}

        private void VerifyExportManager()
        {
            if (!exportStarted)
            {
                var startExport = new System.Action(() =>
                {
                    ExportManager.Application.StartExport();
                });

                if (Globals.MdiParent.InvokeRequired)
                {
                    Globals.MdiParent.Invoke(startExport);
                }
                else
                {
                    startExport();
                }

                exportStarted = true;
            }
        }

		private void PrintCampaignInfo(Campaign campaign, bool isFact, int? year, int? month)
		{
			bool printedHeader = false;

            IDictionary<String, String> mms = new Dictionary<String, String>();
            if (campaign.CampaignType == Campaign.CampaignTypes.PackModule)
            {
                CampaignPackModule campaignPackModule = campaign as CampaignPackModule;
                mms = campaignPackModule.GetUniqueMassmedias(isFact);
            }
            else
            {
                Massmedia mm = ((CampaignOnSingleMassmedia)campaign).Massmedia;
                mms.Add(mm.MassmediaId.ToString() + ',', mm.NameWithoutGroup);
            }

            foreach (KeyValuePair<string, string> mm in mms)
            {
                DataSet ds;
                if (LoadData(campaign, mm.Key, isFact, year, month, out ds))
                {
                    if (!printedHeader)
                    {
                        currentY = 2;
                        VerifyExportManager();
                        activeSheet = ExportManager.Application.GetNewSheet(GetSheetName(campaign, year, month), "Tahoma", 8);
                        SetPageOrientation();

                        printedHeader = true;
                    }

                    PrintCaption(campaign.ActionId.Value, 3, currentY);
					currentY++;
                    PrintHeader(campaign.Action, campaign.Agency, mm.Value, mm.Key);
                    currentY++;
                    PrintContent(ds, campaign, mm.Key, isFact, year, month);
                    currentY += 3;
                }
            }
		}

		private static string GetSheetName(Campaign campaign, int? year, int? month)
		{
			string prefix = (year.HasValue && month.HasValue) ? string.Format("{0} {1} ", month, year) : string.Empty;
			int lenghtPrefix = prefix.Length + campaign.CampaignId.ToString().Length;
			if ((campaign.Name.Length + 3 + lenghtPrefix) > 30)
				return
					string.Format("{0}{1}... ({2})", 
						prefix, campaign.Name.Substring(0, 30 - (6 + lenghtPrefix)),
					              campaign.CampaignId);
			else
				return string.Format("{0}{1} ({2})", prefix, campaign.Name, campaign.CampaignId);
		}

		private void PrintContent(DataSet ds, Campaign campaign, string mmIds, bool isFact, int? year, int? month)
		{
			PrintRollersList(ds.Tables[0], campaign == null ? Campaign.CampaignTypes.Module : campaign.CampaignType);
			if ((campaign != null && campaign.CampaignType == Campaign.CampaignTypes.Sponsor) || (campaign == null && action != null))
				PrintPrograms(ds.Tables[4]);
			currentY++;
			if (_columnWithRollerName > 0)
			{
				PrintTimeList(ds.Tables[1], campaign == null ? Campaign.CampaignTypes.Module : campaign.CampaignType);
				PrintIssuesGrid(ds.Tables[1].Rows.Count, ds.Tables[2], ds.Tables[3], campaign == null ? Campaign.CampaignTypes.Module : campaign.CampaignType, year, month);
			}
			PrintFooter(campaign, ds.Tables[1], ds.Tables[2], mmIds, year, month);
		}

        private bool LoadData(Campaign campaign, string mmIds, bool isFact, int? year, int? month, out DataSet ds)
        {
            Dictionary<string, object> procParameters = new Dictionary<string, object>(2);
            if (campaign != null)
            {
                procParameters.Add("campaignId", campaign.CampaignId);
                procParameters.Add("campaignTypeId", (int)campaign.CampaignType);
            }
            else
            {
                procParameters.Add("actionId", action.ActionId);
            }
            procParameters.Add("massmediaIDString", mmIds);
            procParameters.Add("isFact", isFact);

            if (year.HasValue && month.HasValue)
            {
                procParameters.Add("year", year);
                procParameters.Add("month", month);
            }

            if (_dateFrom.HasValue && _dateTo.HasValue)
            {
                procParameters.Add("startDate", _dateFrom.Value);
                procParameters.Add("finishDate", _dateTo.Value);
            }

            if (_selectively)
            {
                procParameters.Add("rollerIDString", _selectedRollers);
            }

            ds = DataAccessor.LoadDataSet("MediaPlanRetrieve_v2", procParameters);

            if (_selectively)
            {
                bool hasData = false;
                foreach (DataTable dataTable in ds.Tables)
                {
                    if (dataTable.Rows.Count > 0)
                    {
                        hasData = true;
                        break;
                    }
                }

                if (!hasData)
                {
                    return false;
                }
            }

            return true;
        }

		private void PrintPrograms(DataTable dtProgIssues)
		{
			SetCellValue(currentY, 3, "Программы:");
			foreach (DataRow row in dtProgIssues.Rows)
			{
				DateTime issueDate = DateTime.Parse(row["issueDate"].ToString());
				SetCellValue(currentY, 4, issueDate.ToShortDateString());
				SetCellValue(currentY, 5, issueDate.ToShortTimeString());
				SetCellValue(currentY++, 6, row["name"]);
			}
		}

		private void PrintFooter(Campaign campaign, DataTable dtTimeList, DataTable dtIssues, string mmIds, int? year, int? month)
		{
			bool isByMounth = year.HasValue && month.HasValue;
			bool isByPeriod = _dateTo.HasValue && _dateFrom.HasValue;
			string[] ids = mmIds.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
			decimal priceTotal = 0;
			decimal tariffPriceTotal = 0;
			decimal taxPriceTotal = 0;
			if (campaign != null)
			{
				DateTime start = isByMounth ? new DateTime(year.Value, month.Value, 1) : isByPeriod ? _dateFrom.Value : campaign.StartDate;
				DateTime finish = isByMounth ? new DateTime(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value)) : isByPeriod ? _dateTo.Value : campaign.FinishDate;
				foreach (string id in ids)
				{
					campaign.GetPriceByPeriodWithTax(start, finish, int.Parse(id), false, _selectedRollers, out decimal price, out decimal tariffPrice, out decimal taxPrice);
					priceTotal += price;
                    tariffPriceTotal += tariffPrice;
					taxPriceTotal += taxPrice;	
				}
			}
			else
			{
				foreach (string id in ids)
				{
					foreach (DataRow row in action.Campaigns().Rows)
					{
						Campaign c = Campaign.GetCampaignById(int.Parse(row["campaignID"].ToString()));
						if (c.CampaignType == Campaign.CampaignTypes.PackModule
							|| ((CampaignOnSingleMassmedia)c).Massmedia.MassmediaId.ToString() == id)
						{
							DateTime start = isByMounth ? new DateTime(year.Value, month.Value, 1) : isByPeriod ? _dateFrom.Value : c.StartDate;
							DateTime finish = isByMounth
							                  	? new DateTime(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value))
												: isByPeriod ? _dateTo.Value : c.FinishDate;

                            c.GetPriceByPeriodWithTax(start, finish, int.Parse(id), false, _selectedRollers, out decimal price, out decimal tariffPrice, out decimal taxPrice);
                            priceTotal += price;
                            tariffPriceTotal += tariffPrice;
                            taxPriceTotal += taxPrice;
						}
					}
				}
			}

			SetCellValue(currentY++, 3, string.Format("Всего трансляций: {0}", dtIssues.Rows.Count * ids.Length));
            int totalDuration = dtTimeList.Rows.Count > 0 ? ids.Length * int.Parse(dtTimeList.Compute("sum(totalDuration)", string.Empty).ToString()) : 0;
            SetCellValue(currentY, 3, string.Format("Время трансляций: {0}", DateTimeUtils.Time2String(totalDuration)));
			currentY++;
            decimal discount = 1 - (tariffPriceTotal == 0 ? 1 : (priceTotal / tariffPriceTotal));
            if (!_printSettings.HideTariffPrice)
			{
                if (discount == decimal.Zero)
                    SetCellValue(currentY++, 3, $"Стоимость спланированной рекламы: {priceTotal:c}");
				SetCellValue(currentY++, 3, $"Стоимость спланированной рекламы по тарифам: {tariffPriceTotal:c}");

				if (discount != decimal.Zero)
				{
					SetCellValue(currentY++, 3, string.Format("Скидка: {0}", discount.ToString("P")));
					SetCellValue(currentY++, 3, $"Стоимость спланированной рекламы с учетом скидки: {priceTotal:c}");
				}
			}
			else
			{
				SetCellValue(currentY++, 3, $"Стоимость спланированной рекламы: {priceTotal:c}");
            }
			if (taxPriceTotal > 0)
				SetCellValue(currentY++, 3, $"В том числе  НДС  (5%): {taxPriceTotal:c}");
            currentY++;
			SetCellValue(currentY, 3, "Исполнитель:");

			if (campaign != null && _printSettings.PrintWithSignatures && campaign.Agency.SignatureBytes != null)
			{
                activeSheet.InsertImage(currentY, 7, campaign.Agency.SignatureBytes);
            }

			currentY += 4;
			SetCellValue(currentY, 3, "Заказчик:");

			currentY += 2;
			if (campaign != null && ConfigurationUtil.IsPrintContactPerson)
				SetCellValue(currentY, 3, string.Format("Контактное лицо: {0}", campaign.Action.Creator.ContactInfo));
        }

		private void PrintIssuesGrid(int rowsCount, DataTable dtIssues, DataTable dataCounts, Campaign.CampaignTypes campaignType, int? year, int? month)
		{
            List<string[]> dateColumns = new List<string[]>();
			string[] dateColumn = null;
			DateTime currentDate = (year.HasValue && month.HasValue) ? new DateTime(DateTime.MinValue.Year, DateTime.MinValue.Month, 1) : DateTime.MinValue;
			List<int> weekend = new List<int>();

			foreach (DataRow row in dtIssues.Rows)
			{
				DateTime issueDate = DateTime.Parse(row["issueDate"].ToString());
				if (currentDate != issueDate.Date)
				{
					if (year.HasValue && month.HasValue && ((issueDate.Day - currentDate.Day) > 1 || currentDate == DateTime.MinValue))
					{
						if (currentDate == DateTime.MinValue && currentDate.Day != issueDate.Day)
						{
							dateColumn = new string[rowsCount + 2];
							CreateNewColumn(new DateTime(year.Value, month.Value, currentDate.Day), dateColumn, dateColumns, weekend);
						}

						for (int i = currentDate.Day; i < issueDate.Day - 1; i++)
						{
							if (dateColumn == null)
							{
								dateColumn = new string[rowsCount + 2];
								CreateNewColumn(new DateTime(year.Value, month.Value, currentDate.Day), dateColumn, dateColumns, weekend);
							}
							currentDate = currentDate.AddDays(1);
							dateColumn = new string[rowsCount + 2];
							CreateNewColumn(new DateTime(year.Value, month.Value, currentDate.Day), dateColumn, dateColumns, weekend);
						}
					}
					
					currentDate = issueDate.Date;
					dateColumn = new string[rowsCount + 2];
					CreateNewColumn(currentDate, dateColumn, dateColumns, weekend);
				}
				int rollerId = GetRollerIndex(int.Parse(row["rollerId"].ToString()));
				int rowIndex = GetRowIndex(row, campaignType) + 2;

				if (dateColumn != null)
				{
					int posId = int.Parse(row["positionId"].ToString());
					string pos = (posId == (int) RollerPositions.First || posId == (int) RollerPositions.FirstTransferred)
					             	? "(F)"
					             	: (posId == (int) RollerPositions.Second || posId == (int) RollerPositions.SecondTransferred)
					             	  	? "(S)"
					             	  	: (posId == (int) RollerPositions.Last || posId == (int) RollerPositions.LastTransferred)
					             	  	  	? "(L)"
					             	  	  	: string.Empty;
					if (string.IsNullOrEmpty(dateColumn[rowIndex]))
						dateColumn[rowIndex] = string.Format("{0}{1}", rollerId, pos);
					else
						dateColumn[rowIndex] += string.Format(",{0}{1}", rollerId, pos);
				}
			}

			if (year.HasValue && month.HasValue && currentDate.Day != DateTime.DaysInMonth(year.Value, month.Value))
			{
				for(int i = currentDate.Day + 1; i <= DateTime.DaysInMonth(year.Value, month.Value); i++)
				{
					currentDate = currentDate.AddDays(1);
					dateColumn = new string[rowsCount + 2];
					CreateNewColumn(currentDate, dateColumn, dateColumns, weekend);
				}
			}

            int left = campaignType == Campaign.CampaignTypes.Simple ? 5 : 4; 

			foreach (int i in weekend)
				activeSheet.SetBackground(currentY, left + i, currentY + rowsCount + 2, left + i, 0xD2, 0xD2, 0xD2);

            object[,] data = CreateDataMatrix(dateColumns, rowsCount + 2);
			ExportManager.PopulateWorksheet(data, left, currentY, activeSheet);
            ExportManager.CopyData2WorkSheet(activeSheet, dataCounts, left, currentY + rowsCount + 2, true);
                        
			RotateCellsWithDate(left, data.GetLength(1));
			currentY += rowsCount + 5;
			activeSheet.SetAutoFitCells(left, left + dateColumns.Count);

			if (campaignType == Campaign.CampaignTypes.Sponsor)
			{
				activeSheet.SetColumnWidth(_columnWithRollerName, activeSheet.GetColumnWidth(_columnWithRollerName - 2));
                activeSheet.SetColumnWidth(_columnWithRollerName - 1, activeSheet.GetColumnWidth(_columnWithRollerName - 2));
            }
			else
				activeSheet.SetColumnWidth(_columnWithRollerName, activeSheet.GetColumnWidth(_columnWithRollerName - 1));
        }

		private static void CreateNewColumn(DateTime currentDate, string[] dateColumn, IList<string[]> dateColumns, ICollection<int> weekend)
		{
			dateColumn[0] = currentDate.ToShortDateString();
			dateColumn[1] = DateTimeUtils.ResolveWeekDayName(currentDate.DayOfWeek, DateTimeUtils.WeekDayNameFormat.Short);
			dateColumns.Add(dateColumn);

			if ((currentDate.DayOfWeek == DayOfWeek.Saturday
			     || currentDate.DayOfWeek == DayOfWeek.Sunday) && !weekend.Contains(dateColumns.IndexOf(dateColumn)))
				weekend.Add(dateColumns.IndexOf(dateColumn));
		}

		private int GetRowIndex(DataRow row, Campaign.CampaignTypes type)
		{
			return colTimeWindows[CreateTimeCollectionKey(row, type)];
		}

		private void RotateCellsWithDate(int left, int width)
		{
			for (int offset = 0; offset < width; offset++)
				activeSheet.SetOrientationForCells(currentY, left + offset, 90);
		}

		private int GetRollerIndex(int rollerId)
		{
			return colRollers[rollerId];
		}

		private static object[,] CreateDataMatrix(IList<string[]> dateColumns, int rowsCount)
		{
			object[,] data = new object[rowsCount,dateColumns.Count];
			for (int col = 0; col < dateColumns.Count; col++)
				for (int row = 0; row < dateColumns[col].Length; row++)
					data[row, col] = dateColumns[col][row];
			return data;
		}

		private void PrintTimeList(DataTable dtTimes, Campaign.CampaignTypes campaignType)
		{
			SetCellValue(currentY, 1, "Время");
			SetCellValue(currentY, 2, "Коммент.");
            if (campaignType == Campaign.CampaignTypes.Simple)
                SetCellValue(currentY, 3, "Цена");
            SetCellValue(currentY, campaignType == Campaign.CampaignTypes.Simple ? 4 : 3, "Прод-ть");
			activeSheet.SetBoldForRange(currentY, 1, currentY, 3 + (campaignType == Campaign.CampaignTypes.Simple ? 1 : 0));
			ExportManager.CopyData2WorkSheet(activeSheet, dtTimes, 1, ++currentY);
			CreateTimeCollection(dtTimes.Rows, campaignType);
            activeSheet.SetFormatForCell(currentY, 1, currentY + dtTimes.Rows.Count, 1, "time");
            if (campaignType == Campaign.CampaignTypes.Simple)
			{
				activeSheet.SetFormatForCell(currentY, 3, currentY + dtTimes.Rows.Count, 3, typeof(Money));
            }
            currentY -= 2;
        }

		private void CreateTimeCollection(DataRowCollection rows, Campaign.CampaignTypes type)
		{
			colTimeWindows = new Dictionary<string, int>(rows.Count);
			int index = 0;
			foreach (DataRow row in rows)
				colTimeWindows.Add(CreateTimeCollectionKey(row, type), index++);
		}

		private static string CreateTimeCollectionKey(DataRow row, Campaign.CampaignTypes type)
		{
			return string.Format("{0}{1}", row["time"], type == Campaign.CampaignTypes.Simple ? row["price"] : string.Empty);
		}

		private void PrintRollersList(DataTable dtRollers, Campaign.CampaignTypes type)
		{
			int index = 1;
			colRollers = new Dictionary<int, int>(dtRollers.Rows.Count);
			int colIndex = type == Campaign.CampaignTypes.Simple ? 4 : 3;
			SetCellValue(currentY, colIndex, "Ролики:");
			foreach (DataRow row in dtRollers.Rows)
			{
                colIndex = type == Campaign.CampaignTypes.Simple ? 5 : 4;
				colRollers.Add(int.Parse(row["rollerId"].ToString()), index);
				SetCellValue(currentY, colIndex++, string.Format("№{0}", index++));
				SetCellValue(currentY, colIndex++, DateTimeUtils.Time2String(int.Parse(row["duration"].ToString())));
				SetCellValue(currentY, colIndex++, row["quantity"].ToString());
				if (_printSettings.ShowAdvertisingInfo)
					SetCellValue(currentY, colIndex, $"{row["name"]} - {row["advertTypeName"]}");
				else
                    SetCellValue(currentY, colIndex, row["name"].ToString());
                _columnWithRollerName = colIndex;
				currentY++;
			}
		}

		private void PrintHeader(Action a, Agency agency, string mmNames, string mmIds)
		{
			currentY++;

            SetCellValue(currentY++, 1, string.Format("Заказчик: {0}", a.Firm.PrefixWithName));
			if (agency != null)
				SetCellValue(currentY++, 1, string.Format("Исполнитель: {0}", agency.PrefixWithName));
			else
                // TODO: Тут явно неправильно, так как теперь идентификаторы агентства и радиостанции не совпадают!
                SetCellValue(currentY++, 1, string.Format("Исполнители: {0}", action.GetAgenciesString(mmIds)));

			StringBuilder massmediaNames = new StringBuilder();
            StringBuilder groupNames = new StringBuilder();


            string[] radioStationsID = mmIds.Split(',');
			foreach (string item in radioStationsID)
			{
				if(StringUtil.IsNullOrEmpty(item)) continue;

				Massmedia m = Massmedia.GetMassmediaByID(int.Parse(item));
				if (groupNames.Length > 0) groupNames.Append(", ");
				groupNames.Append(m.GroupName);

                if (massmediaNames.Length > 0) massmediaNames.Append(", ");
                massmediaNames.Append(m.MassmediaName);

            }

            SetCellValue(currentY++, 1, string.Format("Радиостанция: {0}", mmNames));
            SetCellValue(currentY++, 1, string.Format("СМИ: {0}", massmediaNames.ToString()));
            SetCellValue(currentY++, 1, string.Format("Территория распространения: {0}", groupNames.ToString()));
        }

		private void PrintCaption(int actionID, int x, int y)
		{
			activeSheet.SetStyleForRange(y, x, y, x, true, true, 12);
            if (_selectively)
            {
                SetCellValue(y, x, string.Format("Частичный график размещения для рекламной акции № {0}", actionID));
            }
            else
            {
                SetCellValue(y, x, string.Format("График размещения для рекламной акции № {0}", actionID));
            }
		}
				
		private void SetCellValue(int rowIndex, int colIndex, object value)
		{
			activeSheet.SetCellValue(rowIndex, colIndex, value);
		}

		private void SetPageOrientation()
		{
			activeSheet.SetLandscapeOrientation();
		}
	}
}