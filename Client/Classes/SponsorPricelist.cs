using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal class SponsorPricelist : MassmediaPricelist
	{
		public SponsorPricelist()
			: base(EntityManager.GetEntity((int) Entities.SponsorPricelist))
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.SponsorTariff);
		}

		public SponsorPricelist(DataRow row)
			: base(EntityManager.GetEntity((int) Entities.SponsorPricelist), row)
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.SponsorTariff);
		}

		//public override bool ShowPassport(System.Windows.Forms.IWin32Window owner) {
		//  // load data to display Passport
		//  DataAccessor.PrepareParameters(
		//    this.parameters, this.entity, InterfaceObjects.PropertyPage, Constants.Actions.Load); 

		//  DataSet ds = null;
		//  if(DataAccessor.IsProcedureExist(parameters)) 
		//    ds = DataAccessor.DoAction(parameters) as DataSet;

		//  bool isNewObject = this.IsNew;
		//  FrmPassport passport = new FrmPricelist(this, ds);
		//  bool res = passport.ShowDialog(owner) == DialogResult.OK; 
		//  // Fire event only if existing object was changed
		//  if(res && !isNewObject) this.OnObjectChanged(this);
		//  return res;
		//}

		public int Bonus
		{
			get { return int.Parse(parameters["bonus"].ToString()); }
		}

		public override DataTable GetTariffList()
		{
			return LoadTariffList();
		}

		public SponsorTariff GetTariffBydate(DateTime date)
		{
            DataTable dt = LoadTariffList(date);
			if (dt.Rows.Count == 0) return null;
			return new SponsorTariff(dt.Rows[0]);
		}

		private DataTable LoadTariffList(DateTime? date = null)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.SponsorTariff));
			procParameters[Pricelist.ParamNames.PricelistId] = PricelistId;
			if (date.HasValue)
			{
				procParameters[Tariff.ParamNames.Time] = new DateTime(1900, 1, 1, date.Value.Hour, date.Value.Minute, 0);
				switch (date.Value.DayOfWeek)
				{
                    case DayOfWeek.Monday:
                        procParameters[Tariff.ParamNames.Monday] = 1;
                        break;
                    case DayOfWeek.Tuesday:
                        procParameters[Tariff.ParamNames.Tuesday] = 1;
                        break;
                    case DayOfWeek.Wednesday:
                        procParameters[Tariff.ParamNames.Wednesday] = 1;
                        break;
                    case DayOfWeek.Thursday:
                        procParameters[Tariff.ParamNames.Thursday] = 1;
                        break;
                    case DayOfWeek.Friday:
                        procParameters[Tariff.ParamNames.Friday] = 1;
                        break;
                    case DayOfWeek.Saturday:
                        procParameters[Tariff.ParamNames.Saturday] = 1;
                        break;
                    case DayOfWeek.Sunday:
						procParameters[Tariff.ParamNames.Sunday] = 1;
						break;
                }
			}

            return ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
        }
    }
}