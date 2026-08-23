using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal partial class ActJournalRow : PresentationObject
	{
		public ActJournalRow() : base(EntityManager.GetEntity((int) Entities.ActJournalRow))
		{
		}

		public ActJournalRow(DataRow row) : base(EntityManager.GetEntity((int) Entities.ActJournalRow), row)
		{
		}

		// DoAction/GetCampaign переехали в ActJournalRow.WinForms.cs.

	}
}