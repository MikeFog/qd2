using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public class EntityCampaign : Entity
	{
		public EntityCampaign(DataRow entityRow, DataRow[] dtTableInfo, DataRow[] dtAttribute, DataRow[] dtAction) :
			base(entityRow, dtTableInfo, dtAttribute, dtAction)
		{
		}

		public override bool Equals(object obj)
		{
			Entity entity = obj as Entity;
			if (entity == null)
				return base.Equals(obj);
			return entity.Id == (int) Entities.CampaignOnMassmedia || entity.Id == (int) Entities.GeneralCampaign ||
			       entity.Id == (int) Entities.SponsorCampaign || entity.Id == (int) Entities.ModuleCampaign;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
    }
}