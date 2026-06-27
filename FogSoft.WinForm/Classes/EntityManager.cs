using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Classes
{
	public static class EntityManager
	{
		private static readonly Dictionary<int, Entity> entitiesById = new Dictionary<int, Entity>();

		private static readonly Dictionary<string, Entity> entitiesByName =
			new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);

		#region Constructors ----------------------------------

		#endregion

		public static Entity GetEntity(int entityId)
		{
			if (!entitiesById.ContainsKey(entityId) || ConfigurationUtil.IsTestMode)
				LoadEntity(entityId);
			Entity entity = entitiesById[entityId];
			entity.AttributeSelector = 0;
			return entity;
		}

        public static Entity GetEntity(string entityName)
		{
			if(!entitiesByName.ContainsKey(entityName))
				LoadEntity(entityName);

			return entitiesByName[entityName];
		}

		private static void LoadEntity(int entityId)
		{
			Dictionary<string, object> parameters =
				new Dictionary<string, object>(2, StringComparer.InvariantCultureIgnoreCase)
					{
						{Constants.ParamNames.EntityId, entityId.ToString()},
						{SecurityManager.ParamNames.UserId, SecurityManager.LoggedUser.Id}
					};
			LoadEntity(parameters);
		}

		private static void LoadEntity(string entityName)
		{
			Dictionary<string, object> parameters =
				new Dictionary<string, object>(2, StringComparer.InvariantCultureIgnoreCase)
					{
						{"entityName", entityName},
						{SecurityManager.ParamNames.UserId, SecurityManager.LoggedUser.Id}
					};
			LoadEntity(parameters);
		}

		public static void ClearHash()
		{
			entitiesByName.Clear();
			entitiesById.Clear();
		}

		/// <summary>
		/// Creates an in-memory entity definition that is not loaded from DB metadata.
		/// Use this for read-only DataTable presentation scenarios (e.g. technical journals),
		/// where SmartGrid/JournalForm requires Entity metadata to render columns.
		/// </summary>
		public static Entity CreateVirtualEntity(
			int entityId, string entityName, string codeName, string pkColumn, params Entity.Attribute[] attributes)
		{
			return CreateVirtualEntity(entityId, entityName, codeName, pkColumn, string.Empty, attributes);
		}

		public static Entity CreateVirtualEntity(
			int entityId, string entityName, string codeName, string pkColumn, string iconName, params Entity.Attribute[] attributes)
		{
			DataTable dtEntity = new DataTable();
			dtEntity.Columns.Add(Constants.ParamNames.EntityId, typeof(int));
			dtEntity.Columns.Add("passport", typeof(string));
			dtEntity.Columns.Add("filter", typeof(string));
			dtEntity.Columns.Add("className", typeof(string));
			dtEntity.Columns.Add("assemblyName", typeof(string));
			dtEntity.Columns.Add("name", typeof(string));
			dtEntity.Columns.Add("codeName", typeof(string));
			dtEntity.Columns.Add("pkColumn", typeof(string));
			dtEntity.Columns.Add("iconName", typeof(string));
			dtEntity.Columns.Add("parentId", typeof(object));

			DataRow entityRow = dtEntity.NewRow();
			entityRow[Constants.ParamNames.EntityId] = entityId;
			entityRow["passport"] = string.Empty;
			entityRow["filter"] = string.Empty;
			entityRow["className"] = typeof(PresentationObject).FullName;
			entityRow["assemblyName"] = string.Empty;
			entityRow["name"] = entityName;
			entityRow["codeName"] = codeName;
			entityRow["pkColumn"] = pkColumn;
			entityRow["iconName"] = iconName;
			entityRow["parentId"] = DBNull.Value;
			dtEntity.Rows.Add(entityRow);

			DataTable dtAttributes = new DataTable();
			dtAttributes.Columns.Add(Entity.ParamNames.NAME, typeof(string));
			dtAttributes.Columns.Add(Entity.ParamNames.ALIAS, typeof(string));
			dtAttributes.Columns.Add(Entity.ParamNames.DATA_TYPE, typeof(string));
			dtAttributes.Columns.Add("selector", typeof(int));
			foreach (Entity.Attribute attribute in attributes)
			{
				DataRow row = dtAttributes.NewRow();
				row[Entity.ParamNames.NAME] = attribute.Name;
				row[Entity.ParamNames.ALIAS] = attribute.Alias;
				row[Entity.ParamNames.DATA_TYPE] = attribute.DataType;
				row["selector"] = 0;
				dtAttributes.Rows.Add(row);
			}

			DataTable dtActions = new DataTable();
			dtActions.Columns.Add(Entity.ParamNames.NAME, typeof(string));
			dtActions.Columns.Add(Entity.ParamNames.ALIAS, typeof(string));
			dtActions.Columns.Add(Entity.ParamNames.IS_ACTION_ENABLED, typeof(bool));
			dtActions.Columns.Add(Entity.ParamNames.IMG_RESOURCE_NAME, typeof(string));
			dtActions.Columns.Add(Constants.ParamNames.EntityId, typeof(int));
			dtActions.Columns.Add("isHidden", typeof(bool));
			dtActions.Columns.Add("ParentID", typeof(object));

			DataRow refreshAction = dtActions.NewRow();
			refreshAction[Entity.ParamNames.NAME] = Constants.EntityActions.Refresh;
			refreshAction[Entity.ParamNames.ALIAS] = "Refresh";
			refreshAction[Entity.ParamNames.IS_ACTION_ENABLED] = false;
			refreshAction[Entity.ParamNames.IMG_RESOURCE_NAME] = string.Empty;
			refreshAction[Constants.ParamNames.EntityId] = entityId;
			refreshAction["isHidden"] = true;
			refreshAction["ParentID"] = DBNull.Value;
			dtActions.Rows.Add(refreshAction);

			return new Entity(
				dtEntity.Rows[0],
				new DataRow[0],
				dtAttributes.Select(),
				dtActions.Select());
		}

		private static void LoadEntity(Dictionary<string, object> parameters)
		{
			DataRow row = null;
            DataRow[] dtAttribute;
            DataRow[] dtAction;
            DataRow[] dtTableInfo;
            if (ConfigurationUtil.IsFullLoadDictionaries)
            {
                if (_dsData == null)
                    FullLoadDictionaries();

                string filter = parameters.ContainsKey(Constants.ParamNames.EntityId)
                                    ? string.Format("entityID = {0}", parameters[Constants.ParamNames.EntityId])
                                    : string.Format("entityName = '{0}'", parameters["entityName"]);

                DataRow[] rows = _dsData.Tables[0].Select(filter);
                if (rows.Length > 0)
                    row = rows[0];
                dtAttribute = _dsData.Tables[1].Select(filter);
                dtAction = _dsData.Tables[2].Select(filter);
                dtTableInfo = _dsData.Tables[3].Select(filter);
            }
            else
            {
                DataSet ds = DataAccessor.LoadDataSet("EntityInfoRetrieve", parameters);
                DataTable dtEntity = ds.Tables[0];
                dtAttribute = ds.Tables[1].Select();
                dtAction = ds.Tables[2].Select();
                dtTableInfo = ds.Tables[3].Select();
                row = dtEntity.Rows[0];
            }
            Entity entity = null;
			if (row != null)
			{
				if (row.IsNull("entityClassName"))
					entity = new Entity(row, dtTableInfo, dtAttribute, dtAction);
				else
				{
					Assembly asm = Assembly.Load(row["assemblyName"].ToString());
					Type tp = asm.GetType(row["entityClassName"].ToString());

					ConstructorInfo ci =
						tp.GetConstructor(
							new[] { typeof(DataRow), typeof(DataRow[]), typeof(DataRow[]), typeof(DataRow[]) });
					entity = (Entity) ci.Invoke(new object[] {row, dtTableInfo, dtAttribute, dtAction});
				}
			}
			if (entity != null)
			{
				entitiesById[entity.Id] = entity;
				entitiesByName[entity.CodeName] = entity;
			}
		}

		#region Full Load

		private static DataSet _dsData = null;

		public static void FullLoadDictionaries()
		{
			Dictionary<string, object> parameters =
				new Dictionary<string, object>(2, StringComparer.InvariantCultureIgnoreCase)
					{
						{SecurityManager.ParamNames.UserId, SecurityManager.LoggedUser.Id}
					};

			_dsData = DataAccessor.LoadDataSet("EntityInfoRetrieve", parameters);
		}

		#endregion

	}
}