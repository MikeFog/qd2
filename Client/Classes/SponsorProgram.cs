using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal class SponsorProgram : ObjectContainer
	{
		public struct ParamNames
		{
			public const string SponsorProgramId = "sponsorProgramId";
		}

		public SponsorProgram() : base(GetEntity())
		{
		}

        public SponsorProgram(int programId) : this()
        {
            this[ParamNames.SponsorProgramId] = programId;
            IsNew = false;
            Refresh();
        }

        public static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.SponsorProgram);
		}

		public SponsorPricelist GetPricelist(DateTime theDate)
		{
			Entity entityPricelist = EntityManager.GetEntity((int) Entities.SponsorPricelist);
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(entityPricelist, InterfaceObjects.Grid, Constants.Actions.Load);

			procParameters["theDate"] = theDate;
			procParameters[ParamNames.SponsorProgramId] = SponsorProgramId;
			DataSet ds = (DataSet) DataAccessor.DoAction(procParameters);

			if (ds.Tables[0].Rows.Count > 0)
				return new SponsorPricelist(ds.Tables[0].Rows[0]);
			return null;
		}

		public int SponsorProgramId
		{
			get { return int.Parse(IDs[0].ToString()); }
		}
	}
}