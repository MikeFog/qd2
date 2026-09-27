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
		public struct ActionNames
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

		/// <summary>
		/// Почему «Выбрать акции для оплаты» погашено — по тем же условиям, что
		/// IsActionEnabled. null — причина не в платеже (например, нет права).
		/// </summary>
		public string SelectActionsToPayUnavailableReason()
		{
			if (!bool.Parse(this["isEnabled"].ToString()))
				return Tr.T("Платёж недоступен для присвоения акциям — галочка в карточке платежа.");
			if (Consumed >= Summa)
				return Tr.T("Платёж распределён полностью.");
			return null;
		}

		/// <summary>Акции — кандидаты на оплату этим платежом.</summary>
		public DataTable GetPaymentCandidates()
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