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
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(179, 413);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 22);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(91, 413);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 22);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Ok";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // tvwStructure
            // 
            this.tvwStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvwStructure.CheckBoxes = false;
            this.tvwStructure.DependantGrid = null;
            this.tvwStructure.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tvwStructure.Location = new System.Drawing.Point(0, 0);
            this.tvwStructure.Name = "tvwStructure";
            this.tvwStructure.SelectedItemsBitColumn = null;
            this.tvwStructure.SelectedItemsImageColumn = null;
            this.tvwStructure.Size = new System.Drawing.Size(350, 400);
            this.tvwStructure.TabIndex = 0;
            // 
            // TreeViewSelector
            // 
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(350, 443);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.tvwStructure);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "TreeViewSelector";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Выбор объекта";
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
					MessageBox.ShowExclamation(MessageAccessor.GetMessage("NoObjectSelected"));
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