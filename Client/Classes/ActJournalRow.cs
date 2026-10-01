using System;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	internal partial class ActJournalRow : PresentationObject
	{
		public ActJournalRow() : base(EntityManager.GetEntity((int) Entities.ActJournalRow))
		{
		}

		public ActJournalRow(DataRow row) : base(EntityManager.GetEntity((int) Entities.ActJournalRow), row)
		{
		}

		// DoAction/GetCampaign переехали в ActJournalRow.WinForms.cs.

		/// <summary>У строки «Итого» (campaignId = 0) печатать нечего — действия погашены.</summary>
		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (parameters.TryGetValue(Campaign.ParamNames.CampaignId, out object id) && id != null
				&& id != DBNull.Value && Convert.ToInt32(id) == 0)
				return false;
			return base.IsActionEnabled(actionName, type);
		}
	}

	/// <summary>
	/// «Выписать акт выполненных работ»: результат CampaignsForActJournalRetrieve перед показом.
	/// Вынесено из ActJournalForm.PopulateDataGrid — десктоп и веб показывают одно и то же.
	/// </summary>
	public static class ActJournal
	{
		/// <summary>
		/// Первая строка акции получает «Сумму по кампаниям» (сумма campaignTotal её строк плюс
		/// mistake первой); в следующих строках той же акции номер и фирма гасятся, повторная
		/// дата — тоже; в конце — строка «Итого» (жирная, row_style = bold) с суммой campaignTotal.
		/// </summary>
		public static void Prepare(DataTable data)
		{
			string actionId = null;
			DateTime datetime = DateTime.MinValue;
			DataRow firstActionRow = null;
			decimal total = 0;
			foreach (DataRow row in data.Rows)
			{
				total += decimal.Parse(row["campaignTotal"].ToString());
				if (actionId != row["actionId"].ToString())
				{
					actionId = row["actionId"].ToString();
					row["total"] = decimal.Parse(row["campaignTotal"].ToString()) + decimal.Parse(row["mistake"].ToString());
					firstActionRow = row;
				}
				else
				{
					row["actionId"] = row["firmName"] = DBNull.Value;
					firstActionRow["total"] =
						decimal.Parse(firstActionRow["total"].ToString()) + decimal.Parse(row["campaignTotal"].ToString());
				}
				if (datetime != DateTime.Parse(row["currentDate"].ToString()))
					datetime = DateTime.Parse(row["currentDate"].ToString());
				else
					row["currentDate"] = DBNull.Value;
			}

			if (!data.Columns.Contains(RowStyleColumn))
				data.Columns.Add(RowStyleColumn, typeof(string));
			object[] rowSum = new object[data.Columns.Count];
			rowSum[data.Columns.IndexOf("firmName")] = Tr.T("Итого");
			rowSum[data.Columns.IndexOf("total")] = total;
			rowSum[data.Columns.IndexOf("campaignId")] = 0;
			rowSum[data.Columns.IndexOf("currentDate2")] = DateTime.Now;
			rowSum[data.Columns.IndexOf("massmediaId")] = 0;
			rowSum[data.Columns.IndexOf(RowStyleColumn)] = "bold";
			data.Rows.Add(rowSum);
		}

		/// <summary>
		/// Вторая таблица ответа непуста — в акте есть выпуски на станциях, не отмеченных
		/// трафик-менеджером как обработанные (ActJournalMassmediaExplamation).
		/// </summary>
		public static bool HasUnprocessedMassmedia(DataTable data) =>
			data.DataSet != null && data.DataSet.Tables.Count > 1 && data.DataSet.Tables[1].Rows.Count > 0;

		public static string UnprocessedMassmediaMessage => Tr.T(Properties.Resources.ActJournalMassmediaExplamation);

		// Служебная колонка SmartGrid/ObjectList: оформление строки задаёт данные.
		private const string RowStyleColumn = "row_style";
	}
}