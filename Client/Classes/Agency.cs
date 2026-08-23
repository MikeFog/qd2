using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

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

	// ShowPassport переехал в Agency.WinForms.cs. Остальные диалоги в этом классе
	// (строка ~172, поиск/выбор агентства) пока на месте — своя партия позже.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Agency : Organization
    {
		public enum AttributeSelectors
		{
			NameOnly = 1
		}

		public new struct ParamNames
		{
			public const string AgencyId = "agencyID";
            public const string ReportPlace = "reportPlace";
            public const string Path2ProposalTemplate = "path2proposalTemplate";
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

        /// <summary>
        /// Агентства, связанные с <paramref name="presentationObject"/>. Если их
        /// не одно — диалог выбора не нужен, результат уже готов (null для 0,
        /// список из одного элемента для 1) и <paramref name="candidatesForDialog"/>
        /// остаётся null. Если их несколько — возвращает null, а
        /// <paramref name="candidatesForDialog"/> заполняется списком на выбор.
        /// </summary>
        internal static List<PresentationObject> GetAgenciesForSelection(
            PresentationObject presentationObject, Dictionary<string, object> parameters,
            out DataTable candidatesForDialog)
        {
            candidatesForDialog = null;

            // Load all agencies associated with given presentation objects
            DataAccessor.PrepareParameters(parameters, presentationObject.Entity,
                                                                         InterfaceObjects.SimpleJournal, Constants.Actions.LoadAgencies);

            DataSet ds = (DataSet)DataAccessor.DoAction(parameters);
            DataTable dtAgency = ds.Tables[Constants.TableNames.Data];

            int count = dtAgency.Rows.Count;
            if (count == 0)
                return null;

            if (count > 1)
            {
                candidatesForDialog = dtAgency;
                return null;
            }

            List<PresentationObject> items = new List<PresentationObject>(1);
            int agencyId = int.Parse(dtAgency.Rows[0][ParamNames.AgencyId].ToString());
            items.Add(GetAgencyByID(agencyId));
            return items;
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