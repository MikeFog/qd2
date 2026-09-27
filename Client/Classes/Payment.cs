using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;

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

		/// <summary>
		/// Оплатить акции этим платежом: по вызову ProfitEntity.UpdateItem на акцию (сумма
		/// прибавляется к уже оплаченной). Каждая запись — отдельный вызов, без общей
		/// транзакции, как было в PaymentCandidatesForm. Ключ — actionID, значение — сумма.
		/// </summary>
		public void PayActions(IEnumerable<KeyValuePair<int, decimal>> sums)
		{
			PresentationObject paymentAction = new PresentationObject(ProfitEntity);
			paymentAction[ParamNames.PaymentID] = PaymentId;
			foreach (KeyValuePair<int, decimal> pair in sums)
			{
				paymentAction[ParamNames.Summa] = pair.Value;
				paymentAction[Action.ParamNames.ActionId] = pair.Key;
				paymentAction.Update();
			}
		}
	}
}
