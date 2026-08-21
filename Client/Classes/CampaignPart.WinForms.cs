using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть CampaignPart: изменение позиционирования и замена ролика в
	// одиночном выпуске. Бизнес-часть — в CampaignPart.cs (ApplyPositionChanges,
	// GetRollersForSubstitution, ApplyRollerSubstitution), она не знает про UI.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class CampaignPart
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			decimal currentPrice = 0;
			if (actionName == Constants.EntityActions.Delete)
			{
				Campaign.Action.Refresh();
				currentPrice = Campaign.Action.TotalPrice;
			}

			base.DoAction(actionName, owner, interfaceObject);
			if (actionName == Constants.EntityActions.Delete)
			{
				//if (isNew) // Is Deleted
				RecalculateAndShowPriceChange(currentPrice);
			}
		}

		protected void ChangePositions(Form owner)
		{
			try
			{
				ChangePositioningForm selector = new ChangePositioningForm(this);
				if (selector.ShowDialog(owner) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor.Current = Cursors.WaitCursor;

					DataTable tableErrors = ApplyPositionChanges(selector.SelectedIDs, selector.NewPosition);
					if (tableErrors.Rows.Count > 0)
					{
						Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки изменения позиционирования", tableErrors);
					}
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		protected void SubstituteRollerForSingleIssue(Roller currentRoller)
		{
			try
			{
				DataTable dt = GetRollersForSubstitution(currentRoller);
				if (dt == null)
				{
					Globals.ShowInfo("CannotFindRollersForSubstitude");
					return;
				}

				Entity eRoller = EntityManager.GetEntity((int)Entities.Roller);
				SelectionForm frm = new SelectionForm(eRoller, dt.DefaultView, Properties.Resources.TitleGetRollerForSubstitude);
				if (frm.ShowDialog(Globals.MdiParent) == DialogResult.OK)
				{
					Cursor.Current = Cursors.WaitCursor;

					Roller r = frm.SelectedObject as Roller;
					if (r != null)
					{
						string warning = ApplyRollerSubstitution(currentRoller, r);
						if (warning != null)
							UserMessage.ShowExclamation(warning);
					}
				}
			}
			finally { Cursor.Current = Cursors.Default; }
		}
	}
}
