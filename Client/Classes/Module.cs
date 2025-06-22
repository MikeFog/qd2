using System;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal class Module : ObjectContainer
	{
		#region Constants -------------------------------------

		internal struct ParamNames
		{
			public const string ModuleId = "moduleID";
		}

		#endregion

		#region Constructors ----------------------------------

		public Module() : base(EntityManager.GetEntity((int) Entities.Module))
		{
		}

		public Module(DataRow row) : base(EntityManager.GetEntity((int) Entities.Module), row)
		{
		}

		public Module(Entity entity, DataRow row) : base(entity, row)
		{
		}

        public Module(int moduleId) : this()
        {
			this[ParamNames.ModuleId] = moduleId;
			IsNew = false;
			Refresh();
        }

        #endregion

        public ModulePricelist GetPriceList(DateTime theDate)
		{
			Entity entityPricelist = EntityManager.GetEntity((int) Entities.ModulePricelist);
			DataAccessor.PrepareParameters(parameters, entityPricelist, InterfaceObjects.Grid, Constants.Actions.Load);

			parameters["theDate"] = theDate.ToShortDateString();
			DataSet ds = (DataSet) DataAccessor.DoAction(parameters);
			if (ds.Tables[0].Rows.Count > 0)
				return (ModulePricelist) entityPricelist.CreateObject(ds.Tables[0].Rows[0]);
			return null;
		}

		public Massmedia Massmedia
		{
			get
			{
				return Massmedia.
					GetMassmediaByID(int.Parse(this[Massmedia.ParamNames.MassmediaId].ToString()));
			}
		}

		public int ModuleId
		{
			get { return int.Parse(this[ParamNames.ModuleId].ToString()); }
		}
	}
}