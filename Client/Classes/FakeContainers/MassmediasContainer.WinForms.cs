using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;

namespace Merlin.Classes.FakeContainers
{
	// UI-часть MassmediasContainer. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	partial class MassmediasContainer
	{
		public override void ShowFilter(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				FilterForm frm = new FilterForm(RootEntity, Globals.PrepareForFilter(RootEntity), _filter);

				if (frm.ShowDialog(owner) == DialogResult.OK)
					FireContainerRefreshed();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}
	}
}
