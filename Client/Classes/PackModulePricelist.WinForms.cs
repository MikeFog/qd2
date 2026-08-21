using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть PackModulePricelist: диспетчеризация действий, открытие редактора
	// содержимого пакета, клонирование и назначение через паспорт.
	// Дословный перенос из PackModulePricelist.cs, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class PackModulePricelist
	{
		public void EditContent(Form owner)
		{
			FrmPackModuleContent fContent = new FrmPackModuleContent(this);
			fContent.ShowDialog(owner);
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.EntityActions.Clone:
					CloneContent(owner);
					break;

				case Constants.EntityActions.Edit:
					EditContent((Form) owner);
					break;

				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		private void CloneContent(IWin32Window owner)
		{
			PackModulePricelist lst = new PackModulePricelist {parameters = Parameters};
			lst.parameters["sourcePricelistID"] = parameters["pricelistID"];
			lst.parameters[Constants.ParamNames.ActionName] = Constants.EntityActions.Clone;

			if (lst.ShowPassport(owner))
			{
				FireContainerRefreshed();
				OnParentChanged(this, 1);
			}
		}

		protected override void AssignExisting(IWin32Window owner)
		{
			PresentationObject newObj =
				EntityManager.GetEntity((int) Entities.PackModuleContent).NewObject;
			newObj[ParamNames.PricelistId] = PricelistId;
			if (newObj.ShowPassport(owner))
			{
				OnObjectCreated(newObj);
			}
		}
	}
}
