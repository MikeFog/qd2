using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть Agency: показ паспорта. Дословный перенос из Agency.cs, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Agency
	{
		public override bool ShowPassport(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				// load data to display Passport
				DataAccessor.PrepareParameters(parameters, entity, InterfaceObjects.PropertyPage,
											   Constants.Actions.Load);

				DataSet ds = null;
				if (DataAccessor.IsProcedureExist(parameters))
				{
					ds = DataAccessor.DoAction(parameters) as DataSet;
				}

				bool isNewObject = IsNew;
				AgencyPassportForm passport = new AgencyPassportForm(this, ds);
				//TODO: !passport.ApplyClicked
				bool res = (passport.ShowDialog(owner) == DialogResult.OK) /*|| passport.ApplyClicked*/;

				// Fire event only if existing object was changed
				if (res && !isNewObject) OnObjectChanged(this);
				return res;
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
				return false;
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}
	}
}
