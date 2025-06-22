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