using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal class ModuleCampaignDay : CampaignDay
	{
		public ModuleCampaignDay() : base(EntityManager.GetEntity((int) Entities.ModuleCampaignDay))
		{
		}
	}
}