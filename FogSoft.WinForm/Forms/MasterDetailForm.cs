using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Controls;

namespace FogSoft.WinForm.Forms
{
	public partial class MasterDetailForm : Form
	{
		private readonly Dictionary<string, object> filterValues = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

		private DataTable dtData;

		public MasterDetailForm()
		{
			InitializeComponent();
		}

		public MasterDetailForm(Entity masterEntity, Entity childEntity, string caption, Dictionary<string, object> filter)
			: this()
		{
			Application.DoEvents();
			Cursor = Cursors.WaitCursor;

			if (filter != null)
			{
				foreach (KeyValuePair<string, object> pair in filter)
				{
					filterValues.Add(pair.Key, pair.Value);
				}
			}

			Text = caption;
			grdMaster.Entity = masterEntity;
			grdDetails.Entity = childEntity;
			grdMaster.DependantGrid = grdDetails;
			tsbFilter.Enabled = masterEntity.IsFilterable;
            grdMaster.RecordCountChanged += RecordCountChanged;

            if (masterEntity.IsFilterable)
				Globals.ResolveFilterInitialValues(filterValues, masterEntity.XmlFilter);
            RefreshJournal();
        }

        private void RecordCountChanged(object sender, int count)
        {
            RefreshStatusInfo();
        }

        public MasterDetailForm(Entity masterEntity, Entity childEntity, string caption)
			: this(masterEntity, childEntity, caption, null)
		{
		}

        public ToolStrip TsJournal
        {
            get { return tsJournal; }
        }

		private void AlterToolbar(Entity entity)
		{
			tsbFilter.Enabled = entity.IsFilterable;
			tsbEdit.Enabled = entity.IsActionEnabled(Constants.EntityActions.ShowPassport, ViewType.Journal);
			tsbDelete.Enabled = entity.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal);
			tsbNew.Enabled = entity.IsActionEnabled(Constants.EntityActions.AddNew, ViewType.Journal);
		}

		private void LoadData(object stateInfo)
		{
			try
			{
				//Dictionary<string, object> parameters = DataAccessor.PrepareParameters(this.masterEntity);
				//// Copy parameters from filter collection to the paramters	

				//if(stateInfo != null) {
				//  Dictionary<string, string> fltr = (Dictionary<string, string>)stateInfo;
				//  foreach(KeyValuePair<string, string> kvp in fltr)
				//    parameters.Add(kvp.Key, kvp.Value);
				//}

				//this.dtData = ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
				dtData = grdMaster.Entity.GetContent(filterValues);
				Invoke(new Globals.VoidCallback(PopulateMasterGrid));
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void PopulateMasterGrid()
		{
			grdMaster.DataSource = dtData.DefaultView;
			Cursor = Cursors.Default;
		}

		private void RefreshJournal()
		{
			WaitCallback async = new WaitCallback(LoadData);
			ThreadPool.QueueUserWorkItem(async);
		}

		private void RefreshStatusInfo()
		{
			slMasterTotal.Text = string.Format(Constants.ItemsCountTemplates.WithObjectType, grdMaster.Entity.Name, grdMaster.ItemsCount);

			if(grdMaster.SelectedObject == null)
				slDetailTotal.Text = string.Empty;
			else
				slDetailTotal.Text =
					string.Format(Constants.ItemsCountTemplates.WithObjectTypeAndParentObjectName,
					              grdDetails.Entity.Name, grdMaster.SelectedObject.Name,
					              grdDetails.ItemsCount);
		}

		private void OnMasterObjectSelected(PresentationObject presentationObject)
		{
			RefreshStatusInfo();
		}

		private void grdMasterOrDetails_Enter(object sender, EventArgs e)
		{
			try
			{
				AlterToolbar(((SmartGrid) sender).Entity);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		#region Toolbar events handlers -----------------------

		private void tsbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				if(splitContainer1.ActiveControl == grdMaster)
					ExportManager.ExportExcel(grdMaster.InternalGrid, grdMaster.Entity);
				else
					ExportManager.ExportExcel(grdDetails.InternalGrid, grdDetails.Entity);
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
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				RefreshJournal();
			}
			catch(Exception ex)
			{
				Cursor = Cursors.Default;
				ErrorManager.PublishError(ex);
			}
		}

		private void tsbNew_Click(object sender, EventArgs e)
		{
			try
			{
				PresentationObject presentationObject = grdMaster.Entity.NewObject;
				if(presentationObject.ShowPassport(this))
				{
					grdMaster.AddRow(presentationObject);
					RefreshStatusInfo();
				}
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void tsbDelete_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();

				if(splitContainer1.ActiveControl == grdMaster)
					grdMaster.DeleteCurrentObject();
				else
					grdDetails.DeleteCurrentObject();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void tsbEdit_Click(object sender, EventArgs e)
		{
			try
			{
				if(splitContainer1.ActiveControl == grdMaster)
					grdMaster.EditCurrentObject();
				else
					grdDetails.EditCurrentObject();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void tsbFilter_Click(object sender, EventArgs e)
		{
			try
			{
				if(Globals.ShowFilter(this, grdMaster.Entity, filterValues))
					RefreshJournal();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		#endregion

		private void GridsItemCountChanged(PresentationObject presentationObject)
		{
			RefreshStatusInfo();
		}

		private void grdMaster_ColumnSelected(Type columnType)
		{
			try
			{
				Globals.EnableSummaryButton(tsbSumma, columnType);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void tsbSumma_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				grdMaster.CalculateColumnSummary();
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
	}
}