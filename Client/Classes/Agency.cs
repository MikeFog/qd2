using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	public class MassmediaAgency : PresentationObject
	{
		public MassmediaAgency() : base(EntityManager.GetEntity((int)Entities.MassmediaAgency))
        {

		}

        public MassmediaAgency(int agencyId, int massmediaId)
			: base(EntityManager.GetEntity((int)Entities.MassmediaAgency))
		{
			parameters[Massmedia.ParamNames.MassmediaId] = massmediaId.ToString();
			parameters[Agency.ParamNames.AgencyId] = agencyId.ToString();
		}

        public int AgencyId
        {
            get { return int.Parse(this[Agency.ParamNames.AgencyId].ToString()); }
        }
    }

	public class StudioAgency : PresentationObject
	{
		public StudioAgency(int agencyId, int studioId)
			: base(EntityManager.GetEntity((int)Entities.StudioAgency))
		{
			parameters[ProductionStudio.ParamNames.StudioId] = studioId.ToString();
			parameters[Agency.ParamNames.AgencyId] = agencyId.ToString();
		}
	}

	public class Agency : Organization
    {
		public enum AttributeSelectors
		{
			NameOnly = 1
		}

		public new struct ParamNames
		{
			public const string AgencyId = "agencyID";
            public const string ReportPlace = "reportPlace";
        }

		public Agency() : base(EntityManager.GetEntity((int)Entities.Agency))
		{
		}

		private Agency(int agencyId) : this()
		{
			this[ParamNames.AgencyId] = agencyId;
			isNew = false;
		}

		public Agency(DataRow row)	: base(EntityManager.GetEntity((int)Entities.Agency), row)
		{
		}

		public int AgencyId
		{
			get { return int.Parse(this[ParamNames.AgencyId].ToString()); }
		}

		public string ReportPlace
		{
            get { return this[ParamNames.ReportPlace].ToString(); }
        }

		public override bool Update()
		{
			if (!base.Update())
				return false;

			foreach(ChildrenChanges childrenChanges in childrenChangesList)
			{
				foreach(PresentationObject po in childrenChanges.AddedObjects)
				{
					Massmedia massmedia = po as Massmedia;
					if(massmedia != null)
					{
						new MassmediaAgency(AgencyId, massmedia.MassmediaId).Update();
						continue;
					}
					ProductionStudio studio = po as ProductionStudio;
					if(studio != null)
					{
						new StudioAgency(AgencyId, studio.StudioID).Update();
						continue;
					}
				}

				foreach(PresentationObject po in childrenChanges.DeletedObjects)
				{
					Massmedia massmedia = po as Massmedia;
					if(massmedia != null)
					{
						new MassmediaAgency(AgencyId, massmedia.MassmediaId).Delete(true);
						continue;
					}
					ProductionStudio studio = po as ProductionStudio;
					if(studio != null)
					{
						new StudioAgency(AgencyId, studio.StudioID).Delete(true);
						continue;
					}
				}
			}
			childrenChangesList.Clear();

			return true;
		}

		internal static Agency GetAgencyByID(int agencyId)
		{
			Agency agency = new Agency(agencyId);
			agency.Refresh();
			return agency;
		}

		public static DataSet LoadAgencies(bool loadActiveOnly)
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.Agency));
			if(loadActiveOnly)
				procParameters.Add("ShowActive", 1);

			return (DataSet)DataAccessor.DoAction(procParameters);
		}

        public DataTable LoadPainting()
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.AgencyId] = AgencyId;

            return DataAccessor.LoadDataSet("AgencyPainting", procParameters).Tables[0];
        }

        public static List<PresentationObject> SelectAgencies(PresentationObject presentationObject, 
			Dictionary<string, object> parameters, IWin32Window owner)
		{
			// Load all agencies associated with given presentation objects
			DataAccessor.PrepareParameters(parameters, presentationObject.Entity,
																		 InterfaceObjects.SimpleJournal, Constants.Actions.LoadAgencies);

			DataSet ds = (DataSet)DataAccessor.DoAction(parameters);
			DataTable dtAgency = ds.Tables[Constants.TableNames.Data];

			// How many rows were returned?
			int count = dtAgency.Rows.Count;
			if(count == 0)
				return null;

			if(count > 1)
			{
				// If more than one row - display selector with checkboxes
				SelectionForm selector = new SelectionForm(
					EntityManager.GetEntity((int)Entities.Agency), dtAgency.DefaultView, "Выбор агентств", true);

				if(selector.ShowDialog(owner) == DialogResult.OK)
				{
					return selector.AddedItems;
				}
				return null;
			}
			else
			{
				List<PresentationObject> items = new List<PresentationObject>(1);
				int agencyId = int.Parse(dtAgency.Rows[0][ParamNames.AgencyId].ToString());
				items.Add(GetAgencyByID(agencyId));
				return items;
			}
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

        public decimal GetTaxValue(DateTime date)
        {
			ChildEntity = EntityManager.GetEntity((int)Entities.AgencyTax);
			var _taxTable = GetContent();


            if (_taxTable == null || _taxTable.Rows.Count == 0)
                return 0;

            DataRow row = _taxTable.AsEnumerable()
                .FirstOrDefault(r =>
                    date >= r.Field<DateTime>("startDate") &&
                    date <= r.Field<DateTime>("finishDate"));

            if (row == null)
                return 0;

            decimal divisor = row.Field<decimal>("divisor");

            if (divisor <= 1m)
                throw new InvalidOperationException($"Invalid divisor value: {divisor}");

            // ставка НДС в процентах
            // divisor = (100 + rate) / rate  =>  rate = 100 / (divisor - 1)
            return Math.Round(
                100m / (divisor - 1m),
                6,
                MidpointRounding.AwayFromZero
            );
        }

    }
}