using System;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Forms
{
	// Данные и расчёт — в ядре (FirmBalanceReport), их зовёт и веб-экран «Баланс для фирмы».
	public class FrmFirmIssuesBalance : FrmFirmBalance
	{
		public FrmFirmIssuesBalance()
		{
		}

		public FrmFirmIssuesBalance(FirmBalanceIssues balance, DateTime startDate)
			: base(balance, startDate)
		{
		}

		protected override void InitOnLoad()
		{
			base.InitOnLoad();

			DataSet ds = FirmBalanceReport.LoadFilterData();
			OpFirms.SetDataSource(EntityManager.GetEntity((int)Entities.Firm), ds.Tables["firm"].Copy());

			GrdAgency.Entity = EntityManager.GetEntity((int)Entities.Agency);
			GrdAgency.DataSource = ds.Tables["agency"].Copy().DefaultView;
		}

		private FirmBalanceReport.Filter CurrentFilter()
		{
			return new FirmBalanceReport.Filter
			{
				Start = DateStart,
				Finish = DateFinish,
				FirmId = FirmID,
				ManagerId = UserID,
				Agencies = AgenciesIDString,
				ShowWhite = ShowWhite,
				ShowBlack = ShowBlack,
			};
		}

		protected override decimal RefreshBalanceOnStartOfInterval()
		{
			return FirmBalanceReport.StartBalance(CurrentFilter());
		}

		protected override decimal RefreshActionInfo(FogSoft.WinForm.Controls.SmartGrid grid)
		{
			var result = new FirmBalanceReport.Result();
			FirmBalanceReport.LoadActions(CurrentFilter(), result);
			grid.Entity = result.ActionEntity;
			grid.DataSource = result.Actions.DefaultView;
			return result.ActionsTotal;
		}

		protected override decimal RefreshPaymentInfo(FogSoft.WinForm.Controls.SmartGrid grid)
		{
			var result = new FirmBalanceReport.Result();
			FirmBalanceReport.LoadPayments(CurrentFilter(), result);
			grid.Entity = result.PaymentEntity;
			grid.DataSource = result.Payments.DefaultView;
			return result.PaymentsTotal;
		}

		protected override DataTable ReloadUsers(PresentationObject firm)
		{
			if (firm == null)
				return null;
			return FirmBalanceReport.LoadManagers(firm.IDs[0]);
		}
	}
}
