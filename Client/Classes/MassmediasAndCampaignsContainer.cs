using System;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal partial class MassmediasAndCampaignsContainer : FakeContainer
	{
		public MassmediasAndCampaignsContainer()
			: base("Радиостанции", null, RelationManager.GetScenario(RelationScenarios.MassmediaAndCampaign))
		{
		}

		public override bool IsFilterable
		{
			get { return true; }
		}

		// ShowFilter переехал в MassmediasAndCampaignsContainer.WinForms.cs.
	}
}