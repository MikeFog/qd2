using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Properties;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class AnnouncementJournalForm : JournalForm
	{
		private ToolStripButton tsbMarkReadAll;
		private ToolStripSeparator tsbSeparator;

		public AnnouncementJournalForm()
		{
			InitializeComponent();
			InitMarkReadAll();
		}

		public AnnouncementJournalForm(Entity entity, string caption) : base(entity, caption)
		{
			InitializeComponent();
			InitMarkReadAll();
		}

		private void InitMarkReadAll()
		{
			tsbMarkReadAll = new ToolStripButton();
			tsbSeparator = new ToolStripSeparator();
			TsJournal.Items.Insert(0, tsbMarkReadAll);
			TsJournal.Items.Insert(1, tsbSeparator);
			tsbMarkReadAll.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
			tsbMarkReadAll.Image = Resources.Tick;
			tsbMarkReadAll.ImageTransparentColor = Color.Magenta;
			tsbMarkReadAll.Name = "tsbMarkReadAll";
			tsbMarkReadAll.Size = new Size(41, 22);
			tsbMarkReadAll.Text = "Пометить все как прочтенное";
			tsbMarkReadAll.ToolTipText = "Пометить все как прочтенное";
			tsbMarkReadAll.Click += tsbMarkReadAll_Click;
			TsJournal.Items[2].Visible = false; // New
			TsJournal.Items[4].Visible = false; // Delete
			TsJournal.Items[9].Visible = false; // Sum
		}

		private void tsbMarkReadAll_Click(object sender, EventArgs e)
		{
			try
			{
				ProgressForm.Show(this, DoMarkReadAll, 0, Grid.DataSource.Table.Rows.Count, 1, null);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void DoMarkReadAll(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = sender as BackgroundWorker;
			foreach (DataRow row in Grid.DataSource.Table.Rows)
			{
				if (worker.CancellationPending)
				{
					e.Cancel = true;
					return;
				}

				if (row[Announcement.ParamNames.ConfirmationDate] == null
					|| row[Announcement.ParamNames.ConfirmationDate] == DBNull.Value)
				{
					Announcement announcement = new Announcement(row);
					announcement.DoAction(Announcement.ActionNames.MarkAsRead, this, InterfaceObjects.SimpleJournal);
				}
				worker.ReportProgress(0, Grid.DataSource.Table.Rows.IndexOf(row));
				Application.DoEvents();
			}
			Invoke(new Globals.VoidCallback(RefreshJournal));
		}
	}
}
