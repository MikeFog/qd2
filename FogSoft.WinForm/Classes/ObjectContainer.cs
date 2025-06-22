using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Passport.Forms;

namespace FogSoft.WinForm.Classes
{
	public class ObjectContainer : PresentationObject, IObjectContainer, IParentObject,
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
		}

        public ObjectContainer(Entity entity, Dictionary<string, object> parameters)
            : base(entity, parameters)
        {
            iterator.LoadContent = GetContent;
        }

		public ObjectContainer(Entity entity)
			: base(entity)
		{
			iterator.LoadContent = GetContent;
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

		// Запоминает последний нефильтрованный список - сделано для работы дерева с гридом - чтобы по два раза не грузилось одно и тоже
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

		public override void DoAction(
			string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.EntityActions.AssignNew:
					AssignNew(owner);
					break;

				case Constants.EntityActions.AssignExisting:
					AssignExisting(owner);
					break;

				case Constants.EntityActions.Refresh:
					Refresh(interfaceObject);
					ClearCache();
					iterator.ClearCache();
					FireContainerRefreshed();
					break;

				case Constants.EntityActions.ShowFilters:
					ShowFilter(owner);
					break;

				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			bool res = base.IsActionEnabled(actionName, type);
			if (string.Compare(actionName, Constants.EntityActions.AssignNew) == 0
				|| string.Compare(actionName, Constants.EntityActions.AssignExisting) == 0)
				res = res && iterator.ChildEntity != null;
			return res;
		}

		protected virtual void AssignExisting(IWin32Window owner)
		{
			if (iterator.ChildEntity == null)
				return;

			SelectionForm selector = new SelectionForm(iterator.ChildEntity, "Выбрать объект");
			if(selector.ShowDialog(owner) == DialogResult.OK)
			{
				PresentationObject presentationObject = selector.SelectedObject;
				for(int i = 0; i < iterator.ChildEntity.PKColumns.Length; i++)
					presentationObject[iterator.ChildEntity.PKColumns[i]] =
						parameters[iterator.ChildEntity.PKColumns[i]];
				OnObjectCreated(selector.SelectedObject);
			}
		}

		protected void FireContainerRefreshed()
		{
            ContainerRefreshed?.Invoke(this);
        }

		/// <summary>
		/// Creates object of given entity and assignes it to the current oject
		protected virtual void AssignNew(IWin32Window owner)
		{
			if (iterator.ChildEntity == null)
				return;

			PresentationObject newObject = iterator.ChildEntity.NewObject;

			for(int i = 0; i < entity.PKColumns.Length; i++)
				newObject[entity.PKColumns[i]] = parameters[entity.PKColumns[i]];

			newObject[Constants.Parameters.ParentName] = Name;

			if(newObject.ShowPassport(owner))
			{
                if (newObject is IObjectContainer objectContainer)
                    objectContainer.RelationScenario = iterator.RelationScenario;
                newObject.Refresh();
				OnObjectCreated(newObject);
			}
		}

		// Shows filter form and fire ContainerRefreshed event 
		public void ShowFilter(IWin32Window owner)
		{
			// load data to display filter
			Dictionary<string, object> clonedParameters = Parameters;
			DataAccessor.PrepareParameters(clonedParameters, iterator.ChildEntity,
			                               InterfaceObjects.FilterPage, Constants.Actions.Load);

			DataSet ds = null;
			if(DataAccessor.IsProcedureExist(clonedParameters))
			{
				ds = DataAccessor.DoAction(clonedParameters) as DataSet;
			}

			//if(this.filter == null)	{
			//  this.filter = new Dictionary<string,object>(StringComparer.InvariantCultureIgnoreCase);
			//  Globals.ResolveFilterInitialValues(this.filter, this.iterator.ChildEntity.XmlFilter);
			//}

			//FrmFilter fFilter = new FrmFilter(this.iterator.ChildEntity, ds, filter);
			FilterForm fFilter = new FilterForm(iterator.ChildEntity, ds, null);
			if(fFilter.ShowDialog(owner) == DialogResult.OK)
				FireContainerRefreshed();
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
    }
}