using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using log4net;
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
	/// Выделение (подэтап 6.4) — «Разместить ролик» и «Удалить выпуски» по выделенным местам (десктоп комбо — Insert и Del):
	/// транзакция на день выделения, как у шаблонов, каждый вызов ModuleIssueIUD — шаг с точкой сохранения, один пересчёт в
	/// конце; опустевшую кампанию, которую создала вкладка в этом сеансе, «Удалить выпуски» удаляет в той же транзакции (В-21).
	///
	/// «Показать модули…» (подэтап 6.3) — добавленные строки (<see cref="AddedRow"/>): модули комбо-модуля, станции или
	/// своей кампании. Строке без кампании модульная кампания создаётся при первой постановке в той же транзакции — как
	/// режимы 1/3 десктопного комбо (ComboModulePlacementForm.EnsureCampaign). Помнит строки экран (на сеанс), не ядро.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class ModuleDaySource
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ModuleDaySource));

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

			internal int PaymentTypeId { get; set; }
			internal string PaymentTypeName { get; set; }
			internal int AgencyId { get; set; }
			internal string AgencyName { get; set; }
		}

		/// <summary>
		/// Отмеченные модульные кампании акции, с которыми работает вкладка. Номер строки (модуль, кампания) выдаётся один
		/// раз за жизнь объекта и не меняется — ключ места = день × 100 000 + номер строки (ключ «день × moduleID» дал бы
		/// одно место двум кампаниям одного модуля). Добавленная строка без кампании получает номер по своему ключу
		/// (<see cref="AddedRow.Key"/>), а когда кампанию создаст или найдёт постановка, строка кампании берёт этот же номер —
		/// место не сдвигается.
		/// </summary>
		public sealed class ModuleScope : WindowsSource.IBrushScope
		{
			internal ModuleScope() { }

			public int ActionId { get; internal set; }
			public int FirmId { get; internal set; }

			/// <summary>
			/// Можно ли менять размещение: «Редактировать» у акции и у модульных кампаний (на момент открытия); у набора без
			/// кампаний — можно ли добавлять модульные кампании (<see cref="CanAddCampaigns"/>): ставить есть куда только в
			/// добавленные строки.
			/// </summary>
			public bool CanEdit { get; internal set; }

			/// <summary>Можно ли создавать модульные кампании постановкой в добавленную строку (на момент открытия).</summary>
			public bool CanAddCampaigns { get; internal set; }

			public DataTable Rollers { get; set; }

			private readonly List<int> _campaignIds = new List<int>();
			private readonly Dictionary<int, CampaignInfo> _campaigns = new Dictionary<int, CampaignInfo>();

			/// <summary>
			/// Отмеченные модульные кампании — в порядке, в котором их передал экран; кампания, которую создала или нашла
			/// постановка в добавленную строку, дописывается в конец (экрану — отметить её).
			/// </summary>
			public IReadOnlyList<int> CampaignIds => _campaignIds;

			/// <summary>День самого раннего выпуска модуля отмеченных кампаний; null — выпусков нет. С него открывается сетка.</summary>
			public DateTime? FirstIssueDate { get; internal set; }

			internal IReadOnlyDictionary<int, CampaignInfo> Campaigns => _campaigns;

			internal void AddCampaign(int campaignId, CampaignInfo campaign)
			{
				if (_campaigns.ContainsKey(campaignId))
					return;
				_campaignIds.Add(campaignId);
				_campaigns.Add(campaignId, campaign);
			}

			/// <summary>
			/// Кампанию удалили («Удалить выпуски», В-21): её строки уходят из набора, а их номера возвращаются ключам
			/// ожидающих строк — добавленная строка этой станции, оплаты и агентства снова станет ожидающей на своём месте.
			/// </summary>
			internal void RemoveCampaign(int campaignId)
			{
				if (!_campaigns.TryGetValue(campaignId, out CampaignInfo c))
					return;
				_campaignIds.Remove(campaignId);
				_campaigns.Remove(campaignId);
				foreach ((int ModuleId, int CampaignId) row in _rowNumbers.Keys.Where(k => k.CampaignId == campaignId).ToList())
				{
					var pending = (row.ModuleId, c.MassmediaId, c.PaymentTypeId, c.AgencyId);
					if (!_pendingNumbers.ContainsKey(pending))
						_pendingNumbers.Add(pending, _rowNumbers[row]);
					_rowNumbers.Remove(row);
				}
			}

			private readonly Dictionary<(int ModuleId, int CampaignId), int> _rowNumbers = new Dictionary<(int, int), int>();
			private readonly Dictionary<(int ModuleId, int MassmediaId, int PaymentTypeId, int AgencyId), int> _pendingNumbers =
				new Dictionary<(int, int, int, int), int>();
			private int _lastNumber;

			/// <summary>Строки (модуль, кампания), которые набор уже показывал, — им выдан номер.</summary>
			internal IEnumerable<(int ModuleId, int CampaignId)> ShownRows => _rowNumbers.Keys;

			/// <summary>
			/// Номер строки (модуль, кампания): выдан раз — тот же при любом периоде и фильтре. Строка ожидающей кампании,
			/// ставшая строкой кампании, сохраняет свой номер.
			/// </summary>
			internal int RowNumber(int moduleId, int campaignId)
			{
				if (_rowNumbers.TryGetValue((moduleId, campaignId), out int number))
					return number;
				if (_campaigns.TryGetValue(campaignId, out CampaignInfo c)
					&& _pendingNumbers.TryGetValue((moduleId, c.MassmediaId, c.PaymentTypeId, c.AgencyId), out number))
					_pendingNumbers.Remove((moduleId, c.MassmediaId, c.PaymentTypeId, c.AgencyId));
				else
					number = ++_lastNumber;
				_rowNumbers.Add((moduleId, campaignId), number);
				return number;
			}

			/// <summary>Номер добавленной строки без кампании — по её ключу, выдан раз.</summary>
			internal int PendingRowNumber((int ModuleId, int MassmediaId, int PaymentTypeId, int AgencyId) key)
			{
				if (!_pendingNumbers.TryGetValue(key, out int number))
				{
					number = ++_lastNumber;
					_pendingNumbers.Add(key, number);
				}
				return number;
			}
		}

		/// <summary>
		/// Отмеченные кампании: из <paramref name="campaignIds"/> остаются только модульные кампании этой акции (чужие,
		/// не модульные и удалённые отбрасываются, повторы — тоже). Пустой набор — строк нет, кроме добавленных
		/// «Показать модули…» (В-23: вкладка есть и у акции без модульных кампаний).
		/// </summary>
		public static ModuleScope Open(ActionOnMassmedia action, IEnumerable<int> campaignIds)
		{
			Dictionary<int, DataRow> rows = new Dictionary<int, DataRow>();
			foreach (DataRow row in ActionWorkspace.LoadCampaigns(action).Rows)
				if (IsModular(row))
					rows[WindowsSource.CampaignIdOf(row)] = row;

			ModuleScope scope = new ModuleScope
			{
				ActionId = action.ActionId,
				FirmId = action.FirmID,
				CanAddCampaigns = CanAddModuleCampaigns(action),
			};
			foreach (int id in campaignIds)
				if (rows.TryGetValue(id, out DataRow row))
					scope.AddCampaign(id, new CampaignInfo
					{
						MassmediaId = ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.MassmediaId], 0),
						Station = StationOf(row),
						PaymentTypeId = ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.PaymentTypeID], 0),
						PaymentTypeName = Convert.ToString(row[PaymentTypeNameColumn]),
						AgencyId = ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.AgencyID], 0),
						AgencyName = Convert.ToString(row[AgencyNameColumn]).Trim(),
					});

			// Не Campaign.startDate: он считается по строкам Issue (окнам), а день модуля — ModuleIssue.issueDate.
			DateTime? first = null;
			if (scope.CampaignIds.Count > 0)
				foreach (DataRow issue in ComboModule.LoadIssues(action.ActionId).Rows)
					if (scope.Campaigns.ContainsKey(IssueCampaignId(issue)))
					{
						DateTime day = IssueDay(issue);
						if (first == null || day < first)
							first = day;
					}
			scope.FirstIssueDate = first;

			scope.CanEdit = scope.CampaignIds.Count > 0
				? ActionWorkspace.CanEdit(action) && CampaignRight(scope.CampaignIds[0])
				: scope.CanAddCampaigns;
			return scope;
		}

		/// <summary>
		/// Можно ли создавать модульные кампании в акции (постановкой в добавленную строку): право добавлять кампании, как
		/// у «Добавить кампании» (ActionWorkspace.CanAddCampaigns), и «Редактировать» модульной кампании (сущность 92) — то
		/// же, что CampaignRight у существующей: правило своих / чужих акций уже проверено правом на акцию.
		/// </summary>
		public static bool CanAddModuleCampaigns(ActionOnMassmedia action) =>
			ActionWorkspace.CanAddCampaigns(action)
			&& EntityManager.GetEntity((int)Entities.ModuleCampaign).IsActionEnabled(Constants.EntityActions.Edit, ViewType.Journal);

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
		private static string StationOf(DataRow row) =>
			StationLabel(row[Campaign.ParamNames.MassmediaName], row[Campaign.ParamNames.GroupName]);

		private static string StationLabel(object massmediaName, object groupName)
		{
			string name = Convert.ToString(massmediaName).Trim();
			string group = Convert.ToString(groupName).Trim();
			return group.Length == 0 ? name : string.Format("{0} ({1})", name, group);
		}

		private static int IssueCampaignId(DataRow issue) => Convert.ToInt32(issue[Campaign.ParamNames.CampaignId]);

		private static int IssueModuleId(DataRow issue) => Convert.ToInt32(issue[ComboModule.ParamNames.ModuleId]);

		private static DateTime IssueDay(DataRow issue) => Convert.ToDateTime(issue[ComboModule.ParamNames.IssueDate]).Date;

		// ---------- «Показать модули…»: добавленные строки и их источники ----------

		/// <summary>Модуль радиостанции — строка источника «Показать модули…».</summary>
		public sealed class ModuleChoice
		{
			internal ModuleChoice() { }

			public int ModuleId { get; internal set; }
			public string ModuleName { get; internal set; }
			public int MassmediaId { get; internal set; }

			/// <summary>Радиостанция с группой — как колонка «Радиостанция» строк.</summary>
			public string Station { get; internal set; }
		}

		/// <summary>Радиостанция, у которой есть модули, — источник «Модули радиостанции».</summary>
		public sealed class StationChoice
		{
			internal StationChoice() { }

			public int MassmediaId { get; internal set; }

			/// <summary>Радиостанция с группой — как колонка «Радиостанция» строк.</summary>
			public string Station { get; internal set; }

			/// <summary>Группа радиостанций (город); пусто — без группы.</summary>
			public string GroupName { get; internal set; }
		}

		/// <summary>
		/// Строка, добавленная «Показать модули…»: модуль, его радиостанция, тип оплаты и агентство будущей кампании, а если
		/// кампания уже есть («модули этой кампании», В-24) — её номер. Неизменяемая. Строка без кампании — ожидающая: её
		/// ключ — <see cref="Key"/>; модульную кампанию с этой станцией, типом оплаты и агентством создаст (или найдёт)
		/// первая постановка в строку (<see cref="PlaceOne"/>), и строка станет обычной строкой модуль × кампания.
		/// </summary>
		public sealed class AddedRow
		{
			/// <summary>Строка без кампании: модуль комбо-модуля или станции, тип оплаты и агентство — выбор экрана.</summary>
			public AddedRow(ModuleChoice module, int paymentTypeId, string paymentTypeName, int agencyId, string agencyName)
				: this(module.ModuleId, module.ModuleName, module.MassmediaId, module.Station,
					paymentTypeId, paymentTypeName, agencyId, agencyName, null)
			{
			}

			internal AddedRow(int moduleId, string moduleName, int massmediaId, string station, int paymentTypeId,
				string paymentTypeName, int agencyId, string agencyName, int? campaignId)
			{
				// Без агентства и типа оплаты кампанию не создать (CampaignIUD: CannotAddCampaignWithoutAgency): станцию без
				// агентства экран пропускает с пояснением, как CampaignAddForm.
				if (paymentTypeId <= 0)
					throw new ArgumentOutOfRangeException(nameof(paymentTypeId));
				if (agencyId <= 0)
					throw new ArgumentOutOfRangeException(nameof(agencyId));
				ModuleId = moduleId;
				ModuleName = moduleName;
				MassmediaId = massmediaId;
				Station = station;
				PaymentTypeId = paymentTypeId;
				PaymentTypeName = paymentTypeName ?? string.Empty;
				AgencyId = agencyId;
				AgencyName = (agencyName ?? string.Empty).Trim();
				CampaignId = campaignId;
			}

			public int ModuleId { get; }
			public string ModuleName { get; }
			public int MassmediaId { get; }
			public string Station { get; }
			public int PaymentTypeId { get; }
			public string PaymentTypeName { get; }
			public int AgencyId { get; }
			public string AgencyName { get; }

			/// <summary>Кампания строки, если она уже была при добавлении; null — создаст постановка.</summary>
			public int? CampaignId { get; }

			/// <summary>Та же строка без кампании — ожидающая: кампанию строки удалили (В-21), первая постановка создаст её заново.</summary>
			public AddedRow WithoutCampaign() => CampaignId == null ? this
				: new AddedRow(ModuleId, ModuleName, MassmediaId, Station, PaymentTypeId, PaymentTypeName, AgencyId, AgencyName, null);

			/// <summary>
			/// Ключ строки — (модуль, станция, тип оплаты, агентство): у станции не бывает двух модульных кампаний с одним типом
			/// оплаты и агентством (UIX_Campaign), поэтому строки с одним ключом — одна строка.
			/// </summary>
			public (int ModuleId, int MassmediaId, int PaymentTypeId, int AgencyId) Key => (ModuleId, MassmediaId, PaymentTypeId, AgencyId);

			internal bool IsOf(CampaignInfo campaign) =>
				campaign.MassmediaId == MassmediaId && campaign.PaymentTypeId == PaymentTypeId && campaign.AgencyId == AgencyId;
		}

		/// <summary>Колонки списка комбо-модулей (<see cref="ComboModules"/>).</summary>
		public const string ComboModuleIdColumn = ComboModule.ParamNames.ComboModuleId;
		public const string ComboModuleNameColumn = "name";

		/// <summary>
		/// Комбо-модули: comboModuleID, name — процедурой ComboModules, как список десктопного шага SelectComboModuleStep
		/// (загрузка сущности 1270). Не через метаданные: у сущности 1270 на ArtvisDev нет групп, проверка прав веба закрыла бы
		/// список всем, а десктоп прав на него не спрашивает.
		/// </summary>
		public static DataTable ComboModules() =>
			DataAccessor.LoadDataSet("ComboModules", DataAccessor.CreateParametersDictionary()).Tables[0];

		/// <summary>Состав комбо-модуля (ComboModuleContentRetrieve) — модули по радиостанциям, как строки десктопного комбо.</summary>
		public static IReadOnlyList<ModuleChoice> ComboModuleContent(int comboModuleId) =>
			ComboModule.LoadContent(comboModuleId).Rows.Cast<DataRow>().Select(row => new ModuleChoice
			{
				ModuleId = Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]),
				ModuleName = Convert.ToString(row[ComboModule.ParamNames.ModuleName]),
				MassmediaId = Convert.ToInt32(row[ComboModule.ParamNames.MassmediaId]),
				Station = StationOf(row),
			}).ToList();

		/// <summary>
		/// Радиостанции с модулями, у которых прайс-лист действует или начнётся (ModuleList на дату): все, без права станции
		/// «добавлять» — как десктопный комбо (В-26). Порядок — по подписи.
		/// </summary>
		public static IReadOnlyList<StationChoice> StationsWithModules(DateTime date)
		{
			Dictionary<int, StationChoice> stations = new Dictionary<int, StationChoice>();
			foreach (DataRow row in LoadStationModules(null, date.Date).Rows)
			{
				int massmediaId = Convert.ToInt32(row[Massmedia.ParamNames.MassmediaId]);
				if (!stations.ContainsKey(massmediaId))
					stations.Add(massmediaId, new StationChoice
					{
						MassmediaId = massmediaId,
						Station = StationOf(row),
						GroupName = Convert.ToString(row[Campaign.ParamNames.GroupName]).Trim(),
					});
			}
			StringComparer text = StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true);
			return stations.Values.OrderBy(x => x.Station, text).ToList();
		}

		/// <summary>Модули радиостанции с прайс-листом, действующим или будущим на дату (ModuleList @massmediaID, @theDate).</summary>
		public static IReadOnlyList<ModuleChoice> StationModules(int massmediaId, DateTime date) =>
			LoadStationModules(massmediaId, date.Date).Rows.Cast<DataRow>().Select(row => new ModuleChoice
			{
				ModuleId = Convert.ToInt32(row[Module.ParamNames.ModuleId]),
				ModuleName = Convert.ToString(row[Constants.Parameters.Name]),
				MassmediaId = Convert.ToInt32(row[Massmedia.ParamNames.MassmediaId]),
				Station = StationOf(row),
			}).ToList();

		/// <summary>
		/// «Модули этой кампании» (В-24): модули станции отмеченной кампании на дату — строки сразу с кампанией, её типом
		/// оплаты и агентством, без вопросов.
		/// </summary>
		public static IReadOnlyList<AddedRow> CampaignModules(ModuleScope s, int campaignId, DateTime date)
		{
			if (!s.Campaigns.TryGetValue(campaignId, out CampaignInfo c))
				throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));
			return StationModules(c.MassmediaId, date)
				.Select(m => new AddedRow(m.ModuleId, m.ModuleName, c.MassmediaId, c.Station, c.PaymentTypeId, c.PaymentTypeName, c.AgencyId, c.AgencyName, campaignId))
				.ToList();
		}

		// ---------- Период ----------

		/// <summary>Модуль и кампания места за день — то, что нужно панели и записи (у PlacementCell — internal).</summary>
		internal sealed class ModuleDay
		{
			internal int ModuleId { get; set; }

			/// <summary>Кампания строки; 0 — добавленная строка без кампании (<see cref="Added"/>), её создаст постановка.</summary>
			internal int CampaignId { get; set; }

			/// <summary>Добавленная строка без кампании: станция, тип оплаты и агентство будущей кампании; иначе null.</summary>
			internal AddedRow Added { get; set; }

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

			/// <summary>0 — добавленная строка без кампании (<see cref="Added"/>).</summary>
			internal int CampaignId { get; set; }

			/// <summary>Кампания строки; у добавленной без кампании — станция, тип оплаты и агентство из строки.</summary>
			internal CampaignInfo Campaign { get; set; }

			internal AddedRow Added { get; set; }
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
		///
		/// <paramref name="added"/> — строки «Показать модули…» (память экрана): строка отмеченной кампании (с её номером или
		/// без номера, но с её станцией, типом оплаты и агентством) — обычная строка модуль × кампания; строка без такой
		/// кампании в наборе — ожидающая: те же колонки, места из того же вызова процедуры, своих выпусков нет; строка
		/// с номером кампании, которой в наборе нет (снята отметка, удалена), не показывается. Совпадающие строки не
		/// дублируются.
		/// </summary>
		public static PlacementWeek LoadPeriod(ModuleScope s, DateTime anyDate, PlacementPeriod period, WindowsSource.WeekView view,
			IEnumerable<AddedRow> added = null)
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

			List<RowSpec> specs = RowSpecs(s, issues, start, finish, added);
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

				int number = spec.Added != null ? s.PendingRowNumber(spec.Added.Key) : s.RowNumber(spec.ModuleId, spec.CampaignId);
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
							Added = spec.Added,
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
		/// Строки периода: размещённые пары (модуль, кампания), модули станции у кампаний без выпусков и добавленные строки;
		/// порядок и подпись типа оплаты — как ComboModulePlacementForm.BuildGridRows (при одинаковом типе оплаты у двух
		/// строк модуля — с агентством).
		/// </summary>
		private static List<RowSpec> RowSpecs(ModuleScope s, List<DataRow> issues, DateTime start, DateTime finish, IEnumerable<AddedRow> addedRows)
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

			// Добавленные «Показать модули…»: строка отмеченной кампании — её строка (как десктопный комбо находит кампанию
			// строки по станции, типу оплаты и агентству, FindModuleCampaignRow), иначе ожидающая строка.
			if (addedRows != null)
				foreach (AddedRow row in addedRows)
				{
					int campaignId = row.CampaignId ?? s.Campaigns.Where(c => row.IsOf(c.Value)).Select(c => c.Key).FirstOrDefault();
					if (campaignId != 0)
					{
						if (s.Campaigns.TryGetValue(campaignId, out CampaignInfo campaign) && !specs.Any(r => r.ModuleId == row.ModuleId && r.CampaignId == campaignId))
							specs.Add(new RowSpec { ModuleId = row.ModuleId, ModuleName = row.ModuleName, CampaignId = campaignId, Campaign = campaign });
					}
					else if (!specs.Any(r => r.Added != null && r.Added.Key == row.Key))
						specs.Add(new RowSpec
						{
							ModuleId = row.ModuleId,
							ModuleName = row.ModuleName,
							Added = row,
							Campaign = new CampaignInfo
							{
								MassmediaId = row.MassmediaId,
								Station = row.Station,
								PaymentTypeId = row.PaymentTypeId,
								PaymentTypeName = row.PaymentTypeName,
								AgencyId = row.AgencyId,
								AgencyName = row.AgencyName,
							},
						});
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

		/// <summary>ModuleList @massmediaID, @theDate: модули станции (null — всех станций) с прайс-листом, не закончившимся к дате.</summary>
		private static DataTable LoadStationModules(int? massmediaId, DateTime theDate)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			if (massmediaId.HasValue)
				parameters[Massmedia.ParamNames.MassmediaId] = massmediaId.Value;
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
		/// как у RangeSource.LoadForWrite; у набора без кампаний — права добавлять модульные кампании) — отказ.
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

			/// <summary>
			/// Кампанию строки создало это действие (постановка в добавленную строку без кампании) — для учёта «кампанию создала
			/// вкладка в этом сеансе» (В-21). Найденная существующая кампания — false.
			/// </summary>
			public bool CampaignCreated { get; internal set; }
		}

		/// <summary>Место строки отмеченной кампании или добавленной строки без кампании.</summary>
		private static bool IsOfScope(ModuleScope s, ModuleDay m) =>
			m != null && (m.CampaignId == 0 ? m.Added != null : s.Campaigns.ContainsKey(m.CampaignId));

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
		///
		/// Добавленная строка без кампании (режимы 1/3 комбо, ComboModulePlacementForm.EnsureCampaign): в той же транзакции
		/// модульная кампания акции с её станцией, типом оплаты и агентством — найденная или созданная (EnsureCampaign);
		/// нужно право добавлять модульные кампании (<see cref="CanAddModuleCampaigns"/>), права станции не проверяются, как
		/// в десктопном комбо (В-26). Любой отказ откатывает и созданную кампанию. После записи кампания попадает в набор
		/// (<see cref="ModuleScope.CampaignIds"/>), строка остаётся на своём месте с тем же номером.
		/// </summary>
		public static PlacedModuleIssue PlaceOne(ModuleScope s, PlacementCell cell, int rollerId, int position)
		{
			ModuleDay m = cell?.ModuleDay;
			if (!IsOfScope(s, m))
				throw new InvalidOperationException(Tr.T("Место не относится к отмеченным модульным кампаниям. Обновите страницу."));
			DateTime day = cell.Date.Date;
			AddedRow pending = m.CampaignId == 0 ? m.Added : null;

			ActionOnMassmedia action = LoadForWrite(s);
			if (pending != null && !(s.CanAddCampaigns && CanAddModuleCampaigns(action)))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			int massmediaId = pending?.MassmediaId ?? ModularCampaignStation(action, m.CampaignId);
			Roller roller = ModuleRoller(s, rollerId, action.FirmID);
			if (!IsPricelistOfDay(m, massmediaId, day))
				throw new InvalidOperationException(Tr.Format("Прайс-лист модуля «{0}» на {1:dd.MM.yyyy} изменился. Обновите страницу.", m.ModuleName, day));

			int campaignId = m.CampaignId;
			bool created = false;
			int moduleIssueId = 0;
			WindowsSource.RunInTransaction(() =>
			{
				if (!IsModuleWhole(m.ModulePricelistId, day))
					throw NotWhole(m, day);
				if (pending != null)
					campaignId = EnsureCampaign(action, pending, out created);
				moduleIssueId = AddModuleIssue(action, campaignId, m, roller, position, day);
				if (moduleIssueId <= 0)
					throw NotWhole(m, day);
				action.Recalculate(false);
			});

			if (pending != null)
				s.AddCampaign(campaignId, InfoOf(pending));

			return Placed(m, roller, day, campaignId, moduleIssueId, created);
		}

		/// <summary>Кампания добавленной строки для набора вкладки — станция, тип оплаты и агентство строки.</summary>
		private static CampaignInfo InfoOf(AddedRow row) => new CampaignInfo
		{
			MassmediaId = row.MassmediaId,
			Station = row.Station,
			PaymentTypeId = row.PaymentTypeId,
			PaymentTypeName = row.PaymentTypeName,
			AgencyId = row.AgencyId,
			AgencyName = row.AgencyName,
		};

		private static PlacedModuleIssue Placed(ModuleDay m, Roller roller, DateTime day, int campaignId, int moduleIssueId, bool campaignCreated) =>
			new PlacedModuleIssue
			{
				ModuleIssueId = moduleIssueId,
				CampaignId = campaignId,
				ModuleId = m.ModuleId,
				RollerId = roller.RollerId,
				Date = day,
				ModuleName = m.ModuleName,
				Name = string.Format("{0:dd.MM.yyyy} — {1} — {2}", day, m.ModuleName, roller.Name),
				CampaignCreated = campaignCreated,
			};

		/// <summary>
		/// Модульная кампания акции для добавленной строки — внутри транзакции постановки: сначала поиск по станции, типу
		/// оплаты и агентству в свежем списке кампаний (Campaigns @actionID на соединении транзакции), не нашлась — CampaignIUD
		/// AddItem тем же путём, что «Добавить кампании» и мастер (ActionWorkspace.Draft: тип 3, сущность 92). Объект кампании
		/// строится заново на каждый вызов: записанный и откаченный объект уже не «новый» (класс Д-5). 2601/2627 (UIX_Campaign:
		/// ту же кампанию только что создал другой) — перечитать и взять её; бизнес-отказ CampaignIUD
		/// (CannotAddCampaignWithoutAgency, CannotAddCampaignInConfirmedFinishedAction) уходит наверх, транзакцию откатит
		/// вызывающий.
		/// </summary>
		private static int EnsureCampaign(ActionOnMassmedia action, AddedRow row, out bool created)
		{
			created = false;
			int campaignId = FindModuleCampaign(action, row);
			if (campaignId != 0)
				return campaignId;

			Campaign campaign = ActionWorkspace.Draft(ActionWorkspace.ModuleCampaignType, row.PaymentTypeId,
				new ActionWorkspace.NewCampaign { MassmediaId = row.MassmediaId, StationName = row.Station, AgencyId = row.AgencyId });
			campaign.Action = action;
			try
			{
				campaign.Update();
			}
			catch (SqlException ex) when (ex.Errors.Cast<SqlError>().Any(e => e.Number == 2601 || e.Number == 2627))
			{
				campaignId = FindModuleCampaign(action, row);
				if (campaignId != 0)
					return campaignId;
				throw;
			}
			created = true;
			return campaign.CampaignId;
		}

		/// <summary>Модульная кампания акции с этой станцией, типом оплаты и агентством по свежему списку; 0 — нет.</summary>
		private static int FindModuleCampaign(ActionOnMassmedia action, AddedRow row)
		{
			foreach (DataRow c in ActionWorkspace.LoadCampaigns(action).Rows)
				if (IsModular(c)
					&& ParseHelper.GetInt32FromObject(c[Campaign.ParamNames.MassmediaId], 0) == row.MassmediaId
					&& ParseHelper.GetInt32FromObject(c[Campaign.ParamNames.PaymentTypeID], 0) == row.PaymentTypeId
					&& ParseHelper.GetInt32FromObject(c[Campaign.ParamNames.AgencyID], 0) == row.AgencyId)
					return WindowsSource.CampaignIdOf(c);
			return 0;
		}

		/// <summary>Станция модульной кампании места по свежему списку кампаний акции; кампании в нём нет — её удалили.</summary>
		private static int ModularCampaignStation(ActionOnMassmedia action, int campaignId) =>
			CampaignStation(ModularCampaigns(action), campaignId);

		private static int CampaignStation(Dictionary<int, DataRow> campaigns, int campaignId) =>
			campaigns.TryGetValue(campaignId, out DataRow row)
				? ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.MassmediaId], 0)
				: throw new InvalidOperationException(Tr.T("Рекламная кампания удалена. Обновите страницу."));

		/// <summary>Модульные кампании акции по свежему списку (Campaigns @actionID) — строки по номеру кампании.</summary>
		private static Dictionary<int, DataRow> ModularCampaigns(ActionOnMassmedia action)
		{
			Dictionary<int, DataRow> result = new Dictionary<int, DataRow>();
			foreach (DataRow row in ActionWorkspace.LoadCampaigns(action).Rows)
				if (IsModular(row))
					result[WindowsSource.CampaignIdOf(row)] = row;
			return result;
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
		private static int AddModuleIssue(ActionOnMassmedia action, int campaignId, ModuleDay m, Roller roller, int position, DateTime day)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Constants.ParamNames.ActionName] = Constants.Actions.AddItem;
			parameters[Campaign.ParamNames.CampaignId] = campaignId;
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

		// ---------- Выделенные места: «Разместить ролик» и «Удалить выпуски» ----------

		/// <summary>
		/// Итог действия над выделенными местами (десктоп комбо — Insert и Del по выделению): сколько выпусков модулей
		/// поставлено или снято, отказы «что и где — причина», а также то, что нужно экрану для «Отменить последнее» и учёта
		/// кампаний, созданных вкладкой (В-21).
		/// </summary>
		public sealed class ModuleBatchResult
		{
			internal ModuleBatchResult()
			{
				Errors = new DataTable();
				Errors.Columns.Add("name", typeof(string));
				Errors.Columns.Add("description", typeof(string));
			}

			/// <summary>Поставлено или снято выпусков модулей.</summary>
			public int Done { get; internal set; }

			/// <summary>Не поставлено или не снято: отказы плюс места (выпуски) дней, до которых пачка не дошла из-за сбоя.</summary>
			public int NotDone { get; internal set; }

			/// <summary>name — что и где, description — причина: отказ, прерывание пачки, сбой пересчёта.</summary>
			public DataTable Errors { get; }

			/// <summary>Пачку прервал сбой (не бизнес-отказ): прежние дни записаны, этот и следующие — нет.</summary>
			public bool Interrupted { get; internal set; }

			/// <summary>«Разместить ролик»: поставленные выпуски — номера из ответа ModuleIssueIUD (запись для «Отменить последнее»).</summary>
			public List<PlacedModuleIssue> Created { get; } = new List<PlacedModuleIssue>();

			/// <summary>«Разместить ролик»: кампании, которые создала постановка в добавленные строки, — «созданы вкладкой» (В-21).</summary>
			public List<int> CreatedCampaignIds { get; } = new List<int>();

			/// <summary>«Удалить выпуски»: опустевшие кампании, созданные вкладкой, — удалены в той же транзакции (В-21).</summary>
			public List<int> DeletedCampaignIds { get; } = new List<int>();

			/// <summary>
			/// «Удалить выпуски»: опустевшие кампании, созданные вкладкой, которые остались — нет права удалять кампании или
			/// удаление отказало. Прочие опустевшие кампании показывает плашка страницы «без выпусков» (В-6).
			/// </summary>
			public List<int> KeptEmptyCampaignIds { get; } = new List<int>();

			internal void Note(string name, string description) => Errors.Rows.Add(name, description);
		}

		/// <summary>Итог одного дня пачки — попадает в <see cref="ModuleBatchResult"/> и в набор только после коммита дня.</summary>
		private sealed class DayWork
		{
			internal WindowsSource.BatchResult Steps { get; } = new WindowsSource.BatchResult();
			internal List<PlacedModuleIssue> Placed { get; } = new List<PlacedModuleIssue>();

			/// <summary>Кампании добавленных строк, найденные или созданные за день, — в набор вкладки.</summary>
			internal Dictionary<int, AddedRow> PendingCampaigns { get; } = new Dictionary<int, AddedRow>();

			internal List<int> CreatedCampaigns { get; } = new List<int>();
			internal List<int> DeletedCampaigns { get; } = new List<int>();
			internal List<int> KeptEmpty { get; } = new List<int>();
			internal List<(string Name, string Description)> Notes { get; } = new List<(string, string)>();
		}

		/// <summary>
		/// Дни пачки по порядку, каждый — в своей транзакции, как шаблоны (этап 5): блокировки окон держатся доли секунды, а не
		/// всю пачку. <paramref name="attempt"/> пишет день с номером i в открытой им транзакции; бизнес-отказы шагов — строки
		/// итога. Взаимоблокировка откатывает день целиком — он повторяется один раз (поэтому попытка собирает итог дня заново);
		/// иной сбой откатывает день и прерывает пачку, прежние дни остаются. Итог дня и <paramref name="committed"/> — только
		/// после коммита: кампания, созданная в откаченном дне, не попадёт ни в итог, ни в набор.
		/// </summary>
		private static void RunDays(ModuleBatchResult result, string kind, string context, IReadOnlyList<(DateTime Day, int Count)> days,
			Func<int, DayWork> attempt, System.Action<DayWork> committed, Func<Exception, int, string> interrupted)
		{
			for (int i = 0; i < days.Count; i++)
			{
				DayWork work = null;
				Exception failure = null;
				for (int n = 0; n < 2 && work == null && failure == null; n++)
				{
					try
					{
						work = attempt(i);
					}
					catch (SqlException ex) when (ex.Number == 1205 && n == 0)
					{
						Log.Warn($"Modules {kind}: deadlock on {days[i].Day:dd.MM.yyyy} ({context}), retrying the day", ex);
					}
					catch (Exception ex)
					{
						failure = ex;
					}
				}
				if (failure != null)
				{
					Log.Error($"Modules {kind} interrupted on {days[i].Day:dd.MM.yyyy} ({context})", failure);
					result.Interrupted = true;
					result.NotDone += days.Skip(i).Sum(d => d.Count);
					result.Note(days[i].Day.ToString("dd.MM.yyyy"), interrupted(failure, days.Count - i));
					return;
				}

				result.Done += work.Steps.Done;
				result.NotDone += work.Steps.Failed;
				foreach (DataRow refused in work.Steps.Errors.Rows)
					result.Note(Convert.ToString(refused["name"]), Convert.ToString(refused["description"]));
				foreach ((string name, string description) in work.Notes)
					result.Note(name, description);
				result.Created.AddRange(work.Placed);
				result.CreatedCampaignIds.AddRange(work.CreatedCampaigns);
				result.DeletedCampaignIds.AddRange(work.DeletedCampaigns);
				result.KeptEmptyCampaignIds.AddRange(work.KeptEmpty);
				committed(work);
			}
		}

		/// <summary>Один пересчёт акции после пачки, своей транзакцией; сбой — строка итога (выпуски уже записаны), как у шаблонов.</summary>
		private static void Recalculate(ActionOnMassmedia action, ModuleBatchResult result, string kind)
		{
			try
			{
				WindowsSource.RunInTransaction(() => action.Recalculate(false));
			}
			catch (Exception ex)
			{
				Log.Error($"Modules {kind}: recalculation failed (action {action.ActionId})", ex);
				result.Note("", Tr.Format("Пересчёт акции не выполнен: {0} Цена акции обновится при следующем изменении размещения.", ErrorManager.GetErrorMessage(ex)));
			}
		}

		/// <summary>Выделенные места без повторов; место не из набора (чужая строка, пустая ячейка) — отказ до записи.</summary>
		private static List<PlacementCell> DistinctCells(ModuleScope s, IEnumerable<PlacementCell> cells)
		{
			List<PlacementCell> result = new List<PlacementCell>();
			HashSet<int> keys = new HashSet<int>();
			foreach (PlacementCell cell in cells ?? Enumerable.Empty<PlacementCell>())
			{
				if (!IsOfScope(s, cell?.ModuleDay))
					throw new InvalidOperationException(Tr.T("Место не относится к отмеченным модульным кампаниям. Обновите страницу."));
				if (keys.Add(cell.Key))
					result.Add(cell);
			}
			return result;
		}

		/// <summary>Для строки итога: «дата — радиостанция — модуль».</summary>
		private static string CellName(ModuleDay m, DateTime day) => string.Format("{0:dd.MM.yyyy} — {1} — {2}", day, m.Station, m.ModuleName);

		/// <summary>
		/// «Разместить ролик» по выделенным местам (десктоп комбо — Insert по выделению, ComboModulePlacementForm.AddIssuesInSelectedCells):
		/// модуль строки каждого места на весь день, ролик и позиция кисти. Проверки — те же, что у клика (<see cref="PlaceOne"/>),
		/// и все до записи: места из модели набора; свежая акция и права, а если среди мест есть ожидающие строки — и право
		/// создавать модульные кампании; кампании мест — в свежем списке кампаний акции; ролик из списка и типов 1–3 (В-25);
		/// прайс-лист модуля каждого места.
		///
		/// Запись — по дням выделения, день — одной транзакцией (<see cref="RunDays"/>); внутри дня места по модулю, каждое — шаг
		/// с точкой сохранения (RangeSource.Step): ModuleIssueIUD вставляет ModuleIssue до своих проверок (AgitationMixError,
		/// hlp_IssueVerify), без отката к точке отказ оставил бы выпуск без строк Issue. Шаг: IsModuleExist → у ожидающей строки
		/// кампания (поиск внутри транзакции, иначе CampaignIUD — <see cref="EnsureCampaign"/>) → ModuleIssueIUD AddItem (номер
		/// выпуска из ответа; −1 — модуль не целиком). Отказ шага откатывает и кампанию, созданную в этом шаге, поэтому номер
		/// кампании между шагами не запоминается: следующий шаг той же строки снова ищет её в транзакции и при нужде создаёт.
		/// Бизнес-отказ — строка итога, пачка идёт дальше; иной сбой откатывает день и прерывает пачку. Пересчёт акции — один, в
		/// конце, своей транзакцией; акция прочитана свежей до записи, поэтому коррекция платежа подтверждённой акции считает
		/// разницу цены за всю пачку. Кампании ожидающих строк после коммита дня попадают в набор (<see cref="ModuleScope.CampaignIds"/>).
		/// </summary>
		public static ModuleBatchResult Place(ModuleScope s, IEnumerable<PlacementCell> cells, int rollerId, int position)
		{
			List<PlacementCell> places = DistinctCells(s, cells);
			ModuleBatchResult result = new ModuleBatchResult();
			if (places.Count == 0)
				return result;

			ActionOnMassmedia action = LoadForWrite(s);
			if (places.Any(c => c.ModuleDay.CampaignId == 0) && !(s.CanAddCampaigns && CanAddModuleCampaigns(action)))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));
			Roller roller = ModuleRoller(s, rollerId, action.FirmID);
			Dictionary<int, DataRow> campaigns = ModularCampaigns(action);
			HashSet<(int, int, int, DateTime)> checkedPricelists = new HashSet<(int, int, int, DateTime)>();
			foreach (PlacementCell cell in places)
			{
				ModuleDay m = cell.ModuleDay;
				int massmediaId = m.CampaignId == 0 ? m.Added.MassmediaId : CampaignStation(campaigns, m.CampaignId);
				if (checkedPricelists.Add((m.ModuleId, m.ModulePricelistId, massmediaId, cell.Date.Date)) && !IsPricelistOfDay(m, massmediaId, cell.Date.Date))
					throw new InvalidOperationException(Tr.Format("Прайс-лист модуля «{0}» на {1:dd.MM.yyyy} изменился. Обновите страницу.", m.ModuleName, cell.Date.Date));
			}

			// Шаги — по (день, модуль), внутри модуля — по строке: один порядок блокировок у всех пачек.
			List<(DateTime Day, List<PlacementCell> Cells)> days = places.GroupBy(c => c.Date.Date).OrderBy(g => g.Key)
				.Select(g => (g.Key, g.OrderBy(c => c.ModuleDay.ModuleId).ThenBy(c => c.Key).ToList())).ToList();
			using (OperationScope.Start($"ModulesPlace action={s.ActionId} cells={places.Count}"))
			{
				Stopwatch sw = Stopwatch.StartNew();
				RunDays(result, "place", "action " + s.ActionId, days.Select(d => (d.Day, d.Cells.Count)).ToList(), i =>
				{
					DayWork work = new DayWork();
					DateTime day = days[i].Day;
					WindowsSource.RunInTransaction(() =>
					{
						foreach (PlacementCell cell in days[i].Cells)
							RangeSource.Step(work.Steps, CellName(cell.ModuleDay, day), 1, () => PlaceStep(action, cell.ModuleDay, roller, position, day, work));
					});
					return work;
				}, work =>
				{
					foreach (KeyValuePair<int, AddedRow> campaign in work.PendingCampaigns)
						s.AddCampaign(campaign.Key, InfoOf(campaign.Value));
				}, (failure, daysLeft) => Tr.Format("Размещение прервано: {0} Выпуски этого дня не добавлены, добавленные ранее — сохранены. Не обработано дней: {1}.",
					ErrorManager.GetErrorMessage(failure), daysLeft));

				if (result.Done > 0)
					Recalculate(action, result, "place");
				Log.Info($"Modules place completed in {sw.ElapsedMilliseconds}ms action={s.ActionId} cells={places.Count} days={days.Count}" +
					$" roller={rollerId} position={position} placed={result.Done} notPlaced={result.NotDone} campaignsCreated={result.CreatedCampaignIds.Count}");
			}
			return result;
		}

		/// <summary>Шаг «Разместить ролик» в транзакции дня (см. <see cref="Place"/>): причина отказа или null.</summary>
		private static string PlaceStep(ActionOnMassmedia action, ModuleDay m, Roller roller, int position, DateTime day, DayWork work)
		{
			if (!IsModuleWhole(m.ModulePricelistId, day))
				return NotWhole(m, day).Message;
			int campaignId = m.CampaignId;
			bool created = false;
			if (campaignId == 0)
				campaignId = EnsureCampaign(action, m.Added, out created);
			int moduleIssueId = AddModuleIssue(action, campaignId, m, roller, position, day);
			if (moduleIssueId <= 0)
				return NotWhole(m, day).Message;

			work.Placed.Add(Placed(m, roller, day, campaignId, moduleIssueId, created));
			if (m.CampaignId == 0)
				work.PendingCampaigns[campaignId] = m.Added;
			if (created)
				work.CreatedCampaigns.Add(campaignId);
			return null;
		}

		/// <summary>Выпуски модулей в выделенных местах — для вопроса «Удалить выпуски» и чек-листа роликов.</summary>
		public sealed class CellIssues
		{
			internal CellIssues(List<DataRow> rows)
			{
				Rows = rows;
			}

			/// <summary>Строки ComboModuleIssuesRetrieve — по дню, модулю, кампании и номеру выпуска.</summary>
			internal List<DataRow> Rows { get; }

			public int Count => Rows.Count;

			/// <summary>Ролики выпусков без повторов; строки чек-листа с номерами из списка кисти — WindowsSource.RollerRows.</summary>
			public IReadOnlyList<int> RollerIds => Rows.Select(IssueRollerId).Distinct().ToList();

			/// <summary>Только выпуски этих роликов; null — все.</summary>
			public CellIssues Only(IReadOnlyCollection<int> rollerIds) =>
				rollerIds == null ? this : new CellIssues(Rows.Where(r => rollerIds.Contains(IssueRollerId(r))).ToList());
		}

		private static int IssueRollerId(DataRow issue) => Convert.ToInt32(issue[Roller.ParamNames.RollerId]);

		private static int IssueModuleIssueId(DataRow issue) => Convert.ToInt32(issue[Issue.ParamNames.ModuleIssueId]);

		/// <summary>
		/// Выпуски модулей строк-кампаний в выделенных местах — ComboModuleIssuesRetrieve @actionID, как десктоп комбо берёт их из
		/// своей панели (GetIssuesInDays): только кампании и модуля строки места за его день; места ожидающих строк пропускаются —
		/// выпусков у них нет. Повтор модуля в тот же день (В-18) — оба выпуска. Место не из набора — отказ.
		/// </summary>
		public static CellIssues IssuesInCells(ModuleScope s, IEnumerable<PlacementCell> cells)
		{
			HashSet<(int, int, DateTime)> keys = new HashSet<(int, int, DateTime)>();
			foreach (PlacementCell cell in DistinctCells(s, cells))
				if (cell.ModuleDay.CampaignId != 0)
					keys.Add((cell.ModuleDay.ModuleId, cell.ModuleDay.CampaignId, cell.Date.Date));
			List<DataRow> rows = new List<DataRow>();
			if (keys.Count > 0)
				foreach (DataRow issue in ComboModule.LoadIssues(s.ActionId).Rows)
					if (keys.Contains((IssueModuleId(issue), IssueCampaignId(issue), IssueDay(issue))))
						rows.Add(issue);
			return new CellIssues(rows.OrderBy(IssueDay).ThenBy(IssueModuleId).ThenBy(IssueCampaignId).ThenBy(IssueModuleIssueId).ToList());
		}

		/// <summary>Для строки итога: «дата — радиостанция — модуль — ролик».</summary>
		private static string IssueName(DataRow issue) => string.Format("{0:dd.MM.yyyy} — {1} — {2} — {3}", IssueDay(issue),
			Convert.ToString(issue[Campaign.ParamNames.MassmediaName]).Trim(), issue[ComboModule.ParamNames.ModuleName], issue[RollerNameColumn]);

		private const string RollerNameColumn = "rollerName";

		/// <summary>
		/// «Удалить выпуски» по выделенным местам (десктоп комбо — Del по ячейкам, DeleteIssuesInSelectedCells): снимок выпусков,
		/// который экран показал и пользователь подтвердил (<see cref="IssuesInCells"/>, при нескольких роликах — только выбранные,
		/// <see cref="CellIssues.Only"/>, как линейная вкладка); ролик выпуска заменили после вопроса — не удаляется. Права — до записи: акция и кампании
		/// (LoadForWrite) и «Удалить» выпуска модуля (сущность 130: веб проверяет её при DoAction, а запись здесь — LoadDataSet).
		///
		/// По дням транзакциями (<see cref="RunDays"/>), внутри дня — шаг с точкой сохранения на выпуск: ModuleIssueIUD DeleteItem
		/// уменьшает счётчики окон до своих проверок (PastIssue, DeadLineViolationDelete), без отката к точке отказ испортил бы
		/// счётчики. Удаление подтверждённых выпусков ближе DaysLog дней шлёт объявление администраторам на каждый выпуск модуля —
		/// как в десктопе.
		///
		/// В-21 — последним в транзакции дня: кампания из <paramref name="createdHereCampaignIds"/> (создана вкладкой в этом
		/// сеансе), у которой в этот день сняты выпуски и не осталось ни ModuleIssue, ни строк Issue (проверка — последней перед
		/// удалением), удаляется тем же путём, что «Удалить» в списке кампаний (ActionWorkspace.DeleteCampaigns: объект строки
		/// списка, CampaignIUD DeleteItem; право «Удалить» проверено до записи), шагом с точкой сохранения. Без права — остаётся
		/// (<see cref="ModuleBatchResult.KeptEmptyCampaignIds"/>, плашка по В-6); прочие опустевшие кампании не трогаются. Удалённые
		/// кампании уходят из набора. Пересчёт акции — один, в конце.
		/// </summary>
		public static ModuleBatchResult Remove(ModuleScope s, CellIssues accepted, ISet<int> createdHereCampaignIds)
		{
			// Снимают ровно то, что пользователь видел и подтвердил (IssuesInCells → вопрос или чек-лист роликов), как десктоп комбо
			// и «Рекламные окна»: выпуск, поставленный в эти места после вопроса, не трогается.
			List<DataRow> issues = accepted.Rows;
			ModuleBatchResult result = new ModuleBatchResult();
			if (issues.Count == 0)
				return result;

			ActionOnMassmedia action = LoadForWrite(s);
			if (!EntityManager.GetEntity((int)Entities.ModuleIssue).IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal))
				throw new InvalidOperationException(Tr.T(Properties.Resources.OperationNotAllowed));

			// Кампании выпусков, созданные вкладкой: строка свежего списка (по ней удаляется) и право «Удалить» — до записи.
			Dictionary<int, DataRow> campaigns = ModularCampaigns(action);
			Entity listEntity = ActionWorkspace.CampaignListEntity();
			List<DataRow> createdHere = new List<DataRow>();
			HashSet<int> deletable = new HashSet<int>();
			foreach (int campaignId in issues.Select(IssueCampaignId).Distinct())
				if (createdHereCampaignIds != null && createdHereCampaignIds.Contains(campaignId) && campaigns.TryGetValue(campaignId, out DataRow row))
				{
					createdHere.Add(row);
					if (listEntity.CreateObject(row).IsActionEnabled(Constants.EntityActions.Delete, ViewType.Journal))
						deletable.Add(campaignId);
				}

			List<(DateTime Day, List<DataRow> Issues)> days = issues.GroupBy(IssueDay).OrderBy(g => g.Key).Select(g => (g.Key, g.ToList())).ToList();
			using (OperationScope.Start($"ModulesRemove action={s.ActionId} issues={issues.Count}"))
			{
				Stopwatch sw = Stopwatch.StartNew();
				RunDays(result, "remove", "action " + s.ActionId, days.Select(d => (d.Day, d.Issues.Count)).ToList(), i =>
				{
					DayWork work = new DayWork();
					WindowsSource.RunInTransaction(() =>
					{
						HashSet<int> touched = new HashSet<int>();
						foreach (DataRow issue in days[i].Issues)
							if (RangeSource.Step(work.Steps, IssueName(issue), 1, () => DeleteStep(IssueModuleIssueId(issue), IssueRollerId(issue))))
								touched.Add(IssueCampaignId(issue));
						foreach (DataRow campaign in createdHere.Where(c => touched.Contains(WindowsSource.CampaignIdOf(c))))
							DeleteIfEmpty(listEntity, campaign, deletable.Contains(WindowsSource.CampaignIdOf(campaign)), work);
					});
					return work;
				}, work =>
				{
					foreach (int campaignId in work.DeletedCampaigns)
						s.RemoveCampaign(campaignId);
				}, (failure, daysLeft) => Tr.Format("Удаление прервано: {0} Выпуски этого дня не удалены, выпуски прежних дней удалены. Не обработано дней: {1}.",
					ErrorManager.GetErrorMessage(failure), daysLeft));

				if (result.Done > 0)
					Recalculate(action, result, "remove");
				Log.Info($"Modules remove completed in {sw.ElapsedMilliseconds}ms action={s.ActionId} issues={issues.Count} days={days.Count}" +
					$" rollers={string.Join(",", accepted.RollerIds)} removed={result.Done} notRemoved={result.NotDone}" +
					$" campaignsDeleted={result.DeletedCampaignIds.Count} keptEmpty={result.KeptEmptyCampaignIds.Count}");
			}
			return result;
		}

		/// <summary>
		/// Шаг «Удалить выпуски»: выпуск модуля заново (ModuleIssueRetrieve @moduleIssueId на соединении транзакции; нет — «Выпуска
		/// уже нет.») и ModuleIssueIUD DeleteItem с его полной строкой: без @campaignID процедура молча пропустила бы проверки
		/// прошлого и закрытого периода. Причина отказа или null.
		/// </summary>
		private static string DeleteStep(int moduleIssueId, int acceptedRollerId)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Issue.ParamNames.ModuleIssueId] = moduleIssueId;
			DataTable fresh = DataAccessor.LoadDataSet("ModuleIssueRetrieve", parameters).Tables[0];
			if (fresh.Rows.Count == 0)
				return Tr.T("Выпуска уже нет.");
			DataRow row = fresh.Rows[0];
			// Ролик выпуска заменили после вопроса — пользователь удалять его не подтверждал.
			if (IssueRollerId(row) != acceptedRollerId)
				return Tr.T("Ролик выпуска изменили после вопроса — выпуск не удалён.");

			parameters = DataAccessor.CreateParametersDictionary();
			parameters[Constants.ParamNames.ActionName] = Constants.Actions.Delete;
			parameters[Issue.ParamNames.ModuleIssueId] = moduleIssueId;
			parameters[Campaign.ParamNames.CampaignId] = row[Campaign.ParamNames.CampaignId];
			parameters[Module.ParamNames.ModuleId] = row[Module.ParamNames.ModuleId];
			parameters[ModulePricelist.ParamNames.ModulePriceListID] = row[ModulePricelist.ParamNames.ModulePriceListID];
			parameters[Issue.ParamNames.IssueDate] = row[Issue.ParamNames.IssueDate];
			parameters[Roller.ParamNames.RollerId] = row[Roller.ParamNames.RollerId];
			parameters[Issue.ParamNames.PositionId] = row[Issue.ParamNames.PositionId];
			parameters[Action.ParamNames.IsConfirmed] = row[Action.ParamNames.IsConfirmed];
			DataAccessor.LoadDataSet("ModuleIssueIUD", parameters);
			return null;
		}

		/// <summary>
		/// В-21 в транзакции дня: кампания, созданная вкладкой, без выпусков модулей и строк Issue (проверка — сейчас, на соединении
		/// транзакции) удаляется, как «Удалить» в списке кампаний — объект строки списка, CampaignIUD DeleteItem, — шагом с точкой
		/// сохранения; без права или при отказе остаётся (отказ — строкой итога).
		/// </summary>
		private static void DeleteIfEmpty(Entity listEntity, DataRow campaign, bool canDelete, DayWork work)
		{
			int campaignId = WindowsSource.CampaignIdOf(campaign);
			if (!IsCampaignEmpty(campaignId, ParseHelper.GetInt32FromObject(campaign[Campaign.ParamNames.MassmediaId], 0)))
				return;
			if (!canDelete)
			{
				work.KeptEmpty.Add(campaignId);
				return;
			}
			PresentationObject target = listEntity.CreateObject(campaign);
			WindowsSource.BatchResult step = new WindowsSource.BatchResult();
			if (RangeSource.Step(step, ActionWorkspace.CampaignName(campaign), 1, () => { target.Delete(silenceFlag: true); }))
				work.DeletedCampaigns.Add(campaignId);
			else
			{
				work.KeptEmpty.Add(campaignId);
				foreach (DataRow refused in step.Errors.Rows)
					work.Notes.Add((Convert.ToString(refused["name"]), Convert.ToString(refused["description"])));
			}
		}

		/// <summary>Нет ни выпусков модулей (ModuleIssueRetrieve @campaignID), ни строк Issue (IssuesDays) — у модульной кампании других выпусков не бывает.</summary>
		private static bool IsCampaignEmpty(int campaignId, int massmediaId)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Campaign.ParamNames.CampaignId] = campaignId;
			parameters["showUnconfirmed"] = true;
			if (DataAccessor.LoadDataSet("ModuleIssueRetrieve", parameters).Tables[0].Rows.Count > 0)
				return false;
			parameters = DataAccessor.CreateParametersDictionary();
			parameters[Campaign.ParamNames.CampaignId] = campaignId;
			parameters[Massmedia.ParamNames.MassmediaId] = massmediaId;
			return DataAccessor.LoadDataSet("IssuesDays", parameters).Tables[0].Rows.Count == 0;
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
		/// с остатком по <paramref name="showUnconfirmed"/> и пометкой заблокированных. У добавленной строки без кампании
		/// выпусков нет — окна тарифов модуля.
		/// </summary>
		public static ModuleDayDetails Details(ModuleScope s, PlacementCell cell, bool showUnconfirmed)
		{
			ModuleDay m = cell.ModuleDay;
			if (!IsOfScope(s, m))
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
