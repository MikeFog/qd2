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

		/// <summary>
		/// Записывает в базу то, что накопил <see cref="SetChildrenChanges"/>.
		/// По умолчанию не делает ничего: связь «родитель-ребёнок» у каждого
		/// класса своя, и знает о ней только он сам.
		///
		/// Существует ради веба. В десктопе накопленное разбирает
		/// <c>Update()</c> доменного класса, но у части классов этот override
		/// оказался в UI-половине (разрез этапа 0), и в сборку без UI не
		/// попадает — тогда изменения набора молча терялись бы. Веб вызывает
		/// этот метод сам, сразу после <c>Update()</c>; десктоп продолжает
		/// звать его из своего <c>Update()</c>, то есть порядок действий
		/// одинаковый. Повторный вызов безвреден: список очищается.
		/// См. docs/tasks/web-migration.md, этап 2.
		/// </summary>
		public virtual void SubmitChildrenChanges()
		{
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

		/// <summary>
		/// Первая половина AssignNew: новый дочерний объект, уже привязанный к
		/// этому контейнеру — ключи родителя и его имя. Без этой привязки
		/// процедура сохранения не узнает, к кому относится запись.
		///
		/// Вынесено из UI-половины ради веба: там карточку показывает не
		/// ShowPassport, а веб-диалог, но подготовка объекта обязана быть той же.
		/// </summary>
		/// <returns>null, если у контейнера нет дочерней сущности.</returns>
		public PresentationObject CreateNewChild()
		{
			if (iterator.ChildEntity == null)
				return null;

			PresentationObject newObject = iterator.ChildEntity.NewObject;

			for(int i = 0; i < entity.PKColumns.Length; i++)
				newObject[entity.PKColumns[i]] = parameters[entity.PKColumns[i]];

			newObject[Constants.Parameters.ParentName] = Name;
			return newObject;
		}

		/// <summary>
		/// Вторая половина AssignNew — после того, как карточка нового объекта
		/// сохранена: передать ему сценарий и отбор, перечитать и сообщить
		/// подписчикам.
		/// </summary>
		public void CompleteNewChild(PresentationObject newObject)
		{
			if (newObject is IObjectContainer objectContainer)
			{
				objectContainer.RelationScenario = iterator.RelationScenario;
				objectContainer.Filter = ObjectsIterator.CacheFilterValues(iterator.Filter);
			}
			newObject.Refresh();
			OnObjectCreated(newObject);
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