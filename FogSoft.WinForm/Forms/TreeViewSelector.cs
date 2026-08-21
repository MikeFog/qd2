using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Forms
{
	public class TreeViewSelector : Form
	{
		#region Members ---------------------------------------

		private TreeView2 tvwStructure;
		private PresentationObject selectedObject;
		private Button btnCancel;
		private Button btnOk;
		private TableLayoutPanel tableLayoutPanel1;
		private FlowLayoutPanel flowLayoutPanel1;
		private Container components = null;

		#endregion

		#region Constructors ----------------------------------

		public TreeViewSelector()
		{
			InitializeComponent();
		}

		public TreeViewSelector(RelationScenario scenario)
			: this()
		{
			FakeContainer container = new FakeContainer(scenario.Name, null, scenario);
			tvwStructure.Root = container;
		}

		public TreeViewSelector(RelationScenario scenario, string caption)
			: this(scenario)
		{
			Text = caption;
		}

		public TreeViewSelector(
			RelationScenario scenario, string caption, bool checkBoxes,
			DataTable dtSelected)
			: this(scenario, caption)
		{
			tvwStructure.CheckBoxes = checkBoxes;
			tvwStructure.SelectedObjects = dtSelected;
		}

		public string SelectedItemsImageColumn
		{
			set { tvwStructure.SelectedItemsImageColumn = value; }
		}

		public string SelectedItemsBitColumn
		{
			set { tvwStructure.SelectedItemsBitColumn = value; }
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.tvwStructure = new FogSoft.WinForm.Controls.TreeView2();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(230, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 33);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(124, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 33);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Ok";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // tvwStructure
            // 
            this.tvwStructure.CheckBoxes = false;
            this.tvwStructure.DependantGrid = null;
            this.tvwStructure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvwStructure.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tvwStructure.Location = new System.Drawing.Point(15, 15);
            this.tvwStructure.Name = "tvwStructure";
            this.tvwStructure.SelectedItemsBitColumn = null;
            this.tvwStructure.SelectedItemsImageColumn = null;
            this.tvwStructure.ShowExpandButton = true;
            this.tvwStructure.Size = new System.Drawing.Size(333, 373);
            this.tvwStructure.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tvwStructure, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(12);
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(545, 665);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnOk);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(15, 391);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(333, 39);
            this.flowLayoutPanel1.TabIndex = 7;
            // 
            // TreeViewSelector
            // 
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(545, 665);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "TreeViewSelector";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Выбор объекта";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				if (!tvwStructure.CheckBoxes && tvwStructure.CurrentObject == null)
				{
					DialogResult = DialogResult.None;
					UserMessage.ShowExclamation(MessageAccessor.GetMessage("NoObjectSelected"));
					return;
				}
				selectedObject = tvwStructure.CurrentObject;
				DialogResult = DialogResult.OK;
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		[Browsable(false)]
		public PresentationObject SelectedObject
		{
			get { return selectedObject; }
		}

		[Browsable(false)]
		public List<PresentationObject> AddedItems
		{
			get { return tvwStructure.AddedItems; }
		}

		[Browsable(false)]
		public List<PresentationObject> DeletedItems
		{
			get { return tvwStructure.DeletedItems; }
		}
	}
}