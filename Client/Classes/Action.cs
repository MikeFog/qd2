using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;
using Merlin.Reports;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Classes
{
	public abstract class Action : ObjectContainer
	{
		public enum ActionMediaPlanType
		{
			Month,
			Simple,
			Massmedias,
			Period
		}

		#region Constants -------------------------------------

		protected delegate void PrintAgencyDocument(Form owner, Agency agency, bool doExport);

		public struct ActionNames
		{
			public const string PrintBill = "PrintBill";
			public const string PrintBillByMounth = "PrintBillByMounth";
			public const string ExportBill = "ExportBill";
			public const string PrintBillMedia = "PrintBillMedia";
			public const string PrintContractMedia = "PrintContractMedia";
			public const string PrintContract = "PrintContract";
			public const string PrintSponsorContract = "PrintSponsorContract";
			public const string PrintMediaPlan = "PrintMediaPlan";
			public const string PrintMediaPlanMonth = "PrintMediaPlanMonth";
			public const string PrintMediaPlanByPeriod = "PrintMediaPlanByPeriod";
			public const string PrintSelectivelyMediaPlan = "PrintSelectivelyMediaPlan";
			public const string PrintSelectivelyMediaPlanMonth = "PrintSelectivelyMediaPlanMonth";
			public const string PrintSelectivelyMediaPlanByPeriod = "PrintSelectivelyMediaPlanByPeriod";
			public const string ExportContract = "ExportContract";

			public const string PrintAgreement = "PrintAgreement";
			public const string ExportAgreement = "ExportAgreement";

			public const string ChangeFirm = "ChangeFirm";
			public const string ChangeCreator = "ChangeCreator";
			public const string ChangeOwner = "ChangeOwner";
			public const string ImportCampaign = "ImportCampaignMediaPlus";

			public const string MarkAsNotReady = "MarkAsNotReady";
			public const string MarkAsReady = "MarkAsReady";

			public const string Activate = "Activate";
			public const string ActivateTest = "ActivateTest";
			public const string Deactivate = "Deactivate";
			public const string Merge = "Merge";
			public const string Recalculate = "Recalculate";
			public const string ActionRollers = "ActionRollers";
			public const string Clone = "Clone";
			public const string SplitAction = "SplitAction";
			public const string SplitCampaigns = "SplitCampaigns";
			public const string Restore = "Restore";
			public const string SetAdvertType = "SetAdvertType";
			public const string PrintBillContract = "Bill-Contract";
		}

		public struct ParamNames
		{
			public const string ActionId = "actionID";
			public const string ManagerID = "managerID";
			public const string FirmId = "firmID";
			public const string FirmName = "firmName";
			public const string TariffPrice = "TariffPrice";
			public const string Discount = "Discount";
			public const string Ratio = "Ratio";
			public const string TotalPrice = "TotalPrice";
			public const string NewCreatorId = "NewCreatorID";
			public const string FinishDate = "finishDate";
			public const string StartDate = "startDate";
			public const string IsConfirmed = "isConfirmed";
			public const string StatusID = "orderStatusID";
			public const string StatusName = "orderStatusName";
			public const string DeleteDate = "deleteDate";
		}

		#endregion

		protected Firm firm;

		#region Constructors ----------------------------------

		protected Action(Entity entity)
			: base(entity)
		{
			parameters[ParamNames.ActionId] = "-1";
			parameters[SecurityManager.ParamNames.UserId] = SecurityManager.LoggedUser.Id.ToString();
			isNew = false;
		}

		protected Action(Entity entity, int actionId)
			: base(entity)
		{
			parameters[ParamNames.ActionId] = actionId.ToString();
			isNew = false;
		}

		protected Action(Entity entity, DataRow row)
			: base(entity, row)
		{
			//this.parameters[ParamNames.ActionID] = -1;
		}

		protected Action(Entity entity, PresentationObject firm)
			: base(entity)
		{
			parameters[ParamNames.FirmId] = firm.IDs[0].ToString();
			parameters[ParamNames.FirmName] = firm.Name;
			parameters[Constants.Parameters.Name] = firm.Name;
			parameters[ParamNames.ActionId] = "-1";
			parameters[SecurityManager.ParamNames.UserId] = SecurityManager.LoggedUser.Id.ToString();
		}

		#endregion

		#region Public ----------------------------------------
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			try
			{
				if (actionName == ActionNames.ChangeFirm)
					ChangeFirm((Form)owner);
				else if (actionName == ActionNames.ChangeCreator)
					ChangeCreator((Form)owner);
				else if (actionName == ActionNames.PrintContract || actionName == ActionNames.ExportContract)
					PrintContracts((Form)owner, actionName == ActionNames.ExportContract);
				else if (actionName == ActionNames.PrintSponsorContract)
					PrintSponsorContracts((Form)owner, false);
				else if (actionName == ActionNames.PrintBill)
					PrintBills((Form)owner, false, false);
				else if (actionName == ActionNames.PrintBillByMounth)
					PrintBills((Form)owner, true, false);
				else if (actionName == ActionNames.PrintMediaPlan)
					PrintMediaPlan(ActionMediaPlanType.Simple, false);
				else if (actionName == ActionNames.PrintMediaPlanMonth)
					PrintMediaPlan(ActionMediaPlanType.Month, false);
				else if (actionName == ActionNames.PrintMediaPlanByPeriod)
					PrintMediaPlan(ActionMediaPlanType.Period, false);
				else if (actionName == ActionNames.PrintSelectivelyMediaPlan)
					PrintMediaPlan(ActionMediaPlanType.Simple, true);
				else if (actionName == ActionNames.PrintSelectivelyMediaPlanMonth)
					PrintMediaPlan(ActionMediaPlanType.Month, true);
				else if (actionName == ActionNames.PrintSelectivelyMediaPlanByPeriod)
					PrintMediaPlan(ActionMediaPlanType.Period, true);
				else if (actionName == ActionNames.SetAdvertType)
					SetAdvertTypeOrSubstituteRoller();
				else if (actionName == ActionNames.PrintBillContract)
					PrintBillContracts((Form)owner, false);
				else
					base.DoAction(actionName, owner, interfaceObject);
			}
			finally
			{
				((Control)owner).Cursor = Cursors.Default;
			}
		}

		public DataTable Campaigns(bool forceLoad = false)
		{
			DataAccessor.PrepareParameters(parameters, ChildEntity, InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			return ((DataSet)DataAccessor.DoAction(parameters, forceLoad)).Tables[0];
		}

		public int ActionId
		{
			get { return int.Parse(this[ParamNames.ActionId].ToString()); }
		}

		public string FirmName
		{
			get { return this[ParamNames.FirmName].ToString(); }
		}

		public decimal TariffPrice
		{
			get { return ParseHelper.GetDecimalFromObject(this[ParamNames.TariffPrice], 0); }
		}

		public float Discount
		{
			get { return ParseHelper.GetFloatFromObject(this[ParamNames.Discount], 0); }
		}

		public decimal TotalPrice
		{
			get { return ParseHelper.GetDecimalFromObject(this[ParamNames.TotalPrice], 0); }
		}

		public Firm Firm
		{
			get
			{
				if (firm == null)
				{
					if (!parameters.ContainsKey(ParamNames.FirmId)) Refresh();
					firm = Firm.GetFirmById(int.Parse(this[ParamNames.FirmId].ToString()));
				}
				return firm;
			}
		}

		public int FirmID
		{
			get
			{
				return ParseHelper.GetInt32FromObject(this[ParamNames.FirmId], 0);
			}
		}

		public DateTime FinishDate
		{
			get
			{
				if (!parameters.ContainsKey(ParamNames.FinishDate)) Refresh();
				if (this[ParamNames.FinishDate] == DBNull.Value)
					return DateTime.MinValue;
				return DateTime.Parse(parameters[ParamNames.FinishDate].ToString());
			}
		}

		public DateTime StartDate
		{
			get
			{
				if (!parameters.ContainsKey(ParamNames.StartDate)) Refresh();
				if (this[ParamNames.StartDate] == DBNull.Value)
					return DateTime.MinValue;
				return DateTime.Parse(parameters[ParamNames.StartDate].ToString());
			}
		}

		public int CreatorId
		{
			get { return int.Parse(parameters[SecurityManager.ParamNames.UserId].ToString()); }
		}

		public DateTime? DeleteDate
		{
			get
			{
				if (parameters[ParamNames.DeleteDate] == DBNull.Value) return null;
				return DateTime.Parse(parameters[ParamNames.DeleteDate].ToString());
			}
		}

		public SecurityManager.User Creator
		{
			get { return SecurityManager.GetUser(CreatorId); }
		}

		public void PrintContracts(Form owner, bool doExport)
		{
			PrintAgencyDocuments(owner, PrintContract, doExport);
		}

		public void PrintBillContracts(Form owner, bool doExport)
		{
			PrintAgencyDocuments(owner, PrintBillContract, doExport);
		}

		public void PrintSponsorContracts(Form owner, bool doExport)
		{
			PrintAgencyDocuments(owner, PrintSponsorContract, doExport);
		}

		public void PrintBills(Form owner, bool byMounth, bool doExport)
		{
			if (byMounth)
			{
				Refresh();
				Application.DoEvents();
				List<PresentationObject> agencies = Agency.SelectAgencies(this, Parameters, owner);
				Application.DoEvents();
				if (agencies == null) return;
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				IList<DateTime> months = GetSelectedMonths();
				if (months == null || months.Count <= 0)
					return;

				foreach (PresentationObject po in agencies)
				{
					PresentationObject bill = GetBill((Agency)po, owner);
					foreach (DateTime month in months)
					{
						Application.DoEvents();
						Agency agency = (Agency)po;
						BillReport report = new BillReport(this, agency, bill, month);
						report.Show(string.Format("Счёт на предоплату, агенство '{0}' за месяц {1} {2} года", agency.Name
							, DateTimeFormatInfo.CurrentInfo.MonthNames[month.Month - 1], month.Year));
					}
				}
			}
			else
				PrintAgencyDocuments(owner, PrintBill, false);
		}

		public void PrintMediaPlan(ActionMediaPlanType type, bool selectively)
		{
			Refresh();
			//DataSet ds = Campaigns;
			//if (ds.Tables.Count > 0)
			//{
			switch (type)
			{
				case ActionMediaPlanType.Massmedias:
					MediaPlan.CreateInstance(this, selectively).Show(false);
					break;
				case ActionMediaPlanType.Simple:
					MediaPlan.CreateInstance(GetCampaigns(Campaigns()), selectively).Show(false);
					break;
				case ActionMediaPlanType.Month:
					IList<DateTime> months = GetSelectedMonths();
					if (months == null)
						return;
					MediaPlan.CreateInstance(GetCampaigns(Campaigns()), months, selectively).Show(false);
					break;
				case ActionMediaPlanType.Period:
					FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "Выбор периода");
					if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
						MediaPlan.CreateInstance(GetCampaigns(Campaigns()), selector.StartDate, selector.FinishDate, selectively).Show(false);
					break;
			}
			//}
		}

		private static IList<Campaign> GetCampaigns(DataTable dt)
		{
			IList<Campaign> campaigns = new List<Campaign>();
			//DataTable dt = ds.Tables[Constants.TableNames.Data];
			foreach (DataRow dr in dt.Rows)
				campaigns.Add(Campaign.GetCampaignById(int.Parse(dr[Campaign.ParamNames.CampaignId].ToString())));
			return campaigns;
		}

		private IList<DateTime> GetSelectedMonths()
		{
			Dictionary<object, object> dMonthsToShow = SelectMonthsToShow();
			FrmMonths f = new FrmMonths(dMonthsToShow, false);
			if (f.ShowDialog(Globals.MdiParent) == DialogResult.Cancel)
				return null;
			IList<DateTime> months = new List<DateTime>();
			foreach (KeyValuePair<object, object> item in f.CheckedItems)
				months.Add((DateTime)item.Key);
			return months;
		}

		private Dictionary<object, object> SelectMonthsToShow()
		{
			DataSet dsMonthes = GetMonthes();

			Dictionary<object, object> dMonthsToShow = new Dictionary<object, object>();

			foreach (DataRow row in dsMonthes.Tables[0].Rows)
			{
				int month = ParseHelper.ParseToInt32(row["MonthDate"].ToString(), -1);
				int year = ParseHelper.ParseToInt32(row["MonthYear"].ToString(), -1);
				if (month >= 0 && year >= 0)
				{
					DateTime date = new DateTime(year, month, 1);
					dMonthsToShow.Add(date, date.ToString("MMMM yyy"));
				}
			}
			return dMonthsToShow;
		}

		private DataSet GetMonthes()
		{
			Dictionary<string, object> procParameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
			{
				[ParamNames.ActionId] = ActionId
			};
			return DataAccessor.LoadDataSet("GetMonthes", procParameters);
		}


		public DataSet GetMassmedias(Agency agency)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters.Add("actionID", ActionId);
			procParameters.Add("agencyID", agency.AgencyId);
			return DataAccessor.LoadDataSet("GetMassmedias", procParameters);
		}

		public bool IsConfirmed
		{
			get
			{
				if (!parameters.ContainsKey(ParamNames.IsConfirmed)) Refresh();
				return bool.Parse(parameters[ParamNames.IsConfirmed].ToString());
			}
		}

		#endregion

		#region Private -----------------------------------

		private void ChangeFirm(Control owner)
		{
			if (IsChangeFirmPossible)
			{
				Firm newFirm = Firm.SelectFirm(owner);
				if (newFirm != null)
				{
					Application.DoEvents();
					owner.Cursor = Cursors.WaitCursor;

					this[ParamNames.FirmId] = newFirm.FirmId;
					Update();
					Refresh();
					OnObjectChanged(this);
					MessageBox.ShowInformation(Properties.Resources.FirmChangeSuccess);
				}
			}
			else
				MessageBox.ShowExclamation(MessageAccessor.GetMessage("ChangeFirmIsForbidden"));
		}

		private bool IsChangeFirmPossible
		{
			get
			{
				if (SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsBookKeeper || !IsConfirmed) return true;
				// если акция началась в предыдущем месяце или ранее, то нельзя
				if (new DateTime(StartDate.Year, StartDate.Month, 1) < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)) return false;
				// если начало в этом месяце, то не должна уже закончиться
				if (FinishDate < DateTime.Today) return false;

				return true;
			}
		}

		private void ChangeCreator(Control owner)
		{
			PresentationObject manager = Utils.SelectManager(owner);
			if (manager != null)
			{
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				parameters[ParamNames.NewCreatorId] = manager.IDs[0].ToString();
				Update();
				Refresh();
			}
		}

		private void PrintAgencyDocuments(Form owner, PrintAgencyDocument doc, bool doExport)
		{
			try
			{
				Refresh();
				Application.DoEvents();

				List<PresentationObject> agencies = Agency.SelectAgencies(this, Parameters, owner);
				Application.DoEvents();

				if (agencies == null) return;
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				foreach (PresentationObject po in agencies)
					doc(owner, po as Agency, doExport);
			}
			finally
			{
				owner.Cursor = Cursors.Default;
			}
		}
		#endregion

		#region Protected -----------------------------------
		protected bool IsDeleted
		{
			get { return this[ParamNames.DeleteDate] != DBNull.Value; }
		}

		protected virtual void PrintContract(Form owner, Agency agency, bool exportReport)
		{
			PresentationObject bill = GetBill(agency, owner);
			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;

			ContractReport report = new ContractReport(this, agency, bill);
			report.Show("Договор");
		}

		protected virtual void PrintSponsorContract(Form owner, Agency agency, bool exportReport)
		{
			PresentationObject bill = GetBill(agency, owner);
			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;

			ContractReport report = new ContractReport(this, agency, bill, true);
			report.Show("Спонсорский договор");
		}

		private void PrintBillContract(Form owner, Agency agency, bool exportReport)
		{
			PresentationObject bill = GetBill(agency, owner);
			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;
			BillReport report = new BillContractReport(this, agency, bill);
			report.Show("Счёт-договор");
		}


		protected virtual void PrintBill(Form owner, Agency agency, bool exportReport)
		{
			// Load Bill data
			PresentationObject bill = GetBill(agency, owner);
			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;
			BillReport report = new BillReport(this, agency, bill);
			if (exportReport) report.Export(ReportExportFormat.WordForWindows);
			else report.Show("Счёт");
		}

		private PresentationObject GetBill(Agency agency, Form owner)
		{
			PresentationObject bill = GetBill(agency.AgencyId, EntityManager.GetEntity((int)Entities.GeneralBill));
			return CreateBill(agency, owner, EntityManager.GetEntity((int)Entities.GeneralBill), bill);
		}

		protected PresentationObject GetBill(int agencyId, Entity entityBill)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(entityBill);
			procParameters[ParamNames.ActionId] = ActionId;
			procParameters[Agency.ParamNames.AgencyId] = agencyId;

			DataSet ds = (DataSet)DataAccessor.DoAction(procParameters);
			if (ds.Tables[Constants.TableNames.Data].Rows.Count == 0)
				return null;
			return entity.CreateObject(ds.Tables[Constants.TableNames.Data].Rows[0]);
		}

		protected PresentationObject CreateBill(Agency agency, Form owner, Entity entityBill, PresentationObject bill)
		{
			Dictionary<string, object> procParameters =
				new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

			procParameters[ParamNames.ActionId] = ActionId;
			procParameters[Agency.ParamNames.AgencyId] = agency.AgencyId;
			if (bill != null)
				procParameters[TableColumns.Bill.BillNo] = bill[TableColumns.Bill.BillNo];

			DateTime billDate = (bill != null) ? ParseHelper.GetDateTimeFromObject(bill[TableColumns.Bill.BillDate], DateTime.Today) : DateTime.Today;

			FrmBill fBill = new FrmBill(entityBill, billDate, procParameters);
			return fBill.ShowDialog(owner) == DialogResult.Cancel ? null : fBill.Bill;
		}

		#endregion

		public IDictionary<string, string> GetUniqueMassmedias(bool isFact)
		{
			Dictionary<string, object> parametersMM = new Dictionary<string, object>();
			parametersMM[ParamNames.ActionId] = ActionId;
			parametersMM["isFact"] = isFact;
			DataSet ds = DataAccessor.LoadDataSet("GetUniqueMMsForAction", parametersMM);

			MediaPlanCampaignGroups mp = new MediaPlanCampaignGroups();
			DataTable dt = ds.Tables[0];
			if (ds.Tables.Count > 1)
			{
				mp.InitUniquesList(ds.Tables[1]);
			}
			foreach (DataRow dataRow in dt.Rows)
				mp.AddMassmedia(int.Parse(dataRow["massmediaID"].ToString())
					, dataRow["name"].ToString()
					, int.Parse(dataRow["rollerID"].ToString())
					, DateTime.Parse(dataRow["date"].ToString()));
			return mp.GetUniqueMassmedias();
		}

		public string GetAgenciesString(string mmIds)
		{
			Dictionary<string, object> parametersMM = new Dictionary<string, object>
			{
				[ParamNames.ActionId] = ActionId,
				["massmediaIDString"] = mmIds,
				["agencies"] = DBNull.Value
			};
			DataAccessor.ExecuteNonQuery("GetAgenciesString", parametersMM);
			return parametersMM["agencies"].ToString();
		}

		protected void SetAdvertTypeOrSubstituteRoller()
		{
			try
			{
				Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
				procParameters.Add(ParamNames.ActionId, ActionId);
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ActionRollers),
					string.Format("Ролики рекламной акции № {0}", ActionId),
					procParameters, showModal: true);
				FireContainerRefreshed();
			}
			finally { Cursor.Current = Cursors.Default; }
		}

		#region Added issues calculation

		public DataTable BuildAddedIssuesTable()
		{
			DataTable addedIssues = CreateAddedIssuesTable();
			if (ChildEntity == null)
				return addedIssues;

			DataTable campaignsTable = Campaigns();
			if (campaignsTable == null || campaignsTable.Rows.Count == 0)
				return addedIssues;

			IList<Campaign> campaigns = GetCampaigns(campaignsTable);
			List<Campaign> actualCampaigns = new List<Campaign>();
			foreach (Campaign campaign in campaigns)
			{
				if (campaign != null)
					actualCampaigns.Add(campaign);
			}

			if (actualCampaigns.Count == 0)
				return addedIssues;

			Entity issueEntity = EntityManager.GetEntity((int)Entities.Issue);
			List<Dictionary<IssueSlotKey, List<DataRow>>> groupedIssues = new List<Dictionary<IssueSlotKey, List<DataRow>>>();

			foreach (Campaign campaign in actualCampaigns)
			{
				campaign.ChildEntity = issueEntity;
				DataTable campaignIssues = campaign.GetContent();
				Dictionary<IssueSlotKey, List<DataRow>> grouped = GroupIssuesBySlot(campaignIssues);

				if (grouped.Count == 0)
					return addedIssues;

				groupedIssues.Add(grouped);
			}

			HashSet<IssueSlotKey> commonSlots = BuildCommonSlots(groupedIssues);
			if (commonSlots.Count == 0)
				return addedIssues;

			List<IssueSlotKey> sortedSlots = new List<IssueSlotKey>(commonSlots);
			sortedSlots.Sort(IssueSlotKeyComparer.Instance);

			foreach (IssueSlotKey slot in sortedSlots)
			{
				DataRow representative = GetRepresentativeIssueRow(slot, groupedIssues);
				if (representative != null)
					TryAddIssueRow(addedIssues, representative, slot);
			}

			return addedIssues;
		}

		private static DataTable CreateAddedIssuesTable()
		{
			DataTable table = new DataTable("AddedIssues");
			table.Columns.Add("issueDate", typeof(DateTime));
			table.Columns.Add(Entity.ParamNames.NAME, typeof(string));
			table.Columns.Add("rollerID", typeof(string));
			table.Columns.Add("durationString", typeof(string));
			table.Columns.Add("position", typeof(string));
			table.Columns.Add("positionID", typeof(string));
			table.Columns.Add("RowNum", typeof(Guid));
			table.Columns.Add(Issue.ParamNames.IssueId, typeof(int));
			return table;
		}

		private static Dictionary<IssueSlotKey, List<DataRow>> GroupIssuesBySlot(DataTable issues)
		{
			Dictionary<IssueSlotKey, List<DataRow>> result = new Dictionary<IssueSlotKey, List<DataRow>>();
			if (issues == null || !issues.Columns.Contains(Issue.ParamNames.IssueDate))
				return result;

			foreach (DataRow row in issues.Rows)
			{
				if (!TryGetIssueDate(row, out DateTime issueDate))
					continue;

				IssueSlotKey key = IssueSlotKey.From(issueDate);
				if (!result.TryGetValue(key, out List<DataRow> rows))
				{
					rows = new List<DataRow>();
					result[key] = rows;
				}

				rows.Add(row);
			}

			return result;
		}

		private static HashSet<IssueSlotKey> BuildCommonSlots(List<Dictionary<IssueSlotKey, List<DataRow>>> groupedIssues)
		{
			HashSet<IssueSlotKey> commonSlots = null;
			foreach (Dictionary<IssueSlotKey, List<DataRow>> campaignSlots in groupedIssues)
			{
				if (commonSlots == null)
				{
					commonSlots = new HashSet<IssueSlotKey>(campaignSlots.Keys);
				}
				else
				{
					commonSlots.IntersectWith(campaignSlots.Keys);
				}

				if (commonSlots.Count == 0)
					break;
			}

			return commonSlots ?? new HashSet<IssueSlotKey>();
		}

		private static DataRow GetRepresentativeIssueRow(IssueSlotKey slot, List<Dictionary<IssueSlotKey, List<DataRow>>> groupedIssues)
		{
			foreach (Dictionary<IssueSlotKey, List<DataRow>> campaignSlots in groupedIssues)
			{
				List<DataRow> slotIssues;
				if (campaignSlots.TryGetValue(slot, out slotIssues) && slotIssues != null && slotIssues.Count > 0)
					return slotIssues[0];
			}

			return null;
		}

		private static bool TryAddIssueRow(DataTable target, DataRow source, IssueSlotKey slot)
		{
			DateTime issueDate;
			if (!TryGetIssueDate(source, out issueDate))
				return false;

			return CreateAddedIssueRow(target, source, slot.ToDateTime());
		}

		private static bool CreateAddedIssueRow(DataTable target, DataRow source, DateTime issueDate)
		{
			if (!ColumnExists(source, Issue.ParamNames.IssueId))
				return false;

			int issueId = ParseHelper.ParseToInt32(source[Issue.ParamNames.IssueId].ToString(), 0);

			DataRow newRow = target.NewRow();
			newRow["issueDate"] = issueDate;
			newRow[Entity.ParamNames.NAME] = ResolveString(source, Entity.ParamNames.NAME, RollerIssue.ParamNames.RollerName);
			newRow["rollerID"] = ResolveString(source, Roller.ParamNames.RollerId);
			newRow["durationString"] = ResolveDurationString(source);
			int positionId = ResolvePositionId(source);
			newRow["position"] = GetPositionDisplayName((RollerPositions)positionId);
			newRow["positionID"] = positionId.ToString(CultureInfo.InvariantCulture);
			newRow["RowNum"] = Guid.NewGuid();
			newRow[Issue.ParamNames.IssueId] = issueId;
			target.Rows.Add(newRow);
			return true;
		}

		private static bool TryGetIssueDate(DataRow row, out DateTime issueDate)
		{
			issueDate = DateTime.MinValue;
			if (!ColumnExists(row, Issue.ParamNames.IssueDate))
				return false;

			issueDate = ParseHelper.GetDateTimeFromObject(row[Issue.ParamNames.IssueDate], DateTime.MinValue);
			return issueDate != DateTime.MinValue;
		}

		private static string ResolveDurationString(DataRow row)
		{
			if (ColumnExists(row, Roller.ParamNames.DurationString))
			{
				object value = row[Roller.ParamNames.DurationString];
				if (value != DBNull.Value)
					return value.ToString();
			}

			if (ColumnExists(row, Roller.ParamNames.Duration))
			{
				int seconds = ParseHelper.ParseToInt32(row[Roller.ParamNames.Duration].ToString(), 0);
				if (seconds > 0)
				{
					TimeSpan span = TimeSpan.FromSeconds(seconds);
					return span.Hours > 0 ? span.ToString(@"hh\:mm\:ss") : span.ToString(@"mm\:ss");
				}
			}

			return string.Empty;
		}

		private static int ResolvePositionId(DataRow row)
		{
			if (ColumnExists(row, Issue.ParamNames.Position))
				return ParseHelper.ParseToInt32(row[Issue.ParamNames.Position].ToString(), (int)RollerPositions.Undefined);

			if (ColumnExists(row, "position"))
				return ParseHelper.ParseToInt32(row["position"].ToString(), (int)RollerPositions.Undefined);

			return (int)RollerPositions.Undefined;
		}

		private static string GetPositionDisplayName(RollerPositions position)
		{
			switch (position)
			{
				case RollerPositions.First:
				case RollerPositions.FirstTransferred:
					return "Первый";
				case RollerPositions.Second:
				case RollerPositions.SecondTransferred:
					return "Второй";
				case RollerPositions.Last:
				case RollerPositions.LastTransferred:
					return "Последний";
				default:
					return "Неопределена";
			}
		}

		private static string ResolveString(DataRow row, params string[] columns)
		{
			if (row == null || row.Table == null)
				return string.Empty;

			foreach (string column in columns)
			{
				if (row.Table.Columns.Contains(column))
				{
					object value = row[column];
					if (value != DBNull.Value)
						return value.ToString();
				}
			}

			return string.Empty;
		}

		private static bool ColumnExists(DataRow row, string columnName)
		{
			return row?.Table?.Columns.Contains(columnName) == true;
		}

		private readonly struct IssueSlotKey : IEquatable<IssueSlotKey>
		{
			private readonly DateTime date;
			private readonly int slotIndex;

			private IssueSlotKey(DateTime date, int slotIndex)
			{
				this.date = date;
				this.slotIndex = slotIndex;
			}

			public static IssueSlotKey From(DateTime dateTime)
			{
				int slot = ((dateTime.Hour * 60) + dateTime.Minute) / 30;
				return new IssueSlotKey(dateTime.Date, slot);
			}

			public DateTime ToDateTime()
			{
				return date.AddMinutes(slotIndex * 30);
			}

			public bool Equals(IssueSlotKey other)
			{
				return date == other.date && slotIndex == other.slotIndex;
			}

			public override bool Equals(object obj)
			{
				return obj is IssueSlotKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (date.GetHashCode() * 397) ^ slotIndex;
				}
			}
		}

		private sealed class IssueSlotKeyComparer : IComparer<IssueSlotKey>
		{
			public static readonly IssueSlotKeyComparer Instance = new IssueSlotKeyComparer();

			private IssueSlotKeyComparer()
			{
			}

			public int Compare(IssueSlotKey x, IssueSlotKey y)
			{
				return x.ToDateTime().CompareTo(y.ToDateTime());
			}
		}
	}
        #endregion
}