namespace Protector.Forms
{
    partial class SetUsersDiscount
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.dtStartDate = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.dtFinishDate = new System.Windows.Forms.DateTimePicker();
            this.numRatio = new System.Windows.Forms.NumericUpDown();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.button2 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.grdUser = new FogSoft.WinForm.Controls.SmartGrid();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRatio)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.dtStartDate, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.dtFinishDate, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.numRatio, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(757, 633);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Дата начала:";
            // 
            // dtStartDate
            // 
            this.dtStartDate.Location = new System.Drawing.Point(230, 3);
            this.dtStartDate.Name = "dtStartDate";
            this.dtStartDate.Size = new System.Drawing.Size(200, 31);
            this.dtStartDate.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(147, 25);
            this.label2.TabIndex = 2;
            this.label2.Text = "Дата окончания:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(126, 25);
            this.label3.TabIndex = 3;
            this.label3.Text = "Коэффициент:";
            // 
            // dtFinishDate
            // 
            this.dtFinishDate.Location = new System.Drawing.Point(230, 40);
            this.dtFinishDate.Name = "dtFinishDate";
            this.dtFinishDate.Size = new System.Drawing.Size(200, 31);
            this.dtFinishDate.TabIndex = 4;
            // 
            // numRatio
            // 
            this.numRatio.DecimalPlaces = 2;
            this.numRatio.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numRatio.Location = new System.Drawing.Point(230, 77);
            this.numRatio.Name = "numRatio";
            this.numRatio.Size = new System.Drawing.Size(200, 31);
            this.numRatio.TabIndex = 5;
            this.numRatio.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 2);
            this.flowLayoutPanel1.Controls.Add(this.button2);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 594);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(751, 36);
            this.flowLayoutPanel1.TabIndex = 6;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(652, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(96, 30);
            this.button2.TabIndex = 1;
            this.button2.Text = "Ok";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // panel1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panel1, 2);
            this.panel1.Controls.Add(this.grdUser);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 114);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(751, 474);
            this.panel1.TabIndex = 7;
            // 
            // grdUser
            // 
            this.grdUser.Caption = "";
            this.grdUser.CaptionVisible = false;
            this.grdUser.CheckBoxes = true;
            this.grdUser.ColumnNameHighlight = null;
            this.grdUser.DataSource = null;
            this.grdUser.DependantGrid = null;
            this.grdUser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdUser.Entity = null;
            this.grdUser.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdUser.IsHighlightInvertColor = false;
            this.grdUser.IsNeedHighlight = false;
            this.grdUser.Location = new System.Drawing.Point(0, 0);
            this.grdUser.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdUser.MenuEnabled = true;
            this.grdUser.Name = "grdUser";
            this.grdUser.QuickSearchVisible = true;
            this.grdUser.SelectedObject = null;
            this.grdUser.ShowMultiselectColumn = true;
            this.grdUser.Size = new System.Drawing.Size(751, 474);
            this.grdUser.TabIndex = 0;
            // 
            // SetUsersDiscount
            // 
            this.AcceptButton = this.button2;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(757, 633);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetUsersDiscount";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Скидки пользователей";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRatio)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtStartDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker dtFinishDate;
        private System.Windows.Forms.NumericUpDown numRatio;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Panel panel1;
        private FogSoft.WinForm.Controls.SmartGrid grdUser;
    }
}