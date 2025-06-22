using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public class ProductionStudio : ObjectContainer
	{
		public struct ParamNames
		{
			public const string StudioId = "studioID";
		}

		public ProductionStudio()
			: base(EntityManager.GetEntity((int) Entities.ProductionStudio))
		{
		}

		public ProductionStudio(DataRow row)
			: base(EntityManager.GetEntity((int) Entities.ProductionStudio), row)
		{
		}

		public override bool Update()
		{
			if (!base.Update())
				return false;

			// Submit children changes to database 
			foreach (ChildrenChanges childrenChanges in childrenChangesList)
			{
				foreach (PresentationObject po in childrenChanges.AddedObjects)
				{
					StudioAgency studioAgency =
						new StudioAgency(((Agency) po).AgencyId, StudioID);
					studioAgency.Update();
				}
				foreach (PresentationObject po in childrenChanges.DeletedObjects)
				{
					StudioAgency studioAgency = new StudioAgency(((Agency) po).AgencyId, StudioID);
					studioAgency.Delete(true);
				}
			}
			childrenChangesList.Clear();
			return true;
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == Constants.EntityActions.AssignNew)
				return base.IsActionEnabled(actionName, type) && ChildEntity != null;
			return base.IsActionEnabled(actionName, type);
		}

		public int StudioID
		{
			get { return int.Parse(IDs[0].ToString()); }
		}
	}
}