namespace Merlin.Forms
{
    partial class ChangePositioningForm
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
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).BeginInit();
            this.SuspendLayout();
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(483, 522);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(396, 522);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(309, 522);
            // 
            // tabPassport
            // 
            this.tabPassport.Size = new System.Drawing.Size(558, 512);
            // 
            // ChangePositioningForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(566, 553);
            this.Name = "ChangePositioningForm";
            this.Text = "Изменить позиционирование";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}