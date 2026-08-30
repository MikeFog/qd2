using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
    // UI-часть (DoAction, DeleteIssues-диалог, PrintTransfers, PrintMediaPlan,
    // EditRollerIssues, EditProgramIssues, ChangeAgency, ChangePaymentType) —
    // в Campaign.WinForms.cs. Перед правками читать
    // docs/scenarios/campaign-edit-form-load.md — EditRollerIssues/
    // EditProgramIssues точки входа в CampaignForm.
    // DisplayCampaignData(ListBox) и заглушка PrintOnAirInquire(Form) тоже там:
    // не диалоги, но принимают UI-типы, а ядро должно собираться вне Client.
    // Конвенция — docs/tasks/web-migration-dialogs.md.
    internal partial class Campaign : CampaignPart
    {
        private Agency _agency;
        protected DataTable _modules;
        private ActionOnMassmedia _action;

        public const int ShortAttributesList = 1;
        #region Nested type: ActionNames

        internal struct ActionNames
		{
			public const string ChangeAgency = "ChangeAgency";
            public const string ChangePaymentType = "ChangePaymentType";
			public const string PrintMediaPlan = "PrintMediaPlan";
			public const string PrintMediaPlanFact = "PrintMediaPlanFact";
			public const string PrintMediaPlanMonth = "PrintMediaPlanMonth";
			public const string PrintMediaPlanFactMonth = "PrintMediaPlanFactMonth";
			public const string PrintMediaPlanByPeriod = "PrintMediaPlanByPeriod";
			public const string PrintMediaPlanFactByPeriod = "PrintMediaPlanFactByPeriod";
            public const string PrintSelectivelyMediaPlan = "PrintSelectivelyMediaPlan";
            public const string PrintSelectivelyMediaPlanFact = "PrintSelectivelyMediaPlanFact";
            public const string PrintSelectivelyMediaPlanMonth = "PrintSelectivelyMediaPlanMonth";
            public const string PrintSelectivelyMediaPlanFactMonth = "PrintSelectivelyMediaPlanFactMonth";
            public const string PrintSelectivelyMediaPlanByPeriod = "PrintSelectivelyMediaPlanByPeriod";
            public const string PrintSelectivelyMediaPlanFactByPeriod = "PrintSelectivelyMediaPlanFactPeriod";
			public const string PrintOnAirInquire = "PrintOnAirInquire";
			public const string ShowDays = "ShowDays";
			public const string ShowRollers = "ShowRollers";
			public const string PrintTransfers = "PrintTransfers";
			public const string ExportMediaPlus = "ExportMediaPlus";
            public const string DeleteIssues = "DeleteIssues";
        }

		#endregion

		#region Nested type: ParamNames

		public struct ParamNames
		{
			public const string AgencyID = "agencyID";
			public const string CampaignId = "campaignID";
			public const string CampaignTypeId = "campaignTypeID";
            public const string CampaignTypeName = "campaignTypeName";
            public const string Discount = "discount";
			public const string FinalPrice = "finalPrice";
			public const string FinishDate = "finishDate";
			public const string FullPrice = "fullPrice";
			public const string GrantorID = "grantorUserId";
			public const string IssuesCount = "issuesCount";
			public const string IssuesDuration = "issuesDuration";

			public const string ManagerDiscount = "managerDiscount";
			public const string MassmediaId = "massmediaID";
			public const string MassmediaName = "massmediaName";
			public const string PackDiscount = "packDiscount";
			public const string PaymentTypeID = "paymentTypeID";
			public const string Price = "price";
			public const string ProgramIssuesCount = "programsCount";
			public const string StartDate = "startDate";
			public const string TariffPrice = "tariffPrice";
			public const string TimeBonus = "timeBonus";
			public const string GroupName = "groupName";
            public const string SplitType = "splitType";
        }

		#endregion

		public enum CampaignTypes
		{
			Simple = 1,
			Sponsor = 2,
			Module = 3,
			PackModule = 4
		}


		public Campaign(Entity entity) : base(entity)
		{
		}

		public Campaign() : base(EntityManager.GetEntity((int) Entities.CampaignOnMassmedia))
		{
		}

		public Campaign(int campaignID)
			: this()
		{
			this[ParamNames.CampaignId] = campaignID;
			isNew = false;
			Refresh();
		}

		public Campaign(DataRow row)
			: base(EntityManager.GetEntity((int) Entities.CampaignOnMassmedia), row)
		{
		}

		public Campaign(Entity entity, DataRow row) : base(entity, row)
		{
		}

		protected Campaign(CampaignTypes campaignType, int paymentTypeId, int? massmediaId, int agencyId)
			: base(EntityManager.GetEntity((int) Entities.CampaignOnMassmedia))
		{
			this[ParamNames.AgencyID] = agencyId;
			this[ParamNames.CampaignTypeId] = campaignType;
			if (massmediaId != null) this[ParamNames.MassmediaId] = massmediaId;
			this[ParamNames.PaymentTypeID] = paymentTypeId;
			SelectEntity(campaignType);
		}

		public DateTime StartDate
		{
			get
			{
				if (!parameters.ContainsKey(ParamNames.StartDate)) Refresh();
				if (this[ParamNames.StartDate] == DBNull.Value) return DateTime.MinValue;
				return DateTime.Parse(this[ParamNames.StartDate].ToString());
			}
		}

		public DateTime FinishDate
		{
			get
			{
				if (!parameters.ContainsKey(ParamNames.FinishDate)) Refresh();
				if (this[ParamNames.FinishDate] == DBNull.Value) return DateTime.MinValue;
				return DateTime.Parse(this[ParamNames.FinishDate].ToString());
			}
		}

		public Decimal TariffPrice
		{
			get { return decimal.Parse(this[ParamNames.TariffPrice].ToString()); }
		}

		public Decimal Price
		{
			get { return decimal.Parse(this[ParamNames.Price].ToString()); }
		}

		public int IssuesCount
		{
			get { return int.Parse(this[ParamNames.IssuesCount].ToString()); }
		}

		public decimal FinalPrice
		{
			get { return decimal.Parse(parameters[ParamNames.FinalPrice].ToString()); }
		}

		public decimal FullPrice
		{
			get { return decimal.Parse(parameters[ParamNames.FullPrice].ToString()); }
		}

		public CampaignTypes CampaignType
		{
			get { return (CampaignTypes) int.Parse(this[ParamNames.CampaignTypeId].ToString()); }
		}

        public string CampaignTypeName
        {
            get { return this[ParamNames.CampaignTypeName].ToString(); }
        }

		public ActionOnMassmedia Action
		{
			set { this[Classes.Action.ParamNames.ActionId] = value.ActionId; }
			get
			{
				if (_action == null)
				{
					if (ActionId == null) Refresh();
					_action = ActionOnMassmedia.GetActionById((int) ActionId);
				}
				return _action;
			}
		}

		public void RecalculateAction(bool refreshFlag = true)
		{
			using (OperationScope.Start("RecalculateAction"))
			{
				Action.Recalculate(refreshFlag);
			}
		}

		public int? ActionId
		{
			get
			{
				if (!parameters.ContainsKey(Classes.Action.ParamNames.ActionId)) return null;
				return int.Parse(this[Classes.Action.ParamNames.ActionId].ToString());
			}
		}

		private bool IsDeleted
		{
			get { return ActionId == null; }
		}

		public int IssuesDuration
		{
			get { return int.Parse(this[ParamNames.IssuesDuration].ToString()); }
		}

		public Decimal Discount
		{
			get { return decimal.Parse(this[ParamNames.Discount].ToString()); }
		}

		public Decimal PackDiscount
		{
			get { return decimal.Parse(this[ParamNames.PackDiscount].ToString()); }
		}

		public Decimal ManagerDiscount
		{
			get { return decimal.Parse(this[ParamNames.ManagerDiscount].ToString()); }
		}

		public int ProgramIssuesCount
		{
			get { return int.Parse(this[ParamNames.ProgramIssuesCount].ToString()); }
		}

		public Agency Agency
		{
			get
			{
				if (_agency == null)
				{
					if (!parameters.ContainsKey(Agency.ParamNames.AgencyId))
						Refresh(InterfaceObjects.SimpleJournal);
					_agency = Agency.GetAgencyByID(int.Parse(this[Agency.ParamNames.AgencyId].ToString()));
				}
				return _agency;
			}
		}

		public int Bonus
		{
			get { return int.Parse(this[ParamNames.TimeBonus].ToString()); }
		}

		private void SetChildEntity(DataRow row)
		{
			if (int.Parse(row[ParamNames.CampaignTypeId].ToString()) == (int) CampaignTypes.Sponsor)
				ChildEntity = EntityManager.GetEntity((int) Entities.CampaignPart);
		}

		public override void Init(DataRow row)
		{
			SelectEntity((CampaignTypes) int.Parse(row[ParamNames.CampaignTypeId].ToString()));
			SetChildEntity(row);
			base.Init(row);
		}

		private void SelectEntity(CampaignTypes campaignType)
		{
			switch (campaignType)
			{
				case CampaignTypes.Simple:
					entity = EntityManager.GetEntity((int) Entities.GeneralCampaign);
					break;

				case CampaignTypes.Module:
					entity = EntityManager.GetEntity((int) Entities.ModuleCampaign);
					break;

				case CampaignTypes.Sponsor:
					entity = EntityManager.GetEntity((int) Entities.SponsorCampaign);
					break;

				case CampaignTypes.PackModule:
					entity = EntityManager.GetEntity((int) Entities.PackModuleCampaign);
					break;
			}
		}

		// DoAction переехал в Campaign.WinForms.cs.

		// DeleteIssues (диалог), PrintTransfers переехали в Campaign.WinForms.cs.

		/// <summary>
		/// Удаляет выбранные выпуски (id — из CampaignDaysForm.SelectedIDs).
		/// Возвращает таблицу ошибок (пустая — без ошибок); anyDeleted — было ли
		/// удалено хоть что-то.
		/// </summary>
		internal DataTable ApplyIssuesDelete(bool isSponsorProgram, IEnumerable<int> selectedIds, out bool anyDeleted)
		{
			anyDeleted = false;
			DataTable tableErrors = ErrorManager.CreateErrorsTable();

			foreach (var id in selectedIds)
			{
				PresentationObject item = null;
				try
				{
					Entity itemEntity = null;
					Dictionary<string, object> parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase)
					{
						[Campaign.ParamNames.CampaignId] = CampaignId
					};

					// в зависимости от типа кампании создаём разные "issue" и пытаемся их удалить
					if(isSponsorProgram)
					{
						itemEntity = EntityManager.GetEntity((int)Entities.ProgramIssue);
						parameters[Issue.ParamNames.IssueId] = id;
					}
					else if (CampaignType == CampaignTypes.Simple || CampaignType == CampaignTypes.Sponsor)
					{
						itemEntity = EntityManager.GetEntity((int)Entities.Issue);
						parameters[Issue.ParamNames.IssueId] = id;
					}
					else if (CampaignType == CampaignTypes.Module)
					{
						itemEntity = EntityManager.GetEntity((int)Entities.ModuleIssue);
						parameters[ModuleIssue.ParamNames.ModuleIssueId] = id;
					}
					else if (CampaignType == CampaignTypes.PackModule)
					{
						itemEntity = EntityManager.GetEntity((int)Entities.PackModuleIssue);
						parameters[Issue.ParamNames.PackModuleIssueID] = id;
					}

					item = itemEntity.CreateObject(parameters);
					item.Delete(true);
					anyDeleted = true;
				}
				catch (Exception ex)
				{
					item.Refresh();
					ErrorManager.AddErrorRow(tableErrors, DateTime.Parse(item[CampaignDay.ParamNames.IssueDate].ToString()), MessageAccessor.GetMessage(ex.Message));
				}
			}
			return tableErrors;
		}

		// PrintTransfers, PrintMediaPlan, EditRollerIssues, EditProgramIssues
		// переехали в Campaign.WinForms.cs.

		// EditRollerIssues, EditProgramIssues переехали в Campaign.WinForms.cs
		// (форма 3.1 — модальная сессия CampaignForm, docs/tasks/web-migration-dialogs.md, §8 п.3).

		public void SetFinalPrice(decimal finalPrice, DateTime todayDate, int? grantorId, int? managerDiscountReasonId)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int) Entities.CampaignOnMassmedia), InterfaceObjects.FakeModule,
				Constants.Actions.SetFinalPrice);
			procParameters[ParamNames.CampaignId] = CampaignId;
			procParameters[ParamNames.CampaignTypeId] = (int) CampaignType;
			procParameters[ParamNames.FinalPrice] = finalPrice;
			procParameters[ParamNames.GrantorID] = (object) grantorId ?? DBNull.Value;
            procParameters["managerDiscountReasonId"] = managerDiscountReasonId;
            procParameters["todayDate"] = todayDate;

            DataAccessor.DoAction(procParameters);
			Refresh();
		}

		private void ShowRollers()
		{
			if (entity.Id == (int) Entities.GeneralCampaign)
				ChildEntity = EntityManager.GetEntity((int) Entities.CampaignRoller);
			else
				ChildEntity = EntityManager.GetEntity((int) Entities.CampaignModule);
			FireContainerRefreshed();
		}

		// ChangeAgency и ChangePaymentType переехали в Campaign.WinForms.cs.

		internal void ApplyAgencyChange(int agencyId)
		{
			this[ParamNames.AgencyID] = agencyId;
			Update();
			OnObjectChanged(this);
		}

		internal void ApplyPaymentTypeChange(int paymentTypeId)
		{
			this[ParamNames.PaymentTypeID] = paymentTypeId;
			Update();
			OnObjectChanged(this);
		}

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (!ActionOnMassmedia.CheckLoggedUserRight(actionName, Action))
				return true;

			if (actionName == ActionNames.ShowDays)
				return type != ViewType.Tree;
			if (actionName == ActionNames.ShowRollers)
				return type != ViewType.Tree;

			return base.IsActionHidden(actionName, type);
        }

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (!ActionOnMassmedia.CheckLoggedUserRight(actionName, Action))
				return false;

			if (actionName == ActionNames.ShowDays)
				return type == ViewType.Tree && ChildEntity != null && ChildEntity.Id != (int) Entities.CampaignDay;
			if (actionName == ActionNames.ShowRollers)
				return type == ViewType.Tree && ChildEntity != null && ChildEntity.Id != (int)Entities.CampaignRoller;
			else
				return base.IsActionEnabled(actionName, type);
		}

		// skipCampaignRecalc: массовые пути (FrmGenerator, клонирование, drag-and-drop
		// пачкой) делают один ActionRecalculate после всей пачки — им не нужен
		// пер-выпусковый hlp_CampaignRecalc внутри IssueIUD/ModuleIssueIUD.
		// Интерактивное одиночное добавление (грид) оставляет false: панель кампании
		// держится именно на этом пересчёте.
		public Issue AddIssue(PresentationObject roller, ITariffWindow tariffWindow, RollerPositions rollerPosition, int? grantorID,
		                      bool skipCampaignRecalc = false)
		{
			using (OperationScope.Start("CampaignAddIssue"))
			{
				RollerIssue issue = new RollerIssue(this, roller, (TariffWindowWithRollerIssues) tariffWindow, rollerPosition, Action.IsConfirmed,grantorID);
				if (skipCampaignRecalc)
					issue["skipCampaignRecalc"] = 1;
				issue.Update();
				return issue;
			}
		}

		public ModuleIssue AddModuleIssue(Module module, PresentationObject roller,
		                                  ModulePricelist modulePriceList, DateTime date, RollerPositions rollerPosition,
										  int? grantorID, bool skipCampaignRecalc = false)
		{
			if (IsModuleExists(modulePriceList, date))
			{
				ModuleIssue issue =
					new ModuleIssue(this, module, roller, modulePriceList, date, Action.IsConfirmed, rollerPosition,
									grantorID);
				if (skipCampaignRecalc)
					issue["skipCampaignRecalc"] = 1;
				issue.Update();
				return issue;
			}
			return null;
		}

		public bool IsModuleExists(ModulePricelist modulePriceList, DateTime date)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters.Add(ModulePricelist.ParamNames.ModulePriceListID, modulePriceList.ModulePriceListID);
			procParameters.Add("date", date.Date);
			return DataAccessor.ExecuteScalar("IsModuleExist", procParameters).ToString() == "1";
		}

		public ProgramIssue AddProgramIssue(PresentationObject sponsorProgram, int tariffId,
		                                    DateTime date, Decimal price, int bonus, bool isConfirmed, PresentationObject advertType = null)
		{
			ProgramIssue programIssue = new ProgramIssue();
			programIssue[ParamNames.CampaignId] = IDs[0];
			programIssue[RollerIssue.ParamNames.IssueDate] = date;
			programIssue[Issue.ParamNames.TariffId] = tariffId;
			programIssue[TableColumns.ProgramIssue.ProgramID] = sponsorProgram.IDs[0];
			programIssue[Issue.ParamNames.TariffPrice] = price;
			programIssue[TableColumns.ProgramIssue.Bonus] = bonus;
			if(advertType != null)
                programIssue[AdvertType.ParamNames.AdvertTypeId] = advertType.IDs[0];
            programIssue["isConfirmed"] = isConfirmed;
			programIssue.Update();
			return programIssue;
		}

		public static Campaign GetCampaignById(int campaignId)
		{
			Campaign campaign = new CampaignOnSingleMassmedia(campaignId);
			campaign.Refresh();
			if (campaign.IsDeleted) return null;

			// TODO: Fix IT!
			if (campaign.CampaignType == CampaignTypes.Module)
			{
				campaign = new CampaignModule(campaignId);
				campaign.Refresh();
			}
			else if (campaign.CampaignType == CampaignTypes.PackModule)
			{
				campaign = new CampaignPackModule(campaignId);
				campaign.Refresh();
			}

			return campaign;
		}

		// DisplayCampaignData(ListBox) переехал в Campaign.WinForms.cs.

		public static Campaign CreateInstance(int campaignTypeId, int paymentTypeId, int? massmediaId, int agencyId)
		{
			if((CampaignTypes)campaignTypeId == CampaignTypes.PackModule)
                return new CampaignPackModule(paymentTypeId, agencyId);
            return new Campaign((CampaignTypes) campaignTypeId, paymentTypeId, massmediaId, agencyId);
		}

		public void ClearModuleList()
		{
			_modules = null;
		}

		public virtual bool HasModuleIssue(PresentationObject module)
		{
			if (isNew || CampaignType != CampaignTypes.Module)
				return false;
			if (_modules == null)
			{
				ChildEntity = EntityManager.GetEntity((int) Entities.CampaignModule);
				_modules = base.GetContent();
			}
			return _modules.Select(string.Format("moduleID={0}", module.IDs[0])).Length > 0;
		}

        public void GetPriceByPeriodWithTax(DateTime startDate, DateTime finishDate, int massmediaId, bool showBlack, string rollerIDs, 
			out decimal price, out decimal tariffPrice, out decimal taxPrice)
        {
			Dictionary<string, object> ps = DataAccessor.CreateParametersDictionary();
            ps["campaignID"] = CampaignId;
            ps["campaignTypeID"] = CampaignType;
            //ps["massmediaID"] = DBNull.Value;
            ps["massmediaID"] = massmediaId;
            ps["startDate"] = startDate;
            ps["finishDate"] = finishDate;
            ps["price"] = DBNull.Value;
            ps["taxPrice"] = DBNull.Value;
            ps["withTax"] = true;
            ps["showBlack"] = showBlack;
            if (!string.IsNullOrEmpty(rollerIDs))
                ps["rollerIDString"] = rollerIDs;
            DataAccessor.ExecuteNonQuery("GetPriceByPeriod", ps);

            price = ParseHelper.GetDecimalFromObject(ps["price"], 0);
            tariffPrice = ParseHelper.GetDecimalFromObject(ps["tariffPrice"], 0);
            taxPrice = ParseHelper.GetDecimalFromObject(ps["taxPrice"], 0);
        }

 		public DataSet GetOnAirInquireReport(int massmediaID, int campaignID, DateTime startDate, DateTime finishDate)
		{
			Dictionary<string, object> procParameters = new Dictionary<string, object>
			                                            	{
			                                            		{"startDate", startDate},
			                                            		{"finishDate", finishDate},
			                                            		{"massmediaId", massmediaID},
			                                            		{"campaignID", campaignID}
			                                            	};
			return DataAccessor.LoadDataSet("OnAirInquireReport", procParameters);
		}

		// Виртуальная заглушка PrintOnAirInquire(Form) переехала в Campaign.WinForms.cs.

        private bool IsChangePossible
		{
			get 
			{
                if (SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsBookKeeper|| !Action.IsConfirmed) return true;
                // если акция началась в предыдущем месяце или ранее, то нельзя
                if (new DateTime(StartDate.Year, StartDate.Month, 1) < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)) return false;
                // если начало в этом месяце, то не должна уже закончиться
                if (FinishDate < DateTime.Today) return false;

                return true;
            }
		}
    }
}
