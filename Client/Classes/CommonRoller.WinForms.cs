using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть CommonRoller: DoAction. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	partial class CommonRoller
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName.Equals(Action.ActionNames.SetAdvertType, System.StringComparison.OrdinalIgnoreCase))
                SetAdvertType((Form)owner, false);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
	}
}
