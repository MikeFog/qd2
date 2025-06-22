namespace Merlin.Forms.CreateCampaign
{
	partial class CampaignImportMediaPlusForm
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
            this.lblFile = new System.Windows.Forms.Label();
            this.textBoxFilePath = new System.Windows.Forms.TextBox();
            this.btnFilePath = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grdAgency = new FogSoft.WinForm.Controls.SmartGrid();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbPaymentType = new FogSoft.WinForm.LookUp();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.btnLoad = new System.Windows.Forms.Button();
            this.lblMassmediaLb = new System.Windows.Forms.Label();
            this.lblMassmedia = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblFile
            // 
            this.lblFile.AutoSize = true;
            this.lblFile.Location = new System.Drawing.Point(12, 9);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(82, 13);
            this.lblFile.TabIndex = 0;
            this.lblFile.Text = "Файл импорта:";
            // 
            // textBoxFilePath
            // 
            this.textBoxFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFilePath.Location = new System.Drawing.Point(12, 26);
            this.textBoxFilePath.Name = "textBoxFilePath";
            this.textBoxFilePath.Size = new System.Drawing.Size(266, 21);
            this.textBoxFilePath.TabIndex = 1;
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
            this.btnFilePath.TabIndex = 2;
            this.btnFilePath.UseVisualStyleBackColor = true;
            this.btnFilePath.Click += new System.EventHandler(this.btnFilePath_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Enabled = false;
            this.btnOk.Location = new System.Drawing.Point(337, 26);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 22);
            this.btnOk.TabIndex = 3;
            this.btnOk.Text = "Ок";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(337, 54);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 22);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // grdAgency
            // 
            this.grdAgency.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
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
            this.grdAgency.Location = new System.Drawing.Point(12, 152);
            this.grdAgency.MenuEnabled = false;
            this.grdAgency.Name = "grdAgency";
            this.grdAgency.QuickSearchVisible = false;
            this.grdAgency.SelectedObject = null;
            this.grdAgency.Size = new System.Drawing.Size(299, 206);
            this.grdAgency.TabIndex = 17;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Тип оплаты:";
            // 
            // cmbPaymentType
            // 
            this.cmbPaymentType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPaymentType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbPaymentType.IsNullable = false;
            this.cmbPaymentType.Location = new System.Drawing.Point(12, 95);
            this.cmbPaymentType.Name = "cmbPaymentType";
            this.cmbPaymentType.SelectedIndex = -1;
            this.cmbPaymentType.SelectedValue = null;
            this.cmbPaymentType.Size = new System.Drawing.Size(299, 21);
            this.cmbPaymentType.TabIndex = 15;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Файл MEDIA PLUS (*.mpm)|*.mpm";
            this.openFileDialog.Title = "Выбор файла импорта";
            // 
            // btnLoad
            // 
            this.btnLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoad.Image = global::Merlin.Properties.Resources.import;
            this.btnLoad.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnLoad.Location = new System.Drawing.Point(221, 53);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(90, 23);
            this.btnLoad.TabIndex = 18;
            this.btnLoad.Text = "Загрузить";
            this.btnLoad.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // lblMassmediaLb
            // 
            this.lblMassmediaLb.AutoSize = true;
            this.lblMassmediaLb.Location = new System.Drawing.Point(12, 119);
            this.lblMassmediaLb.Name = "lblMassmediaLb";
            this.lblMassmediaLb.Size = new System.Drawing.Size(83, 13);
            this.lblMassmediaLb.TabIndex = 19;
            this.lblMassmediaLb.Text = "Радиостанция:";
            // 
            // lblMassmedia
            // 
            this.lblMassmedia.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblMassmedia.Location = new System.Drawing.Point(15, 136);
            this.lblMassmedia.Name = "lblMassmedia";
            this.lblMassmedia.Size = new System.Drawing.Size(296, 13);
            this.lblMassmedia.TabIndex = 20;
            this.lblMassmedia.Text = "Радиостанция не найдена";
            this.lblMassmedia.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // CampaignImportMediaPlusForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(433, 370);
            this.Controls.Add(this.lblMassmedia);
            this.Controls.Add(this.lblMassmediaLb);
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
            this.MinimumSize = new System.Drawing.Size(441, 397);
            this.Name = "CampaignImportMediaPlusForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Импорт кампании \"MEDIAPLUS\"";
            this.Load += new System.EventHandler(this.CampaignImportForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblFile;
		private System.Windows.Forms.TextBox textBoxFilePath;
		private System.Windows.Forms.Button btnFilePath;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private FogSoft.WinForm.Controls.SmartGrid grdAgency;
		private System.Windows.Forms.Label label2;
		private FogSoft.WinForm.LookUp cmbPaymentType;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button btnLoad;
		private System.Windows.Forms.Label lblMassmediaLb;
		private System.Windows.Forms.Label lblMassmedia;
	}
}