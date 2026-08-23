using System;

namespace FogSoft.WinForm.Classes
{
	public partial class FakeContainer : ObjectsIterator, IObjectContainer, IVisualContainer
	{
		public event ObjectDelegate ObjectCreated;
		public event ContainerDelegate ContainerRefreshed;

		#region Members -----------------------------------------

		protected string name;
		private readonly Entity.Action[] actions;

		#endregion

		#region Constructors ------------------------------------

		protected FakeContainer(Entity.Action[] actions)
		{
			this.actions = actions;
		}

		public FakeContainer(string name, Entity.Action[] actions, RelationScenario relationScenario)
		{
			this.relationScenario = relationScenario;
			childEntity = EntityManager.GetEntity(relationScenario.StartingEntityID);
			this.name = name;
			this.actions = actions;
            Globals.ResolveFilterInitialValues(_filter, relationScenario.XmlFilter);
        }

		#endregion

		public bool IsChildNodeExpandable
		{
			get { return true; }
		}

		public string Name
		{
			get { return name; }
		}

		virtual public bool IsFilterable
		{
			get { return !string.IsNullOrEmpty(relationScenario.XmlFilter); }
		}

		protected virtual void FireContainerRefreshed()
		{
			ClearCache();
			if(ContainerRefreshed != null) ContainerRefreshed(this);
		}

		#region IActionHandler Members ------------------------

		public Entity.Action[] ActionList
		{
			get { return actions; }
		}

		public virtual bool IsActionEnabled(string actionName, ViewType type)
		{
			return true;
		}

		public virtual bool IsActionHidden(string actionName, ViewType type)
		{
			return false;
		}

		// DoAction/ShowFilter переехали в FakeContainer.WinForms.cs.

		#endregion


        protected Entity RootEntity
        {
            get
            {
                return relationScenario.StartingEntity;
            }
        }
    }
}