using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть CampaignModule/CampaignModuleRollerInsideDay: диспетчеризация
	// действий и диалог смены предмета рекламы. Бизнес-часть смены предмета
	// рекламы (ApplyAdvertTypeChange) — в CampaignModule.cs.
	// Замена ролика (SubstituteRoller) в этом классе пока не разрезана — общий
	// с ActionRoller.cs/PackModuleIssue.cs кластер, см.
	// docs/tasks/web-migration-dialogs.md, §8.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class CampaignModule
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.ShowRollers)
				ShowModuleRollers();
			if (actionName == ActionNames.ShowDays)
				ShowModuleDays();
			else
				base.DoAction(actionName, owner, interfaceObject);
		}
	}

	internal partial class CampaignModuleRollerInsideDay
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, Roller.ActionNames.ChangeAdvertType, StringComparison.OrdinalIgnoreCase) == 0)
				ChangeAdvertType((Form)owner);
			else if (string.Compare(actionName, Constants.Actions.Substitute, StringComparison.OrdinalIgnoreCase) == 0)
				SubstituteRoller((Form)owner, 2);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void ChangeAdvertType(Form parentForm)
		{
			try
			{
				RollerChangeAdvertTypeForm form = new RollerChangeAdvertTypeForm(Roller, Campaign, ModulePricelist.ModuleID, null);
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
