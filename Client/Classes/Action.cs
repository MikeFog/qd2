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
						Agency agency = (Agency) po;
						BillReport report = new BillReport(this, agency, bill, month);
						report.Show(string.Format("—чЄт на предоплату, агенство '{0}' за мес€ц {1} {2} года", agency.Name
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
						FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "¬ыбор периода");
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
				months.Add((DateTime) item.Key);
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
				// если акци€ началась в предыдущем мес€це или ранее, то нельз€
				if (new DateTime(StartDate.Year, StartDate.Month, 1) < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)) return false;
				// если начало в этом мес€це, то не должна уже закончитьс€
				if(FinishDate < DateTime.Today) return false;

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
            report.Show("ƒоговор");
		}

        protected virtual void PrintSponsorContract(Form owner, Agency agency, bool exportReport)
        {
            PresentationObject bill = GetBill(agency, owner);
            if (bill == null) return;

            Application.DoEvents();
            owner.Cursor = Cursors.WaitCursor;

            ContractReport report = new ContractReport(this, agency, bill, true);
            report.Show("—понсорский договор");
        }

        private void PrintBillContract(Form owner, Agency agency, bool exportReport)
        {
            PresentationObject bill = GetBill(agency, owner);
            if (bill == null) return;

            Application.DoEvents();
            owner.Cursor = Cursors.WaitCursor;
            BillReport report = new BillContractReport(this, agency, bill);
            report.Show("—чЄт-договор");
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
			else report.Show("—чЄт");
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
					string.Format("–олики рекламной акции є {0}", ActionId),
					procParameters, showModal: true);
				FireContainerRefreshed();
            }
            finally { Cursor.Current = Cursors.Default; }
        }
    }
}