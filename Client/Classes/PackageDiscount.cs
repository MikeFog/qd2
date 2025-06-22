using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public class PackageDiscount : ObjectContainer
	{
		private struct Actions
		{
			//public const string AssignMassmedia = "AssignMassmedia";
			public const string AssignPriceList = "AssignPriceList";
		}

		public PackageDiscount() 
			: base(EntityManager.GetEntity((int)Entities.PackageDiscount))
		{
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			/*
			else if (string.Compare(actionName, Actions.AssignMassmedia) == 0)
			{
				Entity child = ChildEntity;
				ChildEntity = EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia);
				base.DoAction(Constants.EntityActions.AssignNew, owner, interfaceObject);
				ChildEntity = child;
				FireContainerRefreshed();
			}
			else 
			*/
			
			if (string.Compare(actionName, Actions.AssignPriceList) == 0)
			{
				Entity child = ChildEntity;
				ChildEntity = EntityManager.GetEntity((int)Entities.PackageDiscountPriceLists);
				base.DoAction(Constants.EntityActions.AssignNew, owner, interfaceObject);
				ChildEntity = child;
				FireContainerRefreshed();
			}
			else base.DoAction(actionName, owner, interfaceObject);
		}
	}
}
