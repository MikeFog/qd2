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
	/// Вкладка «Модули» страницы акции (docs/tasks/web-action-forms.md, этап 6) — то, что в десктопе делают
	/// ComboModulePlacementForm и ComboModuleGrid при редактировании готовой акции (режим 2): строка — модуль ×
	/// модульная кампания, колонка — день недели или месяца, ячейка — остаток самого заполненного окна модуля за день.
	///
	/// Чтение — процедуры десктопа: ComboModuleIssuesRetrieve (размещённые модули, отметки и счётчики дней), ModuleList
	/// (модули станции у кампании без выпусков), ComboModuleFreeTimeRetrieve с набором @moduleIDs (Deploy/50); панель —
	/// ModuleIssueRetrieve, ModuleIssueContentRetrieve (окна выхода выпуска) и TariffWindowRetrieve (окна тарифов
	/// модуля). Окно выхода — фактическое (правило 07.10.2026, docs/window-original-actual.md).
	///
	/// Запись (подэтап 6.2) — клик по месту, как клик комбо: IsModuleExist, ModuleIssueIUD AddItem и ActionRecalculate одной
	/// транзакцией. Права проверяет фасад перед записью: процедуры о «своей / чужой» акции не знают.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class ModuleDaySource
	{
		// ---------- Кампании вкладки ----------

		/// <summary>Модульная кампания — её размещение показывает вкладка «Модули».</summary>
		public static bool IsModular(DataRow campaign) =>
			ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.CampaignTypeId], 0) == (int)Campaign.CampaignTypes.Module;

		/// <summary>Модульная кампания вкладки: станция, тип оплаты и агентство — для строк и их подписей.</summary>
		internal sealed class CampaignInfo
		{
			internal int MassmediaId { get; set; }

			/// <summary>Радиостанция с группой — колонка «Радиостанция».</summary>
			internal string Station { get; set; }

			internal string PaymentTypeName { get; set; }
			internal string AgencyName { get; set; }
		}

		/// <summary>
		/// Отмеченные модульные кампании акции, с которыми работает вкладка. Номер строки (модуль, кампания) выдаётся один
		/// раз за жизнь объекта и не меняется — ключ места = день × 100 000 + номер строки (ключ «день × moduleID» дал бы
		/// одно место двум кампаниям одного модуля).
		/// </summary>
		public sealed class ModuleScope : WindowsSource.IBrushScope
		{
			internal ModuleScope() { }

			public int ActionId { get; internal set; }
			public int FirmId { get; internal set; }

			/// <summary>Можно ли менять размещение: «Редактировать» у акции и у модульных кампаний (на момент открытия).</summary>
			public bool CanEdit { get; internal set; }

			public DataTable Rollers { get; set; }

			/// <summary>Отмеченные модульные кампании — в порядке, в котором их передал экран.</summary>
			public IReadOnlyList<int> CampaignIds { get; internal set; }

			/// <summary>День самого раннего выпуска модуля отмеченных кампаний; null — выпусков нет. С него открывается сетка.</summary>
			public DateTime? FirstIssueDate { get; internal set; }

			internal IReadOnlyDictionary<int, CampaignInfo> Campaigns { get; set; }

			private readonly Dictionary<(int ModuleId, int CampaignId), int> _rowNumbers = new Dictionary<(int, int), int>();

			/// <summary>Строки (модуль, кампания), которые набор уже показывал, — им выдан номер.</summary>
			internal IEnumerable<(int ModuleId, int CampaignId)> ShownRows => _rowNumbers.Keys;

			/// <summary>Номер строки (модуль, кампания): выдан раз — тот же при любом периоде и фильтре.</summary>
			internal int RowNumber(int moduleId, int campaignId)
			{
				if (!_rowNumbers.TryGetValue((moduleId, campaignId), out int number))
				{
					number = _rowNumbers.Count + 1;
					_rowNumbers.Add((moduleId, campaignId), number);
				}
				return number;
			}
		}

		/// <summary>
		/// Отмеченные кампании: из <paramref name="campaignIds"/> остаются только модульные кампании этой акции (чужие,
		/// не модульные и удалённые отбрасываются, повторы — тоже). Пустой набор — вкладке показывать нечего.
		/// </summary>
		public static ModuleScope Open(ActionOnMassmedia action, IEnumerable<int> campaignIds)
		{
			Dictionary<int, DataRow> rows = new Dictionary<int, DataRow>();
			foreach (DataRow row in ActionWorkspace.LoadCampaigns(action).Rows)
				if (IsModular(row))
					rows[WindowsSource.CampaignIdOf(row)] = row;

			List<int> ids = new List<int>();
			Dictionary<int, CampaignInfo> campaigns = new Dictionary<int, CampaignInfo>();
			foreach (int id in campaignIds)
				if (rows.TryGetValue(id, out DataRow row) && !campaigns.ContainsKey(id))
				{
					ids.Add(id);
					campaigns[id] = new CampaignInfo
					{
						MassmediaId = ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.MassmediaId], 0),
						Station = StationOf(row),
						PaymentTypeName = Convert.ToString(row[PaymentTypeNameColumn]),
						AgencyName = Convert.ToString(row[AgencyNameColumn]).Trim(),
					};
				}

			// Не Campaign.startDate: он считается по строкам Issue (окнам), а день модуля — ModuleIssue.issueDate.
			DateTime? first = null;
			if (ids.Count > 0)
				foreach (DataRow issue in ComboModule.LoadIssues(action.ActionId).Rows)
					if (campaigns.ContainsKey(IssueCampaignId(issue)))
					{
						DateTime day = IssueDay(issue);
						if (first == null || day < first)
							first = day;
					}

			return new ModuleScope
			{
				ActionId = action.ActionId,
				FirmId = action.FirmID,
				CanEdit = ids.Count > 0 && ActionWorkspace.CanEdit(action) && CampaignRight(ids[0]),
				CampaignIds = ids,
				FirstIssueDate = first,
				Campaigns = campaigns,
			};
		}

		/// <summary>
		/// «Редактировать» модульной кампании (сущность 92), как у RangeSource.CampaignRight: правило своих / чужих акций
		/// плюс права группы на сущность — от строки кампании не зависят, поэтому читается одна кампания.
		/// </summary>
		private static bool CampaignRight(int campaignId)
		{
			Campaign campaign = Campaign.GetCampaignById(campaignId);
			return campaign != null && campaign.IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal);
		}

		private const string PaymentTypeNameColumn = "paymentTypeName";

		private const string AgencyNameColumn = "agencyName";

		/// <summary>Радиостанция с группой без хвостовых пробелов (в базе бывают: «Бизнес ФМ »).</summary>
		private static string StationOf(DataRow campaign)
		{
			string name = Convert.ToString(campaign[Campaign.ParamNames.MassmediaName]).Trim();
			string group = Convert.ToString(campaign[Campaign.ParamNames.GroupName]).Trim();
			return group.Length == 0 ? name : string.Format("{0} ({1})", name, group);
		}

		private static int IssueCampaignId(DataRow issue) => Convert.ToInt32(issue[Campaign.ParamNames.CampaignId]);

		private static int IssueModuleId(DataRow issue) => Convert.ToInt32(issue[ComboModule.ParamNames.ModuleId]);

		private static DateTime IssueDay(DataRow issue) => Convert.ToDateTime(issue[ComboModule.ParamNames.IssueDate]).Date;

		// ---------- Период ----------

		/// <summary>Модуль и кампания места за день — то, что нужно панели и записи (у PlacementCell — internal).</summary>
		internal sealed class ModuleDay
		{
			internal int ModuleId { get; set; }
			internal int CampaignId { get; set; }
			internal int MassmediaId { get; set; }
			internal int ModulePricelistId { get; set; }

			/// <summary>Прайс-лист станции, к которому относится прайс-лист модуля (TariffWindowRetrieve @pricelistId).</summary>
			internal int PricelistId { get; set; }

			/// <summary>Цена прайс-листа модуля, ₽ за секунду ролика.</summary>
			internal decimal Price { get; set; }

			internal string Station { get; set; }
			internal string ModuleName { get; set; }
			internal string PaymentType { get; set; }
		}

		/// <summary>Строка периода до номера и мест.</summary>
		private sealed class RowSpec
		{
			internal int ModuleId { get; set; }
			internal string ModuleName { get; set; }
			internal int CampaignId { get; set; }
			internal CampaignInfo Campaign { get; set; }
			internal string PaymentType { get; set; }
		}

		private static readonly DateTime KeyEpoch = new DateTime(2000, 1, 1);

		/// <summary>Ключ места: номер дня от 2000-01-01 × 100 000 + номер строки.</summary>
		private static int CellKey(DateTime day, int rowNumber) => (day - KeyEpoch).Days * 100000 + rowNumber;

		/// <summary>
		/// Неделя (пн–вс) или месяц (1-е – последнее число), содержащие <paramref name="anyDate"/>. Строки — модуль ×
		/// кампания: размещённые в отмеченных кампаниях за всё время (как десктопный режим 2 — модули с выпусками акции),
		/// у отмеченной кампании без выпусков — модули её станции, действующие в периоде (ModuleList). Порядок —
		/// радиостанция, модуль, тип оплаты; полоса меняется со станцией. Место есть, если модуль в этот день выходит
		/// целиком и ни одно его окно не отключено (ComboModuleFreeTimeRetrieve).
		/// </summary>
		public static PlacementWeek LoadPeriod(ModuleScope s, DateTime anyDate, PlacementPeriod period, WindowsSource.WeekView view)
		{
			DateTime date = anyDate.Date;
			DateTime start = period == PlacementPeriod.Month
				? new DateTime(date.Year, date.Month, 1)
				: date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
			DateTime finish = period == PlacementPeriod.Month ? start.AddMonths(1).AddDays(-1) : start.AddDays(PlacementWeek.DaysInWeek - 1);
			int dayCount = (finish - start).Days + 1;

			PlacementWeek week = new PlacementWeek(period, dayCount)
			{
				FirstDay = start,
				StartDate = start,
				FinishDate = finish,
				HasPrices = false,
				HasPricelist = false,
				HeaderColumns = new[] { Tr.T("Радиостанция"), Tr.T("Модуль"), Tr.T("Тип оплаты") },
			};

			// Выпуски модулей отмеченных кампаний: строки, отметка «свой» и число выпусков за день.
			List<DataRow> issues = new List<DataRow>();
			if (s.CampaignIds.Count > 0)
				foreach (DataRow issue in ComboModule.LoadIssues(s.ActionId).Rows)
					if (s.Campaigns.ContainsKey(IssueCampaignId(issue)))
						issues.Add(issue);

			List<RowSpec> specs = RowSpecs(s, issues, start, finish);
			Dictionary<(int, DateTime), DataRow> free = specs.Count > 0
				? FreeTimeByModuleAndDay(specs.Select(r => r.ModuleId), start, finish, view)
				: new Dictionary<(int, DateTime), DataRow>();

			Dictionary<(int, int, DateTime), List<int>> own = new Dictionary<(int, int, DateTime), List<int>>();
			for (int d = 0; d < dayCount; d++)
				week.DayTotals[d] = 0;
			foreach (DataRow issue in issues)
			{
				DateTime day = IssueDay(issue);
				if (day < start || day > finish)
					continue;
				var key = (IssueModuleId(issue), IssueCampaignId(issue), day);
				if (!own.TryGetValue(key, out List<int> rollers))
					own[key] = rollers = new List<int>();
				rollers.Add(ParseHelper.GetInt32FromObject(issue[Roller.ParamNames.RollerId], 0));
				// Как ComboModulePlacementForm.ShowIssuesCount: выпуски модулей за день, повтор модуля в день — два.
				week.DayTotals[(day - start).Days]++;
			}

			bool filtered = view.Position != 0 || view.AdvertTypeId.HasValue;
			List<PlacementRow> rows = new List<PlacementRow>(specs.Count);
			int band = 1;
			int lastMassmediaId = -1;
			foreach (RowSpec spec in specs)
			{
				PlacementRow row = new PlacementRow(new[] { spec.Campaign.Station, spec.ModuleName, spec.PaymentType }, dayCount);
				if (spec.Campaign.MassmediaId != lastMassmediaId)
				{
					band = 1 - band;
					lastMassmediaId = spec.Campaign.MassmediaId;
				}
				row.Band = band;

				int number = s.RowNumber(spec.ModuleId, spec.CampaignId);
				for (int d = 0; d < dayCount; d++)
				{
					DateTime day = start.AddDays(d);
					if (!free.TryGetValue((spec.ModuleId, day), out DataRow f))
						continue;
					own.TryGetValue((spec.ModuleId, spec.CampaignId, day), out List<int> rollers);

					PlacementFlags flags = PlacementFlags.None;
					if (rollers != null)
						flags |= PlacementFlags.Mine;
					// Жирный — только при выбранном фильтре: без него процедура отдаёт 1/1 (ComboModuleGrid.MarkFilteredCells).
					if (filtered && IntOrZero(f[ComboModule.ParamNames.PositionFree]) == 1 && IntOrZero(f[ComboModule.ParamNames.AdvertTypeFree]) == 1)
						flags |= PlacementFlags.Match;

					row.Cells[d] = new PlacementCell
					{
						Key = CellKey(day, number),
						Date = day,
						DayIndex = d,
						Text = CellText(f),
						Flags = flags,
						OwnRollerIds = rollers ?? (IReadOnlyList<int>)Array.Empty<int>(),
						ModuleDay = new ModuleDay
						{
							ModuleId = spec.ModuleId,
							CampaignId = spec.CampaignId,
							MassmediaId = Convert.ToInt32(f[ComboModule.ParamNames.MassmediaId]),
							ModulePricelistId = Convert.ToInt32(f[ComboModule.ParamNames.ModulePriceListId]),
							PricelistId = Convert.ToInt32(f[Pricelist.ParamNames.PricelistId]),
							Price = Convert.ToDecimal(f[ComboModule.ParamNames.Price]),
							Station = spec.Campaign.Station,
							ModuleName = spec.ModuleName,
							PaymentType = spec.PaymentType,
						},
					};
				}
				rows.Add(row);
			}
			week.Rows = rows;

			if (rows.Count == 0)
			{
				week.EmptyTitle = Tr.T("Модулей нет");
				week.EmptyHint = Tr.T("Строки — модули, размещённые в отмеченных модульных кампаниях; у кампании без выпусков — модули её радиостанции, действующие в этом периоде.");
			}
			return week;
		}

		/// <summary>
		/// Строки периода: размещённые пары (модуль, кампания) и модули станции у кампаний без выпусков; порядок и подпись
		/// типа оплаты — как ComboModulePlacementForm.BuildGridRows (при одинаковом типе оплаты у двух строк модуля —
		/// с агентством).
		/// </summary>
		private static List<RowSpec> RowSpecs(ModuleScope s, List<DataRow> issues, DateTime start, DateTime finish)
		{
			List<RowSpec> specs = new List<RowSpec>();
			HashSet<(int, int)> added = new HashSet<(int, int)>();
			foreach (DataRow issue in issues)
			{
				int moduleId = IssueModuleId(issue);
				int campaignId = IssueCampaignId(issue);
				if (added.Add((moduleId, campaignId)))
					specs.Add(new RowSpec
					{
						ModuleId = moduleId,
						ModuleName = Convert.ToString(issue[ComboModule.ParamNames.ModuleName]),
						CampaignId = campaignId,
						Campaign = s.Campaigns[campaignId],
					});
			}

			// Кампания без выпусков — модули станции (ModuleList на начало периода: ближайший прайс-лист модуля кончается не
			// раньше него), из них — с прайс-листом, начавшимся не позже конца периода, то есть пересекающим период. На
			// «сегодня», как CampaignForm, будущий период показал бы модули, чей прайс-лист до него уже кончился.
			DateTime theDate = start;
			Dictionary<int, DataTable> modulesOf = new Dictionary<int, DataTable>();
			foreach (int campaignId in s.CampaignIds)
			{
				if (specs.Any(r => r.CampaignId == campaignId))
					continue;
				CampaignInfo campaign = s.Campaigns[campaignId];
				if (!modulesOf.TryGetValue(campaign.MassmediaId, out DataTable modules))
					modulesOf[campaign.MassmediaId] = modules = LoadStationModules(campaign.MassmediaId, theDate);
				foreach (DataRow module in modules.Rows)
					if (Convert.ToDateTime(module[ClosestPricelistStartColumn]).Date <= finish)
						specs.Add(new RowSpec
						{
							ModuleId = Convert.ToInt32(module[Module.ParamNames.ModuleId]),
							ModuleName = Convert.ToString(module[Constants.Parameters.Name]),
							CampaignId = campaignId,
							Campaign = campaign,
						});
			}

			// Строки, которые набор уже показывал, остаются, пока модуль действует в периоде: иначе после первой постановки в
			// кампанию без выпусков остальные модули её станции пропали бы, строки ниже сдвинулись бы и следующий щелчок попал
			// бы в чужую строку. Память — на время жизни набора (вкладку открыли заново — заново).
			foreach (IGrouping<int, (int ModuleId, int CampaignId)> shown in s.ShownRows.Where(r => s.CampaignIds.Contains(r.CampaignId)).GroupBy(r => r.CampaignId).ToList())
			{
				List<int> missing = shown.Select(r => r.ModuleId).Where(id => !specs.Any(r => r.ModuleId == id && r.CampaignId == shown.Key)).ToList();
				if (missing.Count == 0)
					continue;
				CampaignInfo campaign = s.Campaigns[shown.Key];
				if (!modulesOf.TryGetValue(campaign.MassmediaId, out DataTable modules))
					modulesOf[campaign.MassmediaId] = modules = LoadStationModules(campaign.MassmediaId, theDate);
				foreach (DataRow module in modules.Rows)
				{
					int moduleId = Convert.ToInt32(module[Module.ParamNames.ModuleId]);
					if (missing.Contains(moduleId) && Convert.ToDateTime(module[ClosestPricelistStartColumn]).Date <= finish)
						specs.Add(new RowSpec
						{
							ModuleId = moduleId,
							ModuleName = Convert.ToString(module[Constants.Parameters.Name]),
							CampaignId = shown.Key,
							Campaign = campaign,
						});
				}
			}

			foreach (RowSpec spec in specs)
			{
				spec.PaymentType = spec.Campaign.PaymentTypeName;
				bool twin = specs.Any(o => o != spec && o.ModuleId == spec.ModuleId && o.Campaign.PaymentTypeName == spec.Campaign.PaymentTypeName);
				if (twin && spec.Campaign.AgencyName.Length > 0)
					spec.PaymentType += ", " + spec.Campaign.AgencyName;
			}

			StringComparer text = StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true);
			return specs.OrderBy(r => r.Campaign.Station, text).ThenBy(r => r.ModuleName, text).ThenBy(r => r.PaymentType, text).ToList();
		}

		private const string ClosestPricelistStartColumn = "closestPriceListStartDate";

		/// <summary>ModuleList @massmediaID, @theDate: модули станции с прайс-листом, не закончившимся к дате.</summary>
		private static DataTable LoadStationModules(int massmediaId, DateTime theDate)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Massmedia.ParamNames.MassmediaId] = massmediaId;
			parameters["theDate"] = theDate;
			return DataAccessor.LoadDataSet("ModuleList", parameters).Tables[0];
		}

		/// <summary>
		/// ComboModuleFreeTimeRetrieve по набору модулей (@moduleIDs) за период: строка на (модуль, день), только за дни,
		/// когда модуль есть целиком. База без Deploy/50 параметр молча отбросила бы (DataAccessor берёт только известные
		/// процедуре имена) и вернула бы пустой набор — такую выдачу узнаём по отсутствию колонки massmediaID.
		/// </summary>
		private static Dictionary<(int, DateTime), DataRow> FreeTimeByModuleAndDay(IEnumerable<int> moduleIds, DateTime start, DateTime finish,
			WindowsSource.WeekView view)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["moduleIDs"] = string.Join(",", moduleIds.Distinct().Select(id => id.ToString(CultureInfo.InvariantCulture)));
			parameters[Pricelist.ParamNames.StartDate] = start;
			parameters[Pricelist.ParamNames.FinishDate] = finish;
			parameters["showUnconfirmed"] = view.ShowUnconfirmed;
			parameters[Issue.ParamNames.PositionId] = view.Position;
			AdvertTypePresences presence = AdvertTypePresences.Undefined;
			if (view.AdvertTypeId.HasValue)
			{
				parameters["advertTypeId"] = view.AdvertTypeId.Value;
				presence = view.AdvertTypeExists ? AdvertTypePresences.Exist : AdvertTypePresences.NotExist;
			}
			parameters["advertTypePresence"] = (int)presence;

			DataTable data = DataAccessor.LoadDataSet("ComboModuleFreeTimeRetrieve", parameters).Tables[0];
			if (!data.Columns.Contains(ComboModule.ParamNames.MassmediaId) || !data.Columns.Contains(Pricelist.ParamNames.PricelistId))
				throw new InvalidOperationException(Tr.T("Процедура ComboModuleFreeTimeRetrieve в базе старая: нет набора модулей @moduleIDs. Нужно установить Deploy/50."));

			// Ключ — (модуль, день), как ComboModuleGrid.FillFreeTime: при двух строках дня остаётся последняя.
			Dictionary<(int, DateTime), DataRow> result = new Dictionary<(int, DateTime), DataRow>();
			foreach (DataRow row in data.Rows)
				result[(Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]), Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]).Date)] = row;
			return result;
		}

		/// <summary>Текст места — как ComboModuleDay.CellText: «мм:сс» (отрицательный — «-мм:сс»), у штучных ещё «[осталось/всего]».</summary>
		private static string CellText(DataRow f)
		{
			object freeTime = f[ComboModule.ParamNames.FreeTime];
			string text = freeTime == DBNull.Value ? string.Empty : DateTimeUtils.Time2String(Convert.ToInt32(freeTime));
			object freeCapacity = f[ComboModule.ParamNames.FreeCapacity];
			if (freeCapacity != DBNull.Value)
				text = string.Format("{0} [{1}/{2}]", text, Convert.ToInt32(freeCapacity), f[ComboModule.ParamNames.MaxCapacity]).TrimStart();
			return text;
		}

		private static int IntOrZero(object value) => value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);

		// ---------- Постановка ----------

		/// <summary>
		/// Свежая акция для записи: удалённая — отказ; без «Редактировать» акции (на сейчас) или модульных кампаний (на момент
		/// открытия: правило своих / чужих акций у кампании то же, что у акции, а права группы от строки кампании не зависят,
		/// как у RangeSource.LoadForWrite) — отказ.
		/// </summary>
		internal static ActionOnMassmedia LoadForWrite(ModuleScope s)
		{
			ActionOnMassmedia action = ActionWorkspace.Load(s.ActionId);
			if (action == null || ActionWorkspace.IsDeleted(action))
				throw new InvalidOperationException(Tr.T("Рекламная акция удалена. Обновите страницу."));
			if (!s.CanEdit || !ActionWorkspace.CanEdit(action))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			return action;
		}

		/// <summary>Выпуск модуля, поставленный кликом, — номер из ответа ModuleIssueIUD, захваченный в момент записи.</summary>
		public sealed class PlacedModuleIssue
		{
			internal PlacedModuleIssue() { }

			public int ModuleIssueId { get; internal set; }
			public int CampaignId { get; internal set; }
			public int ModuleId { get; internal set; }
			public int RollerId { get; internal set; }

			/// <summary>День выпуска (ModuleIssue.issueDate).</summary>
			public DateTime Date { get; internal set; }

			public string ModuleName { get; internal set; }

			/// <summary>Для строки отказа: «дата — модуль — ролик».</summary>
			public string Name { get; internal set; }
		}

		/// <summary>
		/// Клик «Режима добавления» — как клик комбо (ComboModulePlacementForm.AddModuleIssue → Campaign.AddModuleIssue):
		/// модуль строки места на весь день, ролик и позиция кисти. Одна транзакция: IsModuleExist, ModuleIssueIUD AddItem
		/// (строки Issue во все окна модуля за день) и ActionRecalculate. Акция и её кампании читаются заново перед записью:
		/// коррекция платежа подтверждённой акции сравнивает её прежнюю цену с новой (ActionOnMassmedia.Recalculate), а кампанию
		/// могли удалить. Кампания — строкой списка кампаний акции, как в веере: чтение объекта кампании и его прав стоит ~0,2 с.
		/// Повтор того же модуля в тот же день ставит ещё один выпуск, как в десктопе (В-18). Отказ по бизнес-правилу
		/// (RAISERROR с ключом iMessage) откатывает всё и уходит наверх как есть: текст — ErrorManager.GetErrorMessage(ex).
		/// Модель периода после записи перечитывает экран (LoadPeriod): изменились остатки окон и у соседних модулей
		/// с общими тарифами, отметки и итог дня.
		/// </summary>
		public static PlacedModuleIssue PlaceOne(ModuleScope s, PlacementCell cell, int rollerId, int position)
		{
			ModuleDay m = cell?.ModuleDay;
			if (m == null || !s.Campaigns.ContainsKey(m.CampaignId))
				throw new InvalidOperationException(Tr.T("Место не относится к отмеченным модульным кампаниям. Обновите страницу."));
			DateTime day = cell.Date.Date;

			ActionOnMassmedia action = LoadForWrite(s);
			int massmediaId = ModularCampaignStation(action, m.CampaignId);
			Roller roller = ModuleRoller(s, rollerId, action.FirmID);
			if (!IsPricelistOfDay(m, massmediaId, day))
				throw new InvalidOperationException(Tr.Format("Прайс-лист модуля «{0}» на {1:dd.MM.yyyy} изменился. Обновите страницу.", m.ModuleName, day));

			int moduleIssueId = 0;
			WindowsSource.RunInTransaction(() =>
			{
				if (!IsModuleWhole(m.ModulePricelistId, day))
					throw NotWhole(m, day);
				moduleIssueId = AddModuleIssue(action, m, roller, position, day);
				if (moduleIssueId <= 0)
					throw NotWhole(m, day);
				action.Recalculate(false);
			});

			return new PlacedModuleIssue
			{
				ModuleIssueId = moduleIssueId,
				CampaignId = m.CampaignId,
				ModuleId = m.ModuleId,
				RollerId = roller.RollerId,
				Date = day,
				ModuleName = m.ModuleName,
				Name = string.Format("{0:dd.MM.yyyy} — {1} — {2}", day, m.ModuleName, roller.Name),
			};
		}

		/// <summary>Станция модульной кампании места по свежему списку кампаний акции; кампании в нём нет — её удалили.</summary>
		private static int ModularCampaignStation(ActionOnMassmedia action, int campaignId)
		{
			foreach (DataRow row in ActionWorkspace.LoadCampaigns(action).Rows)
				if (WindowsSource.CampaignIdOf(row) == campaignId && IsModular(row))
					return ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.MassmediaId], 0);
			throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
		}

		/// <summary>Типы роликов, которые ставятся в модули (iRollerActionType): 1 — Реклама, 2 — Новость, 3 — Программа.</summary>
		private const int FirstModuleRollerType = 1;
		private const int LastModuleRollerType = 3;

		/// <summary>
		/// Ролик кисти — из показанного списка роликов фирмы акции (WindowsSource.BrushRoller) и только типов 1–3 (В-25):
		/// у модулей не работают проверки, привязанные к окну (СМИ, агитация, отбивки).
		/// </summary>
		private static Roller ModuleRoller(ModuleScope s, int rollerId, int actionFirmId)
		{
			Roller roller = WindowsSource.BrushRoller(s, rollerId, actionFirmId);
			int type = ParseHelper.GetInt32FromObject(roller[Roller.ParamNames.RolActionTypeID], 0);
			if (type < FirstModuleRollerType || type > LastModuleRollerType)
				throw new InvalidOperationException(Tr.Format("Ролик «{0}» нельзя поставить в модуль: в модули ставятся только ролики типов «Реклама», «Новость» и «Программа».", roller.Name));
			return roller;
		}

		/// <summary>
		/// Прайс-лист модуля места — этого модуля, действует в этот день и входит в прайс-лист станции кампании
		/// (ModulePricelistByDate @massmediaID кампании): место могло устареть, а hlp_IssueVerify этого не проверяет.
		/// </summary>
		private static bool IsPricelistOfDay(ModuleDay m, int massmediaId, DateTime day)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Massmedia.ParamNames.MassmediaId] = massmediaId;
			parameters["theDate"] = day;
			parameters[Module.ParamNames.ModuleId] = m.ModuleId;
			foreach (DataRow row in DataAccessor.LoadDataSet("ModulePricelistByDate", parameters).Tables[0].Rows)
				if (Convert.ToInt32(row[ModulePricelist.ParamNames.ModulePriceListID]) == m.ModulePricelistId)
					return true;
			return false;
		}

		/// <summary>
		/// IsModuleExist: у каждого тарифа прайс-листа модуля есть окно в этот день. LoadDataSet, а не ExecuteScalar
		/// (Campaign.IsModuleExists): внутри транзакции он идёт на её соединении, ExecuteScalar открыл бы своё.
		/// </summary>
		private static bool IsModuleWhole(int modulePricelistId, DateTime day)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[ModulePricelist.ParamNames.ModulePriceListID] = modulePricelistId;
			parameters["date"] = day;
			DataTable result = DataAccessor.LoadDataSet("IsModuleExist", parameters).Tables[0];
			return result.Rows.Count > 0 && Convert.ToInt32(result.Rows[0][0]) == 1;
		}

		/// <summary>Отказ «не выходит целиком» — текст десктопного комбо.</summary>
		private static InvalidOperationException NotWhole(ModuleDay m, DateTime day) =>
			new InvalidOperationException(Tr.Format("Модуль «{0}» ({1}) не выходит {2:dd.MM.yyyy} целиком, выпуск не создан.", m.ModuleName, m.Station, day));

		/// <summary>
		/// ModuleIssueIUD AddItem с параметрами десктопа (ModuleIssue из Campaign.AddModuleIssue): длительность ролика и цену
		/// модуля процедура берёт из базы, грантор — NULL. LoadDataSet, а не DoAction: по метаданным процедура NO_DATA, и DoAction
		/// выбросил бы её ответ — выпуск (ModuleIssueRetrieve в конце) или −1, если у модуля за день не нашлось ни одного окна.
		/// Возвращает номер выпуска или −1.
		/// </summary>
		private static int AddModuleIssue(ActionOnMassmedia action, ModuleDay m, Roller roller, int position, DateTime day)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Constants.ParamNames.ActionName] = Constants.Actions.AddItem;
			parameters[Campaign.ParamNames.CampaignId] = m.CampaignId;
			parameters[Module.ParamNames.ModuleId] = m.ModuleId;
			parameters[ModulePricelist.ParamNames.ModulePriceListID] = m.ModulePricelistId;
			parameters[Roller.ParamNames.RollerId] = roller.RollerId;
			parameters[Issue.ParamNames.PositionId] = position;
			parameters[Issue.ParamNames.IssueDate] = day;
			parameters[Action.ParamNames.IsConfirmed] = action.IsConfirmed;
			DataSet answer = DataAccessor.LoadDataSet("ModuleIssueIUD", parameters);
			for (int i = answer.Tables.Count - 1; i >= 0; i--)
			{
				DataTable table = answer.Tables[i];
				if (table.Columns.Contains(Issue.ParamNames.ModuleIssueId) && table.Rows.Count > 0)
					return Convert.ToInt32(table.Rows[0][Issue.ParamNames.ModuleIssueId]);
			}
			return -1;
		}

		// ---------- Панель места ----------

		/// <summary>Окно в панели места: окно выхода своего выпуска модуля или окно тарифа модуля за день.</summary>
		public sealed class ModuleWindow
		{
			internal ModuleWindow() { }

			public int WindowId { get; internal set; }

			/// <summary>Фактические дата и время выхода окна (windowDateActual).</summary>
			public DateTime Date { get; internal set; }

			/// <summary>Остаток окна (у штучного — «[осталось/всего]»); null — у окна выхода выпуска.</summary>
			public string Text { get; internal set; }

			/// <summary>Окно заблокировано (isDisabled).</summary>
			public bool Disabled { get; internal set; }

			/// <summary>Окно выхода: выпуск модуля, ролик и порядок дочерней строки Issue; у окна тарифа — null.</summary>
			public int? ModuleIssueId { get; internal set; }

			public string RollerName { get; internal set; }
			public string Position { get; internal set; }

			/// <summary>Кампания строки — для «Выходов в эфир этой кампании» окна.</summary>
			internal int CampaignId { get; set; }
		}

		/// <summary>Панель места вкладки «Модули»: модуль строки за день, его выпуски и окна.</summary>
		public sealed class ModuleDayDetails
		{
			internal ModuleDayDetails() { }

			public string Massmedia { get; internal set; }
			public string Module { get; internal set; }
			public string PaymentType { get; internal set; }
			public DateTime Date { get; internal set; }

			/// <summary>Цена прайс-листа модуля, ₽ за секунду ролика.</summary>
			public decimal Price { get; internal set; }

			/// <summary>Выпуски модуля строки за день (ModuleIssueRetrieve) — колонки по <see cref="IssueEntity"/>.</summary>
			public DataTable Issues { get; internal set; }

			/// <summary>true — <see cref="Windows"/> — окна выхода своих выпусков, false — окна тарифов модуля за день.</summary>
			public bool ExitWindows { get; internal set; }

			/// <summary>Окна по времени выхода.</summary>
			public IReadOnlyList<ModuleWindow> Windows { get; internal set; }
		}

		/// <summary>
		/// Панель места. Выпуски строки за день — ModuleIssueRetrieve @campaignID, @issueDate по модулю строки, свои всегда с
		/// макетами (как выпуски кампании в панели CampaignForm). Окна: есть свой выпуск — окна ВЫХОДА его дочерних строк
		/// (ModuleIssueContentRetrieve: actualWindowID, windowDateActual, позиция; у выпуска с перенесёнными трафиком строками
		/// они отличаются от окон тарифов), нет — окна тарифов модуля за день (TariffWindowRetrieve @moduleId, @pricelistId)
		/// с остатком по <paramref name="showUnconfirmed"/> и пометкой заблокированных.
		/// </summary>
		public static ModuleDayDetails Details(ModuleScope s, PlacementCell cell, bool showUnconfirmed)
		{
			ModuleDay m = cell.ModuleDay;
			if (m == null || !s.Campaigns.ContainsKey(m.CampaignId))
				throw new InvalidOperationException(Tr.T("Место не относится к отмеченным модульным кампаниям. Обновите страницу."));

			DataTable issues = LoadRowIssues(m, cell.Date);
			List<ModuleWindow> windows = new List<ModuleWindow>();
			if (issues.Rows.Count > 0)
			{
				foreach (DataRow issue in issues.Rows)
				{
					int moduleIssueId = Convert.ToInt32(issue[Issue.ParamNames.ModuleIssueId]);
					foreach (DataRow line in LoadIssueContent(moduleIssueId).Rows)
						windows.Add(new ModuleWindow
						{
							WindowId = Convert.ToInt32(line[Issue.ActualWindowIdParam]),
							Date = Convert.ToDateTime(line[RollerIssue.ParamNames.IssueDate]),
							ModuleIssueId = moduleIssueId,
							RollerName = Convert.ToString(line[Constants.Parameters.Name]),
							Position = PositionText(ParseHelper.GetInt32FromObject(line[Issue.ParamNames.PositionId], 0)),
							CampaignId = m.CampaignId,
						});
				}
			}
			else
			{
				foreach (DataRow w in LoadModuleWindows(m, cell.Date).Rows)
					windows.Add(new ModuleWindow
					{
						WindowId = Convert.ToInt32(w[TariffWindow.ParamNames.WindowId]),
						Date = Convert.ToDateTime(w[TariffWindow.ParamNames.WindowDateActual]),
						Text = WindowsSource.CellText(w, showUnconfirmed),
						Disabled = w[TariffWindow.ParamNames.IsDisabled] is bool disabled && disabled,
						CampaignId = m.CampaignId,
					});
			}

			return new ModuleDayDetails
			{
				Massmedia = m.Station,
				Module = m.ModuleName,
				PaymentType = m.PaymentType,
				Date = cell.Date,
				Price = m.Price,
				Issues = issues,
				ExitWindows = issues.Rows.Count > 0,
				Windows = windows.OrderBy(w => w.Date).ThenBy(w => w.ModuleIssueId ?? 0).ToList(),
			};
		}

		/// <summary>Порядок дочернего выпуска — как в веере (RangeSource): без порядка — пусто, иначе переводимое имя.</summary>
		private static string PositionText(int positionId) =>
			positionId == 0 ? "" : Issue.GetPositionDisplayName((RollerPositions)positionId);

		/// <summary>Выпуски кампании за день (ModuleIssueRetrieve), только модуля строки.</summary>
		private static DataTable LoadRowIssues(ModuleDay m, DateTime day)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Campaign.ParamNames.CampaignId] = m.CampaignId;
			parameters[RollerIssue.ParamNames.IssueDate] = day.Date;
			parameters["showUnconfirmed"] = true;
			DataTable all = DataAccessor.LoadDataSet("ModuleIssueRetrieve", parameters).Tables[0];
			DataTable result = all.Clone();
			foreach (DataRow row in all.Rows)
				if (Convert.ToInt32(row[Module.ParamNames.ModuleId]) == m.ModuleId)
					result.ImportRow(row);
			return result;
		}

		/// <summary>Дочерние строки Issue выпуска модуля — по окну выхода (ModuleIssueContentRetrieve, загрузка сущности 155).</summary>
		private static DataTable LoadIssueContent(int moduleIssueId)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Issue.ParamNames.ModuleIssueId] = moduleIssueId;
			return DataAccessor.LoadDataSet("ModuleIssueContentRetrieve", parameters).Tables[0];
		}

		/// <summary>
		/// Окна тарифов модуля за день — TariffWindowRetrieve, модульная ветка: день один, поэтому стык смежных прайс-листов
		/// модуля (Д-19) сюда не попадает; без особых окон, заблокированные — с пометкой, время — фактическое.
		/// </summary>
		private static DataTable LoadModuleWindows(ModuleDay m, DateTime day)
		{
			Dictionary<string, object> parameters = DataAccessor.PrepareParameters(EntityManager.GetEntity((int)Entities.TariffWindow));
			parameters[Module.ParamNames.ModuleId] = m.ModuleId;
			parameters[Pricelist.ParamNames.PricelistId] = m.PricelistId;
			parameters[Pricelist.ParamNames.StartDate] = day.Date;
			parameters[Pricelist.ParamNames.FinishDate] = day.Date;
			parameters[MassmediaPricelist.ParamNames.ExcludeSpecialWindows] = true;
			parameters[MassmediaPricelist.ParamNames.ShowDisabledWindows] = true;
			parameters[MassmediaPricelist.ParamNames.UseActualTime] = true;
			return ((DataSet)DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
		}

		/// <summary>Сущность «Выпуск модуля» (130) с колонками панели: модуль, ролик, предмет рекламы, порядок.</summary>
		public static Entity IssueEntity()
		{
			Entity entity = (Entity)EntityManager.GetEntity((int)Entities.ModuleIssue).Clone();
			entity.AttributeSelector = 0;
			return entity;
		}

		/// <summary>
		/// «Все выходы в эфир» окна панели — по требованию: WindowIssuesRetrieve по окну (как у линейной вкладки), выпуски
		/// кампании строки — всегда, остальные — по <paramref name="showUnconfirmed"/>.
		/// </summary>
		public static WindowsSource.WindowIssues LoadWindowIssues(ModuleWindow window, bool showUnconfirmed)
		{
			// Вкладка показывает только «все», поэтому свои выпуски кладутся в них и без «Учитывать макеты»: окно выхода
			// своего макета иначе выглядело бы пустым (окна выхода читаются с макетами).
			WindowsSource.WindowIssues issues = WindowsSource.LoadWindowIssues(window.WindowId, window.CampaignId, true);
			if (showUnconfirmed)
				return issues;
			DataTable shown = issues.All.Clone();
			foreach (DataRow row in issues.All.Rows)
				if (ParseHelper.GetBooleanFromObject(row[Action.ParamNames.IsConfirmed], false)
					|| ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignId], 0) == window.CampaignId)
					shown.ImportRow(row);
			return new WindowsSource.WindowIssues(issues.Campaign, shown);
		}
	}
}
