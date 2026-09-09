namespace Merlin.Forms
{
	partial class ChangePaymentTypeMassForm
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
			this.labelPaymentType = new System.Windows.Forms.Label();
			this.lookUpPaymentType = new FogSoft.WinForm.LookUp();
			this.labelCampaigns = new System.Windows.Forms.Label();
			this.grdCampaigns = new FogSoft.WinForm.Controls.SmartGrid();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.tableLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			//
			// labelPaymentType
			//
			this.labelPaymentType.AutoSize = true;
			this.labelPaymentType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelPaymentType.Name = "labelPaymentType";
			this.labelPaymentType.TabIndex = 0;
			this.labelPaymentType.Text = "Новый тип оплаты:";
			//
			// lookUpPaymentType
			//
			this.lookUpPaymentType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.lookUpPaymentType.IsNullable = false;
			this.lookUpPaymentType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 8);
			this.lookUpPaymentType.Name = "lookUpPaymentType";
			this.lookUpPaymentType.SelectedIndex = -1;
			this.lookUpPaymentType.SelectedValue = null;
			this.lookUpPaymentType.Size = new System.Drawing.Size(940, 33);
			this.lookUpPaymentType.TabIndex = 1;
			this.lookUpPaymentType.SelectedItemChanged += new System.EventHandler(this.lookUpPaymentType_SelectedItemChanged);
			//
			// labelCampaigns
			//
			this.labelCampaigns.AutoSize = true;
			this.labelCampaigns.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelCampaigns.Name = "labelCampaigns";
			this.labelCampaigns.TabIndex = 2;
			this.labelCampaigns.Text = "Отметьте кампании:";
			//
			// grdCampaigns
			//
			this.grdCampaigns.CheckBoxes = true;
			this.grdCampaigns.ColumnNameHighlight = null;
			this.grdCampaigns.DataSource = null;
			this.grdCampaigns.DependantGrid = null;
			this.grdCampaigns.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grdCampaigns.Entity = null;
			this.grdCampaigns.IsHighlightInvertColor = false;
			this.grdCampaigns.IsNeedHighlight = false;
			this.grdCampaigns.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.grdCampaigns.MenuEnabled = false;
			this.grdCampaigns.Name = "grdCampaigns";
			this.grdCampaigns.QuickSearchVisible = false;
			this.grdCampaigns.SelectedObject = null;
			this.grdCampaigns.ShowMultiselectColumn = true;
			this.grdCampaigns.Size = new System.Drawing.Size(940, 300);
			this.grdCampaigns.TabIndex = 3;
			this.grdCampaigns.ObjectChecked += new FogSoft.WinForm.ObjectCheckedDelegate(this.grdCampaigns_ObjectChecked);
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.labelPaymentType, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.lookUpPaymentType, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.labelCampaigns, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.grdCampaigns, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 4);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(12);
			this.tableLayoutPanel1.RowCount = 5;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(968, 461);
			this.tableLayoutPanel1.TabIndex = 0;
			//
			// flowLayoutPanel1
			//
			this.flowLayoutPanel1.Controls.Add(this.btnCancel);
			this.flowLayoutPanel1.Controls.Add(this.btnOk);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 8, 4, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(940, 39);
			this.flowLayoutPanel1.TabIndex = 4;
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(100, 33);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Отмена";
			//
			// btnOk
			//
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Enabled = false;
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(100, 33);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "Ок";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// ChangePaymentTypeMassForm
			//
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(968, 461);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(840, 360);
			this.Name = "ChangePaymentTypeMassForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Сменить тип оплаты";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.Label labelPaymentType;
		private FogSoft.WinForm.LookUp lookUpPaymentType;
		private System.Windows.Forms.Label labelCampaigns;
		private FogSoft.WinForm.Controls.SmartGrid grdCampaigns;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
	}
}
