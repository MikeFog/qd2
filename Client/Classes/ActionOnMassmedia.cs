using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;

namespace Merlin.Classes
{
    // Часть, работающая с диалогами, постепенно переезжает в ActionOnMassmedia.WinForms.cs.
	// Конвенция разреза — docs/tasks/web-migration-dialogs.md.
	public partial class ActionOnMassmedia : Action
	{
		internal class SplitRule
		{
			public enum SplitType
			{
				ByPeriod = 1,
				ByRollers = 2
			}

			public SplitRule(CampaignOnSingleMassmedia campaign)
			{
				this.campaign = campaign;
			}

			public readonly CampaignOnSingleMassmedia campaign;
			public SplitType splitType;
			public DateTime ?date;
			public List<PresentationObject> rollers;
		}

		public ActionOnMassmedia()
			: base(EntityManager.GetEntity((int) Entities.Action))
		{
			SetChildEntity();
		}

		public ActionOnMassmedia(int actionID)
			: base(EntityManager.GetEntity((int) Entities.Action), actionID)
		{
			SetChildEntity();
		}

		public ActionOnMassmedia(DataRow row)
			: base(EntityManager.GetEntity((int) Entities.Action), row)
		{
			SetChildEntity();
		}

		protected ActionOnMassmedia(Entity entity) : base(entity)
		{
			SetChildEntity();
        }

        public ActionOnMassmedia(PresentationObject firm)
			:
				base(EntityManager.GetEntity((int) Entities.Action), firm)
		{
			SetChildEntity();
			this[ParamNames.TotalPrice] = (decimal)0;
			this[ParamNames.IsConfirmed] = false;
            this[ParamNames.Ratio] = (decimal)1;
        }

		public int UserID
		{
			get
			{
				return ParseHelper.ParseToInt32(parameters[SecurityManager.ParamNames.UserId].ToString());
			}
		}

		private SecurityManager.User user;

		public SecurityManager.User User
		{
			get
			{
				if (user == null)
					user = SecurityManager.GetUser(UserID);
				return user;
			}
		}

		public override bool Refresh()
		{
			user = null;
			return base.Refresh();
		}

		// ShowPassport переехал в ActionOnMassmedia.WinForms.cs (открывает ActionForm).

		// DoAction переехал в ActionOnMassmedia.WinForms.cs.

        public override string DeleteConfirmationText 
		{
			get 
			{
                return string.Format(MessageAccessor.GetMessage(IsDeleted ? "DeleteActionPrompt" : "MoveAction2DeletedPrompt"), Name); 
			}
		}

		/// <summary>
		/// Разрешено ли делить/объединять акцию. <paramref name="messageKey"/> — ключ
		/// MessageAccessor с причиной отказа, null если разрешено.
		/// </summary>
		public bool CanSplitOrMerge(DateTime startDate, out string messageKey)
		{
			messageKey = null;
            if (SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsTrafficManager || !IsConfirmed) return true;
			if(startDate <= DateTime.Today)
			{
				messageKey = "SplitAllowedByAdmin";
				return false;
            }
			return true;
        }

		/// <summary>
		/// Кампании — кандидаты на перенос в новую акцию. null, если делить нечего;
		/// тогда <paramref name="messageKey"/> содержит ключ причины.
		/// </summary>
		public DataTable GetCampaignsForSplit(out string messageKey)
		{
			messageKey = null;
			DataTable dt = Campaigns();
			if (dt.Rows.Count < 2)
			{
				messageKey = "CanNotSplitAction";
				return null;
			}
			return dt;
		}

		/// <summary>
		/// Проверяет выбор пользователя для деления акции.
		/// </summary>
		public bool IsSplitSelectionValid(int selectedCount, out string messageKey)
		{
			messageKey = null;
			if (selectedCount == Campaigns().Rows.Count)
			{
				messageKey = "TooManyCampaignsSelected";
				return false;
			}
			if (selectedCount == 0)
			{
				messageKey = "NoCampaignSelected";
				return false;
			}
			return true;
		}

		/// <summary>
		/// Переносит выбранные кампании в новую акцию и пересчитывает обе.
		/// </summary>
		public void ApplySplitAction(IList<PresentationObject> campaignsToMove)
		{
			ActionOnMassmedia newAction = CreateNewActionForSplit();
			foreach (var campaign in campaignsToMove)
			{
				campaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
				campaign.Update();
			}
			Recalculate();
			newAction.Recalculate();
		}

		private ActionOnMassmedia CreateNewActionForSplit()
		{
            ActionOnMassmedia newAction = new ActionOnMassmedia(Firm);
            newAction[ParamNames.IsConfirmed] = IsConfirmed;
            newAction[SecurityManager.ParamNames.UserId] = this[SecurityManager.ParamNames.UserId];
            newAction.Update();
			return newAction;
        }

		// SplitAction, IsSplitOrMergeEnabled и CheckCampaignsSelectionResultForActionSplit
		// переехали в ActionOnMassmedia.WinForms.cs (диалоги и показ сообщений).

        // SplitCampaign переехал в ActionOnMassmedia.WinForms.cs.

        /// <summary>Кампании — кандидаты на разделение по кампаниям (тип Simple).</summary>
        internal bool CanSplitCampaign(out string messageKey)
        {
            messageKey = null;
            if (SetCampaignsFilterByType(Campaign.CampaignTypes.Simple).DefaultView.Count == 0)
            {
                messageKey = "NoCampaignsForSplit";
                return false;
            }
            return true;
        }

        /// <summary>Делит акцию по правилам <paramref name="splitRules"/> на новую акцию.</summary>
        internal void ApplySplitCampaign(IEnumerable<SplitRule> splitRules)
        {
            ActionOnMassmedia newAction = CreateNewActionForSplit();

            foreach (SplitRule rule in splitRules)
            {
                Campaign newCampaign = Campaign.CreateInstance(
                    int.Parse(rule.campaign[Campaign.ParamNames.CampaignTypeId].ToString()),
                    int.Parse(rule.campaign[Campaign.ParamNames.PaymentTypeID].ToString()),
                    int.Parse(rule.campaign[Campaign.ParamNames.MassmediaId].ToString()),
                    int.Parse(rule.campaign[Campaign.ParamNames.AgencyID].ToString()));
                newCampaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
                newCampaign[Campaign.ParamNames.ManagerDiscount] = rule.campaign[Campaign.ParamNames.ManagerDiscount];
                newCampaign.Update();
                MoveIssues(newCampaign, rule);
            }
            Recalculate();
            newAction.Recalculate();
            OnParentChanged(this, 1);
        }

		private void MoveIssues(Campaign newCampaign, SplitRule rule)
		{
			Dictionary<string, object> procParameters =	new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
			{
				["splitType"] = (int)rule.splitType,
                ["oldCampaignId"] = rule.campaign.CampaignId,
                ["newCampaignId"] = newCampaign.CampaignId
            };

            if (rule.splitType == SplitRule.SplitType.ByRollers)
            {
                foreach (var roller in rule.rollers)
                {
                    procParameters[Roller.ParamNames.RollerId] = int.Parse(roller[Roller.ParamNames.RollerId].ToString());
                    DataAccessor.ExecuteNonQuery("MoveIssues2NewCampaign", procParameters);
                }
            }
            else
            {
				procParameters["splitDate"] = rule.date;
                DataAccessor.ExecuteNonQuery("MoveIssues2NewCampaign", procParameters);
            }
        }

		internal DataTable SetCampaignsFilterByType(Campaign.CampaignTypes type)
		{
            DataTable filteredCampaigns = Campaigns();
            filteredCampaigns.DefaultView.RowFilter = string.Format("campaignTypeID = {0}", (int)type);

            return filteredCampaigns;
        }

        // Clone переехал в ActionOnMassmedia.WinForms.cs.
        // ChangePaymentTypeMass (диалог) — тоже там; здесь только применение.

        /// <summary>Действующие типы оплаты — список выбора массовой смены.</summary>
        public static DataTable LoadActivePaymentTypes()
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
            procParameters["ShowActive"] = true;
            return DataAccessor.LoadDataSet("PaymentTypesLoad", procParameters).Tables[0];
        }

        /// <summary>
        /// Кампании акции для чек-листа массовой смены: сущность 91 с селектором 3
        /// (mass-change-payment-type-seed.sql). Клон — чтобы не менять селектор у
        /// общей закэшированной сущности.
        /// </summary>
        public static Entity CampaignListEntity()
        {
            Entity entity = (Entity)EntityManager.GetEntity((int)Entities.GeneralCampaign).Clone();
            entity.AttributeSelector = 3;
            return entity;
        }

        /// <summary>
        /// Массово меняет тип оплаты у выбранных кампаний акции. Каждая кампания —
        /// своя транзакция (CampaignIUD), best-effort: сбойные попадают в
        /// <paramref name="tableErrors"/> (пустая, если ошибок не было), остальные
        /// применяются. Пересчёт не нужен — тип оплаты на цену не влияет.
        /// </summary>
        public void ApplyPaymentTypeChangeMass(int paymentTypeId, IEnumerable<PresentationObject> campaigns, out DataTable tableErrors)
        {
            tableErrors = ErrorManager.CreateErrorsTable();

            foreach (PresentationObject checkedCampaign in campaigns)
            {
                int campaignId = int.Parse(checkedCampaign.IDs[0].ToString());
                string label = string.IsNullOrEmpty(checkedCampaign.Name)
                    ? checkedCampaign[Campaign.ParamNames.MassmediaName].ToString()
                    : checkedCampaign.Name;

                try
                {
                    Campaign campaign = Campaign.GetCampaignById(campaignId);
                    if (campaign == null) continue;
                    if (campaign.PaymentTypeId == paymentTypeId) continue;

                    if (!campaign.IsChangePossible)
                    {
                        ErrorManager.AddErrorRow(tableErrors, DateTime.Now,
                            string.Format("{0}: {1}", label, MessageAccessor.GetMessage("ChangePaymentTypeIsForbidden")));
                        continue;
                    }

                    campaign.ApplyPaymentTypeChange(paymentTypeId);
                }
                catch (Exception ex)
                {
                    ErrorManager.AddErrorRow(tableErrors, DateTime.Now,
                        string.Format("{0}: {1}", label, ErrorManager.GetErrorMessage(ex)));
                }
            }
        }

        // ---- Карточка акции: кампании и цена (ActionForm в десктопе, страница акции в вебе) ----

        /// <summary>
        /// Добавляет кампании в акцию по одной: отказ по одной станции (например, такая
        /// кампания уже есть в акции — UIX_Campaign) не мешает остальным (решение по Д-4,
        /// docs/action-forms.md §10). Новая акция сначала записывается. Пересчёта нет:
        /// у кампании без выпусков нет цены.
        /// </summary>
        /// <param name="added">Записанные кампании, в порядке <paramref name="campaigns"/>.</param>
        /// <returns>Не добавленные — «станция - причина»; пустой список — добавлены все.</returns>
        internal List<string> AddCampaigns(IEnumerable<Campaign> campaigns, out List<Campaign> added)
        {
            if (IsNew) Update();

            added = new List<Campaign>();
            List<string> failed = new List<string>();
            foreach (Campaign campaign in campaigns)
            {
                string name = campaign[Campaign.ParamNames.MassmediaName] as string ?? Tr.T("Пакетная кампания");
                try
                {
                    campaign.Action = this;
                    campaign.Update();
                }
                catch (Exception ex)
                {
                    if (!(ex is System.Data.SqlClient.SqlException))   // SQL-отказы уже в логе (DataAccessor)
                        ErrorManager.LogError("Добавление кампании: " + name, ex); // i18n-ok: лог
                    failed.Add(name + " - " + (ErrorManager.GetErrorMessage(ex) ?? ex.Message));
                    continue;
                }
                added.Add(campaign);
            }
            return failed;
        }

        /// <summary>
        /// Цена акции при одном менеджерском коэффициенте <paramref name="ratio"/> у всех
        /// кампаний: сумма кампаний со всеми скидками, кроме менеджерской, умноженная на него.
        /// </summary>
        internal static decimal PriceWithManagerRatio(DataTable campaigns, decimal ratio)
        {
            decimal price = 0;
            foreach (DataRow row in campaigns.Rows)
            {
                Campaign campaign = new Campaign(row);
                price += campaign.Discount * campaign.PackDiscount * campaign.TariffPrice * ratio;
            }
            return price;
        }

        /// <summary>
        /// Менеджерская скидка хранится на кампании, у акции её нет — усреднённая: итоговая
        /// цена акции / сумма кампаний со всеми скидками, кроме менеджерской, 4 знака.
        /// </summary>
        internal decimal AverageManagerRatio(DataTable campaigns)
        {
            decimal priceWithoutManagerDiscount = PriceWithManagerRatio(campaigns, 1);
            return priceWithoutManagerDiscount == 0
                ? 0
                : Math.Round(TotalPrice / priceWithoutManagerDiscount, 4, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// «Цена рекламной акции»: итоговая цена распределяется по кампаниям с ненулевой ценой
        /// по тарифам, затем пересчёт акции — одной транзакцией. Последняя кампания забирает
        /// остаток, чтобы сумма сходилась до копейки.
        /// </summary>
        /// <param name="byRatio">true — у всех кампаний один менеджерский коэффициент
        /// <paramref name="ratio"/>; false — <paramref name="finalPrice"/> делится пропорционально
        /// нынешним итогам кампаний.</param>
        /// <param name="todayDate">Дата, на которую ставится скидка (бухгалтер и администратор
        /// выбирают её сами, остальным — сегодня).</param>
        internal void ApplyFinalPrice(decimal finalPrice, bool byRatio, decimal ratio, DateTime todayDate, int? managerDiscountReasonId)
        {
            if (!byRatio && TotalPrice == 0)
                throw new InvalidOperationException(Tr.T("Итоговая цена акции равна нулю: распределить новую цену пропорционально нечему. Задайте цену через коэффициент."));

            DataTable dataTable = Campaigns();
            // exclude campaigns with zero tariff price, as they won't be affected by discount and caused SQL error
            DataRow[] rows = dataTable.Select("TariffPrice <> 0");
            dataTable = rows.Length > 0
                ? rows.CopyToDataTable()
                : dataTable.Clone();

            DataAccessor.BeginTransaction();
            try
            {
                decimal distributedSoFar = 0m;
                int rowCount = dataTable.Rows.Count;

                for (int i = 0; i < rowCount; i++)
                {
                    Campaign campaign = new Campaign(dataTable.Rows[i]);
                    if (campaign.TariffPrice == 0) continue;

                    decimal newP;
                    if (i == rowCount - 1)
                    {
                        // ПОСЛЕДНЯЯ СТРОКА: забирает всё, что осталось от целевой суммы
                        newP = finalPrice - distributedSoFar;
                    }
                    else
                    {
                        // ОБЫЧНАЯ СТРОКА: считаем долю и жестко округляем до копеек
                        decimal rawP = byRatio
                            ? ratio * campaign.Discount * campaign.PackDiscount * campaign.TariffPrice
                            : finalPrice * campaign.FullPrice / TotalPrice;

                        newP = Math.Round(rawP, 2, MidpointRounding.AwayFromZero);
                        distributedSoFar += newP;
                    }

                    campaign.SetFinalPrice(newP, todayDate, SecurityManager.LoggedUser.Id, managerDiscountReasonId);
                }
                Recalculate(refreshFlag: true, todayDate: todayDate);
                DataAccessor.CommitTransaction();
            }
            catch
            {
                DataAccessor.RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Клонирует акцию с выбранными кампаниями (<paramref name="selectedItems"/> —
        /// дата клонирования и исходная кампания). Возвращает новую акцию;
        /// <paramref name="tableErrors"/> — таблица ошибок по отдельным кампаниям
        /// (пустая, если ошибок не было).
        /// </summary>
        internal ActionOnMassmedia ApplyClone(IEnumerable<(DateTime date, PresentationObject campaign)> selectedItems, out DataTable tableErrors)
        {
            ActionOnMassmedia newAction = new ActionOnMassmedia(Firm);
            newAction.Update();

            tableErrors = ErrorManager.CreateErrorsTable();

            foreach (var item in selectedItems)
            {
                int campaignTypeId = int.Parse(item.campaign[Campaign.ParamNames.CampaignTypeId].ToString());

                Campaign newCampaign = Campaign.CreateInstance(
                    campaignTypeId,
                    int.Parse(item.campaign[Campaign.ParamNames.PaymentTypeID].ToString()),
                    campaignTypeId == (int)Campaign.CampaignTypes.PackModule ?
                        null : (int?)int.Parse(item.campaign[Campaign.ParamNames.MassmediaId].ToString()),
                    int.Parse(item.campaign[Campaign.ParamNames.AgencyID].ToString()));
                newCampaign[ParamNames.ActionId] = newAction[ParamNames.ActionId];
                newCampaign.Update();
                int shiftInDays = (item.date - DateTime.Parse(item.campaign[Campaign.ParamNames.StartDate].ToString())).Days;

                Campaign selectedCampaign = (Campaign)item.campaign;

                if (selectedCampaign.CampaignType == Campaign.CampaignTypes.Simple)
                    CloneRollerIssues(selectedCampaign, newCampaign, shiftInDays, tableErrors);
                else if (selectedCampaign.CampaignType == Campaign.CampaignTypes.Module)
                    CloneModuleIssues(selectedCampaign, newCampaign, shiftInDays, tableErrors);
                else if (selectedCampaign.CampaignType == Campaign.CampaignTypes.Sponsor)
                {
                    CloneProgramIssues(selectedCampaign, newCampaign, shiftInDays, tableErrors);
                    CloneRollerIssues(selectedCampaign, newCampaign, shiftInDays, tableErrors);
                }
                else if (selectedCampaign.CampaignType == Campaign.CampaignTypes.PackModule)
                {
                    ClonePackModuleIssues(selectedCampaign, (CampaignPackModule)newCampaign, shiftInDays, tableErrors);
                }
            }
            ((ActionOnMassmedia)newAction).Recalculate();
            OnParentChanged(this, 1);
            return newAction;
        }

        private void ClonePackModuleIssues(Campaign campaign, CampaignPackModule newCampaign, int shiftInDays, DataTable tableErrors)
        {
            campaign.ChildEntity = EntityManager.GetEntity((int)Entities.PackModuleIssue);
            foreach (DataRow item in campaign.GetContent().Rows)
            {
                PackModuleIssue issue = new PackModuleIssue(item);
                DateTime newDate = issue.IssueDate.AddDays(shiftInDays);
                // есть ли прайс-лист для этого программы в новом дне ?
                PackModule module = issue.PackModule;
                Pricelist pricelist = module.GetPriceList(newDate);
                if (pricelist == null)
                    ErrorManager.AddErrorRow(tableErrors, newDate, Tr.Format(Properties.Resources.PackModulePricelistNotFound, module.Name));
                else
                {
                    try
                    {
						newCampaign.AddPackModuleIssue((PackModulePricelist)pricelist, issue.Roller, issue.Position, newDate, null);
                    }
                    catch (Exception ex)
                    {
                        ErrorManager.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
                    }
                }
            }
        }

        private void CloneProgramIssues(Campaign campaign, Campaign newCampaign, int shiftInDays, DataTable tableErrors)
        {
			ProgramPartOfSponsorCampaign part = new ProgramPartOfSponsorCampaign(campaign.CampaignId);
            foreach (DataRow item in part.GetProgramIssues().Rows)
            {
                ProgramIssue issue = new ProgramIssue(item);
                DateTime newDate = issue.IssueDate.AddDays(shiftInDays);
				// есть ли прайс-лист для этой программы в новом дне ?
				SponsorPricelist pricelist = issue.SponsorProgram.GetPricelist(newDate);
                if (pricelist == null) 
				{
                    ErrorManager.AddErrorRow(tableErrors, newDate, Tr.Format(Properties.Resources.SponsorPricelistNotFound, issue.SponsorProgram.Name));
                    continue; 
				}
                    
				SponsorTariff tariff = pricelist.GetTariffBydate(newDate);
                if (tariff == null)
                {
                    ErrorManager.AddErrorRow(tableErrors, newDate, Tr.Format(Properties.Resources.SponsorTariffNotFound, issue.SponsorProgram.Name));
                    continue;
                }

				newCampaign.AddProgramIssue(issue.SponsorProgram, tariff.TariffId, newDate, tariff.Price, pricelist.Bonus, false);
            }
        }

        private void CloneModuleIssues(ObjectContainer campaign, Campaign newCampaign, int shiftInDays, DataTable tableErrors)
		{
            campaign.ChildEntity = EntityManager.GetEntity((int)Entities.ModuleIssue);
            foreach(DataRow item in campaign.GetContent().Rows)
			{
				ModuleIssue issue = new ModuleIssue(item);
                DateTime newDate = issue.IssueDate.AddDays(shiftInDays);
				// есть ли прайс-лист для этого модуля в новом дне ?
				Module module = issue.Module;
				ModulePricelist pricelist =  module.GetPriceList(newDate);
				if (pricelist == null)
					ErrorManager.AddErrorRow(tableErrors, newDate, Tr.Format(Properties.Resources.ModulePricelistNotFound, module.Name));
				else
				{
					try
					{
						newCampaign.AddModuleIssue(module, issue.Roller, pricelist, newDate, issue.Position, null);
					}
                    catch (Exception ex)
                    {
                        ErrorManager.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
                    }
                }
            }
        }

        private void CloneRollerIssues(ObjectContainer campaign, Campaign newCampaign, int shiftInDays, DataTable tableErrors)
        {
			Massmedia mm = (new CampaignOnSingleMassmedia(newCampaign.CampaignId)).Massmedia;
			campaign.ChildEntity = EntityManager.GetEntity((int)Entities.Issue);
			foreach (DataRow item in campaign.GetContent().Rows)
			{
                RollerIssue issue = new RollerIssue(item);
				DateTime newDate = issue.IssueDateOriginal.AddDays(shiftInDays);

                TariffWindow window = mm.GetTariffWindow(newDate);
				if (window != null)
                {
					try
					{
						newCampaign.AddIssue(issue.Roller, window, issue.Position, null);
					}
					catch (Exception ex)
					{
						ErrorManager.AddErrorRow(tableErrors, newDate, MessageAccessor.GetMessage(ex.Message));
					}
                }
				else
				{
					ErrorManager.AddErrorRow(tableErrors, newDate, Tr.T("Рекламное окно не найдено"));
				}
			}
        }

        // ShowRollers переехал в ActionOnMassmedia.WinForms.cs.

		public static bool CheckLoggedUserRight(string actionName, ActionOnMassmedia action)
		{
			if (SecurityManager.LoggedUser.Id != action.UserID
				&& !SecurityManager.LoggedUser.IsRightToEditForeignActions()
				&& (!SecurityManager.LoggedUser.IsRightToEditGroupActions() || action.User == null || !SecurityManager.LoggedUser.IsInGroup(action.User.Groups))
				&& (new List<string> { ActionNames.Activate, ActionNames.ActivateTest, ActionNames.Deactivate,
					ActionNames.Merge, ActionNames.Recalculate, Constants.EntityActions.Edit,
					Constants.EntityActions.Delete, Action.ActionNames.ChangeFirm,
					Action.ActionNames.ChangeCreator, Issue.ActionNames.SetFirst, Issue.ActionNames.SetSecond,
					Issue.ActionNames.SetLast, Issue.ActionNames.SetUnknow, Constants.EntityActions.Transfer,
					Constants.Actions.Substitute, Campaign.ActionNames.ChangePaymentType,
					Campaign.ActionNames.ChangeAgency, Action.ActionNames.ChangePaymentTypeMass}).Contains(actionName))
				return false;
			return true;
		}

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (!CheckLoggedUserRight(actionName, this))
				return true;
            if (actionName == ActionNames.Activate || string.Compare(actionName, ActionNames.ActivateTest) == 0)
                return base.IsActionHidden(actionName, type) || IsConfirmed;
            if (actionName == ActionNames.Deactivate)
                return base.IsActionHidden(actionName, type) || !IsConfirmed;

            return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (!CheckLoggedUserRight(actionName, this))
				return false;

			return base.IsActionEnabled(actionName, type);
		}

		/// <summary>Вернуть удалённую акцию из журнала удалённых (ActionRestore).</summary>
		public void ApplyRestore()
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters.Add(ParamNames.ActionId, ActionId);
			DataAccessor.ExecuteNonQuery("ActionRestore", procParameters);
			OnObjectDeleted(this);
		}

		// Merge (диалог) переехал в ActionOnMassmedia.WinForms.cs. Активация разрезана
		// (2026-09-29): запись и разбор результата — RunActivation здесь, окна
		// настроек и показ результата — в UI (десктоп и веб).

		/// <summary>Кандидаты на объединение с этой акцией. null, если объединять не с чем.</summary>
		public DataTable GetActionsForMerge()
		{
			Entity entityAction = EntityManager.GetEntity((int) Entities.Action);
			Dictionary<string, object> parametersActions =
				DataAccessor.PrepareParameters(entityAction, InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			parametersActions[Firm.ParamNames.FirmId] = Firm.FirmId;
			parametersActions[SecurityManager.ParamNames.UserId] = parameters[SecurityManager.ParamNames.UserId];
			parametersActions["withoutActionId"] = ActionId;
			parametersActions["isShowActivate"] = IsConfirmed;
			parametersActions["isShowNotActivate"] = !IsConfirmed;
			DataSet ds = DataAccessor.DoAction(parametersActions) as DataSet;
			return ds?.Tables[Constants.TableNames.Data];
		}

		/// <summary>Объединяет эту акцию с <paramref name="action2"/>.</summary>
		public void ApplyMerge(ActionOnMassmedia action2)
		{
			Dictionary<string, object> parametersMerge = DataAccessor.CreateParametersDictionary();
			parametersMerge["firstActionID"] = ActionId;
			parametersMerge["secondActionID"] = action2.ActionId;
			parametersMerge["liveActionID"] = 0;
			DataAccessor.ExecuteNonQuery("MergeActions", parametersMerge);
			OnParentChanged(this, 1);
			/*
			int liveActionID = (int) parametersMerge["liveActionID"];
			if (liveActionID > 0)
			{
				ActionOnMassmedia action = GetActionById(liveActionID);
				action.Recalculate();
				OnParentChanged(this, 1);
			}
			*/
		}

		public void Recalculate(bool refreshFlag = true, DateTime? todayDate = null)
		{
			using (OperationScope.Start("ActionRecalculate"))
			{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[ParamNames.ActionId] = ActionId;

			if (todayDate.HasValue)
				procParameters["todayDate"] = todayDate.Value;

			// OUTPUT parameter
			procParameters[ParamNames.TotalPrice] = DBNull.Value;
			var oldTiotalPrice = TotalPrice;

            DataAccessor.ExecuteNonQuery("ActionRecalculate", procParameters);
			
			// подтянем OUTPUT в объект (на случай, если refreshFlag = false)
			if (procParameters.ContainsKey(ParamNames.TotalPrice) &&
				procParameters[ParamNames.TotalPrice] != DBNull.Value)
			{
				this[ParamNames.TotalPrice] = procParameters[ParamNames.TotalPrice];
			}

			if (refreshFlag)
				Refresh();

			if (oldTiotalPrice > TotalPrice && IsConfirmed)
				CorrectPaymentAction();
		}
		}

		private void CorrectPaymentAction()
		{
            var p = DataAccessor.CreateParametersDictionary();

            p[ParamNames.ActionId] = ActionId;
            DataAccessor.ExecuteNonQuery("PaymentAction_CorrectByActionTotalPrice", p, 30, true);
        }

        private void SetChildEntity()
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.CampaignOnMassmedia);
		}

		// DeactivateAction переехал в ActionOnMassmedia.WinForms.cs.

		/// <summary>Можно ли деактивировать акцию. false — <paramref name="errorMessage"/> заполнен.</summary>
		public bool CanDeactivate(out string errorMessage)
		{
			if (!(SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsTrafficManager) && StartDate < DateTime.Today)
			{
				errorMessage = Tr.T(Properties.Resources.DeactivationNotAllowed);
				return false;
			}
			errorMessage = null;
			return true;
		}

		public void ApplyDeactivate()
		{
			DataAccessor.PrepareParameters(
				parameters, entity, InterfaceObjects.FakeModule, Constants.Actions.Deactivate);
			DataAccessor.DoAction(parameters);
			Refresh();
			OnObjectDeleted(this);
		}

        /// <summary>Параметры активации — окно «Параметры активации».</summary>
        public sealed class ActivationSettings
        {
            /// <summary>Пытаться переносить выпуски, которые не удалось активировать.</summary>
            public bool TryTransferFailedIssues { get; set; }
            public bool AllowDifferentWindowPrice { get; set; }
            public bool AvoidFirmRollerWindows { get; set; } = true;
            public int TransferAttemptCount { get; set; }

            /// <summary>Параметры без переноса — предпросмотр и «ОК» без галочки.</summary>
            public static ActivationSettings NoTransfer => new ActivationSettings();
        }

        /// <summary>Что вернула активация (или её предпросмотр).</summary>
        public sealed class ActivationResult
        {
            public DataTable Activated { get; internal set; }
            public DataTable Transferred { get; internal set; }
            public DataTable NotActivated { get; internal set; }
            /// <summary>Фатальная ошибка процедуры; null — её нет.</summary>
            public string FatalError { get; internal set; }
        }

        /// <summary>
        /// Ролики и выпуски программ без предмета рекламы. Ролики без предмета не дают
        /// активировать (их сначала назначают в окне «ролики акции»); выпуски программ —
        /// только предупреждение.
        /// </summary>
        public void CheckAdvertTypes(out bool rollersWithout, out bool programIssuesWithout)
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
            procParameters[ParamNames.ActionId] = ActionId;
            DataSet dataSet = DataAccessor.LoadDataSet("RollersWithoutAdvertype", procParameters);
            rollersWithout = dataSet.Tables[0].Rows.Count > 0;
            programIssuesWithout = dataSet.Tables[1].Rows.Count > 0;
        }

        /// <summary>
        /// Активация (или предпросмотр при <paramref name="isTest"/>). После настоящей
        /// активации без фатальной ошибки акция перечитывается, пересчитывается и уходит
        /// из журнала макетов (OnObjectDeleted) — как было в десктопе.
        /// </summary>
        public ActivationResult RunActivation(bool isTest, ActivationSettings settings)
        {
            parameters["isTestActivate"] = isTest;
            parameters["tryTransferFailedIssues"] = settings.TryTransferFailedIssues;
            parameters["allowDifferentWindowPrice"] = settings.TryTransferFailedIssues && settings.AllowDifferentWindowPrice;
            parameters["avoidFirmRollerWindows"] = settings.TryTransferFailedIssues && settings.AvoidFirmRollerWindows;
            parameters["transferAttemptCount"] = settings.TryTransferFailedIssues ? settings.TransferAttemptCount : 0;

            DataAccessor.PrepareParameters(parameters, entity, InterfaceObjects.FakeModule, Constants.Actions.Activate);
            DataSet ds = (DataSet)DataAccessor.DoAction(parameters);

            var result = new ActivationResult
            {
                Activated = ds.Tables["activated"],
                Transferred = ds.Tables.Contains("transferred")
                    ? ds.Tables["transferred"]
                    : (ds.Tables.Count > 3 ? ds.Tables[3] : null),
                NotActivated = ds.Tables["notactivated"],
                FatalError = ds.Tables["fatal_errors"].Rows.Count > 0
                    ? ds.Tables["fatal_errors"].Rows[0]["errorMessage"].ToString()
                    : null,
            };

            if (!isTest && result.FatalError == null)
            {
                Refresh();
                Recalculate();
                OnObjectDeleted(this);
            }
            return result;
        }

        /// <summary>Колонки таблиц результата активации — общие для десктопа и веба.</summary>
        public static class ActivationColumns
        {
            public static Entity.Attribute[] Issues() => new[]
            {
                new Entity.Attribute("radiostationName", "Радиостанция", "nvarchar"),
                new Entity.Attribute("groupName", "Группа", "nvarchar"),
                new Entity.Attribute("name", "Ролик/Программа", "nvarchar"),
                new Entity.Attribute("advertTypeName", "Предмет рекламы", "nvarchar"),
                new Entity.Attribute("issueDate", "Дата", "datetime"),
                new Entity.Attribute("duration", "Пр-ть", "nvarchar"),
                new Entity.Attribute("issuePosition", "Порядок", "nvarchar"),
                new Entity.Attribute("statusDescription", "Статус", "nvarchar"),
            };

            public static Entity.Attribute[] Transferred() => new[]
            {
                new Entity.Attribute("radiostationName", "Радиостанция", "nvarchar"),
                new Entity.Attribute("groupName", "Группа", "nvarchar"),
                new Entity.Attribute("name", "Ролик/Программа", "nvarchar"),
                new Entity.Attribute("advertTypeName", "Предмет рекламы", "nvarchar"),
                new Entity.Attribute("oldIssueDate", "Дата (исходная)", "datetime"),
                new Entity.Attribute("issueDate", "Дата (новая)", "datetime"),
                new Entity.Attribute("duration", "Пр-ть", "nvarchar"),
                new Entity.Attribute("issuePosition", "Порядок", "nvarchar"),
                new Entity.Attribute("statusDescription", "Статус", "nvarchar"),
            };
        }

        public static ActionOnMassmedia GetActionById(int actionId)
		{
			ActionOnMassmedia action = new ActionOnMassmedia(actionId);
			action.Refresh();
			return action;
		}

        // DisplayData(ListBox) переехал в ActionOnMassmedia.WinForms.cs.

        public DataTable Issues
        {
			get
			{
				Dictionary<string, object> procParameters = new Dictionary<string, object>
				{
					[ParamNames.ActionId] = ActionId,
				};
				
				return DataAccessor.LoadDataSet("ActionIssues", procParameters).Tables[0];
            }
        }
    }
}
