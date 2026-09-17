using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;

namespace FogSoft.WinForm
{
	// Часть Globals без зависимости от UI. Остальное (MdiParent, диалоги, журналы,
	// курсоры, работа с буфером обмена) — в Globals.cs и в сборку без UI не входит.
	// Логика не менялась, код перенесён как есть.
	// См. docs/tasks/web-migration.md, этап 0.
	public static partial class Globals
	{
		/// <summary>
		/// Наборы строк для фильтра — процедура с ключом
		/// <c>EntityId_Load_FilterPage</c> (<see cref="InterfaceObjects.FilterPage"/>),
		/// отдельная от паспортной <c>PropertyPage</c>. Перенесено из UI-половины
		/// (ShowFilter в Globals.cs) без изменений: зависимостей от UI не было —
		/// только DataAccessor, как и у PresentationObject.LoadPassportData().
		/// null, если у сущности нет процедуры фильтра: тогда контролы фильтра
		/// обязаны грузить список сами (см. docs/tasks/web-migration.md, этап 2).
		/// </summary>
		public static DataSet PrepareForFilter(Entity entity)
		{
			Dictionary<string, object> parameters =
				DataAccessor.PrepareParameters(entity, InterfaceObjects.FilterPage, Constants.Actions.Load);

			DataSet ds = null;
			if (DataAccessor.IsProcedureExist(parameters))
				ds = DataAccessor.DoAction(parameters) as DataSet;
			return ds;
		}

		public static void ResolveFilterInitialValues(
			Dictionary<string, object> filterValues, string filterXml)
		{
			if(filterXml == null || filterXml.Trim() == string.Empty) return;

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(filterXml);

			foreach(XmlNode node in xmlDoc.SelectNodes("//*[@value]"))
			{
				string name = node.Attributes[PageControl.Attributes.Name].Value;
				string value = node.Attributes[PageControl.Attributes.Value].Value;
				var argument = StringUtil.SubstringAfter(value, ":");
				DateTime currentMonthStart;

                if (Regex.IsMatch(value, ".*:.*"))
				{
					value = StringUtil.SubstringBefore(value, ":");
				}
				switch(value)
				{
                    case PageControl.InitialValueAbbreviations.PREV_MONTH_BEGIN:
                        currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        filterValues[name] = currentMonthStart.AddMonths(-1).ToString();
                        break;
                    case PageControl.InitialValueAbbreviations.PREV_MONTH_END:
                        currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        filterValues[name] = currentMonthStart.AddDays(-1).ToString();
                        break;
                    case PageControl.InitialValueAbbreviations.LAST_MONTH:
						filterValues[name] = DateTime.Today.AddMonths(-1).ToString();
						break;

					case PageControl.InitialValueAbbreviations.LAST_WEEK:
						filterValues[name] = DateTime.Today.AddDays(-7).ToString();
						break;

					case PageControl.InitialValueAbbreviations.TODAY:
						filterValues[name] = DateTime.Today.ToString();
						break;

					case PageControl.InitialValueAbbreviations.StartOfTheMonth:
						filterValues[name] = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString();
						break;

                    case PageControl.InitialValueAbbreviations.StartOfTheLastMonth:
                        filterValues[name] = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1).ToString();
                        break;

					case PageControl.InitialValueAbbreviations.EndOfTheMonth:
						filterValues[name] =
							new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1).ToString();
						break;

					case PageControl.InitialValueAbbreviations.LoggedUser:
						filterValues[name] = SecurityManager.LoggedUser.Id;
						break;

					default:
						filterValues[name] = value;
						break;
				}
			}
		}


		#region Db Version

		public static int DBVersion
		{
			get
			{
				if (!_dbversion.HasValue)
					return ConfigurationUtil.WorkingDbVesion;
				return _dbversion.Value;
			}
			set
			{
				_dbversion = value;
			}
		}

		private static int? _dbversion;

		#endregion
	}
}
