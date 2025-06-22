using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Forms
{
	public class StatBalanceJournalForm : JournalForm
	{
		public StatBalanceJournalForm(Entity entity, string caption) : base(entity, caption)
		{
		}

		protected override void LoadData(object stateInfo)
		{
			if (Filters.ContainsKey("IsGroupByAgency") && bool.Parse(Filters["IsGroupByAgency"].ToString()))
				Grid.Entity = EntityManager.GetEntity((int) Entities.StatsBalanceGroup);
			else
				Grid.Entity = EntityManager.GetEntity((int) Entities.StatsBalance);

			base.LoadData(stateInfo);
		}
	}
}