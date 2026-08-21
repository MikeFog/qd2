using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть RollerIssue: DoAction. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	partial class RollerIssue
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, Constants.Actions.Substitute) == 0)
				SubstituteRollerForSingleIssue(Roller);
			else base.DoAction(actionName, owner, interfaceObject);
		}
	}
}
