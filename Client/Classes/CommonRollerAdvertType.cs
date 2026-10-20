using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// «Изменить предмет рекламы» у выпуска пакетного модуля (175) и ролика модульной кампании в дне (215) —
	/// веб-аналог RollerChangeAdvertTypeForm и ApplyAdvertTypeChange: свой ролик прайс-листа модуля или пакета
	/// («для всех фирм») на отмеченные дни заменяется клоном с выбранным предметом рекламы
	/// (SetAdvertTypeForCommmonRoller). Паспорт ChangeAdvertType, данные — те же, что у замены ролика
	/// (RollerSubstitutionPassport). Пересчёта нет: длина ролика та же.
	///
	/// Отличия от десктопа: право группы на действие проверяется (десктопный IsActionEnabled этих классов отвечает
	/// мимо базового); все дни — одной транзакцией; у пакета прайс-лист — по дню выпуска, а не от выпуска, на котором
	/// открыто окно (в десктопе дни второго прайс-листа того же пакета молча не менялись).
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public sealed class CommonRollerAdvertType
	{
		public const string ActionName = Roller.ActionNames.ChangeAdvertType;
		public const string PassportName = "ChangeAdvertType";
		public const string AdvertTypeField = "advertTypeID";
		public const string DaysField = "days";

		private readonly Campaign campaign;
		private readonly Roller roller;
		private readonly int? moduleId;
		private readonly int? packModuleId;

		private CommonRollerAdvertType(Campaign campaign, Roller roller, int? moduleId, int? packModuleId)
		{
			this.campaign = campaign;
			this.roller = roller;
			this.moduleId = moduleId;
			this.packModuleId = packModuleId;
		}

		/// <summary>
		/// Пункт, у которого десктоп не спрашивает право группы: IsActionEnabled выпуска пакета и ролика модуля
		/// для замены ролика и смены предмета рекламы отвечает по прайс-листу, не зовя базовый.
		/// </summary>
		public static bool SkipsGroupRight(PresentationObject po, string actionName) =>
			(po is PackModuleIssue || po is CampaignModuleRollerInsideDay)
			&& (string.Equals(actionName, ActionName, StringComparison.OrdinalIgnoreCase) || actionName == Constants.Actions.Substitute);

		public static CommonRollerAdvertType For(PresentationObject issue)
		{
			Ensure(issue);
			if (issue is PackModuleIssue pack)
				return new CommonRollerAdvertType(pack.Campaign, pack.Roller, null, pack.PackModuleID);
			CampaignModuleRollerInsideDay module = (CampaignModuleRollerInsideDay)issue;
			return new CommonRollerAdvertType(module.Campaign, module.Roller, module.ModuleID, null);
		}

		/// <summary>
		/// Пункт меню доступен: свой ролик у прайс-листа (IsActionEnabled класса), акция не удалена (IsActionHidden) и
		/// право группы на действие.
		/// </summary>
		private static void Ensure(PresentationObject issue)
		{
			if (!(issue is PackModuleIssue || issue is CampaignModuleRollerInsideDay)
				|| !issue.IsActionEnabled(ActionName, ViewType.Journal) || issue.IsActionHidden(ActionName, ViewType.Journal)
				|| !issue.Entity.IsActionEnabled(ActionName, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
		}

		/// <summary>Наборы строк паспорта: quantity, rollers, days — RollerChangeAdvertTypeForm.LoadData.</summary>
		public DataSet LoadPassportData() => new RollerSubstitution(campaign, roller, moduleId, packModuleId).LoadPassportData();

		/// <summary>Значения подписей паспорта.</summary>
		public Dictionary<string, object> CreatePassportParameters(DataSet ds)
		{
			Dictionary<string, object> parameters = new RollerSubstitution(campaign, roller, moduleId, packModuleId).CreatePassportParameters(ds);
			parameters[Roller.ParamNames.AdvertTypeName] = roller.AdvertTypeName;
			return parameters;
		}

		/// <summary>
		/// Проверки по «ОК» в порядке десктопа; текст сообщения или null. Дни — из строк дерева (колонка issueDate),
		/// а не разбором id «dd.MM.yyyy»: культура веба может быть не русской.
		/// </summary>
		public static string Validate(object advertTypeId, DataTable days, ICollection<object> checkedIds, out List<DateTime> selectedDays)
		{
			selectedDays = new List<DateTime>();
			if (advertTypeId == null || advertTypeId == DBNull.Value)
				return Tr.T(Properties.Resources.AdvertTypeNotSelected);
			if (days != null)
				foreach (DataRow row in RollerSubstitution.SelectDays(days, checkedIds).Rows)
					selectedDays.Add(Convert.ToDateTime(row[Issue.ParamNames.IssueDate]).Date);
			return selectedDays.Count == 0 ? Tr.T(Properties.Resources.NoIssueSelected) : null;
		}

		/// <summary>Запись на все дни одной транзакцией; права и акция — заново перед записью.</summary>
		public void Apply(IReadOnlyCollection<DateTime> days, int advertTypeId)
		{
			ActionOnMassmedia action = ActionWorkspace.Load(campaign.Action.ActionId);
			if (action == null || ActionWorkspace.IsDeleted(action))
				throw new InvalidOperationException(Tr.T("Рекламная акция удалена. Обновите страницу."));
			if (!EntityManager.GetEntity(packModuleId.HasValue ? (int)Entities.PackModuleIssue : (int)Entities.CampaignModuleRollerInsideDay)
					.IsActionEnabled(ActionName, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));

			WindowsSource.RunInTransaction(() =>
			{
				Dictionary<DateTime, object> pricelists = packModuleId.HasValue ? PackPricelistsByDay() : null;
				foreach (DateTime day in days)
				{
					Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
					procParameters[Roller.ParamNames.RollerId] = roller.RollerId;
					procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
					procParameters[AdvertType.ParamNames.AdvertTypeId] = advertTypeId;
					procParameters[Issue.ParamNames.IssueDate] = day;
					if (pricelists == null)
						procParameters[Module.ParamNames.ModuleId] = moduleId;
					else if (pricelists.TryGetValue(day, out object pricelistId))
						procParameters[Pricelist.ParamNames.PricelistId] = pricelistId;
					else
						continue; // выпуска пакета с этим роликом в этот день уже нет — процедура ничего бы не сделала

					DataAccessor.ExecuteNonQuery("SetAdvertTypeForCommmonRoller", procParameters);
				}
			});
		}

		/// <summary>Прайс-лист пакета по дню — у выпусков этого ролика в кампании.</summary>
		private Dictionary<DateTime, object> PackPricelistsByDay()
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters["campaignId"] = campaign.CampaignId;
			procParameters["packModuleId"] = packModuleId;
			DataSet ds = DataAccessor.LoadDataSet("PackModuleIssueRetrieve", procParameters);

			Dictionary<DateTime, object> result = new Dictionary<DateTime, object>();
			foreach (DataRow row in ds.Tables[0].Rows)
				if (Convert.ToInt32(row[Roller.ParamNames.RollerId]) == roller.RollerId)
					result[Convert.ToDateTime(row[Issue.ParamNames.IssueDate]).Date] = row[Pricelist.ParamNames.PricelistId];
			return result;
		}
	}
}
