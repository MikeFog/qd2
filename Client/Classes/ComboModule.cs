using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Комбо-модуль - объединение модулей разных радиостанций. Собственного класса-сущности
	/// у него нет: метаданные администрирования работают на ObjectContainer, поэтому здесь
	/// только загрузка данных для формы размещения.
	/// </summary>
	internal static class ComboModule
	{
		public struct ParamNames
		{
			public const string ComboModuleId = "comboModuleID";
			public const string ModuleId = "moduleID";
			public const string ModulePriceListId = "modulePriceListID";
			public const string IssueDate = "issueDate";
			public const string FreeTime = "freeTime";
			public const string FreeCapacity = "freeCapacity";
			public const string MaxCapacity = "maxCapacity";
			public const string Price = "price";
			public const string MassmediaId = "massmediaID";
			/// <summary>Название радиостанции с городом - в форме размещения города нет отдельной колонкой.</summary>
			public const string MassmediaName = "massmediaNameWithGroup";
			public const string ModuleName = "moduleName";
		}

		/// <summary>Модули комбо-модуля - строки грида размещения.</summary>
		public static DataTable LoadContent(int comboModuleID)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.ComboModuleId] = comboModuleID;
			return DataAccessor.LoadDataSet("ComboModuleContentRetrieve", procParameters).Tables[0];
		}

		/// <summary>
		/// Остаток времени по модулям за период: строка на (модуль, день), и только для тех
		/// дней, когда модуль есть целиком. Дни без строки - пустые ячейки грида.
		/// </summary>
		public static DataTable LoadFreeTime(int comboModuleID, DateTime startDate, DateTime finishDate,
											 bool showUnconfirmed)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.ComboModuleId] = comboModuleID;
			procParameters["startDate"] = startDate;
			procParameters["finishDate"] = finishDate;
			procParameters["showUnconfirmed"] = showUnconfirmed;
			return DataAccessor.LoadDataSet("ComboModuleFreeTimeRetrieve", procParameters).Tables[0];
		}

		/// <summary>Выпуски модулей всей акции - панель «Добавленные выпуски».</summary>
		public static DataTable LoadIssues(int actionID)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[Merlin.Classes.Action.ParamNames.ActionId] = actionID;
			return DataAccessor.LoadDataSet("ComboModuleIssuesRetrieve", procParameters).Tables[0];
		}
	}
}
