using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Данные калькулятора цены (docs/tasks/web-price-calculator.md, этап К.1): модель цен на период и скидки строк одним
	/// вызовом SQL. Расчёт — <see cref="PriceCalculatorEngine"/>.
	/// </summary>
	public static class PriceCalculatorData
	{
		/// <summary>
		/// Станции группы (0 — все) с сегментами прайс-листов на период и периоды сезонного коэффициента менеджера —
		/// <c>pc_GetStationsPriceModel</c>. Станции без прайс-листа с тарифами на период в модель не попадают, как в десктопе.
		/// </summary>
		public static PcModel LoadModel(int massmediaGroupId, DateTime dateFrom, DateTime dateTo, int userId)
		{
			var p = DataAccessor.CreateParametersDictionary();
			if (massmediaGroupId > 0)
				p["MassmediaGroupID"] = massmediaGroupId;
			p["DateFrom"] = dateFrom.Date;
			p["DateTo"] = dateTo.Date;
			p["userID"] = userId;
			DataSet ds = DataAccessor.LoadDataSet("pc_GetStationsPriceModel", p);

			var model = new PcModel
			{
				DateFrom = dateFrom.Date,
				DateTo = dateTo.Date,
				MassmediaGroupId = massmediaGroupId,
				UserId = userId,
			};

			// первая выборка — станции в порядке названия; её цены (наибольшие по сегментам) не нужны: диапазон считает движок
			foreach (DataRow r in ds.Tables[0].Rows)
			{
				model.Stations.Add(new PcStation
				{
					MassmediaId = Convert.ToInt32(r["massmediaID"]),
					Name = Convert.ToString(r["name"]),
					GroupName = r["groupName"] as string,
					ShortName = r["shortName"] as string,
				});
			}

			var byId = model.Stations.ToDictionary(s => s.MassmediaId);
			foreach (DataRow r in ds.Tables[1].Rows)
			{
				PcStation station;
				if (!byId.TryGetValue(Convert.ToInt32(r["massmediaID"]), out station))
					continue;
				station.Segments.Add(new PcSegment
				{
					PricelistId = Convert.ToInt32(r["pricelistID"]),
					Start = Convert.ToDateTime(r["SegmentStart"]).Date,
					End = Convert.ToDateTime(r["SegmentEnd"]).Date,
					PrimeWeekday = Convert.ToDecimal(r["PrimePricePerSecWeekday"]),
					NonPrimeWeekday = Convert.ToDecimal(r["NonPrimePricePerSecWeekday"]),
					PrimeWeekend = Convert.ToDecimal(r["PrimePricePerSecWeekend"]),
					NonPrimeWeekend = Convert.ToDecimal(r["NonPrimePricePerSecWeekend"]),
					ExtraFirst = Convert.ToDecimal(r["extraChargeFirstRoller"]),
					ExtraSecond = Convert.ToDecimal(r["extraChargeSecondRoller"]),
					ExtraLast = Convert.ToDecimal(r["extraChargeLastRoller"]),
				});
			}
			foreach (var station in model.Stations)
				station.Segments.Sort((a, b) => a.Start.CompareTo(b.Start));

			// третья выборка — периоды менеджера; процедура до Deploy/53 отдаёт периоды всех — отбор по менеджеру здесь
			foreach (DataRow r in ds.Tables[2].Rows)
			{
				if (Convert.ToInt32(r["userID"]) != userId)
					continue;
				object ratio = r["maxRatio"];
				model.RatioPeriods.Add(new PcRatioPeriod(
					Convert.ToDateTime(r["startDate"]).Date,
					Convert.ToDateTime(r["finishDate"]).Date,
					ratio == DBNull.Value ? 1m : Convert.ToDecimal(ratio)));
			}
			return model;
		}

		/// <summary>
		/// «Сезонный коэфф.:» по умолчанию, как в десктопе (<c>TemplateEditorControl.SetManagerDiscount</c>): администратору —
		/// 1, остальным — наименьший maxRatio периодов UserDiscount, пересекающих период (<c>GetUserDiscount</c>), без них — 1.
		/// У не-администратора это и нижняя граница поля.
		/// </summary>
		public static decimal DefaultManagerRatio(SecurityManager.User user, DateTime dateFrom, DateTime dateTo)
		{
			if (user == null || user.IsAdmin)
				return 1m;
			return user.GetDiscount(dateFrom, dateTo);
		}

		/// <summary>
		/// «Менеджер:» (ВК-3 а, только администратору): активные пользователи, кому пункт калькулятора выдан группой или лично,
		/// без служебных администраторов и «admin» — <c>pc_CalculatorManagers</c> (Deploy/55); по фамилии и имени.
		/// </summary>
		public static List<KeyValuePair<int, string>> Managers()
		{
			DataSet ds = DataAccessor.LoadDataSet("pc_CalculatorManagers", DataAccessor.CreateParametersDictionary());
			return ds.Tables[0].Rows.Cast<DataRow>()
				.Select(r => new KeyValuePair<int, string>(Convert.ToInt32(r["userID"]), Convert.ToString(r["name"])))
				.ToList();
		}

		/// <summary>Агентства с шаблоном КП (поле агентства «path2proposalTemplate») — список «Создать КП».</summary>
		public static List<KeyValuePair<int, string>> ProposalAgencies()
		{
			var result = new List<KeyValuePair<int, string>>();
			foreach (DataRow r in Agency.LoadAgencies(true).Tables[0].Rows)
			{
				if (!string.IsNullOrWhiteSpace(TemplateFile(r)))
					result.Add(new KeyValuePair<int, string>(Convert.ToInt32(r[Agency.ParamNames.AgencyId]),
						Convert.ToString(r[FogSoft.WinForm.Constants.Parameters.Name])));
			}
			return result;
		}

		/// <summary>
		/// Полный путь шаблона КП агентства, как десктоп: папка <c>ProposalTemplateFolder</c> из настроек (относительная —
		/// от папки программы, абсолютная — как есть) и имя файла агентства; null — у агентства нет шаблона. От имени файла
		/// берётся только имя (<c>Path.GetFileName</c>): путь из карточки агентства не уводит службу веба из папки шаблонов.
		/// </summary>
		public static string ProposalTemplatePath(int agencyId)
		{
			string file = ProposalTemplateFile(agencyId);
			if (string.IsNullOrWhiteSpace(file))
				return null;
			return System.IO.Path.Combine(ProposalTemplateFolder(), System.IO.Path.GetFileName(file.Trim()));
		}

		/// <summary>Папка шаблонов КП: <c>ProposalTemplateFolder</c> из настроек, относительная — от папки программы.</summary>
		public static string ProposalTemplateFolder()
		{
			return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationUtil.ProposalTemplateFolder);
		}

		// список агентств (sl_agencies) отдаёт все поля агентства, в том числе имя файла шаблона КП
		private static string ProposalTemplateFile(int agencyId)
		{
			DataRow row = Agency.LoadAgencies(true).Tables[0].AsEnumerable()
				.FirstOrDefault(r => Convert.ToInt32(r[Agency.ParamNames.AgencyId]) == agencyId);
			return row == null ? null : TemplateFile(row);
		}

		private static string TemplateFile(DataRow agencyRow)
		{
			if (!agencyRow.Table.Columns.Contains(Agency.ParamNames.Path2ProposalTemplate))
				return null;
			object file = agencyRow[Agency.ParamNames.Path2ProposalTemplate];
			return file == DBNull.Value ? null : Convert.ToString(file);
		}

		/// <summary>
		/// «Объёмная скидка» каждой строки и «Пакетная скидка» отмеченных — одним вызовом <c>pc_CalcDiscountsModel</c>
		/// вместо вызова на строку (десктоп звал <c>hlp_CompanyDiscountCalculate</c> по строке и пакет отдельно, К-18).
		/// Правила те же: процедура зовёт <c>hlp_CompanyDiscountCalculate</c> и <c>pc_PackageDiscountCalculateModel</c>.
		/// </summary>
		public static PcDiscounts Discounts(DateTime startDate, IList<PcDiscountRow> rows)
		{
			var result = new PcDiscounts();
			if (rows == null || rows.Count == 0)
				return result;

			var table = new DataTable();
			table.Columns.Add("massmediaID", typeof(short));
			table.Columns.Add("tariffPrice", typeof(decimal));
			table.Columns.Add("durationSec", typeof(int));
			table.Columns.Add("isSelected", typeof(bool));
			foreach (var r in rows)
				table.Rows.Add((short)r.MassmediaId, r.TariffPrice, r.DurationSec, r.Selected);

			var p = DataAccessor.CreateParametersDictionary();
			p["startDate"] = startDate.Date;
			p["campaignTypeID"] = (byte)1;
			p["rows"] = new TvpValue(table, "dbo.pc_CalcRow");
			DataSet ds = DataAccessor.LoadDataSet("pc_CalcDiscountsModel", p);

			foreach (DataRow r in ds.Tables[0].Rows)
				result.Company[Convert.ToInt32(r["massmediaID"])] = Convert.ToDecimal(r["companyDiscount"]);
			if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0 && ds.Tables[1].Rows[0]["packageDiscount"] != DBNull.Value)
				result.Package = Convert.ToDecimal(ds.Tables[1].Rows[0]["packageDiscount"]);
			return result;
		}
	}
}
