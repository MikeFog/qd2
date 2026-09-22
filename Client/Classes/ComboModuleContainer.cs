using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть (AssignNew, EditModules) — в ComboModuleContainer.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	/// <summary>
	/// Комбо-модуль как узел дерева администрирования. До этого класса сущность 1270
	/// работала на голом ObjectContainer - понадобился собственный класс только затем,
	/// чтобы переопределить AssignNew: вместо паспорта на один модуль за раз открывается
	/// массовый набор состава галочками, как «Тарифы для модуля» у ModulePricelist.
	/// </summary>
	internal partial class ComboModuleContainer : ObjectContainer
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
