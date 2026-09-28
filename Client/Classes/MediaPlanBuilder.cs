using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using DataTable = System.Data.DataTable;

namespace Merlin.Classes
{
	/// <summary>
	/// Построитель медиаплана («График размещения»): данные из БД и раскладка
	/// листов. Пишет в любой <see cref="IExportDocument"/> — в десктопе это Excel
	/// через COM (<see cref="MediaPlan"/>), в вебе будет OpenXml. Диалогов не
	/// показывает: настройки печати и выбранные ролики задаёт вызывающий.
	/// Справочник — docs/mediaplan.md.
	/// </summary>
	internal class MediaPlanBuilder
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
        private readonly bool _selectively;
        private IExportDocument _document;
        private bool _sheetCreated;

		private int _columnWithRollerName;

		// Кэши на время одной генерации медиаплана. PrintFooter в сводном режиме
		// перебирает все кампании всех акций на КАЖДОМ листе станции и раньше
		// заново грузил их из БД (Campaigns): на 4 акциях / 14 станциях — 544
		// вызова, ~12 c. Список кампаний и сами объекты Campaign в пределах одного
		// экспорта не меняются, поэтому грузим один раз.
		private List<DataRow> _actionCampaignRowsCache;
		private readonly Dictionary<int, Campaign> _campaignByIdCache = new Dictionary<int, Campaign>();

		/// <summary>Настройки печати (подписи, предмет рекламы, скрыть тариф).</summary>
		public PrintSettings Settings { get; set; } = new PrintSettings { PrintWithSignatures = false };

		/// <summary>
		/// Выбранные ролики для выборочной печати — «id,id,» (@rollerIDString).
		/// Учитывается только если построитель создан с selectively = true.
		/// </summary>
		public string SelectedRollers { get; set; }

        public MediaPlanBuilder(Action action, IList<Campaign> campaigns, IList<DateTime> monthes, DateTime? from, DateTime? to, bool selectively, IList<Action> actions)
		{
			this.campaigns = campaigns;
			this.monthes = monthes;
			this.action = action;
			_actions = actions;
			_dateFrom = from;
			_dateTo = to;
            _selectively = selectively;
		}

		/// <summary>Выборочная печать по роликам.</summary>
		public bool Selectively => _selectively;

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

		/// <summary>Имя файла без папки: «График размещения … для {фирма}.xlsx».</summary>
		public string FileName
		{
			get
			{
				string safeFirm = GetFirmName();
				foreach (char c in Path.GetInvalidFileNameChars())
					safeFirm = safeFirm.Replace(c, '_');
				return IsMultiActionMode
					? $"График размещения по нескольким акциям № {ActionIdsLabel} для {safeFirm}.xlsx"
					: $"График размещения для рекламной акции № {GetActionId()} для {safeFirm}.xlsx";
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

		/// <summary>
		/// Ролики, из которых выбирают при выборочной печати: rollerID, name
		/// (DefaultView отсортирован по имени).
		/// </summary>
		public DataTable GetRollers()
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

            return dataTable;
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
            procParameters.Add("isFact", true);

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

		/// <summary>
		/// Строит медиаплан в документ. Листы создаются по мере появления данных;
		/// false — данных нет, ни одного листа не создано.
		/// </summary>
		public bool Build(IExportDocument document)
		{
			_document = document;
			_sheetCreated = false;

			if (IsActionMode)
			{
				PrintActionInfo();
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
								PrintCampaignInfo(campaign, time.Year, time.Month);
						}
					}
					else
						PrintCampaignInfo(campaign, null, null);
				}
			}
			return _sheetCreated;
		}

		private void PrintActionInfo()
		{
			// Загружаем сырой датасет напрямую, чтобы получить agencyID
			Dictionary<string, object> parametersMM = new Dictionary<string, object>();
			if (IsMultiActionMode)
				parametersMM["actionIDString"] = ActionIdString;
			else
				parametersMM[Merlin.Classes.Action.ParamNames.ActionId] = action.ActionId;
			parametersMM["isFact"] = true;
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

				IDictionary<string, string> mms = GetAgencyMassmedias(
					agencyRows[agencyId], dsRaw.Tables.Count > 1 ? dsRaw.Tables[1] : null);

				bool printedHeader = false;
				foreach (KeyValuePair<string, string> mm in mms)
				{
					DataSet ds;
					if (LoadData(null, mm.Key, null, null, agencyId, out ds))
					{
						if (!printedHeader)
						{
							currentY = 2;
							activeSheet = NewSheet(SafeSheetName(agency.Name));
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
						PrintContent(ds, null, agency, mm.Key, null, null);
						currentY += 3;
					}
				}
			}
		}

		// Станции листа агентства, по блоку на каждую: сначала станции из строк
		// агентства в порядке первого появления, затем станции спонсорских кампаний
		// акции (второй набор GetUniqueMMsForAction), которых среди них нет, — по
		// возрастанию ID. Ключ — «id,» (формат @massmediaIDString), значение — имя.
		// Раньше это делал MediaPlanCampaignGroups; его склейка станций с одинаковыми
		// роликами/днями была отключена (CompareTo всегда 1), порядок тот же.
		private static IDictionary<string, string> GetAgencyMassmedias(IEnumerable<DataRow> agencyRows, DataTable sponsorMassmedias)
		{
			var result = new Dictionary<string, string>();
			var ids = new HashSet<int>();
			foreach (DataRow row in agencyRows)
			{
				int id = int.Parse(row["massmediaID"].ToString());
				if (ids.Add(id))
					result.Add(id + ",", row["name"].ToString());
			}
			if (sponsorMassmedias != null)
			{
				foreach (DataRow row in sponsorMassmedias.Rows.Cast<DataRow>()
					.OrderBy(r => int.Parse(r[Massmedia.ParamNames.MassmediaId].ToString())))
				{
					int id = int.Parse(row[Massmedia.ParamNames.MassmediaId].ToString());
					if (ids.Add(id))
						result.Add(id + ",", row[Massmedia.ParamNames.Name].ToString());
				}
			}
			return result;
		}

		private IDocumentSheet NewSheet(string name)
		{
			_sheetCreated = true;
			return _document.GetNewSheet(name, "Tahoma", 8);
		}

		private void PrintCampaignInfo(Campaign campaign, int? year, int? month)
		{
			bool printedHeader = false;

            IDictionary<String, String> mms = new Dictionary<String, String>();
            if (campaign.CampaignType == Campaign.CampaignTypes.PackModule)
            {
                CampaignPackModule campaignPackModule = campaign as CampaignPackModule;
                mms = campaignPackModule.GetUniqueMassmedias();
            }
            else
            {
                Massmedia mm = ((CampaignOnSingleMassmedia)campaign).Massmedia;
                mms.Add(mm.MassmediaId.ToString() + ',', mm.NameWithoutGroup);
            }

            foreach (KeyValuePair<string, string> mm in mms)
            {
                DataSet ds;
                if (LoadData(campaign, mm.Key, year, month, null, out ds))
                {
                    if (!printedHeader)
                    {
                        currentY = 2;
                        activeSheet = NewSheet(SafeSheetName(GetSheetName(campaign, year, month)));
                        SetPageOrientation();

                        printedHeader = true;
                    }

                    PrintCaption(campaign.ActionId.Value, 3, currentY);
					currentY++;
                    PrintHeader(campaign.Action, campaign.Agency, mm.Value, mm.Key);
                    currentY++;
                    PrintContent(ds, campaign, campaign.Agency, mm.Key, year, month);
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

		// Excel не принимает в имени листа []:*?/\ и больше 31 символа — COM
		// бросает исключение и экспорт обрывается на середине.
		private static string SafeSheetName(string name)
		{
			var sb = new StringBuilder(name ?? string.Empty);
			foreach (char c in new[] { '[', ']', ':', '*', '?', '/', '\\' })
				sb.Replace(c, '_');
			// Апостроф в начале или конце имени Excel тоже не принимает.
			string safe = sb.ToString().Trim('\'');
			return safe.Length > 31 ? safe.Substring(0, 31) : safe;
		}

		private void PrintContent(DataSet ds, Campaign campaign, Agency agency, string mmIds, int? year, int? month)
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

        private bool LoadData(Campaign campaign, string mmIds, int? year, int? month, int? agencyId, out DataSet ds)
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
            procParameters.Add("isFact", true);

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
                procParameters.Add("rollerIDString", SelectedRollers);
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
			// Начало периода блока — дата, на которую берётся ставка НДС для подписи.
			DateTime? periodStart = null;
			if (campaign != null)
			{
				DateTime start = isByMounth ? new DateTime(year.Value, month.Value, 1) : isByPeriod ? _dateFrom.Value : campaign.StartDate;
				periodStart = start;
				DateTime finish = isByMounth ? new DateTime(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value)) : isByPeriod ? _dateTo.Value : campaign.FinishDate;
				foreach (string id in ids)
				{
					campaign.GetPriceByPeriodWithTax(start, finish, int.Parse(id), false, SelectedRollers, out decimal price, out decimal tariffPrice, out decimal taxPrice);
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
							if (periodStart == null || start < periodStart)
								periodStart = start;
							DateTime finish = isByMounth
							                  	? new DateTime(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value))
												: isByPeriod ? _dateTo.Value : c.FinishDate;

                            c.GetPriceByPeriodWithTax(start, finish, int.Parse(id), false, SelectedRollers, out decimal price, out decimal tariffPrice, out decimal taxPrice);
                            priceTotal += price;
                            tariffPriceTotal += tariffPrice;
                            taxPriceTotal += taxPrice;
						}
					}
				}
			}

			// Итоговый блок футера — подряд идущие строки столбца 3, пишем одним
			// SetValuesForRange вместо 3-6 отдельных SetCellValue.
			int totalDuration = dtTimeList.Rows.Count > 0 ? int.Parse(dtTimeList.Compute("sum(totalDuration)", string.Empty).ToString()) : 0;
			decimal discount = 1 - (tariffPriceTotal == 0 ? 1 : (priceTotal / tariffPriceTotal));
			var footLines = new System.Collections.Generic.List<object>
			{
				string.Format("Всего трансляций: {0}", dtIssues.Rows.Count),
				string.Format("Время трансляций: {0}", DateTimeUtils.Time2String(totalDuration)),
			};
			if (!Settings.HideTariffPrice)
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
				footLines.Add(TaxLine(agency, periodStart, taxPriceTotal));
			WriteColumn(currentY, 3, footLines);
			currentY += footLines.Count;
            currentY++;
			SetCellValue(currentY, 3, "Исполнитель:");

			if (agency != null && Settings.PrintWithSignatures && agency.SignatureBytes != null)
			{
                activeSheet.InsertImage(currentY, 7, agency.SignatureBytes);
            }

			currentY += 4;
			SetCellValue(currentY, 3, "Заказчик:");

			currentY += 2;
			if (campaign != null && ConfigurationUtil.IsPrintContactPerson)
				SetCellValue(currentY, 3, string.Format("Контактное лицо: {0}", campaign.Action.Creator.ContactInfo));
        }

		// Ставка — из AgencyTax агентства на начало периода блока (раньше в тексте
		// было жёстко «5%»). Сама сумма НДС считается в GetPriceByPeriod по ставке
		// на дату каждого выпуска. Если на начало периода ставки нет (действует с
		// середины периода), процент не пишем.
		private static string TaxLine(Agency agency, DateTime? periodStart, decimal taxPriceTotal)
		{
			decimal rate = agency != null && periodStart.HasValue ? agency.GetTaxValue(periodStart.Value) : 0;
			return rate > 0
				? $"В том числе НДС ({rate:0.##}%): {taxPriceTotal:c}"
				: $"В том числе НДС: {taxPriceTotal:c}";
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
			SheetWriter.PopulateWorksheet(data, left, currentY, activeSheet);
            SheetWriter.CopyData2WorkSheet(activeSheet, dataCounts, left, currentY + rowsCount + 2, true);
                        
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
			SheetWriter.CopyData2WorkSheet(activeSheet, dtTimes, 1, ++currentY);
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
			// Без роликов в блоке таблицу времени и сетку не рисуем (PrintContent),
			// а не берём ширину от предыдущего блока.
			_columnWithRollerName = 0;
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
				block[r, 4] = Settings.ShowAdvertisingInfo
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
