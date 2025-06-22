using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Controls;
using Merlin.Forms;
using unoidl.com.sun.star.sheet;

namespace Merlin.Classes
{
    internal class CampaignModule : CampaignOnSingleMassmedia
    {
    	public CampaignModule() : base()
    	{
    	}

    	public CampaignModule(int campaignID) : base(campaignID)
    	{
    	}

    	public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName == ActionNames.ShowRollers)
                ShowModuleRollers();
            if (actionName == ActionNames.ShowDays)
                ShowModuleDays();
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private void ShowModuleDays()
        {
            ChildEntity = EntityManager.GetEntity((int)Entities.ModuleCampaignDay);
            FireContainerRefreshed();
        }

        private void ShowModuleRollers()
        {
            ChildEntity = EntityManager.GetEntity((int)Entities.CampaignModule);
            FireContainerRefreshed();
        }

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (actionName == ActionNames.ShowRollers)
				return type != ViewType.Tree;
			if (actionName == ActionNames.ShowDays)
				return type != ViewType.Tree;

			return base.IsActionHidden(actionName, type);
		}

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (actionName == ActionNames.ShowRollers)
				return type == ViewType.Tree && ChildEntity != null && ChildEntity.Id != (int)Entities.CampaignModule;
            if (actionName == ActionNames.ShowDays)
				return type == ViewType.Tree && ChildEntity != null && ChildEntity.Id != (int)Entities.ModuleCampaignDay;
            else
                return base.IsActionEnabled(actionName, type);
        }
    }

	internal class CampaignModuleRollerInsideDay : CampaignRoller
	{
        private ModulePricelist _pricelist;

        public CampaignModuleRollerInsideDay()
			:base(EntityManager.GetEntity((int)Entities.CampaignModuleRollerInsideDay))
		{
		}

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (actionName == Constants.Actions.Substitute)
                return !ModulePricelist.HasRollerAssigned;
            if(string.Compare(actionName, Roller.ActionNames.ChangeAdvertType, StringComparison.OrdinalIgnoreCase) == 0)
                return ModulePricelist.HasRollerAssigned;

            return base.IsActionEnabled(actionName, type);
        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Compare(actionName, Roller.ActionNames.ChangeAdvertType, StringComparison.OrdinalIgnoreCase) == 0)
                ChangeAdvertType((Form)owner);
            else if (string.Compare(actionName, Constants.Actions.Substitute, StringComparison.OrdinalIgnoreCase) == 0)
                SubstituteRoller((Form)owner, 2);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private ModulePricelist ModulePricelist
        {
            get
            {
                if (_pricelist == null)
                    _pricelist = new ModulePricelist(int.Parse(this[ModulePricelist.ParamNames.ModulePriceListID].ToString()));
                return _pricelist;
            }
        }

        private void ChangeAdvertType(Form parentForm)
        {
            try
            {
                RollerChangeAdvertTypeForm form = new RollerChangeAdvertTypeForm(Roller, Campaign, ModulePricelist.ModuleID, null);
                if (form.ShowDialog(parentForm) == DialogResult.OK)
                {
                    Application.DoEvents();
                    Cursor.Current = Cursors.WaitCursor;
                    foreach(var date in form.SelectedDays)
                    {
                        Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();

                        procParameters[Roller.ParamNames.RollerId] = Roller.RollerId;
                        procParameters[Campaign.ParamNames.CampaignId] = Campaign.CampaignId;
                        procParameters[Module.ParamNames.ModuleId] = ModulePricelist.ModuleID;
                        procParameters[AdvertType.ParamNames.AdvertTypeId] = form.AdvertTypeId;
                        procParameters[Issue.ParamNames.IssueDate] = date;

                        DataAccessor.ExecuteNonQuery("SetAdvertTypeForCommmonRoller", procParameters);
                    }
                    OnParentChanged(this, 1);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                parentForm.Cursor = Cursors.Default;
            }
        }
    }

    internal class CampaignModuleRollerIssue : CampaignPart
	{
		public CampaignModuleRollerIssue()
			: base(EntityManager.GetEntity((int)Entities.CampaignModuleRollerIssue))
		{
		}
	}
}