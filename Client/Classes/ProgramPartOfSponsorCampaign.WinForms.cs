using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Controls;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть ProgramPartOfSponsorCampaign: диспетчеризация, диалог назначения
	// предмета рекламы, модальная сессия CampaignForm. Бизнес-часть
	// (ApplyAdvertTypeToIssues) — в ProgramPartOfSponsorCampaign.cs.
	// EditProgramIssues — форма 3.1, перенесён целиком без разреза (§8, п.3).
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ProgramPartOfSponsorCampaign
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.ShowDays)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.SponsorCampaignDay);
				base.FireContainerRefreshed();
			}
			else if (actionName == ActionNames.ShowPrograms)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.SponsorCampaignProgram);
				base.FireContainerRefreshed();
			}
			else if (actionName == Action.ActionNames.SetAdvertType)
				SetAdvertType();
			else if (actionName == ActionNames.EditIssues)
				EditProgramIssues(owner as Form);
			else if (actionName == Campaign.ActionNames.DeleteIssues)
			{
				if (Campaign.DeleteIssues(owner as Form, true, isFireEvent: false))
					FireContainerRefreshed();
			}
			else if (actionName == Constants.EntityActions.Refresh)
			{
				ClearCache();
				iterator.ClearCache();
				FireContainerRefreshed();
			}
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void SetAdvertType()
		{
			SponsorCampaignSetAdertTypeForm selector = new SponsorCampaignSetAdertTypeForm(Campaign);
			if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
				ApplyAdvertTypeToIssues(selector.SelectedIDs, selector.AdvertTypeId);
			}
		}

		public void EditProgramIssues(Form parentForm)
		{
			Campaign campaign = Campaign;
			CampaignForm fCampaign = new CampaignForm(campaign, new ProgramIssuesGrid2());
			fCampaign.ShowDialog(parentForm);
			Application.DoEvents();
			if (fCampaign.ChangeFlag)
			{
				campaign.RecalculateAction();
				FireContainerRefreshed();
			}
		}
	}
}
