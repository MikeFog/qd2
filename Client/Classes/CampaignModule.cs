using System;
using System.Collections.Generic;
using System.Reflection;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Controls;
using unoidl.com.sun.star.sheet;

namespace Merlin.Classes
{
    // UI-часть (DoAction) — в CampaignModule.WinForms.cs.
    // Конвенция — docs/tasks/web-migration-dialogs.md.
    internal partial class CampaignModule : CampaignOnSingleMassmedia
    {
    	public CampaignModule() : base()
    	{
    	}

    	public CampaignModule(int campaignID) : base(campaignID)
    	{
    	}

        // DoAction переехал в CampaignModule.WinForms.cs.

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

	// UI-часть (DoAction, ChangeAdvertType) — в CampaignModule.WinForms.cs.
	internal partial class CampaignModuleRollerInsideDay : CampaignRoller
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

        // DoAction переехал в CampaignModule.WinForms.cs целиком. Ветка Substitute
        // зовёт унаследованный CampaignRoller.SubstituteRoller — он тоже в
        // UI-половине (CampaignRoller.WinForms.cs), а запись в БД разведена
        // с показом журнала (CampaignRoller.ApplyRollerSubstitutionForDays).

        private ModulePricelist ModulePricelist
        {
            get
            {
                if (_pricelist == null)
                    _pricelist = new ModulePricelist(int.Parse(this[ModulePricelist.ParamNames.ModulePriceListID].ToString()));
                return _pricelist;
            }
        }

        /// <summary>Меняет предмет рекламы у ролика на выбранные даты.</summary>
        internal void ApplyAdvertTypeChange(System.Collections.IEnumerable selectedDays, int advertTypeId)
        {
            foreach (var date in selectedDays)
            {
                Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();

                procParameters[Roller.ParamNames.RollerId] = Roller.RollerId;
                procParameters[Campaign.ParamNames.CampaignId] = Campaign.CampaignId;
                procParameters[Module.ParamNames.ModuleId] = ModulePricelist.ModuleID;
                procParameters[AdvertType.ParamNames.AdvertTypeId] = advertTypeId;
                procParameters[Issue.ParamNames.IssueDate] = date;

                DataAccessor.ExecuteNonQuery("SetAdvertTypeForCommmonRoller", procParameters);
            }
            OnParentChanged(this, 1);
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