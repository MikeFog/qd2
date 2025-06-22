using System.Data;

namespace FogSoft.WinForm.Classes
{
	public class FieldTypeResolver
	{
		public readonly bool IsString = false;
		public readonly bool IsDoubleString = false;
		public readonly bool IsTextArea = false;
		public readonly bool IsInteger = false;
		public readonly bool IsDecimal = false;
		public readonly bool IsBoolean = false;
		public readonly bool IsDate = false;
		public readonly bool IsDateTime = false;
		public readonly bool IsTime = false;
		public readonly bool IsTimeDuration = false;
		public readonly bool IsMoney = false;
 
        public FieldTypeResolver(string fieldTypeName, ColumnInfo columnInfo)
		{
			// Top priority is for type declared right in the Xml Document
			if(fieldTypeName != "")
			{
				string typeName = fieldTypeName.ToLower();

				if(typeName == ColumnInfo.UDTypes.Int)
				{
					IsInteger = true;
					return;
				}

				if(typeName == ColumnInfo.UDTypes.Decimal)
				{
					IsDecimal = true;
					return;
				}

				if(typeName == "password" || typeName == "string")
				{
					IsString = true;
					return;
				}

				if(typeName == "boolean")
				{
					IsBoolean = true;
					return;
				}

				if (typeName == ColumnInfo.UDTypes.Date.ToLower())
				{
					IsDate = true;
					return;
				}

				if(typeName == "money")
				{
					IsMoney = true;
					return;
				}

				if(typeName == "varchar")
				{
					IsString = true;
					return;
				}

				if(typeName == ColumnInfo.UDTypes.Time.ToLower())
				{
					IsTime = true;
					return;
				}

				if(typeName == ColumnInfo.UDTypes.DateTime.ToLower())
				{
					IsDateTime = true;
					return;
				}

				if (typeName == ColumnInfo.UDTypes.TimeDuration.ToLower())
				{
					IsTimeDuration = true;
					return;
				}
			}
			else if(columnInfo != null)
			{
				if(columnInfo.DataType == SqlDbType.VarChar.ToString().ToLower() ||
				   columnInfo.DataType == SqlDbType.NVarChar.ToString().ToLower() ||
				   columnInfo.DataType == SqlDbType.Char.ToString().ToLower())
				{
					IsString = true;
					return;
				}

				if(columnInfo.DataType == ColumnInfo.UDTypes.DoubleString)
				{
					IsDoubleString = true;
					return;
				}

				if(columnInfo.DataType == SqlDbType.Bit.ToString().ToLower())
				{
					IsBoolean = true;
					return;
				}

				if(columnInfo.DataType == SqlDbType.Int.ToString().ToLower() ||
				   columnInfo.DataType == SqlDbType.SmallInt.ToString().ToLower() ||
				   columnInfo.DataType == SqlDbType.TinyInt.ToString().ToLower() ||
				   columnInfo.DataType == ColumnInfo.UDTypes.Int)
				{
					IsInteger = true;
					return;
				}

				if(columnInfo.DataType == SqlDbType.Float.ToString().ToLower() ||
				   columnInfo.DataType == SqlDbType.Money.ToString().ToLower() ||
				   columnInfo.DataType == SqlDbType.Decimal.ToString().ToLower())
				{
					IsDecimal = true;
					return;
				}

				if(columnInfo.DataType == SqlDbType.NText.ToString().ToLower() ||
				   columnInfo.DataType == SqlDbType.Text.ToString().ToLower() ||
				   columnInfo.DataType == ColumnInfo.UDTypes.TextArea)
				{
					IsTextArea = true;
					return;
				}

				if(columnInfo.DataType == SqlDbType.DateTime.ToString().ToLower() ||
				   columnInfo.DataType == ColumnInfo.UDTypes.Date)
				{
					IsDate = true;
					return;
				}

				if(columnInfo.DataType == ColumnInfo.UDTypes.DateTime)
				{
					IsDateTime = true;
					return;
				}

				if(columnInfo.DataType == ColumnInfo.UDTypes.Time)
				{
					IsTime = true;
					return;
				}

				if(columnInfo.DataType == ColumnInfo.UDTypes.TimeDuration)
				{
					IsTimeDuration = true;
					return;
				}
			}
		}
	}
}