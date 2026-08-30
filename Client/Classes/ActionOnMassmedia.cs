using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;

namespace Merlin.Classes
{
    // Часть, работающая с диалогами, постепенно переезжает в ActionOnMassmedia.WinForms.cs.
	// Конвенция разреза — docs/tasks/web-migration-dialogs.md.
	public partial class ActionOnMassmedia : Action
	{
		internal class SplitRule
		{
			public enum SplitType
			{
				ByPeriod = 1,
				ByRollers = 2
			}

			public SplitRule(CampaignOnSingleMassmedia campaign)
			{
				this.campaign = campaign;
			}

			public readonly CampaignOnSingleMassmedia campaign;
			public SplitType splitType;
			public DateTime ?date;
			public List<PresentationObject> rollers;
		}

		public ActionOnMassmedia()
			: base(EntityManager.GetEntity((int) Entities.Action))
		{
			SetChildEntity();
		}

		public ActionOnMassmedia(int actionID)
			: base(EntityManager.GetEntity((int) Entities.Action), actionID)
		{
			SetChildEntity();
		}

		public ActionOnMassmedia(DataRow row)
			: base(EntityManager.GetEntity((int) Entities.Action), row)
		{
			SetChildEntity();
		}

		protected ActionOnMassmedia(Entity entity) : base(entity)
		{
			SetChildEntity();
        }

        public ActionOnMassmedia(PresentationObject firm)
			:
				base(EntityManager.GetEntity((int) Entities.Action), firm)
		{
			SetChildEntity();
			this[ParamNames.TotalPrice] = (decimal)0;
			this[ParamNames.IsConfirmed] = false;
            this[ParamNames.Ratio] = (decimal)1;
        }

		public int UserID
		{
			get
			{
				return ParseHelper.ParseToInt32(parameters[SecurityManager.ParamNames.UserId].ToString());
			}
		}

		private SecurityManager.User user;

		public SecurityManager.User User
		{
			get
			{
				if (user == null)
					user = SecurityManager.GetUser(UserID);
				return user;
			}
		}

		public override bool Refresh()
		{
			user = null;
			return base.Refresh();
		}

		// ShowPassport переехал в ActionOnMassmedia.WinForms.cs (открывает ActionForm).

		// DoAction переехал в ActionOnMassmedia.WinForms.cs.

        protected override string DeleteConfirmationText 
		{
			get 
			{
                return string.Format(MessageAccessor.GetMessage(IsDeleted ? "DeleteActionPrompt" : "MoveAction2DeletedPrompt"), Name); 
			}
		}

		/// <summary>
		/// Разрешено ли делить/объединять акцию. <paramref name="messageKey"/> — ключ
		/// MessageAccessor с причиной отказа, null если разрешено.
		/// </summary>
		internal bool CanSplitOrMerge(DateTime startDate, out string messageKey)
		{
			messageKey = null;
            if (SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsTrafficManager || !IsConfirmed) return true;
			if(startDate <= DateTime.Today)
			{
				messageKey = "SplitAllowedByAdmin";
				return false;
            }
			return true;
        }

		/// <summary>
		/// Кампании — кандидаты на перенос в новую акцию. null, если делить нечего;
		/// тогда <paramref name="messageKey"/> содержит ключ причины.
		/// </summary>
		internal DataTable GetCampaignsForSplit(out string messageKey)
		{
			messageKey = null;
			DataTable dt = Campaigns();
			if (dt.Rows.Count < 2)
			{
				messageKey = "CanNotSplitAction";
				return null;
			}
			return dt;
		}

		/// <summary>
		/// Проверяет выбор пользователя для деления акции.
		/// </summary>
		internal bool IsSplitSelectionValid(int selectedCount, out string messageKey)
		{
			messageKey = null;
			if (selectedCount == Campaigns().Rows.Count)
			{
				messageKey = "TooManyCampaignsSelected";
				return false;
			}
			if (selectedCount == 0)
			{
				messageKey = "NoCampaignSelected";
				return false;
			}
			return true;
		}

		/// <summary>
		/// Переносит выбранные кампании в новую акцию и пересчитывает обе.
		/// </summary>
		internal void ApplySplitAction(IList<PresentationObject> campaignsToMove)
		{
			ActionOnMassmedia newAction = CreateNewActionForSplit();
			foreach (var campaign in campaignsToMove)
			{
				campaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
				campaign.Update();
			}
			Recalculate();
			newAction.Recalculate();
		}

		private ActionOnMassmedia CreateNewActionForSplit()
		{
            ActionOnMassmedia newAction = new ActionOnMassmedia(Firm);
            newAction[ParamNames.IsConfirmed] = IsConfirmed;
            newAction[SecurityManager.ParamNames.UserId] = this[SecurityManager.ParamNames.UserId];
            newAction.Update();
			return newAction;
        }

		// SplitAction, IsSplitOrMergeEnabled и CheckCampaignsSelectionResultForActionSplit
		// переехали в ActionOnMassmedia.WinForms.cs (диалоги и показ сообщений).

        // SplitCampaign переехал в ActionOnMassmedia.WinForms.cs.

        /// <summary>Кампании — кандидаты на разделение по кампаниям (тип Simple).</summary>
        internal bool CanSplitCampaign(out string messageKey)
        {
            messageKey = null;
            if (SetCampaignsFilterByType(Campaign.CampaignTypes.Simple).DefaultView.Count == 0)
            {
                messageKey = "NoCampaignsForSplit";
                return false;
            }
            return true;
        }

        /// <summary>Делит акцию по правилам <paramref name="splitRules"/> на новую акцию.</summary>
        internal void ApplySplitCampaign(IEnumerable<SplitRule> splitRules)
        {
            ActionOnMassmedia newAction = CreateNewActionForSplit();

            foreach (SplitRule rule in splitRules)
            {
                Campaign newCampaign = Campaign.CreateInstance(
                    int.Parse(rule.campaign[Campaign.ParamNames.CampaignTypeId].ToString()),
                    int.Parse(rule.campaign[Campaign.ParamNames.PaymentTypeID].ToString()),
                    int.Parse(rule.campaign[Campaign.ParamNames.MassmediaId].ToString()),
                    int.Parse(rule.campaign[Campaign.ParamNames.AgencyID].ToString()));
                newCampaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
                newCampaign[Campaign.ParamNames.ManagerDiscount] = rule.campaign[Campaign.ParamNames.ManagerDiscount];
                newCampaign.Update();
                MoveIssues(newCampaign, rule);
            }
            Recalculate();
            newAction.Recalculate();
            OnParentChanged(this, 1);
        }

		private void MoveIssues(Campaign newCampaign, SplitRule rule)
		{
			Dictionary<string, object> procParameters =	new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
			{
				["splitType"] = (int)rule.splitType,
                ["oldCampaignId"] = rule.campaign.CampaignId,
                ["newCampaignId"] = newCampaign.CampaignId
            };

            if (rule.splitType == SplitRule.SplitType.ByRollers)
            {
                foreach (var roller in rule.rollers)
                {
                    procParameters[Roller.ParamNames.RollerId] = int.Parse(roller[Roller.ParamNames.RollerId].ToString());
                    DataAccessor.ExecuteNonQuery("MoveIssues2NewCampaign", procParameters);
                }
            }
            else
            {
				procParameters["splitDate"] = rule.date;
                DataAccessor.ExecuteNonQuery("MoveIssues2NewCampaign", procParameters);
            }
        }

		internal DataTable SetCampaignsFilterByType(Campaign.CampaignTypes type)
		{
            DataTable filteredCampaigns = Campaigns();
            filteredCampaigns.DefaultView.RowFilter = string.Format("campaignTypeID = {0}", (int)type);

            return filteredCampaigns;
        }

        // Clone переехал в ActionOnMassmedia.WinForms.cs.

        /// <summary>
        /// Клонирует акцию с выбранными кампаниями (<paramref name="selectedItems"/> —
        /// дата клонирования и исходная кампания). Возвращает новую акцию;
        /// <paramref name="tableErrors"/> — таблица ошибок по отдельным кампаниям
        /// (пустая, если ошибок не было).
        /// </summary>
        internal ActionOnMassmedia ApplyClone(IEnumerable<(DateTime date, PresentationObject campaign)> selectedItems, out DataTable tableErrors)
        {
            ActionOnMassmedia newAction = new ActionOnMassmedia(Firm);
            newAction.Update();

            tableErrors = ErrorManager.CreateErrorsTable();

            foreach (var item in selectedItems)
            {
                int campaignTypeId = int.Parse(item.campaign[Campaign.ParamNames.CampaignTypeId].ToString());

                Campaign newCampaign = Campaign.CreateInstance(
                    campaignTypeId,
                    int.Parse(item.campaign[Campaign.ParamNames.PaymentTypeID].ToString()),
                    campaignTypeId == (int)Campaign.CampaignTypes.PackModule ?
                        null : (int?)int.Parse(item.campaign[Campaign.ParamNames.MassmediaId].ToString()),
                    int.Parse(item.campaign[Campaign.ParamNames.AgencyID].ToString()));
                newCampaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
                newCampaign.Update();
                int shiftInDays = (item.date - DateTime.Parse(item.campaign[Campaign.ParamNames.StartDate].ToString())).Days;

                Campaign selectedCampaign = (Campaign)item.campaign;

                if (selectedCampaign.CampaignType == Campaign.CampaignTypes.Simple)
                    CloneRollerIssues(selectedCampaign, newCampaign, shiftInDays, tableErrors);
                else if (selectedCampaign.CampaignType == Campaign.CampaignTypes.Module)
                    CloneModuleIssues(selectedCampaign, newCampaign, shiftInDays, tableErrors);
                else if (selectedCampaign.CampaignType == Campaign.CampaignTypes.Sponsor)
                {
                    CloneProgramIssues(selectedCampaign, newCampaign, shiftInDays, tableErrors);
                    CloneRollerIssues(selectedCampaign, newCampaign, shiftInDays, tableErrors);
                }
                else if (selectedCampaign.CampaignType == Campaign.CampaignTypes.PackModule)
                {
                    ClonePackModuleIssues(selectedCampaign, (CampaignPackModule)newCampaign, shiftInDays, tableErrors);
                }
            }
            ((ActionOnMassmedia)newAction).Recalculate();
            OnParentChanged(this, 1);
            return newAction;
        }

        private void ClonePackModuleIssues(Campaign campaign, CampaignPackModule newCampaign, int shiftInDays, DataTable tableErrors)
        {
            campaign.ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleIssue);
            foreach (DataRow item in campaign.GetContent().Rows)
            {
                PackModuleIssue issue = new PackModuleIssue(item);
                DateTime newDate = issue.IssueDate.AddDays(shiftInDays);
                // есть ли прайс-лист для этого программы в новом дне ?
                PackModule module = issue.PackModule;
                Pricelist pricelist = module.GetPriceList(newDate);
                if (pricelist == null)
                    ErrorManager.AddErrorRow(tableErrors, newDate, string.Format(Properties.Resources.PackModulePricelistNotFound, module.Name));
                else
                {
                    try
                    {
						newCampaign.AddPackModuleIssue((PackModulePricelist)pricelist, issue.Roller, issue.Position, newDate, null);
                    }
                    catch (Exception ex)
                    {
                        ErrorManager.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
                    }
                }
            }
        }

        private void CloneProgramIssues(Campaign campaign, Campaign newCampaign, int shiftInDays, DataTable tableErrors)
        {
			ProgramPartOfSponsorCampaign part = new ProgramPartOfSponsorCampaign(campaign.CampaignId);
            foreach (DataRow item in part.GetProgramIssues().Rows)
            {
                ProgramIssue issue = new ProgramIssue(item);
                DateTime newDate = issue.IssueDate.AddDays(shiftInDays);
				// есть ли прайс-лист для этой программы в новом дне ?
				SponsorPricelist pricelist = issue.SponsorProgram.GetPricelist(newDate);
                if (pricelist == null) 
				{
                    ErrorManager.AddErrorRow(tableErrors, newDate, string.Format(Properties.Resources.SponsorPricelistNotFound, issue.SponsorProgram.Name));
                    continue; 
				}
                    
				SponsorTariff tariff = pricelist.GetTariffBydate(newDate);
                if (tariff == null)
                {
                    ErrorManager.AddErrorRow(tableErrors, newDate, string.Format(Properties.Resources.SponsorTariffNotFound, issue.SponsorProgram.Name));
                    continue;
                }

				newCampaign.AddProgramIssue(issue.SponsorProgram, tariff.TariffId, newDate, tariff.Price, pricelist.Bonus, false);
            }
        }

        private void CloneModuleIssues(ObjectContainer campaign, Campaign newCampaign, int shiftInDays, DataTable tableErrors)
		{
            campaign.ChildEntity = EntityManager.GetEntity((int)Entities.ModuleIssue);
            foreach(DataRow item in campaign.GetContent().Rows)
			{
				ModuleIssue issue = new ModuleIssue(item);
                DateTime newDate = issue.IssueDate.AddDays(shiftInDays);
				// есть ли прайс-лист для этого модуля в новом дне ?
				Module module = issue.Module;
				ModulePricelist pricelist =  module.GetPriceList(newDate);
				if (pricelist == null)
					ErrorManager.AddErrorRow(tableErrors, newDate, string.Format(Properties.Resources.ModulePricelistNotFound, module.Name));
				else
				{
					try
					{
						newCampaign.AddModuleIssue(module, issue.Roller, pricelist, newDate, issue.Position, null, skipCampaignRecalc: true);
					}
                    catch (Exception ex)
                    {
                        ErrorManager.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
                    }
                }
            }
        }

        private void CloneRollerIssues(ObjectContainer campaign, Campaign newCampaign, int shiftInDays, DataTable tableErrors)
        {
			Massmedia mm = (new CampaignOnSingleMassmedia(newCampaign.CampaignId)).Massmedia;
			campaign.ChildEntity = EntityManager.GetEntity((int)Entities.Issue);
			foreach (DataRow item in campaign.GetContent().Rows)
			{
                RollerIssue issue = new RollerIssue(item);
				DateTime newDate = issue.IssueDateOriginal.AddDays(shiftInDays);

                TariffWindow window = mm.GetTariffWindow(newDate);
				if (window != null)
                {
					try
					{
						newCampaign.AddIssue(issue.Roller, window, issue.Position, null, skipCampaignRecalc: true);
					}
					catch (Exception ex)
					{
						ErrorManager.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
					}
                }
				else
				{
					ErrorManager.AddErrorRow(tableErrors, newDate, "Рекламное окно не найдено");
				}
			}
        }

        // ShowRollers переехал в ActionOnMassmedia.WinForms.cs.

		public static bool CheckLoggedUserRight(string actionName, ActionOnMassmedia action)
		{
			if (SecurityManager.LoggedUser.Id != action.UserID
				&& !SecurityManager.LoggedUser.IsRightToEditForeignActions()
				&& (!SecurityManager.LoggedUser.IsRightToEditGroupActions() || action.User == null || !SecurityManager.LoggedUser.IsInGroup(action.User.Groups))
				&& (new List<string> { ActionNames.Activate, ActionNames.ActivateTest, ActionNames.Deactivate,
					ActionNames.Merge, ActionNames.Recalculate, Constants.EntityActions.Edit,
					Constants.EntityActions.Delete, Action.ActionNames.ChangeFirm,
					Action.ActionNames.ChangeCreator, Issue.ActionNames.SetFirst, Issue.ActionNames.SetSecond,
					Issue.ActionNames.SetLast, Issue.ActionNames.SetUnknow, Constants.EntityActions.Transfer,
					Constants.Actions.Substitute, Campaign.ActionNames.ChangePaymentType,
					Campaign.ActionNames.ChangeAgency}).Contains(actionName))
				return false;
			return true;
		}

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (!CheckLoggedUserRight(actionName, this))
				return true;
            if (actionName == ActionNames.Activate || string.Compare(actionName, ActionNames.ActivateTest) == 0)
                return base.IsActionHidden(actionName, type) || IsConfirmed;
            if (actionName == ActionNames.Deactivate)
                return base.IsActionHidden(actionName, type) || !IsConfirmed;

            return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (!CheckLoggedUserRight(actionName, this))
				return false;

			return base.IsActionEnabled(actionName, type);
		}

		// Restore, Merge и ActivateAction переехали в ActionOnMassmedia.WinForms.cs.
		// Restore и ActivateAction перенесены целиком без разреза: Restore
		// использует Globals.SetWaitCursor/SetDefaultCursor (не связано с
		// ShowDialog, но само по себе UI); ActivateAction слишком плотно
		// переплетён с отображением трёх журналов результатов через
		// специально созданные для показа виртуальные сущности — деловая
		// логика активации и подготовка данных для отображения не разделяются
		// без переделки самого способа сообщать результат активации.

		/// <summary>Кандидаты на объединение с этой акцией. null, если объединять не с чем.</summary>
		internal DataTable GetActionsForMerge()
		{
			Entity entityAction = EntityManager.GetEntity((int) Entities.Action);
			Dictionary<string, object> parametersActions =
				DataAccessor.PrepareParameters(entityAction, InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			parametersActions[Firm.ParamNames.FirmId] = Firm.FirmId;
			parametersActions[SecurityManager.ParamNames.UserId] = parameters[SecurityManager.ParamNames.UserId];
			parametersActions["withoutActionId"] = ActionId;
			parametersActions["isShowActivate"] = IsConfirmed;
			parametersActions["isShowNotActivate"] = !IsConfirmed;
			DataSet ds = DataAccessor.DoAction(parametersActions) as DataSet;
			return ds?.Tables[Constants.TableNames.Data];
		}

		/// <summary>Объединяет эту акцию с <paramref name="action2"/>.</summary>
		internal void ApplyMerge(ActionOnMassmedia action2)
		{
			Dictionary<string, object> parametersMerge = DataAccessor.CreateParametersDictionary();
			parametersMerge["firstActionID"] = ActionId;
			parametersMerge["secondActionID"] = action2.ActionId;
			parametersMerge["liveActionID"] = 0;
			DataAccessor.ExecuteNonQuery("MergeActions", parametersMerge);
			OnParentChanged(this, 1);
			/*
			int liveActionID = (int) parametersMerge["liveActionID"];
			if (liveActionID > 0)
			{
				ActionOnMassmedia action = GetActionById(liveActionID);
				action.Recalculate();
				OnParentChanged(this, 1);
			}
			*/
		}

		public void Recalculate(bool refreshFlag = true, DateTime? todayDate = null)
		{
			using (OperationScope.Start("ActionRecalculate"))
			{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.ActionId] = ActionId;

			if (todayDate.HasValue)
				procParameters["todayDate"] = todayDate.Value;

			// OUTPUT parameter
			procParameters[ParamNames.TotalPrice] = DBNull.Value;
			var oldTiotalPrice = TotalPrice;

            DataAccessor.ExecuteNonQuery("ActionRecalculate", procParameters);
			
			// подтянем OUTPUT в объект (на случай, если refreshFlag = false)
			if (procParameters.ContainsKey(ParamNames.TotalPrice) &&
				procParameters[ParamNames.TotalPrice] != DBNull.Value)
			{
				this[ParamNames.TotalPrice] = procParameters[ParamNames.TotalPrice];
			}

			if (refreshFlag)
				Refresh();

			if (oldTiotalPrice > TotalPrice && IsConfirmed)
				CorrectPaymentAction();
		}
		}

		private void CorrectPaymentAction()
		{
            var p = DataAccessor.CreateParametersDictionary();

            p[ParamNames.ActionId] = ActionId;
            DataAccessor.ExecuteNonQuery("PaymentAction_CorrectByActionTotalPrice", p, 30, true);
        }

        private void SetChildEntity()
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.CampaignOnMassmedia);
		}

		// DeactivateAction переехал в ActionOnMassmedia.WinForms.cs.

		/// <summary>Можно ли деактивировать акцию. false — <paramref name="errorMessage"/> заполнен.</summary>
		internal bool CanDeactivate(out string errorMessage)
		{
			if (!(SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsTrafficManager) && StartDate < DateTime.Today)
			{
				errorMessage = Properties.Resources.DeactivationNotAllowed;
				return false;
			}
			errorMessage = null;
			return true;
		}

		internal void ApplyDeactivate()
		{
			DataAccessor.PrepareParameters(
				parameters, entity, InterfaceObjects.FakeModule, Constants.Actions.Deactivate);
			DataAccessor.DoAction(parameters);
			Refresh();
			OnObjectDeleted(this);
		}

        // ActivateAction переехал в ActionOnMassmedia.WinForms.cs (см. комментарий выше).

        // CheckActionRollersAndProgramIssues переехал в
        // ActionOnMassmedia.WinForms.cs (вызывает UI-метод
        // SetAdvertTypeOrSubstituteRoller и сам содержит показ сообщений;
        // используется только из уже перенесённого ActivateAction).

        public static ActionOnMassmedia GetActionById(int actionId)
		{
			ActionOnMassmedia action = new ActionOnMassmedia(actionId);
			action.Refresh();
			return action;
		}

        // DisplayData(ListBox) переехал в ActionOnMassmedia.WinForms.cs.

        public DataTable Issues
        {
			get
			{
				Dictionary<string, object> procParameters = new Dictionary<string, object>
				{
					[ParamNames.ActionId] = ActionId,
				};
				
				return DataAccessor.LoadDataSet("ActionIssues", procParameters).Tables[0];
            }
        }
    }
}
