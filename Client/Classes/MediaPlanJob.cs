using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes.GridExport;

namespace Merlin.Classes
{
	/// <summary>
	/// Медиаплан («График размещения») для веба: публичный вход в
	/// <see cref="MediaPlanBuilder"/> (он и Campaign — internal). Источник — то же,
	/// что у десктопных точек входа (docs/mediaplan.md, §1.1): кампании акции,
	/// одна кампания, кампания строки акта, набор акций. Разбивка (месяцы или
	/// период), настройки печати и выбранные ролики задаются при построении —
	/// после диалогов, как в десктопе.
	/// </summary>
	public sealed class MediaPlanJob
	{
		/// <summary>Разбивка медиаплана: целиком, по выбранным месяцам, за период.</summary>
		public enum Breakdown { Whole, Months, Period }

		/// <summary>Действия «График размещения» акции (Action.WinForms.cs, DoAction).</summary>
		public static readonly IReadOnlyDictionary<string, (Breakdown Breakdown, bool Selectively)> ActionActions =
			new Dictionary<string, (Breakdown, bool)>
			{
				[Action.ActionNames.PrintMediaPlan] = (Breakdown.Whole, false),
				[Action.ActionNames.PrintMediaPlanMonth] = (Breakdown.Months, false),
				[Action.ActionNames.PrintMediaPlanByPeriod] = (Breakdown.Period, false),
				[Action.ActionNames.PrintSelectivelyMediaPlan] = (Breakdown.Whole, true),
				[Action.ActionNames.PrintSelectivelyMediaPlanMonth] = (Breakdown.Months, true),
				[Action.ActionNames.PrintSelectivelyMediaPlanByPeriod] = (Breakdown.Period, true),
			};

		/// <summary>
		/// Действия «График размещения» кампаний (Campaign.WinForms.cs, DoAction): пары
		/// «X» / «XFact» — одно и то же, медиаплан всегда по фактическим окнам.
		/// </summary>
		public static readonly IReadOnlyDictionary<string, (Breakdown Breakdown, bool Selectively)> CampaignActions =
			new Dictionary<string, (Breakdown, bool)>
			{
				[Campaign.ActionNames.PrintMediaPlan] = (Breakdown.Whole, false),
				[Campaign.ActionNames.PrintMediaPlanFact] = (Breakdown.Whole, false),
				[Campaign.ActionNames.PrintMediaPlanMonth] = (Breakdown.Months, false),
				[Campaign.ActionNames.PrintMediaPlanFactMonth] = (Breakdown.Months, false),
				[Campaign.ActionNames.PrintMediaPlanByPeriod] = (Breakdown.Period, false),
				[Campaign.ActionNames.PrintMediaPlanFactByPeriod] = (Breakdown.Period, false),
				[Campaign.ActionNames.PrintSelectivelyMediaPlan] = (Breakdown.Whole, true),
				[Campaign.ActionNames.PrintSelectivelyMediaPlanFact] = (Breakdown.Whole, true),
				[Campaign.ActionNames.PrintSelectivelyMediaPlanMonth] = (Breakdown.Months, true),
				[Campaign.ActionNames.PrintSelectivelyMediaPlanFactMonth] = (Breakdown.Months, true),
				[Campaign.ActionNames.PrintSelectivelyMediaPlanByPeriod] = (Breakdown.Period, true),
				[Campaign.ActionNames.PrintSelectivelyMediaPlanFactByPeriod] = (Breakdown.Period, true),
			};

		/// <summary>«Распечатать график размещения» у строки акта (ActJournalRow.WinForms.cs).</summary>
		public const string ActJournalRowAction = Campaign.ActionNames.PrintMediaPlan;

		private readonly IList<Campaign> _campaigns;
		private readonly IList<Action> _actions;
		private readonly int? _actionId;
		private readonly int? _campaignId;

		/// <summary>Выборочная печать по роликам («Распечатать частичный»).</summary>
		public bool Selectively { get; }

		/// <summary>Период по умолчанию для «по периоду» — даты акции или кампании.</summary>
		public DateTime PeriodStart { get; }
		public DateTime PeriodFinish { get; }

		private MediaPlanJob(IList<Campaign> campaigns, IList<Action> actions, int? actionId, int? campaignId,
			bool selectively, DateTime periodStart, DateTime periodFinish)
		{
			_campaigns = campaigns;
			_actions = actions;
			_actionId = actionId;
			_campaignId = campaignId;
			Selectively = selectively;
			PeriodStart = periodStart;
			PeriodFinish = periodFinish;
		}

		/// <summary>«График размещения» акции: лист на каждую кампанию (Action.PrintMediaPlan).</summary>
		public static MediaPlanJob ForAction(Action action, bool selectively)
		{
			action.Refresh();
			return new MediaPlanJob(Action.GetCampaigns(action.Campaigns()), null, action.ActionId, null,
				selectively, action.StartDate, action.FinishDate);
		}

		/// <summary>«График размещения» кампании любого вида (Campaign.PrintMediaPlan).</summary>
		public static MediaPlanJob ForCampaign(PresentationObject campaign, bool selectively)
		{
			var c = (Campaign)campaign;
			c.Refresh();
			return new MediaPlanJob(new List<Campaign> { c }, null, null, c.CampaignId,
				selectively, c.StartDate, c.FinishDate);
		}

		/// <summary>Строка акта выполненных работ — медиаплан её кампании (ActJournalRow.DoAction).</summary>
		public static MediaPlanJob ForActJournalRow(PresentationObject row)
		{
			int campaignId = ParseHelper.ParseToInt32(row[Campaign.ParamNames.CampaignId].ToString());
			return ForCampaign(Campaign.GetCampaignById(campaignId), false);
		}

		/// <summary>Сводный план по набору акций (MDIForm.PrintMultiActionMediaPlan).</summary>
		public static MediaPlanJob ForActions(IEnumerable<int> actionIds)
		{
			IList<Action> actions = actionIds.Select(id => (Action)ActionOnMassmedia.GetActionById(id)).ToList();
			return new MediaPlanJob(null, actions, null, null, false, DateTime.Today, DateTime.Today);
		}

		/// <summary>Месяцы с выпусками — для «по месяцам» (как FrmMonths в десктопе).</summary>
		public IList<DateTime> AvailableMonths()
		{
			Dictionary<string, object> ps = DataAccessor.CreateParametersDictionary();
			if (_campaignId.HasValue)
				ps[Campaign.ParamNames.CampaignId] = _campaignId.Value;
			else
				ps[Action.ParamNames.ActionId] = _actionId.Value;
			ps["isFact"] = true;
			var months = new List<DateTime>();
			foreach (DataRow row in DataAccessor.LoadDataSet("GetMonthes", ps).Tables[0].Rows)
			{
				int month = ParseHelper.ParseToInt32(row["MonthDate"].ToString(), -1);
				int year = ParseHelper.ParseToInt32(row["MonthYear"].ToString(), -1);
				if (month > 0 && year > 0)
					months.Add(new DateTime(year, month, 1));
			}
			return months;
		}

		/// <summary>Ролики для выборочной печати: rollerID, name (по разбивке).</summary>
		public DataTable Rollers(IList<DateTime> months, DateTime? from, DateTime? to) =>
			CreateBuilder(months, from, to).GetRollers();

		/// <summary>Медиаплан файлом .xlsx; null — выпусков нет, печатать нечего.</summary>
		/// <param name="selectedRollers">Для выборочной печати — «id,id,»; иначе null.</param>
		public ExportFile Build(PrintSettings settings, IList<DateTime> months, DateTime? from, DateTime? to,
			string selectedRollers)
		{
			MediaPlanBuilder builder = CreateBuilder(months, from, to);
			builder.Settings = settings;
			builder.SelectedRollers = selectedRollers;
			var document = new OpenXmlExportDocument();
			if (!builder.Build(document))
				return null;
			return new ExportFile { Name = builder.FileName, Content = document.ToArray() };
		}

		private MediaPlanBuilder CreateBuilder(IList<DateTime> months, DateTime? from, DateTime? to) =>
			new MediaPlanBuilder(null, _campaigns, months, from, to, Selectively, _actions);

		#region Сводный план по нескольким акциям

		/// <summary>
		/// Номера акций из строки через запятую (пробел, точку с запятой): без
		/// повторов, по возрастанию — как FrmMultiActionMediaPlan.
		/// </summary>
		public static IList<int> ParseActionIds(string text)
		{
			var ids = new SortedSet<int>();
			foreach (string token in (text ?? string.Empty).Split(new[] { ',', ';', ' ', '\t', '\r', '\n' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				if (int.TryParse(token.Trim(), out int id) && id > 0)
					ids.Add(id);
			}
			return ids.ToList();
		}

		/// <summary>Номера, которых нет среди существующих (не удалённых) акций.</summary>
		public static IList<int> FindMissingActions(IList<int> ids)
		{
			Dictionary<string, object> ps = DataAccessor.CreateParametersDictionary();
			ps["actionIDString"] = string.Join(",", ids) + ",";
			var existing = new HashSet<int>();
			foreach (DataRow row in DataAccessor.LoadDataSet("MultiActionMediaPlanActions", ps).Tables[0].Rows)
				existing.Add(int.Parse(row["actionID"].ToString()));
			return ids.Where(id => !existing.Contains(id)).ToList();
		}

		/// <summary>
		/// Акции для выбора галочками (FrmActionsSelector): Actions1 по отбору сущности
		/// «Рекламная акция», подтверждённые и макеты вместе; права менеджера проверяет
		/// процедура по @loggedUserID.
		/// </summary>
		public static DataTable LoadActionsForSelection(IDictionary<string, object> filter)
		{
			Entity entity = EntityManager.GetEntity((int)Entities.Action);
			Dictionary<string, object> ps = DataAccessor.PrepareParameters(entity);
			foreach (KeyValuePair<string, object> kvp in filter)
				ps[kvp.Key] = kvp.Value;
			ps["isShowActivate"] = true;
			ps["isShowNotActivate"] = true;
			return ((DataSet)DataAccessor.DoAction(ps)).Tables[Constants.TableNames.Data];
		}

		#endregion
	}
}
