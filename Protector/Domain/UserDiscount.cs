using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Passport.Forms;

namespace Protector.Domain
{
	public class UserDiscount : PresentationObject
	{
		public UserDiscount(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.UserDiscount), row)
		{
		}

		public UserDiscount()
			: base(EntityManager.GetEntity((int)Entities.UserDiscount))
		{
		}

		/// <summary>
		/// Override Show Passport: Set User from FilterValues parent form
		/// </summary>
		/// <param name="parentForm"></param>
		/// <returns></returns>
		public override bool ShowPassport(IWin32Window parentForm)
		{
			try
			{
				if (!entity.HasPassport) return false;

				// load data to display Passport
				Dictionary<string, object> procParameters = Parameters;
				DataAccessor.PrepareParameters(
					procParameters, entity, InterfaceObjects.PropertyPage, Constants.Actions.Load);

				DataSet ds = null;
				if (DataAccessor.IsProcedureExist(procParameters))
				{
					ds = DataAccessor.DoAction(procParameters) as DataSet;
				}

				bool isNewObject = IsNew;

				JournalForm form = (parentForm as JournalForm) ?? ((UserControl)parentForm).ParentForm as JournalForm;
				Debug.Assert(form != null);
				this["userID"] = form.Filters["userID"];

				PassportForm passport = GetPassportForm(ds);
				bool res = (passport.ShowDialog(parentForm) == DialogResult.OK || passport.IsApplyClicked);

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
