using System;
using System.Collections.Generic;
using System.Data;

namespace FogSoft.WinForm.Classes
{
	public enum ReportType
	{
		Unknow = 0,
		Grid = 1,
		Pie = 2,
		Bar = 3
	}

	public class ReportGenerator
	{
		public ReportGenerator(Entity entity, Dictionary<string, object> filterValues)
		{
			Entity = entity;
			FilterValues = filterValues;
		}

		public ReportGenerator(Entity entity)
		{
			Entity = entity;
			FilterValues = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
			Globals.ResolveFilterInitialValues(FilterValues, Entity.XmlFilter);
		}

		#region Members ---------------------------------------

		public Entity Entity { get; private set;}

		public Dictionary<string, object> FilterValues { get; private set; }

		public DataTable Data { get; private set; }

		#endregion

		public virtual Dictionary<ReportType, string> ReportTypes
		{
			get
			{
                Dictionary<ReportType, string> types = new Dictionary<ReportType, string>
				                                              	{
																	{ReportType.Grid, "Список"},
																	{ReportType.Pie, "Пирог"}
																	//{ReportType.Bar, "Колонки"},
				                                              		
																};
				return types;
			}
		}

		public virtual string GetValueColumn(ReportType type) { return null; }
		public virtual string TitleColumn { get { return null; } }

		public void ReloadData()
		{
			Entity.ClearCache();

			Data = FilterValues.Count > 0 ? Entity.GetContent(FilterValues) : Entity.GetContent();
		}
	}
}
