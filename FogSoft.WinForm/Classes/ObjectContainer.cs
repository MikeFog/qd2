using System.Collections;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Classes
{
	// UI-часть (DoAction, AssignNew, AssignExisting, ShowFilter) вынесена
	// в ObjectContainer.WinForms.cs — см. docs/tasks/web-migration.md, этап 0.
	public partial class ObjectContainer : PresentationObject, IObjectContainer, IParentObject,
	                               IVisualContainer
	{
		protected class ChildrenChanges
		{
			public readonly Entity Entity;
			public readonly List<PresentationObject> AddedObjects;
			public readonly List<PresentationObject> DeletedObjects;

			public ChildrenChanges(
				Entity entity, List<PresentationObject> addedObjects,
				List<PresentationObject> deletedObjects)
			{
				Entity = entity;
				AddedObjects = addedObjects;
				DeletedObjects = deletedObjects;
			}
		}

		public event ContainerDelegate ContainerRefreshed;

		#region Members ---------------------------------------

		protected readonly ObjectsIterator iterator = new ObjectsIterator();
		protected List<ChildrenChanges> childrenChangesList = new List<ChildrenChanges>();
		private bool isChildNodeExpandable;

		#endregion

		#region Constructors ----------------------------------

		public ObjectContainer(Entity entity, DataRow row)
			: base(entity, row)
		{
			iterator.LoadContent = GetContent;
			iterator.ChildObjectPostProcessor = ProcessCreatedChildObject;
		}

        public ObjectContainer(Entity entity, Dictionary<string, object> parameters)
            : base(entity, parameters)
        {
            iterator.LoadContent = GetContent;
            iterator.ChildObjectPostProcessor = ProcessCreatedChildObject;
        }

		public ObjectContainer(Entity entity)
			: base(entity)
		{
			iterator.LoadContent = GetContent;
			iterator.ChildObjectPostProcessor = ProcessCreatedChildObject;
		}

		#endregion

		public IEnumerator<PresentationObject> GetEnumerator()
		{
			return iterator.GetEnumerator();
		}

		#region IObjectContainer Members ----------------------

		public virtual DataTable GetContent()
		{
			return GetContent(iterator.Filter);
		}

		public virtual DataTable GetContent(Dictionary<string, object> filterValues)
		{
			return GetContent(filterValues, true);
		}

		public virtual DataTable GetFilteredContent(Dictionary<string, object> filterValues)
		{
			return GetContent(filterValues, true);
		}

		// ?????????? ????????? ??????????????? ?????? - ??????? ??? ?????? ?????? ? ?????? - ????? ?? ??? ???? ?? ????????? ???? ? ????
		private DataTable lastContentFilter = null;
		private Dictionary<string, object> lastFilterValues = null;
		private bool? lastForceFilterUsage = null;

		public virtual DataTable GetContent(Dictionary<string, object> filterValues, bool forceFilterUsage)
		{
			if (!ConfigurationUtil.IsUseSimpleCache || lastContentFilter == null
				|| ObjectsIterator.IsNewFilter(filterValues, lastFilterValues) || !lastForceFilterUsage.HasValue || lastForceFilterUsage.Value != forceFilterUsage)
			{
				lastForceFilterUsage = forceFilterUsage;
				lastFilterValues = ObjectsIterator.CacheFilterValues(filterValues);

				Entity childEntity = iterator.ChildEntity;
				if (childEntity == null && iterator.RelationScenario != null)
					childEntity = iterator.RelationScenario.GetChildEntity(entity.Id).ChildEntity;

				Dictionary<string, object> procParameters = Parameters;
				DataAccessor.PrepareParameters(procParameters, childEntity, InterfaceObjects.SimpleJournal,
				                               Constants.Actions.Load);

				if (forceFilterUsage || (iterator.ChildEntity.IsFilterable && iterator.RelationScenario == null))
				{
					if (filterValues != null)
					{
						foreach (KeyValuePair<string, object> kvp in filterValues)
							procParameters[kvp.Key] = kvp.Value;
					}
				}
				lastContentFilter = ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data];
			}

			return lastContentFilter;
		}

		public virtual Entity ChildEntity
		{
			get { return iterator.ChildEntity; }
			set
			{
				iterator.ChildEntity = value;
				ClearCache();
			}
		}

		public void ClearCache()
		{
			lastContentFilter = null;
			lastFilterValues = null;
			lastForceFilterUsage = null;
			iterator?.ClearCache();
		}

		public virtual bool IsChildNodeExpandable
		{
			get { return isChildNodeExpandable; }
			set { isChildNodeExpandable = value; }
		}

		public virtual RelationScenario RelationScenario
		{
			get { return iterator.RelationScenario; }
			set
			{
				ClearCache();
				iterator.RelationScenario = value;
				if(iterator.RelationScenario != null)
				{
					RelationScenario.EntityRelation entityRelation =
						iterator.RelationScenario.GetChildEntity(entity.Id);
					if(entityRelation != null)
					{
						iterator.ChildEntity = entityRelation.ChildEntity;
						isChildNodeExpandable = entityRelation.IsChildNodeExpandable;
					}
				}
			}
		}

		public void SetChildrenChanges(
			Entity childEntity, List<PresentationObject> addedItems,
			List<PresentationObject> deletedItems)
		{
			childrenChangesList.Add(new ChildrenChanges(childEntity, addedItems, deletedItems));
		}

		#endregion

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			bool res = base.IsActionEnabled(actionName, type);
			if (string.Compare(actionName, Constants.EntityActions.AssignNew) == 0
				|| string.Compare(actionName, Constants.EntityActions.AssignExisting) == 0)
				res = res && iterator.ChildEntity != null;
			return res;
		}

		protected void FireContainerRefreshed()
		{
            ContainerRefreshed?.Invoke(this);
        }

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

        public Dictionary<string, object> Filter
        {
            get { return iterator.Filter; }
            set { iterator.Filter = value; }
        }

        // NEW: точка расширения для наследников контейнера
        protected virtual PresentationObject ProcessCreatedChildObject(PresentationObject childObject, DataRow row)
        {
            return childObject;
        }
    }
}