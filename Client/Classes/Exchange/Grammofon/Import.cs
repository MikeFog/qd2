using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using ExcelLibrary.SpreadSheet;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes.Exchange.Grammofon
{
	internal class Import
	{
		private const string regexPatternDateHeader = @"^Даты$";

		public class IssuesData
		{
			public IssuesData(string name, DataTable issues, DataTable rollers)
			{
				Name = name;
				Issues = issues;
				Rollers = rollers;
			}

			public string Name { get; private set; }
			public DataTable Issues { get; set; }
			public DataTable Rollers { get; set; }
		}

		#region Load Data

		public bool LoadData(string filePath)
		{
			if (File.Exists(filePath))
			{
				try
				{
					Sheets = new Dictionary<int, IssuesData>();
					Workbook book = Workbook.Open(filePath);
					foreach (Worksheet worksheet in book.Worksheets)
					{
						IssuesData data = LoadData(worksheet);
						if (data != null)
							Sheets.Add(book.Worksheets.IndexOf(worksheet), data);
					}
					return true;
				}
				catch (Exception exp)
				{
					ErrorManager.LogError("Cannot Open File", exp);
				}
			}

			Sheets = null;
			return false;
		}

		public IssuesData LoadData(Worksheet worksheet)
		{
			object vallMassmedia = worksheet.Cells[1, 0].Value;
			object vallCity = worksheet.Cells[0, 0].Value;
			int index = worksheet.Book.Worksheets.IndexOf(worksheet);

			DataTable data = GetIssuesTable();

			IList<Roller> rollers = ReadRollers(worksheet);
			if (rollers.Count > 0)
				ReadIssues(worksheet, rollers, data);

			return (data.Rows.Count > 0) ? new IssuesData(string.Format("{0}. {1}: {2} ({3})", index, worksheet.Name, vallMassmedia, vallCity), data, GetTableFromRollers(rollers)) : null;
		}

		private static DataTable GetTableFromRollers(IList<Roller> rollers)
		{
			DataTable tRollers = GetRollersTable();
			foreach (Roller roller in rollers)
				tRollers.Rows.Add(rollers.IndexOf(roller), roller.Name, roller.Duration, null);
			return tRollers;
		}

		private static IList<Roller> ReadRollers(Worksheet worksheet)
		{
			IList<Roller> rollers = new List<Roller>();
			
			// Read rollers
			for (int i = 4; i <= 15; i++)
			{
				if (worksheet.Cells[i, 0] != null)
				{
					object vName = worksheet.Cells[i, 0].Value;
					if (vName != null && !string.IsNullOrEmpty(vName.ToString()) && worksheet.Cells[i, 6] != null && worksheet.Cells[i, 8] != null)
						rollers.Add(new Roller { Name = vName.ToString(), BackColor = worksheet.Cells[i, 6].Style.BackColor, Duration = ParseHelper.GetInt32FromObject(worksheet.Cells[i, 8].Value, 0) });
				}
			}
			return rollers;
		}

		private static void ReadIssues(Worksheet worksheet, IList<Roller> rollers, DataTable data)
		{
			int dateRow = FindDateHeaderRow(worksheet);
			if (dateRow < 0)
				return;

			int firstRow = dateRow + 3;
			int iRow = firstRow;
			while (worksheet.Cells[iRow, 0] != null 
			       && worksheet.Cells[iRow, 0].Value != null
			       && worksheet.Cells.LastRowIndex > iRow)
			{
				if (IsDate(worksheet.Cells[iRow, 0]))
				{
					DateTime datetime = worksheet.Cells[iRow, 0].DateTimeValue;

					for (int iCell = 0; iCell < worksheet.Cells.LastRowIndex; iCell++)
					{
						if (worksheet.Cells[dateRow, iCell] != null && worksheet.Cells[iRow, iCell] != null &&
						    IsDate(worksheet.Cells[dateRow, iCell]))
						{
							DateTime date =
								worksheet.Cells[dateRow, iCell].DateTimeValue.Date.AddHours(datetime.Hour).AddMinutes(datetime.Minute);
							
							foreach (Roller roller in rollers)
							{
								if (worksheet.Cells[iRow, iCell].Style != null && roller.BackColor == worksheet.Cells[iRow, iCell].Style.BackColor)
									data.Rows.Add(date, rollers.IndexOf(roller));
							}
						}
					}
				}

				iRow++;
			}
		}

		/// <summary>
		/// Проверяет на тип даты у колонки
		/// </summary>
		/// <param name="cell"></param>
		/// <returns></returns>
		private static bool IsDate(Cell cell)
		{
			if (cell.IsEmpty)
				return false;
			try
			{
				if (cell.Format != null && (cell.Format.FormatType == CellFormatType.Date 
						|| cell.Format.FormatType == CellFormatType.Time
						|| cell.Format.FormatType == CellFormatType.DateTime))
					return true;

				DateTime date = cell.DateTimeValue;
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static int FindDateHeaderRow(Worksheet worksheet)
		{
			int iRow = 16;
			//Find first tariff time
			while(true)
			{
				if (worksheet.Cells[iRow, 0] != null)
				{
					object oVal = worksheet.Cells[iRow, 0].Value;

					if (oVal != null)
					{
						string val = oVal.ToString();
						if (Regex.IsMatch(val, regexPatternDateHeader))
							return iRow;
					}
				}

				if (iRow >= worksheet.Cells.LastRowIndex)
					return -1;

				iRow++;
			}
		}

		private struct Roller
		{
			public string Name { get; set; }
			public Color BackColor { get; set; }
			public int Duration { get; set; }
		}

		#endregion

		private static DataTable GetIssuesTable()
		{
			DataTable table = new DataTable("issues");
			table.Columns.Add("date", typeof(DateTime));
			table.Columns.Add("rollerID", typeof(int));
			return table;
		}

		private static DataTable GetRollersTable()
		{
			DataTable table = new DataTable("rollers");
			table.Columns.Add("rollerID", typeof(int));
			table.Columns.Add("name", typeof(string));
			table.Columns.Add("duration", typeof(int));
			table.Columns.Add("realID", typeof(int));
			return table;
		}

		public IDictionary<int, IssuesData> Sheets { get; private set; }

		public DataSet ImportData(int sheetIndex, ActionOnMassmedia action, Massmedia massmedia, int paymentTypeID, Agency agency)
		{
			Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
			dictionary.Add("actionID", action.ActionId);
			dictionary.Add("massmediaID", massmedia.MassmediaId);
			dictionary.Add("agencyID", agency.AgencyId);
			dictionary.Add("paymentTypeID", paymentTypeID);
			dictionary.Add("isConfirmed", action.IsConfirmed);
			dictionary.Add("deadline", massmedia.DeadLine);
			dictionary.Add("extraChargeFirstRoller", massmedia["extraChargeFirstRoller"]);
			dictionary.Add("extraChargeSecondRoller", massmedia["extraChargeSecondRoller"]);
			dictionary.Add("extraChargeLastRoller", massmedia["extraChargeLastRoller"]);
			dictionary.Add("grammofonMistake", massmedia["grammofonMistake"]);
			DataSet ds = new DataSet();
			ds.Tables.Add(Sheets[sheetIndex].Issues);
			ds.Tables.Add(Sheets[sheetIndex].Rollers);
			return DataAccessor.LoadDataSet("CampaignImportGrammofon", dictionary, 60, ds);
		}
	}
}
