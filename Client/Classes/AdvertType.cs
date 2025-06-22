using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using static FogSoft.WinForm.Constants;

namespace Merlin.Classes
{
	public class AdvertType : ObjectContainer
	{
		public struct ParamNames
		{
			public const string AdvertTypeId = "AdvertTypeId";
			public const string ParentId = "parentID";
			public const string IsParent = "isParent";
		}

		public AdvertType() : base(EntityManager.GetEntity((int) Entities.AdvertType))
		{
		}

		public AdvertType(DataRow row) : base(EntityManager.GetEntity((int) Entities.AdvertType), row)
		{
		}

		public override DataTable GetContent()
		{
            Entity childEntity = RelationScenario.GetChildEntity(entity.Id).ChildEntity;
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(childEntity);
			procParameters[ParamNames.ParentId] = AdvertTypeId;

			return ((DataSet) DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
		}

		public override bool IsChildNodeExpandable
		{
			get { return !Convert.ToBoolean(Parameters[ParamNames.IsParent]); }
		}

		private int AdvertTypeId
		{
			get { return int.Parse(IDs[0].ToString()); }
		}

        protected override void AssignNew(IWin32Window owner)
        {
			PresentationObject newObject = iterator.ChildEntity.NewObject;

			newObject[ParamNames.ParentId] = parameters[entity.PKColumns[0]];

			if (newObject.ShowPassport(owner))
			{
				newObject.Refresh();
				OnObjectCreated(newObject);
			}
		}
    }
}