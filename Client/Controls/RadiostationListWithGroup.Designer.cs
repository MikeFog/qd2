namespace Merlin.Controls
{
    partial class RadiostationListWithGroup
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cmbRadioStationGroup = new FogSoft.WinForm.LookUp();
            this.grdRadiostations = new FogSoft.WinForm.Controls.SmartGrid();
            this.SuspendLayout();
            // 
            // cmbRadioStationGroup
            // 
            this.cmbRadioStationGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbRadioStationGroup.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbRadioStationGroup.IsNullable = false;
            this.cmbRadioStationGroup.Location = new System.Drawing.Point(0, 0);
            this.cmbRadioStationGroup.Name = "cmbRadioStationGroup";
            this.cmbRadioStationGroup.SelectedIndex = -1;
            this.cmbRadioStationGroup.SelectedValue = null;
            this.cmbRadioStationGroup.Size = new System.Drawing.Size(497, 33);
            this.cmbRadioStationGroup.TabIndex = 0;
            this.cmbRadioStationGroup.SelectedItemChanged += new System.EventHandler(this.CmbGroup_SelectedItemChanged);
            // 
            // grdRadiostations
            // 
            this.grdRadiostations.Caption = "";
            this.grdRadiostations.CaptionVisible = false;
            this.grdRadiostations.CheckBoxes = true;
            this.grdRadiostations.ColumnNameHighlight = null;
            this.grdRadiostations.DataSource = null;
            this.grdRadiostations.DependantGrid = null;
            this.grdRadiostations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdRadiostations.Entity = null;
            this.grdRadiostations.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdRadiostations.IsHighlightInvertColor = false;
            this.grdRadiostations.IsNeedHighlight = false;
            this.grdRadiostations.Location = new System.Drawing.Point(0, 33);
            this.grdRadiostations.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdRadiostations.MenuEnabled = false;
            this.grdRadiostations.Name = "grdRadiostations";
            this.grdRadiostations.QuickSearchVisible = false;
            this.grdRadiostations.SelectedObject = null;
            this.grdRadiostations.ShowMultiselectColumn = true;
            this.grdRadiostations.Size = new System.Drawing.Size(497, 640);
            this.grdRadiostations.TabIndex = 1;
            // 
            // RadiostationListWithGroup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grdRadiostations);
            this.Controls.Add(this.cmbRadioStationGroup);
            this.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "RadiostationListWithGroup";
            this.Size = new System.Drawing.Size(497, 673);
            this.ResumeLayout(false);

        }

        #endregion

        private FogSoft.WinForm.LookUp cmbRadioStationGroup;
        private FogSoft.WinForm.Controls.SmartGrid grdRadiostations;
    }
}
