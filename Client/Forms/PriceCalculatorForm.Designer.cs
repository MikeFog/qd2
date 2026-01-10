// PriceCalculatorForm.Designer.cs
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
            this.grdPriceCalculator = new Merlin.Controls.PriceCalculatorGrid();
            this.templateEditor = new Merlin.Controls.TemplateEditorControl();
            this.SuspendLayout();
            // 
            // grdPriceCalculator
            // 
            this.grdPriceCalculator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdPriceCalculator.Location = new System.Drawing.Point(0, 403);
            this.grdPriceCalculator.Name = "grdPriceCalculator";
            this.grdPriceCalculator.Size = new System.Drawing.Size(1714, 931);
            this.grdPriceCalculator.SummaryUpdater = null;
            this.grdPriceCalculator.TabIndex = 1;
            // 
            // templateEditor
            // 
            this.templateEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.templateEditor.Dock = System.Windows.Forms.DockStyle.Top;
            this.templateEditor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.templateEditor.Location = new System.Drawing.Point(0, 0);
            this.templateEditor.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.templateEditor.Name = "templateEditor";
            this.templateEditor.Size = new System.Drawing.Size(1714, 403);
            this.templateEditor.TabIndex = 2;
            // 
            // PriceCalculatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1714, 1334);
            this.Controls.Add(this.grdPriceCalculator);
            this.Controls.Add(this.templateEditor);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimumSize = new System.Drawing.Size(1562, 1130);
            this.Name = "PriceCalculatorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Калькулятор кампании";
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.PriceCalculatorGrid grdPriceCalculator;
        private Controls.TemplateEditorControl templateEditor;
    }
}
