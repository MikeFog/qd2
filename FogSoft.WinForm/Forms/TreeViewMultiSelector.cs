using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;

namespace FogSoft.WinForm.Forms
{
	public class TreeViewMultiSelector : Form
	{
		public delegate bool IsAllowedNodeCallback(IObjectContainer container);

		private readonly IsAllowedNodeCallback isAllowedChecker;

		#region Members ---------------------------------------

		private TreeView2 tvwStructure;
		private PresentationObject selectedObject;
		private Button btnCancel;
		private Button btnOk;
		private SmartGrid grdSelected;
		private Container components = null;

		#endregion

		#region Constructors ----------------------------------

		public TreeViewMultiSelector()
		{
			InitializeComponent();
		}

		public TreeViewMultiSelector(
			RelationScenario scenario, string caption, IsAllowedNodeCallback isAllowedChecker,
			DataTable selected)
			: this()
		{
			Text = (String.IsNullOrEmpty(caption)) ? scenario.Name : caption;
			FakeContainer container = new FakeContainer(Text, null, scenario);
			this.isAllowedChecker = isAllowedChecker;
			tvwStructure.Root = container;
			grdSelected.Entity = container.ChildEntity;
			if(selected != null)
			{
				grdSelected.DataSource = selected.Copy().DefaultView;
				// disable primary key constraint
				DisableConstraints(grdSelected.DataSource, container.ChildEntity);
				// grdSelected.DataSource.Table.DataSet.EnforceConstraints = false;
				foreach (DataRow row in selected.Rows)
				{
					grdSelected.Added2Checked.Add(new PresentationObject(grdSelected.Entity, row));
				}
			}
		}

		public TreeViewMultiSelector(RelationScenario scenario, string caption)
			: this(scenario, caption, IsAllowedNode, null) {}

		public TreeViewMultiSelector(RelationScenario scenario, string caption, DataTable selected)
			: this(scenario, caption, IsAllowedNode, selected) {}

		//public TreeViewSelector(RelationScenario scenario, string caption, bool checkBoxes, 
		//  DataTable dtSelected) : this(scenario, caption) {
		//  this.tvwStructure.CheckBoxes = checkBoxes;
		//  this.tvwStructure.SelectedObjects = dtSelected;
		//}

		#endregion

		private static bool IsAllowedNode(IObjectContainer container)
		{
			return !container.IsChildNodeExpandable;
		}

		private bool _constraintsCleared;
		private void DisableConstraints(DataView dataView, Entity gridEntity)
		{
			if (_constraintsCleared || dataView == null || dataView.Table == null)
				return;
			if (dataView.Table.Constraints.Count > 0)
				dataView.Table.Constraints.Clear();
			foreach (DataColumn column in dataView.Table.Columns)
				column.AllowDBNull = true;
			// add new PK
			DataColumn[] pkColumns = new DataColumn[gridEntity.PKColumns.Length];
			for (int i = 0; i < gridEntity.PKColumns.Length; i++)
				pkColumns[i] = dataView.Table.Columns[gridEntity.PKColumns[i]];
			dataView.Table.Constraints.Add(new UniqueConstraint(pkColumns, true));
			_constraintsCleared = true;
		}

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
            this.grdSelected = new FogSoft.WinForm.Controls.SmartGrid();
            this.tvwStructure = new FogSoft.WinForm.Controls.TreeView2();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
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
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(91, 413);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 22);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Ok";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // grdSelected
            // 
            this.grdSelected.Caption = "";
            this.grdSelected.CaptionVisible = false;
            this.grdSelected.CheckBoxes = true;
            this.grdSelected.ColumnNameHighlight = null;
            this.grdSelected.DataSource = null;
            this.grdSelected.DependantGrid = null;
            this.grdSelected.Entity = null;
            this.grdSelected.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdSelected.InterfaceObject = FogSoft.WinForm.InterfaceObjects.Selector;
            this.grdSelected.IsHighlightInvertColor = false;
            this.grdSelected.IsNeedHighlight = false;
            this.grdSelected.Location = new System.Drawing.Point(0, 279);
            this.grdSelected.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdSelected.MenuEnabled = false;
            this.grdSelected.Name = "grdSelected";
            this.grdSelected.Padding = new System.Windows.Forms.Padding(5);
            this.grdSelected.QuickSearchVisible = false;
            this.grdSelected.SelectedObject = null;
            this.grdSelected.Size = new System.Drawing.Size(350, 128);
            this.grdSelected.TabIndex = 7;
            this.grdSelected.ObjectChecked += new FogSoft.WinForm.ObjectCheckedDelegate(this.grdSelected_Checked);
            // 
            // tvwStructure
            // 
            this.tvwStructure.CheckBoxes = false;
            this.tvwStructure.DependantGrid = null;
            this.tvwStructure.Dock = System.Windows.Forms.DockStyle.Top;
            this.tvwStructure.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tvwStructure.Location = new System.Drawing.Point(5, 5);
            this.tvwStructure.Name = "tvwStructure";
            this.tvwStructure.SelectedItemsBitColumn = null;
            this.tvwStructure.SelectedItemsImageColumn = null;
            this.tvwStructure.Size = new System.Drawing.Size(340, 272);
            this.tvwStructure.TabIndex = 0;
            this.tvwStructure.ContainerSelected += new FogSoft.WinForm.ContainerDelegate(this.tvwStructure_ContainerSelected);
            // 
            // TreeViewMultiSelector
            // 
            this.ClientSize = new System.Drawing.Size(350, 443);
            this.Controls.Add(this.grdSelected);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.tvwStructure);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TreeViewMultiSelector";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.ShowInTaskbar = false;
            this.Text = "Выбор объекта";
            this.ResumeLayout(false);

		}

		#endregion

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
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

		public List<PresentationObject> SelectedObjects
		{
			get { return grdSelected.Added2Checked; }
		}

		[Browsable(false)]
		public List<PresentationObject> DeletedItems
		{
			get { return tvwStructure.DeletedItems; }
		}

		private void tvwStructure_ContainerSelected(IObjectContainer container)
		{
			//tvwStructure.CurrentObject
			if(tvwStructure.CurrentObject != null)
			{
				if(isAllowedChecker(container))
				{
					PresentationObject node = container as PresentationObject;
					DisableConstraints(grdSelected.DataSource, grdSelected.Entity);
					if(!grdSelected.Added2Checked.Contains(node))
					{
						Globals.SetParameterValue(node.Parameters, SmartGrid.COL_IsSelected, true);
						grdSelected.AddRow(node);
						grdSelected.Added2Checked.Add(node);
						grdSelected.InternalGrid.CurrentRow.Cells[0].Value = true;
					}
				}
			}
		}

		private void grdSelected_Checked(PresentationObject presentationObject, bool state)
		{
			if(presentationObject != null)
			{
				if (grdSelected.Added2Checked.Contains(presentationObject) && !state)
				{
					grdSelected.DeleteRow(presentationObject);
					grdSelected.Added2Checked.Remove(presentationObject);
				}
			}
		}
	}
}