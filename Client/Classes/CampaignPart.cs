using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;
using static Merlin.Classes.Campaign;

namespace Merlin.Classes
{
	internal class CampaignPart : ObjectContainer
	{
        public const string OBJECT_ID = "objectID";
        private Campaign _campaign;

        protected CampaignPart(Entity entity, DataRow row) : base(entity, row)
		{
		}

		protected CampaignPart(Entity entity) : base(entity)
		{
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
            decimal currentPrice = 0;
            if (actionName == Constants.EntityActions.Delete)
            {
                Campaign.Action.Refresh();
                currentPrice = Campaign.Action.TotalPrice;
            }

            base.DoAction(actionName, owner, interfaceObject);
			if (actionName == Constants.EntityActions.Delete)
			{
                //if (isNew) // Is Deleted
                RecalculateAndShowPriceChange(currentPrice);
			}
		}

		public void RecalculateAndShowPriceChange(decimal price)
		{
			Campaign?.RecalculateAction();

			decimal newPrice = ((Campaign != null && Campaign.Action != null) ? Campaign.Action.TotalPrice : decimal.Zero);
            ShowPriceChangeMessage(price, newPrice);
        }

        public static void ShowPriceChangeMessage(decimal price, decimal newPrice)
        {
            Dictionary<string, object> msgParameters =
                new Dictionary<string, object>(2, StringComparer.InvariantCultureIgnoreCase)
                {
                    ["oldPrice"] = price.ToString("c"),
                    ["newPrice"] = newPrice.ToString("c")
                };
            Globals.ShowCompleted((newPrice == price) ? "CampaignPriceWithoutChanged" : "CampaignPriceChanged", msgParameters);
        }

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (!ActionOnMassmedia.CheckLoggedUserRight(actionName, Campaign.Action))
				return true;
            if (actionName != Constants.EntityActions.Refresh)
                return IsMarkedAsDeleted || base.IsActionHidden(actionName, type);
            return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (!ActionOnMassmedia.CheckLoggedUserRight(actionName, Campaign.Action))
				return false;

			return base.IsActionEnabled(actionName, type);
		}

        public bool IsMarkedAsDeleted
        {
            get { return parameters[ActionOnMassmedia.ParamNames.DeleteDate] != DBNull.Value; }
        }

		public Campaign Campaign
		{
			get
			{
				if (_campaign == null)
				{
					RefreshCampaign();
				}
				return _campaign;
			}
		}

		private void RefreshCampaign()
		{
			_campaign = Campaign.GetCampaignById(CampaignId);
		}

		public int CampaignId
		{
			get { return int.Parse(this[Campaign.ParamNames.CampaignId].ToString()); }
		}





        protected void ChangePositions(Form owner)
        {
            try
            {
                ChangePositioningForm selector = new ChangePositioningForm(this);
                if (selector.ShowDialog(owner) == DialogResult.OK)
                {
                    Application.DoEvents();
                    Cursor.Current = Cursors.WaitCursor;
                    DataTable tableErrors = ErrorManager.CreateErrorsTable();

                    Campaign.Action.Refresh();
                    decimal price = Campaign.Action.TotalPrice;
                    foreach (var id in selector.SelectedIDs)
                    {
                        PresentationObject item = null;
                        try
                        {
                            Entity itemEntity = null;
                            Dictionary<string, object> parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

                            if (Campaign.CampaignType == CampaignTypes.Simple || Campaign.CampaignType == CampaignTypes.Sponsor)
                            {
                                itemEntity = EntityManager.GetEntity((int)Entities.Issue);
                                parameters[Issue.ParamNames.IssueId] = id;
                            }
                            else if (Campaign.CampaignType == CampaignTypes.Module)
                            {
                                itemEntity = EntityManager.GetEntity((int)Entities.ModuleIssue);
                                parameters[ModuleIssue.ParamNames.ModuleIssueId] = id;
                            }
                            else if (Campaign.CampaignType == CampaignTypes.PackModule)
                            {
                                itemEntity = EntityManager.GetEntity((int)Entities.PackModuleIssue);
                                parameters[ModuleIssue.ParamNames.PackModuleIssueID] = id;
                            }
                            item = itemEntity.CreateObject(parameters);

                            item.Refresh();
                            // по любому это будет кто-то из наследников Issue, или сам Issue
                            ((Issue)item).SetPosition(selector.NewPosition);
                        }
                        catch (Exception ex)
                        {

                            item.Refresh();
                            ErrorManager.AddErrorRow(tableErrors, DateTime.Parse(item[CampaignDay.ParamNames.IssueDate].ToString()), MessageAccessor.GetMessage(ex.Message));
                        }
                    }
                    if (tableErrors.Rows.Count > 0)
                    {
                        Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки изменения позиционирования", tableErrors);
                    }
                    //OnParentChanged(this, 2);
                    FireContainerRefreshed();
                    RecalculateAndShowPriceChange(price);
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        protected void SubstituteRollerForSingleIssue(Roller currentRoller)
        {
            try
            {
                Entity eRoller = EntityManager.GetEntity((int)Entities.Roller);
                Dictionary<string, object> filter = DataAccessor.CreateParametersDictionary();
                filter["isActiveOnly"] = true;
                filter["withoutID"] = currentRoller.RollerId;
                filter[Action.ParamNames.FirmId] = Campaign.Action.FirmID;
                DataTable dt = eRoller.GetContent(filter);
                if (dt.Rows.Count == 0)
                {
                    Globals.ShowInfo("CannotFindRollersForSubstitude");
                    return;
                }

                SelectionForm frm = new SelectionForm(eRoller, dt.DefaultView, Properties.Resources.TitleGetRollerForSubstitude);
                if (frm.ShowDialog(Globals.MdiParent) == DialogResult.OK)
                {
                    Cursor.Current = Cursors.WaitCursor;

                    Campaign.Action.Refresh();
                    decimal price = Campaign.Action.TotalPrice;

                    Roller r = frm.SelectedObject as Roller;
                    if (r != null)
                    {
                        Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
                            Entity, InterfaceObjects.FakeModule, Constants.Actions.Substitute);
                        procParameters["oldRollerId"] = currentRoller.RollerId;
                        procParameters["oldDuration"] = currentRoller.Duration;
                        procParameters["newRollerId"] = r.RollerId;
                        procParameters["newDuration"] = r.Duration;
                        procParameters[Campaign.ParamNames.CampaignId] = Campaign.CampaignId;
                        procParameters[Campaign.ParamNames.CampaignTypeId] = (int)Campaign.CampaignType;
                        procParameters[Issue.ParamNames.IssueId] = this[Issue.ParamNames.IssueId];

                        DataSet ds = PrepareSubstitutionParametersAndExecute(procParameters);

                        if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                            FogSoft.WinForm.Forms.MessageBox.ShowExclamation(ds.Tables[0].Rows[0]["message"].ToString());
                        //Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.RollerUnSubtitude), "Незамененные ролики", ds.Tables[0]);
                        RecalculateAndShowPriceChange(price);
                        //Refresh();
                        OnParentChanged(this, EntityManager.GetEntity((int)Entities.GeneralCampaign));
                    }
                }
            }
            finally { Cursor.Current = Cursors.Default; }
        }

        protected virtual DataSet PrepareSubstitutionParametersAndExecute(Dictionary<string, object> procParameters)
        {
            procParameters[TariffWindow.ParamNames.OriginalWindowId] = this[TariffWindow.ParamNames.OriginalWindowId];
            return DataAccessor.DoAction(procParameters) as DataSet;
        }

        protected DataTable CreateTableWithDays(DateTime issueDate)
        {
            DataTable days = new DataTable("days");
            days.Columns.Add(TariffWindow.ParamNames.WindowId, Type.GetType("System.Int32"));
            days.Columns.Add(Issue.ParamNames.IssueDate, Type.GetType("System.DateTime"));
            object[] rowVals = new object[days.Columns.Count];
            rowVals[0] = DBNull.Value;
            rowVals[1] = issueDate;
            days.Rows.Add(rowVals);
            return days;
        }
    }
}