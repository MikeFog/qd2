using System;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.FakeContainers
{
	partial class MassmediasContainer : FakeContainer
	{
		protected MassmediasContainer(Entity.Action[] actions) : base(actions)
		{
			Globals.ResolveFilterInitialValues(_filter, RootEntity.XmlFilter);
		}

		public MassmediasContainer(string name, Entity.Action[] actions, RelationScenario relationScenario) : base(name, actions, relationScenario)
		{
			Globals.ResolveFilterInitialValues(_filter, RootEntity.XmlFilter);
		}

		public override bool IsFilterable
		{
			get
			{
				return true;
			}
		}

		// ShowFilter переехал в MassmediasContainer.WinForms.cs.

		public Entity RootEntity
		{
			get
			{
				return EntityManager.GetEntity((int)Entities.MassMedia);
			}
		}
	}
}
