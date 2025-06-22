using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	internal static class Utils
	{
		public static PresentationObject SelectManager(IWin32Window owner)
		{
			SelectionForm fSelector =
				new SelectionForm(EntityManager.GetEntity((int) Entities.User), "Менеджер");
			if (fSelector.ShowDialog(owner) == DialogResult.OK) return fSelector.SelectedObject;
			return null;
		}

		public static PresentationObject CreateBankById(int bankID)
		{
			Entity entity = EntityManager.GetEntity((int) Entities.Bank);
			Dictionary<string, object> parameters =
				new Dictionary<string, object>(1, StringComparer.CurrentCultureIgnoreCase);
			parameters.Add("bankID", bankID.ToString());
			PresentationObject bank = entity.CreateObject(parameters);
			bank.Refresh();
			return bank;
		}

		public static SecurityManager.User AskConfirmation(IWin32Window owner)
		{
			return AskConfirmation(owner, null, null);
		}

		public static SecurityManager.User AskConfirmation(IWin32Window owner, string title, string text)
		{
			FrmConfirmation fConfirmation = (title != null || text != null) ? new FrmConfirmation(title, text) : new FrmConfirmation();
			if (fConfirmation.ShowDialog(owner) == DialogResult.OK)
			{
				if (fConfirmation.User == null)
					Globals.ShowInfo("LoginIncorrect");
				else if (fConfirmation.User.IsAdmin || fConfirmation.User.IsGrantor)
					return fConfirmation.User;
				else
				{
					Dictionary<string, object> parameters =
						new Dictionary<string, object>(2, StringComparer.CurrentCultureIgnoreCase)
							{
								{"FirstName", fConfirmation.User.FirstName},
								{"LastName", fConfirmation.User.LastName}
							};
					Globals.ShowInfo("ConfirmationError", parameters);
				}
			}
			return null;
		}
	}
}