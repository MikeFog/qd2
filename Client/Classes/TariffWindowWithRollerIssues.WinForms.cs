using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть TariffWindowWithRollerIssues: диспетчеризация и диалог продления
	// окна. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class TariffWindowWithRollerIssues
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, ActionNames.Extend) == 0)
				Extend();
			else if (string.Compare(actionName, ActionNames.GroupWithNext) == 0)
				GroupWithWindow(false);
			else if (string.Compare(actionName, ActionNames.GroupWithPrev) == 0)
				GroupWithWindow(true);
			else if (string.Compare(actionName, ActionNames.UngroupNext) == 0)
				UngroupWindows(false);
			else if (string.Compare(actionName, ActionNames.UngroupPrev) == 0)
				UngroupWindows(true);
			else base.DoAction(actionName, owner, interfaceObject);
		}

		private void Extend()
		{
			int mmId = int.Parse(parameters[Massmedia.ParamNames.MassmediaId].ToString());
			FrmWindowTariffTemplate frm = new FrmWindowTariffTemplate(WindowDate, Duration, DurationTotal, Massmedia.GetMassmediaByID(mmId));
			if (frm.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
				if (TariffExtend != null)
					TariffExtend(this);
			}
		}
	}
}
