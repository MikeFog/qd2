namespace FogSoft.WinForm.Controls
{
    partial class TreeView2
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TreeView tvStructure;
        private System.Windows.Forms.ImageList imlTreeView;
        private System.Windows.Forms.Button btnToggleExpand;
        private System.Windows.Forms.Panel panelContainer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tvStructure = new System.Windows.Forms.TreeView();
            this.imlTreeView = new System.Windows.Forms.ImageList(this.components);
            this.btnToggleExpand = new System.Windows.Forms.Button();
            this.panelContainer = new System.Windows.Forms.Panel();
            this.panelContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // tvStructure
            // 
            this.tvStructure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvStructure.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tvStructure.HideSelection = false;
            this.tvStructure.ImageIndex = 0;
            this.tvStructure.ImageList = this.imlTreeView;
            this.tvStructure.Location = new System.Drawing.Point(0, 30);
            this.tvStructure.Name = "tvStructure";
            this.tvStructure.SelectedImageIndex = 0;
            this.tvStructure.Size = new System.Drawing.Size(211, 224);
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
            // btnToggleExpand
            // 
            this.btnToggleExpand.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnToggleExpand.Location = new System.Drawing.Point(0, 0);
            this.btnToggleExpand.Name = "btnToggleExpand";
            this.btnToggleExpand.Size = new System.Drawing.Size(211, 30);
            this.btnToggleExpand.TabIndex = 1;
            this.btnToggleExpand.Text = "Раскрыть всё";
            this.btnToggleExpand.Click += new System.EventHandler(this.BtnToggleExpand_Click);
            // 
            // panelContainer
            // 
            this.panelContainer.Controls.Add(this.tvStructure);
            this.panelContainer.Controls.Add(this.btnToggleExpand);
            this.panelContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContainer.Location = new System.Drawing.Point(0, 0);
            this.panelContainer.Name = "panelContainer";
            this.panelContainer.Size = new System.Drawing.Size(211, 254);
            this.panelContainer.TabIndex = 2;
            // 
            // TreeView2
            // 
            this.Controls.Add(this.panelContainer);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "TreeView2";
            this.Size = new System.Drawing.Size(211, 254);
            this.panelContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}