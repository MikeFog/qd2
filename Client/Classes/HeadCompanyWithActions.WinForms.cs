using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть HeadCompanyWithActions и трёх наследников: DoAction у каждого.
	// Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class HeadCompanyWithActions
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowActionsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.Action);
                FireContainerRefreshed();
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
	}

	internal partial class HeadCompanyWithConfirmedActions
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowFirmsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithConfirmedActions);
                FireContainerRefreshed();
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
	}

	internal partial class HeadCompanyWithUnconfirmedActions
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowFirmsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithUnconfirmedActions);
                FireContainerRefreshed();
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
	}

	internal partial class HeadCompanyWithDeletedActions
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowFirmsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithDeletedActions);
                FireContainerRefreshed();
            }
            else if (string.Equals(actionName, ShowActionsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.ActionDeleted);
                FireContainerRefreshed();
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
	}
}
