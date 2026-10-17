using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using log4net;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Действия над одним выпуском модуля (сущность 130 «Выпуск модуля») — веб-аналог ModuleIssue.Delete, Issue.DoAction и
	/// ModuleIssue.DoAction: «Удалить», «Сделать первым / вторым / последним», «Убрать порядок», «Заменить рекламный ролик»
	/// (docs/tasks/web-action-forms.md, подэтап 6.5). Вход — номер выпуска: им пользуются и панель места вкладки «Модули»,
	/// и дерево журналов (узел выпуска модуля), где набора вкладки нет.
	///
	/// Строка выпуска читается заново (ModuleIssueRetrieve @moduleIssueId), и процедуре уходит полная: кампания, модуль,
	/// прайс-лист модуля, день, ролик, isConfirmed акции (ModuleDaySource.ModuleIssueParameters). Права — здесь же, до записи
	/// («Редактировать» акции, удалённая акция — отказ, плюс действие сущности 130): запись идёт LoadDataSet, мимо проверки
	/// прав DataAccessor.DoAction. Акция читается заново перед записью: коррекция платежа подтверждённой акции сравнивает её
	/// прежнюю цену с новой. Один пересчёт на действие; после действия — текст о цене акции, как в десктопе.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class ModuleIssueChange
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ModuleIssueChange));

		public const string SetFirstAction = Issue.ActionNames.SetFirst;
		public const string SetSecondAction = Issue.ActionNames.SetSecond;
		public const string SetLastAction = Issue.ActionNames.SetLast;
		public const string SetUnknownAction = Issue.ActionNames.SetUnknow;
		public const string SubstituteAction = Constants.Actions.Substitute;
		public const string DeleteAction = Constants.EntityActions.Delete;

		private static readonly string[] PositionActions = { SetFirstAction, SetSecondAction, SetLastAction, SetUnknownAction };
		private static readonly string[] AllActions = PositionActions.Concat(new[] { SubstituteAction, DeleteAction }).ToArray();

		/// <summary>Отказ ModuleIssueIUD AddItem, которого нет у UpdateItem (Д-20), — его проверяет <see cref="SetPosition"/>.</summary>
		private const string PositionErrorForTheSameAction = "PositionErrorForTheSameAction";

		/// <summary>Итог действия над выпуском модуля. Отказ — исключение (текст — ErrorPresenter.Describe), итог есть — действие выполнено.</summary>
		public sealed class Result
		{
			internal Result() { }

			/// <summary>Текст о цене акции после действия — как сообщение десктопа (CampaignPriceChanged / CampaignPriceWithoutChanged).</summary>
			public string PriceMessage { get; internal set; }

			/// <summary>«Удалить»: кампания выпуска, созданная вкладкой в этом сеансе, опустела и удалена в той же транзакции (В-21).</summary>
			public int? DeletedCampaignId { get; internal set; }

			/// <summary>
			/// «Удалить»: опустевшая кампания «создана здесь» осталась — нет права удалять кампании или удаление отказало (причина —
			/// <see cref="Warning"/>); плашка «без выпусков» по В-6.
			/// </summary>
			public int? KeptEmptyCampaignId { get; internal set; }

			/// <summary>Что не вышло при выполненном действии: отказ удаления опустевшей кампании, сбой пересчёта после замены ролика.</summary>
			public string Warning { get; internal set; }

			/// <summary>
			/// «Заменить рекламный ролик»: незаменённые строки RollerSubstitute (windowDateOriginal — время окна выхода, message —
			/// причина), как таблица «Незамененные ролики» у замены ролика кампании; null — заменено. Отказ одной строки оставляет
			/// без замены весь день (остальные строки дня процедура помечает «Dependence»).
			/// </summary>
			public DataTable Unsubstituted { get; internal set; }
		}

		/// <summary>
		/// Доступные действия выпуска — как пункты меню десктопа (ModuleIssue.IsActionEnabled: права группы на сущность 130 и
		/// правило чужих акций; у текущего порядка пункт погашен; позиции погашены у модуля со штучными тарифами, maxCapacity
		/// 1–3; замена — у ролика «для всех фирм» и его клона) и вдобавок «Редактировать» акции: у удалённой акции и без права
		/// пусто. Выпуска нет — пусто.
		/// </summary>
		public static ISet<string> EnabledActions(int moduleIssueId)
		{
			HashSet<string> enabled = new HashSet<string>();
			DataRow row = ModuleDaySource.LoadModuleIssue(moduleIssueId);
			if (row == null)
				return enabled;
			ActionOnMassmedia action = ActionWorkspace.Load(Convert.ToInt32(row[Action.ParamNames.ActionId]));
			if (action == null || !ActionWorkspace.CanEdit(action))
				return enabled;
			ModuleIssue issue = new ModuleIssue(row);
			foreach (string actionName in AllActions)
				if (issue.IsActionEnabled(actionName, ViewType.Journal))
					enabled.Add(actionName);
			return enabled;
		}

		/// <summary>Позиция в блоке по пункту меню (<see cref="SetFirstAction"/> … <see cref="SetUnknownAction"/>): −20, −10, 10, 0.</summary>
		public static int PositionOf(string actionName)
		{
			switch (actionName)
			{
				case SetFirstAction: return (int)RollerPositions.First;
				case SetSecondAction: return (int)RollerPositions.Second;
				case SetLastAction: return (int)RollerPositions.Last;
				case SetUnknownAction: return (int)RollerPositions.Undefined;
				default: throw new ArgumentOutOfRangeException(nameof(actionName), actionName, null);
			}
		}

		/// <summary>
		/// «Сделать первым / вторым / последним», «Убрать порядок» (<paramref name="positionId"/> −20, −10, 10, 0) — как
		/// Issue.SetPosition десктопа, но со свежей полной строкой (десктопный комбо слал строку панели без isConfirmed, Д-17).
		/// Одна транзакция: проверка Д-20 — у другого выпуска того же модуля этой акции в тот же день уже есть эта позиция
		/// (UpdateItem этого не проверяет, AddItem отказывает PositionErrorForTheSameAction; проверка не атомарна — чужая
		/// транзакция может успеть между ней и записью, как и у AddItem), ModuleIssueIUD UpdateItem (цена выпуска —
		/// fn_GetIssuePrice с наценкой прайс-листа модуля) и пересчёт акции. Любой отказ откатывает всё: UpdateItem уменьшает
		/// счётчики окон до своих проверок. Модулю со штучными тарифами позицию отказывает процедура
		/// (MaxCapacityModuleSetPositionError — потому и передаётся @modulePricelistID). Позиция пишется в окна выхода
		/// дочерних строк, а hlp_IssueVerify проверяет окна слота модуля (Д-24, фасадом не лечится).
		/// </summary>
		public static Result SetPosition(int moduleIssueId, int positionId)
		{
			string actionName = PositionActions.FirstOrDefault(a => PositionOf(a) == positionId)
				?? throw new ArgumentOutOfRangeException(nameof(positionId), positionId, null);
			ActionOnMassmedia action = LoadForWrite(Load(moduleIssueId), actionName);
			decimal oldPrice = action.TotalPrice;
			WindowsSource.RunInTransaction(() =>
			{
				DataRow row = Load(moduleIssueId);
				if (positionId != 0 && HasPositionInAction(action.ActionId, row, positionId))
					throw new InvalidOperationException(MessageAccessor.GetMessage(PositionErrorForTheSameAction) ?? PositionErrorForTheSameAction);
				Dictionary<string, object> parameters = ModuleDaySource.ModuleIssueParameters(row, Constants.Actions.Update);
				parameters[Issue.ParamNames.PositionId] = positionId;
				DataAccessor.LoadDataSet("ModuleIssueIUD", parameters);
				action.Recalculate(false);
			});
			return Priced(oldPrice, action);
		}

		/// <summary>Другой выпуск того же модуля этой акции в тот же день с той же позицией (условие PositionErrorForTheSameAction у AddItem).</summary>
		private static bool HasPositionInAction(int actionId, DataRow issue, int positionId)
		{
			int moduleIssueId = Convert.ToInt32(issue[Issue.ParamNames.ModuleIssueId]);
			int moduleId = Convert.ToInt32(issue[Module.ParamNames.ModuleId]);
			DateTime day = Convert.ToDateTime(issue[Issue.ParamNames.IssueDate]).Date;
			return ComboModule.LoadIssues(actionId).Rows.Cast<DataRow>().Any(r =>
				Convert.ToInt32(r[Issue.ParamNames.ModuleIssueId]) != moduleIssueId
				&& Convert.ToInt32(r[Module.ParamNames.ModuleId]) == moduleId
				&& Convert.ToDateTime(r[Issue.ParamNames.IssueDate]).Date == day
				&& Convert.ToInt32(r[Issue.ParamNames.PositionId]) == positionId);
		}

		/// <summary>Число дочерних строк Issue выпуска — окон, из которых «Удалить» снимет модуль (ModuleIssueContentRetrieve).</summary>
		public static int WindowCount(int moduleIssueId) => ModuleDaySource.LoadIssueContent(moduleIssueId).Rows.Count;

		/// <summary>
		/// «Удалить» — модуль снимается на весь день (все дочерние строки Issue), как ModuleIssue.Delete десктопа, но с одним
		/// пересчётом вместо трёх (Д-16) — ModuleDaySource.RemoveOne: DeleteItem со свежей полной строкой, В-21 и пересчёт одной
		/// транзакцией. В-21: кампания выпуска из <paramref name="createdHereCampaignIds"/> (создана вкладкой в этом сеансе; из
		/// дерева — пусто), оставшаяся без выпусков, удаляется в той же транзакции, как у «Удалить выпуски»; из набора вкладки
		/// <paramref name="scope"/> (если удаляют с вкладки) она уходит, её строки снова ожидающие. Удаление подтверждённого
		/// выпуска ближе DaysLog дней шлёт объявление администраторам — как в десктопе.
		/// </summary>
		public static Result Delete(int moduleIssueId, int acceptedRollerId, ISet<int> createdHereCampaignIds, ModuleDaySource.ModuleScope scope = null)
		{
			DataRow row = Load(moduleIssueId);
			ActionOnMassmedia action = LoadForWrite(row, DeleteAction);
			decimal oldPrice = action.TotalPrice;
			ModuleDaySource.ModuleBatchResult removed = ModuleDaySource.RemoveOne(action, row, acceptedRollerId, createdHereCampaignIds, scope);
			Result result = Priced(oldPrice, action);
			result.DeletedCampaignId = removed.DeletedCampaignIds.Cast<int?>().FirstOrDefault();
			result.KeptEmptyCampaignId = removed.KeptEmptyCampaignIds.Cast<int?>().FirstOrDefault();
			if (removed.Errors.Rows.Count > 0)
				result.Warning = Convert.ToString(removed.Errors.Rows[0]["description"]);
			return result;
		}

		/// <summary>
		/// Кандидаты на замену ролика — как CampaignPart.GetRollersForSubstitution (активные ролики фирмы акции, кроме текущего),
		/// но только типов «Реклама», «Новость», «Программа» (В-25: у модулей не работают проверки, привязанные к окну); null —
		/// менять не на что.
		/// </summary>
		public static DataTable SubstituteCandidates(int moduleIssueId)
		{
			DataRow row = Load(moduleIssueId);
			LoadForWrite(row, SubstituteAction);
			return Candidates(new ModuleIssue(row));
		}

		/// <summary>
		/// «Заменить рекламный ролик» — как десктопный ModuleIssue (CampaignPart.ApplyRollerSubstitution →
		/// PrepareSubstitutionParametersAndExecute): RollerSubstitute с @moduleID и таблицей #days из дня выпуска — ролик
		/// меняется во всех выпусках этого модуля кампании за день с тем же роликом (повтор модуля в день, В-18, — тоже) и в их
		/// дочерних строках; соседние модули кампании не трогаются. Режим «один выпуск» (@issueID) не годится: он сменил бы
		/// ModuleIssue.rollerID у всех модулей кампании за день, а дочернюю строку — одну.
		///
		/// Не одной транзакцией: загрузка с таблицей (#days) открывает своё соединение и свою транзакцию (DataAccessor.LoadDataSet
		/// с DataSet), внутри транзакции ядра она ждала бы её же блокировок. Поэтому замена — первой, своей транзакцией, а
		/// пересчёт — следующей и только при другой длине ролика (при равной RollerSubstitute не меняет ни цену, ни занятость
		/// окон), как замена у ролика кампании (сущности 95/215). Сбой пересчёта после записанной замены — <see cref="Result.Warning"/>.
		/// </summary>
		public static Result Substitute(int moduleIssueId, int newRollerId)
		{
			DataRow row = Load(moduleIssueId);
			ActionOnMassmedia action = LoadForWrite(row, SubstituteAction);
			ModuleIssue issue = new ModuleIssue(row);
			// Ролик «для всех фирм» и его клон десктоп заменить не даёт (ModuleIssue.IsActionEnabled).
			if (!issue.IsActionEnabled(SubstituteAction, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			DataRow candidate = Candidates(issue)?.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r[Roller.ParamNames.RollerId]) == newRollerId)
				?? throw new InvalidOperationException(Tr.T("Ролика нет в списке роликов фирмы. Обновите страницу."));
			Campaign campaign = issue.Campaign
				?? throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			Roller oldRoller = issue.Roller;
			Roller newRoller = new Roller(candidate);
			decimal oldPrice = action.TotalPrice;

			DataTable unsubstituted = CampaignRoller.ApplyRollerSubstitutionForDays(campaign, oldRoller, newRoller,
				Days(issue.IssueDate), row[Module.ParamNames.ModuleId], null);
			Result result = new Result();
			if (unsubstituted != null && unsubstituted.Rows.Count > 0)
				result.Unsubstituted = unsubstituted;
			else if (oldRoller.Duration != newRoller.Duration)
			{
				try
				{
					WindowsSource.RunInTransaction(() => action.Recalculate(false));
				}
				catch (Exception ex)
				{
					Log.Error($"Module issue {moduleIssueId}: recalculation after roller substitution failed (action {action.ActionId})", ex);
					result.Warning = Tr.Format("Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.", ErrorManager.GetErrorMessage(ex));
				}
			}
			result.PriceMessage = CampaignPart.PriceChangeText(oldPrice, action.TotalPrice);
			return result;
		}

		/// <summary>Кандидаты на замену у выпуска — ролики фирмы типов 1–3; null — нет.</summary>
		private static DataTable Candidates(ModuleIssue issue)
		{
			DataTable all = issue.GetRollersForSubstitution(issue.Roller);
			if (all == null)
				return null;
			DataTable result = all.Clone();
			foreach (DataRow r in all.Rows)
			{
				int type = ParseHelper.GetInt32FromObject(r[Roller.ParamNames.RolActionTypeID], 0);
				if (type >= ModuleDaySource.FirstModuleRollerType && type <= ModuleDaySource.LastModuleRollerType)
					result.ImportRow(r);
			}
			return result.Rows.Count == 0 ? null : result;
		}

		/// <summary>Таблица #days процедуры RollerSubstitute на один день — как CampaignPart.CreateTableWithDays у десктопного выпуска модуля.</summary>
		private static DataTable Days(DateTime day)
		{
			DataTable days = new DataTable("days");
			days.Columns.Add(TariffWindow.ParamNames.WindowId, typeof(int));
			days.Columns.Add(Issue.ParamNames.IssueDate, typeof(DateTime));
			days.Rows.Add(DBNull.Value, day);
			return days;
		}

		/// <summary>Свежая строка выпуска (ModuleIssueRetrieve @moduleIssueId); выпуска нет — отказ.</summary>
		private static DataRow Load(int moduleIssueId) =>
			ModuleDaySource.LoadModuleIssue(moduleIssueId) ?? throw new InvalidOperationException(Tr.T("Выпуска уже нет."));

		/// <summary>
		/// Свежая акция выпуска для записи: удалённая — отказ; без «Редактировать» акции (права группы и правило чужих акций —
		/// то же, что CheckLoggedUserRight у пунктов выпуска) или без действия сущности 130 у группы — отказ.
		/// </summary>
		private static ActionOnMassmedia LoadForWrite(DataRow issue, string actionName)
		{
			ActionOnMassmedia action = ActionWorkspace.Load(Convert.ToInt32(issue[Action.ParamNames.ActionId]));
			if (action == null || ActionWorkspace.IsDeleted(action))
				throw new InvalidOperationException(Tr.T("Рекламная акция удалена. Обновите страницу."));
			if (!ActionWorkspace.CanEdit(action)
				|| !EntityManager.GetEntity((int)Entities.ModuleIssue).IsActionEnabled(actionName, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			return action;
		}

		private static Result Priced(decimal oldPrice, ActionOnMassmedia action) =>
			new Result { PriceMessage = CampaignPart.PriceChangeText(oldPrice, action.TotalPrice) };
	}
}
