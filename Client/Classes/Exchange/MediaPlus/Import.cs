using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes.Exchange.MediaPlus
{
	internal class Import
	{
		private IList<SimpleIssue> issues;

		public Massmedia Massmedia { get; private set; }

		public bool LoadData(string filePath)
		{
			if (File.Exists(filePath))
			{
				try
				{
					XmlFormat format = new XmlFormat(filePath);
					issues = format.Issues;
					Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
					dictionary["mediaPlusMassmediaID"] = format.MediaPlusMassmediaID;
					DataTable dt = EntityManager.GetEntity((int) Entities.MassMedia).GetContent(dictionary);
					Massmedia = (dt.Rows.Count == 1) ? new Massmedia(dt.Rows[0]) : null;
					return true;
				}
				catch (Exception exp)
				{
					ErrorManager.LogError("Cannot Open File", exp);
				}
			}

			return false;
		}

		private DataTable GetTable()
		{
			DataTable table = new DataTable("issues");
			table.Columns.Add("duration", typeof(int));
			table.Columns.Add("date", typeof(DateTime));
			table.Columns.Add("hour", typeof(byte));
			table.Columns.Add("isfirsthalf", typeof(bool));
			foreach (SimpleIssue issue in issues)
				table.Rows.Add(issue.Duration, issue.Date, issue.Hour, issue.IsFirstHalf);
			return table;
		}

		public DataSet ImportData(ActionOnMassmedia action, int paymentTypeID, Agency agency)
		{
			Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
			dictionary.Add("actionID", action.ActionId);
			dictionary.Add("massmediaID", Massmedia.MassmediaId);
			dictionary.Add("agencyID", agency.AgencyId);
			dictionary.Add("paymentTypeID", paymentTypeID);
			dictionary.Add("isConfirmed", action.IsConfirmed);
			dictionary.Add("deadline", Massmedia.DeadLine);
			dictionary.Add("extraChargeFirstRoller", Massmedia["extraChargeFirstRoller"]);
			dictionary.Add("extraChargeSecondRoller", Massmedia["extraChargeSecondRoller"]);
			dictionary.Add("extraChargeLastRoller", Massmedia["extraChargeLastRoller"]);
			return DataAccessor.LoadDataSet("CampaignImportMediaPlus", dictionary, GetTable());
		}
	}
}