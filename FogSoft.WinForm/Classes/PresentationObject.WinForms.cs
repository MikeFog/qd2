using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Forms;

namespace FogSoft.WinForm.Classes
{
	// UI-часть PresentationObject: показ паспорта и обработка действий меню.
	// Основная часть класса — в PresentationObject.cs, она компилируется также
	// в сборку без UI (FogSoft.Core). Логика не менялась, код перенесён как есть.
	// См. docs/tasks/web-migration.md, этап 0.
	public partial class PresentationObject : IActionHandler
	{
		public virtual bool ShowPassport(IWin32Window parentForm)
		{
			try
			{
				if(!entity.HasPassport) return false;

				// load data to display Passport
				Dictionary<string, object> procParameters = Parameters;
				DataAccessor.PrepareParameters(
					procParameters, entity, InterfaceObjects.PropertyPage, Constants.Actions.Load);

				DataSet ds = null;
				if(DataAccessor.IsProcedureExist(procParameters))
				{
					ds = DataAccessor.DoAction(procParameters) as DataSet;
				}

				bool isNewObject = IsNew;

				PassportForm passport = GetPassportForm(ds);
				bool res = (passport.ShowDialog(parentForm) == DialogResult.OK || passport.IsApplyClicked);

				// Fire event only if existing object was changed
				if(res && !isNewObject) OnObjectChanged(this);
				return res;
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
				return false;
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		public virtual PassportForm GetPassportForm(DataSet ds)
		{
			return new PassportForm(this, ds);
		}

		public virtual void DoAction(
			string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.EntityActions.Delete:
					Delete();
					break;
				case Constants.EntityActions.ShowPassport:
					ShowPassport(owner);
					break;
				case Constants.EntityActions.Refresh:
					Refresh(interfaceObject);
					break;
				case Constants.EntityActions.Detach:
					Detach();
					break;
			}
		}
	}
}
