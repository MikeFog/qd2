using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal class SponsorCampaignPart : CampaignPart 
	{
		public SponsorCampaignPart(Entity entity) : base(entity)
		{
		}

		public SponsorCampaignPart(Entity entity, DataRow row) : base(entity, row)
		{
		}
	}

	internal class SponsorCampaignDay : SponsorCampaignPart
	{
		public SponsorCampaignDay() 
			: base(EntityManager.GetEntity((int)Entities.SponsorCampaignDay))
		{
		}

		public SponsorCampaignDay(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.SponsorCampaignDay), row)
		{
		}
	}

	internal class SponsorCampaignDayInProgramm : SponsorCampaignPart
	{
		public SponsorCampaignDayInProgramm()
			: base(EntityManager.GetEntity((int)Entities.SponsorCampaignDayInProgramm))
		{
		}

		public SponsorCampaignDayInProgramm(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.SponsorCampaignDayInProgramm), row)
		{
		}

        public override DataTable GetContent()
        {
			Dictionary<string, object> filterValues = DataAccessor.CreateParametersDictionary();
			filterValues["showDeleted"] = Campaign.IsMarkedAsDeleted;

            return GetContent(filterValues, true);
        }
    }

	internal class SponsorCampaignProgram : SponsorCampaignPart
	{
        public struct ParamNames
        {
            public const string ProgramId = "programId";
        }

        public SponsorCampaignProgram()
			: base(EntityManager.GetEntity((int)Entities.SponsorCampaignProgram))
		{
		}

		public SponsorCampaignProgram(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.SponsorCampaignProgram), row)
		{
		}

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName == Campaign.ActionNames.DeleteIssues)
                DeleteIssues((Form)owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private void DeleteIssues(Form owner)
        {
            Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters[OBJECT_ID] = this[ParamNames.ProgramId];
            if(Campaign.DeleteIssues(owner, true, parameters, isFireEvent: false))
				FireContainerRefreshed();	
        }
    }

	internal class SponsorCampaignProgramInDay : SponsorCampaignPart
	{
		public SponsorCampaignProgramInDay()
			: base(EntityManager.GetEntity((int)Entities.SponsorCampaignProgramInDay))
		{
		}

		public SponsorCampaignProgramInDay(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.SponsorCampaignProgramInDay), row)
		{
		}
	}
}
