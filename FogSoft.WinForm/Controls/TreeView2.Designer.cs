namespace FogSoft.WinForm.Controls {
  partial class TreeView2 {
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
            this.tvStructure = new System.Windows.Forms.TreeView();
            this.imlTreeView = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // tvStructure
            // 
            this.tvStructure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvStructure.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tvStructure.HideSelection = false;
            this.tvStructure.ImageIndex = 0;
            this.tvStructure.ImageList = this.imlTreeView;
            this.tvStructure.Location = new System.Drawing.Point(0, 0);
            this.tvStructure.Name = "tvStructure";
            this.tvStructure.SelectedImageIndex = 0;
            this.tvStructure.Size = new System.Drawing.Size(211, 254);
            this.tvStructure.TabIndex = 0;
            this.tvStructure.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.TvStructure_AfterCheck);
            this.tvStructure.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.TvStructure_BeforeExpand);
            this.tvStructure.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TvStructure_AfterSelect);
            this.tvStructure.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TvStructure_MouseDown);
            // 
            // imlTreeView
            // 
            this.imlTreeView.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imlTreeView.ImageSize = new System.Drawing.Size(16, 16);
            this.imlTreeView.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // TreeView2
            // 
            this.Controls.Add(this.tvStructure);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "TreeView2";
            this.Size = new System.Drawing.Size(211, 254);
            this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TreeView tvStructure;
    private System.Windows.Forms.ImageList imlTreeView;
  }
}
