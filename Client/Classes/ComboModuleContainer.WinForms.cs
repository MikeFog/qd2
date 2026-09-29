using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	// UI-часть ComboModuleContainer: AssignNew открывает массовый набор состава
	// галочками вместо паспорта. Загрузка и запись — в ComboModuleContainer.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ComboModuleContainer
	{
		protected override void AssignNew(IWin32Window owner)
		{
			EditModules(owner);
		}

		/// <summary>
		/// Набор состава комбо-модуля галочками - логика 1 в 1 с
		/// ModulePricelist.EditTariffList: плоский список с чекбоксами,
		/// добавленные/снятые строки проводятся через ComboModuleContentIUD.
		/// Модули комбо-модуля разбросаны по всем станциям, поэтому список -
		/// весь каталог модулей активных станций, а не окно одной станции.
		/// </summary>
		private void EditModules(IWin32Window owner)
		{
			Entity moduleEntity = EntityManager.GetEntity((int) Entities.Module);

			SelectionForm selector = new SelectionForm(
				moduleEntity, LoadAllModules().DefaultView, "Модули комбо-модуля", true);
			if (selector.ShowDialog(owner) != DialogResult.OK) return;

			ApplyModulesChanges(selector.AddedItems, selector.DeletedItems);
		}
	}
}
