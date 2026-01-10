using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Controls;
using Merlin.Forms;

namespace Merlin.Classes
{
	internal class RollerPartOfSponsorCampaign : CampaignPart
	{
		public RollerPartOfSponsorCampaign()
			: base(GetEntity())
		{
		}

		public RollerPartOfSponsorCampaign(DataRow row)
			: base(GetEntity(), row)
		{
		}

		private static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.RollerPart);
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.EditIssues)
				EditRollerIssues(owner as Form);
			else if (actionName == ActionNames.ShowDays)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.CampaignDay);
				base.FireContainerRefreshed();
			}
			else if (actionName == ActionNames.ShowRollers)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.CampaignRoller);
				base.FireContainerRefreshed();
			}
			else if (actionName == Campaign.ActionNames.DeleteIssues)
			{
				if (Campaign.DeleteIssues(owner as Form, isFireEvent: false))
					FireContainerRefreshed();
			}
            else if (actionName == Constants.Actions.ChangePositions)
                ChangePositions((Form)owner);
            else if (actionName == Constants.EntityActions.Refresh)
            {
                ClearCache();
                iterator.ClearCache();
                FireContainerRefreshed();
            }
            else
				base.DoAction(actionName, owner, interfaceObject);
		}

		public void EditRollerIssues(Form parentForm)
		{
			Campaign campaign = Campaign;
			CampaignForm fCampaign = new CampaignForm(campaign, new RollerIssuesGrid3());
			fCampaign.ShowDialog(parentForm);
			Application.DoEvents();
			if (fCampaign.ChangeFlag)
			{
                FireContainerRefreshed();
            }
		}

		#region Nested type: ActionNames

		public struct ActionNames
		{
			public const string EditIssues = "EditIssues";
			public const string ShowRollers = "ShowRollers";
			public const string ShowDays = "ShowDays";
		}

		#endregion
	}
}