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
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(153, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Тип рекламной кампании:";
            // 
            // cmbCampaignType
            // 
            this.cmbCampaignType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbCampaignType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbCampaignType.IsNullable = false;
            this.cmbCampaignType.Location = new System.Drawing.Point(14, 30);
            this.cmbCampaignType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbCampaignType.Name = "cmbCampaignType";
            this.cmbCampaignType.SelectedIndex = -1;
            this.cmbCampaignType.SelectedValue = null;
            this.cmbCampaignType.Size = new System.Drawing.Size(490, 21);
            this.cmbCampaignType.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 58);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 15);
            this.label2.TabIndex = 4;
            this.label2.Text = "Тип оплаты:";
            // 
            // cmbPaymentType
            // 
            this.cmbPaymentType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPaymentType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbPaymentType.IsNullable = false;
            this.cmbPaymentType.Location = new System.Drawing.Point(14, 77);
            this.cmbPaymentType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbPaymentType.Name = "cmbPaymentType";
            this.cmbPaymentType.SelectedIndex = -1;
            this.cmbPaymentType.SelectedValue = null;
            this.cmbPaymentType.Size = new System.Drawing.Size(490, 21);
            this.cmbPaymentType.TabIndex = 5;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(534, 62);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(93, 25);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Enabled = false;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(534, 30);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(93, 25);
            this.btnOk.TabIndex = 10;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // grdMassmedia
            // 
            this.grdMassmedia.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdMassmedia.Caption = "Радиостанции";
            this.grdMassmedia.CaptionVisible = true;
            this.grdMassmedia.CheckBoxes = false;
            this.grdMassmedia.ColumnNameHighlight = null;
            this.grdMassmedia.DataSource = null;
            this.grdMassmedia.DependantGrid = null;
            this.grdMassmedia.Entity = null;
            this.grdMassmedia.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdMassmedia.IsHighlightInvertColor = false;
            this.grdMassmedia.IsNeedHighlight = false;
            this.grdMassmedia.Location = new System.Drawing.Point(14, 156);
            this.grdMassmedia.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdMassmedia.MenuEnabled = false;
            this.grdMassmedia.Name = "grdMassmedia";
            this.grdMassmedia.QuickSearchVisible = false;
            this.grdMassmedia.SelectedObject = null;
            this.grdMassmedia.Size = new System.Drawing.Size(490, 193);
            this.grdMassmedia.TabIndex = 12;
            // 
            // grdAgency
            // 
            this.grdAgency.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdAgency.Caption = "Счёт выписан от";
            this.grdAgency.CaptionVisible = true;
            this.grdAgency.CheckBoxes = false;
            this.grdAgency.ColumnNameHighlight = null;
            this.grdAgency.DataSource = null;
            this.grdAgency.DependantGrid = null;
            this.grdAgency.Entity = null;
            this.grdAgency.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdAgency.IsHighlightInvertColor = false;
            this.grdAgency.IsNeedHighlight = false;
            this.grdAgency.Location = new System.Drawing.Point(14, 355);
            this.grdAgency.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdAgency.MenuEnabled = false;
            this.grdAgency.Name = "grdAgency";
            this.grdAgency.QuickSearchVisible = false;
            this.grdAgency.SelectedObject = null;
            this.grdAgency.Size = new System.Drawing.Size(490, 167);
            this.grdAgency.TabIndex = 13;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 105);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 15);
            this.label3.TabIndex = 14;
            this.label3.Text = "Группа:";
            // 
            // lookUpRolType
            // 
            this.lookUpRolType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lookUpRolType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lookUpRolType.IsNullable = false;
            this.lookUpRolType.Location = new System.Drawing.Point(14, 125);
            this.lookUpRolType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.lookUpRolType.Name = "lookUpRolType";
            this.lookUpRolType.SelectedIndex = -1;
            this.lookUpRolType.SelectedValue = null;
            this.lookUpRolType.Size = new System.Drawing.Size(490, 21);
            this.lookUpRolType.TabIndex = 15;
            // 
            // CampaignNewForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(646, 527);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lookUpRolType);
            this.Controls.Add(this.grdAgency);
            this.Controls.Add(this.grdMassmedia);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbPaymentType);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbCampaignType);
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
            this.ResumeLayout(false);
            this.PerformLayout();

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
	}
}