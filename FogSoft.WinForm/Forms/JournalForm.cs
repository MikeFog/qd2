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

		private readonly Entity entity;

		private readonly Dictionary<string, object> filterValues =
			new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

		protected DataTable dtData;

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
				this.entity = entity;
				grid.Entity = entity;
				AlterToolbar();
                grid.RecordCountChanged += RecordCountChanged;

                //// Don't load data on form load if there are filters for this Entity
                if (!entity.IsFilterable)
				{
					if (!doNotRefresh)
						needRefreshOnLoad = true;
				}
				else
				{
					Globals.ResolveFilterInitialValues(filterValues, this.entity.XmlFilter);
					if ((filterValues.Count > 0 || ConfigurationUtil.IsJournalLoadWhenEmptyFilter) && !doNotRefresh)
						needRefreshOnLoad = true;
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

		private void SetHighlightEnabled()
		{
			tsbHighlight.Visible = grid.Entity != null && grid.DataSource != null && grid.Entity.GetColumnsForHighlight().Count > 0;
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
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				this.filterValues = filterValues;
				needRefreshOnLoad = true;
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
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				Text = caption;
				this.entity = entity;
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
			get { return filterValues; }
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

		private readonly bool needRefreshOnLoad = false;

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (needRefreshOnLoad)
				RefreshJournal();
		}

		private void AlterToolbar()
		{
			tsbFilter.Enabled = entity.IsFilterable;
			tsbEdit.Enabled = entity.IsActionEnabled(Constants.EntityActions.ShowPassport, ViewType.Journal);
			tsbDelete.Enabled = entity.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal);
			tsbNew.Enabled = entity.IsActionEnabled(Constants.EntityActions.AddNew, ViewType.Journal);
			tsbRefresh.Enabled = entity.IsRefreshEnabled();
		}

		public void RefreshJournal()
		{
			tsbHighlight.Visible = false;
			WaitCallback async = new WaitCallback(LoadData);

			if(filterValues.Count > 0)
			{
				tsbFilter.Image = Resources.FilterSet;
				ThreadPool.QueueUserWorkItem(async, filterValues);
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
				Application.DoEvents();

				if (!IsDisposed && IsHandleCreated)
					Globals.SetWaitCursor(this);

				grid.Entity.ClearCache();

				dtData = stateInfo != null ? grid.Entity.GetContent((Dictionary<string, object>) stateInfo) : grid.Entity.GetContent();

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
				if (dtData != null)
					grid.DataSource = dtData.DefaultView;
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
				PresentationObject presentationObject = entity.NewObject;
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
				bool filterReturn = OnFilterClick != null ? (OnFilterClick(this, entity, xmlFilter, filterValues)) 
					: (xmlFilter == null) ? Globals.ShowFilter(this, entity, filterValues)
				                    	: Globals.ShowFilter(this, entity, xmlFilter, filterValues);
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