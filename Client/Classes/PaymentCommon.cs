using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть (DoAction, SelectActions) — в PaymentCommon.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class PaymentCommon : Payment
	{
		private struct ActionNames
		{
			public const string SelectActionsToPay = "SelectActionsToPay";
		}

		public PaymentCommon()
			: base(EntityManager.GetEntity((int) Entities.PaymentCommon))
		{
		}

		public PaymentCommon(DataRow row)
			: base(EntityManager.GetEntity((int) Entities.PaymentCommon), row)
		{
		}

		public override Entity ProfitEntity
		{
			get { return EntityManager.GetEntity((int) Entities.PaymentCommonAction); }
		}

		// DoAction и SelectActions переехали в PaymentCommon.WinForms.cs.

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ActionNames.SelectActionsToPay)
				return bool.Parse(this["isEnabled"].ToString()) && base.IsActionEnabled(actionName, type)
				       && (Consumed < Summa);
			return base.IsActionEnabled(actionName, type);
		}

		/// <summary>Акции — кандидаты на оплату этим платежом.</summary>
		internal DataTable GetPaymentCandidates()
		{
			Entity entityPaymentCandidate =
				EntityManager.GetEntity((int) Entities.ActionPaymentCandidate);
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(entityPaymentCandidate);

			procParameters[ParamNames.PaymentID] = PaymentId;
			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			return ds.Tables[Constants.TableNames.Data];
		}
	}
}