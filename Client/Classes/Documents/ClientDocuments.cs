using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Reports;
using F = Merlin.Classes.Documents.DocumentFields.Names;

namespace Merlin.Classes.Documents
{
	/// <summary>
	/// Значения полей Word-шаблонов документов для клиента (docs/tasks/web-reports.md §8.4).
	/// Источники те же, что у Crystal-отчётов десктопа, — чтобы документ совпадал по данным:
	/// <c>GenericReport.GetTextPart</c>/<c>PrintFooter</c> (реквизиты), <c>BillReport</c>
	/// (<c>rpt_GenericBill</c>, сумма прописью, QR), <c>ContractReport</c> (ставка НДС),
	/// <c>OnAirInquireReport</c> (выходы, цена за месяц).
	/// </summary>
	public static class ClientDocuments
	{
		/// <summary>Как у десктопа для пустых «в лице» и «регистрации» фирмы — линия для заполнения от руки.</summary>
		private const string Blank = "_________________________";

		/// <summary>
		/// Договор и спонсорский договор. <paramref name="action"/> = null — договор из
		/// карточки фирмы без акции (тогда номера нет).
		/// </summary>
		public static DocumentData Contract(Action action, Firm firm, Agency agency, DateTime date, string number,
			bool withSignature)
		{
			DocumentData data = Common(agency, firm ?? action.Firm, action, withSignature);
			data.SetFlag(F.ByAction, action != null);
			SetHeader(data, number, date);
			SetTaxRate(data, agency.GetTaxValue(date));
			return data;
		}

		/// <summary>
		/// Счёт и счёт-договор; <paramref name="month"/> — счёт за один месяц (строки только за него).
		/// </summary>
		public static DocumentData Bill(Action action, Agency agency, string number, DateTime date, DateTime? month,
			bool withSignature)
		{
			DocumentData data = Common(agency, action.Firm, action, withSignature);
			SetHeader(data, number, date);

			DataTable rows = LoadBillRows(action, agency, month);
			decimal total = 0, tax = 0;
			int rowNumber = 0;
			data.SetEmptyList(F.Rows);
			foreach (DataRow row in rows.Rows)
			{
				decimal rowSum = row["price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["price"]);
				// НДС строки округляется до копеек, итог НДС — сумма округлённых (как формула fTaxRounded в GenericBill.rpt).
				decimal rowTax = row["tax"] == DBNull.Value ? 0 : Math.Round(Convert.ToDecimal(row["tax"]), 2);
				total += rowSum;
				tax += rowTax;
				data.AddItem(F.Rows)
					.Set(F.RowNumber, (++rowNumber).ToString(CultureInfo.CurrentCulture))
					.Set(F.RowName, row["name"].ToString())
					.Set(F.RowQuantity, row["quantity"].ToString())
					.Set(F.RowSum, FormatMoney(rowSum))
					.Set(F.RowSumWithoutTax, FormatMoney(rowSum - rowTax))
					.Set(F.RowTax, FormatMoney(rowTax));
			}
			// Как BillReport.CalculateBillTotal.
			total = Math.Round(total, 2, MidpointRounding.ToEven);
			SetSums(data, total, tax);
			data.Set(F.TotalWithoutTax, FormatMoney(total - tax));

			decimal rate = agency.GetTaxValue(date);
			SetTaxRate(data, rate);
			data.SetFlag(F.ForMonth, month.HasValue)
				.Set(F.Month, month.HasValue ? MonthText(month.Value) : string.Empty);

			byte[] qr = QrPaymentHelper.GenerateBillQrPng(agency, number, action.ActionId, total, tax, rate);
			// 3 × 3 см, как в Crystal (BillReport.QrSizeTwips).
			data.SetImage(F.Qr, qr == null ? null : new DocumentImage(qr, QrSizeEmu, QrSizeEmu));

			SecurityManager.User manager = action.Creator;
			data.Set(F.ManagerName, manager == null ? string.Empty : (manager.LastName + " " + manager.FirstName).Trim())
				.Set(F.ManagerPhone, manager == null ? string.Empty : (manager.Phone ?? string.Empty).Trim())
				.Set(F.ManagerEmail, manager == null ? string.Empty : (manager.Email ?? string.Empty).Trim())
				.Set(F.ManagerContacts, manager == null ? string.Empty : manager.ContactInfo.Trim());
			return data;
		}

		private const long QrSizeEmu = 3 * 360000;

		/// <summary>
		/// Эфирная справка по кампании на радиостанции за месяц. Кампания — <see cref="PresentationObject"/>,
		/// потому что класс <c>Campaign</c> внутренний (как <c>MediaPlanJob.ForCampaign</c>).
		/// </summary>
		public static DocumentData OnAirInquire(PresentationObject campaignObject, Agency agency, Massmedia massmedia,
			DateTime month, bool withPrice, bool withSignature)
		{
			var campaign = (Campaign)campaignObject;
			var monthStart = new DateTime(month.Year, month.Month, 1);
			// Те же границы, что у десктопа (CampaignOnSingleMassmedia.PrintOnAirInquire).
			DataSet issues = campaign.GetOnAirInquireReport(massmedia.MassmediaId, campaign.CampaignId,
				monthStart, monthStart.AddMonths(1).AddDays(-1));

			DocumentData data = Common(agency, campaign.Action.Firm, campaign.Action, false);
			data.Set(F.Month, MonthText(monthStart))
				.Set(F.StationName, massmedia.Prefix)
				.Set(F.StationFounder, massmedia.Founder)
				.Set(F.StationGroup, massmedia.GroupName)
				.Set(F.StationRadio, massmedia.NameWithoutGroup)
				.Set(F.StationDirector, massmedia.Director)
				.Set(F.StationCertificate, massmedia.CertificateIssued);

			byte[] painting = massmedia.SignatureBytes ?? FirstPainting(issues.Tables[0]);
			data.SetImage(F.StationSignature, withSignature ? DocumentImage.FromBytes(painting) : null);

			AddIssues(data, F.Issues, issues.Tables[0]);
			AddIssues(data, F.SponsorIssues, issues.Tables.Count > 1 ? issues.Tables[1] : null);
			data.Set(F.IssueCount, issues.Tables[0].Rows.Count.ToString(CultureInfo.CurrentCulture))
				.SetFlag(F.HasSponsorIssues, issues.Tables.Count > 1 && issues.Tables[1].Rows.Count > 0);

			data.SetFlag(F.WithPrice, withPrice);
			decimal price = 0, taxPrice = 0;
			if (withPrice)
				campaign.GetPriceByPeriodWithTax(monthStart,
					monthStart.AddMonths(1).AddSeconds(-1), massmedia.MassmediaId, false, null,
					out price, out _, out taxPrice);
			SetSums(data, price, taxPrice);
			SetTaxRate(data, agency.GetTaxValue(monthStart));
			return data;
		}

		/// <summary>
		/// Агентства, по которым печатаются документы акции (или кампании): как десктопный
		/// «Выбор агентств» (<c>Agency.SelectAgencies</c>) — если агентство одно, выбирать нечего.
		/// </summary>
		public static IList<Agency> AgenciesOf(PresentationObject owner)
		{
			// Копия: PrepareParameters дописывает в словарь служебные ключи.
			var parameters = new Dictionary<string, object>(owner.Parameters, StringComparer.OrdinalIgnoreCase);
			DataTable candidates;
			List<PresentationObject> single = Agency.GetAgenciesForSelection(owner, parameters, out candidates);
			var result = new List<Agency>();
			if (single != null)
				result.AddRange(single.Cast<Agency>());
			else if (candidates != null)
				foreach (DataRow row in candidates.Rows)
					result.Add(Agency.GetAgencyByID(Convert.ToInt32(row[Agency.ParamNames.AgencyId])));
			return result;
		}

		private static DocumentData Common(Agency agency, Firm firm, Action action, bool withAgencySignature)
		{
			var data = new DocumentData();
			PresentationObject agencyBank = agency.Bank;
			data.Set(F.AgencyName, agency.PrefixWithName)
				.Set(F.AgencyShortName, agency.Name)
				.Set(F.AgencyFullForm, agency.FullPrefix)
				.Set(F.AgencyActingBy, agency.ReportString)
				.Set(F.AgencyRegistration, agency.Registration)
				.Set(F.AgencyDirector, agency.Director)
				.Set(F.AgencyBookKeeper, agency.BookKeeper)
				.Set(F.AgencyAddress, agency.Address)
				.Set(F.AgencyPhone, agency.Phone)
				.Set(F.AgencyEmail, agency.Email)
				.Set(F.AgencyInn, agency.INN)
				.Set(F.AgencyKpp, agency.KPP)
				.Set(F.AgencyOgrn, agency.EGRN)
				.Set(F.AgencyAccount, agency.Account)
				.Set(F.AgencyBank, BankName(agencyBank))
				.Set(F.AgencyCorAccount, BankValue(agencyBank, Organization.ParamNames.BankAccount))
				.Set(F.AgencyBik, BankValue(agencyBank, Organization.ParamNames.BankBIK))
				.Set(F.AgencyPlace, agency.ReportPlace)
				.SetImage(F.AgencySignature, withAgencySignature ? DocumentImage.FromBytes(AgencyPainting(agency)) : null);

			PresentationObject firmBank = firm.Bank;
			data.Set(F.FirmName, firm.PrefixWithName)
				.Set(F.FirmShortName, firm.Name)
				.Set(F.FirmActingBy, string.IsNullOrEmpty(firm.ReportString) ? Blank : firm.ReportString)
				.Set(F.FirmRegistration, string.IsNullOrEmpty(firm.Registration) ? Blank : firm.Registration)
				.Set(F.FirmDirector, firm.Director)
				.Set(F.FirmAddress, firm.Address)
				.Set(F.FirmPhone, firm.Phone)
				.Set(F.FirmEmail, firm.Email)
				.Set(F.FirmInn, firm.INN)
				.Set(F.FirmKpp, firm.KPP)
				.Set(F.FirmOgrn, firm.EGRN)
				.Set(F.FirmAccount, firm.Account)
				.Set(F.FirmBank, BankName(firmBank))
				.Set(F.FirmCorAccount, BankValue(firmBank, Organization.ParamNames.BankAccount))
				.Set(F.FirmBik, BankValue(firmBank, Organization.ParamNames.BankBIK));

			data.Set(F.ActionNumber, action == null ? string.Empty : action.ActionId.ToString(CultureInfo.CurrentCulture));
			return data;
		}

		private static void SetHeader(DocumentData data, string number, DateTime date)
		{
			data.Set(F.Number, number)
				.Set(F.Date, date.ToString("d", CultureInfo.CurrentCulture))
				.Set(F.DateInWords, date.ToString("D", CultureInfo.CurrentCulture));
		}

		private static void SetTaxRate(DocumentData data, decimal rate)
		{
			data.SetFlag(F.WithTax, rate > 0)
				.Set(F.TaxRate, rate > 0 ? rate.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty);
		}

		private static void SetSums(DocumentData data, decimal total, decimal tax)
		{
			data.Set(F.Total, FormatMoney(total))
				.Set(F.TotalInWords, Money.MoneyToString(total, false))
				.Set(F.TaxSum, FormatMoney(tax));
		}

		private static void AddIssues(DocumentData data, string listName, DataTable table)
		{
			data.SetEmptyList(listName);
			if (table == null)
				return;
			foreach (DataRow row in table.Rows)
				data.AddItem(listName)
					.Set(F.IssueName, row["name"].ToString())
					.Set(F.IssueDuration, row["durationString"].ToString())
					.Set(F.IssueDate, row["issueDate"].ToString())
					.Set(F.IssueTime, row["issueTime"].ToString());
		}

		private static DataTable LoadBillRows(Action action, Agency agency, DateTime? month)
		{
			// Как BillReport.LoadBillData.
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Action.ParamNames.ActionId] = action.ActionId;
			parameters[Agency.ParamNames.AgencyId] = agency.AgencyId;
			if (month.HasValue)
			{
				var start = new DateTime(month.Value.Year, month.Value.Month, 1);
				parameters["beginDate"] = start;
				parameters["endDate"] = start.AddMonths(1).AddDays(-1);
			}
			return DataAccessor.LoadDataSet("rpt_GenericBill", parameters).Tables[0];
		}

		private static byte[] AgencyPainting(Agency agency)
		{
			if (agency.SignatureBytes != null)
				return agency.SignatureBytes;
			return FirstPainting(agency.LoadPainting());
		}

		private static byte[] FirstPainting(DataTable table)
		{
			if (table == null || table.Rows.Count == 0 || !table.Columns.Contains("dirPainting"))
				return null;
			return table.Rows[0]["dirPainting"] as byte[];
		}

		private static string BankName(PresentationObject bank)
		{
			return bank == null ? string.Empty : bank.Name;
		}

		private static string BankValue(PresentationObject bank, string column)
		{
			return bank == null ? string.Empty : Convert.ToString(bank[column]);
		}

		private static string FormatMoney(decimal value)
		{
			return value.ToString("N2", CultureInfo.CurrentCulture);
		}

		/// <summary>«сентябрь 2026» (десктоп на .NET Framework писал «Сентябрь»; в .NET 10 месяцы со строчной).</summary>
		private static string MonthText(DateTime month)
		{
			return DateTimeFormatInfo.CurrentInfo.MonthNames[month.Month - 1] + " " + month.Year.ToString(CultureInfo.CurrentCulture);
		}
	}
}
