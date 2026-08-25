using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	/// <summary>
	/// Комбо-модуль как узел дерева администрирования. До этого класса сущность 1270
	/// работала на голом ObjectContainer - понадобился собственный класс только затем,
	/// чтобы переопределить AssignNew: вместо паспорта на один модуль за раз открывается
	/// массовый набор состава галочками, как «Тарифы для модуля» у ModulePricelist.
	/// </summary>
	internal class ComboModuleContainer : ObjectContainer
	{
		public ComboModuleContainer() : base(GetEntity())
		{
		}

		public ComboModuleContainer(DataRow row) : base(GetEntity(), row)
		{
		}

		private static Entity GetEntity()
		{
			return EntityManager.GetEntity((int) Entities.ComboModule);
		}

		private int ComboModuleId
		{
			get { return int.Parse(IDs[0].ToString()); }
		}

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
			Entity contentEntity = EntityManager.GetEntity((int) Entities.ComboModuleContent);

			SelectionForm selector = new SelectionForm(
				moduleEntity, LoadAllModules().DefaultView, "Модули комбо-модуля", true);
			if (selector.ShowDialog(owner) != DialogResult.OK) return;

			foreach (PresentationObject po in selector.AddedItems)
				contentEntity.CreateObject(MakeContentParameters(po)).Update();

			foreach (PresentationObject po in selector.DeletedItems)
				contentEntity.CreateObject(MakeContentParameters(po)).Delete(true);

			FireContainerRefreshed();
		}

		private Dictionary<string, object> MakeContentParameters(PresentationObject module)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ComboModule.ParamNames.ComboModuleId] = ComboModuleId;
			procParameters[ComboModule.ParamNames.ModuleId] = ((Module) module).ModuleId;
			return procParameters;
		}

		private DataTable LoadAllModules()
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ComboModule.ParamNames.ComboModuleId] = ComboModuleId;
			return DataAccessor.LoadDataSet("ComboModuleAllModulesSelection", procParameters).Tables[0];
		}
	}
}
