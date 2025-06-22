namespace Merlin.Forms {
  partial class StudioOrderActionForm {
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StudioOrderActionForm));
      this.tabAction = new System.Windows.Forms.TabControl();
      this.pageAction = new System.Windows.Forms.TabPage();
      this.toolStrip1 = new System.Windows.Forms.ToolStrip();
      this.tsbAdd = new System.Windows.Forms.ToolStripButton();
      this.tsbDelete = new System.Windows.Forms.ToolStripButton();
      this.tsbEdit = new System.Windows.Forms.ToolStripButton();
      this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
      this.tsbSetDiscount = new System.Windows.Forms.ToolStripButton();
			this.grdOrders = new FogSoft.WinForm.Controls.SmartGrid();
      this.chkPrintBill = new System.Windows.Forms.CheckBox();
      this.chkPrintAgreement = new System.Windows.Forms.CheckBox();
      this.lblTotalPrice = new System.Windows.Forms.Label();
      this.lblTariffPrice = new System.Windows.Forms.Label();
      this.lblFirmName = new System.Windows.Forms.Label();
			this.separator3 = new FogSoft.WinForm.Controls.Separator();
      this.label2 = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.lblFirm = new System.Windows.Forms.Label();
      this.btnOk = new System.Windows.Forms.Button();
      this.tabAction.SuspendLayout();
      this.pageAction.SuspendLayout();
      this.toolStrip1.SuspendLayout();
      this.SuspendLayout();
      // 
      // tabAction
      // 
      this.tabAction.Controls.Add(this.pageAction);
      this.tabAction.Location = new System.Drawing.Point(4, 3);
      this.tabAction.Name = "tabAction";
      this.tabAction.SelectedIndex = 0;
      this.tabAction.Size = new System.Drawing.Size(512, 258);
      this.tabAction.TabIndex = 6;
      // 
      // pageAction
      // 
      this.pageAction.Controls.Add(this.toolStrip1);
      this.pageAction.Controls.Add(this.grdOrders);
      this.pageAction.Controls.Add(this.chkPrintBill);
      this.pageAction.Controls.Add(this.chkPrintAgreement);
      this.pageAction.Controls.Add(this.lblTotalPrice);
      this.pageAction.Controls.Add(this.lblTariffPrice);
      this.pageAction.Controls.Add(this.lblFirmName);
      this.pageAction.Controls.Add(this.separator3);
      this.pageAction.Controls.Add(this.label2);
      this.pageAction.Controls.Add(this.label1);
      this.pageAction.Controls.Add(this.lblFirm);
      this.pageAction.Location = new System.Drawing.Point(4, 22);
      this.pageAction.Name = "pageAction";
      this.pageAction.Size = new System.Drawing.Size(504, 232);
      this.pageAction.TabIndex = 0;
      this.pageAction.Text = "Общие";
      // 
      // toolStrip1
      // 
      this.toolStrip1.CanOverflow = false;
      this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
      this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
      this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbAdd,
            this.tsbDelete,
            this.tsbEdit,
            this.toolStripSeparator1,
            this.tsbSetDiscount});
      this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
      this.toolStrip1.Location = new System.Drawing.Point(464, 72);
      this.toolStrip1.Name = "toolStrip1";
      this.toolStrip1.Size = new System.Drawing.Size(24, 100);
      this.toolStrip1.TabIndex = 22;
      this.toolStrip1.Text = "toolStrip1";
      // 
      // tsbAdd
      // 
      this.tsbAdd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.tsbAdd.Image = ((System.Drawing.Image)(resources.GetObject("tsbAdd.Image")));
      this.tsbAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
      this.tsbAdd.Name = "tsbAdd";
      this.tsbAdd.Size = new System.Drawing.Size(22, 20);
      this.tsbAdd.Text = "toolStripButton1";
      this.tsbAdd.ToolTipText = "Добавить заказ";
      this.tsbAdd.Click += new System.EventHandler(this.CreateOrder);
      // 
      // tsbDelete
      // 
      this.tsbDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.tsbDelete.Image = ((System.Drawing.Image)(resources.GetObject("tsbDelete.Image")));
      this.tsbDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
      this.tsbDelete.Name = "tsbDelete";
      this.tsbDelete.Size = new System.Drawing.Size(22, 20);
      this.tsbDelete.Text = "toolStripButton2";
      this.tsbDelete.ToolTipText = "Удалить заказ";
      this.tsbDelete.Click += new System.EventHandler(this.DeleteOrder);
      // 
      // tsbEdit
      // 
      this.tsbEdit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.tsbEdit.Image = ((System.Drawing.Image)(resources.GetObject("tsbEdit.Image")));
      this.tsbEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
      this.tsbEdit.Name = "tsbEdit";
      this.tsbEdit.Size = new System.Drawing.Size(22, 20);
      this.tsbEdit.Text = "toolStripButton3";
      this.tsbEdit.ToolTipText = "Редактировать заказ";
      this.tsbEdit.Click += new System.EventHandler(this.EditOrder);
      // 
      // toolStripSeparator1
      // 
      this.toolStripSeparator1.Name = "toolStripSeparator1";
      this.toolStripSeparator1.Size = new System.Drawing.Size(22, 6);
      // 
      // tsbSetDiscount
      // 
      this.tsbSetDiscount.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
      this.tsbSetDiscount.Image = ((System.Drawing.Image)(resources.GetObject("tsbSetDiscount.Image")));
      this.tsbSetDiscount.ImageTransparentColor = System.Drawing.Color.Magenta;
      this.tsbSetDiscount.Name = "tsbSetDiscount";
      this.tsbSetDiscount.Size = new System.Drawing.Size(22, 20);
      this.tsbSetDiscount.Text = "toolStripButton4";
      this.tsbSetDiscount.ToolTipText = "Менеджерская скидка";
      this.tsbSetDiscount.Click += new System.EventHandler(this.SetDiscount);
      // 
      // grdOrders
      // 
      this.grdOrders.Caption = "Заказы на производство роликов";
      this.grdOrders.CaptionVisible = true;
      this.grdOrders.CheckBoxes = false;
      this.grdOrders.DataSource = null;
      this.grdOrders.DependantGrid = null;
      this.grdOrders.Entity = null;
      this.grdOrders.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdOrders.InterfaceObject = FogSoft.WinForm.InterfaceObjects.SimpleJournal;
      this.grdOrders.Location = new System.Drawing.Point(8, 72);
      this.grdOrders.MenuEnabled = false;
      this.grdOrders.Name = "grdOrders";
      this.grdOrders.QuickSearchVisible = false;
      this.grdOrders.SelectedObject = null;
      this.grdOrders.Size = new System.Drawing.Size(447, 114);
      this.grdOrders.TabIndex = 21;
      // 
      // chkPrintBill
      // 
      this.chkPrintBill.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.chkPrintBill.Location = new System.Drawing.Point(8, 208);
      this.chkPrintBill.Name = "chkPrintBill";
      this.chkPrintBill.Size = new System.Drawing.Size(280, 16);
      this.chkPrintBill.TabIndex = 20;
      this.chkPrintBill.Text = "Распечатать счёт на предоплату";
      // 
      // chkPrintAgreement
      // 
      this.chkPrintAgreement.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.chkPrintAgreement.Location = new System.Drawing.Point(8, 192);
      this.chkPrintAgreement.Name = "chkPrintAgreement";
      this.chkPrintAgreement.Size = new System.Drawing.Size(280, 16);
      this.chkPrintAgreement.TabIndex = 19;
      this.chkPrintAgreement.Text = "Распечатать договор на производство роликов";
      // 
      // lblTotalPrice
      // 
      this.lblTotalPrice.AutoSize = true;
      this.lblTotalPrice.Location = new System.Drawing.Point(168, 44);
      this.lblTotalPrice.Name = "lblTotalPrice";
      this.lblTotalPrice.Size = new System.Drawing.Size(13, 13);
      this.lblTotalPrice.TabIndex = 15;
      this.lblTotalPrice.Text = "0";
      // 
      // lblTariffPrice
      // 
      this.lblTariffPrice.AutoSize = true;
      this.lblTariffPrice.Location = new System.Drawing.Point(168, 26);
      this.lblTariffPrice.Name = "lblTariffPrice";
      this.lblTariffPrice.Size = new System.Drawing.Size(13, 13);
      this.lblTariffPrice.TabIndex = 14;
      this.lblTariffPrice.Text = "0";
      // 
      // lblFirmName
      // 
      this.lblFirmName.AutoSize = true;
      this.lblFirmName.Location = new System.Drawing.Point(168, 8);
      this.lblFirmName.Name = "lblFirmName";
      this.lblFirmName.Size = new System.Drawing.Size(64, 13);
      this.lblFirmName.TabIndex = 13;
      this.lblFirmName.Text = "lblFirmName";
      // 
      // separator3
      // 
      this.separator3.BackColor = System.Drawing.SystemColors.ControlText;
      this.separator3.Location = new System.Drawing.Point(8, 64);
      this.separator3.Name = "separator3";
      this.separator3.Size = new System.Drawing.Size(488, 2);
      this.separator3.TabIndex = 9;
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(16, 44);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(118, 13);
      this.label2.TabIndex = 4;
      this.label2.Text = "Окончательная цена:";
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(16, 26);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(104, 13);
      this.label1.TabIndex = 3;
      this.label1.Text = "Сумма по тарифам:";
      // 
      // lblFirm
      // 
      this.lblFirm.AutoSize = true;
      this.lblFirm.Location = new System.Drawing.Point(16, 8);
      this.lblFirm.Name = "lblFirm";
      this.lblFirm.Size = new System.Drawing.Size(43, 13);
      this.lblFirm.TabIndex = 1;
      this.lblFirm.Text = "Фирма:";
      // 
      // btnOk
      // 
      this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.btnOk.Location = new System.Drawing.Point(212, 267);
      this.btnOk.Name = "btnOk";
      this.btnOk.Size = new System.Drawing.Size(93, 26);
      this.btnOk.TabIndex = 7;
      this.btnOk.Text = "Ок";
      this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
      // 
      // StudioOrderActionForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
      this.ClientSize = new System.Drawing.Size(520, 299);
      this.Controls.Add(this.tabAction);
      this.Controls.Add(this.btnOk);
      this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "StudioOrderActionForm";
      this.ShowInTaskbar = false;
      this.Text = "Заказ на производство роликов";
      this.tabAction.ResumeLayout(false);
      this.pageAction.ResumeLayout(false);
      this.pageAction.PerformLayout();
      this.toolStrip1.ResumeLayout(false);
      this.toolStrip1.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TabControl tabAction;
    private System.Windows.Forms.TabPage pageAction;
    private System.Windows.Forms.CheckBox chkPrintBill;
    private System.Windows.Forms.CheckBox chkPrintAgreement;
    private System.Windows.Forms.Label lblTotalPrice;
    private System.Windows.Forms.Label lblTariffPrice;
    private System.Windows.Forms.Label lblFirmName;
		private FogSoft.WinForm.Controls.Separator separator3;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label lblFirm;
    private System.Windows.Forms.Button btnOk;
		private FogSoft.WinForm.Controls.SmartGrid grdOrders;
    private System.Windows.Forms.ToolStrip toolStrip1;
    private System.Windows.Forms.ToolStripButton tsbAdd;
    private System.Windows.Forms.ToolStripButton tsbDelete;
    private System.Windows.Forms.ToolStripButton tsbEdit;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripButton tsbSetDiscount;
  }
}