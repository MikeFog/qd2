using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Controls
{
    public partial class TreeView2 : UserControl, IObjectControl
	{
		#region Events ----------------------------------------

		public event ContainerDelegate ContainerSelected;
		public event ObjectDelegate ObjectCreated;
        //public event ObjectDelegate ObjectDeleted;
        //public event ObjectDelegate ObjectChanged;

        #endregion
        private bool isExpanded = false;
        private const string FAKE_NODE = "fake node";
		private readonly Dictionary<string, int> iconResolver = new Dictionary<string, int>();
		private SmartGrid dependantGrid;
		private DataTable dtSelected;
		private bool skipCheckBoxEvent;
		private readonly List<PresentationObject> addedObjects = new List<PresentationObject>();
		private readonly List<PresentationObject> deletedObjects = new List<PresentationObject>();

		private readonly List<object> addedIDs = new List<object>();
		private readonly List<object> deletedIDs = new List<object>();

		public TreeView2()
		{
			InitializeComponent();
		}

        private void BtnToggleExpand_Click(object sender, EventArgs e)
        {
            try
            {
                ParentForm.Cursor = Cursors.WaitCursor;

                if (isExpanded)
                {
                    // Сворачиваем все узлы
                    tvStructure.CollapseAll();
                    btnToggleExpand.Text = "Раскрыть всё";
                    isExpanded = false;
                }
                else
                {
                    // Разворачиваем все узлы
                    tvStructure.ExpandAll();
                    btnToggleExpand.Text = "Схлопнуть всё";
                    isExpanded = true;
                }
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                ParentForm.Cursor = Cursors.Default;
            }
        }

        [Browsable(false)]
		public FakeContainer Root
		{
			set
			{
				TreeNode node = tvStructure.Nodes.Add(value.Name);
				node.Tag = value;
				node.Checked = true;
				AddFakeChildNode(node);
				tvStructure.SelectedNode = node;
				node.Expand();
			}
		}

		public bool ShowExpandButton
		{
			get { return btnToggleExpand.Visible; }
			set { btnToggleExpand.Visible = value; }
        }

        [Browsable(false)]
		public PresentationObject CurrentObject
		{
			get { return SelectedNodeTag as PresentationObject; }
		}

		public void InitFixedTree(DataTable dtSource, string id, string parentid, string name)
		{
			
			idColumn = id;
			parentIDColumn = parentid;
			nameColumn = name;
			ShowData(dtSource);
		}

		public void ShowData(DataTable dtSource)
		{
            DataSource = dtSource;
            DataRow[] childs = DataSource.Select(string.Format("{0} is null", parentIDColumn));
			tvStructure.Nodes.Clear();
            TreeNode root = tvStructure.Nodes.Add("Все");
            foreach (DataRow row in childs)
            {
                TreeNode node = root.Nodes.Add(row[nameColumn].ToString());
                node.Tag = row[idColumn].ToString();
                if (row[SelectedItemsImageColumn] != null)
                {
                    string imageName = row[SelectedItemsImageColumn].ToString();
                    node.BackColor = Color.Transparent;
                    node.ImageIndex = node.SelectedImageIndex = ResolveImageIndex(imageName);
                }
                AddChildsForNode(node);
            }
            root.Expand();
        }

		private void AddChildsForNode(TreeNode node)
		{
			DataRow[] subChilds = DataSource.Select(string.Format("{0} = '{1}'", parentIDColumn, node.Tag));
			foreach (DataRow child in subChilds)
			{
				TreeNode treeNode = node.Nodes.Add(child[nameColumn].ToString());
				treeNode.Tag = child[idColumn];
				if (child[SelectedItemsImageColumn] != null)
				{
					string imageName = child[SelectedItemsImageColumn].ToString();
					treeNode.BackColor = Color.Transparent;
					treeNode.ImageIndex = treeNode.SelectedImageIndex = ResolveImageIndex(imageName);
				}
			}
		}

		public DataTable DataSource { get; private set; }

		private string idColumn;
		private string parentIDColumn;
		private string nameColumn;

		public string SelectedItemsImageColumn { get; set; }

		public string SelectedItemsBitColumn { get; set; }

		[Browsable(false)]
		public SmartGrid DependantGrid
		{
			get { return dependantGrid; }
			set
			{
				dependantGrid = value;
				if (dependantGrid != null)
				{
					dependantGrid.ObjectDeleted += OnGridObjectDeleted;
					dependantGrid.EntityParentChanged += OnParentChanged;
					dependantGrid.RebuildCurrentNode += RebuildTree;
                    dependantGrid.RebuildTree += RebuildTree;
                }
			}
		}

		public bool CheckBoxes
		{
			get { return tvStructure.CheckBoxes; }
			set { tvStructure.CheckBoxes = value; }
		}

		//TODO: Change later
		[Browsable(false)]
		public List<PresentationObject> AddedItems
		{
			get { return addedObjects; }
		}

		//TODO: Change later
		[Browsable(false)]
		public List<PresentationObject> DeletedItems
		{
			get { return deletedObjects; }
		}

		[Browsable(false)]
		public List<object> AddedIDs
		{
			get { return addedIDs; }
		}

		[Browsable(false)]
		public List<object> DeletedIDs
		{
			get { return deletedIDs; }
		}

		private void TvStructure_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			if (DataSource != null)
				return;
			try
			{
				Application.DoEvents();
				ParentForm.Cursor = Cursors.WaitCursor;
				
				if (ChildNodeIsFakeNode(e.Node))
				{
					RemoveFakeChildNode(e.Node);

                    if (e.Node.Tag is IObjectContainer objContainer)
                    {
                        int imageIndex = ResolveImageIndex(objContainer.ChildEntity.IconName);

                        foreach (PresentationObject po in objContainer)
                        {
                            int imageCurrentIndex = imageIndex;
                            string imgName = dtSelected == null ? null : TryGetSelectedImage(po);
                            if (!string.IsNullOrEmpty(imgName))
                                imageCurrentIndex = ResolveImageIndex(imgName);
                            AddObject2Node(e.Node, po, IsExpandableObject(po), imageCurrentIndex, false);
							if (po is IObjectContainer objContainer2 && (po.Entity.Id == 137 || po.Entity.Id == 118))
								objContainer2.Filter = objContainer.Filter;
                        }
                    }
                }
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				ParentForm.Cursor = Cursors.Default;
			}
		}

		private static void AddFakeChildNode(TreeNode node)
		{
			node.Nodes.Add(FAKE_NODE);
		}

		private static void RemoveFakeChildNode(TreeNode parentNode)
		{
			parentNode.Nodes[0].Remove();
		}

		private bool ChildNodeIsFakeNode(TreeNode node)
		{
			return node.Nodes[0].Text == FAKE_NODE;
		}

		private bool IsExpandableObject(PresentationObject presentationObject)
		{
            bool isHasChilds = ParseHelper.GetBooleanFromObject(presentationObject["isHasChilds"], true);
            return presentationObject is IObjectContainer objContainer && objContainer.IsChildNodeExpandable && isHasChilds;
		}

		private void AddObject2Node(TreeNode parentNode, PresentationObject presentationObject, bool isExpandable, 
			int imageIndex, bool addAtFirstPlace)
		{
			TreeNode node = addAtFirstPlace
			                	?
			                		parentNode.Nodes.Insert(0, presentationObject.Name)
			                	: parentNode.Nodes.Add(presentationObject.Name);
			node.Tag = presentationObject;
			if(imageIndex >= 0)
			{
				node.BackColor = Color.Transparent;
				node.ImageIndex = imageIndex;
				node.SelectedImageIndex = imageIndex;
			}

			if(isExpandable) AddFakeChildNode(node);

			if(tvStructure.CheckBoxes)
			{
				skipCheckBoxEvent = true;
				node.Checked = IsObjectSelected(presentationObject);
				skipCheckBoxEvent = false;
			}
		}

		private int ResolveImageIndex(string iconName)
		{
			if(Globals.IconLoader == null) return -1;

			if(!iconResolver.ContainsKey(iconName))
			{
				// Icon isn't added to Image List yet
				Image icon = Globals.IconLoader(iconName);
				if(icon == null) return -1;

				imlTreeView.Images.Add(icon);
				iconResolver[iconName] = imlTreeView.Images.Count - 1;
			}
			return ParseHelper.ParseToInt32(iconResolver[iconName].ToString());
		}

		private void TvStructure_AfterSelect(object sender, TreeViewEventArgs e)
		{
			try
			{
				Application.DoEvents();
				ParentForm.Cursor = Cursors.WaitCursor;

				if(CurrentObjectContainer != null)
					RefreshDependantGrid();

				if(CurrentObjectContainer != null)
					FireContainerSelected(CurrentObjectContainer);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				ParentForm.Cursor = Cursors.Default;
			}
		}

		private void RefreshDependantGrid()
		{
			if(dependantGrid != null && CurrentObjectContainer.ChildEntity != null)
			{
				dependantGrid.Caption =
					string.Format("{0} / {1}", CurrentObjectContainer.Name, CurrentObjectContainer.ChildEntity.Name);

				dependantGrid.Entity = CurrentObjectContainer.ChildEntity;
				CurrentObjectContainer.ClearCache();
                dependantGrid.DataSource = CurrentObjectContainer.GetContent().DefaultView;
				if(CurrentObjectContainer != null)
					dependantGrid.RelationScenario = CurrentObjectContainer.RelationScenario;
			}
		}

		private void TvStructure_MouseDown(object sender, MouseEventArgs e)
		{
			try
			{
				if(e.Button == MouseButtons.Right)
				{
					tvStructure.SelectedNode = tvStructure.GetNodeAt(e.X, e.Y);
					if(tvStructure.SelectedNode != null)
					{
						tvStructure.ContextMenuStrip =
							MenuManager.CreatePopupMenu(CurrentActionHandler, OnPopupMenuItemClick, ViewType.Tree);
					}
				}
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}


		private Object SelectedNodeTag
		{
			get { return tvStructure.SelectedNode?.Tag; }
		}

		private IActionHandler CurrentActionHandler
		{
			get { return SelectedNodeTag as IActionHandler; }
		}

		private IObjectContainer CurrentObjectContainer
		{
			get { return SelectedNodeTag as IObjectContainer; }
		}

		[Browsable(false)]
		public DataTable SelectedObjects
		{
			set { dtSelected = value; }
		}

		private void OnPopupMenuItemClick(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Globals.SetWaitCursor(Globals.MdiParent);
				CurrentActionHandler.ObjectCreated -= OnObjectCreated;
				CurrentActionHandler.ObjectCreated += OnObjectCreated;

				AddHandlersOnContainerEvents(CurrentObjectContainer as IVisualContainer);
				AddHandlersOnObjectEvents(CurrentObject);

				CurrentActionHandler.DoAction(
					((ToolStripItem) sender).Name, ParentForm, InterfaceObjects.SimpleJournal);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
                Globals.SetDefaultCursor(Globals.MdiParent);
            }
		}

		private void AddHandlersOnContainerEvents(IVisualContainer objectContainer)
		{
			if(objectContainer != null)
			{
				objectContainer.ContainerRefreshed -= OnContainerRefreshed;
				objectContainer.ContainerRefreshed += OnContainerRefreshed;
			}
		}

		private void AddHandlersOnObjectEvents(PresentationObject presentationObject)
		{
			if(presentationObject != null)
			{
				presentationObject.ObjectDeleted -= OnTreeViewObjectDeleted;
				presentationObject.ObjectDeleted += OnTreeViewObjectDeleted;

				presentationObject.ObjectChanged -= OnTreeViewObjectChanged;
				presentationObject.ObjectChanged += OnTreeViewObjectChanged;

				presentationObject.ObjectCloned -= OnObjectCloned;
				presentationObject.ObjectCloned += OnObjectCloned;

				presentationObject.ParentChanged -= OnParentChanged;
				presentationObject.ParentChanged += OnParentChanged;

                presentationObject.ParentChanged2 -= RebuildTree;
                presentationObject.ParentChanged2 += RebuildTree;
            }
		}

        private void RebuildTree(PresentationObject presentationObject)
        {
            if (tvStructure.SelectedNode == null || CurrentObject == null) return;

            var expandedObjects = new List<PresentationObject>();
            CurrentObject.ObjectChanged += OnTreeViewObjectChanged;
            CurrentObject.Refresh();
            CurrentObject.ObjectChanged -= OnTreeViewObjectChanged;
            SaveExpandedNodes(tvStructure.SelectedNode, expandedObjects);

			if (IsExpandableObject(tvStructure.SelectedNode.Tag as PresentationObject))
			{
				tvStructure.SelectedNode.Collapse();
				tvStructure.SelectedNode.Nodes.Clear();
				CurrentObjectContainer.ClearCache();
				tvStructure.SelectedNode.Nodes.Add(FAKE_NODE);
				tvStructure.SelectedNode.Expand();

				ExpandNode(tvStructure.SelectedNode, expandedObjects);
				FireContainerSelected(CurrentObjectContainer);
			}
        }

        private void RebuildTree(PresentationObject presentationObject, Entity parentEntity)
		{
            if (tvStructure.SelectedNode == null) return;

			while (!CurrentObject.Entity.Equals(parentEntity))
			{
				tvStructure.SelectedNode = tvStructure.SelectedNode.Parent;
            }

			RebuildTree(presentationObject);
        }

		private void SaveExpandedNodes(TreeNode node, IList<PresentationObject> expandenObjects)
		{
			foreach (TreeNode treeNode in node.Nodes)
				if (treeNode.IsExpanded)
				{
					expandenObjects.Add((PresentationObject)treeNode.Tag);
					SaveExpandedNodes(treeNode, expandenObjects);
				}
        }

		private void ExpandNode(TreeNode node, List<PresentationObject> expandenObjects)
		{
            foreach (TreeNode treeNode in node.Nodes)
            {
				PresentationObject obj = expandenObjects.Find(o => o.Equals((PresentationObject)treeNode.Tag));
				if(obj != null)
                //if (expandenObjects.Contains((PresentationObject)treeNode.Tag))
				{
					if (obj is ObjectContainer container)
						((ObjectContainer)treeNode.Tag).ChildEntity = container.ChildEntity;
                    treeNode.Expand();
					ExpandNode(treeNode, expandenObjects);
                }
            }
        }

        private void OnParentChanged(PresentationObject presentationObject, int parentDepth)
		{
			try
			{
				if(tvStructure.SelectedNode == null) return;

				for (int i = 0; i < parentDepth; i++)
				{
					if (tvStructure.SelectedNode.Parent != null)
						tvStructure.SelectedNode = tvStructure.SelectedNode.Parent;
				}
				tvStructure.SelectedNode.Collapse();
				tvStructure.SelectedNode.Nodes.Clear();
				CurrentObjectContainer.ClearCache();
				tvStructure.SelectedNode.Nodes.Add(FAKE_NODE);
				tvStructure.SelectedNode.Expand();

				FireContainerSelected(CurrentObjectContainer);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void OnObjectCloned(PresentationObject presentationObject)
		{
			try
			{
				if(tvStructure.SelectedNode == null) return;

				// Just refresh parent Node
				tvStructure.SelectedNode = tvStructure.SelectedNode.Parent;
				RefreshCurrentNode();
				//this.objectContainer_ContainerRefreshed((IObjectContainer)this.tvStructure.SelectedNode.Tag);
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

		private void OnObjectCreated(PresentationObject presentationObject)
		{
			try
			{
				dependantGrid?.AddRow(presentationObject);

				// If selected node isn't expanded yet and has FAKE_NODE as child it's not necesary to add
				// created object
				if(
					!(tvStructure.SelectedNode.Nodes.Count > 0 && ChildNodeIsFakeNode(tvStructure.SelectedNode)) &&
					CurrentObjectContainer != null && CurrentObjectContainer.IsChildNodeExpandable)
				{
					AddObject2Node(
						tvStructure.SelectedNode, presentationObject,
						IsExpandableObject(presentationObject),
						ResolveImageIndex(presentationObject.Entity.IconName),
						true);
				}

				// inform parent form that new object is created
				FireObjectCreated(presentationObject);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void OnContainerRefreshed(IObjectContainer objectContainer)
		{
			CurrentObjectContainer.ClearCache();
			objectContainer.ClearCache();
			tvStructure.SelectedNode.Nodes.Clear();
			if(objectContainer.IsChildNodeExpandable)
			{
				AddFakeChildNode(tvStructure.SelectedNode);
				tvStructure.SelectedNode.Collapse();
				tvStructure.SelectedNode.Expand();
			}

			RefreshDependantGrid();
			FireContainerSelected(objectContainer);
		}

		private void OnTreeViewObjectDeleted(PresentationObject presentationObject)
		{
			DeleteNode(presentationObject);
			dependantGrid?.DeleteRow(presentationObject);
		}

		private void OnTreeViewObjectChanged(PresentationObject presentationObject)
		{
			UpdateNode(presentationObject);
			dependantGrid?.UpdateRow(presentationObject);
		}

		private void OnGridObjectDeleted(PresentationObject presentationObject)
		{
			DeleteNode(presentationObject);
		}

		private void FireObjectCreated(PresentationObject presentationObject)
		{
			ObjectCreated?.Invoke(presentationObject);
		}

		private void FireContainerSelected(IObjectContainer objectContainer)
		{
			ContainerSelected?.Invoke(objectContainer);
		}

		public void DeleteCurrentObject()
		{
			PresentationObject presentationObject = CurrentObject;
			if(presentationObject != null && presentationObject.Delete())
			{
				DeleteNode(presentationObject);
			}
		}

		public void EditCurrentObject()
		{
			PresentationObject presentationObject = CurrentObject;
			if(presentationObject != null) 
			{
                if (presentationObject.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal))
                {
                    presentationObject.DoAction(Constants.EntityActions.Edit, Globals.MdiParent, InterfaceObjects.PropertyPage);
                    //UpdateNode(presentationObject);
                }
				else if(presentationObject.ShowPassport(ParentForm))
                UpdateNode(presentationObject);
			}
		}

		private void UpdateNode(PresentationObject presentationObject)
		{
			TreeNode node = FindNode(presentationObject);
			if(node != null) node.Text = presentationObject.Name;
		}

		private void DeleteNode(PresentationObject presentationObject)
		{
			TreeNode node = FindNode(presentationObject);
			
			if (node != null)
			{
				TreeNode parent = node.Parent;
				node.Remove();
				if (parent != null)
				{
                    if (parent.Tag is IObjectContainer objContainer)
                        objContainer.ClearCache();
                }
			}
		}

		private TreeNode FindNode(PresentationObject presentationObject)
		{
			if(presentationObject == CurrentObject) return tvStructure.SelectedNode;

			foreach(TreeNode node in tvStructure.SelectedNode.Nodes)
				if (presentationObject != null && node != null && presentationObject.Equals(node.Tag))
					return node;

			return null;
		}

		public void RefreshCurrentNode()
		{
			if(CurrentObjectContainer != null)
				OnContainerRefreshed(CurrentObjectContainer);
		}

		private bool IsObjectSelected(PresentationObject presentationObject)
		{
			StringBuilder builder = new StringBuilder();
			for(int i = 0; i < presentationObject.Entity.PKColumns.Length; i++)
			{
				string columnName = presentationObject.Entity.PKColumns[i];
				if(!dtSelected.Columns.Contains(columnName)) return false;
				if(builder.Length > 0) builder.Append(" And ");
				builder.Append(columnName);
				builder.Append("=");
				builder.Append(presentationObject[columnName]);
				if (!string.IsNullOrEmpty(SelectedItemsBitColumn))
				{
					if (builder.Length > 0) builder.Append(" And ");
					builder.AppendFormat(" [{0}] = 1 ", SelectedItemsBitColumn);
				}
			}

			return dtSelected.Select(builder.ToString()).Length > 0;
		}

		private string TryGetSelectedImage(PresentationObject presentationObject)
		{
			if (string.IsNullOrEmpty(SelectedItemsImageColumn))
				return null;

			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < presentationObject.Entity.PKColumns.Length; i++)
			{
				string columnName = presentationObject.Entity.PKColumns[i];
				if (!dtSelected.Columns.Contains(columnName)) return null;
				if (builder.Length > 0) builder.Append(" And ");
				builder.Append(columnName);
				builder.Append("=");
				builder.Append(presentationObject[columnName]);
			}

			DataRow[] rows = dtSelected.Select(builder.ToString());
			if (rows.Length > 0)
			{
				return ParseHelper.GetStringFromObject(rows[0][SelectedItemsImageColumn], null);
			}
			return null;
		}

		private void TvStructure_AfterCheck(object sender, TreeViewEventArgs e)
		{
			try
			{
				if(skipCheckBoxEvent) return;

				skipCheckBoxEvent = true;

				TreeNode node = e.Node;
				if(node.Checked)
				{
					CheckNode(node);
				}
				else
				{
					UncheckNode(node);
				}

				skipCheckBoxEvent = false;
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

		private void CheckNode(TreeNode node)
		{
			if (DataSource == null)
				CheckObject(node.Tag as PresentationObject);
			else
				CheckIDs(node.Tag);

			TreeNode parent = node.Parent;
			while (parent != null)
			{
				if (!parent.Checked)
				{
					parent.Checked = true;
					CheckObject(parent.Tag as PresentationObject);
				}
				parent = parent.Parent;
			}

			if (!node.IsExpanded && DataSource == null)
			{
				node.ExpandAll();
			}

			foreach (TreeNode child in node.Nodes)
			{
				if (!child.Checked)
				{
					CheckNode(child);
					child.Checked = true;
				}
			}
		}

		private void UncheckNode(TreeNode node)
		{
			if (DataSource == null)
				UncheckObject(node.Tag as PresentationObject);
			else
				UncheckIDs(node.Tag);

			if (!node.IsExpanded && DataSource == null)
			{
				node.ExpandAll();
			}

			foreach (TreeNode child in node.Nodes)
			{
				if (child.Checked)
				{
					UncheckNode(child);
					child.Checked = false;
				}
			}
		}

		private void UncheckIDs(object key)
		{
			if (addedIDs.Contains(key))
				addedIDs.Remove(key);
			else
			{
				if (!deletedIDs.Contains(key))
					deletedIDs.Add(key);
			}
		}

		private void CheckIDs(object key)
		{
			if (deletedIDs.Contains(key))
				deletedIDs.Remove(key);
			else
			{
				if (!addedIDs.Contains(key))
					addedIDs.Add(key);
			}
		}

		private void UncheckObject(PresentationObject presentationObject)
		{
			if (presentationObject == null)
				return;

			UncheckIDs(presentationObject.Key);

			if (addedObjects.Contains(presentationObject))
				addedObjects.Remove(presentationObject);
			else
			{
				if (!deletedObjects.Contains(presentationObject))
					deletedObjects.Add(presentationObject);
			}
		}

		private void CheckObject(PresentationObject presentationObject)
		{
			if (presentationObject == null)
				return;

			CheckIDs(presentationObject.Key);

			if (deletedObjects.Contains(presentationObject))
				deletedObjects.Remove(presentationObject);
			else
			{
				if (!addedObjects.Contains(presentationObject))
					addedObjects.Add(presentationObject);
			}
		}

		public void ShowRootFilter()
		{
			tvStructure.SelectedNode = tvStructure.Nodes[0];
			AddHandlersOnContainerEvents(CurrentObjectContainer as IVisualContainer);
			((FakeContainer)SelectedNodeTag).ShowFilter(ParentForm);			
		}
    }
}