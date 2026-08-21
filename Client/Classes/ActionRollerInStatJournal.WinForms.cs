using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	// UI-часть ActionRollerInStatJournal: диспетчеризация и диалог выбора
	// предмета рекламы. Бизнес-часть (CanSetAdvertType, ApplyAdvertTypeChange) —
	// в ActionRollerInStatJournal.cs. Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ActionRollerInStatJournal
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName.Equals(Action.ActionNames.SetAdvertType, System.StringComparison.OrdinalIgnoreCase))
				SetAdvertType(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void SetAdvertType(IWin32Window owner)
		{
			try
			{
				if (!CanSetAdvertType(out string errorMessage))
				{
					UserMessage.ShowExclamation(errorMessage);
					return;
				}

				Entity entity = EntityManager.GetEntity((int)Entities.AdvertTypeChild);
				SelectionForm form = new SelectionForm(entity, entity.GetContent().DefaultView, "Выбор предмета рекламы");
				if (form.ShowDialog(owner) == DialogResult.OK)
				{
					Cursor.Current = Cursors.WaitCursor;
					ApplyAdvertTypeChange(form.SelectedObject.IDs[0], form.SelectedObject.Name);
				}
			}
			finally { Cursor.Current = Cursors.Default; }
		}
	}
}
