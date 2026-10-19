using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть TariffWindowWithRollerIssues: диспетчеризация и диалог продления
	// окна. Дословный перенос; с 19.10.2026 продление начинается со следующего дня.
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
			// Продление — со следующего дня: своя минута окна занята им самим, и генерация по шаблону писала бы
			// «Окно … уже существует» на день окна (проверка занятой минуты, Р-7, Deploy/35). Дни без прайс-листа
			// генерация молча пропускает — окно последнего дня прайс-листа продлевать некуда.
			DateTime start = WindowDate.AddDays(1);
			Massmedia massmedia = Massmedia.GetMassmediaByID(mmId);
			if (massmedia.GetPriceList(start.Date) == null)
			{
				UserMessage.ShowExclamation(string.Format("Продлить окно нельзя: на {0:dd.MM.yyyy} у радиостанции нет прайс-листа.", start));
				return;
			}
			FrmWindowTariffTemplate frm = new FrmWindowTariffTemplate(start, Duration, DurationTotal, massmedia);
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
