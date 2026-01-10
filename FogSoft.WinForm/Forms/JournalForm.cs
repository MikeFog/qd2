using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.Properties;

namespace FogSoft.WinForm.Forms
{
	public partial class JournalForm : Form, IJournal
	{
		#region Members ---------------------------------------

		private readonly Entity _entity;
		private readonly Dictionary<string, object> _filterValues =	new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
		protected DataTable _dtData;
        private readonly bool _needRefreshOnLoad = false;

        #endregion

        #region Constructors ----------------------------------

        public JournalForm()
		{
			InitializeComponent();
			SubInitializeComponent();
		}

		public JournalForm(Entity entity, string caption, bool doNotRefresh)
			:this()
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				Text = caption;
				this._entity = entity;
				grid.Entity = entity;
				AlterToolbar();
                grid.RecordCountChanged += RecordCountChanged;

                //// Don't load data on form load if there are filters for this Entity
                if (!entity.IsFilterable)
				{
					if (!doNotRefresh)
						_needRefreshOnLoad = true;
				}
				else
				{
					Globals.ResolveFilterInitialValues(_filterValues, this._entity.XmlFilter);
					if ((_filterValues.Count > 0 || ConfigurationUtil.IsJournalLoadWhenEmptyFilter) && !doNotRefresh)
						_needRefreshOnLoad = true;
					else
						Cursor = Cursors.Default;
				}

				SetHighlightEnabled();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		public JournalForm(Entity entity, string caption)
			: this(entity, caption, false)
		{
		}

		public JournalForm(Entity entity, string caption, Dictionary<string, object> filterValues)
			: this(entity, caption, true)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                this._filterValues = filterValues;
				_needRefreshOnLoad = true;
				SetHighlightEnabled();
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

		public JournalForm(Entity entity, string caption, DataTable dt)
			: this()
		{
			try
			{
				Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                Text = caption;
				this._entity = entity;
				grid.Entity = entity;
				AlterToolbar();

				grid.DataSource = dt.DefaultView;
				tsbRefresh.Enabled = false;
				SetHighlightEnabled();
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

		public Dictionary<string, object> Filters
		{
			get { return _filterValues; }
		}

		#endregion

	    public ToolStrip TsJournal
	    {
	        get { return tsJournal; }
	    }

	    public SmartGrid Grid
	    {
	        get { return grid; }
	    }

		public ToolStripButton FilterBtn
		{
			get { return tsbFilter; }
		}

	    protected virtual void SubInitializeComponent()
        {

        }

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (_needRefreshOnLoad)
			{
                grid.RefreshAll += RefreshJournal;
                RefreshJournal();
            }
		}

		private void AlterToolbar()
		{
			tsbFilter.Enabled = _entity.IsFilterable;
			tsbEdit.Enabled = _entity.IsActionEnabled(Constants.EntityActions.ShowPassport, ViewType.Journal);
			tsbDelete.Enabled = _entity.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal);
			tsbNew.Enabled = _entity.IsActionEnabled(Constants.EntityActions.AddNew, ViewType.Journal);
			tsbRefresh.Enabled = _entity.IsRefreshEnabled();
		}

        private void SetHighlightEnabled()
        {
            tsbHighlight.Visible = grid.Entity != null && grid.DataSource != null && grid.Entity.GetColumnsForHighlight().Count > 0;
        }

        public void RefreshJournal()
		{
			tsbHighlight.Visible = false;
			WaitCallback async = new WaitCallback(LoadData);

			if(_filterValues.Count > 0)
			{
				tsbFilter.Image = Resources.FilterSet;
				ThreadPool.QueueUserWorkItem(async, _filterValues);
            }
			else
			{
				tsbFilter.Image = Resources.Filter;
				ThreadPool.QueueUserWorkItem(async);
			}
		}

		protected virtual void LoadData(object stateInfo)
		{
			try
			{
				if (!IsDisposed && IsHandleCreated)
					Globals.SetWaitCursor(this);
                Application.DoEvents();

                grid.Entity.ClearCache();
				

                _dtData = stateInfo != null ? grid.Entity.GetContent((Dictionary<string, object>) stateInfo) : grid.Entity.GetContent();

				if (!IsHandleCreated)
					Thread.Sleep(500);

				if (!IsDisposed && IsHandleCreated)
					Invoke(new Globals.VoidCallback(PopulateDataGrid));
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				if (!IsDisposed && IsHandleCreated)
					Globals.SetDefaultCursor(this);
			}
		}

		protected virtual void PopulateDataGrid()
		{
			try
			{
				if (_dtData != null)
					grid.DataSource = _dtData.DefaultView;
				RefreshStatusInfo();
				SetHighlightEnabled();
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

		private void RefreshStatusInfo()
		{
			slTotal.Text = string.Format(Constants.ItemsCountTemplates.Default, grid.ItemsCount);
		}

        private void RecordCountChanged(object sender, int count)
        {
            slTotal.Text = string.Format(Constants.ItemsCountTemplates.Default, count);
        }

        private void grid_ColumnSelected(Type columnType)
		{
			Globals.EnableSummaryButton(tsbSumma, columnType);
		}

		private void grid_ObjectDeleted(PresentationObject presentationObject)
		{
			RefreshStatusInfo();
		}

		private void tsbNew_Click(object sender, EventArgs e)
		{
			try
			{
				PresentationObject presentationObject = _entity.NewObject;
				if(presentationObject.ShowPassport(this))
				{
					grid.AddRow(presentationObject);
					RefreshStatusInfo();
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

		private void tsbEdit_Click(object sender, EventArgs e)
		{
			try
			{
				grid.EditCurrentObject();
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

		private void tsbDelete_Click(object sender, EventArgs e)
		{
			try
			{
				grid.DeleteCurrentObject();
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

		private void tsbRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				RefreshJournal();
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

		private XPathNavigator xmlFilter = null;
		
		public XPathNavigator XmlFilter
		{
			get { return xmlFilter; }
			set { xmlFilter = value; }
		}

		public delegate bool FilterClick(Form parent, Entity entity, XPathNavigator xmlFilter, Dictionary<string, object> filterValues);

		public event FilterClick OnFilterClick;

		private void tsbFilter_Click(object sender, EventArgs e)
		{
			try
			{
				//Если прикручен обработчик на фильтр то используем только его
				bool filterReturn = OnFilterClick != null ? (OnFilterClick(this, _entity, xmlFilter, _filterValues)) 
					: (xmlFilter == null) ? Globals.ShowFilter(this, _entity, _filterValues)
				                    	: Globals.ShowFilter(this, _entity, xmlFilter, _filterValues);
				if (filterReturn)
				{
					Cursor = Cursors.WaitCursor;
					RefreshJournal();
				}
			}
			catch(Exception ex)
			{
				Cursor = Cursors.Default;
				ErrorManager.PublishError(ex);
			}
		}

		private void tsbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				ProgressForm.Show(this, Export_DoWorkEventHandler, "Экспорт...", null);
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

		void Export_DoWorkEventHandler(object sender, DoWorkEventArgs e)
		{
			ExportManager.ExportExcel(grid.InternalGrid, grid.Entity, grid.IsNeedHighlight);
		}

		private void tsbSumma_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				grid.CalculateColumnSummary();
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

		private void JournalForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F5)
				RefreshJournal();
		}

		private void tsbHighlight_Click(object sender, EventArgs e)
		{
			FormGridHighlight frm = new FormGridHighlight(grid);
			frm.ShowDialog(this);
		}
	}
}