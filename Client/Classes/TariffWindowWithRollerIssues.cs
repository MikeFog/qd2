using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;
using static FogSoft.WinForm.Constants;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Classes
{
    internal class TariffWindowWithRollerIssues : TariffWindow, IComparable<TariffWindowWithRollerIssues>
	{
		public delegate TariffWindowWithRollerIssues GetTariffWindow2GroupDelegate(bool isWithPrev);
		public GetTariffWindow2GroupDelegate GetTariffWindow2Group;

		public delegate void TariffWindowUngroupedDelegate(bool isWithPrev, bool isUngroup);
		public TariffWindowUngroupedDelegate TariffWindowUngrouped;

		internal new struct ParamNames
		{
			public const string MaxCapacity = "maxCapacity";
			public const string CapacityInUseConfirmed = "capacityInUseConfirmed";
			public const string CapacityInUseUnconfirmed = "capacityInUseUnconfirmed";
			public const string WindowPrevId = "windowPrevId";
            public const string WindowNextId = "windowNextId";
        }

		private struct ActionNames
		{
			public const string Extend = "Extend";
			public const string GroupWithPrev = "GroupWithPrev";
			public const string GroupWithNext = "GroupWithNext";
			public const string UngroupPrev = "UngroupPrev";
            public const string UngroupNext = "UngroupNext";
        }

		private bool isGroupWithPrevEnabled = false;
        private bool isGroupWithNextEnabled = false;

        private TariffWindowWithRollerIssues(int windowId) : base(GetTariffWindowEntity())
		{
			parameters[TariffWindow.ParamNames.WindowId] = windowId;
			isNew = false;
		}

		public TariffWindowWithRollerIssues(Entity entity) : base(entity)
		{
		}

		public TariffWindowWithRollerIssues() : base(GetTariffWindowEntity())
		{
		}

		public TariffWindowWithRollerIssues(DataRow row)
			: base(EntityManager.GetEntity((int)Entities.TariffWindowTM), row)
		{
		}

		public TariffWindowWithRollerIssues(DataRow row, Entities entity)
			: base(EntityManager.GetEntity((int)entity), row)
		{
		}

		internal static TariffWindow GetWindowByDate(DateTime windowDate, Massmedia mm)
		{
			Dictionary<string, object> parameters =
				DataAccessor.PrepareParameters(GetTariffWindowEntity(), InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			parameters.Add("ActualDate", windowDate);
			parameters.Add("massmediaID", mm.MassmediaId);
			DataSet ds = (DataSet)DataAccessor.DoAction(parameters);
			DataTable dtAgency = ds.Tables[Constants.TableNames.Data];
			TariffWindowWithRollerIssues window = null;
			if (dtAgency != null && dtAgency.Rows.Count > 0)
				window = new TariffWindowWithRollerIssues(dtAgency.Rows[0]);
			return window;
		}

		public bool IsInGroup
		{
			get { return IsInGroupWithNext || IsInGroupWithPrev; }
		}

        public bool IsTariffUnited
        {
            get { return (int)this["IsTariffUnited"] == 1; }
        }

        private bool IsInGroupWithPrev
		{
            get { return this[ParamNames.WindowPrevId] != DBNull.Value; }
        }

        private bool IsInGroupWithNext
        {
            get { return this[ParamNames.WindowNextId] != DBNull.Value; }
        }

        public bool IsGroupWithPrevEnabled
		{
			set { isGroupWithPrevEnabled = value; }
		}

        public bool IsGroupWithNextEnabled
        {
            set { isGroupWithNextEnabled = value; }
        }

        public bool IsRollerExist(int rollerId)
		{
			Dictionary<string, object> procParams = PrepareProcParameters();
			procParams[Roller.ParamNames.RollerId] = rollerId;

			DataSet ds = (DataSet)DataAccessor.DoAction(procParams);
			return ds.Tables[Constants.TableNames.Data].Rows.Count > 0;
		}

		public bool IsRollerOfTheFirmExist(int firmId)
		{
			Dictionary<string, object> procParams = PrepareProcParameters();
			procParams[Firm.ParamNames.FirmId] = firmId;

			DataSet ds = (DataSet)DataAccessor.DoAction(procParams);
			return ds.Tables[Constants.TableNames.Data].Rows.Count > 0;
		}

		private Dictionary<string, object> PrepareProcParameters()
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int)Entities.TariffWindow),
				InterfaceObjects.SimpleJournal,
				Constants.Actions.LoadIssues);
			procParameters[TariffWindow.ParamNames.WindowId] = WindowId;
			procParameters["showUnconfirmed"] = false;
			return procParameters;
		}

		public bool HasCurrentCampaignIssues { get; set; }

		public bool IsFirstPositionOccupied
		{
			get { return bool.Parse(this["isFirstPositionOccupied"].ToString()); }
		}

		public bool IsSecondPositionOccupied
		{
			get { return bool.Parse(this["isSecondPositionOccupied"].ToString()); }
		}

		public bool IsLastPositionOccupied
		{
			get { return bool.Parse(this["isLastPositionOccupied"].ToString()); }
		}

		public int GetFreeTime(bool withUnconfirmed)
		{
			return Duration - TimeInUseConfirmed - (withUnconfirmed ? TimeInUseUnconfirmed : 0);
		}

		public int FirstPositionsUnconfirmed
		{
			get { return int.Parse(this["firstPositionsUnconfirmed"].ToString()); }
		}

		public int SecondPositionsUnconfirmed
		{
			get { return int.Parse(this["secondPositionsUnconfirmed"].ToString()); }
		}

		public int LastPositionsUnconfirmed
		{
			get { return int.Parse(this["lastPositionsUnconfirmed"].ToString()); }
		}

        private bool IsCreatedByTraffic 
		{ 
			get 
			{ 
				return this[Tariff.ParamNames.TariffId] == DBNull.Value; 
			} 
		}

        private static Entity GetTariffWindowEntity()
		{
			return EntityManager.GetEntity((int) Entities.TariffWindow);
		}

		public static TariffWindowWithRollerIssues CreateTariffWindowById(int windowId)
		{
			return new TariffWindowWithRollerIssues(windowId);
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, ActionNames.Extend) == 0)
				Extend();
			else if (string.Compare(actionName, ActionNames.GroupWithNext) == 0)
				GroupWithWindow(false);
            else if (string.Compare(actionName, ActionNames.GroupWithPrev) == 0)
                GroupWithWindow(true);
            else if (string.Compare(actionName, ActionNames.UngroupNext) == 0)
                UngroupWindows(false);
            else if (string.Compare(actionName, ActionNames.UngroupPrev) == 0)
                UngroupWindows(true);
            else base.DoAction(actionName, owner, interfaceObject);
		}

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
			if (string.Compare(actionName, ActionNames.UngroupPrev) == 0)
				return IsInGroupWithPrev;
            if (string.Compare(actionName, ActionNames.UngroupNext) == 0)
                return IsInGroupWithNext;
            if (string.Compare(actionName, ActionNames.GroupWithNext) == 0)
                return !IsInGroupWithNext && isGroupWithNextEnabled;
            if (string.Compare(actionName, ActionNames.GroupWithPrev) == 0)
                return !IsInGroupWithPrev && isGroupWithPrevEnabled;
            if (string.Compare(actionName, EntityActions.Delete) == 0)
                return IsCreatedByTraffic;
            return base.IsActionEnabled(actionName, type);
        }

        public event ObjectDelegate TariffExtend;

		private void Extend()
		{
			int mmId = int.Parse(parameters[Massmedia.ParamNames.MassmediaId].ToString());
			FrmWindowTariffTemplate frm = new FrmWindowTariffTemplate(WindowDate, Duration, DurationTotal, Massmedia.GetMassmediaByID(mmId));
			if (frm.ShowDialog(Globals.MdiParent) == DialogResult.OK)
			{
				if (TariffExtend != null)
					TariffExtend(this);
			}
		}

        private void GroupWithWindow(bool isWithPrev)
		{
			try
			{
                TariffWindowWithRollerIssues window = GetTariffWindow2Group(isWithPrev);
				if (window != null)
				{
                    Cursor.Current = Cursors.WaitCursor;
                    if (isWithPrev)
						window[ParamNames.WindowNextId] = WindowId;
					else
						window[ParamNames.WindowPrevId] = WindowId;
					window.Update();

					if (isWithPrev)
						this[ParamNames.WindowPrevId] = window.WindowId;
					else
						this[ParamNames.WindowNextId] = window.WindowId;
					Update();
                    TariffWindowUngrouped?.Invoke(isWithPrev, false);
                }
			}
			finally { Cursor.Current = Cursors.Default; }
		}

		private void UngroupWindows(bool isWithPrev)
		{
			try
			{
				TariffWindowWithRollerIssues window = CreateTariffWindowById(isWithPrev ? int.Parse(this[ParamNames.WindowPrevId].ToString()) : int.Parse(this[ParamNames.WindowNextId].ToString()));
				window.Refresh();

				if (MessageBox.ShowQuestion(string.Format("Хотите отменить объединение рекламных окон '{0}' и '{1}'?", WindowDate.ToString("g"), window.WindowDate.ToString("g"))) == DialogResult.Yes)
				{
                    Cursor.Current = Cursors.WaitCursor;
                    if (isWithPrev)
						window[ParamNames.WindowNextId] = null;
					else
						window[ParamNames.WindowPrevId] = null;
					window.Update();

					if (isWithPrev)
						this[ParamNames.WindowPrevId] = null;
					else
						this[ParamNames.WindowNextId] = null;
					Update();
					TariffWindowUngrouped?.Invoke(window.WindowDate < WindowDate, true);
				}
			}
			finally { Cursor.Current = Cursors.Default; }
        }

        override public DataTable LoadIssues(bool showUnconfirmed, Entity issueEntity)
		{
			if(issueEntity.Id == (int)Entities.ModuleIssue)
				return LoadModuleIssues(showUnconfirmed);
			return LoadIssues(showUnconfirmed, WindowId);
		}

		public static DataTable LoadIssues(bool showUnconfirmed, int windowID)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int)Entities.TariffWindow),
				InterfaceObjects.SimpleJournal,
				Constants.Actions.LoadIssues);
			procParameters[TariffWindow.ParamNames.WindowId] = windowID;
			procParameters["showUnconfirmed"] = showUnconfirmed;
			DataSet ds = (DataSet)DataAccessor.DoAction(procParameters);
			return ds.Tables[Constants.TableNames.Data];
		}

		private DataTable LoadModuleIssues(bool showUnconfirmed)
		{
		    Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
		    procParameters.Add(TariffWindow.ParamNames.WindowId, WindowId);
		    procParameters.Add("showUnconfirmed", showUnconfirmed);
		    return ModuleIssue.GetEntity().GetContent(procParameters);
		}

        public int CompareTo(TariffWindowWithRollerIssues other)
        {
			if (other == null)
				return 0;

			if (TimeLeftWithUnconfirmed > other.TimeLeftWithUnconfirmed) return -1;
			if (TimeLeftWithUnconfirmed < other.TimeLeftWithUnconfirmed) return 1;
			return 0;
		}
    }
}