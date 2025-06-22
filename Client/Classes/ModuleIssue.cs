using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using static Merlin.Classes.CampaignOnSingleMassmedia;

namespace Merlin.Classes
{
	internal class ModuleInCampaign: CampaignPart
	{
        public ModuleInCampaign() : base(GetEntity())
		{
		}

		public ModuleInCampaign(DataRow row) : base(GetEntity(), row)
		{
		}

		public static Entity GetEntity()
		{
			return EntityManager.GetEntity((int)Entities.CampaignModule);
		}

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
			if (actionName == Campaign.ActionNames.DeleteIssues)
				DeleteIssues((Form)owner);
            else if (actionName == Constants.Actions.ChangePositions)
                ChangePositions((Form)owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private void DeleteIssues(Form owner)
        {
            Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters[CampaignPart.OBJECT_ID] = this[Module.ParamNames.ModuleId];
			if (Campaign.DeleteIssues(owner, false, parameters, isFireEvent: false))
				FireContainerRefreshed();
        }
    }

	internal class ModuleIssue : Issue
	{
		private Module _module;
        private Roller _roller;
		private ModulePricelist _pricelist;

        public ModuleIssue() : base(GetEntity())
		{
		}

		public ModuleIssue(DataRow row) : base(GetEntity(), row)
		{
		}

		public ModuleIssue(Campaign campaign, Module module, PresentationObject roller,
						   ModulePricelist modulePriceList, DateTime date, bool isConfirmed, RollerPositions position, int? grantorID)
			: this()
		{
			this[Module.ParamNames.ModuleId] = module.IDs[0];
			this[Roller.ParamNames.RollerId] = roller.IDs[0];
			this[ModulePricelist.ParamNames.ModulePriceListID] = modulePriceList.ModulePriceListID;
			this[RollerIssue.ParamNames.IssueDate] = date.Date;
			this[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			this[RollerIssue.ParamNames.RollerDuration] = roller[Roller.ParamNames.Duration];
			this[Action.ParamNames.IsConfirmed] = isConfirmed;
			this[ParamNames.Position] = (int)position;
			this[ParamNames.TariffPrice] = modulePriceList.Price;
			this["grantorID"] = (grantorID ?? (object)DBNull.Value);
		}

		public override bool Delete()
		{
			bool res = base.Delete();
			Campaign campaign =
				new Campaign(int.Parse(parameters[Campaign.ParamNames.CampaignId].ToString()));
			((ActionOnMassmedia)campaign.Action).Recalculate();
			return res;
		}

		public static Entity GetEntity()
		{
			return EntityManager.GetEntity((int)Entities.ModuleIssue);
		}

		private int ModuleId
		{
			get { return int.Parse(this[Module.ParamNames.ModuleId].ToString()); }
		}

        private int RollerId
        {
            get { return int.Parse(this[Roller.ParamNames.RollerId].ToString()); }
        }

        public Module Module
		{ 
			get 
			{ 
				if(_module == null ) _module= new Module(ModuleId);
				return _module;
			}
		}

        public Roller Roller
        {
            get
            {
                if (_roller == null) _roller = new Roller(RollerId);
                return _roller;
            }
        }

        public override DateTime IssueDate
		{
			get { return ParseHelper.GetDateTimeFromObject(this[RollerIssue.ParamNames.IssueDate], DateTime.MinValue); }
		}

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
			if (actionName == ActionNames.SetFirst || actionName == ActionNames.SetSecond || actionName == ActionNames.SetLast)
			{
				if (_pricelist == null)
					_pricelist = new ModulePricelist(int.Parse(this[ModulePricelist.ParamNames.ModulePriceListID].ToString()));
				return !_pricelist.CheckTariffWithMaxCapacity() && base.IsActionEnabled(actionName, type);
			}
			else if (string.Compare(actionName, Constants.Actions.Substitute) == 0)
				return !Roller.IsCommon && !Roller.IsCloneOfCommon && base.IsActionEnabled(actionName, type);
			return base.IsActionEnabled(actionName, type);
        }

        public override bool Refresh()
        {
			_roller = null;
            return base.Refresh();
        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
			if (string.Compare(actionName, Constants.Actions.Substitute) == 0)
				SubstituteRollerForSingleIssue(Roller);
			else
				base.DoAction(actionName, owner, interfaceObject);
        }

        protected override DataSet PrepareSubstitutionParametersAndExecute(Dictionary<string, object> procParameters)
        {
            procParameters[Module.ParamNames.ModuleId] = ModuleId;
			// Создадим специальную таблицу, небходимую для хранимой процедуры
			DataTable days = CreateTableWithDays(IssueDate);

            return DataAccessor.LoadDataSet("RollerSubstitute", procParameters, days);
        }
    }
}