using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	// UI-часть HeadCompany: переприкрепление фирм к головной компании.
	// Бизнес-часть — в HeadCompany.cs (GetFirmsForReassign, ApplyFirmsReassign).
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class HeadCompany
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == "EditFirms")
				EditFirms(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void EditFirms(IWin32Window owner)
		{
			Cursor.Current = Cursors.WaitCursor;
			Entity entity = EntityManager.GetEntity((int)Entities.Firm);

			SelectionForm fSelector = new SelectionForm(entity, GetFirmsForReassign().DefaultView, "Фирмы-заказчики", true);
			if (fSelector.ShowDialog(owner) == DialogResult.OK)
			{
				ApplyFirmsReassign(fSelector.AddedItems);
			}
		}
	}
}
