using System.Drawing;

namespace FogSoft.WinForm.Controls {
  partial class ObjectPicker2 {
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectPicker2));
            this.txtObjectName = new System.Windows.Forms.TextBox();
            this.btnSelect = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnCreateNew = new System.Windows.Forms.Button();
            this.ttOjectPicker = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // txtObjectName
            // 
            this.txtObjectName.BackColor = System.Drawing.Color.White;
            this.txtObjectName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtObjectName.Location = new System.Drawing.Point(0, 0);
            this.txtObjectName.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtObjectName.Name = "txtObjectName";
            this.txtObjectName.ReadOnly = true;
            this.txtObjectName.Size = new System.Drawing.Size(463, 23);
            this.txtObjectName.TabIndex = 0;
            // 
            // btnSelect
            // 
            this.btnSelect.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSelect.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnSelect.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnSelect.Image = ((System.Drawing.Image)(resources.GetObject("btnSelect.Image")));
            this.btnSelect.Location = new System.Drawing.Point(463, 0);
            this.btnSelect.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(28, 25);
            this.btnSelect.TabIndex = 7;
            this.btnSelect.Text = "...";
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // btnClear
            // 
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnClear.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnClear.Image = ((System.Drawing.Image)(resources.GetObject("btnClear.Image")));
            this.btnClear.Location = new System.Drawing.Point(491, 0);
            this.btnClear.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(28, 25);
            this.btnClear.TabIndex = 6;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnCreateNew
            // 
            this.btnCreateNew.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCreateNew.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnCreateNew.Image = global::FogSoft.WinForm.Properties.Resources.NewItem;
            this.btnCreateNew.Location = new System.Drawing.Point(519, 0);
            this.btnCreateNew.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCreateNew.Name = "btnCreateNew";
            this.btnCreateNew.Size = new System.Drawing.Size(28, 25);
            this.btnCreateNew.TabIndex = 8;
            this.btnCreateNew.Click += new System.EventHandler(this.btnCreateNew_Click);
            // 
            // ObjectPicker2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtObjectName);
            this.Controls.Add(this.btnSelect);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnCreateNew);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "ObjectPicker2";
            this.Size = new System.Drawing.Size(547, 25);
            this.Resize += new System.EventHandler(this.ObjectPicker2_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox txtObjectName;
    private System.Windows.Forms.Button btnSelect;
    private System.Windows.Forms.Button btnClear;
    private System.Windows.Forms.Button btnCreateNew;
    private System.Windows.Forms.ToolTip ttOjectPicker;

  }
}
