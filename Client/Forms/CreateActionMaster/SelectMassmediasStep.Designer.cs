namespace Merlin.Forms.CreateActionMaster
{
	partial class SelectMassmediasStep
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
            this.grdMassmedia = new FogSoft.WinForm.Controls.SmartGrid();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbRadioStationGroup = new FogSoft.WinForm.LookUp();
            this.label2 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.lookUpPaymentType = new FogSoft.WinForm.LookUp();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // grdMassmedia
            // 
            this.grdMassmedia.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdMassmedia.Caption = "Радиостанции";
            this.grdMassmedia.CaptionVisible = true;
            this.grdMassmedia.CheckBoxes = true;
            this.grdMassmedia.ColumnNameHighlight = null;
            this.grdMassmedia.DataSource = null;
            this.grdMassmedia.DependantGrid = null;
            this.grdMassmedia.Entity = null;
            this.grdMassmedia.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdMassmedia.IsHighlightInvertColor = false;
            this.grdMassmedia.IsNeedHighlight = false;
            this.grdMassmedia.Location = new System.Drawing.Point(14, 111);
            this.grdMassmedia.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdMassmedia.MenuEnabled = false;
            this.grdMassmedia.Name = "grdMassmedia";
            this.grdMassmedia.QuickSearchVisible = false;
            this.grdMassmedia.SelectedObject = null;
            this.grdMassmedia.Size = new System.Drawing.Size(490, 311);
            this.grdMassmedia.TabIndex = 13;
            this.grdMassmedia.ObjectChecked += new FogSoft.WinForm.ObjectCheckedDelegate(this.grdMassmedia_ObjectChecked);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 49);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 15);
            this.label1.TabIndex = 14;
            this.label1.Text = "Группа:";
            // 
            // cmbRadioStationGroup
            // 
            this.cmbRadioStationGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbRadioStationGroup.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbRadioStationGroup.IsNullable = false;
            this.cmbRadioStationGroup.Location = new System.Drawing.Point(14, 67);
            this.cmbRadioStationGroup.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbRadioStationGroup.Name = "cmbRadioStationGroup";
            this.cmbRadioStationGroup.SelectedIndex = -1;
            this.cmbRadioStationGroup.SelectedValue = null;
            this.cmbRadioStationGroup.Size = new System.Drawing.Size(367, 21);
            this.cmbRadioStationGroup.TabIndex = 15;
            this.cmbRadioStationGroup.SelectedItemChanged += new System.EventHandler(this.SelectedItemChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 92);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(145, 15);
            this.label2.TabIndex = 16;
            this.label2.Text = "Выберите радиостанции:";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(411, 52);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(93, 25);
            this.btnCancel.TabIndex = 18;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Enabled = false;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(411, 20);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(93, 25);
            this.btnOk.TabIndex = 17;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // lookUpPaymentType
            // 
            this.lookUpPaymentType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lookUpPaymentType.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lookUpPaymentType.IsNullable = false;
            this.lookUpPaymentType.Location = new System.Drawing.Point(14, 24);
            this.lookUpPaymentType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.lookUpPaymentType.Name = "lookUpPaymentType";
            this.lookUpPaymentType.SelectedIndex = -1;
            this.lookUpPaymentType.SelectedValue = null;
            this.lookUpPaymentType.Size = new System.Drawing.Size(367, 21);
            this.lookUpPaymentType.TabIndex = 21;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 6);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(130, 15);
            this.label4.TabIndex = 22;
            this.label4.Text = "Выберите тип оплаты:";
            // 
            // SelectMassmediasStep
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(523, 434);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lookUpPaymentType);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbRadioStationGroup);
            this.Controls.Add(this.grdMassmedia);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(508, 352);
            this.Name = "SelectMassmediasStep";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Веерное размещение: Выбор радиостанций";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private FogSoft.WinForm.Controls.SmartGrid grdMassmedia;
		private System.Windows.Forms.Label label1;
		private FogSoft.WinForm.LookUp cmbRadioStationGroup;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
        private FogSoft.WinForm.LookUp lookUpPaymentType;
        private System.Windows.Forms.Label label4;
    }
}