// PriceCalculatorForm.Designer.cs
using System.Windows.Forms;

namespace Merlin.Forms
{
    partial class PriceCalculatorForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tpCalc = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.flpSaved = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.txtFirmName = new System.Windows.Forms.TextBox();
            this.cmbAgency = new System.Windows.Forms.ComboBox();
            this.btnSelectFirm = new System.Windows.Forms.Button();
            this.btnDeleteAllChecked = new System.Windows.Forms.Button();
            this.btnCreaateProposal = new System.Windows.Forms.Button();
            this.chkAll = new System.Windows.Forms.CheckBox();
            this.grdPriceCalculator = new Merlin.Controls.PriceCalculatorGrid();
            this.templateEditor = new Merlin.Controls.TemplateEditorControl();
            this.tabControl1.SuspendLayout();
            this.tpCalc.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.pnlTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tpCalc);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 238);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1540, 712);
            this.tabControl1.TabIndex = 5;
            // 
            // tpCalc
            // 
            this.tpCalc.Controls.Add(this.grdPriceCalculator);
            this.tpCalc.Location = new System.Drawing.Point(4, 34);
            this.tpCalc.Name = "tpCalc";
            this.tpCalc.Padding = new System.Windows.Forms.Padding(3);
            this.tpCalc.Size = new System.Drawing.Size(1532, 674);
            this.tpCalc.TabIndex = 0;
            this.tpCalc.Text = "Расчёты";
            this.tpCalc.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.flpSaved);
            this.tabPage2.Controls.Add(this.pnlTop);
            this.tabPage2.Location = new System.Drawing.Point(4, 34);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1532, 674);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Сохранённые варианты";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // flpSaved
            // 
            this.flpSaved.AutoScroll = true;
            this.flpSaved.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpSaved.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpSaved.Location = new System.Drawing.Point(3, 68);
            this.flpSaved.Name = "flpSaved";
            this.flpSaved.Size = new System.Drawing.Size(1526, 603);
            this.flpSaved.TabIndex = 0;
            this.flpSaved.WrapContents = false;
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.button1);
            this.pnlTop.Controls.Add(this.txtFirmName);
            this.pnlTop.Controls.Add(this.cmbAgency);
            this.pnlTop.Controls.Add(this.btnSelectFirm);
            this.pnlTop.Controls.Add(this.btnDeleteAllChecked);
            this.pnlTop.Controls.Add(this.btnCreaateProposal);
            this.pnlTop.Controls.Add(this.chkAll);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(3, 3);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1526, 65);
            this.pnlTop.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Image = global::Merlin.Properties.Resources.Ластик;
            this.button1.Location = new System.Drawing.Point(1300, 16);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(48, 35);
            this.button1.TabIndex = 7;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtFirmName
            // 
            this.txtFirmName.Location = new System.Drawing.Point(894, 20);
            this.txtFirmName.Name = "txtFirmName";
            this.txtFirmName.Size = new System.Drawing.Size(400, 31);
            this.txtFirmName.TabIndex = 6;
            // 
            // cmbAgency
            // 
            this.cmbAgency.FormattingEnabled = true;
            this.cmbAgency.Location = new System.Drawing.Point(491, 16);
            this.cmbAgency.Name = "cmbAgency";
            this.cmbAgency.Size = new System.Drawing.Size(182, 33);
            this.cmbAgency.TabIndex = 5;
            // 
            // btnSelectFirm
            // 
            this.btnSelectFirm.Location = new System.Drawing.Point(679, 16);
            this.btnSelectFirm.Name = "btnSelectFirm";
            this.btnSelectFirm.Size = new System.Drawing.Size(209, 38);
            this.btnSelectFirm.TabIndex = 3;
            this.btnSelectFirm.Text = "Выбрать фирму";
            this.btnSelectFirm.UseVisualStyleBackColor = true;
            this.btnSelectFirm.Click += new System.EventHandler(this.btnSelectFirm_Click);
            // 
            // btnDeleteAllChecked
            // 
            this.btnDeleteAllChecked.Location = new System.Drawing.Point(158, 16);
            this.btnDeleteAllChecked.Name = "btnDeleteAllChecked";
            this.btnDeleteAllChecked.Size = new System.Drawing.Size(202, 38);
            this.btnDeleteAllChecked.TabIndex = 2;
            this.btnDeleteAllChecked.Text = "Удалить отмеченные";
            this.btnDeleteAllChecked.UseVisualStyleBackColor = true;
            // 
            // btnCreaateProposal
            // 
            this.btnCreaateProposal.Location = new System.Drawing.Point(365, 16);
            this.btnCreaateProposal.Name = "btnCreaateProposal";
            this.btnCreaateProposal.Size = new System.Drawing.Size(120, 38);
            this.btnCreaateProposal.TabIndex = 1;
            this.btnCreaateProposal.Text = "Создать КП";
            this.btnCreaateProposal.UseVisualStyleBackColor = true;
            this.btnCreaateProposal.Click += new System.EventHandler(this.btnCreaateProposal_Click);
            // 
            // chkAll
            // 
            this.chkAll.AutoSize = true;
            this.chkAll.Checked = true;
            this.chkAll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAll.Location = new System.Drawing.Point(13, 16);
            this.chkAll.Name = "chkAll";
            this.chkAll.Size = new System.Drawing.Size(139, 29);
            this.chkAll.TabIndex = 0;
            this.chkAll.Text = "Выбрать все";
            this.chkAll.UseVisualStyleBackColor = true;
            // 
            // grdPriceCalculator
            // 
            this.grdPriceCalculator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdPriceCalculator.Location = new System.Drawing.Point(3, 3);
            this.grdPriceCalculator.Margin = new System.Windows.Forms.Padding(0);
            this.grdPriceCalculator.Name = "grdPriceCalculator";
            this.grdPriceCalculator.Size = new System.Drawing.Size(1526, 668);
            this.grdPriceCalculator.SummaryUpdater = null;
            this.grdPriceCalculator.TabIndex = 2;
            this.grdPriceCalculator.UseManagerDiscountPeriods = true;
            // 
            // templateEditor
            // 
            this.templateEditor.AutoSize = true;
            this.templateEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.templateEditor.BackColor = System.Drawing.SystemColors.Control;
            this.templateEditor.Dock = System.Windows.Forms.DockStyle.Top;
            this.templateEditor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.templateEditor.Location = new System.Drawing.Point(0, 0);
            this.templateEditor.Margin = new System.Windows.Forms.Padding(0);
            this.templateEditor.MinimumSize = new System.Drawing.Size(1200, 0);
            this.templateEditor.Name = "templateEditor";
            this.templateEditor.Size = new System.Drawing.Size(1540, 238);
            this.templateEditor.TabIndex = 4;
            // 
            // PriceCalculatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1540, 950);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.templateEditor);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimumSize = new System.Drawing.Size(1562, 1006);
            this.Name = "PriceCalculatorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Калькулятор кампании";
            this.tabControl1.ResumeLayout(false);
            this.tpCalc.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Controls.TemplateEditorControl templateEditor;
        private TabControl tabControl1;
        private TabPage tpCalc;
        private Controls.PriceCalculatorGrid grdPriceCalculator;
        private TabPage tabPage2;
        private FlowLayoutPanel flpSaved;
        private Panel pnlTop;
        private CheckBox chkAll;
        private Button btnCreaateProposal;
        private Button btnDeleteAllChecked;
        private Button btnSelectFirm;
        private ComboBox cmbAgency;
        private TextBox txtFirmName;
        private Button button1;
    }
}
