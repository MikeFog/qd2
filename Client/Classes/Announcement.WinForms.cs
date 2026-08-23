using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть Announcement: DoAction. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Announcement
	{
		public override void DoAction(string actionName, IWin32Window owner,
		                              InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.MarkAsRead)
				SetReadMark(true);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}
	}
}
