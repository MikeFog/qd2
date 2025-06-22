using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal class CampaignModuleDay : CampaignDay
	{
		public CampaignModuleDay()
			: base(EntityManager.GetEntity((int) Entities.CampaignModuleDay))
		{
		}
	}
}