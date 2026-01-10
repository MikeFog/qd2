using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Forms;
using Merlin.License;

namespace Merlin.Classes
{
    public class Massmedia : Organization
    {
		#region Constants -------------------------------------

		public enum AttributeSelectors
		{
			NameAndGroupOnly = 1,
			TrafficDeadLine = 2

        }

		protected struct ActionNames
		{
			public const string AddSponsorProgram = "AddSponsorProgram";
			public const string AddDisabledWindow = "AddDisabledWindow";
			public const string AddPriceList = "AddPriceList";
			public const string AddModule = "AddModule";
		}

		public new struct ParamNames
		{
			public const string MassmediaId = "massmediaID";
			public const string ActiveOnly = "activeOnly";
			public const string DeadLine = "deadLine";
			public const string ExportName = "exportName";
			public const string Name = "name";
			public const string Founder = "fullPrefix";
			public const string GroupName = "groupName";
            public const string GroupId = "massmediaGroupID";
            public const string MassmediaName = "Prefix";
            public const string CertificateIssued = "CertificateIssued";
            public const string VolumeC = "volume_c";
            public const string VolumeN = "volume_n";
            public const string VolumeP = "volume_p";
            public const string VolumeM = "volume_m";
            public const string VolumeJ = "volume_j";
        }

		#endregion

		#region Constructors ----------------------------------

		public Massmedia() : base(GetEntity())
		{
		}

		private Massmedia(int massmediaID) : base(GetEntity())
		{
			this[ParamNames.MassmediaId] = massmediaID;
			isNew = false;
		}

		public Massmedia(DataRow row) : base(EntityManager.GetEntity((int) Entities.MassMedia), row)
		{
		}

		#endregion

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch (actionName)
			{
				case ActionNames.AddSponsorProgram:
				case ActionNames.AddDisabledWindow:
				case ActionNames.AddPriceList:
				case ActionNames.AddModule:
					base.DoAction(Constants.EntityActions.AssignNew, owner, interfaceObject);
					break;
				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ActionNames.AddSponsorProgram)
				return ChildEntity != null && ChildEntity.Id == (int) Entities.SponsorProgram;
			else if (actionName == ActionNames.AddDisabledWindow)
				return ChildEntity != null && ChildEntity.Id == (int) Entities.DisabledWindow;
			else if (actionName == ActionNames.AddPriceList)
				return ChildEntity != null && ChildEntity.Id == (int) Entities.Pricelist;
			else if (actionName == ActionNames.AddModule)
				return ChildEntity != null && ChildEntity.Id == (int) Entities.Module;
			else if (actionName == Constants.EntityActions.ShowFilters)
				return ChildEntity != null && ChildEntity.Id == (int) Entities.ProgramIssue;

			return base.IsActionEnabled(actionName, type);
		}

		public int MassmediaId
		{
			get { return int.Parse(IDs[0].ToString()); }
		}

		public DataTable Agencies
		{
			get
			{
				ChildEntity = EntityManager.GetEntity((int) Entities.MassmediaAgency);
				return GetContent();
			}
		}

		public DateTime? DeadLine
		{
			get
			{
				if (!parameters.ContainsKey(ParamNames.DeadLine)) Refresh();
				if (this[ParamNames.DeadLine] == DBNull.Value)
					return null;
				return DateTime.Parse(this[ParamNames.DeadLine].ToString());
			}
			set { this[ParamNames.DeadLine] = value; }
		}

		public string Founder
		{
			get { return this[ParamNames.Founder].ToString(); }
		}

		public string GroupName
		{
			get { return this[ParamNames.GroupName].ToString(); }
		}

        public string MassmediaName
        {
            get { return this[ParamNames.MassmediaName].ToString(); }
        }

		public	string VolumeCStr
		{
            get { return this[ParamNames.VolumeC].ToString().Replace(",", "."); }
        }

        public string VolumeNStr
        {
            get { return this[ParamNames.VolumeN].ToString().Replace(",", "."); }
        }

        public string VolumePStr
        {
            get { return this[ParamNames.VolumeP].ToString().Replace(",", "."); }
        }

        public string VolumeMStr
        {
            get { return this[ParamNames.VolumeM].ToString().Replace(",", "."); }
        }

        public string VolumeJStr
        {
            get { return this[ParamNames.VolumeJ].ToString().Replace(",", "."); }
        }

        /// <summary>
        /// Returns modules which have tariff for given date
        /// </summary>
        /// <param name="theDate"></param>
        /// <returns></returns>
        public DataTable GetModules(DateTime? theDate)
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.Module);
			this["theDate"] = theDate;
			return GetContent();
		}

		public Pricelist GetPriceList(DateTime theDate)
		{
			Entity entityPricelist = EntityManager.GetEntity((int) Entities.Pricelist);
			DataAccessor.PrepareParameters(
				parameters, entityPricelist, InterfaceObjects.Grid, Constants.Actions.Load);

			this["theDate"] = theDate;
			DataSet ds = (DataSet) DataAccessor.DoAction(parameters);
			if (ds.Tables[0].Rows.Count > 0)
				return (Pricelist) entityPricelist.CreateObject(ds.Tables[0].Rows[0]);

			return null;
		}

		internal DataSet GetRollerCells(Pricelist pricelist, DateTime startDate, DateTime finishDate,
		                              Module module, bool showUnconfirmed, Campaign campaign, RollerPositions rollerPosition)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int) Entities.GridCell), InterfaceObjects.Grid,
				Constants.Actions.Load);
			procParameters["showUnconfirmed"] = showUnconfirmed;
			procParameters["position"] = (int) rollerPosition;
			if (module != null)
				procParameters[Module.ParamNames.ModuleId] = module.ModuleId;
			if (campaign != null)
				procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			return GetIssues(pricelist, procParameters, startDate, finishDate);
		}

		public DataSet GetProgramIssues(Pricelist pricelist, DateTime date)
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.ProgramIssue));
			return GetIssues(pricelist, procParameters, date, date.AddDays(1));
		}

		private DataSet GetIssues(Pricelist pricelist, Dictionary<string, object> procParameters,
		                          DateTime startDate, DateTime finishDate)
		{
			// Process startDate and finishDate
			ProcessStartFinishDates(pricelist, procParameters, startDate, finishDate);

			DataSet ds = (DataSet) DataAccessor.DoAction(procParameters);
			return ds /*.Tables[Constants.TableNames.Data]*/;
		}

		public DataTable GetSponsorPrograms(bool activeOnly)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int) Entities.SponsorProgram));
			procParameters[ParamNames.MassmediaId] = MassmediaId;
			procParameters[ParamNames.ActiveOnly] = activeOnly;

			DataSet ds = (DataSet) DataAccessor.DoAction(procParameters);
			return ds.Tables[Constants.TableNames.Data];
		}

		/// <summary>
		/// Get Only Confirmed Issues
		/// </summary>
		/// <param name="pricelist"></param>
		/// <param name="sponsorProgram"></param>
		/// <param name="startDate"></param>
		/// <param name="finishDate"></param>
		/// <returns></returns>
        internal DataTable GetProgramIssues(Pricelist pricelist, SponsorProgram sponsorProgram,
		                                  DateTime startDate, DateTime finishDate)
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.ProgramIssue));
			procParameters[RollerIssue.ParamNames.StartDate] = startDate.Date;
			procParameters[RollerIssue.ParamNames.FinishDate] = finishDate.Date;
			procParameters[SponsorCampaignProgram.ParamNames.ProgramId] = sponsorProgram.IDs[0];
			procParameters["onlyConfirmed"] = true;
			return ((DataSet) DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
		}

		private void ProcessStartFinishDates(Pricelist pricelist, Dictionary<string, object> procParameters,
		                                     DateTime startDate, DateTime finishDate)
		{
			DateTime broadcastStart = ((MassmediaPricelist) pricelist).BroadcastStart;

			procParameters[RollerIssue.ParamNames.StartDate] =
				startDate.Date.AddHours(broadcastStart.Hour).AddMinutes(broadcastStart.Minute);
			procParameters[RollerIssue.ParamNames.FinishDate] =
				finishDate.Date.AddDays(1).AddHours(broadcastStart.Hour).AddMinutes(broadcastStart.Minute - 1);
			procParameters[ParamNames.MassmediaId] = MassmediaId;
		}

		public override bool Update()
		{
			if (IsNew && !AdvertAgLicence.CheckLicenseMassmediasCountForAdd())
				return false;

			if (!base.Update())
				return false;

			// Submit children changes to database 
			foreach (ChildrenChanges childrenChanges in childrenChangesList)
			{
				foreach (PresentationObject po in childrenChanges.AddedObjects)
				{
					MassmediaAgency massmediaAgency =
						new MassmediaAgency(((Agency) po).AgencyId, MassmediaId);
					massmediaAgency.Update();
				}

				foreach (PresentationObject po in childrenChanges.DeletedObjects)
				{
					MassmediaAgency massmediaAgency =
						new MassmediaAgency(((Agency) po).AgencyId, MassmediaId);
					massmediaAgency.Delete(true);
				}
			}
			childrenChangesList.Clear();

			return true;
		}

		internal static DataView LoadMassmediaList()
		{
			Dictionary<string, object> procParameters = DataAccessor.
				PrepareParameters(EntityManager.GetEntity((int) Entities.MassMedia));
			procParameters["ShowActive"] = true;
		    return LoadMassmediaList(procParameters);
		}

		public static DataView LoadGroupsWithShowAllOption()
		{
			Entity entity = EntityManager.GetEntity((int)Entities.MassmediaGroup);
			entity.ClearCache();
            DataTable dataTable = entity.GetContent().Copy();
            DataRow row = dataTable.NewRow();
            row[0] = 0;
            row[1] = "Показать все";
            dataTable.Rows.InsertAt(row, 0);
            return dataTable.DefaultView;
        }

		public static void LoadRadiostationsByGroup(LookUp cmbRadioStationGroup, SmartGrid grdRadiostations)
		{
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(grdRadiostations.Entity);
            int groupId = ParseHelper.GetInt32FromObject(cmbRadioStationGroup.SelectedValue, 0);
            if (groupId > 0)
                procParameters.Add(Massmedia.ParamNames.GroupId, groupId);
            grdRadiostations.DataSource = ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data].DefaultView;
        }

        internal static DataView LoadMassmediaList(Dictionary<string, object> parameters)
        {
            DataSet ds = (DataSet)DataAccessor.DoAction(parameters);
            return ds.Tables[Constants.TableNames.Data].DefaultView;
        }

		internal void SetDeadLine(DateTime dateTime)
		{
			Refresh();
			this[ParamNames.DeadLine] = dateTime;
			Update();
		}

		internal static Massmedia GetMassmediaByID(int massmediaID)
		{
			Massmedia massmedia = new Massmedia(massmediaID);
			massmedia.Refresh();
			return massmedia;
		}

		public static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.MassMedia);
		}

		public override PassportForm GetPassportForm(DataSet ds)
		{
			return new MassmediaPassport(this, ds);
		}

		public string EnterPath
		{
			get { return this["rollerEnterPath"].ToString(); }
		}

		public string ExitPath
		{
			get { return this["rollerExitPath"].ToString(); }
		}

		public string EtcPath
		{
			get { return this["rollerEtcPath"].ToString(); }
		}

		public string RollersPath
		{
			get { return this["rollerPath"].ToString(); }
		}

		public int EnterMax
		{
			get { return GetNumber(this["rollerEnterMax"].ToString()); }
		}

		public int ExitMax
		{
			get { return GetNumber(this["rollerExitMax"].ToString()); }
		}

		public int EtcMax
		{
			get { return GetNumber(this["rollerEtcMax"].ToString()); }
		}

		public int EnterMin
		{
			get { return GetNumber(this["rollerEnterMin"].ToString()); }
		}

		public int ExitMin
		{
			get { return GetNumber(this["rollerExitMin"].ToString()); }
		}

		public int EtcMin
		{
			get { return GetNumber(this["rollerEtcMin"].ToString()); }
		}

		private static int GetNumber(string str)
		{
			return ParseHelper.ParseToInt32(str, 0);
		}

        internal TariffWindow GetTariffWindow(DateTime issueDate)
        {
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.MassmediaId] = MassmediaId;
			procParameters["windowDate"] = issueDate;

			DataTable dt = DataAccessor.LoadDataSet("TariffWindowRetrieveByDate", procParameters).Tables[0];
			if (dt.Rows.Count == 0) return null;
			return new TariffWindowWithRollerIssues(dt.Rows[0]);
        }

        public override string Name
        {
            get
            {
                if (StringUtil.IsNullOrEmpty(GroupName))
                    return base.Name;
				return base.Name + " (" + GroupName + ")";
            }
        }

		public string NameWithoutGroup
		{
			get { return base.Name; }
		}
        public string CertificateIssued
        {
            get { return this[ParamNames.CertificateIssued].ToString(); }
        }
    }
}