using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;

namespace Merlin.Classes
{
	public class Roller : PresentationObject
	{
		public enum AttributeSelectors
		{
			NameOnly = 1
		}

		public struct ParamNames
		{
			public const string RollerId = "rollerID";
			public const string RollerTypeId = "rolTypeID";
			public const string Duration = "duration";
			public const string DurationString = "durationString";
			public const string Path = "path";
            public const string IsMute = "isMute";
            public const string IsCommon = "isCommon";
            public const string AdvertTypeName = "advertTypeName";
            public const string ParentId = "parentID";
        }

        public struct ActionNames
        {
            public const string ChangeAdvertType = "ChangeAdvertType";
        }

        public Roller() : base(GetEntity())
		{
		}

		public Roller(DataRow row) : base(GetEntity(), row)
		{
		}

        public Roller(Dictionary<string, object> parameters)
            : base(GetEntity(), parameters)
        {
        }

		public Roller(int rollerID)	: this()
		{
			this[ParamNames.RollerId] = rollerID;
			isNew = false;
			Refresh();
		}

        protected Roller(Entity entity) : base(entity) { }

        /*
		public void SaveAdvertTypes(List<PresentationObject> list)
		{
			DataAccessor.PrepareParameters(parameters, entity, InterfaceObjects.PropertyPage,
			                               Constants.Actions.Detach);
			if (DataAccessor.IsProcedureExist(parameters))
			{
				DataAccessor.DoAction(parameters);
			}
			if (list == null) return;
			foreach (PresentationObject po in list)
			{
				if (po.Entity.Id == (int) Entities.RollerAdvertType)
				{
					po[ParamNames.RollerId] = RollerId;
					po.Update();
				}
			}
		}
		*/

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
				RollerPassportForm passport = new RollerPassportForm(this, ds);
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

		public bool HasAdvertType
		{
			get { return this[AdvertType.ParamNames.AdvertTypeId] != DBNull.Value; }
		}

		public bool IsDummy
		{
			get { return bool.Parse(this[ParamNames.IsMute].ToString()); }
		}

        public bool IsCommon
        {
            get { return bool.Parse(this[ParamNames.IsCommon].ToString()); }
			set { this[ParamNames.IsCommon] = value; }
        }

        public bool IsCloneOfCommon
        {
            get { return this[ParamNames.ParentId] != DBNull.Value; }
        }

        public int Duration
		{
			get { return int.Parse(this[ParamNames.Duration].ToString()); }
		}

		public string DurationString
		{
			get { return this[ParamNames.DurationString].ToString(); }
		}

        public string AdvertTypeName
        {
            get { return this[ParamNames.AdvertTypeName].ToString(); }
        }

        private int FirmId
		{
			get { return int.Parse(this[Firm.ParamNames.FirmId].ToString()); }
		}

		public Firm Firm
		{
			get { return Firm.GetFirmById(FirmId); }
		}
		
		internal int RollerId
		{
			get { return int.Parse(this[ParamNames.RollerId].ToString()); }
		}

		public static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.Roller);
		}

		public bool IsUsed
		{
			get
			{
				Dictionary<string, object> pr = new Dictionary<string, object>();
				pr["rollerID"] = RollerId;
				pr["isUsed"] = null;
				DataAccessor.ExecuteNonQuery("GetRollerIsUsed", pr);
				return (bool)pr["isUsed"];
			}
		}
	}
}