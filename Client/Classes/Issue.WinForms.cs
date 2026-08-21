using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть Issue: DoAction и UpdatePosition (Application.DoEvents в catch).
	// Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class Issue
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, ActionNames.SetFirst) == 0)
				UpdatePosition(RollerPositions.First);
			else if (string.Compare(actionName, ActionNames.SetSecond) == 0)
				UpdatePosition(RollerPositions.Second);
			else if (string.Compare(actionName, ActionNames.SetLast) == 0)
				UpdatePosition(RollerPositions.Last);
			else if (string.Compare(actionName, ActionNames.SetUnknow) == 0)
				UpdatePosition(RollerPositions.Undefined);
			else base.DoAction(actionName, owner, interfaceObject);
		}

		private void UpdatePosition(RollerPositions pos)
		{
			decimal price = decimal.Zero;
            if(Campaign != null && Campaign.Action != null)
			{
				Campaign.Action.Refresh();
                price = Campaign.Action.TotalPrice;
            }
			
			try
			{
				SetPosition(pos);
				Refresh();
				OnParentChanged(this, 1);
				RecalculateAndShowPriceChange(price);
			}
			catch (Exception exp)
			{
				MessageAccessor.Parameters = parameters;
				ErrorManager.PublishError(exp);
				Application.DoEvents();
				Refresh();
			}
		}
	}
}
