using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;

namespace Merlin.Classes
{
	public class SponsorTariff : PresentationObject
	{
		public SponsorTariff() : base(EntityManager.GetEntity((int)Entities.SponsorTariff))
		{
		}

        public SponsorTariff(DataRow row) : base(EntityManager.GetEntity((int)Entities.SponsorTariff), row)
        {
        }

		public int TariffId
		{
			get { return int.Parse(this[Tariff.ParamNames.TariffId].ToString()); }
		}

        internal decimal Price
        {
            get { return decimal.Parse(this[Tariff.ParamNames.Price].ToString()); }
        }

        public override bool ShowPassport(IWin32Window parentForm)
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
				SponsorTariffPassport passport = new SponsorTariffPassport(this, ds);
				//TODO: !passport.ApplyClicked
				bool res = (passport.ShowDialog(parentForm) == DialogResult.OK) /*|| passport.ApplyClicked*/;

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
