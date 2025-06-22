using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal class MassmediasAndCampaignsContainer : FakeContainer
	{
		public MassmediasAndCampaignsContainer()
			: base("Радиостанции", null, RelationManager.GetScenario(RelationScenarios.MassmediaAndCampaign))
		{
		}

		public override bool IsFilterable
		{
			get { return true; }
		}

		public override void ShowFilter(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				Entity entity = EntityManager.GetEntity((int)Entities.MassmediasWithCampaigns);

				if (Globals.ShowFilter(owner, entity, _filter))
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
	}
}