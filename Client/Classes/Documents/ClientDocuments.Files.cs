using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes.GridExport;

namespace Merlin.Classes.Documents
{
	/// <summary>
	/// Готовые .docx для веба (docs/tasks/web-reports.md §8 этапы 5, 7): шаблон агентства на дату
	/// документа (или начальный, <see cref="StartingTemplates.ForPrint"/>), заполненный данными.
	/// Имена файлов — как у десктопа. Ошибка шаблона — <see cref="DocumentTemplateException"/>.
	/// </summary>
	public static partial class ClientDocuments
	{
		/// <summary>Имя действия «Эфирная справка» у кампаний (класс <c>Campaign</c> в ядре внутренний).</summary>
		public const string PrintOnAirInquireAction = Campaign.ActionNames.PrintOnAirInquire;

		/// <summary>
		/// Документ по акции: договор, спонсорский договор, счёт-договор или счёт
		/// (<paramref name="month"/> — счёт за один месяц, как «Распечатать по месяцам»).
		/// </summary>
		public static ExportFile ActionDocumentFile(Action action, Agency agency, DocumentKind kind, DocumentBill bill,
			DateTime? month, bool withSignature)
		{
			string number = bill.Number.ToString(CultureInfo.CurrentCulture);
			byte[] template = StartingTemplates.ForPrint(agency.AgencyId, kind, bill.Date);
			DocumentData data = kind == DocumentKind.Bill || kind == DocumentKind.BillContract
				? Bill(action, agency, number, bill.Date, kind == DocumentKind.Bill ? month : null, withSignature)
				: Contract(action, null, agency, bill.Date, number, withSignature);
			string name = month.HasValue
				? Tr.Format("{0} №{1} к акции {2} за {3} для {4}", KindName(kind), number, action.ActionId, MonthText(month.Value), action.FirmName)
				: Tr.Format("{0} №{1} к акции {2} для {3}", KindName(kind), number, action.ActionId, action.FirmName);
			return new ExportFile { Name = SafeFileName(name) + ".docx", Content = DocxTemplate.Render(template, data) };
		}

		/// <summary>Договор из карточки фирмы, без акции (десктоп: <c>Firm.PrintContract</c>): только дата.</summary>
		public static ExportFile FirmContractFile(Firm firm, Agency agency, DocumentKind kind, DateTime date, bool withSignature)
		{
			byte[] template = StartingTemplates.ForPrint(agency.AgencyId, kind, date);
			DocumentData data = Contract(null, firm, agency, date, string.Empty, withSignature);
			string name = Tr.Format("{0} от {1:dd.MM.yyyy} для {2}", KindName(kind), date, firm.Name);
			return new ExportFile { Name = SafeFileName(name) + ".docx", Content = DocxTemplate.Render(template, data) };
		}

		/// <summary>Месяцы акции для счёта по месяцам — по фактическим окнам (<c>Action.SelectMonthsToShow</c>).</summary>
		public static IList<DateTime> BillMonths(Action action)
		{
			var parameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
			{
				[Action.ParamNames.ActionId] = action.ActionId,
				["isFact"] = true
			};
			return Months(DataAccessor.LoadDataSet("GetMonthes", parameters).Tables[0]);
		}

		/// <summary>Месяцы кампании для эфирной справки (<c>sl_Months</c>, как в десктопе).</summary>
		public static IList<DateTime> OnAirMonths(PresentationObject campaign)
		{
			var parameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
			{
				[Campaign.ParamNames.CampaignId] = ((Campaign)campaign).CampaignId
			};
			return Months(DataAccessor.LoadDataSet("sl_Months", parameters).Tables[0]);
		}

		private static IList<DateTime> Months(DataTable table)
		{
			var months = new List<DateTime>();
			foreach (DataRow row in table.Rows)
			{
				int month = ParseHelper.ParseToInt32(row["MonthDate"].ToString(), -1);
				int year = ParseHelper.ParseToInt32(row["MonthYear"].ToString(), -1);
				if (month > 0 && year > 0)
					months.Add(new DateTime(year, month, 1));
			}
			return months;
		}

		/// <summary>
		/// Кампания, по которой печатается эфирная справка: сама кампания или кампания строки
		/// журнала актов (<c>ActJournalRow.GetCampaign</c>). null — у объекта справки нет.
		/// </summary>
		public static PresentationObject OnAirCampaign(PresentationObject target)
		{
			// Строки кампаний в дереве акции — базовый Campaign (сущность 78): нужен объект своего вида.
			if (target is CampaignOnSingleMassmedia || target is CampaignPackModule)
				return target;
			if (target is Campaign)
				return Campaign.GetCampaignById(((Campaign)target).CampaignId);
			if (target is ActJournalRow)
				return Campaign.GetCampaignById(int.Parse(target[Campaign.ParamNames.CampaignId].ToString()));
			return null;
		}

		/// <summary>
		/// Радиостанции справки: у кампании на одной станции — она; у пакетного модуля — станция
		/// строки (<c>packmodulemassmediaID</c>) или все станции пакета (<c>CampaignPackModule.PrintOnAirInquire</c>).
		/// </summary>
		public static IList<Massmedia> OnAirMassmedias(PresentationObject campaign)
		{
			var result = new List<Massmedia>();
			var single = campaign as CampaignOnSingleMassmedia;
			if (single != null)
			{
				result.Add(single.Massmedia);
				return result;
			}
			var pack = campaign as CampaignPackModule;
			if (pack == null)
				return result;

			object rowMassmedia;
			if (pack.Parameters.TryGetValue("packmodulemassmediaID", out rowMassmedia) && rowMassmedia != null
				&& ParseHelper.ParseToInt32(rowMassmedia.ToString(), -1) > 0)
			{
				result.Add(Massmedia.GetMassmediaByID(ParseHelper.ParseToInt32(rowMassmedia.ToString(), -1)));
				return result;
			}
			DataSet massmedias = pack.Massmedias;
			if (massmedias != null)
				foreach (DataRow row in massmedias.Tables[Constants.TableNames.Data].Rows)
				{
					int id = ParseHelper.ParseToInt32(row["packmodulemassmediaID"].ToString(), -1);
					if (id > 0)
						result.Add(Massmedia.GetMassmediaByID(id));
				}
			return result;
		}

		/// <summary>Агентство кампании — по нему выбирается шаблон эфирной справки.</summary>
		public static Agency CampaignAgency(PresentationObject campaign)
		{
			return ((Campaign)campaign).Agency;
		}

		/// <summary>Эфирная справка по кампании на станции за месяц.</summary>
		public static ExportFile OnAirInquireFile(PresentationObject campaignObject, Massmedia massmedia, DateTime month,
			bool withPrice, bool withSignature)
		{
			var campaign = (Campaign)campaignObject;
			Agency agency = campaign.Agency;
			var monthStart = new DateTime(month.Year, month.Month, 1);
			byte[] template = StartingTemplates.ForPrint(agency.AgencyId, DocumentKind.OnAirInquire, monthStart);
			DocumentData data = OnAirInquire(campaign, agency, massmedia, monthStart, withPrice, withSignature);
			string name = Tr.Format("{0} {1} {2} к акции {3} для {4}", KindName(DocumentKind.OnAirInquire), massmedia.Name,
				MonthText(monthStart), campaign.Action.ActionId, campaign.Action.FirmName);
			return new ExportFile { Name = SafeFileName(name) + ".docx", Content = DocxTemplate.Render(template, data) };
		}

		private static string SafeFileName(string name)
		{
			foreach (char c in Path.GetInvalidFileNameChars())
				name = name.Replace(c, '_');
			return name;
		}
	}
}
