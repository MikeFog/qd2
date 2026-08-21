using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	// SelectManager: чистый выбор без применения (форма 3), дословный перенос.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal static partial class Utils
	{
		public static PresentationObject SelectManager(IWin32Window owner)
		{
			SelectionForm fSelector =
				new SelectionForm(EntityManager.GetEntity((int)Entities.User), "Менеджер");
			if (fSelector.ShowDialog(owner) == DialogResult.OK) return fSelector.SelectedObject;
			return null;
		}
	}
}
