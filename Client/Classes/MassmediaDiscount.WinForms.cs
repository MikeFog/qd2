using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть MassmediaDiscount: DoAction. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	partial class MassmediaDiscount
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, Actions.AssignRelease) == 0)
			{
				Entity child = ChildEntity;
				ChildEntity = EntityManager.GetEntity((int)Entities.DiscountRelease);
				base.DoAction(Constants.EntityActions.AssignNew, owner, interfaceObject);
				ChildEntity = child;
				FireContainerRefreshed();
			}
			else 
				base.DoAction(actionName, owner, interfaceObject);
		}
	}
}
