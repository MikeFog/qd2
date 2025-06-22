namespace Merlin.Forms {
    partial class ActionForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ActionForm));
            this.lblPackDiscount = new System.Windows.Forms.Label();
            this.lblTariffSum = new System.Windows.Forms.Label();
            this.lblFirmName = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblFirm = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.lblTotalPrice = new System.Windows.Forms.Label();
            this.pageAction = new System.Windows.Forms.TabPage();
            this.chkPrintBillContract = new System.Windows.Forms.CheckBox();
            this.chkPrintSponsorContract = new System.Windows.Forms.CheckBox();
            this.chkPrintMediaPlan = new System.Windows.Forms.CheckBox();
            this.tsAction = new System.Windows.Forms.ToolStrip();
            this.tsbAdd = new System.Windows.Forms.ToolStripButton();
            this.tsbDelete = new System.Windows.Forms.ToolStripButton();
            this.tsbEditRollerIssues = new System.Windows.Forms.ToolStripButton();
            this.tsbEditProgIssues = new System.Windows.Forms.ToolStripButton();
            this.tsbPrintMediaPlan = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSetDiscount = new System.Windows.Forms.ToolStripButton();
            this.grdCampaign = new FogSoft.WinForm.Controls.SmartGrid();
            this.chkPrintContract = new System.Windows.Forms.CheckBox();
            this.chkPrintBill = new System.Windows.Forms.CheckBox();
            this.separator3 = new FogSoft.WinForm.Controls.Separator();
            this.separator1 = new FogSoft.WinForm.Controls.Separator();
            this.btnOk = new System.Windows.Forms.Button();
            this.tabAction = new System.Windows.Forms.TabControl();
            this.pageAction.SuspendLayout();
            this.tsAction.SuspendLayout();
            this.tabAction.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblPackDiscount
            // 
            this.lblPackDiscount.AutoSize = true;
            this.lblPackDiscount.Location = new System.Drawing.Point(255, 124);
            this.lblPackDiscount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPackDiscount.Name = "lblPackDiscount";
            this.lblPackDiscount.Size = new System.Drawing.Size(36, 25);
            this.lblPackDiscount.TabIndex = 15;
            this.lblPackDiscount.Text = "1.0";
            // 
            // lblTariffSum
            // 
            this.lblTariffSum.AutoSize = true;
            this.lblTariffSum.Location = new System.Drawing.Point(255, 98);
            this.lblTariffSum.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTariffSum.Name = "lblTariffSum";
            this.lblTariffSum.Size = new System.Drawing.Size(22, 25);
            this.lblTariffSum.TabIndex = 14;
            this.lblTariffSum.Text = "0";
            // 
            // lblFirmName
            // 
            this.lblFirmName.AutoSize = true;
            this.lblFirmName.Location = new System.Drawing.Point(255, 70);
            this.lblFirmName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFirmName.Name = "lblFirmName";
            this.lblFirmName.Size = new System.Drawing.Size(113, 25);
            this.lblFirmName.TabIndex = 13;
            this.lblFirmName.Text = "lblFirmName";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(27, 153);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 25);
            this.label5.TabIndex = 8;
            this.label5.Text = "Итого:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 124);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(151, 25);
            this.label2.TabIndex = 4;
            this.label2.Text = "Пакетная скидка:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 98);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(173, 25);
            this.label1.TabIndex = 3;
            this.label1.Text = "Сумма по тарифам:";
            // 
            // lblFirm
            // 
            this.lblFirm.AutoSize = true;
            this.lblFirm.Location = new System.Drawing.Point(27, 70);
            this.lblFirm.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFirm.Name = "lblFirm";
            this.lblFirm.Size = new System.Drawing.Size(72, 25);
            this.lblFirm.TabIndex = 1;
            this.lblFirm.Text = "Фирма:";
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblName.Location = new System.Drawing.Point(27, 10);
            this.lblName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(62, 21);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "label1";
            // 
            // lblTotalPrice
            // 
            this.lblTotalPrice.AutoSize = true;
            this.lblTotalPrice.Location = new System.Drawing.Point(255, 153);
            this.lblTotalPrice.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTotalPrice.Name = "lblTotalPrice";
            this.lblTotalPrice.Size = new System.Drawing.Size(57, 25);
            this.lblTotalPrice.TabIndex = 18;
            this.lblTotalPrice.Text = "0.00р";
            // 
            // pageAction
            // 
            this.pageAction.Controls.Add(this.chkPrintBillContract);
            this.pageAction.Controls.Add(this.chkPrintSponsorContract);
            this.pageAction.Controls.Add(this.chkPrintMediaPlan);
            this.pageAction.Controls.Add(this.tsAction);
            this.pageAction.Controls.Add(this.grdCampaign);
            this.pageAction.Controls.Add(this.chkPrintContract);
            this.pageAction.Controls.Add(this.chkPrintBill);
            this.pageAction.Controls.Add(this.lblTotalPrice);
            this.pageAction.Controls.Add(this.lblPackDiscount);
            this.pageAction.Controls.Add(this.lblTariffSum);
            this.pageAction.Controls.Add(this.lblFirmName);
            this.pageAction.Controls.Add(this.separator3);
            this.pageAction.Controls.Add(this.label5);
            this.pageAction.Controls.Add(this.label2);
            this.pageAction.Controls.Add(this.label1);
            this.pageAction.Controls.Add(this.separator1);
            this.pageAction.Controls.Add(this.lblFirm);
            this.pageAction.Controls.Add(this.lblName);
            this.pageAction.Location = new System.Drawing.Point(4, 34);
            this.pageAction.Margin = new System.Windows.Forms.Padding(4);
            this.pageAction.Name = "pageAction";
            this.pageAction.Size = new System.Drawing.Size(1170, 610);
            this.pageAction.TabIndex = 0;
            this.pageAction.Text = "Общие";
            // 
            // chkPrintBillContract
            // 
            this.chkPrintBillContract.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkPrintBillContract.Location = new System.Drawing.Point(15, 491);
            this.chkPrintBillContract.Margin = new System.Windows.Forms.Padding(4);
            this.chkPrintBillContract.Name = "chkPrintBillContract";
            this.chkPrintBillContract.Size = new System.Drawing.Size(453, 26);
            this.chkPrintBillContract.TabIndex = 28;
            this.chkPrintBillContract.Text = "Распечатать счет-договор";
            this.chkPrintBillContract.UseVisualStyleBackColor = true;
            // 
            // chkPrintSponsorContract
            // 
            this.chkPrintSponsorContract.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkPrintSponsorContract.Location = new System.Drawing.Point(15, 547);
            this.chkPrintSponsorContract.Margin = new System.Windows.Forms.Padding(4);
            this.chkPrintSponsorContract.Name = "chkPrintSponsorContract";
            this.chkPrintSponsorContract.Size = new System.Drawing.Size(576, 24);
            this.chkPrintSponsorContract.TabIndex = 27;
            this.chkPrintSponsorContract.Text = "Распечатать спонсорский договор на проведение рекламной акции";
            // 
            // chkPrintMediaPlan
            // 
            this.chkPrintMediaPlan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkPrintMediaPlan.Location = new System.Drawing.Point(15, 574);
            this.chkPrintMediaPlan.Margin = new System.Windows.Forms.Padding(4);
            this.chkPrintMediaPlan.Name = "chkPrintMediaPlan";
            this.chkPrintMediaPlan.Size = new System.Drawing.Size(545, 26);
            this.chkPrintMediaPlan.TabIndex = 26;
            this.chkPrintMediaPlan.Text = "Распечатать график размещения рекламной акции";
            this.chkPrintMediaPlan.UseVisualStyleBackColor = true;
            // 
            // tsAction
            // 
            this.tsAction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tsAction.CanOverflow = false;
            this.tsAction.Dock = System.Windows.Forms.DockStyle.None;
            this.tsAction.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsAction.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.tsAction.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbAdd,
            this.tsbDelete,
            this.tsbEditRollerIssues,
            this.tsbEditProgIssues,
            this.tsbPrintMediaPlan,
            this.toolStripSeparator2,
            this.tsbSetDiscount});
            this.tsAction.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.tsAction.Location = new System.Drawing.Point(1124, 192);
            this.tsAction.Name = "tsAction";
            this.tsAction.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.tsAction.Size = new System.Drawing.Size(37, 206);
            this.tsAction.TabIndex = 25;
            this.tsAction.Text = "toolStrip1";
            // 
            // tsbAdd
            // 
            this.tsbAdd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbAdd.Image = ((System.Drawing.Image)(resources.GetObject("tsbAdd.Image")));
            this.tsbAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbAdd.Name = "tsbAdd";
            this.tsbAdd.Size = new System.Drawing.Size(33, 28);
            this.tsbAdd.Text = "toolStripButton1";
            this.tsbAdd.ToolTipText = "Добавить рекламную кампанию...";
            this.tsbAdd.Click += new System.EventHandler(this.AddCampaign);

            // 
            // tsbDelete
            // 
            this.tsbDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbDelete.Enabled = false;
            this.tsbDelete.Image = ((System.Drawing.Image)(resources.GetObject("tsbDelete.Image")));
            this.tsbDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbDelete.Name = "tsbDelete";
            this.tsbDelete.Size = new System.Drawing.Size(33, 28);
            this.tsbDelete.Text = "toolStripButton2";
            this.tsbDelete.ToolTipText = "Удалить рекламную кампанию";
            this.tsbDelete.Click += new System.EventHandler(this.DeleteCampaign);
            // 
            // tsbEditRollerIssues
            // 
            this.tsbEditRollerIssues.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbEditRollerIssues.Enabled = false;
            this.tsbEditRollerIssues.Image = ((System.Drawing.Image)(resources.GetObject("tsbEditRollerIssues.Image")));
            this.tsbEditRollerIssues.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbEditRollerIssues.Name = "tsbEditRollerIssues";
            this.tsbEditRollerIssues.Size = new System.Drawing.Size(33, 28);
            this.tsbEditRollerIssues.Text = "toolStripButton3";
            this.tsbEditRollerIssues.ToolTipText = "Редактировать размещение роликов";
            this.tsbEditRollerIssues.Click += new System.EventHandler(this.EditRollerIssues);
            // 
            // tsbEditProgIssues
            // 
            this.tsbEditProgIssues.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbEditProgIssues.Enabled = false;
            this.tsbEditProgIssues.Image = ((System.Drawing.Image)(resources.GetObject("tsbEditProgIssues.Image")));
            this.tsbEditProgIssues.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbEditProgIssues.Name = "tsbEditProgIssues";
            this.tsbEditProgIssues.Size = new System.Drawing.Size(33, 28);
            this.tsbEditProgIssues.Text = "toolStripButton1";
            this.tsbEditProgIssues.ToolTipText = "Редактировать выходы спонсорских программ ";
            this.tsbEditProgIssues.Click += new System.EventHandler(this.EditProgIssues);
            // 
            // tsbPrintMediaPlan
            // 
            this.tsbPrintMediaPlan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbPrintMediaPlan.Image = global::Merlin.Properties.Resources.printer1;
            this.tsbPrintMediaPlan.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbPrintMediaPlan.Name = "tsbPrintMediaPlan";
            this.tsbPrintMediaPlan.Size = new System.Drawing.Size(45, 28);
            this.tsbPrintMediaPlan.Text = "toolStripButton1";
            this.tsbPrintMediaPlan.ToolTipText = "Распечатать график размещения кампании";
            this.tsbPrintMediaPlan.Click += new System.EventHandler(this.tsbPrintMediaPlan_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(33, 6);
            // 
            // tsbSetDiscount
            // 
            this.tsbSetDiscount.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSetDiscount.Enabled = false;
            this.tsbSetDiscount.Image = ((System.Drawing.Image)(resources.GetObject("tsbSetDiscount.Image")));
            this.tsbSetDiscount.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSetDiscount.Name = "tsbSetDiscount";
            this.tsbSetDiscount.Size = new System.Drawing.Size(33, 28);
            this.tsbSetDiscount.Text = "toolStripButton4";
            this.tsbSetDiscount.ToolTipText = "Установить менеджерскую скидку";
            this.tsbSetDiscount.Click += new System.EventHandler(this.SetDiscount);
            // 
            // grdCampaign
            // 
            this.grdCampaign.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdCampaign.Caption = "Рекламные кампании";
            this.grdCampaign.CaptionVisible = true;
            this.grdCampaign.CheckBoxes = false;
            this.grdCampaign.ColumnNameHighlight = null;
            this.grdCampaign.DataSource = null;
            this.grdCampaign.DependantGrid = null;
            this.grdCampaign.Entity = null;
            this.grdCampaign.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdCampaign.IsHighlightInvertColor = false;
            this.grdCampaign.IsNeedHighlight = false;
            this.grdCampaign.Location = new System.Drawing.Point(15, 192);
            this.grdCampaign.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.grdCampaign.MenuEnabled = false;
            this.grdCampaign.Name = "grdCampaign";
            this.grdCampaign.QuickSearchVisible = false;
            this.grdCampaign.SelectedObject = null;
            this.grdCampaign.Size = new System.Drawing.Size(1096, 262);
            this.grdCampaign.TabIndex = 24;
            this.grdCampaign.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.grdCampaign_ObjectSelected);
            // 
            // chkPrintContract
            // 
            this.chkPrintContract.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkPrintContract.Location = new System.Drawing.Point(15, 520);
            this.chkPrintContract.Margin = new System.Windows.Forms.Padding(4);
            this.chkPrintContract.Name = "chkPrintContract";
            this.chkPrintContract.Size = new System.Drawing.Size(576, 24);
            this.chkPrintContract.TabIndex = 21;
            this.chkPrintContract.Text = "Распечатать договор на проведение рекламной акции";
            // 
            // chkPrintBill
            // 
            this.chkPrintBill.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkPrintBill.Location = new System.Drawing.Point(15, 464);
            this.chkPrintBill.Margin = new System.Windows.Forms.Padding(4);
            this.chkPrintBill.Name = "chkPrintBill";
            this.chkPrintBill.Size = new System.Drawing.Size(420, 24);
            this.chkPrintBill.TabIndex = 19;
            this.chkPrintBill.Text = "Распечатать счет на предоплату";
            // 
            // separator3
            // 
            this.separator3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.separator3.BackColor = System.Drawing.SystemColors.ControlText;
            this.separator3.Location = new System.Drawing.Point(15, 182);
            this.separator3.Margin = new System.Windows.Forms.Padding(4);
            this.separator3.Name = "separator3";
            this.separator3.Size = new System.Drawing.Size(1149, 2);
            this.separator3.TabIndex = 9;
            // 
            // separator1
            // 
            this.separator1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.separator1.BackColor = System.Drawing.SystemColors.ControlText;
            this.separator1.Location = new System.Drawing.Point(15, 58);
            this.separator1.Margin = new System.Windows.Forms.Padding(4);
            this.separator1.Name = "separator1";
            this.separator1.Size = new System.Drawing.Size(1149, 2);
            this.separator1.TabIndex = 2;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(524, 656);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(140, 39);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Ok";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // tabAction
            // 
            this.tabAction.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabAction.Controls.Add(this.pageAction);
            this.tabAction.Location = new System.Drawing.Point(3, 3);
            this.tabAction.Margin = new System.Windows.Forms.Padding(4);
            this.tabAction.Name = "tabAction";
            this.tabAction.SelectedIndex = 0;
            this.tabAction.Size = new System.Drawing.Size(1178, 648);
            this.tabAction.TabIndex = 4;

            // 
            // ActionForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1185, 705);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.tabAction);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(769, 677);
            this.Name = "ActionForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Рекламная акция";
            this.Load += new System.EventHandler(this.ActionForm_Load);
            this.pageAction.ResumeLayout(false);
            this.pageAction.PerformLayout();
            this.tsAction.ResumeLayout(false);
            this.tsAction.PerformLayout();
            this.tabAction.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblPackDiscount;
        private System.Windows.Forms.Label lblTariffSum;
        private System.Windows.Forms.Label lblFirmName;
        private FogSoft.WinForm.Controls.Separator separator3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private FogSoft.WinForm.Controls.Separator separator1;
        private System.Windows.Forms.Label lblFirm;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblTotalPrice;
        private System.Windows.Forms.TabPage pageAction;
        private System.Windows.Forms.CheckBox chkPrintContract;
        private System.Windows.Forms.CheckBox chkPrintBill;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.TabControl tabAction;
        private FogSoft.WinForm.Controls.SmartGrid grdCampaign;
        private System.Windows.Forms.ToolStrip tsAction;
        private System.Windows.Forms.ToolStripButton tsbAdd;
        private System.Windows.Forms.ToolStripButton tsbDelete;
        private System.Windows.Forms.ToolStripButton tsbEditRollerIssues;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton tsbSetDiscount;
        private System.Windows.Forms.ToolStripButton tsbEditProgIssues;
        private System.Windows.Forms.ToolStripButton tsbPrintMediaPlan;
        private System.Windows.Forms.CheckBox chkPrintMediaPlan;
        private System.Windows.Forms.CheckBox chkPrintSponsorContract;
        private System.Windows.Forms.CheckBox chkPrintBillContract;
    }
}