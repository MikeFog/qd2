namespace Merlin.Classes
{
	public sealed class TableColumns
	{
		private TableColumns()
		{
		}

		public struct Bill
		{
			public const string BillNo = "billNo";
			public const string BillDate = "billDate";
		}

		public struct Bank
		{
			public const string BankId = "bankID";
			public const string CorAccount = "corAccount";
			public const string Bik = "bik";
		}

		public struct ProgramIssue
		{
			public const string ProgramID = "programID";
			public const string Bonus = "bonus";
		}
	}
}