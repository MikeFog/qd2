using System.Windows.Forms;

namespace FogSoft.WinForm.Classes
{
	public class FakeContainer : ObjectsIterator, IObjectContainer, IActionHandler, IVisualContainer
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
			get { return false; }
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

		public virtual void DoAction(
			string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.EntityActions.AddNew:
					PresentationObject newObject = childEntity.NewObject;
					if(newObject.ShowPassport(owner) && ObjectCreated != null)
					{
						IObjectContainer oc = newObject as IObjectContainer;
						if(oc != null)
							oc.RelationScenario = relationScenario;
						ObjectCreated(newObject);
					}
					break;

				case Constants.EntityActions.Refresh:
					FireContainerRefreshed();
					break;
			}
		}

		#endregion

		public virtual void ShowFilter(IWin32Window owner)
		{			
		}
	}
}