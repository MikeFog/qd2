using System.Data;
using Merlin.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Карточка агентства — правило AgencyPassportForm поверх паспорта сущности 8: «Расчетный счет» — счёт
/// агентства в выбранном банке. При смене банка в поле встаёт сохранённый счёт этого банка (набор
/// agencyBank процедуры agencyPassport), введённое для прежнего банка теряется (решение Миши 03.11.2026).
/// Пустой счёт при выбранном банке отклоняет AgencyIUD (AgencyBankAccountRequired).
/// </summary>
public static class AgencyPassport
{
	private const string AgencyBankTable = "agencyBank";

	public static PassportRules Rules(Agency agency, DataSet? data) => new()
	{
		FieldChanged = name =>
		{
			if (name == Organization.ParamNames.BankId)
				agency[Organization.ParamNames.Account] = AccountInBank(data, agency[Organization.ParamNames.BankId]);
		},
	};

	private static string AccountInBank(DataSet? data, object? bankId)
	{
		string id = Convert.ToString(bankId) ?? "";
		if (id.Length == 0 || data == null || !data.Tables.Contains(AgencyBankTable))
			return "";

		foreach (DataRow row in data.Tables[AgencyBankTable]!.Rows)
			if (Convert.ToString(row[Organization.ParamNames.BankId]) == id)
				return Convert.ToString(row[Organization.ParamNames.Account]) ?? "";
		return "";
	}
}
