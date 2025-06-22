using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	class PackModule : ObjectContainer
	{
		public struct ParamNames
		{
			public const string PackModuleId = "packModuleId";
		}

		public PackModule(DataRow row) : base(GetPackModuleEntity(), row) 
		{ }

		public PackModule(int packModuleID) : base(GetPackModuleEntity())
		{
			this[ParamNames.PackModuleId] = packModuleID;
			isNew = false;
			Refresh();
		}


        public static DataTable LoadModules(DateTime date)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters.Add("date", date);
			return EntityManager.GetEntity((int)Entities.PackModule).GetContent(parameters);
		}

		private static Entity GetPackModuleEntity()
		{
			return EntityManager.GetEntity((int)Entities.PackModule);
		}

		public Pricelist GetPriceList(DateTime date)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();

			procParameters["theDate"] = date.ToShortDateString();
			procParameters[ParamNames.PackModuleId] = PackModuleId;

			DataSet ds = DataAccessor.LoadDataSet("PackModulePricelistByDate", procParameters);
			if(ds.Tables[0].Rows.Count > 0)
			{
 				Entity ent = EntityManager.GetEntity((int)Entities.PackModulePricelist);
				return (Pricelist)ent.CreateObject(ds.Tables[0].Rows[0]);
			}

			return null;			
		}

		public int PackModuleId
		{
			get { return int.Parse(parameters[ParamNames.PackModuleId].ToString()); }
		}
	}
}