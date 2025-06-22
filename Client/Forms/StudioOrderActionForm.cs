using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes.Domain.StudioOrder;

namespace Merlin.Forms
{
	public partial class StudioOrderActionForm : Form
	{
		private readonly StudioOrderAction action;

		public StudioOrderActionForm()
		{
			InitializeComponent();
		}

		public StudioOrderActionForm(StudioOrderAction action)
			: this()
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				this.action = action;
				InitFormFromClass();
				EnableToolbar();
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

		private void InitFormFromClass()
		{
			lblFirmName.Text = action.FirmName;
			if (action.IsNew)
				UpdateActionStats(0, 0);
			else
				UpdateActionStats(action.TariffPrice, action.TotalPrice);

			LoadOrders();
		}

		private void LoadOrders()
		{
			grdOrders.Entity = EntityManager.GetEntity((int) Entities.StudioOrder);
			grdOrders.DataSource = action.GetContent().DefaultView;
		}

		private void UpdateActionStats(decimal tariffPrice, decimal totalPrice)
		{
			lblTariffPrice.Text = tariffPrice.ToString("c");
			lblTotalPrice.Text = totalPrice.ToString("c");
		}

		private void EnableToolbar()
		{
			tsbDelete.Enabled = tsbEdit.Enabled = tsbSetDiscount.Enabled =
			                                      grdOrders.SelectedObject != null;
		}

		private void CreateOrder(object sender, EventArgs e)
		{
			try
			{
				if (action.IsNew) action.Update();

				StudioOrder order = new StudioOrder(action);

				if (order.ShowPassport(this))
				{
					grdOrders.AddRow(order);
					RefreshFormData();
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void DeleteOrder(object sender, EventArgs e)
		{
			try
			{
				PresentationObject presentationObject = grdOrders.SelectedObject;
				if (presentationObject != null && presentationObject.Delete())
				{
					grdOrders.DeleteRow(presentationObject);
					RefreshFormData();
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

		private void EditOrder(object sender, EventArgs e)
		{
			try
			{
				PresentationObject presentationObject = grdOrders.SelectedObject;
				if (presentationObject != null && presentationObject.ShowPassport(this))
				{
					grdOrders.UpdateRow(presentationObject);
					RefreshFormData();
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

		private void SetDiscount(object sender, EventArgs e)
		{
			try
			{
				if (grdOrders.SelectedObject == null) return;
				StudioOrder order = (StudioOrder)grdOrders.SelectedObject;
				ManagerDiscountForm fDiscount = new ManagerDiscountForm(order);

				if (fDiscount.ShowDialog(this) == DialogResult.OK)
				{
					Application.DoEvents();
					Cursor = Cursors.WaitCursor;

					order.Ratio = fDiscount.Ratio;
					order.Update();
					grdOrders.UpdateRow(order);

					RefreshFormData();
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

		private void RefreshFormData()
		{
			action.Refresh();
			UpdateActionStats(action.TariffPrice, action.TotalPrice);
			EnableToolbar();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				if (chkPrintAgreement.Checked)
					action.PrintContracts(Globals.MdiParent, false);
				if (chkPrintBill.Checked)
					action.PrintBills(Globals.MdiParent, false, false);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}
	}
}