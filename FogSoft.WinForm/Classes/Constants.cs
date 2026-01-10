namespace FogSoft.WinForm
{
	public enum MessageNames
	{
		StartFinishDateError = 1
	}

	public enum InterfaceObjects
	{
		FakeModule = 0,
		SimpleJournal = 118,
		PropertyPage = 119,
		Selector = 1,
		FilterPage = 2,
		Grid = 3,
		BalanceJournal = 4,
		EditCampaign = 100,
		SelectForCommon = 200,
		SelectForStudioOrder = 210,
		SelectMMForPMCampaign = 220
	}

	public static class Constants
	{
		public struct Parameters
		{
			public const string ParentId = "parentId";
			public const string ChildId = "childId";

			public const string Entity = "entity";
			public const string InterfaceObject = "interfaceObject";
			public const string Action = "action";
			public const string Id = "id";
			public const string Name = "name";
			public const string ParentName = "parent_name";
		}

		public struct EntityActions
		{
			public const string Refresh = "RefreshItem";
			public const string Delete = "DeleteItem";
			public const string AddNew = "AddItem";
			public const string ShowPassport = "Properties";
			public const string ShowFilters = "ShowFilters";
			public const string Edit = "Edit";
			public const string Clone = "Clone";
			public const string Transfer = "Transfer";

			public const string AssignNew = "AssignNew";
			public const string AssignExisting = "AssignExisting";
			public const string Detach = "Detach";
		}

		public struct ActionsImages
		{
			public const string Refresh = "Icons.RefreshItem.png";
			public const string Add = "Icons.AddItem.png";
			public const string Delete = "Icons.DeleteItem.png";
			public const string Properties = "Icons.Properties.png";
			public const string Play = "Icons.Play.png";
			public const string Stop = "Icons.Stop.png";
			public const string Save = "Icons.Save.png";
			public const string ExportExcel = "Icons.ExportExcel.png";
			public const string User = "Icons.User.png";
			public const string Module = "Icons.Module.png";
			public const string PackModule = "Icons.PackModule.png";
			public const string Issue = "Icons.Issue.png";
			public const string SponsorProgram = "Icons.SponsorProgram.png";
			public const string Day = "Icons.Day.png";
			public const string Filter = "Icons.Filter.png";
			public const string Firm = "Icons.Firm.png";
		}

		public struct Actions
		{
			public const string Clone = "Clone";
			public const string Load = "Load";
			public const string LoadIssues = "LoadIssues";
			public const string LoadAgencies = "LoadAgencies";
			public const string LoadForSelection = "LoadForSelection";
			public const string AddItem = "AddItem";
			public const string Update = "UpdateItem";
			public const string Delete = "DeleteItem";
			public const string Detach = "DetachItem";
			public const string Attach = "AttachItem";
			public const string LoadNo = "LoadNo";
			public const string Recalculate = "Recalculate";
			public const string Activate = "Activate";
			public const string Deactivate = "Deactivate";
			public const string Transfer = "Transfer";
			public const string Substitute = "Substitute";
			public const string Generate = "Generate";
			public const string SetFinalPrice = "SetFinalPrice";
			public const string PlayRoller = "PlayRoller";
            public const string ChangePositions = "ChangePositions";
        }

		public struct TableNames
		{
			public const string Data = "data";
            public const string WindowsWithThisFirmIssue = "windowsWithThisFirm"; 
        }

		public struct ParamNames
		{
			public const string EntityId = "entityid";
			public const string ModuleId = "sys_moduleid";
			public const string ActionName = "actionname";
		}

		internal struct ItemsCountTemplates
		{
			internal const string Default = "Всего объектов: {0}";
			internal const string WithObjectType = "Всего объектов типа '{0}': {1}";

			internal const string WithObjectTypeAndParentObjectName =
				"Всего объектов типа '{0}' для объекта '{1}': {2}";
		}

		public const string MethodNotImplemented = "Метод еще не реализован.";
	}
}