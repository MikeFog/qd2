using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Controls;
using Merlin.Forms;
using Merlin.Properties;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Classes
{
    internal class Campaign : CampaignPart
    {
		#region Constants -------------------------------------

		public const int ShortAttributesList = 1;
        #endregion
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
            public const string PrintSelectivelyMediaPlanFactByPeriod = "PrintSelectivelyMediaPlanFactByPeriod";
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

		#region CampaignTypes enum

		public enum CampaignTypes
		{
			Simple = 1,
			Sponsor = 2,
			Module = 3,
			PackModule = 4
		}

		#region Constructors ----------------------------------

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
		#endregion

		#endregion

		private Agency agency;
		protected DataTable modules;


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


        private ActionOnMassmedia _action;

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
			Action.Recalculate(refreshFlag);
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
				if (agency == null)
				{
					if (!parameters.ContainsKey(Agency.ParamNames.AgencyId))
						Refresh(InterfaceObjects.SimpleJournal);
					agency = Agency.GetAgencyByID(int.Parse(this[Agency.ParamNames.AgencyId].ToString()));
				}
				return agency;
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

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.ChangePaymentType)
				ChangePaymentType(owner);
			else if (actionName == ActionNames.ChangeAgency)
				ChangeAgency(owner);
			else if (actionName == ActionNames.ShowRollers)
				ShowRollers();
			else if (actionName == ActionNames.ShowDays)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.CampaignDay);
				FireContainerRefreshed();
			}
			else if (actionName == Constants.EntityActions.Edit)
				EditRollerIssues(owner, new RollerIssuesGrid3());
			else if (actionName == ActionNames.PrintMediaPlan)
				PrintMediaPlan(false, false, false, false);
			else if (actionName == ActionNames.PrintMediaPlanFact)
				PrintMediaPlan(true, false, false, false);
			else if (actionName == ActionNames.PrintMediaPlanMonth)
				PrintMediaPlan(false, true, false, false);
			else if (actionName == ActionNames.PrintMediaPlanFactMonth)
				PrintMediaPlan(true, true, false, false);
			else if (actionName == ActionNames.PrintMediaPlanByPeriod)
				PrintMediaPlan(false, false, true, false);
			else if (actionName == ActionNames.PrintMediaPlanFactByPeriod)
				PrintMediaPlan(true, false, true, false);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlan)
				PrintMediaPlan(false, false, false, true);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlanFact)
				PrintMediaPlan(true, false, false, true);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlanMonth)
				PrintMediaPlan(false, true, false, true);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlanFactMonth)
				PrintMediaPlan(true, true, false, true);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlanByPeriod)
				PrintMediaPlan(false, false, true, true);
			else if (actionName == ActionNames.PrintSelectivelyMediaPlanFactByPeriod)
				PrintMediaPlan(true, false, true, true);
			else if (actionName == ActionNames.PrintTransfers)
				PrintTransfers();
			else if (actionName == ActionNames.DeleteIssues)
				DeleteIssues(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		public bool DeleteIssues(IWin32Window owner, bool isSponsorProgram = false, Dictionary<string, object> extraParameters = null, bool isFireEvent = true)
		{
            bool resFlag = false;
            try
			{
				Entity currentChild = null;

                if (ChildEntity != null)
					currentChild = ChildEntity;

				ChildEntity = EntityManager.GetEntity((int)Entities.CampaignDay);
				CampaignDaysForm selector = new CampaignDaysForm(this, isSponsorProgram, extraParameters);

				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor.Current = Cursors.WaitCursor;

					Action.Refresh();
                    decimal price = Action.TotalPrice;

                    DataTable tableErrors = CreateErrorsTable();
					
                    foreach (var id in selector.SelectedIDs)
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
							resFlag = true;
						}
						catch (Exception ex)
						{
							item.Refresh();
							AddErrorRow(tableErrors, DateTime.Parse(item[CampaignDay.ParamNames.IssueDate].ToString()), MessageAccessor.GetMessage(ex.Message));
						}
					}
					if (tableErrors.Rows.Count > 0)
					{
						Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки удаления", tableErrors);
					}
                    RecalculateAndShowPriceChange(price);
                    if (currentChild != null)
                        ChildEntity = currentChild;
                    if (isFireEvent)
						FireContainerRefreshed();
                }
                return resFlag;
            }
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
                return resFlag;
            }
            finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

        private void PrintTransfers()
		{
			DataSet ds = DataAccessor.LoadDataSet("CampaignIssuesTransfers", new Dictionary<string, object> { {ParamNames.CampaignId, CampaignId } });
			if (ds == null  || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
				Globals.ShowInfo("CampaignHaveNotTransfers");
			else
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.CampaignIssuesTransfers), Resources.CampaignIssuesTransfersTitle, ds.Tables[0]);
		}

        public void PrintMediaPlan(bool isActual, bool isByMonth, bool isByPeriod, bool selectively)
		{
			Application.DoEvents();
			Refresh();

			if (isByMonth)
			{
				Dictionary<string, object> procParameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
				procParameters[ParamNames.CampaignId] = CampaignId;
				procParameters["isFact"] = isActual;
				DataSet ds = DataAccessor.LoadDataSet("GetMonthes", procParameters);

				Dictionary<object, object> dMonthsToShow = new Dictionary<object, object>();

				foreach (DataRow row in ds.Tables[0].Rows)
				{
					int month = ParseHelper.ParseToInt32(row["MonthDate"].ToString(), -1);
					int year = ParseHelper.ParseToInt32(row["MonthYear"].ToString(), -1);
					if (month >= 0 && year >= 0)
					{
						DateTime date = new DateTime(year, month, 1);
						dMonthsToShow.Add(date, date.ToString("MMMM yyy"));
					}
				}

				FrmMonths f = new FrmMonths(dMonthsToShow, false);
				if (f.ShowDialog(Globals.MdiParent) == DialogResult.Cancel) return;
				IList<DateTime> months = new List<DateTime>();
				foreach (KeyValuePair<object, object> item in f.CheckedItems)
					months.Add((DateTime)item.Key);
                MediaPlan.CreateInstance(this, months, selectively).Show(isActual);
			}
			else if (isByPeriod)
			{
				FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "Выбор периода");
				if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
                    MediaPlan.CreateInstance(this, selector.StartDate, selector.FinishDate, selectively).Show(isActual);
			}
			else
                MediaPlan.CreateInstance(this, selectively).Show(isActual);
		}

		protected void EditRollerIssues(IWin32Window owner, TariffGrid tariffGrid)
		{
			CampaignForm campaign = new CampaignForm(this, tariffGrid);
			campaign.ShowDialog(owner);
			Application.DoEvents();
			if (campaign.ChangeFlag)
			{
				//Action.Recalculate();
				Refresh();
				FireContainerRefreshed();
			}
		}

		public void EditProgramIssues(IWin32Window owner)
		{
			CampaignForm campaign = new CampaignForm(this, new ProgramIssuesGrid2());
			campaign.ShowDialog(owner);
			Application.DoEvents();
			if (campaign.ChangeFlag)
			{
				Action.Recalculate();
				Refresh();
			}
		}

		public void SetFinalPrice(decimal finalPrice, DateTime todayDate, int? grantorId)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int) Entities.CampaignOnMassmedia), InterfaceObjects.FakeModule,
				Constants.Actions.SetFinalPrice);
			procParameters[ParamNames.CampaignId] = CampaignId;
			procParameters[ParamNames.CampaignTypeId] = (int) CampaignType;
			procParameters[ParamNames.FinalPrice] = finalPrice;
			procParameters[ParamNames.GrantorID] = (object) grantorId ?? DBNull.Value;
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

		private void ChangeAgency(IWin32Window owner)
		{
            if (IsChangePossible)
			{
                SelectionForm selector;
                if (SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsBookKeeper)
                    selector = new SelectionForm(EntityManager.GetEntity((int)Entities.Agency), "Рекламное агентство");
				else
					selector = new SelectionForm(EntityManager.GetEntity((int)Entities.Agency),
					(this is CampaignOnSingleMassmedia radioStation) 
						? radioStation.Massmedia.Agencies.DefaultView : SecurityManager.LoggedUser.Agencies.DefaultView,
                    "Рекламное агентство");
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					this[ParamNames.AgencyID] = selector.SelectedObject.IDs[0];
					Update();
					OnObjectChanged(this);
                    MessageBox.ShowInformation(Resources.AgencyChangeSuccess);
                }
			}
            else
                MessageBox.ShowExclamation(Resources.ChangeAgencyIsForbidden);
        }

		private void ChangePaymentType(IWin32Window owner)
		{
			if (IsChangePossible)
			{
				SelectionForm selector = new SelectionForm(EntityManager.GetEntity((int)Entities.PaymentType), "Типы оплаты");
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					this[ParamNames.PaymentTypeID] = selector.SelectedObject.IDs[0];
					Update();
					OnObjectChanged(this);
					MessageBox.ShowInformation(Resources.PaymentTypeChangeSuccess);
				}
			}
            else
                MessageBox.ShowExclamation(Resources.ChangePaymentTypeIsForbidden);
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

		public Issue AddIssue(PresentationObject roller, ITariffWindow tariffWindow, RollerPositions rollerPosition, int? grantorID)
		{
			RollerIssue issue = new RollerIssue(this, roller, (TariffWindowWithRollerIssues) tariffWindow, rollerPosition, Action.IsConfirmed,grantorID);
			issue.Update();
			return issue;
		}

		public ModuleIssue AddModuleIssue(Module module, PresentationObject roller,
		                                  ModulePricelist modulePriceList, DateTime date, RollerPositions rollerPosition,
										  int? grantorID)
		{
			if (IsModuleExists(modulePriceList, date))
			{
				ModuleIssue issue =
					new ModuleIssue(this, module, roller, modulePriceList, date, Action.IsConfirmed, rollerPosition,
									grantorID);
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

		internal void DisplayCampaignData(ListBox lstStat)
		{
			string text;
			lstStat.Items.Clear();
			if (StartDate == DateTime.MinValue)
				text = string.Empty;
			else
				text = StartDate.ToShortDateString();
			lstStat.Items.Add("Начало: " + text);

			if (FinishDate == DateTime.MinValue)
				text = string.Empty;
			else
				text = FinishDate.ToShortDateString();
			lstStat.Items.Add("Окончание: " + text);
			lstStat.Items.Add("Выпусков: " + IssuesCount);
			lstStat.Items.Add("Общее время: " + DateTimeUtils.Time2String(IssuesDuration));
			lstStat.Items.Add("Тариф: " + TariffPrice.ToString("c"));
			lstStat.Items.Add("Коэффициент: " + Discount);
			lstStat.Items.Add("Итого: " + Price.ToString("c"));

			if (CampaignType == CampaignTypes.Sponsor)
			{
				lstStat.Items.Add("");
				lstStat.Items.Add("Программ: " + ProgramIssuesCount);
				lstStat.Items.Add("Бонус: " + DateTimeUtils.Time2String(Bonus - IssuesDuration));
			}
		}

		public static Campaign CreateInstance(int campaignTypeId, int paymentTypeId, int? massmediaId, int agencyId)
		{
			if((CampaignTypes)campaignTypeId == CampaignTypes.PackModule)
                return new CampaignPackModule(paymentTypeId, agencyId);
            return new Campaign((CampaignTypes) campaignTypeId, paymentTypeId, massmediaId, agencyId);
		}

		public void ClearModuleList()
		{
			modules = null;
		}

		public virtual bool HasModuleIssue(PresentationObject module)
		{
			if (isNew || CampaignType != CampaignTypes.Module)
				return false;
			if (modules == null)
			{
				ChildEntity = EntityManager.GetEntity((int) Entities.CampaignModule);
				modules = base.GetContent();
			}
			return modules.Select(string.Format("moduleID={0}", module.IDs[0])).Length > 0;
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

		public virtual void PrintOnAirInquire(Form owner) {}

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