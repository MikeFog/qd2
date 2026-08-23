namespace FogSoft.WinForm.Passport.Classes
{
	// Словарь имён атрибутов XML-паспорта и сокращений начальных значений фильтра.
	// Вынесено из PageControl.cs, чтобы эти константы были доступны коду без UI
	// (FogSoft.Core). Значения не менялись. См. docs/tasks/web-migration.md, этап 0.
	public abstract partial class PageControl
	{
		public struct Attributes
		{
			public const string Source = "source";
			public const string Caption = "caption";
			public const string Name = "name";
			public const string Entity = "entity";
			public const string DestName = "destination";
			public const string Multiselect = "multiselect";
			public const string RelationScenario = "relationScenario";
			public const string ObjectName = "objectName";
			//public const string ReadOnly = "readonly";
			public const string Filter = "filter";
			public const string Type = "type";
			public const string ColumnWithId = "columnWithID";
			public const string Value = "value";
			public const string Mandatory = "mandatory";
			public const string ParentLookupName = "parentLookupName";
			public const string IsCreateNewAllowed = "isCreateNewAllowed";
			public const string Locked = "locked"; // locked for all but Admin!
			public const string Disabled = "disabled";
			public const string Height = "height";
			public const string MinValue = "min";
			public const string MaxValue = "max";
			public const string PassportChar = "passportchar";
			public const string Show = "show";
			public const string DecimalPlaces = "decimalplaces";
			public const string NeedSavePict = "needsavepict";
			public const string Required = "required";
			public const string HashIt = "hashit";
			public const string Anchor = "anchor";
			public const string MaxLenght = "maxlenght";

			public const string ColumnName = "columnname";
			public const string ColumnParentid = "columnparentid";
			public const string ColumnId = "columnid";
			public const string IsMandatoryOnCreate = "isMandatoryOnCreate";

        }

		public struct InitialValueAbbreviations
		{
			public const string LAST_MONTH = "LAST_MONTH";
			public const string LAST_WEEK = "LAST_WEEK";
			public const string TODAY = "TODAY";
            public const string PREV_MONTH_BEGIN = "PREV_MONTH_BEGIN";
            public const string PREV_MONTH_END = "PREV_MONTH_END";
            public const string StartOfTheMonth = "StartOfTheMonth";
			public const string EndOfTheMonth = "EndOfTheMonth";
            public const string StartOfTheLastMonth = "StartOfTheLastMonth";
			public const string LoggedUser = "LoggedUser";
		}
	}
}
