using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Controls;
using Merlin.Forms;
using static Merlin.Forms.UniversalPassportForm;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Merlin.Classes
{
	internal class ProgramPartOfSponsorCampaign : CampaignPart
	{
		public struct ActionNames
		{
			public const string ShowPrograms = "ShowPrograms";
			public const string ShowDays = "ShowDays";
			public const string ShowRollers = "ShowRollers";
			public const string EditIssues = "EditIssues";
		}

		public ProgramPartOfSponsorCampaign() : base(GetEntity())
		{
		}

		public ProgramPartOfSponsorCampaign(DataRow row) : base(GetEntity(), row)
		{
		}

        public ProgramPartOfSponsorCampaign(int campaignId) : this()
        {
			this[Campaign.ParamNames.CampaignId] = campaignId;
        }

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
				if (Campaign.DeleteIssues(owner as Form, true, isFireEvent: false))
					FireContainerRefreshed();
                else
                    base.DoAction(actionName, owner, interfaceObject);
        }

        private void SetAdvertType()
        {
            SponsorCampaignSetAdertTypeForm selector = new SponsorCampaignSetAdertTypeForm(Campaign);
            if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
                foreach (var id in selector.SelectedIDs)
				{
					ProgramIssue issue = new ProgramIssue(id);
					issue.AdvertTypeId = selector.AdvertTypeId;
					issue.Update();
				}
				FireContainerRefreshed();
            }
        }

        public void EditProgramIssues(Form parentForm)
		{
			Campaign campaign = Campaign;
			CampaignForm fCampaign = new CampaignForm(campaign, new ProgramIssuesGrid2());
			fCampaign.ShowDialog(parentForm);
			Application.DoEvents();
			if (fCampaign.ChangeFlag)
				campaign.RecalculateAction();
		}

		private static Entity GetEntity()
		{
			return EntityManager.GetEntity((int)Entities.ProgramPart);
		}

		public DataTable GetProgramIssues()
		{
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.ProgramIssue));
            procParameters[Campaign.ParamNames.CampaignId] = this[Campaign.ParamNames.CampaignId];

            return ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
        }
	}
}