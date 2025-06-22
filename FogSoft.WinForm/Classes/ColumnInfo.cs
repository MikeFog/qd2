using System;
using System.Data;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;

namespace FogSoft.WinForm.Classes
{
	public class ColumnInfo
	{
		public struct UDTypes
		{
			public const string DoubleString = "doubleString";
			public const string TextArea = "TextArea";
			public const string Date = "DF_DATE";
			public const string Time = "DF_TIME";
			public const string DateTime = "DF_DATETIME";
			public const string TimeDuration = "timeDuration";
			public const string Int = "int";
			public const string Decimal = "decimal";
		}

		public readonly string ColumnName;
		public readonly object ColumnDefault;
		public readonly bool IsNullable;
		public readonly string DataType;
		public readonly int MaxLength;
		//TODO: Temporary solution. Change later
		public readonly decimal MaxValue = 100000;
		public readonly decimal MinValue = -100000;

		public ColumnInfo(DataRow row)
		{
			ColumnName = row["column_name"].ToString();
			DataType = row["data_type"].ToString();			
			IsNullable = row["is_nullable"].ToString().ToLower() == "yes";

			if(row["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
				MaxLength = ParseHelper.ParseToInt32(row["CHARACTER_MAXIMUM_LENGTH"].ToString(), 0);

			string defValue = ProcessDefaultValue(row["column_default"]);
			switch(DataType.ToLower())
			{
				case "tinyint":
					MinValue = SqlByte.MinValue.Value;
					MaxValue = SqlByte.MaxValue.Value;
					if (defValue != null) ColumnDefault = ParseHelper.ParseToInt32(defValue);
					break;
				case "smallint":
					MinValue = SqlInt16.MinValue.Value;
					MaxValue = SqlInt16.MaxValue.Value;
					if(defValue != null) ColumnDefault = ParseHelper.ParseToInt32(defValue);
					break;
				case "int":
				case "integer":
					MinValue = SqlInt32.MinValue.Value;
					MaxValue = SqlInt32.MaxValue.Value;
					if (defValue != null) ColumnDefault = ParseHelper.ParseToInt32(defValue);
					break;
				case "bigint":
					MinValue = SqlInt64.MinValue.Value;
					MaxValue = SqlInt64.MaxValue.Value;
					if (defValue != null) ColumnDefault = ParseHelper.ParseToInt32(defValue);
					break;
				case "decimal":
				case "numeric":
					MinValue = Decimal.MinValue;
					MaxValue = Decimal.MaxValue;
					if (defValue != null) ColumnDefault = ParseHelper.GetDecimalFromObject(defValue, 0);
					break;
				case "float":
				case "real":
					MinValue = Decimal.MinValue;
					MaxValue = Decimal.MaxValue;
					if (defValue != null) ColumnDefault = Single.Parse(defValue);
					break;
				case "smallmoney":
					MinValue = -214748.3648m;
					MaxValue = 214748.3647m;
					if (defValue != null) ColumnDefault = ParseHelper.GetDecimalFromObject(defValue, 0);
					break;
				case "money":
					MinValue = SqlMoney.MinValue.Value;
					MaxValue = SqlMoney.MaxValue.Value;
					if (defValue != null) ColumnDefault = ParseHelper.GetDecimalFromObject(defValue, 0);
					break;
				case "bit":
					if (defValue != null) ColumnDefault = ParseHelper.ParseToBoolean(defValue, false);
					break;
			}
		}

		private string ProcessDefaultValue(object defaultValue)
		{
			// Default value selected from database looks this way: (15)
			// We have to delete first and final bracket symbols
			if(defaultValue != DBNull.Value)
			{
				string value = RemoveSigns(defaultValue.ToString());

				if(DataType == SqlDbType.Bit.ToString().ToLower())
					value = value == "1" ? Boolean.TrueString : Boolean.FalseString;
				return value;
			}
			return null;
		}

		private static string RemoveSigns(string defaultValue)
		{
			if (Regex.IsMatch(defaultValue, @"^\(.*\)$"))
				return RemoveSigns(Regex.Replace(defaultValue, @"^\(|\)$", String.Empty));
			if (Regex.IsMatch(defaultValue, "^'.*'$"))
				return RemoveSigns(Regex.Replace(defaultValue, @"^'|'$", String.Empty));
			return defaultValue;
		}

		public static bool IsBooleanData(ColumnInfo columnInfo, Entity.Attribute attribute)
		{
			return IsSQLType(columnInfo, attribute, SqlDbType.Bit);
		}

		public static bool IsMoneyData(ColumnInfo columnInfo, Entity.Attribute attribute)
		{
			return IsSQLType(columnInfo, attribute, SqlDbType.Money);
		}

		public static bool IsFloatData(ColumnInfo columnInfo, Entity.Attribute attribute)
		{
			return IsSQLType(columnInfo, attribute, SqlDbType.Float);
		}

		public static bool IsSQLType(ColumnInfo columnInfo, Entity.Attribute attribute, SqlDbType type)
		{
			if (!String.IsNullOrEmpty(attribute.DataType) && attribute.DataType.ToLower() == type.ToString().ToLower()) return true;
			return columnInfo != null && !String.IsNullOrEmpty(columnInfo.DataType) && columnInfo.DataType.ToLower() == type.ToString().ToLower();
		}
	}
}