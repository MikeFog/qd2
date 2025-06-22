using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Classes
{
	public class ObjectsIterator : IEnumerable<PresentationObject>
	{
		protected RelationScenario relationScenario;
		protected Entity childEntity;
		protected Dictionary<string, object> _filter = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

		public DataLoadDelegate LoadContent;

		private DataTable lastContentFilter = null;
		private Dictionary<string, object> lastFilterValues = null;

		public virtual DataTable GetContent()
		{
			return GetContent(_filter);
		}		
				
		public virtual DataTable GetContent(Dictionary<string, object> filterValues)
		{
			if(LoadContent != null)
				return LoadContent();

			if (!ConfigurationUtil.IsUseSimpleCache || lastContentFilter == null || IsNewFilter(filterValues, lastFilterValues))
			{
				lastFilterValues = CacheFilterValues(filterValues);
				Dictionary<string, object> procParameters =	DataAccessor.PrepareParameters(childEntity);
				AppendFilterValues(procParameters, filterValues);
				lastContentFilter = ((DataSet) DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
			}

			return lastContentFilter;
		}

		public static bool IsNewFilter(IDictionary<string, object> newFilterValues, IDictionary<string, object> lastFilterValues)
		{
			if (newFilterValues == null && lastFilterValues == null)
				return false;

			if (newFilterValues == null || lastFilterValues == null)
				return true;

			foreach (KeyValuePair<string, object> pair in lastFilterValues)
				if (!newFilterValues.ContainsKey(pair.Key) || newFilterValues[pair.Key] != pair.Value)
					return true;
			foreach (KeyValuePair<string, object> pair in newFilterValues)
				if (!lastFilterValues.ContainsKey(pair.Key) || lastFilterValues[pair.Key] != pair.Value)
					return true;
			return false;
		}

		public static Dictionary<string, object> CacheFilterValues(IDictionary<string, object> newFilterValues)
		{
			if (newFilterValues == null)
				return null;
			Dictionary<string, object> filterValues = DataAccessor.CreateParametersDictionary();
			foreach (KeyValuePair<string, object> pair in newFilterValues)
			{
				if (filterValues.ContainsKey(pair.Key))
					filterValues[pair.Key] = pair.Value;
				else 
					filterValues.Add(pair.Key, pair.Value);
			}
			return filterValues;
		}

		private void AppendFilterValues(IDictionary<string, object> procParameters, 
			IEnumerable<KeyValuePair<string, object>> filterValues)
		{
			if(filterValues != null)
			{
				foreach(KeyValuePair<string, object> kvp in filterValues)
					procParameters[kvp.Key] = kvp.Value;
			}
		}

		public Entity ChildEntity
		{
			get { return childEntity; }
			set
			{
				childEntity = value;
				ClearCache();
				
				//if (value != null)
				//	Globals.ResolveFilterInitialValues(_filter, childEntity.XmlFilter);
			}
		}

		public void ResolveFilterInitialValues()
		{
            Globals.ResolveFilterInitialValues(_filter, childEntity.XmlFilter);
        }

        public RelationScenario RelationScenario
		{
			get { return relationScenario; }
			set
			{
				ClearCache();
				relationScenario = value;
			}
		}

		public void ClearCache()
		{
			lastContentFilter = null;
			lastFilterValues = null;
		}

		public IEnumerator<PresentationObject> GetEnumerator()
		{
			DataTable dtData = GetContent();

			for(int rowIndex = 0; rowIndex < dtData.Rows.Count; rowIndex++)
				yield return CreateChilObject(dtData.Rows[rowIndex]);
		}

		private PresentationObject CreateChilObject(DataRow row)
		{
			PresentationObject presentationObject = ResolveEntityForSelectedRow(row).CreateObject(row);
			IObjectContainer objectContainer = presentationObject as IObjectContainer;
			if(objectContainer != null)
				objectContainer.RelationScenario = relationScenario;

			return presentationObject;
		}

		private Entity ResolveEntityForSelectedRow(DataRow row)
		{
			if(row.Table.Columns.Contains(Constants.ParamNames.EntityId))
				return EntityManager.GetEntity(ParseHelper.ParseToInt32(row[Constants.ParamNames.EntityId].ToString()));
			return childEntity;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

        public Dictionary<string, object> Filter
		{
			get { return _filter; }
			set { _filter = value; }
		}
    }
}