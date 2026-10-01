using System.Collections.Generic;
using System.Drawing;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using Merlin.Properties;

namespace Merlin.Forms
{
	public partial class ActJournalForm : JournalForm
	{
		public ActJournalForm(Entity entity, string caption) 
			: base(entity, caption, true)
		{
		}
		
		public ActJournalForm(Entity entity, string caption, Dictionary<string, object> filterValues) : base(entity, caption, filterValues)
		{
			FilterBtn.Enabled = false;
		}

		// Пересборка таблицы (итоги по акции, гашение повторов, «Итого») — ядро ActJournal, его
		// зовёт и веб-экран.
		protected override void PopulateDataGrid()
		{
			ActJournal.Prepare(_dtData);
			base.PopulateDataGrid();
			Grid.InternalGrid.Rows[Grid.InternalGrid.Rows.Count - 1].DefaultCellStyle.Font 
				= new Font(Grid.InternalGrid.DefaultCellStyle.Font, FontStyle.Bold);

			if (ActJournal.HasUnprocessedMassmedia(_dtData))
				UserMessage.ShowInformation(Resources.ActJournalMassmediaExplamation);
		}
	}
}