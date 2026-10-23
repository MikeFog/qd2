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
	/// «Изменить позиционирование» (ChangePositions) у линейной и модульной кампании, ролика кампании, модуля кампании и
	/// роликов спонсорской — веб-аналог ChangePositioningForm и CampaignPart.ApplyPositionChanges: паспорт
	/// IssueChangePositioning, дерево «день → выпуски» с отбором «Показать:» по текущей позиции, «Сделать:» — новая позиция;
	/// запись — UpdateItem на выпуск (IssueIUD, у модульной — ModuleIssueIUD), один пересчёт в конце; у роликов спонсорской —
	/// ещё и после каждого выпуска (В-33: позиция меняет расход бонуса).
	///
	/// Отличия от десктопа: по дням транзакциями с точкой сохранения на выпуск (UpdateItem уменьшает счётчики окон до своих
	/// проверок; у десктопа — транзакция на выпуск); смена «Показать:» сбрасывает отметки (десктоп менял и скрытые); две
	/// проверки, которых у UpdateItem нет: окно выхода без тарифа (цена выпуска стала бы 0) и та же позиция у другого выпуска
	/// этой акции в окне (у модуля — в дне), PositionErrorForTheSameAction. Чужие акции — правило CheckLoggedUserRight по имени
	/// действия (ChangePositions в нём с 20.10.2026). Пакетные кампании — с вкладкой «Пакеты».
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public sealed class CampaignPositionChange
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(CampaignPositionChange));

		public const string ActionName = Constants.Actions.ChangePositions;
		public const string PassportName = "IssueChangePositioning";
		public const string CurrentPositionField = "lookupCurrentPosition";
		public const string FuturePositionField = "lookupFuturePosition";
		public const string DaysField = "days";
		private const string PositionErrorForTheSameAction = "PositionErrorForTheSameAction";

		private readonly Campaign campaign;
		private readonly object objectId;
		private readonly string objectName;

		private CampaignPositionChange(Campaign campaign, object objectId, string objectName)
		{
			this.campaign = campaign;
			this.objectId = objectId;
			this.objectName = objectName;
		}

		/// <summary>Наборы паспорта: days и positions.</summary>
		public DataSet Data { get; private set; }

		/// <summary>Перенесено ли для объекта — только по классу и строке (зовётся при построении меню).</summary>
		public static bool Supports(PresentationObject target)
		{
			if (target is Campaign c)
				return c.CampaignType == Campaign.CampaignTypes.Simple || c.CampaignType == Campaign.CampaignTypes.Module;
			return target is CampaignRoller || target is ModuleInCampaign || CampaignIssuesDeletion.IsSponsorPart(target, Entities.RollerPart);
		}

		/// <summary>Пункт доступен: перенесён и доступен у части (права группы, чужая и удалённая акция — CampaignPart).</summary>
		public static bool CanChange(PresentationObject target, ViewType view) =>
			Supports(target) && target.IsActionEnabled(ActionName, view) && !target.IsActionHidden(ActionName, view);

		public static CampaignPositionChange Open(PresentationObject target)
		{
			if (!CanChange(target, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			Campaign campaign = Campaign.GetCampaignById(((CampaignPart)target).CampaignId);
			if (campaign == null)
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));

			object objectId = null;
			if (target is CampaignRoller roller)
				objectId = roller.Roller.RollerId;
			else if (target is ModuleInCampaign)
				objectId = target[Module.ParamNames.ModuleId];
			CampaignPositionChange change = new CampaignPositionChange(campaign, objectId, target.Name);
			change.Data = change.Load((int)RollerPositions.Undefined);
			return change;
		}

		/// <summary>Подписи паспорта и «Показать:» = «Не определён», как OnLoad формы; «Сделать:» — первая строка списка.</summary>
		public Dictionary<string, object> PassportValues() => new Dictionary<string, object>
		{
			["radiostation"] = campaign.Name,
			["campaignType"] = Tr.T(campaign.CampaignTypeName),
			["rollerName"] = objectName,
			[CurrentPositionField] = (int)RollerPositions.Undefined,
		};

		/// <summary>«Показать:» сменили — дерево заново (новый экземпляр таблицы: отметки сбрасываются).</summary>
		public void Reload(object currentPosition)
		{
			if (currentPosition == null || currentPosition == DBNull.Value)
				return;
			DataSet fresh = Load(Convert.ToInt32(currentPosition));
			Data.Tables.Remove(DaysField);
			DataTable days = fresh.Tables[DaysField];
			fresh.Tables.Remove(days);
			Data.Tables.Add(days);
		}

		private DataSet Load(int positionId)
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = (int)campaign.CampaignType;
			if (objectId != null)
				procParameters[CampaignPart.OBJECT_ID] = objectId;
			procParameters[Issue.ParamNames.PositionId] = positionId;
			return CampaignIssuesDeletion.PrepareTree(DataAccessor.LoadDataSet("IssueChangePositioningPassport", procParameters));
		}

		/// <summary>Проверки по «ОК» в порядке десктопа: позиции разные, отмечен хотя бы один выпуск (только целые id).</summary>
		public static string Validate(object current, object future, IEnumerable<object> checkedIds, out List<int> issueIds, out int position)
		{
			issueIds = new List<int>();
			position = future == null || future == DBNull.Value ? 0 : Convert.ToInt32(future);
			if (current != null && current != DBNull.Value && Convert.ToInt32(current) == position)
				return MessageAccessor.GetMessage("CurrentAndFuturePositionsSholdBeDifferent");
			foreach (object id in checkedIds)
				if (id != null && int.TryParse(id.ToString(), out int issueId))
					issueIds.Add(issueId);
			return issueIds.Count == 0 ? MessageAccessor.GetMessage("NoIssuesSelected") : null;
		}

		/// <summary>Итог: изменено, не изменено, отказы (выпуск, причина) и текст о цене акции (null — пересчёта не было).</summary>
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
		/// Запись: акция и права заново; по дням, день — одной транзакцией, выпуск — шаг с точкой сохранения. Взаимоблокировка
		/// повторяет день один раз; иной сбой прерывает, прежние дни остаются. Один пересчёт акции в конце, своей транзакцией.
		/// </summary>
		public Result Apply(IReadOnlyCollection<int> issueIds, int position)
		{
			Campaign fresh = Campaign.GetCampaignById(campaign.CampaignId);
			if (fresh == null)
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			ActionOnMassmedia action = ActionWorkspace.Load(fresh.Action.ActionId);
			if (action == null || ActionWorkspace.IsDeleted(action))
				throw new InvalidOperationException(Tr.T("Рекламная акция удалена. Обновите страницу."));
			if (!ActionOnMassmedia.CheckLoggedUserRight(ActionName, action))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			bool module = fresh.CampaignType == Campaign.CampaignTypes.Module;
			bool perStep = WindowsSource.RecalculatesPerStep(fresh);
			decimal oldPrice = action.TotalPrice;

			HashSet<int> wanted = new HashSet<int>(issueIds);
			List<IGrouping<DateTime, Planned>> days = Data.Tables[DaysField].Rows.Cast<DataRow>()
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
			using (OperationScope.Start($"ChangePositions campaign={fresh.CampaignId} issues={wanted.Count} position={position}"))
			{
				for (int i = 0; i < days.Count; i++)
				{
					WindowsSource.BatchResult day = null;
					Exception failure = null;
					for (int n = 0; n < 2 && day == null && failure == null; n++)
					{
						try
						{
							day = WriteDay(action, fresh.CampaignId, module, perStep, days[i], position);
						}
						catch (SqlException ex) when (ex.Number == 1205 && n == 0)
						{
							Log.Warn($"Change positions: deadlock on {days[i].Key:dd.MM.yyyy} (campaign {fresh.CampaignId}), retrying the day", ex);
						}
						catch (Exception ex)
						{
							failure = ex;
						}
					}
					if (failure != null)
					{
						Log.Error($"Change positions interrupted on {days[i].Key:dd.MM.yyyy} (campaign {fresh.CampaignId})", failure);
						result.NotDone += days.Skip(i).Sum(d => d.Count());
						result.Batch.Refuse(days[i].Key.ToString("dd.MM.yyyy"), Tr.Format("Изменение позиционирования прервано: {0} Выпуски этого дня не изменены, выпуски прежних дней изменены. Не обработано дней: {1}.",
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
						Log.Error($"Change positions: recalculation failed (action {action.ActionId})", ex);
						result.Batch.Refuse("", Tr.Format("Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.", ErrorManager.GetErrorMessage(ex)));
					}
				}
			}
			Log.Info($"Change positions completed in {watch.ElapsedMilliseconds}ms: campaign={fresh.CampaignId} position={position} changed={result.Batch.Done} refused={result.NotDone} days={days.Count}");
			return result;
		}

		/// <summary>
		/// Один день. У роликов спонсорской кампании (<paramref name="perStep"/>) — пересчёт акции после каждого изменённого
		/// выпуска (В-33): позиция меняет наценку бонуса (f_GetSponsorDuration), а UpdateItem сверяет бонус с
		/// Campaign.issuesDuration, которое ведёт только пересчёт, — без него пачка превысила бы бонус.
		/// </summary>
		private static WindowsSource.BatchResult WriteDay(ActionOnMassmedia action, int campaignId, bool module, bool perStep, IEnumerable<Planned> day, int position)
		{
			int actionId = action.ActionId;
			WindowsSource.BatchResult work = new WindowsSource.BatchResult();
			// День повторяется при взаимоблокировке: цена акции в объекте не должна остаться от откатанных пересчётов на шаг.
			WindowsSource.RunInTransactionKeepingPrice(action, () =>
			{
				if (module)
				{
					foreach (Planned p in day.OrderBy(x => x.Id))
						RangeSource.Step(work, p.Name, 1, () => ModuleIssueChange.TryWritePosition(actionId, p.Id, position));
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
				// Порядок блокировок — по окну выхода, как у пачек вкладки «Рекламные окна».
				foreach (KeyValuePair<Planned, DataRow> x in rows.OrderBy(x => Convert.ToInt32(x.Value[Issue.ActualWindowIdParam])))
					if (RangeSource.Step(work, x.Key.Name, 1, () => WriteIssue(actionId, x.Value, position)) && perStep)
						action.Recalculate(false);
			});
			return work;
		}

		/// <summary>Позиция выпуска ролика; причина отказа или null.</summary>
		private static string WriteIssue(int actionId, DataRow row, int position)
		{
			// IssueIUD берёт наценки через INNER JOIN Tariff окна выхода: у окна без тарифа цена выпуска стала бы 0.
			if (row[Tariff.ParamNames.TariffId] == DBNull.Value)
				return Tr.T("У окна выхода нет тарифа: позиция не меняется, иначе цена выпуска станет нулевой.");
			int issueId = Convert.ToInt32(row[Issue.ParamNames.IssueId]);
			int windowId = Convert.ToInt32(row[Issue.ActualWindowIdParam]);
			// Условие PositionErrorForTheSameAction у AddItem; UpdateItem его не проверяет.
			if (position != 0 && TariffWindowWithRollerIssues.LoadIssues(true, windowId).Rows.Cast<DataRow>().Any(r =>
					Convert.ToInt32(r[Issue.ParamNames.IssueId]) != issueId
					&& Convert.ToInt32(r[Action.ParamNames.ActionId]) == actionId
					&& Convert.ToInt32(r[Issue.ParamNames.PositionId]) == position))
				return MessageAccessor.GetMessage(PositionErrorForTheSameAction) ?? PositionErrorForTheSameAction;

			((Issue)EntityManager.GetEntity((int)Entities.Issue).CreateObject(row)).SetPosition((RollerPositions)position);
			return null;
		}
	}
}
