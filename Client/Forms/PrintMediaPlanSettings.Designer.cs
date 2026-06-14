using System.Drawing;
using System.Windows.Forms;

namespace Merlin.Forms
{
    partial class PrintMediaPlanSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private CheckBox chkPrintWithSignatures;
        private CheckBox chkShowAdvertisingInfo;
        private CheckBox chkShowOnlyFinalCost;
        private CheckBox chkSaveDirectlyToDisk;
        private Button btnOK;
        private Button btnCancel;
        private TableLayoutPanel tableLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel1;

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
            this.chkPrintWithSignatures = new System.Windows.Forms.CheckBox();
            this.chkShowAdvertisingInfo = new System.Windows.Forms.CheckBox();
            this.chkShowOnlyFinalCost = new System.Windows.Forms.CheckBox();
            this.chkSaveDirectlyToDisk = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkPrintWithSignatures
            // 
            this.chkPrintWithSignatures.AutoSize = true;
            this.chkPrintWithSignatures.Checked = true;
            this.chkPrintWithSignatures.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPrintWithSignatures.Location = new System.Drawing.Point(10, 12);
            this.chkPrintWithSignatures.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkPrintWithSignatures.Name = "chkPrintWithSignatures";
            this.chkPrintWithSignatures.Size = new System.Drawing.Size(603, 27);
            this.chkPrintWithSignatures.TabIndex = 1;
            this.chkPrintWithSignatures.Text = "Распечатать документ с подготовленными подписями?";
            this.chkPrintWithSignatures.UseVisualStyleBackColor = true;
            // 
            // chkShowAdvertisingInfo
            // 
            this.chkShowAdvertisingInfo.AutoSize = true;
            this.chkShowAdvertisingInfo.Location = new System.Drawing.Point(10, 45);
            this.chkShowAdvertisingInfo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkShowAdvertisingInfo.Name = "chkShowAdvertisingInfo";
            this.chkShowAdvertisingInfo.Size = new System.Drawing.Size(386, 27);
            this.chkShowAdvertisingInfo.TabIndex = 2;
            this.chkShowAdvertisingInfo.Text = "Вывести информацию о предмете рекламы?";
            this.chkShowAdvertisingInfo.UseVisualStyleBackColor = true;
            // 
            // chkShowOnlyFinalCost
            //
            this.chkShowOnlyFinalCost.AutoSize = true;
            this.chkShowOnlyFinalCost.Checked = true;
            this.chkShowOnlyFinalCost.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowOnlyFinalCost.Location = new System.Drawing.Point(10, 78);
            this.chkShowOnlyFinalCost.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkShowOnlyFinalCost.Name = "chkShowOnlyFinalCost";
            this.chkShowOnlyFinalCost.Size = new System.Drawing.Size(386, 27);
            this.chkShowOnlyFinalCost.TabIndex = 3;
            this.chkShowOnlyFinalCost.Text = "Скрыть стоимость по тарифам";
            this.chkShowOnlyFinalCost.UseVisualStyleBackColor = true;
            //
            // chkSaveDirectlyToDisk
            //
            this.chkSaveDirectlyToDisk.AutoSize = true;
            this.chkSaveDirectlyToDisk.Checked = true;
            this.chkSaveDirectlyToDisk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSaveDirectlyToDisk.Location = new System.Drawing.Point(10, 111);
            this.chkSaveDirectlyToDisk.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkSaveDirectlyToDisk.Name = "chkSaveDirectlyToDisk";
            this.chkSaveDirectlyToDisk.Size = new System.Drawing.Size(386, 27);
            this.chkSaveDirectlyToDisk.TabIndex = 6;
            this.chkSaveDirectlyToDisk.Text = "Сразу сохранять файл на диск";
            this.chkSaveDirectlyToDisk.UseVisualStyleBackColor = true;
            //
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOK.Location = new System.Drawing.Point(276, 123);
            this.btnOK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 33);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(382, 123);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 33);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnOK);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 146);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(654, 54);
            this.flowLayoutPanel1.TabIndex = 6;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.chkPrintWithSignatures, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.chkShowAdvertisingInfo, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.chkShowOnlyFinalCost, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.chkSaveDirectlyToDisk, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(660, 203);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // PrintMediaPlanSettings
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(660, 203);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrintMediaPlanSettings";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Настройки печати";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
