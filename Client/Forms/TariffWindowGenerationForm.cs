using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Properties;
using Merlin.Classes;
using Merlin.Classes.FakeContainers;

namespace Merlin.Forms
{
	public partial class TariffWindowGenerationForm : Form
	{
		public TariffWindowGenerationForm()
		{
			InitializeComponent();
			InitializeToolbar();
			tvwStructure.Root =
				new MassmediasContainer("Радиостанция", null, RelationManager.GetScenario(RelationScenarios.TariffWindows));
		}

		private void InitializeToolbar()
		{
			tsbEdit.Image = Resources.EditItem;
			tsbDelete.Image = Resources.DeleteItem;
			tsbRefresh.Image = Resources.Refresh;
			tsbExcel.Image = Resources.ExportExcel;
			tsJump2Date.Image = Properties.Resources.calendar;
			tsbFilter.Image = Resources.Filter;
		}

		private void tvwStructure_ContainerSelected(IObjectContainer container)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				RefreshGrid(container as Pricelist);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void RefreshGrid(Pricelist pricelist)
		{
			windowGrid.Pricelist = pricelist;
			windowGrid.RefreshGrid();
		}

		private void tsbRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;
				tvwStructure.RefreshCurrentNode();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void tsbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ExportManager.ExportExcel(windowGrid.RawDataGridView, null);
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

		private void tsbDelete_Click(object sender, EventArgs e)
		{
			try
			{
				IObjectControl objectControl = splitContainer1.ActiveControl as IObjectControl;
				if (objectControl != null)
					objectControl.DeleteCurrentObject();
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

		private void tsbEdit_Click(object sender, EventArgs e)
		{
			try
			{
				IObjectControl objectControl = splitContainer1.ActiveControl as IObjectControl;
				if (objectControl != null)
					objectControl.EditCurrentObject();
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

		private void tsJump2Date_Click(object sender, EventArgs e)
		{
			try
			{
				FrmDateSelector fSelector = new FrmDateSelector("Выбор даты");
				fSelector.Mode = FrmDateSelector.SelectorMode.SelectOne;
				if (fSelector.ShowDialog(this) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor = Cursors.WaitCursor;
					windowGrid.CurrentDate = fSelector.StartDate;
					windowGrid.RefreshGrid();
				}
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

		private void tsbFilter_Click(object sender, EventArgs e)
		{
			tvwStructure.ShowRootFilter();
		}
	}
}