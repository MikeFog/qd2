using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.FakeContainers
{
	// UI-часть AdvertTypeContainer. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class AdvertTypeContainer
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName == ActionNames.ShowTree)
                ShowTree();
            else if (actionName == ActionNames.ShowFlat)
                ShowFlat();
            base.DoAction(actionName, owner, interfaceObject);
        }
	}
}
