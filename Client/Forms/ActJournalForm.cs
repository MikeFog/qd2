using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Properties;

namespace Merlin.Forms
{
	public partial class ActJournalForm : JournalForm
	{
		public ActJournalForm(Entity entity, string caption) 
			: base(entity, caption, true)
		{
		}
		
		public ActJournalForm(Entity entity, string caption, Dictionary<string, object> filterValues) : base(entity, caption, filterValues)
		{
			FilterBtn.Enabled = false;
		}

		protected override void PopulateDataGrid()
		{
			string actionId = null;
			DateTime datetime = DateTime.MinValue;
			DataRow firstActionRow = null;
			decimal total = 0;
			foreach (DataRow row in dtData.Rows)
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
			object[] rowSum = new object[dtData.Columns.Count];
			rowSum[dtData.Columns.IndexOf("firmName")] = "Итого";
			rowSum[dtData.Columns.IndexOf("total")] = total;
			rowSum[dtData.Columns.IndexOf("campaignId")] = 0;
			rowSum[dtData.Columns.IndexOf("currentDate2")] = DateTime.Now;
			rowSum[dtData.Columns.IndexOf("massmediaId")] = 0;
			dtData.Rows.Add(rowSum);
			base.PopulateDataGrid();
			Grid.InternalGrid.Rows[Grid.InternalGrid.Rows.Count - 1].DefaultCellStyle.Font 
				= new Font(Grid.InternalGrid.DefaultCellStyle.Font, FontStyle.Bold);

			if (dtData.DataSet != null && dtData.DataSet.Tables.Count > 1 && dtData.DataSet.Tables[1].Rows.Count > 0)
				MessageBox.ShowInformation(Resources.ActJournalMassmediaExplamation);
		}
	}
}