using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть FirmBalance: DoAction и абстрактное объявление
	// Jump2FirmBalanceJournal(IWin32Window) — оба типизированы на UI, реализации
	// в наследниках (FirmBalanceIssues.WinForms.cs, FirmBalanceStudioOrder.WinForms.cs).
	// Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public abstract partial class FirmBalance
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == "Jump2FirmBalance")
				Jump2FirmBalanceJournal(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		protected abstract void Jump2FirmBalanceJournal(IWin32Window owner);
	}
}
