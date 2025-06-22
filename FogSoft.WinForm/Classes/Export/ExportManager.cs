using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using FogSoft.WinForm.Classes.Export.MSExcel;
using FogSoft.WinForm.Classes.Export.OOCalc;
using log4net;
using unoidl.com.sun.star.bridge.oleautomation;
using DataTable=System.Data.DataTable;

namespace FogSoft.WinForm.Classes.Export
{
	public static class ExportManager
	{
		private static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
		private enum ExportOffice
		{
			MS,
			OO
		}

		private static ExportOffice ExportOfficeType
		{
			get
			{
				ExportOffice ret = ExportOffice.OO;
				Log.Info("Try to find settings");
				string export = ConfigurationManager.AppSettings["ExportOffice"];
				Log.InfoFormat("Export Settings: {0}", export);
				if (!string.IsNullOrEmpty(export) && Enum.IsDefined(typeof(ExportOffice), export))
					ret = (ExportOffice) Enum.Parse(typeof (ExportOffice), export);
				Log.InfoFormat("Find settings: {0}", ret);
				return ret;
			}
		}

		private static IExportDocument doc;

		public static IExportDocument Application
		{
			get
			{
				if (doc != null && doc.Visible())
					return doc;

				try
				{
					return GetExApp(ExportOfficeType);
				}
				catch(Exception e)
				{
					Log.Error("ExportManager - get_Application", e);
					return GetExApp(ExportOfficeType == ExportOffice.MS ? ExportOffice.OO : ExportOffice.MS);
				}
			}
		}

		private static IExportDocument GetExApp(ExportOffice type)
		{
			if (type == ExportOffice.MS)
			{
				Log.Info("Create new ms document");
                return doc = new MSExportDocument();
			}
			else if (type == ExportOffice.OO)
			{
				Log.Info("Create new oo document");
				return doc = new OOExportDocument();
			}
			return null;
		}

		public static void OnAppQuit()
		{
			if (doc != null)
				doc.OnAppQuit();
		}
		
		private class Column
		{
			public readonly string MappingName;
			public readonly string ColumnName;
			public readonly Type columnType;

			public Column(string MappingName, string ColumnName, DataView dv)
			{
				this.MappingName = MappingName;
				this.ColumnName = ColumnName;
				columnType = dv.Table.Columns[MappingName].DataType;
			}

			internal object FormatValue(object val)
			{
				if(columnType == typeof(bool))
					return (bool) val ? "Да" : "Нет";
				return val;
			}
		}

		public static void ExportExcel(DataGridView dg, Entity entity)
		{
			ExportExcel(dg, entity, false);
		}

		public static void ExportExcel(DataGridView dg, Entity entity, bool needHighlight)
		{
			CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;
			//Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            try
			{
				DataView dv = null;

				if(dg.DataSource is DataSet)
					dv = ((DataSet) dg.DataSource).Tables[dg.DataMember].DefaultView;
				else if(dg.DataSource is DataTable)
					dv = ((DataTable) dg.DataSource).DefaultView;
				else if(dg.DataSource is DataView)
					dv = (DataView) dg.DataSource;
				//else 
				//	throw new ApplicationException("Неизвестный источник данных " + (dg.DataSource != null ? string.Format("'{0}'", dg.DataSource.GetType()) : string.Empty));

				if (dv == null || dv.Count == 0)
					return;

				IExportDocument docC = Application;

				docC.StartExport();
				IDocumentSheet sheet = docC.GetNewSheet("", "Tahoma", 10);
				CopyData2WorkSheet(sheet, dg, dv, entity, needHighlight);
                
				sheet.SetAutoFitCells();
				docC.FinishExport();
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = oldCulture;
			}
		}

		private static void SetColumnFormat(DataGridView greed, DataView dv, IDocumentSheet sheet, Entity entity, int top, int left)
		{
            if (entity != null && dv.Table != null)
			{
				foreach (Entity.Attribute entityAttribute in entity.SortedAttributes)
				{
					if (dv.Table.Columns.Contains(entityAttribute.Name))
					{
						ColumnInfo columnInfo;
						entity.ColumnsInfo.TryGetValue(entityAttribute.Name, out columnInfo);
						int index = GetColumnIndex(greed, entityAttribute);
						if (index >= 0)
						{
							Type type = null;
							if (ColumnInfo.IsMoneyData(columnInfo, entityAttribute))
								type = typeof(Money);
							else if (ColumnInfo.IsFloatData(columnInfo, entityAttribute))
								type = typeof(float);
							else if (entityAttribute.DataType == "time")
								type = typeof(Time);
							else if (entityAttribute.DataType == "date2")
							{
								type = typeof(DateTime);
							}

                            if (type != null)
								sheet.SetFormatForCell(top, index + left, dv.Count + top, index + left, type);
						}
					}
				}
			}
		}

		private static int GetColumnIndex(DataGridView greed, Entity.Attribute entityAttribute)
		{
			int index = -1;
			foreach (DataGridViewColumn column in greed.Columns)
			{
				if (column.GetType() != typeof(DataGridViewImageColumn))
				{
					index++;
					if (column.DataPropertyName == entityAttribute.Name)
						return index;
				}
			}
			return -1;
		}

		public static void CopyData2WorkSheet(IDocumentSheet sheet, DataTable dt, int left, int top, bool rotate)
        {
            object[,] data = ProcessData(dt, rotate);
            PopulateWorksheet(data, left, top, sheet);
        }

		public static void CopyData2WorkSheet(IDocumentSheet sheet, DataTable dt, int left, int top)
		{
		    CopyData2WorkSheet(sheet, dt, left, top, false);
		}

		public static void PopulateWorksheet(object[,] data, int left, int top, IDocumentSheet ws)
		{
			int height = data.GetLength(0);
			int width = data.GetLength(1);
			int bottom = top + height - 1;
			int right = left + width - 1;

			if (height == 0 && width == 0)
				return;

			ws.SetValuesForRange(top, left, bottom, right, data);
			ws.SetBordersStyles(top, left, bottom, right, height > 1);
		}

		private static void CopyData2WorkSheet(IDocumentSheet ws, DataGridView dg, DataView dv, Entity entity, bool needHighlight)
		{

			List<Column> columns = GetColumns(dg, dv);
			object[,] data = ProcessData(dv, columns);

			int left = 1, top = 1, width = columns.Count - 1, height = dv.Count;
			MakeCaption(ws, left, ref top, width, columns);

            ws.SetValuesForRange(top, left, top + height - 1, left + width, data);

            ws.SetBordersStyles(top - 1, left, top + height - 1, left + width, columns.Count > 1);
			SetColumnFormat(dg, dv, ws, entity, top, left);
			SetHighlight(dg, ws, left, top, width, needHighlight);
		}

		private static void SetHighlight(DataGridView dg, IDocumentSheet ws, int left, int top, int width, bool needHighlight)
		{
			if (needHighlight)
			{
				foreach (DataGridViewRow row in dg.Rows)
				{
					Color color = row.Cells[0].Style.BackColor;
					ws.SetBackground(row.Index + top, left, row.Index + top, left + width, color.R, color.G, color.B);
				}
			}
		}

		private static object[,] ProcessData(DataView dv, List<Column> columns)
		{
			object[,] data = new object[dv.Count,columns.Count];
			for (int i = 0; i < dv.Count; i++)
				for (int j = 0; j < columns.Count; j++)
				{
					data[i, j] = columns[j].FormatValue(dv[i][columns[j].MappingName]);
					//ErrorManager.Log.Info(data[i, j]);
				}
			return data;
		}

        private static object[,] ProcessData(DataTable dt, bool rotate)
		{
            object[,] data = rotate ? new object[dt.Columns.Count, dt.Rows.Count] : new object[dt.Rows.Count, dt.Columns.Count];
			for(int i = 0; i < dt.Rows.Count; i++)
                for (int j = 0; j < dt.Columns.Count; j++)
                {
					if (rotate)
						data[j, i] = dt.Rows[i][j];
					else
						data[i, j] = dt.Rows[i][j];
                }
            return data;
		}

		private static void MakeCaption(
			IDocumentSheet ws, int left, ref int top, int width, List<Column> columns)
		{
			for(int i = 0; i < columns.Count; i++)
				ws.SetCellValue(top, left + i, columns[i].ColumnName);
			ws.SetBoldForRange(top, left, top, left + width);
			top++;
		}

		private static List<Column> GetColumns(DataGridView dg, DataView dv)
		{
			List<Column> columns = new List<Column>(dg.Columns.Count);
			
			foreach(DataGridViewColumn cs in dg.Columns)
			{
				if (dv.Table.Columns.Contains(cs.DataPropertyName))
					columns.Add(new Column(cs.DataPropertyName, cs.HeaderText, dv));
			}
			return columns;
		}
	}
}