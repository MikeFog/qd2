using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using log4net;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	/// <summary>
	/// Удаление выпусков из дерева кампании.
	///
	/// «Удалить» у узлов дерева кампании — дней, роликов, модулей, пакетных модулей, спонсорских программ и выпусков
	/// (наследники CampaignPart, кроме самой кампании и выпусков ролика и модуля, у которых своё веб-удаление). Веб-аналог
	/// CampaignPart.DoAction(Delete): процедура DeleteItem по метаданным, пересчёт акции и текст о смене её цены. Отличия
	/// от десктопа: запись и пересчёт — одной транзакцией (отказ процедуры откатывает всё), а на «Нет» в вопросе пересчёта
	/// нет (в десктопе он шёл и после отказа). Права проверяются здесь же, как пункт меню.
	///
	/// «Удалить рекламные выпуски» (DeleteIssues) — веб-аналог Campaign.DeleteIssues и CampaignDaysForm: паспорт
	/// CampaignDaysDelete, дерево «день → выпуски» (CampaignDaysTreePassport) с отбором по позиции, запись — DeleteItem
	/// на выпуск (IssueIUD, у модульной — ModuleIssueIUD), один пересчёт в конце. Отличия от десктопа: по дням транзакциями
	/// с точкой сохранения на выпуск (у десктопа — транзакция на выпуск); смена «Позиция:» сбрасывает отметки (десктоп
	/// удалял и скрытые); в чужой акции — отказ, как у «Удалить» (в десктопе DeleteIssues в CheckLoggedUserRight не входит).
	/// Пакетные кампании и программы спонсорской — с вкладками «Пакеты» и «Спонсорство».
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class CampaignIssuesDeletion
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(CampaignIssuesDeletion));

		/// <summary>Удалить часть кампании; возвращает текст о цене акции.</summary>
		public static string DeletePart(PresentationObject part)
		{
			if (!(part is CampaignPart p) || !p.IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));

			Campaign campaign = Campaign.GetCampaignById(p.CampaignId);
			if (campaign == null)
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			// Узел дерева мог устареть: акцию удалили в другом сеансе (процедуры DeleteItem частей этого не проверяют, а
			// счётчики окон удалённой акции уже вычтены — второе вычитание испортило бы их после восстановления).
			ActionOnMassmedia action = ActionWorkspace.Load(campaign.Action.ActionId);
			if (action == null || ActionWorkspace.IsDeleted(action))
				throw new InvalidOperationException(Tr.T("Рекламная акция удалена. Обновите страницу."));
			if (!ActionOnMassmedia.CheckLoggedUserRight(Constants.EntityActions.Delete, action))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			decimal oldPrice = action.TotalPrice;

			WindowsSource.RunInTransaction(() =>
			{
				p.Delete(silenceFlag: true);
				action.Recalculate(false);
			});
			return CampaignPart.PriceChangeText(oldPrice, action.TotalPrice);
		}

		// ---------- «Удалить рекламные выпуски» ----------

		public const string DeleteIssuesAction = Campaign.ActionNames.DeleteIssues;
		public const string PassportName = "CampaignDaysDelete";
		public const string DaysField = "days";
		public const string PositionField = "lookupCurrentPosition";
		private const string PositionsTable = "positions";

		/// <summary>
		/// Перенесено ли для объекта: линейная и модульная кампании, ролик кампании, модуль кампании, ролики спонсорской.
		/// Только по классу и строке — зовётся при построении меню.
		/// </summary>
		public static bool DeletesIssues(PresentationObject target)
		{
			if (target is Campaign campaign)
				return campaign.CampaignType == Campaign.CampaignTypes.Simple || campaign.CampaignType == Campaign.CampaignTypes.Module;
			return target is CampaignRoller || target is ModuleInCampaign || target is RollerPartOfSponsorCampaign;
		}

		/// <summary>Пункт доступен: перенесён, доступен у части (права группы, удалённая акция) и не чужая акция.</summary>
		public static bool CanDeleteIssues(PresentationObject target, ViewType view) =>
			DeletesIssues(target) && target.IsActionEnabled(DeleteIssuesAction, view) && !target.IsActionHidden(DeleteIssuesAction, view)
			&& ActionOnMassmedia.CheckLoggedUserRight(Constants.EntityActions.Delete, ActionOf(target));

		private static ActionOnMassmedia ActionOf(PresentationObject target) =>
			target is Campaign campaign ? campaign.Action : ((CampaignPart)target).Campaign.Action;

		/// <summary>Что удаляется: кампания, ролик или модуль в ней, и наборы строк паспорта.</summary>
		public sealed class Scope
		{
			internal Campaign Campaign;
			internal object ObjectId;

			/// <summary>Наборы паспорта: days и positions.</summary>
			public DataSet Data { get; internal set; }

			public string Station => Campaign.Name;
			public string CampaignTypeName => Campaign.CampaignTypeName;

			/// <summary>Узел ролика или модуля после удаления может исчезнуть — перечитывать родителя.</summary>
			public bool ReloadsParent => ObjectId != null;

			/// <summary>
			/// «Позиция:» сменили — перечитать дерево: таблица days подменяется новым экземпляром, поэтому дерево строится
			/// заново и отметки сбрасываются.
			/// </summary>
			public void Reload(object positionId)
			{
				DataSet fresh = Load(this, positionId);
				Data.Tables.Remove(DaysField);
				DataTable days = fresh.Tables[DaysField];
				fresh.Tables.Remove(days);
				Data.Tables.Add(days);
			}
		}

		public static Scope OpenIssuesDeletion(PresentationObject target)
		{
			if (!CanDeleteIssues(target, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			Campaign campaign = Campaign.GetCampaignById(((CampaignPart)target).CampaignId);
			if (campaign == null)
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));

			// Ветки DoAction частей: ролик — @objectID = rollerID, модуль — moduleID, остальное — вся кампания.
			Scope s = new Scope { Campaign = campaign };
			if (target is CampaignRoller roller)
				s.ObjectId = roller.Roller.RollerId;
			else if (target is ModuleInCampaign)
				s.ObjectId = target[Module.ParamNames.ModuleId];
			s.Data = Load(s, null);
			return s;
		}

		/// <summary>CampaignDaysForm.LoadData; подписи позиций — переведённые (значения справочника iIssuePosition).</summary>
		private static DataSet Load(Scope s, object positionId)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[Campaign.ParamNames.CampaignId] = s.Campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = (int)s.Campaign.CampaignType;
			if (s.ObjectId != null)
				procParameters[CampaignPart.OBJECT_ID] = s.ObjectId;
			if (positionId != null && positionId != DBNull.Value)
				procParameters[Issue.ParamNames.PositionId] = positionId;
			return PrepareTree(DataAccessor.LoadDataSet("CampaignDaysTreePassport", procParameters));
		}

		/// <summary>
		/// Наборы CampaignDaysTreePassport для показа: дни по порядку, выпуски внутри дня — по подписи (время выхода; в
		/// процедуре порядок внутри дня случайный), подписи позиций переведены (значения справочника iIssuePosition).
		/// Общее с «Изменить позиционирование».
		/// </summary>
		internal static DataSet PrepareTree(DataSet ds)
		{
			if (ds.Tables.Contains(DaysField))
			{
				DataTable days = ds.Tables[DaysField];
				DataTable sorted = new DataView(days) { Sort = "parentID, issueDate, name" }.ToTable(DaysField);
				ds.Tables.Remove(days);
				ds.Tables.Add(sorted);
			}
			if (ds.Tables.Contains(PositionsTable))
				foreach (DataRow row in ds.Tables[PositionsTable].Rows)
					row[Constants.Parameters.Name] = Tr.T(Convert.ToString(row[Constants.Parameters.Name]));
			return ds;
		}

		/// <summary>Отмеченные выпуски — только целые id (id дня — дата строкой); пусто — NoIssuesSelected.</summary>
		public static string Validate(IEnumerable<object> checkedIds, out List<int> issueIds)
		{
			issueIds = new List<int>();
			foreach (object id in checkedIds)
				if (id != null && int.TryParse(id.ToString(), out int issueId))
					issueIds.Add(issueId);
			return issueIds.Count == 0 ? MessageAccessor.GetMessage("NoIssuesSelected") : null;
		}

		/// <summary>Итог: удалено, не удалено, отказы (выпуск, причина) и текст о цене акции (null — пересчёта не было).</summary>
		public sealed class Result
		{
			internal Result() { }

			public WindowsSource.BatchResult Batch { get; } = new WindowsSource.BatchResult();
			public int NotDone { get; internal set; }
			public string PriceMessage { get; internal set; }
		}

		private sealed class Planned
		{
			public int Id;
			public DateTime Day;
			public string Name;
		}

		/// <summary>
		/// Запись: права и акция заново; по дням, день — одной транзакцией (блокировки окон станции доли секунды, а не всю
		/// пачку), выпуск — шаг с точкой сохранения (ModuleIssueIUD уменьшает счётчики окон до своих проверок). Взаимоблокировка
		/// повторяет день один раз; иной сбой прерывает, прежние дни остаются. Один пересчёт акции в конце, своей транзакцией.
		/// </summary>
		public static Result DeleteIssues(Scope s, IReadOnlyCollection<int> issueIds)
		{
			Campaign campaign = Campaign.GetCampaignById(s.Campaign.CampaignId);
			if (campaign == null)
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			ActionOnMassmedia action = ActionWorkspace.Load(campaign.Action.ActionId);
			if (action == null || ActionWorkspace.IsDeleted(action))
				throw new InvalidOperationException(Tr.T("Рекламная акция удалена. Обновите страницу."));
			bool module = campaign.CampaignType == Campaign.CampaignTypes.Module;
			// Модульная запись идёт мимо WebActionAuthorization (LoadDataSet), а отказ авторизации внутри шага порвал бы день.
			if (!ActionOnMassmedia.CheckLoggedUserRight(Constants.EntityActions.Delete, action)
				|| !EntityManager.GetEntity(module ? (int)Entities.ModuleIssue : (int)Entities.Issue).IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			decimal oldPrice = action.TotalPrice;

			HashSet<int> wanted = new HashSet<int>(issueIds);
			List<IGrouping<DateTime, Planned>> days = s.Data.Tables[DaysField].Rows.Cast<DataRow>()
				.Where(r => r["parentID"] != DBNull.Value && int.TryParse(Convert.ToString(r["id"]), out int id) && wanted.Contains(id))
				.Select(r => new Planned
				{
					Id = int.Parse(Convert.ToString(r["id"])),
					Day = Convert.ToDateTime(r[Issue.ParamNames.IssueDate]).Date,
					Name = Convert.ToString(r["parentID"]) + " " + Convert.ToString(r[Constants.Parameters.Name]),
				})
				.GroupBy(p => p.Day).OrderBy(g => g.Key).ToList();

			Result result = new Result();
			Stopwatch watch = Stopwatch.StartNew();
			using (OperationScope.Start($"CampaignIssuesDelete campaign={campaign.CampaignId} issues={wanted.Count}"))
			{
				for (int i = 0; i < days.Count; i++)
				{
					WindowsSource.BatchResult day = null;
					Exception failure = null;
					for (int n = 0; n < 2 && day == null && failure == null; n++)
					{
						try
						{
							day = DeleteDay(campaign.CampaignId, module, days[i]);
						}
						catch (SqlException ex) when (ex.Number == 1205 && n == 0)
						{
							Log.Warn($"Campaign issues delete: deadlock on {days[i].Key:dd.MM.yyyy} (campaign {campaign.CampaignId}), retrying the day", ex);
						}
						catch (Exception ex)
						{
							failure = ex;
						}
					}
					if (failure != null)
					{
						Log.Error($"Campaign issues delete interrupted on {days[i].Key:dd.MM.yyyy} (campaign {campaign.CampaignId})", failure);
						result.NotDone += days.Skip(i).Sum(d => d.Count());
						result.Batch.Refuse(days[i].Key.ToString("dd.MM.yyyy"), Tr.Format("Удаление прервано: {0} Выпуски этого дня не удалены, выпуски прежних дней удалены. Не обработано дней: {1}.",
							ErrorManager.GetErrorMessage(failure), days.Count - i));
						break;
					}
					result.Batch.Done += day.Done;
					result.NotDone += day.Failed;
					foreach (DataRow refused in day.Errors.Rows)
						result.Batch.Refuse(Convert.ToString(refused["name"]), Convert.ToString(refused["description"]));
				}

				if (result.Batch.Done > 0)
				{
					try
					{
						WindowsSource.RunInTransaction(() => action.Recalculate(false));
						result.PriceMessage = CampaignPart.PriceChangeText(oldPrice, action.TotalPrice);
					}
					catch (Exception ex)
					{
						Log.Error($"Campaign issues delete: recalculation failed (action {action.ActionId})", ex);
						result.Batch.Refuse("", Tr.Format("Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.", ErrorManager.GetErrorMessage(ex)));
					}
				}
			}
			Log.Info($"Campaign issues delete completed in {watch.ElapsedMilliseconds}ms: campaign={campaign.CampaignId} removed={result.Batch.Done} notRemoved={result.NotDone} days={days.Count}");
			return result;
		}

		/// <summary>День одной транзакцией: свежие строки выпусков (исчезнувший — отказ, а не ошибка 515 LogDeletedIssue), шаги.</summary>
		private static WindowsSource.BatchResult DeleteDay(int campaignId, bool module, IEnumerable<Planned> day)
		{
			WindowsSource.BatchResult work = new WindowsSource.BatchResult();
			WindowsSource.RunInTransaction(() =>
			{
				if (module)
				{
					foreach (Planned p in day.OrderBy(x => x.Id))
						RangeSource.Step(work, p.Name, 1, () => ModuleDaySource.DeleteStep(p.Id,
							row => Convert.ToInt32(row[Campaign.ParamNames.CampaignId]) != campaignId ? Tr.T("Выпуска уже нет.") : null));
					return;
				}

				List<KeyValuePair<Planned, DataRow>> rows = new List<KeyValuePair<Planned, DataRow>>();
				foreach (Planned p in day)
				{
					DataRow row = Issue.LoadRow(p.Id);
					if (row == null || Convert.ToInt32(row[Campaign.ParamNames.CampaignId]) != campaignId)
						work.Refuse(p.Name, Tr.T("Выпуска уже нет."));
					else
						rows.Add(new KeyValuePair<Planned, DataRow>(p, row));
				}
				// Порядок блокировок — по окну выхода, как у «Удалить выпуски» на вкладке «Рекламные окна».
				foreach (KeyValuePair<Planned, DataRow> x in rows.OrderBy(x => Convert.ToInt32(x.Value[Issue.ActualWindowIdParam])))
					RangeSource.Step(work, x.Key.Name, 1, () => new RollerIssue(x.Value).Delete(silenceFlag: true));
			});
			return work;
		}
	}
}
