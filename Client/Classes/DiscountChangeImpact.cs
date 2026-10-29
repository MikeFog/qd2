using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// «Изменение затронет акции»: перед записью или удалением правки скидки (объёмной — сущности 22, 23; пакетной —
	/// 189, 190, 191) — акции, в которых эта скидка уже посчитана (Campaign.discountReleaseID,
	/// Action.packageDiscountPriceListID) и которым правка может её поменять при следующем пересчёте
	/// (процедура DiscountChangeAffectedActions). Решает человек: «Сохранить» — правка пишется, акции не пересчитываются.
	/// Общее для десктопа (DiscountAffectedActionsForm) и веба; спрашивает каждый UI сам (<see cref="SaveWarning"/>).
	/// </summary>
	internal static class DiscountChangeImpact
	{
		private const string ProcedureName = "DiscountChangeAffectedActions";

		/// <summary>Перед записью карточки (AddItem/UpdateItem); null — правка ничего не задевает.</summary>
		public static SaveWarning ForSave(PresentationObject po)
		{
			// Копия исходную скидку не меняет
			if (po.Parameters.TryGetValue(Constants.ParamNames.ActionName, out object action)
				&& action as string == Constants.Actions.Clone)
				return null;

			return Build(po, po.IsNew ? Constants.Actions.AddItem : Constants.Actions.Update);
		}

		/// <summary>Перед удалением строки, которая входит в содержимое скидки (порог, станция пакета).</summary>
		public static SaveWarning ForDelete(PresentationObject po)
		{
			return Build(po, Constants.Actions.Delete);
		}

		private static SaveWarning Build(PresentationObject po, string actionName)
		{
			Dictionary<string, object> parameters = po.Parameters;
			parameters["entityID"] = po.Entity.Id;
			parameters[Constants.ParamNames.ActionName] = actionName;

			DataTable affected = DataAccessor.LoadDataSet(ProcedureName, parameters).Tables[0];
			if (affected.Rows.Count == 0)
				return null;

			bool byCampaign = po.Entity.Id == (int)Entities.DiscountRelease || po.Entity.Id == (int)Entities.DiscountValue;

			// Подписи колонок — по-русски: их переводит показ таблицы (SmartGrid, веб-ObjectList).
			var columns = new List<Entity.Attribute>
			{
				new Entity.Attribute("actionID", "Акция", "int"),
				new Entity.Attribute("firm", "Фирма", "nvarchar"),
				new Entity.Attribute("manager", "Менеджер", "nvarchar"),
				new Entity.Attribute("startDate", "Начало акции", "datetime"),
				new Entity.Attribute("finishDate", "Окончание акции", "datetime"),
				new Entity.Attribute("status", "Статус", "nvarchar"),
			};
			if (byCampaign)
				columns.Add(new Entity.Attribute("massmedia", "Радиостанция", "nvarchar"));
			columns.Add(new Entity.Attribute("discount", byCampaign ? "Скидка кампании" : "Пакетная скидка", "float"));

			var paragraphs = new List<string>
			{
				byCampaign
					? Tr.T("Скидка уже используется в рекламных кампаниях. Внесённые изменения могут повлиять на сумму этих кампаний и акций.")
					: Tr.T("Скидка уже используется в акциях. Внесённые изменения могут повлиять на сумму этих акций."),
				Tr.T("Сами акции сейчас не пересчитываются: скидка у них может измениться при следующем пересчёте (любая правка выпусков)."),
				Tr.T("Сохранить изменения?"),
			};

			return new SaveWarning(Tr.T("Изменение затронет акции"), paragraphs, affected, columns.ToArray(), Tr.T("Сохранить"));
		}
	}
}
