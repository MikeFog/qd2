namespace Merlin.Forms {
  partial class PaymentCandidatesForm {
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
		this.lblRest = new System.Windows.Forms.Label();
		this.lblSumma = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this.grdCandidates = new FogSoft.WinForm.Controls.SmartGrid();
		this.btnCancel = new System.Windows.Forms.Button();
		this.btnOk = new System.Windows.Forms.Button();
		this.SuspendLayout();
		// 
		// lblRest
		// 
		this.lblRest.AutoSize = true;
		this.lblRest.Location = new System.Drawing.Point(100, 33);
		this.lblRest.Name = "lblRest";
		this.lblRest.Size = new System.Drawing.Size(13, 13);
		this.lblRest.TabIndex = 8;
		this.lblRest.Text = "0";
		// 
		// lblSumma
		// 
		this.lblSumma.AutoSize = true;
		this.lblSumma.Location = new System.Drawing.Point(100, 9);
		this.lblSumma.Name = "lblSumma";
		this.lblSumma.Size = new System.Drawing.Size(13, 13);
		this.lblSumma.TabIndex = 7;
		this.lblSumma.Text = "0";
		// 
		// label2
		// 
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(12, 33);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(54, 13);
		this.label2.TabIndex = 6;
		this.label2.Text = "Остаток:";
		// 
		// label1
		// 
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(12, 9);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(42, 13);
		this.label1.TabIndex = 5;
		this.label1.Text = "Сумма:";
		// 
		// grdCandidates
		// 
		this.grdCandidates.Caption = "Кандидаты на оплату";
		this.grdCandidates.CaptionVisible = true;
		this.grdCandidates.CheckBoxes = true;
		this.grdCandidates.DataSource = null;
		this.grdCandidates.DependantGrid = null;
		this.grdCandidates.Entity = null;
		this.grdCandidates.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
		this.grdCandidates.InterfaceObject = FogSoft.WinForm.InterfaceObjects.SimpleJournal;
		this.grdCandidates.Location = new System.Drawing.Point(12, 49);
		this.grdCandidates.MenuEnabled = false;
		this.grdCandidates.Name = "grdCandidates";
		this.grdCandidates.QuickSearchVisible = false;
		this.grdCandidates.SelectedObject = null;
		this.grdCandidates.Size = new System.Drawing.Size(452, 191);
		this.grdCandidates.TabIndex = 9;
		this.grdCandidates.ObjectChecked += new FogSoft.WinForm.ObjectCheckedDelegate(this.grdCandidates_ObjectChecked);
		// 
		// btnCancel
		// 
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.Location = new System.Drawing.Point(240, 246);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(75, 23);
		this.btnCancel.TabIndex = 11;
		this.btnCancel.Text = "Отмена";
		// 
		// btnOk
		// 
		this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.btnOk.Location = new System.Drawing.Point(160, 246);
		this.btnOk.Name = "btnOk";
		this.btnOk.Size = new System.Drawing.Size(75, 23);
		this.btnOk.TabIndex = 10;
		this.btnOk.Text = "Ок";
		this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
		// 
		// PaymentCandidatesForm
		// 
		this.AcceptButton = this.btnOk;
		this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.CancelButton = this.btnCancel;
		this.ClientSize = new System.Drawing.Size(474, 277);
		this.Controls.Add(this.btnCancel);
		this.Controls.Add(this.btnOk);
		this.Controls.Add(this.grdCandidates);
		this.Controls.Add(this.lblRest);
		this.Controls.Add(this.lblSumma);
		this.Controls.Add(this.label2);
		this.Controls.Add(this.label1);
		this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
		this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		this.MaximizeBox = false;
		this.MinimizeBox = false;
		this.Name = "PaymentCandidatesForm";
		this.ShowIcon = false;
		this.ShowInTaskbar = false;
		this.Text = "Кандидаты на оплату";
		this.ResumeLayout(false);
		this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label lblRest;
    private System.Windows.Forms.Label lblSumma;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
    private FogSoft.WinForm.Controls.SmartGrid grdCandidates;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Button btnOk;
  }
}