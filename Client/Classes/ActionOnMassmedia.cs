using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Classes
{
    public class ActionOnMassmedia : Action
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

		public override bool ShowPassport(IWin32Window owner)
		{
			ActionForm fAction = new ActionForm(this /*, false*/);
			fAction.ShowDialog(owner);
			return true;
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			Application.DoEvents();

			if (actionName == Constants.EntityActions.Edit)
			{
				if (ShowPassport(owner))
				{
                    //FireContainerRefreshed();
                    OnParentChanged(this, 1);
                }
			}
			else if (actionName == ActionNames.Deactivate)
				DeactivateAction();
			else if (actionName == ActionNames.Activate|| string.Compare(actionName, ActionNames.ActivateTest) == 0)
				ActivateAction(string.Compare(actionName, ActionNames.ActivateTest) == 0);
			else if (string.Compare(actionName, ActionNames.Merge) == 0)
				Merge();
			else if (string.Compare(actionName, ActionNames.ActionRollers) == 0)
				ShowRollers();
			else if (string.Compare(actionName, ActionNames.Recalculate) == 0)
			{
				Recalculate(true);
				FireContainerRefreshed();
			}
            else if (actionName == ActionNames.Clone)
                Clone();
            else if (actionName == ActionNames.SplitCampaigns)
                SplitCampaign();
            else if (actionName == ActionNames.SplitAction)
                SplitAction();
            else if (actionName == ActionNames.Restore)
                Restore(owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
		}

        protected override string DeleteConfirmationText 
		{
			get 
			{
                return string.Format(MessageAccessor.GetMessage(IsDeleted ? "DeleteActionPrompt" : "MoveAction2DeletedPrompt"), Name); 
			}
		}

        private bool IsSplitOrMergeEnabled(DateTime startDate)
		{
            if (SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsTrafficManager || !IsConfirmed) return true;
			if(startDate <= DateTime.Today)
			{
                MessageBox.ShowExclamation(MessageAccessor.GetMessage("SplitAllowedByAdmin"));
				return false;
            }
			return true;
        }

		private ActionOnMassmedia CreateNewActionForSplit()
		{
            ActionOnMassmedia newAction = new ActionOnMassmedia(Firm);
            newAction[ParamNames.IsConfirmed] = IsConfirmed;
            newAction[SecurityManager.ParamNames.UserId] = this[SecurityManager.ParamNames.UserId];
            newAction.Update();
			return newAction;
        }

		private void SplitAction()
		{
			try
			{
				if (!IsSplitOrMergeEnabled(StartDate.Date)) return;
						
                DataTable dt = Campaigns();
				if (dt.Rows.Count < 2)
				{
					MessageBox.ShowInformation(MessageAccessor.GetMessage("CanNotSplitAction"));
					return;
				}

                SelectionForm fSelector = new SelectionForm(EntityManager.GetEntity((int)Entities.CampaignOnMassmedia),
                        dt.DefaultView, "Выберите рекламные компании которые хотите перенести в новую акцию", true,
                        CheckCampaignsSelectionResultForActionSplit);

                if (fSelector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
				{
                    Cursor.Current = Cursors.WaitCursor;
                    // clone action
                    ActionOnMassmedia newAction = CreateNewActionForSplit();
                    foreach (var campaign in fSelector.AddedItems)
					{
                        campaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
                        campaign.Update();
                    }
                    Recalculate();
                    newAction.Recalculate();
                    FireContainerRefreshed();
                }
            }
			finally
			{
                Cursor.Current = Cursors.Default;
            }
		}

        private void SplitCampaign()
		{
            if (!IsSplitOrMergeEnabled(StartDate.Date)) return;

            DataTable dt = SetCampaignsFilterByType(Campaign.CampaignTypes.Simple);
            if (dt.DefaultView.Count == 0)
            {
                MessageBox.ShowInformation(MessageAccessor.GetMessage("NoCampaignsForSplit"));
                return;
            }

            SelectCampaignsForm fSelector = new SelectCampaignsForm(this, SelectionMode.Split);

            if (fSelector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
				// all data has been collected, let's go split
				try
				{
                    Cursor.Current = Cursors.WaitCursor;
                    // clone action
                    ActionOnMassmedia newAction = CreateNewActionForSplit();

                    foreach (SplitRule rule in fSelector.SplitRules)
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
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        private bool CheckCampaignsSelectionResultForActionSplit(SelectionForm selectionForm)
        {
			if(selectionForm.AddedItems.Count == this.Campaigns().Rows.Count)
			{
                MessageBox.ShowExclamation(MessageAccessor.GetMessage("TooManyCampaignsSelected"));
                return false;
			}
            if (selectionForm.AddedItems.Count == 0)
            {
                MessageBox.ShowExclamation(MessageAccessor.GetMessage("NoCampaignSelected"));
                return false;
            }
            return true;
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

        private void Clone()
        {
			try
			{
				SelectCampaignsForm form = new SelectCampaignsForm(this, SelectionMode.Clone);
				if (form.ShowDialog(Globals.MdiParent) == DialogResult.OK)
				{
					Cursor.Current = Cursors.WaitCursor;

					// clone action
					ActionOnMassmedia newAction = new ActionOnMassmedia(Firm);
					newAction.Update();

					DataTable tableErrors = Campaign.CreateErrorsTable();

					foreach (var item in form.SelectedItems)
					{
						int campaignTypeId = int.Parse(item.presentationObject[Campaign.ParamNames.CampaignTypeId].ToString());

                        Campaign newCampaign = Campaign.CreateInstance(
							campaignTypeId,
							int.Parse(item.presentationObject[Campaign.ParamNames.PaymentTypeID].ToString()),
							campaignTypeId == (int)Campaign.CampaignTypes.PackModule ? 
								null : (int?)int.Parse(item.presentationObject[Campaign.ParamNames.MassmediaId].ToString()),
							int.Parse(item.presentationObject[Campaign.ParamNames.AgencyID].ToString()));
						newCampaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
						newCampaign.Update();
						int shiftInDays = (item.date - DateTime.Parse(item.presentationObject[Campaign.ParamNames.StartDate].ToString())).Days;

						Campaign selectedCampaign = (Campaign)item.presentationObject;

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
                    if (tableErrors.Rows.Count > 0)
						Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки клонирования", tableErrors);

					Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.Issue), string.Format("Клонированные выходы в эфир новой акции № {0}", newAction.ActionId), newAction.Issues);
					
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
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
                    Campaign.AddErrorRow(tableErrors, newDate, string.Format(Properties.Resources.PackModulePricelistNotFound, module.Name));
                else
                {
                    try
                    {
						newCampaign.AddPackModuleIssue((PackModulePricelist)pricelist, issue.Roller, issue.Position, newDate, null);
                    }
                    catch (Exception ex)
                    {
                        Campaign.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
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
                    Campaign.AddErrorRow(tableErrors, newDate, string.Format(Properties.Resources.SponsorPricelistNotFound, issue.SponsorProgram.Name));
                    continue; 
				}
                    
				SponsorTariff tariff = pricelist.GetTariffBydate(newDate);
                if (tariff == null)
                {
                    Campaign.AddErrorRow(tableErrors, newDate, string.Format(Properties.Resources.SponsorTariffNotFound, issue.SponsorProgram.Name));
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
					Campaign.AddErrorRow(tableErrors, newDate, string.Format(Properties.Resources.ModulePricelistNotFound, module.Name));
				else
				{
					try
					{
						newCampaign.AddModuleIssue(module, issue.Roller, pricelist, newDate, issue.Position, null);
					}
                    catch (Exception ex)
                    {
                        Campaign.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
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
				DateTime newDate = issue.IssueDate.AddDays(shiftInDays);

                TariffWindow window = mm.GetTariffWindow(newDate);
				if (window != null)
                {
					try
					{
						newCampaign.AddIssue(issue.Roller, window, issue.Position, null);
					}
					catch (Exception ex)
					{
						Campaign.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
					}
                }
				else
				{
					Campaign.AddErrorRow(tableErrors, newDate, "Рекламное окно не найдено");
				}
			}
        }

        private void ShowRollers()
		{
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ActionRollersStat), 
				string.Format("Статистика по роликам для акции №{0}", ActionId), 
				new Dictionary<string, object>{{"actionID", ActionId}});
		}

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

		private void Restore(IWin32Window owner)
		{
			try
			{
                Globals.SetWaitCursor((Form)owner);

                Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
                procParameters.Add("actionID", ActionId);
				DataAccessor.ExecuteNonQuery("ActionRestore", procParameters);
				OnObjectDeleted(this);
				MessageBox.ShowCompleted(MessageAccessor.GetMessage("ActionRestored"));
			}
			finally
			{
				Globals.SetDefaultCursor((Form)owner);
			}
		}

		public void Merge()
		{
            if (!IsSplitOrMergeEnabled(StartDate.Date)) return;

            Entity entityAction = EntityManager.GetEntity((int) Entities.Action);
			Dictionary<string, object> parametersActions =
				DataAccessor.PrepareParameters(entityAction, InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			parametersActions[Firm.ParamNames.FirmId] = Firm.FirmId;
			parametersActions[SecurityManager.ParamNames.UserId] = parameters[SecurityManager.ParamNames.UserId];
            parametersActions["withoutActionId"] = ActionId;
            parametersActions["isShowActivate"] = IsConfirmed;
            parametersActions["isShowNotActivate"] = !IsConfirmed;
            DataSet ds = DataAccessor.DoAction(parametersActions) as DataSet;
			if (ds != null)
			{
				DataTable table = ds.Tables[Constants.TableNames.Data];
				SelectionForm selection = new SelectionForm(entityAction, table.DefaultView, "Объдинить с ...");
				if (selection.ShowDialog() == DialogResult.OK && selection.SelectedObject != null && selection.SelectedObject is ActionOnMassmedia)
				{
                    ActionOnMassmedia action2 = (ActionOnMassmedia)selection.SelectedObject;
                    if (!IsSplitOrMergeEnabled(action2.StartDate.Date)) return;

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
			}
		}

		public void Recalculate(bool refreshFlag = true, DateTime? todayDate = null)
		{
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
                EntityManager.GetEntity((int)Entities.Action), InterfaceObjects.FakeModule,
                Constants.Actions.Recalculate);

			procParameters[ParamNames.ActionId] = ActionId;

			if (todayDate.HasValue)
				procParameters["todayDate"] = todayDate.Value;

            DataAccessor.DoAction(procParameters);

			if (refreshFlag)
				Refresh();
        }

        private void SetChildEntity()
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.CampaignOnMassmedia);
		}

		private void DeactivateAction()
		{
			if(!(SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsTrafficManager) && StartDate < DateTime.Today)
			{
				MessageBox.ShowExclamation(Properties.Resources.DeactivationNotAllowed);
				return;
			}

			if (Globals.ShowQuestion("ConfirmActionDeactivate", null) == DialogResult.Yes)
			{
				try
				{
					Cursor.Current = Cursors.WaitCursor;

					DataAccessor.PrepareParameters(
						parameters, entity, InterfaceObjects.FakeModule, Constants.Actions.Deactivate);
					DataAccessor.DoAction(parameters);
					Refresh();
					OnObjectDeleted(this);
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
		}

		private void ActivateAction(bool isTestActivation)
		{
			if (!isTestActivation && Globals.ShowQuestion("ConfirmActionActivate", null) != DialogResult.Yes)
				return;

			try
			{
				if (!isTestActivation && !CheckActionRollersAndProgramIssues()) return;

				Cursor.Current = Cursors.WaitCursor;
				parameters["isTestActivate"] = isTestActivation;

				DataAccessor.PrepareParameters(
					parameters, entity, InterfaceObjects.FakeModule, Constants.Actions.Activate);
				DataSet ds = (DataSet)DataAccessor.DoAction(parameters);

				if (ds.Tables["activated"].Rows.Count > 0)
				{
					Globals.ShowSimpleJournal(
						EntityManager.GetEntity((int)Entities.RollerIssueActivated),
						(isTestActivation
							? "Предварительный просмотр результатов активации"
							: "Результаты активации") + ": активированное"
						, ds.Tables["activated"]);
				}

				if (ds.Tables["notactivated"].Rows.Count > 0)
				{
					Globals.ShowSimpleJournal(
						EntityManager.GetEntity((int)Entities.RollerIssueActivated),
						(isTestActivation
							? "Предварительный просмотр результатов активации"
							: "Результаты активации") + ": неактивированное"
						, ds.Tables["notactivated"]);
				}

				if (!isTestActivation)
				{
					Refresh();
                    Recalculate();
                    OnObjectDeleted(this);
                }
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

        private bool CheckActionRollersAndProgramIssues()
        {
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.ActionId] = ActionId;
			DataTable dtRollers = DataAccessor.LoadDataSet("RollersWithoutAdvertype", procParameters).Tables[0];
            DataTable dtProgramIssues = DataAccessor.LoadDataSet("RollersWithoutAdvertype", procParameters).Tables[1];

            if (dtRollers.Rows.Count > 0)
			{
				SetAdvertTypeOrSubstituteRoller();
				MessageBox.ShowExclamation(MessageAccessor.GetMessage("ActivationWithRollersWithoutAdvType"));

                return false;
            }

			if (dtProgramIssues.Rows.Count > 0)
			{
                MessageBox.ShowExclamation(Properties.Resources.ActivationWithProgramIssuesWithoutAdvType);
                return false;
            }
			return true;
        }

        public static ActionOnMassmedia GetActionById(int actionId)
		{
			ActionOnMassmedia action = new ActionOnMassmedia(actionId);
			action.Refresh();
			return action;
		}

        internal void DisplayData(ListBox lstStat)
        {
            lstStat.Items.Clear();
            lstStat.Items.Add("Начало: " + (StartDate == DateTime.MinValue ? string.Empty : StartDate.ToShortDateString()));
            lstStat.Items.Add("Окончание: " + (FinishDate == DateTime.MinValue ? string.Empty : FinishDate.ToShortDateString()));
            lstStat.Items.Add("Выпусков: " + this["iCount"]);
            lstStat.Items.Add("Общее время: " + this["duration"]);
            lstStat.Items.Add("Тариф: " + TariffPrice.ToString("c"));
            lstStat.Items.Add("Итого: " + TotalPrice.ToString("c"));
        }

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