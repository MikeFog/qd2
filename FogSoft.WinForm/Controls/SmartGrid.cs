using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace FogSoft.WinForm.Controls
{
    public partial class SmartGrid : UserControl, IObjectControl
	{
		#region Events ----------------------------------------

		public event EmptyDelegate DblClick;
		public event ObjectDelegate ObjectDeleted;
		public event ObjectDelegate ObjectChanged;
		public event ObjectDelegate ObjectSelected;
		public event ObjectDelegate ObjectCreated;
		public event ContainerDelegate ContainerRefreshed;
		public event ColumnSelectedDelegate ColumnSelected;
		public event ObjectCheckedDelegate ObjectChecked;
		public event ObjectParentChange EntityParentChanged;
        public event EventHandler<int> RecordCountChanged;

        #endregion

        #region Members ---------------------------------------

        private BindingManagerBase bm;
		private PresentationObject selectedObject;
		private Entity entity;
		private InterfaceObjects interfaceObject = InterfaceObjects.SimpleJournal;
		private RelationScenario relationScenario;
		private bool isMenuEnabled = true;
		private bool checkboxes = false;
		private bool showMultiselectColumn = true;
        private DataGridViewColumn currentColumn;
		private List<PresentationObject> added2Checked, removedFromChecked;
		private SmartGrid dependantGrid;

        #endregion

        private const string QuickSearchText = "Поиск по полю";
		public const string COL_IsSelected = "isObjectSelected";

		public SmartGrid()
		{
			InitializeComponent();
			dataGrid.AutoGenerateColumns = false;
		}

		public new bool Enabled
		{
			get { return base.Enabled; }
			set 
			{ 
				base.Enabled = value;
				dataGrid.ForeColor = value ? SystemColors.ControlText : SystemColors.GrayText; 
			}
		}

		public bool QuickSearchVisible
		{
			get { return panelSearch.Visible; }
			set { panelSearch.Visible = value; }
		}

		public string Caption
		{
			get { return lblCaption.Text; }
			set { lblCaption.Text = value; }
		}

		public bool CaptionVisible
		{
			get { return lblCaption.Visible; }
			set { lblCaption.Visible = value; }
		}

		public bool CheckBoxes 
		{
			get { return checkboxes; }
			set { checkboxes = value; }
		}

        public bool ShowMultiselectColumn 
		{
            get { return showMultiselectColumn; }
            set { showMultiselectColumn = value; }
        }

        [Browsable(false)]
		public int ItemsCount
		{
			get { return (bm == null) ? 0 : bm.Count; }
		}

		[Browsable(false)]
		public Entity Entity
		{
			get { return entity; }
			set
			{
				SelectedObject = null;
				if(value == null) return;
				entity = value;
			}
		}

		[Browsable(false)]
		public SmartGrid DependantGrid
		{
			get { return dependantGrid; }
			set
			{
				dependantGrid = value;
				if(dependantGrid != null)
					dependantGrid.ObjectDeleted += DependantGrid_ObjectDeleted;
			}
		}

		[DefaultValue((int) InterfaceObjects.SimpleJournal)]
		public InterfaceObjects InterfaceObject
		{
			get { return interfaceObject; }
			set { interfaceObject = value; }
		}

		[Browsable(false)]
		public RelationScenario RelationScenario
		{
			set { relationScenario = value; }
		}

		[Browsable(false)]
		public DataGridView InternalGrid
		{
			get { return dataGrid; }
		}

		public bool MenuEnabled
		{
			get { return isMenuEnabled; }
			set { isMenuEnabled = value; }
		}

		[Browsable(false)]
		public List<PresentationObject> Added2Checked
		{
			get { return added2Checked; }
		}

		[Browsable(false)]
		public List<PresentationObject> RemovedFromChecked
		{
			get { return removedFromChecked; }
		}

		[Browsable(false)]
		public DataView DataSource
		{
			get { return dataGrid.DataSource as DataView; }
			set
			{
				Clear();
				if(value == null) return;

				if(entity != null)
				{
					SetTablePKColumn(value.Table);

					SetColumnHeaders(value.Table.Columns);
                    dataGrid.DataSource = value;

					bm = BindingContext[dataGrid.DataSource];
					bm.PositionChanged += new EventHandler(Bm_PositionChanged);
					if(selectedObject != null)
						SelectedObject = selectedObject;

					RefreshDependantGrid();
					FireObjectSelected();
				}
				else
				{
					dataGrid.AutoGenerateColumns = true;
					dataGrid.DataSource = value;
				}
				if (dataGrid.Columns.Count > 0)
				{
					currentColumn = CheckCurrentColumnData(dataGrid.Columns[0]);
					if (currentColumn != null)
					{
						txQuickSearch.ReadOnly = false;
						SelectObject();
					}
				}
				HighlightRows();
			}
		}

		[Browsable(false)]
		public string ColumnNameHighlight {get; set;}

		[Browsable(false)]
		[DefaultValue((int)ColorHighlight.Red)]
		public ColorHighlight ColorHighlight {get; set;}

		[Browsable(false)]
		public bool IsNeedHighlight { get; set; }

		[Browsable(false)]
		public bool IsHighlightInvertColor { get; set; }

		public void HighlightRows()
		{
			if (IsNeedHighlight && !string.IsNullOrEmpty(ColumnNameHighlight))
			{
                if (dataGrid.DataSource is DataView dataView && dataView.Count > 0)
                {
                    if (!dataView.Table.Columns.Contains(ColumnNameHighlight))
                    {
                        ColumnNameHighlight = null;
                        IsNeedHighlight = false;
                        return;
                    }

                    DataRow[] mins = dataView.Table.Select(string.Format("[{0}] is not null ", ColumnNameHighlight)
                        , string.Format("[{0}] asc ", ColumnNameHighlight));
                    DataRow[] maxs = dataView.Table.Select(string.Format("[{0}] is not null ", ColumnNameHighlight)
                        , string.Format("[{0}] desc ", ColumnNameHighlight));

                    object min = null;
                    object max = null;

                    if (mins.Length > 0)
                        min = mins[0][ColumnNameHighlight];

                    if (maxs.Length > 0)
                        max = maxs[0][ColumnNameHighlight];

                    if (StringUtil.IsDBNullOrNull(min) || StringUtil.IsDBNullOrNull(max))
                        return;

                    CurrencyManager manager = (CurrencyManager)bm;
                    DataView dv = (DataView)manager.List;
                    for (int i = 0; i < dv.Count; i++)
                    {
                        object o = dv[i][ColumnNameHighlight];

                        if (StringUtil.IsDBNullOrNull(o))
                            continue;

                        Color color = GetColor(o, min, max);
                        foreach (DataGridViewCell cell in dataGrid.Rows[i].Cells)
                        {
                            if (cell.Style.BackColor != Color.LightYellow) // This is quick search color
                                cell.Style.BackColor = color;
                        }
                    }
                }
            }
		}

		/// <summary>
		/// Get Palit Color by find percentage
		/// </summary>
		/// <param name="o"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		private Color GetColor(object o, object min, object max)
		{
			int colorVal;
			const int maxColor = 255;
			if (o is float)
			{
				float fMin = ParseHelper.GetFloatFromObject(min, float.MinValue);
				float fMax = ParseHelper.GetFloatFromObject(max, float.MaxValue);
				float val = ParseHelper.GetFloatFromObject(o, fMin);
				if (fMax - fMin == 0)
					return Color.White;
				colorVal = 255 - (int)((float)maxColor * (val - fMin) / (fMax - fMin));
			}
			else if (o is decimal)
			{
				decimal dMin = ParseHelper.GetDecimalFromObject(min, decimal.MinValue);
				decimal dMax = ParseHelper.GetDecimalFromObject(max, decimal.MaxValue);
				decimal val = ParseHelper.GetDecimalFromObject(o, dMin);
				if (dMax - dMin == 0)
					return Color.White;
				colorVal = 255 - (int)((decimal)maxColor * (val - dMin) / (dMax - dMin));
			}
			else if (o is int)
			{
				int iMin = ParseHelper.GetInt32FromObject(min, int.MinValue);
				int iMax = ParseHelper.GetInt32FromObject(max, int.MaxValue);
				int val = ParseHelper.GetInt32FromObject(o, iMin);
				if (iMax - iMin == 0)
					return Color.White;
				colorVal = 255 - (int)((float)maxColor * ((float)val - (float)iMin) / ((float)iMax - (float)iMin));
			}
			else if (o is DateTime)
			{
				DateTime dMax = ParseHelper.GetDateTimeFromObject(max, DateTime.MaxValue);
				DateTime dMin = ParseHelper.GetDateTimeFromObject(min, DateTime.MinValue);
				DateTime val = ParseHelper.GetDateTimeFromObject(o, dMin);
				if ((dMax - dMin).TotalMinutes == 0)
					return Color.White;
				colorVal = 255 - (int)((float)maxColor * (((float)(val - dMin).TotalMinutes)) / ((float)(dMax - dMin).TotalMinutes));
			}
			else if (o is double)
			{
				double dMax = ParseHelper.GetDoubleFromObject(max, double.MaxValue);
				double dMin = ParseHelper.GetDoubleFromObject(min, double.MinValue);
				double val = ParseHelper.GetDoubleFromObject(o, dMin);
				if (dMax - dMin == 0)
					return Color.White;
				colorVal = 255 - (int)((double)maxColor * ((double)val - (double)dMin) / ((double)dMax - (double)dMin));
			}
			else 
				throw new NotImplementedException();

			if (IsHighlightInvertColor)
				colorVal = 255 - colorVal;

			return Color.FromArgb(ColorHighlight != WinForm.Controls.ColorHighlight.Red ? colorVal : maxColor
			                      , ColorHighlight != WinForm.Controls.ColorHighlight.Green ? colorVal : maxColor
			                      , ColorHighlight != WinForm.Controls.ColorHighlight.Blue ? colorVal : maxColor);
		}

		private DataGridViewColumn CheckCurrentColumnData(DataGridViewColumn column)
		{
			if (column == null || string.IsNullOrEmpty(column.DataPropertyName))
			{
				if (currentColumn != null)
					return currentColumn;
				
				if (dataGrid.Columns.Count <= 0)
					return null;

				column = dataGrid.Columns[0];
				while (string.IsNullOrEmpty(column.DataPropertyName))
				{
					int index = dataGrid.Columns.IndexOf(column);
					if ((index + 1) < dataGrid.Columns.Count)
						column = dataGrid.Columns[dataGrid.Columns.IndexOf(column) + 1];
					else
						return null;
				}
			}

			return column;
		}

		public PresentationObject SelectedObject
		{
			get
			{
				if(dataGrid.DataSource == null) return null;
				return CreateObject(CurrentRowView);
			}
			set
			{
				selectedObject = value;
				SelectObject();
			}
		}

		public void AddRow(PresentationObject presentationObject)
		{
			if(entity == null || !IsAllowedEntity(presentationObject.Entity)) return;

			if(dataGrid.DataSource == null)
				DataSource = entity.LoadSingleObject(presentationObject).DefaultView;
			else
				Globals.AddObject2DataTable(GridTable, presentationObject);

			GridTable.AcceptChanges();
			SelectedObject = presentationObject;

			FireObjectCreated(presentationObject);
		}

		public void UpdateRow(PresentationObject presentationObject)
		{
			if(dataGrid.DataSource == null || !IsAllowedEntity(presentationObject.Entity)) return;

			try
			{
				Cursor = Cursors.WaitCursor;
				// recognize current object -----------------------------
				DataRow row = GridTable.Rows.Find(presentationObject.IDs);
				if (row != null)
				{
					foreach (DataColumn column in row.Table.Columns)
						row[column] = presentationObject[column.Caption] ?? DBNull.Value;
				}

				//GridTable.AcceptChanges();
				//SetColumnsWidth();
				FireObjectChanged(presentationObject);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private DataTable GridTable
		{
			get
			{
				DataView dataView = (DataView)dataGrid.DataSource;
				return dataView.Table;
			}
		}

		private bool IsAllowedEntity(Entity entityCandidate)
		{
			return entityCandidate.Equals(entity) ||
			       (entity.ParentId != null && ParseHelper.ParseToInt32(entityCandidate.ParentId.ToString()) == entity.Id);
		}

		public void DeleteRow(PresentationObject presentationObject)
		{
			if(dataGrid.DataSource == null || !IsAllowedEntity(presentationObject.Entity)) return;

			// recognize current object -----------------------------
			if (GridTable.PrimaryKey.Length > 0)
			{
				DataRow row = GridTable.Rows.Find(presentationObject.IDs);
				if (row != null)
				{
					dependantGrid?.Clear();

					row.Delete();
					if (added2Checked.Contains(presentationObject))
						added2Checked.Remove(presentationObject);
					GridTable.AcceptChanges();
				}
			}

            ObjectDeleted?.Invoke(presentationObject);
        }

		public void CalculateColumnSummary()
		{
            if (!(dataGrid.DataSource is DataView dataView)) return;

            DataTable dataTable = dataView.Table;
			if (string.IsNullOrEmpty(dataView.Sort) || dataView.Sort.Length < 1)
				return;
			Match m = Regex.Match(dataView.Sort, @"\[.+\]");
			string columnName = m.Value.Substring(1, m.Value.Length - 2);
			object res = dataTable.Compute(string.Format("Sum([{0}])", columnName), null);
			MessageBox.ShowInformation(FormatColumnSummary(res, dataTable.Columns[columnName]));
		}

		private static string FormatColumnSummary(object summa, DataColumn tableColumn)
		{
			string res;

			if (tableColumn.DataType == typeof(decimal) || tableColumn.DataType == typeof(double))
			{
				res = decimal.Parse(summa.ToString()).ToString("f");
			}
			else
			{
				res = summa.ToString();
			}

			return string.Format("Сумма по колонке равна {0}", res);
		}

		public void Clear()
		{
			dataGrid.DataSource = null;
			dataGrid.Columns.Clear();
			lbQuickSearch.Text = QuickSearchText;
			ClearQuickSearch();
			currentColumn = null;
			txQuickSearch.ReadOnly = true;
			added2Checked = new List<PresentationObject>();
			removedFromChecked = new List<PresentationObject>();
			
			CheckBox headerBox = CheckboxHeader;
			if(headerBox != null) headerBox.Checked = false;
        }

		private CheckBox CheckboxHeader
		{
			get
			{
                Control[] res = dataGrid.Controls.Find("checkboxHeader", true);
				if (res.Length > 0) return (CheckBox)res[0];
				return null;
            }
		}

        private void ClearQuickSearch()
		{
			txQuickSearch.TextChanged -= TxQuickSearch_TextChanged;
			txQuickSearch.Text = string.Empty;
			txQuickSearch.BackColor = SystemColors.Window;
			txQuickSearch.TextChanged += TxQuickSearch_TextChanged;
		}

		private PresentationObject CreateObject(DataRowView drv)
		{
			if(drv == null) return null;

			PresentationObject presentationObject = ResolveEntityForSelectedRow(drv).CreateObject(drv.Row);
            if (presentationObject is IObjectContainer objectContainer)
            {
                if (relationScenario != null)
                    objectContainer.RelationScenario = relationScenario;
                else if (dependantGrid != null)
                    objectContainer.ChildEntity = dependantGrid.Entity;
            }
            return presentationObject;
		}

		private Entity ResolveEntityForSelectedRow(DataRowView drv)
		{
			if(drv.DataView.Table.Columns.Contains(Constants.ParamNames.EntityId))
				return EntityManager.GetEntity(ParseHelper.ParseToInt32(drv[Constants.ParamNames.EntityId].ToString()));
			return entity;
		}

		private void SetTablePKColumn(DataTable table)
		{
			// Set PK columns in the DataTable to find necessary row quickly
			if(entity.PKColumns.Length > 0)
			{
				DataColumn[] pkColumns = new DataColumn[entity.PKColumns.Length];
				for(int i = 0; i < entity.PKColumns.Length; i++)
					pkColumns[i] = table.Columns[entity.PKColumns[i]];
				table.PrimaryKey = pkColumns;
			}
		}

		private void SetColumnHeaders(DataColumnCollection columns)
		{
			if(checkboxes) AddMultiSelectColumn();

			Image icon = null;
			if(Globals.IconLoader != null) icon = Globals.IconLoader(entity.IconName);

			if(icon != null) AddImageColumn(icon);

			foreach (Entity.Attribute entityAttribute in entity.SortedAttributes)
			{
				if(columns.Contains(entityAttribute.Name))
					AddColumn(entityAttribute);
			}

			//dataGrid.Columns[dataGrid.Columns.Count-1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

		private DataRowView CurrentRowView
		{
			get
			{
				if(bm == null || bm.Position < 0) return null;
				return bm.Current as DataRowView;
			}
		}

		private void AddImageColumn(Image icon)
		{
			DataGridViewImageColumn column = new DataGridViewImageColumn(true)
			                                 	{
			                                 		Image = icon,
			                                 		ValuesAreIcons = false,
			                                 		Resizable = DataGridViewTriState.False
			                                 	};
			dataGrid.Columns.Add(column);
		}

		private void AddColumn(Entity.Attribute entityAttribute)
		{
			DataGridViewColumn column = CreateDataGridColumn(entityAttribute);
			column.DataPropertyName = entityAttribute.Name;
			column.HeaderText = entityAttribute.Alias;
			if (!(column is DataGridViewComboBoxColumn))
				column.ReadOnly = true;

			dataGrid.Columns.Add(column);
		}

		private void AddMultiSelectColumn()
		{
			DataGridViewCheckBoxColumn column = new DataGridViewCheckBoxColumn
			                                    	{
			                                    		DataPropertyName = COL_IsSelected,
			                                    		ReadOnly = false
			                                    	};
			
			dataGrid.Columns.Add(column);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

			if (checkboxes && showMultiselectColumn)
				Show_chkBox();
        }

        private void Show_chkBox()
        {
			if (dataGrid.Columns.Count > 0)
			{
				Rectangle rect = dataGrid.GetCellDisplayRectangle(0, -1, true);
				// set checkbox header to center of header cell. +1 pixel to position 
				rect.Y = 3;
				rect.X = rect.Location.X + 3;// (rect.Width / 4);
                CheckBox checkboxHeader = new CheckBox
                {
                    Name = "checkboxHeader",
                    Size = new Size(18, 18),
                    Location = rect.Location
                };
                checkboxHeader.CheckedChanged += new EventHandler(CheckboxHeader_CheckedChanged);
				dataGrid.Controls.Add(checkboxHeader);
			}
        }

        private void CheckboxHeader_CheckedChanged(object sender, EventArgs e)
        {
			MultiSelectCheckAll(CheckboxHeader.Checked);
        }

        private DataGridViewColumn CreateDataGridColumn(Entity.Attribute attribute)
		{
            entity.ColumnsInfo.TryGetValue(attribute.Name, out ColumnInfo columnInfo);

            if (ColumnInfo.IsBooleanData(columnInfo, attribute))
			{
				DataGridViewImageColumn column = new DataGridViewImageColumn
				                                 	{
				                                 		CellTemplate = new DataGridViewExBooleanCell(),
				                                 		SortMode = DataGridViewColumnSortMode.Automatic,
				                                 		Resizable = DataGridViewTriState.False
				                                 	};
				return column;
			}

			if(ColumnInfo.IsMoneyData(columnInfo, attribute))
			{
				DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
				column.CellTemplate.Style.Format = "c";
				column.CellTemplate.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
				return column;
			}

			if (ColumnInfo.IsFloatData(columnInfo, attribute))
			{
				DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
				column.CellTemplate.Style.Format = "f";
				column.CellTemplate.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
				return column;
			}

			return new DataGridViewTextBoxColumn();
		}

        private void DataGrid_MouseDown(object sender, MouseEventArgs e)
		{
			try
			{
				if(e.Button == MouseButtons.Right && isMenuEnabled)
				{
					PresentationObject currentObject = SelectedObject;
					if(currentObject != null) DisplayPopUpMenu(currentObject);
				}
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

		private void DisplayPopUpMenu(IActionHandler currentObject)
		{
			dataGrid.ContextMenuStrip =
				MenuManager.CreatePopupMenu(currentObject, MenuItemClick, ViewType.Journal);
		}

		private void MenuItemClick(object sender, EventArgs e)
		{
			try
			{
				ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
				PresentationObject currentObject = SelectedObject;

				IActionHandler actionHandler = currentObject;
				actionHandler.ObjectCreated -= OnObjectCreated;
				actionHandler.ObjectCreated += OnObjectCreated;

				currentObject.ObjectDeleted -= OnObjectDeleted;
				currentObject.ObjectChanged -= OnObjectChanged;

				currentObject.ObjectDeleted += OnObjectDeleted;
				currentObject.ObjectChanged += OnObjectChanged;

				currentObject.ObjectCloned -= OnObjectCreated;
				currentObject.ObjectCloned -= OnObjectCloned;
				currentObject.ObjectCloned += OnObjectCreated;
				currentObject.ObjectCloned += OnObjectCloned;

				currentObject.ParentChanged -= OnParentChanged;
				currentObject.ParentChanged += OnParentChanged;

                currentObject.ParentChanged2 -= OnObjectParentChange2;
                currentObject.ParentChanged2 += OnObjectParentChange2;

                if (currentObject is IVisualContainer objectContainer)
                {
                    objectContainer.ContainerRefreshed -= OnContainerRefreshed;
                    objectContainer.ContainerRefreshed += OnContainerRefreshed;
                }

                currentObject.DoAction(menuItem.Name, ParentForm, interfaceObject);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				dataGrid.ContextMenuStrip = null;
			}
		}

		private void OnParentChanged(PresentationObject presentationObject, int i)
		{
            EntityParentChanged?.Invoke(presentationObject, i);
        }

		private void OnObjectParentChange2(PresentationObject presentationObject, Entity parentEntity)
		{
			RebuildTree?.Invoke(presentationObject, parentEntity);
		}

        private void OnObjectCreated(PresentationObject presentationObject)
		{
			try
			{
				dependantGrid?.AddRow(presentationObject);
				RefreshDependantGrid();
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
				AddRow(presentationObject);

				RefreshDependantGrid();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void OnObjectDeleted(PresentationObject presentationObject)
		{
			try
			{
				DeleteRow(presentationObject);
				RefreshDependantGrid();
                RebuildCurrentNode?.Invoke(presentationObject);
            }
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void OnObjectChanged(PresentationObject presentationObject)
		{
			try
			{
				UpdateRow(presentationObject);
				ObjectChanged?.Invoke(presentationObject);

				RefreshDependantGrid();
				RebuildCurrentNode?.Invoke(presentationObject);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void OnContainerRefreshed(IObjectContainer container)
		{
			try
			{
                ContainerRefreshed?.Invoke(container);
                if (RebuildCurrentNode != null && container is PresentationObject po) RebuildCurrentNode(po);

            }
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void Bm_PositionChanged(object sender, EventArgs e)
		{
			PresentationObject po = SelectedObject;
			if (po != null && po.Equal(selectedObject)) return;
			selectedObject = po;
			FireObjectSelected();
			RefreshDependantGrid();
		}

		private void RefreshDependantGrid()
		{
			if(dependantGrid != null)
			{
				if (SelectedObject is IObjectContainer objectContainer)
				{
					SetDependantGridCaption(objectContainer);
					dependantGrid.DataSource = objectContainer.GetContent().DefaultView;
				}
				else
				{
                    SetDependantGridCaption(null);
                    dependantGrid.DataSource = null;
				}
            }
		}

		private void SetDependantGridCaption(IObjectContainer objectContainer)
		{
			dependantGrid.Caption = objectContainer == null ? string.Empty : string.Format("{0} / {1}", SelectedObject.Name, objectContainer.ChildEntity.Name);
		}

		private void FireGridDblClicked()
		{
            DblClick?.Invoke();
        }

		private void FireObjectSelected()
		{
			if(ObjectSelected != null && SelectedObject != null)
				ObjectSelected(SelectedObject);
		}

		private void FireObjectChecked(PresentationObject po, bool state)
		{
			ObjectChecked?.Invoke(po, state);
		}

		private void FireObjectDeleted(PresentationObject presentationObject)
		{
            ObjectDeleted?.Invoke(presentationObject);
        }

		private void FireObjectChanged(PresentationObject presentationObject)
		{
            ObjectChanged?.Invoke(presentationObject);
        }

		private void FireObjectCreated(PresentationObject presentationObject)
		{
			ObjectCreated?.Invoke(presentationObject);
		}

		private void DataGrid_DoubleClick(object sender, EventArgs e)
		{
			Point pnt = dataGrid.PointToClient(Cursor.Position);
			DataGridView.HitTestInfo hti = dataGrid.HitTest(pnt.X, pnt.Y);
			if(hti.Type != DataGridViewHitTestType.Cell && hti.Type != DataGridViewHitTestType.RowHeader)
				return;

			PresentationObject presentationObject = SelectedObject;
			if(presentationObject != null && presentationObject.IsActionEnabled(Constants.EntityActions.ShowPassport, ViewType.Journal) &&
			   isMenuEnabled &&
			   presentationObject.ShowPassport(this))
			{
				UpdateRow(presentationObject);
			}

			FireGridDblClicked();
		}

		private void DataGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			try
			{
				currentColumn = CheckCurrentColumnData(dataGrid.Columns[e.ColumnIndex]);
				txQuickSearch.ReadOnly = (currentColumn == null);

				if (currentColumn != null)
				{
					if (ColumnSelected != null)
					{
						string columnName = currentColumn.DataPropertyName;
						DataView dv = (DataView) dataGrid.DataSource;
                        DataColumn dataColumn = dv.Table.Columns[columnName];
                        if (dataColumn != null)
                        {
                            ColumnSelected(dataColumn.DataType);
                        }
					}
					SelectObject();
				}

				HighlightRows();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void TxQuickSearch_TextChanged(object sender, EventArgs e)
		{
			try
			{
				int recordsCount = QuickSearch();

				if (txQuickSearch.Text.Length > 0)
				{
					txQuickSearch.BackColor = recordsCount == 0 ? Color.LightPink : Color.LightYellow;
				}
				else
				{
					txQuickSearch.BackColor = SystemColors.Window;
				}
                RecordCountChanged?.Invoke(this, recordsCount);
            }
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private int QuickSearch()
		{
			if(currentColumn == null || currentColumn is DataGridViewCheckBoxColumn) 
				return 0;

			string searchText = txQuickSearch.Text.ToLower();
            if (string.IsNullOrEmpty(searchText))
            {
                // Если строка поиска пуста, показываем все данные
                DataSource.RowFilter = null;
            }
            else
            {
				// Фильтруем данные по введенной подстроке
				if (DataSource.Table.Columns[currentColumn.DataPropertyName].DataType == typeof(string))
					DataSource.RowFilter = $"{currentColumn.DataPropertyName} LIKE '%{searchText}%'";
				else
				{
					txQuickSearch.Text = string.Empty;
                    MessageBox.ShowInformation(Properties.Resources.SearchForTextColumnsOnlyWarning);
				}
            }

			RefreshDependantGrid();

			return DataSource.Count;
			
		}

		public bool Contains(PresentationObject presentationObject)
		{
			if (dataGrid.DataSource == null || presentationObject == null || !IsAllowedEntity(presentationObject.Entity)) return false;
			return (GridTable.Rows.Find(presentationObject.IDs) != null);
		}

		private void SelectObject()
		{
			if(selectedObject == null) return;
			string query = selectedObject.PKWhereClause;
			DataView dataView = (DataView)dataGrid.DataSource;

			DataRow[] rows = dataView.Table.Select(query, dataView.Sort);
			if(rows.Length > 0)
			{
				DataRow[] tempRows;
				if(dataView.Sort == string.Empty)
				{
					//Если сортировка не задана - просто находим заданную строку в массиве строк DataTable
					tempRows = new DataRow[dataView.Count];
					dataView.Table.Rows.CopyTo(tempRows, 0);
				}
				else
					tempRows = dataView.Table.Select(dataView.RowFilter, dataView.Sort);

				int rowIndex = Array.IndexOf(tempRows, rows[0]);
				bm.Position = rowIndex;
			}
		}

		private void DataGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
		{
			dataGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
		}

		private void DataGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			try
			{
				if(IsMultiselectColumn(e.ColumnIndex))
				{
					MultiSelectColumnValueChanged(e);
				}
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void MultiSelectColumnValueChanged(DataGridViewCellEventArgs e)
		{
			if (_lockMultiSelect)
				return;
			PresentationObject po = CreateObject(DataSource.Table.DefaultView[e.RowIndex]);


            //PresentationObject po = CreateObject(bm.Current as DataRowView);
			CheckedStatusChanged(po, GetCell(e.RowIndex, e.ColumnIndex).Value);
			FireObjectChecked(po, ParseHelper.GetBooleanFromObject(GetCell(e.RowIndex, e.ColumnIndex).Value, false));
		}

		private void CheckedStatusChanged(PresentationObject po, object value)
		{
			if (ParseHelper.GetBooleanFromObject(value, false))
			{
				if(removedFromChecked.Contains(po))
					removedFromChecked.Remove(po);
				else
					added2Checked.Add(po);
			}
			else
			{
				if(added2Checked.Contains(po))
					added2Checked.Remove(po);
				else
					removedFromChecked.Add(po);
			}
		}

		private DataGridViewCell GetCell(int rowIndex, int columnIndex)
		{
			return dataGrid.Rows[rowIndex].Cells[columnIndex];
		}

		private bool IsMultiselectColumn(int columnIndex)
		{
			return dataGrid.Columns[columnIndex].DataPropertyName == COL_IsSelected;
		}

		private void DependantGrid_ObjectDeleted(PresentationObject presentationObject)
		{
			PresentationObject po = SelectedObject;
			if(po != null)
			{
				po.Refresh();
				UpdateRow(po);
			}
		}

		public void DeleteCurrentObject()
		{
			PresentationObject presentationObject = SelectedObject;
			if(presentationObject != null)
			{
				if(presentationObject.Delete())
				{
					DeleteRow(presentationObject);
					FireObjectDeleted(presentationObject);
				}
			}
		}

		public void EditCurrentObject()
		{
			PresentationObject presentationObject = SelectedObject;
			if(presentationObject != null)
			{
				if (presentationObject.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal))
				{
					presentationObject.DoAction(Constants.EntityActions.Edit, Globals.MdiParent, InterfaceObjects.PropertyPage);
                    //UpdateRow(presentationObject);
                }
				else if (presentationObject.ShowPassport(ParentForm))
					UpdateRow(presentationObject);
			}
		}

		private bool _lockMultiSelect = false;
        internal Action<PresentationObject> RebuildCurrentNode { get; set; }
        internal Action<PresentationObject, Entity> RebuildTree { get; set; }

        private void MultiSelectCheckAll(bool checkFlag)
		{
			dataGrid.SuspendLayout();
			//_lockMultiSelect = true;
			foreach (DataGridViewRow row in dataGrid.Rows)
			{
				if (row.Cells[0] is DataGridViewCheckBoxCell)
				{
					bool isRowChecked = row.Cells[0].Value != null && ((bool)row.Cells[0].Value);

					if ((checkFlag && !isRowChecked) || (!checkFlag && isRowChecked))
					{
						row.Cells[0].Value = checkFlag;
						//PresentationObject po = CreateObject(row.DataBoundItem as DataRowView);
						//CheckedStatusChanged(po, checkFlag);
                        dataGrid.RefreshEdit();
                    }
				}				
			}
			_lockMultiSelect = false;
			dataGrid.ResumeLayout(false);
		}
	}

    public enum ColorHighlight
    {
        Red = 0,
        Green = 1,
        Blue = 2
    }

    public class DataGridViewExBooleanCell : DataGridViewImageCell
    {
        public DataGridViewExBooleanCell()
            : base()
        {
        }

        protected override Size GetPreferredSize(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex, Size constraintSize)
        {
            return Properties.Resources.Tick.Size;
        }

        protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle, TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
        {
            if (value == null)
                return value;

            return ParseHelper.GetBooleanFromObject(value, false) ? Properties.Resources.Tick : null;
        }
    }

    public delegate void ColumnSelectedDelegate(Type columnType);
}