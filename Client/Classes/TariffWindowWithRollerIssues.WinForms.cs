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

        private void GroupWithWindow(bool isWithPrev)
		{
			try
			{
                TariffWindowWithRollerIssues window = GetTariffWindow2Group(isWithPrev);
				if (window != null)
				{
                    Cursor.Current = Cursors.WaitCursor;
                    if (isWithPrev)
						window[ParamNames.WindowNextId] = WindowId;
					else
						window[ParamNames.WindowPrevId] = WindowId;
					window.Update();

					if (isWithPrev)
						this[ParamNames.WindowPrevId] = window.WindowId;
					else
						this[ParamNames.WindowNextId] = window.WindowId;
					Update();
                    TariffWindowUngrouped?.Invoke(isWithPrev, false);
                }
			}
			finally { Cursor.Current = Cursors.Default; }
		}

		private void UngroupWindows(bool isWithPrev)
		{
			try
			{
				TariffWindowWithRollerIssues window = CreateTariffWindowById(isWithPrev ? int.Parse(this[ParamNames.WindowPrevId].ToString()) : int.Parse(this[ParamNames.WindowNextId].ToString()));
				window.Refresh();

				if (UserInteraction.Confirm(string.Format("Хотите отменить объединение рекламных окон '{0}' и '{1}'?", WindowDate.ToString("g"), window.WindowDate.ToString("g"))))
				{
                    Cursor.Current = Cursors.WaitCursor;
                    if (isWithPrev)
						window[ParamNames.WindowNextId] = null;
					else
						window[ParamNames.WindowPrevId] = null;
					window.Update();

					if (isWithPrev)
						this[ParamNames.WindowPrevId] = null;
					else
						this[ParamNames.WindowNextId] = null;
					Update();
					TariffWindowUngrouped?.Invoke(window.WindowDate < WindowDate, true);
				}
			}
			finally { Cursor.Current = Cursors.Default; }
        }
	}
}
