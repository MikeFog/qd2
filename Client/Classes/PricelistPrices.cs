using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// «Сменить цену» прайс-листа — без UI, общий для десктопа (ChangeTariffPricesForm) и веба:
	/// все разные цены тарифов прайс-листа и их замена во всех тарифах сразу. Тарифы со
	/// сгенерированными окнами процедура не меняет — они возвращаются журналом.
	/// Публичный фасад: MassmediaPricelist internal.
	/// </summary>
	public static class PricelistPrices
	{
		/// <summary>Имя действия прайс-листа (iEntityAction, сущность 80).</summary>
		public const string ActionName = "ChangeTariffPrices";

		/// <summary>Колонки результата Load.</summary>
		public static class Columns
		{
			public const string Price = "price";
			public const string TariffsCount = "tariffsCount";
			public const string WithWindowsCount = "withWindowsCount";
		}

		/// <summary>Разные цены тарифов прайс-листа по возрастанию: цена, тарифов, из них с окнами.</summary>
		public static DataTable Load(object pricelist)
		{
			Dictionary<string, object> procParameters = new Dictionary<string, object>
				{ { Pricelist.ParamNames.PricelistId, ((Pricelist)pricelist).PricelistId } };
			return DataAccessor.LoadDataSet("PricelistTariffPrices", procParameters).Tables[0];
		}

		/// <summary>Новая цена допустима (Tariff.price — decimal(18,2)); текст ошибки или null.</summary>
		public static string ValidateNewPrice(decimal newPrice)
		{
			if (newPrice < 0)
				return Tr.T("Цена не может быть отрицательной.");
			if (decimal.Round(newPrice, 2) != newPrice)
				return Tr.T("В цене не больше двух знаков после запятой.");
			if (newPrice >= MaxPrice)
				return Tr.T("Слишком большая цена.");
			return null;
		}

		private const decimal MaxPrice = 10000000000000000m; // 10^16: decimal(18,2) вмещает 16 цифр до запятой

		/// <summary>
		/// Заменяет цены (старая → новая) во всех тарифах прайс-листа одним UPDATE: замены
		/// одновременные, не цепочкой. Пары без изменения цены пропускаются. Возвращает число
		/// изменённых тарифов; тарифы с окнами, оставшиеся по старой цене, — в tableErrors.
		/// </summary>
		public static int Apply(object pricelist, IDictionary<decimal, decimal> newPrices, out DataTable tableErrors)
		{
			tableErrors = ErrorManager.CreateErrorsTable();

			string prices = string.Join(",", newPrices
				.Where(p => p.Key != p.Value)
				.Select(p => p.Key.ToString(CultureInfo.InvariantCulture) + ":" + p.Value.ToString(CultureInfo.InvariantCulture)));
			if (prices.Length == 0)
				return 0;

			Dictionary<string, object> procParameters = new Dictionary<string, object>
				{
					{ Pricelist.ParamNames.PricelistId, ((Pricelist)pricelist).PricelistId },
					{ "prices", prices }
				};
			DataSet ds = DataAccessor.LoadDataSet("PricelistTariffPricesChange", procParameters);

			foreach (DataRow row in ds.Tables[1].Rows)
				ErrorManager.AddErrorRow(tableErrors, DateTime.Now,
					Tr.Format("{0:HH:mm}, цена {1:N2}: у тарифа есть сгенерированные окна - цена не изменена",
						Convert.ToDateTime(row["time"]), Convert.ToDecimal(row["price"])));

			return Convert.ToInt32(ds.Tables[0].Rows[0]["changedCount"]);
		}
	}
}
