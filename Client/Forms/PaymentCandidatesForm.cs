using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Action = Merlin.Classes.Action;

namespace Merlin.Forms
{
	public partial class PaymentCandidatesForm : Form
	{
		private readonly Dictionary<PresentationObject, decimal> dicWithDistributedMoney;
		private readonly Payment payment;
		private decimal freeMoney;

		public PaymentCandidatesForm()
		{
			InitializeComponent();
		}

		public PaymentCandidatesForm(Payment payment, Entity entity, DataTable dataSource, string caption)
			: this()
		{
			this.payment = payment;
			Text = caption;
			lblSumma.Text = payment.Summa.ToString("c");
			lblRest.Text = (payment.Summa - payment.Consumed).ToString("c");
			freeMoney = payment.Summa - payment.Consumed;
			dicWithDistributedMoney = new Dictionary<PresentationObject, decimal>(dataSource.Rows.Count);

			grdCandidates.ShowMultiselectColumn = false;
			grdCandidates.Entity = entity;
			grdCandidates.DataSource = dataSource.DefaultView;
		}

		private void grdCandidates_ObjectChecked(PresentationObject presentationObject, bool state)
		{
			decimal d = decimal.Parse(presentationObject["finalPrice"].ToString()) -
			            decimal.Parse(presentationObject["paidUp"].ToString());
			decimal distributedMoney = 0;

			if (state)
			{
				if (d <= freeMoney)
					distributedMoney = d;
				else
					distributedMoney = freeMoney;
				freeMoney -= distributedMoney;
				dicWithDistributedMoney[presentationObject] = distributedMoney;
			}
			else
			{
				distributedMoney = dicWithDistributedMoney[presentationObject];
				freeMoney += distributedMoney;
			}
			lblRest.Text = freeMoney.ToString("c");
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				//Entity entity = EntityManager.GetEntity((int)Entities.PaymentStudioOrderAction);
				Entity entity = payment.ProfitEntity;
				PresentationObject paymentAction = new PresentationObject(entity);
				paymentAction[Payment.ParamNames.PaymentID] = payment.PaymentId;
				foreach (PresentationObject presentationObject in grdCandidates.Added2Checked)
				{
					paymentAction[Payment.ParamNames.Summa] = dicWithDistributedMoney[presentationObject];
					paymentAction[Action.ParamNames.ActionId] = presentationObject[Action.ParamNames.ActionId];
					paymentAction.Update();
				}
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
	}
}