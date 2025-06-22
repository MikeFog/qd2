using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace FogSoft.WinForm.Controls
{
	public partial class ObjectPicker2 : UserControl
	{
		public event ObjectDelegate ObjectSelected;
		public event EmptyDelegate SelectionCleared;

		private bool _isCreateNewAllowed = false;
		private RelationScenario _scenario;
		private Entity entity;
		private DataTable dataTable;
		private PresentationObject presentationObject;

		private readonly Dictionary<string, object> filterValues =
			new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
		
		public Dictionary<string, object> FilterValues
		{
			get { return filterValues; }
		}

		public ObjectPicker2()
		{
			InitializeComponent();
			SetTooltips();
		}

		public ObjectPicker2(RelationScenario scenario) : this()
		{
			this._scenario = scenario;
			btnCreateNew.Visible = _isCreateNewAllowed;
		}

		public bool IsCreateNewAllowed
		{
			set
			{
				_isCreateNewAllowed = btnCreateNew.Visible = value;
			}
		}

		public RelationScenario Scenario
		{
			set { _scenario = value; }
		}

		[Browsable(false)]
		public PresentationObject SelectedObject
		{
			get { return presentationObject; }
		}

		[Browsable(false)]
		public string ObjectName
		{
			set
			{
				txtObjectName.Text = value;
			}
		}

		public void SetDataSource(Entity dataEntity, DataTable dtData)
		{
			entity = dataEntity;
			dataTable = dtData;
			btnSelect.Enabled = true;
		}

		public void SelectObject(object Id)
		{
			// If Object pivker works with datatable object with given ID
			// can be found there. But if we're working with tree structure
			// we just create new Presentation Object
			if(dataTable == null)
			{
				LoaObjectFormDB(Id);
			}
			else
			{
				// Set PK columns in the DataTable for quick finding necessary row
				if(entity.PKColumns.Length > 0)
				{
					DataColumn[] pkColumns = new DataColumn[entity.PKColumns.Length];
					for(int i = 0; i < entity.PKColumns.Length; i++)
						pkColumns[i] = dataTable.Columns[entity.PKColumns[i]];
					dataTable.PrimaryKey = pkColumns;
				}

				// Find necessary row
				DataRow row = dataTable.Rows.Find(Id);
				// And create object from this row
				if (row != null)
				{
					presentationObject = entity.CreateObject(row);
					txtObjectName.Text = presentationObject.Name;
				}
			}
		}

		private void SetTooltips()
		{
			ttOjectPicker.SetToolTip(btnClear, "Отменить выбор");
			ttOjectPicker.SetToolTip(btnSelect, "Выбрать объект");
			ttOjectPicker.SetToolTip(btnCreateNew, "Создать новый объект");
		}

		private void ObjectPicker2_Resize(object sender, EventArgs e)
		{
			if(Height != txtObjectName.Height)
				Height = txtObjectName.Height;
		}

		private void LoaObjectFormDB(object Id)
		{
			Dictionary<string, object> parameters =
				new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase) {{entity.PKColumns[0], Id.ToString()}};
			presentationObject = entity.CreateObject(parameters);
			presentationObject.Refresh();
			txtObjectName.Text = presentationObject.Name;
		}

		private void btnSelect_Click(object sender, EventArgs e)
		{
			try
			{
				Parent.Cursor = Cursors.WaitCursor;
				if(_scenario == null)
				{
//          if(this.dataTable == null && !this.entity.IsFilterable) this.LoadData();
					if (dataTable == null) LoadData();

					SelectionForm selector = new SelectionForm(entity, dataTable.DefaultView, "Выбрать объект");
					if (selector.ShowDialog(Parent) == DialogResult.OK)
						presentationObject = selector.SelectedObject;
				}
				else
				{
					TreeViewSelector tvSelector = new TreeViewSelector(_scenario);
					if (tvSelector.ShowDialog(Parent) == DialogResult.OK)
						presentationObject = tvSelector.SelectedObject;
				}
				if (presentationObject != null)
					txtObjectName.Text = presentationObject.Name;
                if (ObjectSelected != null && presentationObject != null)                  
                    ObjectSelected(presentationObject);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Parent.Cursor = Cursors.Default;
			}
		}

		private void LoadData()
		{
			// If entity supports Filters - don't load data here. Give user
			// a chance to set filters before loading huge amount of data

			// Добавил новый кеш parameters для object selector - так как забитые по умолчанию значения затираются значениями из фильтра
			Dictionary<string, object> parameters = DataAccess.DataAccessor.CreateParametersDictionary();
			if (entity.IsFilterable)
				Globals.ResolveFilterInitialValues(parameters, entity.XmlFilter);
			foreach (KeyValuePair<string, object> pair in filterValues)
			{
				if (parameters.ContainsKey(pair.Key))
					parameters[pair.Key] = pair.Value;
				else 
					parameters.Add(pair.Key, pair.Value);
			}
			dataTable = entity.GetContent(filterValues);
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			try
			{
				ClearSelected();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Parent.Cursor = Cursors.Default;
			}
		}

		public void ClearSelected()
		{
			txtObjectName.Text = string.Empty;
			presentationObject = null;
			if(SelectionCleared != null) SelectionCleared();
		}

		private void btnCreateNew_Click(object sender, EventArgs e)
		{
			try
			{
				PresentationObject obj = entity.NewObject;
				if(obj.ShowPassport(ParentForm))
				{
					presentationObject = obj;
					txtObjectName.Text = presentationObject.Name;
					if (dataTable == null) LoadData();
					Globals.AddObject2DataTable(dataTable, obj);
					if(ObjectSelected != null) ObjectSelected(presentationObject);
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

		public void SetEntity(Entity dataEntity)
		{
			entity = dataEntity;
		}

		public new bool Enabled
		{
			get
			{
				return base.Enabled;
			}
			set
			{
				base.Enabled = value;
				txtObjectName.Enabled = value;
				btnClear.Enabled = value;
				btnCreateNew.Enabled = value;
				btnSelect.Enabled = value;
			}
		}
	}
}