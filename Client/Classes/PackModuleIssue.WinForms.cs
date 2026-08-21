using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Controls;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть PackModuleIssueInCampaignForm/PackModuleIssue: диспетчеризация
	// действий и диалог смены предмета рекламы. Бизнес-часть смены предмета
	// рекламы (ApplyAdvertTypeChange) — в PackModuleIssue.cs.
	// SubstituteRoller (замена ролика) — тонкая обёртка над диалогом
	// CampaignRoller.Substitute, он в UI-половине CampaignRoller.WinForms.cs;
	// запись в БД разведена с показом журнала
	// (CampaignRoller.ApplyRollerSubstitutionForDays).
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class PackModuleIssueInCampaignForm
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.Actions.Substitute)
				SubstituteRollerForSingleIssue(Roller);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}
	}

	internal partial class PackModuleIssue
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.Actions.Substitute)
			{
				if (entity.AttributeSelector == Issue.AttributeSelectorShort)
					SubstituteRollerForSingleIssue(Roller);
				else
					SubstituteRoller((Form)owner);
			}
			else if (actionName == Constants.Actions.PlayRoller)
				MediaControl.Current.Play(this);
			else if (string.Compare(actionName, Roller.ActionNames.ChangeAdvertType, StringComparison.OrdinalIgnoreCase) == 0)
				ChangeAdvertType((Form)owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void SubstituteRoller(Form owner)
		{
			decimal price = decimal.Zero;

			if (Campaign != null && Campaign.Action != null)
			{
				Campaign.Action.Refresh();
				price = Campaign.Action.TotalPrice;
			}

			CampaignRoller.Substitute((Form)owner, Campaign, PackModuleID, null,
					   new Roller(int.Parse(this[Roller.ParamNames.RollerId].ToString())),
					   delegate
					   {
						   RecalculateAndShowPriceChange(price);
						   OnParentChanged(this, 1);
					   });
		}

		private void ChangeAdvertType(Form parentForm)
		{
			try
			{
				RollerChangeAdvertTypeForm form = new RollerChangeAdvertTypeForm(Roller, Campaign, null, PackModulePricelist.PackModuleId);
				if (form.ShowDialog(parentForm) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor.Current = Cursors.WaitCursor;
					ApplyAdvertTypeChange(form.SelectedDays, form.AdvertTypeId);
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				parentForm.Cursor = Cursors.Default;
			}
		}
	}
}
