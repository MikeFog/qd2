using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть Agency: показ паспорта, диалог выбора агентства. Дословный
	// перенос из Agency.cs, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Agency
	{
		public static List<PresentationObject> SelectAgencies(PresentationObject presentationObject,
			Dictionary<string, object> parameters, IWin32Window owner)
		{
			List<PresentationObject> result = GetAgenciesForSelection(presentationObject, parameters, out DataTable candidatesForDialog);
			if (candidatesForDialog == null)
				return result;

			// If more than one row - display selector with checkboxes
			SelectionForm selector = new SelectionForm(
				EntityManager.GetEntity((int)Entities.Agency), candidatesForDialog.DefaultView, "Выбор агентств", true);

			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				return selector.AddedItems;
			}
			return null;
		}

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
