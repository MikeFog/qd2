using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть HeadCompanyWithActions: диспетчеризация. Само переключение вида узла
	// (какую сущность показывать детьми) — ShowActions/ShowFirms в ядре, у наследников
	// своя сущность фирм, у журнала удалённых — свои акции.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal abstract partial class HeadCompanyWithActions
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowActionsAction, StringComparison.OrdinalIgnoreCase))
                ShowActions();
            else if (string.Equals(actionName, ShowFirmsAction, StringComparison.OrdinalIgnoreCase))
                ShowFirms();
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
	}
}
