using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.Reports
{
	public class VolumeOfRealizationReportGenerator : ReportGenerator
	{
		public VolumeOfRealizationReportGenerator() 
			: base(EntityManager.GetEntity((int)Entities.StatsVolumeofRealization))
		{
		}

		public VolumeOfRealizationReportGenerator(Dictionary<string, object> filterValues)
			: base(EntityManager.GetEntity((int)Entities.StatsVolumeofRealization), filterValues)
		{
		}

		public override string GetValueColumn(ReportType type)
		{
			return "sum1";
		}

		public override string TitleColumn
		{
			get
			{
				if (Data != null)
				{
					foreach (DataColumn column in Data.Columns)
					{
						if (column.ColumnName != "RowNum"
						    && column.ColumnName != "sum1"
						    && column.ColumnName != "percent")
							return column.ColumnName;
					}
				}
				return base.TitleColumn;
			}
		}

		public override Dictionary<ReportType, string> ReportTypes
		{
			get
			{
				Dictionary<ReportType, string> list = base.ReportTypes;
				int count = 0;
				foreach (KeyValuePair<string, object> value in FilterValues)
				{
					if (value.Key.StartsWith("IsGroupBy") 
						&& value.Value is bool && (bool)value.Value)
						count++;
				}
				if (count > 1 && list.ContainsKey(ReportType.Pie))
					list.Remove(ReportType.Pie);
				if (count > 1 && list.ContainsKey(ReportType.Bar))
					list.Remove(ReportType.Bar);
				return list;
			}
		}
	}
}
