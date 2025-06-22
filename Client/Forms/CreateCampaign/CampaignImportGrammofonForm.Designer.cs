namespace Merlin.Forms.CreateCampaign
{
	partial class CampaignImportGrammofonForm
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
            this.btnLoad = new System.Windows.Forms.Button();
            this.grdAgency = new FogSoft.WinForm.Controls.SmartGrid();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbPaymentType = new FogSoft.WinForm.LookUp();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnFilePath = new System.Windows.Forms.Button();
            this.textBoxFilePath = new System.Windows.Forms.TextBox();
            this.lblFile = new System.Windows.Forms.Label();
            this.grdMassmedia = new FogSoft.WinForm.Controls.SmartGrid();
            this.lblImportSheet = new System.Windows.Forms.Label();
            this.cbSheets = new System.Windows.Forms.ComboBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.label3 = new System.Windows.Forms.Label();
            this.lookUpRolType = new FogSoft.WinForm.LookUp();
            this.SuspendLayout();
            // 
            // btnLoad
            // 
            this.btnLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoad.Image = global::Merlin.Properties.Resources.import;
            this.btnLoad.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnLoad.Location = new System.Drawing.Point(221, 53);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(90, 23);
            this.btnLoad.TabIndex = 29;
            this.btnLoad.Text = "Загрузить";
            this.btnLoad.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
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
            this.grdAgency.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdAgency.IsHighlightInvertColor = false;
            this.grdAgency.IsNeedHighlight = false;
            this.grdAgency.Location = new System.Drawing.Point(12, 366);
            this.grdAgency.MenuEnabled = false;
            this.grdAgency.Name = "grdAgency";
            this.grdAgency.QuickSearchVisible = false;
            this.grdAgency.SelectedObject = null;
            this.grdAgency.Size = new System.Drawing.Size(299, 121);
            this.grdAgency.TabIndex = 28;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 119);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 13);
            this.label2.TabIndex = 26;
            this.label2.Text = "Тип оплаты:";
            // 
            // cmbPaymentType
            // 
            this.cmbPaymentType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPaymentType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbPaymentType.IsNullable = false;
            this.cmbPaymentType.Location = new System.Drawing.Point(12, 135);
            this.cmbPaymentType.Name = "cmbPaymentType";
            this.cmbPaymentType.SelectedIndex = -1;
            this.cmbPaymentType.SelectedValue = null;
            this.cmbPaymentType.Size = new System.Drawing.Size(299, 21);
            this.cmbPaymentType.TabIndex = 27;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(337, 54);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 22);
            this.btnCancel.TabIndex = 25;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Enabled = false;
            this.btnOk.Location = new System.Drawing.Point(337, 26);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 22);
            this.btnOk.TabIndex = 24;
            this.btnOk.Text = "Ок";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnFilePath
            // 
            this.btnFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFilePath.Image = global::Merlin.Properties.Resources.open;
            this.btnFilePath.Location = new System.Drawing.Point(284, 26);
            this.btnFilePath.Margin = new System.Windows.Forms.Padding(0);
            this.btnFilePath.Name = "btnFilePath";
            this.btnFilePath.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnFilePath.Size = new System.Drawing.Size(27, 21);
            this.btnFilePath.TabIndex = 23;
            this.btnFilePath.UseVisualStyleBackColor = true;
            this.btnFilePath.Click += new System.EventHandler(this.btnFilePath_Click);
            // 
            // textBoxFilePath
            // 
            this.textBoxFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFilePath.Location = new System.Drawing.Point(12, 26);
            this.textBoxFilePath.Name = "textBoxFilePath";
            this.textBoxFilePath.Size = new System.Drawing.Size(266, 21);
            this.textBoxFilePath.TabIndex = 22;
            // 
            // lblFile
            // 
            this.lblFile.AutoSize = true;
            this.lblFile.Location = new System.Drawing.Point(12, 9);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(82, 13);
            this.lblFile.TabIndex = 21;
            this.lblFile.Text = "Файл импорта:";
            // 
            // grdMassmedia
            // 
            this.grdMassmedia.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdMassmedia.Caption = "Радиостанция";
            this.grdMassmedia.CaptionVisible = true;
            this.grdMassmedia.CheckBoxes = false;
            this.grdMassmedia.ColumnNameHighlight = null;
            this.grdMassmedia.DataSource = null;
            this.grdMassmedia.DependantGrid = null;
            this.grdMassmedia.Entity = null;
            this.grdMassmedia.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdMassmedia.IsHighlightInvertColor = false;
            this.grdMassmedia.IsNeedHighlight = false;
            this.grdMassmedia.Location = new System.Drawing.Point(12, 203);
            this.grdMassmedia.MenuEnabled = false;
            this.grdMassmedia.Name = "grdMassmedia";
            this.grdMassmedia.QuickSearchVisible = false;
            this.grdMassmedia.SelectedObject = null;
            this.grdMassmedia.Size = new System.Drawing.Size(299, 157);
            this.grdMassmedia.TabIndex = 30;
            // 
            // lblImportSheet
            // 
            this.lblImportSheet.AutoSize = true;
            this.lblImportSheet.Location = new System.Drawing.Point(14, 79);
            this.lblImportSheet.Name = "lblImportSheet";
            this.lblImportSheet.Size = new System.Drawing.Size(130, 13);
            this.lblImportSheet.TabIndex = 31;
            this.lblImportSheet.Text = "Импортируемый листок:";
            // 
            // cbSheets
            // 
            this.cbSheets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbSheets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSheets.Enabled = false;
            this.cbSheets.FormattingEnabled = true;
            this.cbSheets.Location = new System.Drawing.Point(12, 95);
            this.cbSheets.Name = "cbSheets";
            this.cbSheets.Size = new System.Drawing.Size(299, 21);
            this.cbSheets.TabIndex = 32;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Файл ГРАММОФОН (*.xls)|*.xls";
            this.openFileDialog.Title = "Выбор файла импорта";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 159);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 13);
            this.label3.TabIndex = 33;
            this.label3.Text = "Группа:";
            // 
            // lookUpRolType
            // 
            this.lookUpRolType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lookUpRolType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lookUpRolType.IsNullable = false;
            this.lookUpRolType.Location = new System.Drawing.Point(12, 176);
            this.lookUpRolType.Name = "lookUpRolType";
            this.lookUpRolType.SelectedIndex = -1;
            this.lookUpRolType.SelectedValue = null;
            this.lookUpRolType.Size = new System.Drawing.Size(296, 21);
            this.lookUpRolType.TabIndex = 34;
            // 
            // CampaignImportGrammofonForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(433, 499);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lookUpRolType);
            this.Controls.Add(this.cbSheets);
            this.Controls.Add(this.lblImportSheet);
            this.Controls.Add(this.grdMassmedia);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.grdAgency);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbPaymentType);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnFilePath);
            this.Controls.Add(this.textBoxFilePath);
            this.Controls.Add(this.lblFile);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(441, 526);
            this.Name = "CampaignImportGrammofonForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Импорт кампании \"ГРАММОФОН\"";
            this.Load += new System.EventHandler(this.CampaignImportGrammofonForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnLoad;
		private FogSoft.WinForm.Controls.SmartGrid grdAgency;
		private System.Windows.Forms.Label label2;
		private FogSoft.WinForm.LookUp cmbPaymentType;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnFilePath;
		private System.Windows.Forms.TextBox textBoxFilePath;
		private System.Windows.Forms.Label lblFile;
		private FogSoft.WinForm.Controls.SmartGrid grdMassmedia;
		private System.Windows.Forms.Label lblImportSheet;
		private System.Windows.Forms.ComboBox cbSheets;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Label label3;
		private FogSoft.WinForm.LookUp lookUpRolType;
	}
}