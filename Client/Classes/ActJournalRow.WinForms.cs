using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть ActJournalRow: DoAction (открывает PrintOnAirInquire(Form)) и
	// GetCampaign (только для DoAction). Дословный перенос.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ActJournalRow
	{
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
