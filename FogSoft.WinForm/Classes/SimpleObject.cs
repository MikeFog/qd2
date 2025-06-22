using System.Collections.Generic;
using System.Data;

namespace FogSoft.WinForm.Classes
{
	public class SimpleObjectEntity : Entity
	{
		private static int id;

		private static int ID
		{
			get { return ++id; }
		}

		private bool hasAttributes;

		public override PresentationObject CreateObject(Dictionary<string, object> parameters)
		{
			return Init(base.CreateObject(parameters));
		}

		public override PresentationObject CreateObject(DataRow row)
		{
			return Init(base.CreateObject(row));
		}

		public override DataTable GetContent(Dictionary<string, object> filterValues)
		{
			DataTable table = base.GetContent(filterValues);
			// fill attributes from underlying datatable when attributes is missing
			if(!hasAttributes)
			{
				string dataType, name;
				if(sortedAttributes.ContainsKey(AttributeSelector))
					SortedAttributes.Clear();
				else
					sortedAttributes.Add(AttributeSelector, new List<Attribute>());
				// format column as MONEY, when column name starts with '$'
				foreach(DataColumn column in table.Columns)
				{
					if(PKColumns.Length > 0 && PKColumns[0].Equals(column.ColumnName)) continue;
					name = column.ColumnName;
					if(name.StartsWith("$"))
					{
						dataType = "money";
						name = column.ColumnName.Substring(1);
					}
					else
					{
						dataType = column.DataType.ToString();
					}
					SortedAttributes.Add(new Attribute(column.ColumnName, name, dataType));
				}
			}
			return table;
		}

		private PresentationObject Init(PresentationObject presentationObject)
		{
			Dictionary<string, object> parameters = presentationObject.Parameters;
			foreach(string name in PKColumns)
				if(!parameters.ContainsKey(name))
					parameters.Add(name, ID);
			presentationObject.Parameters = parameters;
			return presentationObject;
		}

		internal override DataTable LoadSingleObject(PresentationObject presentationObject)
		{
			presentationObject = Init(presentationObject);
			DataTable table = new DataTable(Name);

			foreach(string name in presentationObject.Parameters.Keys)
				table.Columns.Add(name, presentationObject.Parameters[name].GetType());

			DataRow row = table.NewRow();
			foreach(string name in presentationObject.Parameters.Keys)
				row[name] = presentationObject.Parameters[name];

			table.Rows.Add(row);
			return table;
		}

		public SimpleObjectEntity(
			DataRow entityRow, DataRow[] dtTableInfo, DataRow[] dtAttribute, DataRow[] dtAction)
			: base(entityRow, dtTableInfo, dtAttribute, dtAction)
		{
			hasAttributes = sortedAttributes.Count > 0;
		}
	}
}