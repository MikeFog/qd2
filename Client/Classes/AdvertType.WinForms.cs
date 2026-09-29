using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть AdvertType: AssignNew (открывает паспорт); черновик — CreateChildDraft в ядре.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class AdvertType
	{
        protected override void AssignNew(IWin32Window owner)
        {
			PresentationObject newObject = CreateChildDraft();

			if (newObject.ShowPassport(owner))
			{
				newObject.Refresh();
				OnObjectCreated(newObject);
			}
		}
	}
}
