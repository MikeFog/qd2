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
        private Button btnOK;
        private Button btnCancel;

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
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chkPrintWithSignatures
            // 
            this.chkPrintWithSignatures.Checked = true;
            this.chkPrintWithSignatures.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPrintWithSignatures.Location = new System.Drawing.Point(12, 12);
            this.chkPrintWithSignatures.Margin = new System.Windows.Forms.Padding(4);
            this.chkPrintWithSignatures.Name = "chkPrintWithSignatures";
            this.chkPrintWithSignatures.Size = new System.Drawing.Size(621, 28);
            this.chkPrintWithSignatures.TabIndex = 1;
            this.chkPrintWithSignatures.Text = "Распечатать документ с подготовленными подписями?";
            this.chkPrintWithSignatures.UseVisualStyleBackColor = true;
            // 
            // chkShowAdvertisingInfo
            // 
            this.chkShowAdvertisingInfo.Location = new System.Drawing.Point(12, 48);
            this.chkShowAdvertisingInfo.Margin = new System.Windows.Forms.Padding(4);
            this.chkShowAdvertisingInfo.Name = "chkShowAdvertisingInfo";
            this.chkShowAdvertisingInfo.Size = new System.Drawing.Size(422, 28);
            this.chkShowAdvertisingInfo.TabIndex = 2;
            this.chkShowAdvertisingInfo.Text = "Вывести информацию о предмете рекламы?";
            this.chkShowAdvertisingInfo.UseVisualStyleBackColor = true;
            // 
            // chkShowOnlyFinalCost
            // 
            this.chkShowOnlyFinalCost.Checked = true;
            this.chkShowOnlyFinalCost.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowOnlyFinalCost.Location = new System.Drawing.Point(12, 84);
            this.chkShowOnlyFinalCost.Margin = new System.Windows.Forms.Padding(4);
            this.chkShowOnlyFinalCost.Name = "chkShowOnlyFinalCost";
            this.chkShowOnlyFinalCost.Size = new System.Drawing.Size(422, 28);
            this.chkShowOnlyFinalCost.TabIndex = 3;
            this.chkShowOnlyFinalCost.Text = "Скрыть стоимость по тарифам";
            this.chkShowOnlyFinalCost.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnOK.Location = new System.Drawing.Point(267, 126);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 30);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCancel.Location = new System.Drawing.Point(359, 126);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // PrintMediaPlanSettings
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(715, 173);
            this.Controls.Add(this.chkPrintWithSignatures);
            this.Controls.Add(this.chkShowAdvertisingInfo);
            this.Controls.Add(this.chkShowOnlyFinalCost);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrintMediaPlanSettings";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Настройки печати";
            this.ResumeLayout(false);

        }

        #endregion
    }
}