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
	}
}
