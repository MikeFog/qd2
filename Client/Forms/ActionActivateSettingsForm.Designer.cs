namespace Merlin.Forms
{
    partial class ActionActivateSettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.Label lblConfirmation;
        private System.Windows.Forms.CheckBox chkTryTransferFailedIssues;
        private System.Windows.Forms.GroupBox grpTransferSettings;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelTransfer;
        private System.Windows.Forms.Label lblTransferAttemptCount;
        private System.Windows.Forms.NumericUpDown numTransferAttemptCount;
        private System.Windows.Forms.CheckBox chkAllowDifferentWindowPrice;
        private System.Windows.Forms.CheckBox chkAvoidFirmRollerWindows;
        private System.Windows.Forms.Label lblTransferRules;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelButtons;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.lblConfirmation = new System.Windows.Forms.Label();
            this.chkTryTransferFailedIssues = new System.Windows.Forms.CheckBox();
            this.grpTransferSettings = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelTransfer = new System.Windows.Forms.TableLayoutPanel();
            this.lblTransferAttemptCount = new System.Windows.Forms.Label();
            this.numTransferAttemptCount = new System.Windows.Forms.NumericUpDown();
            this.chkAllowDifferentWindowPrice = new System.Windows.Forms.CheckBox();
            this.chkAvoidFirmRollerWindows = new System.Windows.Forms.CheckBox();
            this.lblTransferRules = new System.Windows.Forms.Label();
            this.flowLayoutPanelButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.tableLayoutPanelMain.SuspendLayout();
            this.grpTransferSettings.SuspendLayout();
            this.tableLayoutPanelTransfer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTransferAttemptCount)).BeginInit();
            this.flowLayoutPanelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 1;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Controls.Add(this.lblConfirmation, 0, 0);
            this.tableLayoutPanelMain.Controls.Add(this.chkTryTransferFailedIssues, 0, 1);
            this.tableLayoutPanelMain.Controls.Add(this.grpTransferSettings, 0, 2);
            this.tableLayoutPanelMain.Controls.Add(this.flowLayoutPanelButtons, 0, 3);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.Padding = new System.Windows.Forms.Padding(8);
            this.tableLayoutPanelMain.RowCount = 4;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(640, 390);
            this.tableLayoutPanelMain.TabIndex = 0;
            // 
            // lblConfirmation
            // 
            this.lblConfirmation.AutoSize = true;
            this.lblConfirmation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblConfirmation.Location = new System.Drawing.Point(11, 8);
            this.lblConfirmation.Margin = new System.Windows.Forms.Padding(3, 0, 3, 8);
            this.lblConfirmation.Name = "lblConfirmation";
            this.lblConfirmation.Size = new System.Drawing.Size(618, 24);
            this.lblConfirmation.TabIndex = 0;
            this.lblConfirmation.Text = "Активировать акцию с выбранными параметрами?";
            // 
            // chkTryTransferFailedIssues
            // 
            this.chkTryTransferFailedIssues.AutoSize = true;
            this.chkTryTransferFailedIssues.Location = new System.Drawing.Point(11, 43);
            this.chkTryTransferFailedIssues.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.chkTryTransferFailedIssues.Name = "chkTryTransferFailedIssues";
            this.chkTryTransferFailedIssues.Size = new System.Drawing.Size(571, 28);
            this.chkTryTransferFailedIssues.TabIndex = 1;
            this.chkTryTransferFailedIssues.Text = "Пытаться переносить выпуски, которые не удалось активировать";
            this.chkTryTransferFailedIssues.UseVisualStyleBackColor = true;
            this.chkTryTransferFailedIssues.CheckedChanged += new System.EventHandler(this.ChkTryTransferFailedIssues_CheckedChanged);
            // 
            // grpTransferSettings
            // 
            this.grpTransferSettings.Controls.Add(this.tableLayoutPanelTransfer);
            this.grpTransferSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTransferSettings.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.grpTransferSettings.Location = new System.Drawing.Point(11, 82);
            this.grpTransferSettings.Name = "grpTransferSettings";
            this.grpTransferSettings.Size = new System.Drawing.Size(618, 204);
            this.grpTransferSettings.TabIndex = 2;
            this.grpTransferSettings.TabStop = false;
            this.grpTransferSettings.Text = "Параметры переноса";
            // 
            // tableLayoutPanelTransfer
            // 
            this.tableLayoutPanelTransfer.ColumnCount = 2;
            this.tableLayoutPanelTransfer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tableLayoutPanelTransfer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanelTransfer.Controls.Add(this.lblTransferAttemptCount, 0, 0);
            this.tableLayoutPanelTransfer.Controls.Add(this.numTransferAttemptCount, 1, 0);
            this.tableLayoutPanelTransfer.Controls.Add(this.chkAllowDifferentWindowPrice, 0, 1);
            this.tableLayoutPanelTransfer.Controls.Add(this.chkAvoidFirmRollerWindows, 0, 2);
            this.tableLayoutPanelTransfer.Controls.Add(this.lblTransferRules, 0, 3);
            this.tableLayoutPanelTransfer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelTransfer.Location = new System.Drawing.Point(3, 27);
            this.tableLayoutPanelTransfer.Name = "tableLayoutPanelTransfer";
            this.tableLayoutPanelTransfer.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanelTransfer.RowCount = 4;
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelTransfer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelTransfer.Size = new System.Drawing.Size(612, 174);
            this.tableLayoutPanelTransfer.TabIndex = 0;
            // 
            // lblTransferAttemptCount
            // 
            this.lblTransferAttemptCount.AutoSize = true;
            this.lblTransferAttemptCount.Location = new System.Drawing.Point(9, 11);
            this.lblTransferAttemptCount.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            this.lblTransferAttemptCount.Name = "lblTransferAttemptCount";
            this.lblTransferAttemptCount.Size = new System.Drawing.Size(290, 24);
            this.lblTransferAttemptCount.TabIndex = 0;
            this.lblTransferAttemptCount.Text = "Количество попыток поиска окна:";
            // 
            // numTransferAttemptCount
            // 
            this.numTransferAttemptCount.Location = new System.Drawing.Point(429, 9);
            this.numTransferAttemptCount.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numTransferAttemptCount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTransferAttemptCount.Name = "numTransferAttemptCount";
            this.numTransferAttemptCount.Size = new System.Drawing.Size(86, 31);
            this.numTransferAttemptCount.TabIndex = 1;
            this.numTransferAttemptCount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // chkAllowDifferentWindowPrice
            // 
            this.chkAllowDifferentWindowPrice.AutoSize = true;
            this.tableLayoutPanelTransfer.SetColumnSpan(this.chkAllowDifferentWindowPrice, 2);
            this.chkAllowDifferentWindowPrice.Location = new System.Drawing.Point(9, 46);
            this.chkAllowDifferentWindowPrice.Name = "chkAllowDifferentWindowPrice";
            this.chkAllowDifferentWindowPrice.Size = new System.Drawing.Size(384, 28);
            this.chkAllowDifferentWindowPrice.TabIndex = 2;
            this.chkAllowDifferentWindowPrice.Text = "Разрешить перенос в окно с другой ценой";
            this.chkAllowDifferentWindowPrice.UseVisualStyleBackColor = true;
            //
            // chkAvoidFirmRollerWindows
            //
            this.chkAvoidFirmRollerWindows.AutoSize = true;
            this.tableLayoutPanelTransfer.SetColumnSpan(this.chkAvoidFirmRollerWindows, 2);
            this.chkAvoidFirmRollerWindows.Checked = true;
            this.chkAvoidFirmRollerWindows.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAvoidFirmRollerWindows.Location = new System.Drawing.Point(9, 80);
            this.chkAvoidFirmRollerWindows.Name = "chkAvoidFirmRollerWindows";
            this.chkAvoidFirmRollerWindows.Size = new System.Drawing.Size(470, 28);
            this.chkAvoidFirmRollerWindows.TabIndex = 3;
            this.chkAvoidFirmRollerWindows.Text = "Не использовать окна, где есть ролики данной фирмы";
            this.chkAvoidFirmRollerWindows.UseVisualStyleBackColor = true;
            //
            // lblTransferRules
            // 
            this.lblTransferRules.AutoSize = false;
            this.tableLayoutPanelTransfer.SetColumnSpan(this.lblTransferRules, 2);
            this.lblTransferRules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTransferRules.Location = new System.Drawing.Point(9, 114);
            this.lblTransferRules.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.lblTransferRules.Name = "lblTransferRules";
            this.lblTransferRules.Size = new System.Drawing.Size(594, 54);
            this.lblTransferRules.TabIndex = 4;
            this.lblTransferRules.Text = "Поиск выполняется только в рамках того же дня. Позиция выпуска сохраняется без из" +
    "менений.";
            // 
            // flowLayoutPanelButtons
            // 
            this.flowLayoutPanelButtons.Controls.Add(this.btnCancel);
            this.flowLayoutPanelButtons.Controls.Add(this.btnOK);
            this.flowLayoutPanelButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanelButtons.Location = new System.Drawing.Point(11, 292);
            this.flowLayoutPanelButtons.Name = "flowLayoutPanelButtons";
            this.flowLayoutPanelButtons.Size = new System.Drawing.Size(618, 42);
            this.flowLayoutPanelButtons.TabIndex = 3;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(515, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 33);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOK.Location = new System.Drawing.Point(409, 3);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 33);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "ОК";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // ActionActivateSettingsForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(640, 390);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ActionActivateSettingsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Параметры активации";
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelMain.PerformLayout();
            this.grpTransferSettings.ResumeLayout(false);
            this.tableLayoutPanelTransfer.ResumeLayout(false);
            this.tableLayoutPanelTransfer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTransferAttemptCount)).EndInit();
            this.flowLayoutPanelButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
