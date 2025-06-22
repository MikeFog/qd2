using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;

namespace Merlin.Classes.FakeContainers
{
	class MassmediasContainer : FakeContainer
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

		public override void ShowFilter(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				FilterForm frm = new FilterForm(RootEntity, Globals.PrepareForFilter(RootEntity), _filter);

				if (frm.ShowDialog(owner) == DialogResult.OK)
					FireContainerRefreshed();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		public Entity RootEntity
		{
			get
			{
				return EntityManager.GetEntity((int)Entities.MassMedia);
			}
		}
	}
}
