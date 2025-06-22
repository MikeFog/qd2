using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal class ActJournalRow : PresentationObject
	{
		public ActJournalRow() : base(EntityManager.GetEntity((int) Entities.ActJournalRow))
		{
		}

		public ActJournalRow(DataRow row) : base(EntityManager.GetEntity((int) Entities.ActJournalRow), row)
		{
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Campaign.ActionNames.PrintOnAirInquire)
				GetCampaign().PrintOnAirInquire((Form) owner);
			else if (actionName == Campaign.ActionNames.PrintMediaPlan)
				GetCampaign().PrintMediaPlan(false, false, false, false);
		}

		private Campaign GetCampaign()
		{
			int campaignId = int.Parse(parameters[Campaign.ParamNames.CampaignId].ToString());
			return Campaign.GetCampaignById(campaignId);
		}
	}
}