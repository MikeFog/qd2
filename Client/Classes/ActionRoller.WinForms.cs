using System;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	// UI-часть ActionRoller: диспетчеризация, диалог замены ролика (кластер,
	// не разрезан, §8 п.4) и диалог смены предмета рекламы. Бизнес-часть смены
	// предмета рекламы (ApplyAdvertTypeChange) — в ActionRoller.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ActionRoller
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName.Equals(Action.ActionNames.SetAdvertType, System.StringComparison.OrdinalIgnoreCase))
				SetAdvertType((Form)owner, true);
			else if (string.Compare(actionName, Constants.Actions.Substitute, StringComparison.OrdinalIgnoreCase) == 0)
				SubstituteRoller((Form)owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void SubstituteRoller(Form owner)
		{
			try
			{
				Entity entity = EntityManager.GetEntity((int)Entities.Roller);
				ActionOnMassmedia action = new ActionOnMassmedia((int)this[Action.ParamNames.ActionId]);

				SelectionForm form = new SelectionForm(entity, action.Firm.GetRollers().DefaultView, "Замена ролика");
				if (form.ShowDialog(owner) == DialogResult.OK)
				{
					owner.UseWaitCursor = true;
					Application.DoEvents();

					var newRollerId = (int)form.SelectedObject.IDs[0];
					decimal price = action.TotalPrice;

					foreach (DataRow campaignrRow in action.Campaigns().Rows)
					{
						CampaignOnSingleMassmedia campaign = new CampaignOnSingleMassmedia(campaignrRow);
						if (campaign.CampaignType == Campaign.CampaignTypes.Simple ||
							campaign.CampaignType == Campaign.CampaignTypes.Sponsor)
							CampaignRoller.Subtitute(campaign, this, new Roller(newRollerId), campaign.Days(this), null, null);
						else if (campaign.CampaignType == Campaign.CampaignTypes.Module)
						{
							CampaignModule campaignModule = new CampaignModule(campaign.CampaignId)
							{
								ChildEntity = EntityManager.GetEntity((int)Entities.CampaignModule)
							};
							foreach (DataRow moduleRow in campaignModule.GetContent().Rows)
							{
								Module module = new Module(moduleRow);
								CampaignRoller.Subtitute(campaign, this, new Roller(newRollerId), campaign.Days(this), module.ModuleId, null);
							}

						}
						else if (campaign.CampaignType == Campaign.CampaignTypes.PackModule)
						{
							CampaignPackModule campaignPackModule = new CampaignPackModule(campaign.CampaignId)
							{
								ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleInCampaign)
							};
							foreach (DataRow packModuleRow in campaignPackModule.GetContent().Rows)
							{
								PackModule packModule = new PackModule(packModuleRow);
								CampaignRoller.Subtitute(campaign, this, new Roller(newRollerId), campaign.Days(this), null, packModule.PackModuleId);
							}
						}
						else
						{
							Debug.Assert(false, "Unknown campaign type");
						}
					}
					action.Recalculate();
					OnDataNeedRefresh();
					CampaignPart.ShowPriceChangeMessage(price, action.TotalPrice);
				}
			}
			finally { owner.UseWaitCursor = false; }
		}

		protected void SetAdvertType(Form owner, bool changeFlag)
		{
			try
			{
				Entity entity = EntityManager.GetEntity((int)Entities.AdvertTypeChild);
				SelectionForm form = new SelectionForm(entity, entity.GetContent().DefaultView, "Выбор предмета рекламы");
				if (form.ShowDialog(owner) == DialogResult.OK)
				{
					owner.UseWaitCursor = true;
					Application.DoEvents();

					ApplyAdvertTypeChange(form.SelectedObject.IDs[0], changeFlag);
				}
			}
			finally { owner.UseWaitCursor = false; }
		}
	}
}
