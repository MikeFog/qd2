using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.FakeContainers
{
	// UI-часть AdvertTypeContainer. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class AdvertTypeContainer
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName == ActionNames.ShowTree)
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.AdvertType);
                FireContainerRefreshed();
            }
            else if (actionName == ActionNames.ShowFlat)
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.AdvertTypeChild);
                FireContainerRefreshed();
            }
            base.DoAction(actionName, owner, interfaceObject);
        }
	}
}
