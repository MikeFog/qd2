using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть PackageDiscount: DoAction. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	partial class PackageDiscount
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			/*
			else if (string.Compare(actionName, ActionNames.AssignMassmedia) == 0)
			{
				Entity child = ChildEntity;
				ChildEntity = EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia);
				base.DoAction(Constants.EntityActions.AssignNew, owner, interfaceObject);
				ChildEntity = child;
				FireContainerRefreshed();
			}
			else 
			*/
			
			if (string.Compare(actionName, ActionNames.AssignPriceList) == 0)
			{
				Entity child = ChildEntity;
				ChildEntity = EntityManager.GetEntity((int)Entities.PackageDiscountPriceLists);
				base.DoAction(Constants.EntityActions.AssignNew, owner, interfaceObject);
				ChildEntity = child;
				FireContainerRefreshed();
			}
			else base.DoAction(actionName, owner, interfaceObject);
		}

		// Число станций — условие всех прайс-листов пакета: перед записью показываем задетые акции
		public override bool Update()
		{
			return DiscountAffectedActionsForm.ConfirmSave(this) && base.Update();
		}
	}
}
