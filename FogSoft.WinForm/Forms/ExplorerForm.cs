using System;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;

namespace FogSoft.WinForm.Forms
{
	public partial class ExplorerForm : Form
	{
		public ExplorerForm()
		{
			InitializeComponent();
		}

		public ExplorerForm(FakeContainer container, string caption)
			: this()
		{
			Text = caption;
			tvwStructure.DependantGrid = grid;
			tvwStructure.Root = container;
			tsbFilter.Enabled = container.IsFilterable;
            grid.RecordCountChanged += RecordCountChanged;
        }

        private void RecordCountChanged(object sender, int count)
        {
			RefreshStatusInfo();
        }

        private void AlterToolbar(PresentationObject presentationObject)
		{
			if(presentationObject == null)
			{
				tsbEdit.Enabled = tsbDelete.Enabled = false;
			}
			else
			{
				tsbEdit.Enabled = presentationObject.IsActionEnabled(Constants.EntityActions.ShowPassport, ViewType.Journal) 
					|| presentationObject.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal);
				tsbDelete.Enabled = presentationObject.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal);
			}
		}

		private void tsbEdit_Click(object sender, EventArgs e)
		{
			try
			{
                if (splitContainer1.ActiveControl is IObjectControl objectControl)
                    objectControl.EditCurrentObject();
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
				IObjectControl objectControl = splitContainer1.ActiveControl as IObjectControl;
				if(objectControl != null)
					objectControl.DeleteCurrentObject();
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

		private void tsbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ExportManager.ExportExcel(grid.InternalGrid, grid.Entity);
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

		private void tvwStructure_ContainerSelected(IObjectContainer container)
		{
			try
			{
				AlterToolbar(container as PresentationObject);
				RefreshStatusInfo();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void grid_ObjectCreatedOrDeleted(PresentationObject presentationObject)
		{
			RefreshStatusInfo();
		}

		private void grid_ColumnSelected(Type columnType)
		{
			Globals.EnableSummaryButton(tsbSumma, columnType);
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

		private void tsbRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				tvwStructure.RefreshCurrentNode();
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

		private void grid_Enter(object sender, EventArgs e)
		{
			tsbEdit.Enabled = grid.Entity != null &&
			                  (grid.Entity.IsActionEnabled(Constants.EntityActions.ShowPassport, ViewType.Journal) 
							  || grid.Entity.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal));
			tsbDelete.Enabled = grid.Entity != null &&
			                    grid.Entity.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal);
		}

		private void tsbFilter_Click(object sender, EventArgs e)
		{
			tvwStructure.ShowRootFilter();
		}
	}
}