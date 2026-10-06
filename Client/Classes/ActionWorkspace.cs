using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Страница акции в вебе (docs/tasks/web-action-forms.md, этап 1) — вход в ядро для того, что
	/// в десктопе делает карточка акции ActionForm, кроме размещения: права, список кампаний,
	/// добавить и удалить кампании, скидка менеджера, цена акции. Классы Campaign и CampaignPart
	/// internal — снаружи сборки с ними работают через этот фасад. Запись — методами доменных
	/// классов, которые зовёт и десктоп (ActionOnMassmedia.AddCampaigns, ApplyFinalPrice,
	/// Campaign.ApplyManagerDiscount): деньги считаются одним кодом в обоих клиентах.
	///
	/// Права проверяет фасад перед каждой записью, а не кнопка экрана (план, §3.9): страница
	/// открывается и по прямой ссылке.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class ActionWorkspace
	{
		// ---------- Акция ----------

		/// <summary>Акция по номеру; null — такой нет.</summary>
		public static ActionOnMassmedia Load(int actionId)
		{
			ActionOnMassmedia action = new ActionOnMassmedia(actionId);
			return action.Refresh() ? action : null;
		}

		/// <summary>
		/// Видна ли акция пользователю — то же условие, что у журналов акций (Actions1):
		/// своя, право видеть чужие или право видеть акции своей группы и создатель в ней.
		/// Загрузка по номеру (Actions1 @actionID) этого условия не проверяет.
		/// </summary>
		public static bool CanView(ActionOnMassmedia action)
		{
			SecurityManager.User user = SecurityManager.LoggedUser;
			if (user.Id == action.UserID || user.IsAdmin || user.IsRightToViewForeignActions())
				return true;
			return user.IsRightToViewGroupActions() && action.User != null && user.IsInGroup(action.User.Groups);
		}

		/// <summary>Акция в журнале удалённых.</summary>
		public static bool IsDeleted(ActionOnMassmedia action) => action.DeleteDate != null;

		/// <summary>
		/// Можно ли менять акцию — право «Редактировать» акции, как у пункта журнала:
		/// права группы плюс правило чужих и групповых акций (CheckLoggedUserRight).
		/// Без него страница открывается на просмотр (решение В-7, 05.10.2026).
		/// </summary>
		public static bool CanEdit(ActionOnMassmedia action) =>
			!IsDeleted(action) && action.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal);

		/// <summary>
		/// Объект для меню «⋯» шапки: у удалённой акции — своя сущность (восстановить, удалить
		/// окончательно), как в журнале удалённых.
		/// </summary>
		public static PresentationObject MenuTarget(ActionOnMassmedia action) =>
			IsDeleted(action)
				? EntityManager.GetEntity((int)Entities.ActionDeleted).CreateObject(action.Parameters)
				: action;

		// ---------- Кампании ----------

		/// <summary>Список кампаний — сущность 78 с коротким набором колонок, как в ActionForm.</summary>
		public static Entity CampaignListEntity()
		{
			Entity entity = (Entity)EntityManager.GetEntity((int)Entities.CampaignOnMassmedia).Clone();
			entity.AttributeSelector = Campaign.ShortAttributesList;
			return entity;
		}

		/// <summary>Колонки строки кампании, которые читает экран (сам Campaign — internal).</summary>
		public static class CampaignColumns
		{
			public const string TariffPrice = Campaign.ParamNames.TariffPrice;
			public const string MassmediaId = Campaign.ParamNames.MassmediaId;
			public const string CampaignTypeId = Campaign.ParamNames.CampaignTypeId;
		}

		/// <summary>
		/// «Редактировать» у кампании в вебе есть только у линейной — её размещение на вкладке
		/// «Рекламные окна»; у остальных видов вкладок пока нет (пункт серый).
		/// </summary>
		public static bool IsLinearCampaign(PresentationObject campaign) =>
			campaign is Campaign c && c.CampaignType == Campaign.CampaignTypes.Simple;

		/// <summary>Акция кампании — для перехода на страницу акции; null — кампания без акции.</summary>
		public static int? ActionIdOf(PresentationObject campaign) => ((Campaign)campaign).ActionId;

		/// <summary>Кампании акции (Campaigns @actionID), мимо кэша — после каждой операции свежие.</summary>
		public static DataTable LoadCampaigns(ActionOnMassmedia action) => action.Campaigns(forceLoad: true);

		/// <summary>Виды мест, которые есть в акции: по ним показываются вкладки размещения.</summary>
		[Flags]
		public enum PlaceKinds
		{
			None = 0,
			/// <summary>Линейные кампании — вкладка «Окна».</summary>
			Windows = 1,
			/// <summary>Модульные — «Модули».</summary>
			Modules = 2,
			/// <summary>Спонсорские — «Спонсорство».</summary>
			Sponsorship = 4,
			/// <summary>Пакетная — «Пакеты».</summary>
			Packs = 8,
		}

		public static PlaceKinds KindsOf(DataTable campaigns)
		{
			PlaceKinds kinds = PlaceKinds.None;
			foreach (DataRow row in campaigns.Rows)
				kinds |= KindOf(CampaignTypeOf(row));
			return kinds;
		}

		private static PlaceKinds KindOf(Campaign.CampaignTypes type)
		{
			switch (type)
			{
				case Campaign.CampaignTypes.Simple: return PlaceKinds.Windows;
				case Campaign.CampaignTypes.Module: return PlaceKinds.Modules;
				case Campaign.CampaignTypes.Sponsor: return PlaceKinds.Sponsorship;
				case Campaign.CampaignTypes.PackModule: return PlaceKinds.Packs;
				default: return PlaceKinds.None;
			}
		}

		private static Campaign.CampaignTypes CampaignTypeOf(DataRow row) =>
			(Campaign.CampaignTypes)ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignTypeId], 0);

		/// <summary>
		/// Кампания без выпусков: ни роликов (у модульной и пакетной — тоже строки Issue), ни
		/// выходов программ. Счётчики ведёт ActionRecalculate.
		/// </summary>
		public static bool IsEmpty(DataRow campaign) =>
			ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.IssuesCount], 0)
			+ ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.ProgramIssuesCount], 0) == 0;

		/// <summary>Подпись кампании для сообщений: станция с группой, у пакетной — тип.</summary>
		public static string CampaignName(DataRow campaign) =>
			ParseHelper.GetStringFromObject(campaign[Constants.Parameters.Name], string.Empty);

		// ---------- Добавить кампании ----------

		/// <summary>
		/// Можно ли добавлять кампании: правка акции плюс «Редактировать» у линейной кампании —
		/// так решает десктоп для всех типов («если есть права редактировать обычную кампанию,
		/// то есть и права создавать», ActionForm.AddCampaign).
		/// </summary>
		public static bool CanAddCampaigns(ActionOnMassmedia action) =>
			CanEdit(action)
			&& EntityManager.GetEntity((int)Entities.GeneralCampaign).IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal);

		/// <summary>Списки диалога «Добавить кампании» — процедура карточки новой кампании.</summary>
		public sealed class NewCampaignChoices
		{
			/// <summary>Виды кампаний: id, name.</summary>
			public DataTable CampaignTypes { get; internal set; }
			/// <summary>Действующие типы оплаты: id, name.</summary>
			public DataTable PaymentTypes { get; internal set; }
			/// <summary>Станции, куда пользователь может ставить (massmediaList @checkCanAdd = 1).</summary>
			public DataTable Stations { get; internal set; }
			/// <summary>Группы станций: id (0 — все), name.</summary>
			public DataTable StationGroups { get; internal set; }
		}

		/// <summary>NewCampaignPassport — то же, что грузит CampaignPassportFormBaseController.</summary>
		public static NewCampaignChoices LoadNewCampaignChoices()
		{
			Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(
				EntityManager.GetEntity((int)Entities.CampaignOnMassmedia), InterfaceObjects.PropertyPage, Constants.Actions.Load);
			DataSet data = (DataSet)DataAccessor.DoAction(procParameters);
			return new NewCampaignChoices
			{
				CampaignTypes = data.Tables["campaign_type"],
				PaymentTypes = data.Tables["payment_type"],
				Stations = data.Tables["massmedia"],
				StationGroups = data.Tables["massmedia_group"],
			};
		}

		public const int PackModuleCampaignType = (int)Campaign.CampaignTypes.PackModule;

		/// <summary>Действующие агентства станции (MassmediaAgencies): agencyID, name.</summary>
		public static DataTable StationAgencies(DataRow station)
		{
			Massmedia massmedia = (Massmedia)EntityManager.GetEntity((int)Entities.MassMedia).CreateObject(station);
			return massmedia.Agencies;
		}

		/// <summary>Агентства пользователя — для пакетной кампании, у неё нет станции (UserAgencies).</summary>
		public static DataTable UserAgencies() => SecurityManager.LoggedUser.Agencies;

		/// <summary>Кампания к добавлению: станция (null у пакетной) и её агентство.</summary>
		public sealed class NewCampaign
		{
			public int? MassmediaId { get; set; }
			public string StationName { get; set; }
			public int AgencyId { get; set; }
		}

		/// <summary>
		/// Добавляет кампании одного вида и типа оплаты (по станции на кампанию, у пакетной —
		/// одна без станции). Отказ по одной станции не мешает остальным (Д-4).
		/// </summary>
		/// <returns>Не добавленные — «станция - причина»; пустой список — добавлены все.</returns>
		public static IReadOnlyList<string> AddCampaigns(ActionOnMassmedia action, int campaignTypeId, int paymentTypeId,
			IEnumerable<NewCampaign> campaigns)
		{
			if (!CanAddCampaigns(action))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));

			List<Campaign> drafts = new List<Campaign>();
			foreach (NewCampaign item in campaigns)
			{
				Campaign campaign = Campaign.CreateInstance(campaignTypeId, paymentTypeId,
					campaignTypeId == PackModuleCampaignType ? null : item.MassmediaId, item.AgencyId);
				if (!string.IsNullOrEmpty(item.StationName))
					campaign[Campaign.ParamNames.MassmediaName] = item.StationName;
				drafts.Add(campaign);
			}

			return action.AddCampaigns(drafts, out _);
		}

		// ---------- Удалить кампании ----------

		/// <summary>
		/// Удаляет кампании по одной (CampaignIUD, каждая своей транзакцией, как в десктопе),
		/// затем один пересчёт акции. Отказы — таблицей ошибок (описание), остальные удаляются.
		/// </summary>
		/// <param name="priceMessage">Сообщение о цене акции после пересчёта (как в десктопе после Del).</param>
		/// <returns>Таблица ошибок; пустая — удалены все.</returns>
		public static DataTable DeleteCampaigns(ActionOnMassmedia action, IReadOnlyList<PresentationObject> campaigns, out string priceMessage)
		{
			if (!CanEdit(action))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));

			action.Refresh();
			decimal oldPrice = action.TotalPrice;
			DataTable errors = ErrorManager.CreateErrorsTable();
			bool anyDeleted = false;

			foreach (PresentationObject campaign in campaigns)
			{
				string name = string.IsNullOrEmpty(campaign.Name) ? Convert.ToString(campaign[Campaign.ParamNames.MassmediaName]) : campaign.Name;
				if (!campaign.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal))
				{
					ErrorManager.AddErrorRow(errors, DateTime.Now, name + ": " + Tr.T(Properties.Resources.OperationNotAllowed));
					continue;
				}

				try
				{
					if (campaign.Delete(silenceFlag: true))
						anyDeleted = true;
				}
				catch (Exception ex)
				{
					ErrorManager.AddErrorRow(errors, DateTime.Now, name + ": " + ErrorManager.GetErrorMessage(ex));
				}
			}

			priceMessage = null;
			if (anyDeleted)
			{
				action.Recalculate(refreshFlag: true);
				priceMessage = CampaignPart.PriceChangeText(oldPrice, action.TotalPrice);
			}
			return errors;
		}

		/// <summary>
		/// Удаление одной кампании из меню строки (журнал акций, страница акции) — то, что в
		/// десктопе делает CampaignPart.DoAction(Delete): удалить, пересчитать акцию, сказать о цене.
		/// </summary>
		/// <returns>Сообщение о цене акции; null — кампания не удалена.</returns>
		public static string DeleteCampaign(PresentationObject campaign)
		{
			ActionOnMassmedia action = ((Campaign)campaign).Action;
			action.Refresh();
			decimal oldPrice = action.TotalPrice;
			if (!campaign.Delete(silenceFlag: true))
				return null;

			action.Recalculate(refreshFlag: true);
			return CampaignPart.PriceChangeText(oldPrice, action.TotalPrice);
		}

		// ---------- Скидка менеджера и цена акции ----------

		/// <summary>Нужна ли причина скидки: её спрашивают у всех, кроме создателя акции.</summary>
		public static bool NeedsDiscountReason(ActionOnMassmedia action) => SecurityManager.LoggedUser.Id != action.UserID;

		/// <summary>Дату скидки выбирают бухгалтер и администратор, остальным — сегодня.</summary>
		public static bool CanChooseDiscountDate =>
			SecurityManager.LoggedUser.IsBookKeeper || SecurityManager.LoggedUser.IsAdmin;

		/// <summary>Причины скидки (справочник ManagerDiscountReason): ManagerDiscountReasonId, name.</summary>
		public static DataTable DiscountReasons()
		{
			Entity entity = EntityManager.GetEntity((int)Entities.ManagerDiscountReason);
			entity.ClearCache();
			return entity.GetContent();
		}

		/// <summary>Что показывает окно «Менеджерская скидка» по кампании.</summary>
		public sealed class ManagerDiscountInfo
		{
			public string CampaignName { get; internal set; }
			public decimal TariffPrice { get; internal set; }
			/// <summary>Объёмная скидка.</summary>
			public decimal Discount { get; internal set; }
			public decimal PackDiscount { get; internal set; }
			/// <summary>Нынешняя цена со всеми скидками.</summary>
			public decimal FullPrice { get; internal set; }
			public decimal ManagerDiscount { get; internal set; }

			/// <summary>Итоговая цена при коэффициенте — та же формула, что в десктопе.</summary>
			public decimal PriceForRatio(decimal ratio) => ratio * Discount * PackDiscount * TariffPrice;

			/// <summary>Коэффициент при итоговой цене; 0 — если цена без менеджерской скидки нулевая.</summary>
			public decimal RatioForPrice(decimal price)
			{
				decimal basePrice = TariffPrice * Discount * PackDiscount;
				return basePrice == 0 ? 0 : price / basePrice;
			}
		}

		/// <summary>Кампания свежая из базы — цены могли смениться с момента загрузки списка.</summary>
		public static ManagerDiscountInfo LoadManagerDiscount(PresentationObject campaign)
		{
			Campaign c = (Campaign)campaign;
			c.Refresh();
			return new ManagerDiscountInfo
			{
				CampaignName = c.Name,
				TariffPrice = c.TariffPrice,
				Discount = c.Discount,
				PackDiscount = c.PackDiscount,
				FullPrice = c.FullPrice,
				ManagerDiscount = Math.Min(Math.Max(c.ManagerDiscount, 0), 1000),
			};
		}

		/// <summary>
		/// Скидка менеджера кампании: итоговая цена и пересчёт акции одной транзакцией. Лимит
		/// коэффициента проверяет процедура (MaxRatioExcess) — «разрешающего» в вебе нет.
		/// </summary>
		/// <param name="date">null — сегодня (дату выбирают только бухгалтер и администратор).</param>
		public static void ApplyManagerDiscount(ActionOnMassmedia action, PresentationObject campaign, decimal finalPrice,
			DateTime? date, int? managerDiscountReasonId)
		{
			if (!CanEdit(action))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));

			DateTime todayDate = CanChooseDiscountDate && date.HasValue ? date.Value : DateTime.Today;
			((Campaign)campaign).ApplyManagerDiscount(finalPrice, todayDate, null, managerDiscountReasonId);
		}

		/// <summary>Что показывает окно «Цена рекламной акции».</summary>
		public sealed class ActionPriceInfo
		{
			public decimal TariffPrice { get; internal set; }
			public decimal TotalPrice { get; internal set; }
			/// <summary>Усреднённая менеджерская скидка акции.</summary>
			public decimal AverageRatio { get; internal set; }
			internal DataTable Campaigns { get; set; }

			/// <summary>Цена акции при одном коэффициенте у всех кампаний.</summary>
			public decimal PriceForRatio(decimal ratio) => ActionOnMassmedia.PriceWithManagerRatio(Campaigns, ratio);
		}

		public static ActionPriceInfo LoadActionPrice(ActionOnMassmedia action)
		{
			action.Refresh();
			DataTable campaigns = LoadCampaigns(action);
			return new ActionPriceInfo
			{
				TariffPrice = action.TariffPrice,
				TotalPrice = action.TotalPrice,
				AverageRatio = action.AverageManagerRatio(campaigns),
				Campaigns = campaigns,
			};
		}

		/// <summary>
		/// Цена акции: распределение по кампаниям и пересчёт одной транзакцией
		/// (ActionOnMassmedia.ApplyFinalPrice — тот же код, что у десктопа).
		/// </summary>
		public static void ApplyActionPrice(ActionOnMassmedia action, decimal finalPrice, bool byRatio, decimal ratio,
			DateTime? date, int? managerDiscountReasonId)
		{
			if (!CanEdit(action))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));

			DateTime todayDate = CanChooseDiscountDate && date.HasValue ? date.Value : DateTime.Today;
			action.ApplyFinalPrice(finalPrice, byRatio, ratio, todayDate, managerDiscountReasonId);
		}
	}
}
