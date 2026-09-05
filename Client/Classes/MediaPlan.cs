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
using System.IO;
using System.Linq;
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
		// Сводный медиаплан по набору акций («График размещения по нескольким
		// акциям»): выпуски всех этих акций печатаются как одна большая акция.
		private readonly IList<Action> _actions;
		private readonly DateTime? _dateFrom;
		private readonly DateTime? _dateTo;
		private bool _isFact;
        private readonly bool _selectively;
        private bool exportStarted = false;

        private string _selectedRollers = null;
		private int _columnWithRollerName;

		// Кэши на время одной генерации медиаплана. PrintFooter в сводном режиме
		// перебирает все кампании всех акций на КАЖДОМ листе станции и раньше
		// заново грузил их из БД (Campaigns): на 4 акциях / 14 станциях — 544
		// вызова, ~12 c. Список кампаний и сами объекты Campaign в пределах одного
		// экспорта не меняются, поэтому грузим один раз.
		private List<DataRow> _actionCampaignRowsCache;
		private readonly Dictionary<int, Campaign> _campaignByIdCache = new Dictionary<int, Campaign>();
		private PrintSettings _printSettings = new PrintSettings() { 
			PrintWithSignatures = false, SaveDirectlyToDisk = !string.IsNullOrEmpty(UserSettings.Load("Path2SaveReports")) 
		};
		private string _savedFilePath;

		#region Singleton

        private MediaPlan(Action action, IList<Campaign> campaigns, IList<DateTime> monthes, DateTime? from, DateTime? to, bool selectively)
			: this(action, campaigns, monthes, from, to, selectively, null)
		{
		}

        private MediaPlan(Action action, IList<Campaign> campaigns, IList<DateTime> monthes, DateTime? from, DateTime? to, bool selectively, IList<Action> actions)
		{
			this.campaigns = campaigns;
			this.monthes = monthes;
			this.action = action;
			_actions = actions;
			_dateFrom = from;
			_dateTo = to;
            _selectively = selectively;
		}

        public static MediaPlan CreateInstance(IList<Action> actions, bool selectively)
		{
			return new MediaPlan(null, null, null, null, null, selectively, actions);
		}

		/// <summary>Режим сводного плана по набору акций.</summary>
		private bool IsMultiActionMode => _actions != null;

		/// <summary>Работаем «от акции» (одиночной или набора), а не от кампаний.</summary>
		private bool IsActionMode => action != null || _actions != null;

		/// <summary>Список акций через запятую с хвостовой запятой — для @actionIDString.</summary>
		private string ActionIdString => string.Join(",", _actions.Select(a => a.ActionId)) + ",";

		/// <summary>«123, 456, 789» — для заголовка листа.</summary>
		private string ActionIdsLabel => string.Join(", ", _actions.Select(a => a.ActionId).OrderBy(id => id));

		/// <summary>Заказчики всех акций, без повторов — «Фирма1, Фирма2».</summary>
		private string ActionFirmsString =>
			string.Join(", ", _actions.Select(a => a.Firm.PrefixWithName).Distinct());

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
			isFact = true;

			_isFact = isFact;
			_savedFilePath = null;
            CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;
            // Проверка пути сохранения может уходить в недоступный сетевой каталог и висеть
            // несколько секунд, поэтому курсор ожидания ставим до неё.
            Application.UseWaitCursor = true;
            Application.DoEvents();
            try
			{
                string savedPath = UserSettings.Load("Path2SaveReports");
				bool pathIsSet = !string.IsNullOrWhiteSpace(savedPath) && Directory.Exists(savedPath);
				var frmSettings = new Forms.PrintMediaPlanSettings(pathIsSet);

				// В самом диалоге курсор обычный, ожидание возобновляем на время экспорта.
				Application.UseWaitCursor = false;
				if(frmSettings.ShowDialog(Globals.MdiParent) == DialogResult.Cancel) return;
				_printSettings = frmSettings.Settings;

				Application.UseWaitCursor = true;
				Application.DoEvents();

                // Экспорт идёт синхронно на UI-потоке (STA): Excel создаётся и
                // освобождается на одном апартаменте — без маршалинга между потоками,
                // из-за которого процесс EXCEL.EXE раньше зависал в памяти.
                ExportMediaPlan();

				Application.UseWaitCursor = false;
				if (!string.IsNullOrEmpty(_savedFilePath))
				{
					UserMessage.ShowCompleted($"Файл успешно сохранён: {_savedFilePath}");
				}
			}
			catch(Exception e)
			{
				// Экспорт мог упасть на середине, когда Excel ещё скрыт (ScreenUpdating=false).
				// Показываем то, что успело записаться, чтобы не оставлять окно без владельца.
				if (exportStarted)
				{
					try { ExportManager.Application.FinishExport(); }
					catch { }
				}
				ErrorManager.LogError("Error to show media plan", e);
			}
			finally
			{
                Thread.CurrentThread.CurrentCulture = oldCulture;
                Application.UseWaitCursor = false;
			}
		}

        private void ExportMediaPlan()
		{
			PrintMediaPlan(_isFact);
            if (exportStarted)
            {
				string folder = UserSettings.Load("Path2SaveReports") ?? string.Empty;
				bool canSaveToDisk = _printSettings.SaveDirectlyToDisk
					&& !string.IsNullOrEmpty(folder)
					&& Directory.Exists(folder);

				if (canSaveToDisk)
				{
					string firmName = GetFirmName();
					string safeFirm = firmName;
					foreach (char c in Path.GetInvalidFileNameChars())
						safeFirm = safeFirm.Replace(c, '_');
					string fileName = IsMultiActionMode
						? $"График размещения по нескольким акциям № {ActionIdsLabel} для {safeFirm}.xlsx"
						: $"График размещения для рекламной акции № {GetActionId()} для {safeFirm}.xlsx";
					string filePath = Path.Combine(folder, fileName);

					ExportManager.Application.SaveToDisk(filePath);
					_savedFilePath = filePath;
				}
				else
				{
					ExportManager.Application.FinishExport();
				}
            }
		}

		private string GetFirmName()
		{
			if (_actions != null)
				return ActionFirmsString;
			if (action != null)
				return action.Firm.PrefixWithName;
			if (campaigns != null && campaigns.Count > 0)
				return campaigns[0].Action.Firm.PrefixWithName;
			return string.Empty;
		}

		private int GetActionId()
		{
			if (action != null)
				return action.ActionId;
			if (campaigns != null && campaigns.Count > 0)
				return campaigns[0].ActionId.Value;
			return 0;
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

			if (IsActionMode)
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
			// Загружаем сырой датасет напрямую, чтобы получить agencyID
			Dictionary<string, object> parametersMM = new Dictionary<string, object>();
			if (IsMultiActionMode)
				parametersMM["actionIDString"] = ActionIdString;
			else
				parametersMM[Merlin.Classes.Action.ParamNames.ActionId] = action.ActionId;
			parametersMM["isFact"] = isFact;
			DataSet dsRaw = DataAccessor.LoadDataSet("GetUniqueMMsForAction", parametersMM);
			DataTable dt = dsRaw.Tables[0];

			// Группируем строки по agencyID, сохраняя порядок первого появления
			var agencyRows = new Dictionary<int, List<DataRow>>();
			var agencyOrder = new List<int>();
			foreach (DataRow row in dt.Rows)
			{
				int agencyId = int.Parse(row["agencyID"].ToString());
				if (!agencyRows.ContainsKey(agencyId))
				{
					agencyRows[agencyId] = new List<DataRow>();
					agencyOrder.Add(agencyId);
				}
				agencyRows[agencyId].Add(row);
			}

			// Для каждого агентства — отдельный лист Excel
			foreach (int agencyId in agencyOrder)
			{
				Agency agency = Agency.GetAgencyByID(agencyId);

				// Строим MediaPlanCampaignGroups только из строк этого агентства
				MediaPlanCampaignGroups mp = new MediaPlanCampaignGroups();
				if (dsRaw.Tables.Count > 1)
					mp.InitUniquesList(dsRaw.Tables[1]);
				foreach (DataRow row in agencyRows[agencyId])
					mp.AddMassmedia(
						int.Parse(row["massmediaID"].ToString()),
						row["name"].ToString(),
						int.Parse(row["rollerID"].ToString()),
						DateTime.Parse(row["date"].ToString()));

				IDictionary<string, string> mms = mp.GetUniqueMassmedias();

				bool printedHeader = false;
				foreach (KeyValuePair<string, string> mm in mms)
				{
					DataSet ds;
					if (LoadData(null, mm.Key, isFact, null, null, agencyId, out ds))
					{
						if (!printedHeader)
						{
							currentY = 2;
							VerifyExportManager();
							activeSheet = ExportManager.Application.GetNewSheet(agency.Name, "Tahoma", 8);
							SetPageOrientation();
							printedHeader = true;
						}

						if (IsMultiActionMode)
						{
							PrintCaption(ActionIdsLabel, 3, currentY);
							currentY++;
							PrintHeader(_actions[0], agency, mm.Value, mm.Key, ActionFirmsString);
						}
						else
						{
							PrintCaption(action.ActionId, 3, currentY);
							currentY++;
							PrintHeader(action, agency, mm.Value, mm.Key);
						}
						PrintContent(ds, null, agency, mm.Key, isFact, null, null);
						currentY += 3;
					}
				}
			}
		}

        private void VerifyExportManager()
        {
            if (!exportStarted)
            {
                ExportManager.StartNewApplication();
                ExportManager.Application.StartExport();
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
                if (LoadData(campaign, mm.Key, isFact, year, month, null, out ds))
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
                    PrintContent(ds, campaign, campaign.Agency, mm.Key, isFact, year, month);
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

		private void PrintContent(DataSet ds, Campaign campaign, Agency agency, string mmIds, bool isFact, int? year, int? month)
		{
			PrintRollersList(ds.Tables[0], campaign == null ? Campaign.CampaignTypes.Module : campaign.CampaignType);
			if ((campaign != null && campaign.CampaignType == Campaign.CampaignTypes.Sponsor) || (campaign == null && IsActionMode))
				PrintPrograms(ds.Tables[4]);
			currentY++;
			if (_columnWithRollerName > 0)
			{
				PrintTimeList(ds.Tables[1], campaign == null ? Campaign.CampaignTypes.Module : campaign.CampaignType);
				PrintIssuesGrid(ds.Tables[1].Rows.Count, ds.Tables[2], ds.Tables[3], campaign == null ? Campaign.CampaignTypes.Module : campaign.CampaignType, year, month);
			}
			PrintFooter(campaign, agency, ds.Tables[1], ds.Tables[2], mmIds, year, month);
		}

        private bool LoadData(Campaign campaign, string mmIds, bool isFact, int? year, int? month, int? agencyId, out DataSet ds)
        {
            Dictionary<string, object> procParameters = new Dictionary<string, object>(2);
			if(agencyId != null) 
                procParameters.Add("agencyId", agencyId);

            if (campaign != null)
            {
                procParameters.Add("campaignId", campaign.CampaignId);
                procParameters.Add("campaignTypeId", (int)campaign.CampaignType);
            }
            else if (IsMultiActionMode)
            {
                procParameters.Add("actionIDString", ActionIdString);
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
			int count = dtProgIssues.Rows.Count;
			if (count == 0)
			{
				WriteRow(currentY, 3, new object[] { "Программы:" });
				return;
			}
			// Блок: count строк, колонки [3..6]. Строка 0: "Программы:" + первая
			// программа; строки 1..N-1: дата / время / название.
			var block = new object[count, 4];
			block[0, 0] = "Программы:";
			int r = 0;
			foreach (DataRow row in dtProgIssues.Rows)
			{
				DateTime issueDate = DateTime.Parse(row["issueDate"].ToString());
				block[r, 1] = issueDate.ToShortDateString();
				block[r, 2] = issueDate.ToShortTimeString();
				block[r, 3] = row["name"];
				r++;
			}
			activeSheet.SetValuesForRange(currentY, 3, currentY + count - 1, 6, block);
			currentY += count;
		}

		// Кампании всех акций плана (одной или набора) — для подсчёта стоимости.
		// Материализуется один раз за экспорт: PrintFooter зовёт это на каждом
		// листе станции, а a.Campaigns() каждый раз идёт в БД.
		private List<DataRow> ActionCampaignRows()
		{
			if (_actionCampaignRowsCache == null)
				_actionCampaignRowsCache = _actions != null
					? _actions.SelectMany(a => a.Campaigns().Rows.Cast<DataRow>()).ToList()
					: action.Campaigns().Rows.Cast<DataRow>().ToList();
			return _actionCampaignRowsCache;
		}

		// Campaign.GetCampaignById идёт в БД (Refresh, иногда дважды). В пределах
		// одного медиаплана объект кампании не меняется — кэшируем.
		private Campaign GetCampaignByIdCached(int campaignId)
		{
			if (!_campaignByIdCache.TryGetValue(campaignId, out Campaign campaign))
			{
				campaign = Campaign.GetCampaignById(campaignId);
				_campaignByIdCache[campaignId] = campaign;
			}
			return campaign;
		}

		private void PrintFooter(Campaign campaign, Agency agency, DataTable dtTimeList, DataTable dtIssues, string mmIds, int? year, int? month)
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
					foreach (DataRow row in ActionCampaignRows())
					{
						Campaign c = GetCampaignByIdCached(int.Parse(row["campaignID"].ToString()));
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

			// Итоговый блок футера — подряд идущие строки столбца 3, пишем одним
			// SetValuesForRange вместо 3-6 отдельных SetCellValue.
			int totalDuration = dtTimeList.Rows.Count > 0 ? ids.Length * int.Parse(dtTimeList.Compute("sum(totalDuration)", string.Empty).ToString()) : 0;
			decimal discount = 1 - (tariffPriceTotal == 0 ? 1 : (priceTotal / tariffPriceTotal));
			var footLines = new System.Collections.Generic.List<object>
			{
				string.Format("Всего трансляций: {0}", dtIssues.Rows.Count * ids.Length),
				string.Format("Время трансляций: {0}", DateTimeUtils.Time2String(totalDuration)),
			};
			if (!_printSettings.HideTariffPrice)
			{
				if (discount == decimal.Zero)
					footLines.Add($"Стоимость спланированной рекламы: {priceTotal:c}");
				footLines.Add($"Стоимость спланированной рекламы по тарифам: {tariffPriceTotal:c}");
				if (discount != decimal.Zero)
				{
					footLines.Add(string.Format("Скидка: {0}", discount.ToString("P")));
					footLines.Add($"Стоимость спланированной рекламы с учетом скидки: {priceTotal:c}");
				}
			}
			else
			{
				footLines.Add($"Стоимость спланированной рекламы: {priceTotal:c}");
			}
			if (taxPriceTotal > 0)
				footLines.Add($"В том числе  НДС  (5%): {taxPriceTotal:c}");
			WriteColumn(currentY, 3, footLines);
			currentY += footLines.Count;
            currentY++;
			SetCellValue(currentY, 3, "Исполнитель:");

			if (agency != null && _printSettings.PrintWithSignatures && agency.SignatureBytes != null)
			{
                activeSheet.InsertImage(currentY, 7, agency.SignatureBytes);
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
			bool simple = campaignType == Campaign.CampaignTypes.Simple;
			WriteRow(currentY, 1, simple
				? new object[] { "Время", "Коммент.", "Цена", "Прод-ть" }
				: new object[] { "Время", "Коммент.", "Прод-ть" });
			activeSheet.SetBoldForRange(currentY, 1, currentY, 3 + (simple ? 1 : 0));
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
			colRollers = new Dictionary<int, int>(dtRollers.Rows.Count);
			int labelCol = type == Campaign.CampaignTypes.Simple ? 4 : 3;   // "Ролики:"
			int dataCol = labelCol + 1;                                     // №, длит., кол-во, имя
			int rollerCount = dtRollers.Rows.Count;

			if (rollerCount == 0)
			{
				WriteRow(currentY, labelCol, new object[] { "Ролики:" });
				return;
			}

			// Блок: rollerCount строк, колонки [labelCol .. dataCol+3].
			// Строка 0: "Ролики:" + данные ролика 0; строки 1..N-1: данные ролика i.
			var block = new object[rollerCount, 5];
			block[0, 0] = "Ролики:";
			int index = 1;
			int r = 0;
			foreach (DataRow row in dtRollers.Rows)
			{
				colRollers.Add(int.Parse(row["rollerId"].ToString()), index);
				block[r, 1] = string.Format("№{0}", index++);
				block[r, 2] = DateTimeUtils.Time2String(int.Parse(row["duration"].ToString()));
				block[r, 3] = row["quantity"].ToString();
				block[r, 4] = _printSettings.ShowAdvertisingInfo
					? $"{row["name"]} - {row["advertTypeName"]}"
					: row["name"].ToString();
				r++;
			}
			_columnWithRollerName = dataCol + 3;
			activeSheet.SetValuesForRange(currentY, labelCol, currentY + rollerCount - 1, labelCol + 4, block);
			currentY += rollerCount;
		}

		private void PrintHeader(Action a, Agency agency, string mmNames, string mmIds, string customerNamesOverride = null)
		{
			currentY++;

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

            var lines = new System.Collections.Generic.List<object>
            {
                string.Format("Заказчик: {0}", customerNamesOverride ?? a.Firm.PrefixWithName),
                agency != null
                    ? string.Format("Исполнитель: {0}", agency.PrefixWithName)
                    // TODO: Тут явно неправильно, так как теперь идентификаторы агентства и радиостанции не совпадают!
                    : string.Format("Исполнители: {0}", action.GetAgenciesString(mmIds)),
                string.Format("Радиостанция: {0}", mmNames),
                string.Format("СМИ: {0}", massmediaNames.ToString()),
                string.Format("Территория распространения: {0}", groupNames.ToString()),
            };
            WriteColumn(currentY, 1, lines);
            currentY += lines.Count;
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

		private void PrintCaption(string actionsLabel, int x, int y)
		{
			activeSheet.SetStyleForRange(y, x, y, x, true, true, 12);
			SetCellValue(y, x, string.Format("График размещения по нескольким акциям № {0}", actionsLabel));
		}

		private void SetCellValue(int rowIndex, int colIndex, object value)
		{
			activeSheet.SetCellValue(rowIndex, colIndex, value);
		}

		// Каждый SetCellValue — это 2-3 маршалированных COM-вызова в EXCEL.EXE.
		// На сводном медиаплане (десятки листов станций, ~50 подписей на лист) это
		// секунды. Блок соседних ячеек одного столбца/ряда пишется одним
		// SetValuesForRange. Пропуски в блоке (null) Excel очищает — вызывать
		// только на диапазонах, которые целиком пишет этот же метод.
		private void WriteColumn(int top, int col, System.Collections.Generic.IList<object> values)
		{
			if (values == null || values.Count == 0) return;
			if (values.Count == 1) { SetCellValue(top, col, values[0]); return; }
			var data = new object[values.Count, 1];
			for (int i = 0; i < values.Count; i++) data[i, 0] = values[i];
			activeSheet.SetValuesForRange(top, col, top + values.Count - 1, col, data);
		}

		private void WriteRow(int row, int left, System.Collections.Generic.IList<object> values)
		{
			if (values == null || values.Count == 0) return;
			if (values.Count == 1) { SetCellValue(row, left, values[0]); return; }
			var data = new object[1, values.Count];
			for (int i = 0; i < values.Count; i++) data[0, i] = values[i];
			activeSheet.SetValuesForRange(row, left, row, left + values.Count - 1, data);
		}

		private void SetPageOrientation()
		{
			activeSheet.SetLandscapeOrientation();
		}
	}
}
