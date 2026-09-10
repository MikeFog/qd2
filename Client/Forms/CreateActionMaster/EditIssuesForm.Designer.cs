namespace Merlin.Forms.CreateActionMaster
{
	partial class EditIssuesForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.splitContainerCampaigns = new System.Windows.Forms.SplitContainer();
			this.grdCampaigns = new FogSoft.WinForm.Controls.SmartGrid();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerCampaigns)).BeginInit();
			this.splitContainerCampaigns.Panel1.SuspendLayout();
			this.splitContainerCampaigns.Panel2.SuspendLayout();
			this.splitContainerCampaigns.SuspendLayout();
			this.splitContainer4.Panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainerCampaigns
			// 
			// Нижняя часть левой колонки: сверху «Добавленные выпуски» (грид базовой формы),
			// снизу чек-лист кампаний акции, по которым работает веер.
			this.splitContainerCampaigns.Dock = System.Windows.Forms.DockStyle.Fill;
			// Чек-лист кампаний держит свой размер, лишнее место достаётся «Добавленным выпускам»
			// (грид выпусков растёт, когда на форме скрыт splitContainer4.Panel1).
			this.splitContainerCampaigns.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainerCampaigns.Location = new System.Drawing.Point(0, 0);
			this.splitContainerCampaigns.Name = "splitContainerCampaigns";
			this.splitContainerCampaigns.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.splitContainerCampaigns.Panel2MinSize = 60;
			this.splitContainerCampaigns.Size = new System.Drawing.Size(691, 188);
			this.splitContainerCampaigns.SplitterDistance = 110;
			this.splitContainerCampaigns.TabIndex = 0;
			// 
			// grdCampaigns
			// 
			this.grdCampaigns.Caption = "Кампании акции";
			this.grdCampaigns.CaptionVisible = true;
			this.grdCampaigns.CheckBoxes = true;
			this.grdCampaigns.ColumnNameHighlight = null;
			this.grdCampaigns.DataSource = null;
			this.grdCampaigns.DependantGrid = null;
			this.grdCampaigns.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grdCampaigns.Entity = null;
			this.grdCampaigns.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdCampaigns.IsHighlightInvertColor = false;
			this.grdCampaigns.IsNeedHighlight = false;
			this.grdCampaigns.Location = new System.Drawing.Point(0, 0);
			this.grdCampaigns.MenuEnabled = false;
			this.grdCampaigns.Name = "grdCampaigns";
			this.grdCampaigns.QuickSearchVisible = false;
			this.grdCampaigns.SelectedObject = null;
			this.grdCampaigns.ShowMultiselectColumn = true;
			this.grdCampaigns.ShowRowNumbers = false;
			this.grdCampaigns.Size = new System.Drawing.Size(691, 74);
			this.grdCampaigns.TabIndex = 0;
			// 
			// EditIssuesForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(786, 577);
			this.Cursor = System.Windows.Forms.Cursors.Default;
			this.Name = "EditIssuesForm";
			this.Text = "Веерное размещение рекламы";
			// Перекладываем «Добавленные выпуски» базовой формы в верхнюю панель нового
			// сплиттера, а сам сплиттер ставим на их место.
			this.splitContainer4.Panel2.Controls.Remove(this.grdCurrentCampaignIssues);
			this.splitContainerCampaigns.Panel1.Controls.Add(this.grdCurrentCampaignIssues);
			this.splitContainerCampaigns.Panel2.Controls.Add(this.grdCampaigns);
			this.splitContainer4.Panel2.Controls.Add(this.splitContainerCampaigns);
			this.splitContainer4.Panel2.ResumeLayout(false);
			this.splitContainerCampaigns.Panel1.ResumeLayout(false);
			this.splitContainerCampaigns.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainerCampaigns)).EndInit();
			this.splitContainerCampaigns.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private System.Windows.Forms.SplitContainer splitContainerCampaigns;
		protected FogSoft.WinForm.Controls.SmartGrid grdCampaigns;

		#endregion
	}
}