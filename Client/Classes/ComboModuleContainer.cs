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

		/// <summary>Каталог модулей активных станций; isObjectSelected — уже в составе.</summary>
		internal DataTable LoadAllModules()
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ComboModule.ParamNames.ComboModuleId] = ComboModuleId;
			return DataAccessor.LoadDataSet("ComboModuleAllModulesSelection", procParameters).Tables[0];
		}

		/// <summary>
		/// Запись изменений состава: добавленные модули — строкой ComboModuleContentIUD,
		/// снятые — её удалением.
		/// </summary>
		internal void ApplyModulesChanges(IEnumerable<PresentationObject> added, IEnumerable<PresentationObject> removed)
		{
			Entity contentEntity = EntityManager.GetEntity((int) Entities.ComboModuleContent);

			foreach (PresentationObject po in added)
			{
				// Entity.CreateObject(Dictionary) присваивает Parameters, а у этого
				// свойства побочный эффект isNew = false - без явного сброса Update()
				// отправил бы UpdateItem с пустым comboModuleContentID, который тихо
				// не находит ни одной строки (WHERE comboModuleContentID = NULL).
				PresentationObject content = contentEntity.CreateObject(MakeContentParameters(po));
				content.IsNew = true;
				content.Update();
			}

			foreach (PresentationObject po in removed)
				contentEntity.CreateObject(MakeContentParameters(po)).Delete(true);

			FireContainerRefreshed();
		}
	}

	/// <summary>
	/// Состав комбо-модуля («Добавить» у узла комбо-модуля) снаружи сборки (веб):
	/// ComboModuleContainer internal.
	/// </summary>
	public static class ComboModuleComposition
	{
		/// <summary>Колонка признака «модуль уже в составе».</summary>
		public const string SelectedColumn = "isObjectSelected";

		public static DataTable Load(PresentationObject comboModule) => ((ComboModuleContainer)comboModule).LoadAllModules();

		public static void Apply(PresentationObject comboModule, IEnumerable<PresentationObject> added, IEnumerable<PresentationObject> removed) =>
			((ComboModuleContainer)comboModule).ApplyModulesChanges(added, removed);
	}
}
