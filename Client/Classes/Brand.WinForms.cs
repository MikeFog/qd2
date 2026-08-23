using System.Windows.Forms;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть Brand: AssignExisting/AssignNew (открывают паспорт фирмы) и
	// приватный AssignFirm(firm, owner) — обёртка с Cursor вокруг публичного
	// AssignFirm(firm), который остаётся в ядре. Дословный перенос.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Brand
	{
		protected override void AssignExisting(IWin32Window owner)
		{
			PresentationObject firm = Firm.SelectFirm(owner);

			if (firm != null)
				AssignFirm(firm, owner);
		}

		protected override void AssignNew(IWin32Window owner)
		{
			PresentationObject firm = EntityManager.GetEntity((int) Entities.Firm).NewObject;

			if (firm.ShowPassport(owner))
			{
				Application.DoEvents();
				AssignFirm(firm, owner);
			}
		}

		private void AssignFirm(PresentationObject firm, IWin32Window owner)
		{
			Form ownerForm = (Form) owner;
			try
			{
				Application.DoEvents();
				ownerForm.Cursor = Cursors.WaitCursor;

                AssignFirm(firm);
			}
			finally
			{
				ownerForm.Cursor = Cursors.Default;
			}
		}
	}
}
