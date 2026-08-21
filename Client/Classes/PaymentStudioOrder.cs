using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	public abstract class Payment : ObjectContainer
	{
		public struct ParamNames
		{
			public const string PaymentID = "paymentId";
			public const string Summa = "summa";
			public const string Consumed = "consumed";
		}

		protected Payment(Entity entity)
			: base(entity)
		{
			parameters["userName"] = SecurityManager.LoggedUser.FullName;
		}

		public Payment(Entity entity, DataRow row) : base(entity, row)
		{
		}

		public decimal Summa
		{
			get { return decimal.Parse(parameters[ParamNames.Summa].ToString()); }
		}

		public decimal Consumed
		{
			get { return decimal.Parse(parameters[ParamNames.Consumed].ToString()); }
		}

		public int PaymentId
		{
			get { return int.Parse(parameters[ParamNames.PaymentID].ToString()); }
		}

		public abstract Entity ProfitEntity { get; }
	}

	// UI-часть (DoAction, SelectActions) — в PaymentStudioOrder.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class PaymentStudioOrder : Payment
	{
		private struct ActionNames
		{
			public const string SelectActionsToPay = "SelectActionsToPay";
		}

		public PaymentStudioOrder() : base(EntityManager.GetEntity((int) Entities.PaymentStudioOrder))
		{
		}

		public PaymentStudioOrder(DataRow row) : base(EntityManager.GetEntity((int) Entities.PaymentStudioOrder), row)
		{
		}

		public override Entity ProfitEntity
		{
			get { return EntityManager.GetEntity((int) Entities.PaymentStudioOrderAction); }
		}

		// DoAction и SelectActions переехали в PaymentStudioOrder.WinForms.cs.

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
				EntityManager.GetEntity((int) Entities.StudioOrderActionPaymentCandidate);
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(entityPaymentCandidate);

			procParameters[ParamNames.PaymentID] = PaymentId;
			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;
			return ds.Tables[Constants.TableNames.Data];
		}
	}
}