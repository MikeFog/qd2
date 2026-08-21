using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	// UI-часть ModulePricelist: диспетчеризация действий, клонирование и
	// редактирование списка тарифов. Бизнес-часть (ApplyTariffListChanges) —
	// в ModulePricelist.cs. Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ModulePricelist
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch (actionName)
			{
				case Constants.EntityActions.Clone:
					CloneTariffList(owner);
					break;

				case "EditTariffList":
					EditTariffList(owner);
					break;

				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		private void CloneTariffList(IWin32Window owner)
		{
			ModulePricelist lst = new ModulePricelist
			{
				parameters = Parameters
			};

			lst.parameters[Constants.ParamNames.ActionName] = Constants.Actions.Clone;
			lst.parameters["sourceModulePriceListID"] = this["modulePriceListID"];
			lst.parameters.Remove(ModulePricelist.ParamNames.ModulePriceListID);

			if (lst.ShowPassport(owner))
			{
				FireContainerRefreshed();
				OnParentChanged(this, 1);
			}
		}

		private void EditTariffList(IWin32Window owner)
		{
			SelectionForm selector = new SelectionForm(
				EntityManager.GetEntity((int)Entities.Tariff), LoadTariffList().DefaultView, "Тарифы для модуля", true);
			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				Application.DoEvents();
				//Form.ActiveForm.Cursor = Cursors.WaitCursor;
				ApplyTariffListChanges(selector.AddedItems, selector.DeletedItems);
			}
		}
	}
}
