using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Controls;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть RollerPartOfSponsorCampaign: диспетчеризация и модальная сессия
	// CampaignForm. Дословный перенос, логика не менялась — мирный близнец
	// ProgramPartOfSponsorCampaign.WinForms.cs (форма 3.1, §8 п.3 конвенции).
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class RollerPartOfSponsorCampaign
	{
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
	}
}
