using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Remoting;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Classes
{
    public class Entity : ObjectsIterator, IObjectContainer, ICloneable
    {
		private struct ClassNames
		{
			public const string PresentationObject = "FogSoft.WinForm.Classes.PresentationObject";
			public const string Container = "FogSoft.WinForm.Classes.ObjectContainer";
		}

		public struct ParamNames
		{
			public const string NAME = "name";
			public const string ALIAS = "alias";
			public const string DATA_TYPE = "dataType";
			public const string IS_ACTION_ENABLED = "isActionEnabled";
			public const string IMG_RESOURCE_NAME = "imgResourceName";
		}

		#region Internal classes ------------------------------

		public class Action
		{
			public readonly string Name;
			public readonly string Alias;
			public readonly bool IsEnabled;
			public string EntityID = "";
			public string EntityActionID = "";
			public string ImgResourceName { get; private set;}
			public readonly IList<Action> ChildActions = new List<Action>();

			public Action(string name, string alias)
			{
				Name = name;
				Alias = alias;
				IsEnabled = true;
			}

			public Action(string name, string alias, string imgresourcename)
			{
				Name = name;
				Alias = alias;
				IsEnabled = true;
				ImgResourceName = imgresourcename;
			}

			internal Action(DataRow row)
			{
				Name = row[ParamNames.NAME].ToString();
				Alias = row[ParamNames.ALIAS].ToString();

				EntityID = row[Constants.ParamNames.EntityId].ToString();

				if (row.Table.Columns.Contains("EntityActionID"))
					EntityActionID = row["EntityActionID"].ToString();

				IsEnabled = ParseHelper.ParseToBoolean(row[ParamNames.IS_ACTION_ENABLED].ToString(), false);

				ImgResourceName = row[ParamNames.IMG_RESOURCE_NAME].ToString();
			}

			internal void ProcessChildren(DataRow[] rows)
			{
				int actionId = int.Parse(EntityActionID);

                foreach (DataRow row in rows)
				{
                    if (row["ParentID"] != DBNull.Value && int.Parse(row["ParentID"].ToString()) == actionId)
					{
						ChildActions.Add(new Action(row));
					}
				}	
            }

			internal bool HasChildren
			{
				get { return ChildActions.Count > 0; }
			}
		}

		public class Attribute : ICloneable
		{
			public readonly string Name;
			public readonly string Alias;
			//public readonly bool IsEditable;
			public readonly string DataType;

			internal Attribute(DataRow row)
			{
				Name = row[ParamNames.NAME].ToString();
				Alias = row[ParamNames.ALIAS].ToString();
				DataType = row[ParamNames.DATA_TYPE].ToString();
				//this.IsEditable = node.Attributes[Names.IS_EDITABLE].Value == "1";
			}

			public Attribute(string name, string alias, string dataType)
			{
				Name = name;
				Alias = alias;
				DataType = dataType;
			}

			public object Clone()
			{
				return MemberwiseClone();
			}
		}
		#endregion

		#region Members ---------------------------------------

		public readonly int Id;
		public readonly string IconName;
		public readonly string XmlPassport;
		public readonly string XmlFilter;
		public readonly string Name;
		public readonly string CodeName;
		public readonly string[] PKColumns;
		public readonly object ParentId;

		private Action[] actionList;
		private readonly Dictionary<string, Action> colActions = new Dictionary<string, Action>();

		public readonly Dictionary<string, ColumnInfo> ColumnsInfo =
			new Dictionary<string, ColumnInfo>(StringComparer.InvariantCultureIgnoreCase);

		// This collection contains other collections with attributes,
		// one collection for each selector
		protected Dictionary<int, List<Attribute>> sortedAttributes =
			new Dictionary<int, List<Attribute>>();

		private readonly string className;
		private readonly string assemblyName;
		private int attributeSelector = 0;

		#endregion

		#region Constructors ----------------------------------

		public Entity(DataRow entityRow, DataRow[] dtTableInfo, DataRow[] dtAttribute, DataRow[] dtAction)
		{
			ChildEntity = this;
			Id = ParseHelper.ParseToInt32(entityRow[Constants.ParamNames.EntityId].ToString());
			XmlPassport = entityRow["passport"].ToString();
			XmlFilter = entityRow["filter"].ToString();

			className = entityRow["className"].ToString();
			assemblyName = entityRow["assemblyName"].ToString();
			Name = entityRow["name"].ToString();
			CodeName = entityRow["codeName"].ToString();
			PKColumns = entityRow["pkColumn"].ToString().Split(',');
			if(PKColumns != null)
			{
				for(int i = 0; i < PKColumns.Length; i++)
					PKColumns[i] = PKColumns[i].Trim();
			}

			IconName = entityRow["iconName"].ToString();
			ParentId = entityRow["parentId"] == DBNull.Value ? null : entityRow["parentId"];

			ProcessColumnsInfo(dtTableInfo);
			ProcessAttributes(dtAttribute);
			ProcessActions(dtAction);
		}

		private Entity(Entity baseEntity)
		{
			ChildEntity = baseEntity.ChildEntity;
			ChildEntity = this;
			Id = baseEntity.Id;
			XmlPassport = baseEntity.XmlPassport;
			XmlFilter = baseEntity.XmlFilter;

			className = baseEntity.className;
			assemblyName = baseEntity.assemblyName;
			Name = baseEntity.Name;
			CodeName = baseEntity.CodeName;
			PKColumns = baseEntity.PKColumns;
			ColumnsInfo = baseEntity.ColumnsInfo;
			ParentId = baseEntity.ParentId;

			IconName = baseEntity.IconName;

			sortedAttributes = new Dictionary<int, List<Attribute>>();
			foreach(KeyValuePair<int, List<Attribute>> kvp in baseEntity.sortedAttributes)
			{
				List<Attribute> clonedAttributes = new List<Attribute>();
				foreach(Attribute attr in kvp.Value)
					clonedAttributes.Add(attr.Clone() as Attribute);
				sortedAttributes.Add(kvp.Key, clonedAttributes);
			}
			actionList = baseEntity.actionList;
			colActions = baseEntity.colActions;
		}

		#endregion

		public bool IsFilterable
		{
			get { return XmlFilter.Length > 0; }
		}

		public Action[] ActionList
		{
			get { return actionList; }
		}

		public bool HasPassport
		{
			get { return XmlPassport.Length > 0; }
		}

		public bool  IsRefreshEnabled()
		{
            colActions.TryGetValue("Refresh", out Action action);
            if (action != null) 
				return action.IsEnabled;
			return DataAccessor.IsProcedureExist(DataAccessor.PrepareParameters(this));
		}

		public bool IsActionEnabled(string actionName, ViewType type)
		{
            colActions.TryGetValue(actionName, out Action action);
            return (action != null) && action.IsEnabled;
		}

		public int AttributeSelector
		{
			set { attributeSelector = value; }
			get { return attributeSelector; }
		}

		public List<Attribute> SortedAttributes
		{
			get { return sortedAttributes[attributeSelector]; }
		}

		public PresentationObject NewObject
		{
			get
			{
				if(className == ClassNames.PresentationObject)
					return new PresentationObject(this);
				if(className == ClassNames.Container)
					return new ObjectContainer(this);
				if(assemblyName == String.Empty)
					return Activator.CreateInstance(Type.GetType(className)) as PresentationObject;

				ObjectHandle objHandler = Activator.CreateInstance(assemblyName, className);
				return objHandler.Unwrap() as PresentationObject;
			}
		}

		/// <summary>
		/// Creates instance of the object from data row
		/// </summary>
		/// <param name="row"></param>
		/// <returns>Created object</returns>
		public virtual PresentationObject CreateObject(DataRow row)
		{
			if(className == ClassNames.PresentationObject)
				return new PresentationObject(this, row);
			if(className == ClassNames.Container)
				return new ObjectContainer(this, row);
			if(assemblyName == String.Empty)
				return Activator.
				       	CreateInstance(Type.GetType(className), new object[] {this, row}) as PresentationObject;

			ObjectHandle objHandler = Activator.CreateInstance(assemblyName, className);
			PresentationObject presentationObject = (PresentationObject)objHandler.Unwrap();

			presentationObject.Init(row);
			return presentationObject;
		}

		public virtual PresentationObject CreateObject(Dictionary<string, object> parameters)
		{
			PresentationObject presentationObject;

			if(className == ClassNames.PresentationObject)
				presentationObject = new PresentationObject(this);
			else if(className == ClassNames.Container)
				presentationObject = new ObjectContainer(this);
			else if(assemblyName == String.Empty)
				presentationObject = Activator.CreateInstance(Type.GetType(className), new object[] {this}) as PresentationObject;
			else
			{
				ObjectHandle objHandler = Activator.CreateInstance(assemblyName, className);
				presentationObject = (PresentationObject)objHandler.Unwrap();
			}

			presentationObject.Parameters = parameters;
			return presentationObject;
		}

		public bool IsChildNodeExpandable
		{
			get { return true; }
		}

		string IObjectContainer.Name
		{
			get { return Name; }
		}

		public override bool Equals(object obj)
		{
			if(obj is Entity)
				return GetHashCode() == obj.GetHashCode();
			return false;
		}

		public override int GetHashCode()
		{
			return Id;
		}

		internal virtual DataTable LoadSingleObject(PresentationObject presentationObject)
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(this);
			foreach(string columnName in PKColumns)
				procParameters[columnName] = presentationObject[columnName];
			DataSet ds = (DataSet)DataAccessor.DoAction(procParameters);
			return ds.Tables[Constants.TableNames.Data];
		}

		private void ProcessActions(DataRow[] rows)
		{
			actionList = new Action[GetVisibleAndRootActionsCount(rows)];
			int i = 0;
			foreach (DataRow row in rows)
			{
				Action action = new Action(row);
				if (!ParseHelper.ParseToBoolean(row["isHidden"].ToString()) && row["ParentID"] == DBNull.Value)
				{
					action.ProcessChildren(rows);
					actionList[i++] = action;
				}
				colActions[action.Name] = action;
			}
		}

		private int GetVisibleAndRootActionsCount(DataRow[] rows)
		{
			int count = 0;
			foreach (DataRow row in rows)
			{
				if (!ParseHelper.GetBooleanFromObject(row["isHidden"], true) && row["ParentID"] == DBNull.Value) 
					count++;
			}
			return count;
		}

		private void ProcessAttributes(DataRow[] rows)
		{
			foreach (DataRow row in rows)
			{
				if(!sortedAttributes.ContainsKey(GetSelectorValue(row)))
					sortedAttributes.Add(GetSelectorValue(row), new List<Attribute>());

				sortedAttributes[GetSelectorValue(row)].Add(new Attribute(row));
			}
		}

		private int GetSelectorValue(DataRow row)
		{
			return ParseHelper.ParseToInt32(row["selector"].ToString());
		}

		private void ProcessColumnsInfo(DataRow[] rows)
		{
			foreach (DataRow row in rows)
				ColumnsInfo[row["column_name"].ToString()] = new ColumnInfo(row);
		}

		public object Clone()
		{
			return new Entity(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Dictionary<string, string> GetColumnsForHighlight()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (sortedAttributes.ContainsKey(attributeSelector))
			{
				foreach (Attribute attribute in SortedAttributes)
				{
					if ((new List<string> {"money", "float", "datetime", "real", "smallmoney", "numeric", "int"})
						.Contains(attribute.DataType.ToLower()))
					{
						dictionary.Add(attribute.Name, attribute.Alias);
					}
				}
			}
			return dictionary;
		}
    }
}