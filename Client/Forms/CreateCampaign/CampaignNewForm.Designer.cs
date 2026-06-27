namespace Merlin.Forms.CreateCampaign
{
	partial class CampaignNewForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            this.label1 = new System.Windows.Forms.Label();
            this.cmbCampaignType = new FogSoft.WinForm.LookUp();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbPaymentType = new FogSoft.WinForm.LookUp();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.grdMassmedia = new FogSoft.WinForm.Controls.SmartGrid();
            this.grdAgency = new FogSoft.WinForm.Controls.SmartGrid();
            this.label3 = new System.Windows.Forms.Label();
            this.lookUpRolType = new FogSoft.WinForm.LookUp();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 6, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(226, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Тип рекламной кампании:";
            // 
            // cmbCampaignType
            // 
            this.cmbCampaignType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbCampaignType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbCampaignType.IsNullable = false;
            this.cmbCampaignType.Location = new System.Drawing.Point(14, 34);
            this.cmbCampaignType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbCampaignType.Name = "cmbCampaignType";
            this.cmbCampaignType.SelectedIndex = -1;
            this.cmbCampaignType.SelectedValue = null;
            this.cmbCampaignType.Size = new System.Drawing.Size(618, 33);
            this.cmbCampaignType.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 70);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 25);
            this.label2.TabIndex = 4;
            this.label2.Text = "Тип оплаты:";
            // 
            // cmbPaymentType
            // 
            this.cmbPaymentType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbPaymentType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbPaymentType.IsNullable = false;
            this.cmbPaymentType.Location = new System.Drawing.Point(14, 98);
            this.cmbPaymentType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbPaymentType.Name = "cmbPaymentType";
            this.cmbPaymentType.SelectedIndex = -1;
            this.cmbPaymentType.SelectedValue = null;
            this.cmbPaymentType.Size = new System.Drawing.Size(618, 33);
            this.cmbPaymentType.TabIndex = 5;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(528, 3);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 33);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Enabled = false;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(420, 3);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 33);
            this.btnOk.TabIndex = 10;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // grdMassmedia
            // 
            this.grdMassmedia.Caption = "Радиостанции";
            this.grdMassmedia.CaptionVisible = true;
            this.grdMassmedia.CheckBoxes = true;
            this.grdMassmedia.ColumnNameHighlight = null;
            this.grdMassmedia.DataSource = null;
            this.grdMassmedia.DependantGrid = null;
            this.grdMassmedia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdMassmedia.Entity = null;
            this.grdMassmedia.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdMassmedia.IsHighlightInvertColor = false;
            this.grdMassmedia.IsNeedHighlight = false;
            this.grdMassmedia.Location = new System.Drawing.Point(14, 215);
            this.grdMassmedia.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdMassmedia.MenuEnabled = false;
            this.grdMassmedia.Name = "grdMassmedia";
            this.grdMassmedia.QuickSearchVisible = false;
            this.grdMassmedia.SelectedObject = null;
            this.grdMassmedia.ShowMultiselectColumn = true;
            this.grdMassmedia.Size = new System.Drawing.Size(618, 439);
            this.grdMassmedia.TabIndex = 12;
            // 
            // grdAgency
            // 
            this.grdAgency.Caption = "Счёт выписан от";
            this.grdAgency.CaptionVisible = true;
            this.grdAgency.CheckBoxes = false;
            this.grdAgency.ColumnNameHighlight = null;
            this.grdAgency.DataSource = null;
            this.grdAgency.DependantGrid = null;
            this.grdAgency.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdAgency.Entity = null;
            this.grdAgency.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdAgency.IsHighlightInvertColor = false;
            this.grdAgency.IsNeedHighlight = false;
            this.grdAgency.Location = new System.Drawing.Point(14, 660);
            this.grdAgency.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdAgency.MenuEnabled = false;
            this.grdAgency.Name = "grdAgency";
            this.grdAgency.QuickSearchVisible = false;
            this.grdAgency.SelectedObject = null;
            this.grdAgency.ShowMultiselectColumn = true;
            this.grdAgency.Size = new System.Drawing.Size(618, 133);
            this.grdAgency.TabIndex = 13;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 137);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 25);
            this.label3.TabIndex = 14;
            this.label3.Text = "Группа:";
            // 
            // lookUpRolType
            // 
            this.lookUpRolType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lookUpRolType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lookUpRolType.IsNullable = false;
            this.lookUpRolType.Location = new System.Drawing.Point(14, 165);
            this.lookUpRolType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.lookUpRolType.Name = "lookUpRolType";
            this.lookUpRolType.SelectedIndex = -1;
            this.lookUpRolType.SelectedValue = null;
            this.lookUpRolType.Size = new System.Drawing.Size(618, 33);
            this.lookUpRolType.TabIndex = 15;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnOk);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(14, 799);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(618, 41);
            this.flowLayoutPanel1.TabIndex = 16;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.cmbCampaignType, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.cmbPaymentType, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.lookUpRolType, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.grdMassmedia, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.grdAgency, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 8);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(10, 0, 10, 10);
            this.tableLayoutPanel1.RowCount = 9;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(646, 802);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // CampaignNewForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(646, 802);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(508, 552);
            this.Name = "CampaignNewForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Новая рекламная кампания";
            this.Load += new System.EventHandler(this.CampaignNewForm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private FogSoft.WinForm.LookUp cmbCampaignType;
		private System.Windows.Forms.Label label2;
		private FogSoft.WinForm.LookUp cmbPaymentType;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private FogSoft.WinForm.Controls.SmartGrid grdMassmedia;
		private FogSoft.WinForm.Controls.SmartGrid grdAgency;
		private System.Windows.Forms.Label label3;
		private FogSoft.WinForm.LookUp lookUpRolType;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
	}
}
