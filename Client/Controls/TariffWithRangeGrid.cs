using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;
using Merlin.Classes;
using Merlin.Classes.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Controls
{
	internal partial class TariffWithRangeGrid : TariffGrid, IRollerGrid
	{
        private static readonly ILog Log = LogManager.GetLogger(typeof(TariffWithRangeGrid));
        private static int _tempIssueId = 0;

        private const string MinBroadcastColumnName = "minBroadcast";
		private const string MaxBroadcastColumnName = "maxBroadcast";
        private readonly ActionOnMassmedia _action;
        // Число кампаний, по которым сейчас работает веер (счётчик выпусков в шапке дня).
        // Меняется вместе с SelectedCampaignIds — по одной радиостанции может идти
        // несколько кампаний акции (разный тип оплаты/агентство, см. UIX_Campaign).
        private int _massmediasCount;
		private Dictionary<string, string> _timeResolver;
        private bool showRollerNumbers;
        private Dictionary<int, int> rollerNumbers;
        // Частичные (красные) группы текущей недели по датам слотов — батч-кэш для
        // GetRollerNumbersText, см. RefreshPartialRollerGroups.
        private Dictionary<DateTime, IList<SlotIssueGroup>> _partialRollerGroupsByDate;
        // Чужие акции той же фирмы по датам слотов — для подсказки бирюзовых/оранжевых
        // ячеек, см. GetOtherFirmActions. Загружается вместе с раскраской окон (populateGrid).
        private Dictionary<DateTime, List<OtherFirmAction>> _otherFirmActionsByDate;
        // Ролики чужих акций той же фирмы по датам слотов — для номеров роликов
        // в бирюзовых/оранжевых ячейках, см. GetRollerNumbersText. Приезжают тем же
        // запросом, что и раскраска окон (populateGrid).
        private Dictionary<DateTime, List<OtherFirmRoller>> _otherFirmRollersByDate;

        public ActionOnMassmedia Action
        {
			get => _action;
        }

        /// <summary>
        /// Линейные кампании акции, в контексте которых работает веер: сетка, добавление,
        /// удаление и «Добавленные выпуски» — только по ним. null — все линейные кампании
        /// акции (так же трактуют NULL и SQL-процедуры).
        /// Задаётся чек-листом кампаний на EditIssuesForm, применяется по «Обновить».
        /// </summary>
        public IList<int> SelectedCampaignIds { get; private set; }

        /// <summary>
        /// Сменить набор кампаний веера. Пересобирает «Добавленные выпуски» (пересечение
        /// слотов считается уже по выбранным кампаниям); сетку перезабрасывает вызывающий.
        /// Если выбор не изменился — ничего не делает: метод зовётся на каждом RefreshGrid
        /// формы, а пересборка AddedIssues ходит в базу за выпусками каждой кампании.
        /// </summary>
        public void SetSelectedCampaigns(IList<int> campaignIds, int campaignsCount)
        {
            if (IsSameSelection(campaignIds))
                return;

            SelectedCampaignIds = campaignIds;
            _massmediasCount = campaignsCount;
            InitAddedIssuesData();
        }

        private bool IsSameSelection(IList<int> campaignIds)
        {
            if (SelectedCampaignIds == null || campaignIds == null)
                return SelectedCampaignIds == null && campaignIds == null;
            if (SelectedCampaignIds.Count != campaignIds.Count)
                return false;
            return !campaignIds.Except(SelectedCampaignIds).Any();
        }

        // CSV для SQL-процедур; null — все линейные кампании акции.
        private string CampaignIdsParameter
        {
            get { return Merlin.Classes.Action.BuildCampaignIdsCsv(SelectedCampaignIds); }
        }

        public TariffWithRangeGrid(ActionOnMassmedia action, int massmediasCount)
		{
			InitializeComponent();
			InitializeDelegates();
			FixedCols = 1;
			// Выставлять monday/startDate/finishDate напрямую здесь бесполезно: базовый
			// TariffGrid.SetGridCaptions() (вызывается позже, из populateGrid) молча
			// пересчитывает их заново из _currentDate — тем же способом, что и обычная
			// навигация по неделям (CurrentDate). Поэтому нужную неделю задаём именно через
			// _currentDate, а не через startDate/monday/finishDate напрямую.
			_currentDate = GetInitialDisplayDate(action);
            _action = action;
            _massmediasCount = massmediasCount;
            InitAddedIssuesData();
		}

		/// <summary>
		/// Открывать форму сразу на неделе, где у акции реально есть контент, а не на
		/// текущей календарной неделе (та вообще может не иметь отношения к акции — тогда
		/// пользователь видит пустую сетку без единой подсветки и вручную листает назад).
		/// Берём самое раннее Campaign.startDate среди линейных кампаний акции.
		/// Campaign.startDate для типа 1 пересчитывается в ActionRecalculate как
		/// MIN(TariffWindow.dayOriginal) по выпускам кампании — то есть это фактически дата
		/// самого раннего выпуска, а не отдельно заданная плановая дата. Если выпусков ещё
		/// нет ни у одной кампании (веер открывают впервые) — startDate у всех NULL,
		/// откатываемся на сегодня, как и раньше.
		/// Не action.Campaigns(): тот метод шлёт общий словарь Action.parameters, который
		/// используется и для других, не всегда action-уровневых, походов на этом же объекте.
		/// Отдельный запрос с чистым набором параметров — только actionID — снимает любые
		/// сомнения на этот счёт, независимо от того, задевает это в реальности или нет.
		/// </summary>
		private static DateTime GetInitialDisplayDate(ActionOnMassmedia action)
		{
			DateTime? earliest = null;

			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Merlin.Classes.Action.ParamNames.ActionId] = action.ActionId;
			DataTable campaigns = DataAccessor.LoadDataSet("Campaigns", parameters).Tables[0];

			foreach (DataRow row in campaigns.Rows)
			{
				if (ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignTypeId], 0)
				    != (int)Campaign.CampaignTypes.Simple)
					continue;

				DateTime start = ParseHelper.GetDateTimeFromObject(row[Campaign.ParamNames.StartDate], DateTime.MinValue);
				if (start == DateTime.MinValue)
					continue;

				if (earliest == null || start < earliest.Value)
					earliest = start;
			}

			return earliest ?? DateTime.Today.Date;
		}

	    private void InitAddedIssuesData()
	    {
			AddedIssues = _action.BuildAddedIssuesTable(SelectedCampaignIds);
        }

	    private DataTable Data { get; set; }
        public DataTable AddedIssues { get; set; }

        /// <summary>
        /// В сетке есть хотя бы один слот. Пусто, когда в акции нет линейных кампаний
        /// (веер модульной/спонсорской акции): расставлять и редактировать нечего.
        /// Заполняется после populateGrid / RefreshGrid.
        /// </summary>
        public bool HasSlots => Data != null && Data.Rows.Count > 0;

        // Режим ячейки: вместо остатка свободного времени — номера роликов текущей акции
        // (см. RollerNumbers), размещённых в этом слоте. Карта номеров фиксируется снаружи
        // (CampaignForm) на момент включения режима, сама повторно не пересчитывается.
        public bool ShowRollerNumbers
        {
            get { return showRollerNumbers; }
            set { showRollerNumbers = value; }
        }

        // rollerID -> номер ролика (как в колонке "№" grdRollers на момент включения ShowRollerNumbers)
        public Dictionary<int, int> RollerNumbers
        {
            get { return rollerNumbers; }
            set { rollerNumbers = value; }
        }

		private DateTime? MinBroadCast { get; set; }
		private DateTime? MaxBroadCast { get; set; }

		private void InitializeDelegates()
		{
			loadPricelist = delegate
			{
				pricelist = new MassmediaPricelist();
				pricelist[Pricelist.ParamNames.StartDate] = DateTime.MinValue;
				pricelist[Pricelist.ParamNames.FinishDate] = DateTime.MaxValue;
			};

			populateGrid = delegate
			{
				Dictionary<string, object> dictionary = DataAccessor.CreateParametersDictionary();
				dictionary.Add("dateStart", StartDate);
				dictionary.Add("actionID", _action.ActionId);
				dictionary.Add(Campaign.ParamNames.CampaignIds, CampaignIdsParameter);
				if (_advertTypePresence != AdvertTypePresences.Undefined)
					dictionary.Add("advertTypeID", _advertType.IDs[0]);
				DataSet dataSet = DataAccessor.LoadDataSet("TariffWindowWithRange", dictionary);
				Data = dataSet.Tables[0];

				object oMaxBroadCast = dataSet.Tables[1].Rows[0][MaxBroadcastColumnName];
				MaxBroadCast = StringUtil.IsDBNullOrNull(oMaxBroadCast)
				               	? null : (DateTime?)ParseHelper.GetDateTimeFromObject(oMaxBroadCast, DateTime.Now);
				object oMinBroadCast = dataSet.Tables[1].Rows[0][MinBroadcastColumnName];
				MinBroadCast = StringUtil.IsDBNullOrNull(oMinBroadCast)
								? null : (DateTime?)ParseHelper.GetDateTimeFromObject(oMinBroadCast, DateTime.Now);

				if (MaxBroadCast.HasValue && MinBroadCast.HasValue)
					_tariffWindows = new ITariffWindow[(int)(MaxBroadCast.Value.AddDays(1) - MinBroadCast.Value).TotalHours * 2, 7];
				else
					_tariffWindows = null;

                PopulateGridTable(dataSet.Tables[2]);

                _otherFirmActionsByDate = new Dictionary<DateTime, List<OtherFirmAction>>();
                foreach (DataRow row in dataSet.Tables[3].Rows)
                {
                    DateTime windowDate = ParseHelper.GetDateTimeFromObject(row["date"], DateTime.MinValue);
                    if (windowDate == DateTime.MinValue)
                        continue;

                    if (!_otherFirmActionsByDate.TryGetValue(windowDate, out List<OtherFirmAction> actions))
                        _otherFirmActionsByDate[windowDate] = actions = new List<OtherFirmAction>();

                    actions.Add(new OtherFirmAction
                    {
                        ActionId = ParseHelper.GetInt32FromObject(row["actionID"], 0),
                        OwnerName = StringUtil.GetStringOrEmpty(row["ownerName"]),
                        HasConfirmed = ParseHelper.GetInt32FromObject(row["hasConfirmed"], 0) == 1
                    });
                }

                _otherFirmRollersByDate = new Dictionary<DateTime, List<OtherFirmRoller>>();
                foreach (DataRow row in dataSet.Tables[4].Rows)
                {
                    DateTime windowDate = ParseHelper.GetDateTimeFromObject(row["date"], DateTime.MinValue);
                    if (windowDate == DateTime.MinValue)
                        continue;

                    if (!_otherFirmRollersByDate.TryGetValue(windowDate, out List<OtherFirmRoller> rollers))
                        _otherFirmRollersByDate[windowDate] = rollers = new List<OtherFirmRoller>();

                    rollers.Add(new OtherFirmRoller
                    {
                        RollerId = ParseHelper.GetInt32FromObject(row[Roller.ParamNames.RollerId], 0),
                        PositionId = ParseHelper.GetInt32FromObject(row[Issue.ParamNames.PositionId], 0),
                        HasConfirmed = ParseHelper.GetInt32FromObject(row["hasConfirmed"], 0) == 1
                    });
                }
            };

			updateDB = delegate (DataGridViewCell cell)
			{
			   AddIssuesRange(cell);
			};

			onGridPopulated = delegate
			{
                MarkCells();
                RefreshWindowsColors();
                if (MinBroadCast.HasValue)
                {
                    foreach (DataRow row in AddedIssues.Rows)
                    {
                        DateTime time = ParseHelper.GetDateTimeFromObject(row["issueDate"], DateTime.MinValue);
                        if (time != DateTime.MinValue)
                        {
                            int index = (time.Date - StartDate).Days;
                            if (time.Hour < MinBroadCast.Value.Hour ||
                                (time.Hour < MinBroadCast.Value.Hour && time.Minute < MinBroadCast.Value.Minute))
                                index--;
                            if (index >= 0 && index < 7)
                                ChangeIssuesCounter(index + FixedCols, _massmediasCount);
                        }
                    }
                }

                if (showRollerNumbers)
                    RefreshCellTexts();
			};
		}

		public DataRow AddIssuesRange(DateTime windowDate)
		{
			return AddIssuesRange(windowDate, false);
		}

		public DataRow AddIssuesRange(DateTime windowDate, bool ignoreWindowsWithTheSameFirmIssue,
			bool recalculate = true)
		{
			return AddIssuesRange(windowDate, Roller, ignoreWindowsWithTheSameFirmIssue, recalculate);
		}

		// Ролик передаётся явно — для генерации по Шаблону 3, где на разные слоты может
		// попасть разный ролик (в отличие от свойства Roller — единого выбора на форме).
		public DataRow AddIssuesRange(DateTime windowDate, Roller roller, bool ignoreWindowsWithTheSameFirmIssue,
			bool recalculate = true)
		{
			return AddIssuesRange(windowDate, roller, RollerPosition, ignoreWindowsWithTheSameFirmIssue, recalculate);
		}

		// Позиция передаётся явно — для drag-and-drop переноса, где позиция сохраняется
		// от исходного выпуска, а не берётся из текущего выбора на форме.
		public DataRow AddIssuesRange(DateTime windowDate, Roller roller, RollerPositions position,
			bool ignoreWindowsWithTheSameFirmIssue, bool recalculate = true)
		{
            using (OperationScope.Start($"AddIssuesRange date={windowDate:yyyy-MM-dd HH:mm} recalc={recalculate}"))
            {
                Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
                parameters[ActionOnMassmedia.ParamNames.ActionId] = _action.ActionId;
                parameters["issueDate"] = windowDate;
                parameters["rollerID"] = roller.RollerId;
                parameters["rollerDuration"] = roller.Duration;
                parameters["positionId"] = (int)position;
                parameters["considerUnconfirmed"] = ShowUnconfirmed ? 1 : 0;
                parameters["ignoreWindowsWithTheSameFirmIssue"] = ignoreWindowsWithTheSameFirmIssue ? 1 : 0;
                parameters[Campaign.ParamNames.CampaignIds] = CampaignIdsParameter;
                if (Grantor != null)
                    parameters["grantorID"] = Grantor.Id;

                DataAccessor.ExecuteNonQuery("AddRangeIssues", parameters);
                if (recalculate)
                    _action.Recalculate();

                DataRow row = AddedIssues.NewRow();
                row[Issue.ParamNames.IssueId] = System.Threading.Interlocked.Decrement(ref _tempIssueId);
                Debug.WriteLine(row[Issue.ParamNames.IssueId]);
                row["issueDate"] = windowDate;
                row[Entity.ParamNames.NAME] = roller.Name;
                row[Roller.ParamNames.RollerId] = roller.RollerId;
                row["durationString"] = roller.DurationString;
                row["RowNum"] = Guid.NewGuid();
                row[Issue.ParamNames.PositionName] = Issue.GetPositionDisplayName(position);
                row[Issue.ParamNames.PositionId] = (int)position;
                row[ActionOnMassmedia.ParamNames.ActionId] = _action.ActionId;

                // replace AddedIssues.Rows.Add(row); with sorted insert
                InsertIssueRowSorted(row);

                return row;
            }
		}

		/// <summary>
		/// Выпуски одного слота с одинаковым роликом и позицией и кампании, в которых они
		/// стоят. Единица операции для «частичных» (красных) слотов: там выпуск есть не во
		/// всех выбранных кампаниях, и удалять/переносить его надо ровно по этим кампаниям,
		/// иначе перенос размножит рекламу на остальные (AddRangeIssues ставит выпуск во все
		/// кампании, переданные в @campaignIDs).
		/// </summary>
		public class SlotIssueGroup
		{
			public int RollerId;
			public string RollerName;
			public int Duration;
			public string DurationString;
			public RollerPositions Position;
			public readonly List<int> CampaignIds = new List<int>();
		}

		/// <summary>
		/// Фактическое содержимое слота по выбранным кампаниям, сгруппированное по паре
		/// «ролик + позиция». Ходит в базу: в AddedIssues частичных слотов нет.
		/// </summary>
		public IList<SlotIssueGroup> GetSlotIssueGroups(DateTime windowDate)
		{
			IList<SlotIssueGroup> groups;
			return GetSlotIssueGroups(new[] { windowDate }).TryGetValue(windowDate, out groups)
				? groups : new List<SlotIssueGroup>();
		}

		/// <summary>
		/// То же самое, но сразу по нескольким слотам одним запросом — при массовом
		/// удалении/замене по выделению в десятки окон раньше это был отдельный
		/// круговой запрос на каждое окно (заметная пауза перед диалогом подтверждения,
		/// см. RangeSlotIssues.sql). Ключ результата — тот же DateTime, что был передан.
		/// </summary>
		public Dictionary<DateTime, IList<SlotIssueGroup>> GetSlotIssueGroups(IEnumerable<DateTime> windowDates)
		{
			DataTable table = FetchSlotIssues(windowDates);
			Dictionary<DateTime, Dictionary<string, SlotIssueGroup>> groupsByDate =
				new Dictionary<DateTime, Dictionary<string, SlotIssueGroup>>();
			Dictionary<DateTime, IList<SlotIssueGroup>> result = new Dictionary<DateTime, IList<SlotIssueGroup>>();

			foreach (DataRow row in table.Rows)
			{
				DateTime windowDate = ParseHelper.GetDateTimeFromObject(row["requestedIssueDate"], DateTime.MinValue);
				if (!groupsByDate.TryGetValue(windowDate, out Dictionary<string, SlotIssueGroup> groups))
				{
					groups = new Dictionary<string, SlotIssueGroup>();
					groupsByDate.Add(windowDate, groups);
					result.Add(windowDate, new List<SlotIssueGroup>());
				}

				int rollerId = ParseHelper.GetInt32FromObject(row[Roller.ParamNames.RollerId], 0);
				int positionId = ParseHelper.GetInt32FromObject(row[Issue.ParamNames.PositionId], 0);
				string key = rollerId + "/" + positionId;

				if (!groups.TryGetValue(key, out SlotIssueGroup group))
				{
					group = new SlotIssueGroup
					{
						RollerId = rollerId,
						RollerName = StringUtil.GetStringOrEmpty(row["rollerName"]),
						Duration = ParseHelper.GetInt32FromObject(row[Roller.ParamNames.Duration], 0),
						DurationString = StringUtil.GetStringOrEmpty(row["durationString"]),
						Position = (RollerPositions)positionId
					};
					groups.Add(key, group);
					result[windowDate].Add(group);
				}

				group.CampaignIds.Add(ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignId], 0));
			}

			return result;
		}

		// ISO 8601 с "T": на сервере с русским @@LANGUAGE 'YYYY-MM-DD HH:MM:SS' парсится
		// как 'YYYY-DD-MM' (день/месяц переставлены) — с "T" формат однозначен всегда.
		private const string SqlDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

		private DataTable FetchSlotIssues(IEnumerable<DateTime> windowDates)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Merlin.Classes.Action.ParamNames.ActionId] = _action.ActionId;
			parameters["issueDates"] = string.Join(",", windowDates.Select(d => d.ToString(SqlDateTimeFormat)));
			parameters[Campaign.ParamNames.CampaignIds] = CampaignIdsParameter;

			return DataAccessor.LoadDataSet("RangeSlotIssues", parameters).Tables[0];
		}

		/// <summary>
		/// Одна строка выпуска в слоте (без группировки по ролику/позиции, в отличие от
		/// <see cref="GetSlotIssueGroups(DateTime)"/>) — для массовой замены ролика
		/// (EditIssuesForm.ReplaceRollerInSelectedWindows), где нужен именно текущий ролик и
		/// исходное окно каждого отдельного выпуска, а не агрегат по слоту.
		/// WindowDate — какому из запрошенных слотов принадлежит строка (удаление дублей и
		/// выравнивание роликов считают выпуски по каждому окну отдельно).
		/// </summary>
		public class SlotIssueRow
		{
			public DateTime WindowDate;
			public int CampaignId;
			public int RollerId;
			public string RollerName;
			public int Duration;
			public int PositionId;
			public int OriginalWindowId;
			public DateTime WindowDayOriginal;
		}

		public IList<SlotIssueRow> GetSlotIssueRows(DateTime windowDate)
		{
			return GetSlotIssueRows(new[] { windowDate });
		}

		/// <summary>
		/// То же самое сразу по нескольким слотам одним запросом — см. GetSlotIssueGroups
		/// (IEnumerable&lt;DateTime&gt;) про причину. Строки всех слотов возвращаются одним
		/// плоским списком: вызывающему (замена ролика) не важно, из какого именно
		/// выделенного окна взялась строка, только OriginalWindowId/WindowDayOriginal.
		/// </summary>
		public IList<SlotIssueRow> GetSlotIssueRows(IEnumerable<DateTime> windowDates)
		{
			DataTable table = FetchSlotIssues(windowDates);
			List<SlotIssueRow> result = new List<SlotIssueRow>();

			foreach (DataRow row in table.Rows)
			{
				result.Add(new SlotIssueRow
				{
					WindowDate = ParseHelper.GetDateTimeFromObject(row["requestedIssueDate"], DateTime.MinValue),
					CampaignId = ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignId], 0),
					RollerId = ParseHelper.GetInt32FromObject(row[Roller.ParamNames.RollerId], 0),
					RollerName = StringUtil.GetStringOrEmpty(row["rollerName"]),
					Duration = ParseHelper.GetInt32FromObject(row[Roller.ParamNames.Duration], 0),
					PositionId = ParseHelper.GetInt32FromObject(row[Issue.ParamNames.PositionId], 0),
					OriginalWindowId = ParseHelper.GetInt32FromObject(row["originalWindowID"], 0),
					WindowDayOriginal = ParseHelper.GetDateTimeFromObject(row["windowDayOriginal"], DateTime.MinValue)
				});
			}

			return result;
		}

		/// <summary>
		/// Результат <see cref="CheckFirmConflict"/> — сигнал для диалога подтверждения переноса
		/// (EditIssuesForm.RangeGrid_DragDrop), не блокирует сам перенос.
		/// </summary>
		public class FirmConflictInfo
		{
			public bool HasConflict;
			public bool AnyConfirmed;
		}

		/// <summary>
		/// Есть ли в целевом получасе, на станциях переносимых кампаний, уже выпуск этой же
		/// фирмы — из любой акции (текущей или чужой, подтверждённой или нет). Только для
		/// подтверждения переноса мышью; реальную проверку при записи по-прежнему делает
		/// AddRangeIssues (@ignoreWindowsWithTheSameFirmIssue).
		/// </summary>
		public FirmConflictInfo CheckFirmConflict(IList<int> campaignIds, DateTime windowDate)
		{
			if (campaignIds == null || campaignIds.Count == 0)
				return new FirmConflictInfo();

			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Merlin.Classes.Action.ParamNames.ActionId] = _action.ActionId;
			parameters["issueDate"] = windowDate;
			parameters[Campaign.ParamNames.CampaignIds] = Merlin.Classes.Action.BuildCampaignIdsCsv(campaignIds);

			DataTable table = DataAccessor.LoadDataSet("RangeSlotFirmConflict", parameters).Tables[0];
			if (table.Rows.Count == 0)
				return new FirmConflictInfo();

			DataRow row = table.Rows[0];
			return new FirmConflictInfo
			{
				HasConflict = ParseHelper.GetBooleanFromObject(row["hasConflict"], false),
				AnyConfirmed = ParseHelper.GetBooleanFromObject(row["anyConfirmed"], false)
			};
		}

		/// <summary>
		/// Чужая акция той же фирмы, у которой в слоте есть выпуск (подсказка бирюзовой/
		/// оранжевой ячейки). HasConfirmed — есть ли среди её выпусков в этом слоте хотя бы
		/// один подтверждённый (для текста подсказки, по аналогии с диалогом переноса).
		/// </summary>
		public class OtherFirmAction
		{
			public int ActionId;
			public string OwnerName;
			public bool HasConfirmed;
		}

		/// <summary>
		/// Ролик чужой акции той же фирмы, стоящий в слоте (бирюзовые/оранжевые
		/// ячейки). В режиме номеров роликов показывается наравне со своими: список
		/// роликов на форме — фирменный (Firm.GetRollers), так что номер чужого выпуска
		/// находится в той же карте RollerNumbers.
		/// </summary>
		public class OtherFirmRoller
		{
			public int RollerId;
			public int PositionId;
			public bool HasConfirmed;
		}

		/// <summary>
		/// Чужие акции той же фирмы, у которых есть выпуск в этом получасе — для подсказки
		/// бирюзовых/оранжевых ячеек. Данные уже загружены вместе с раскраской окон
		/// (см. populateGrid, TariffWindowWithRange.sql, п.9) — похода в базу на ховер нет.
		/// </summary>
		public IList<OtherFirmAction> GetOtherFirmActions(DateTime windowDate)
		{
			return _otherFirmActionsByDate != null &&
			       _otherFirmActionsByDate.TryGetValue(windowDate, out List<OtherFirmAction> actions)
				? actions
				: (IList<OtherFirmAction>)new List<OtherFirmAction>();
		}

		/// <summary>
		/// Удалить выпуски группы — только в тех кампаниях, где они есть. AddedIssues не
		/// трогаем: частичного слота там нет по определению.
		/// </summary>
		public void DeleteSlotIssueGroup(SlotIssueGroup group, DateTime windowDate)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Merlin.Classes.Action.ParamNames.ActionId] = _action.ActionId;
			parameters["issueDate"] = windowDate;
			parameters["rollerID"] = group.RollerId;
			parameters["positionId"] = (int)group.Position;
			parameters[Campaign.ParamNames.CampaignIds] =
				Merlin.Classes.Action.BuildCampaignIdsCsv(group.CampaignIds);
			if (Grantor != null)
				parameters["grantorID"] = Grantor.Id;

			DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters);
		}

		/// <summary>
		/// Поставить выпуски группы в другое окно — в том же составе кампаний, что и на
		/// исходном слоте (см. SlotIssueGroup). Пересчёт акции — на вызывающем.
		/// </summary>
		public void AddSlotIssueGroup(SlotIssueGroup group, DateTime windowDate)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Merlin.Classes.Action.ParamNames.ActionId] = _action.ActionId;
			parameters["issueDate"] = windowDate;
			parameters["rollerID"] = group.RollerId;
			parameters["rollerDuration"] = group.Duration;
			parameters["positionId"] = (int)group.Position;
			parameters["considerUnconfirmed"] = ShowUnconfirmed ? 1 : 0;
			parameters["ignoreWindowsWithTheSameFirmIssue"] = 0;
			parameters[Campaign.ParamNames.CampaignIds] =
				Merlin.Classes.Action.BuildCampaignIdsCsv(group.CampaignIds);
			if (Grantor != null)
				parameters["grantorID"] = Grantor.Id;

			DataAccessor.ExecuteNonQuery("AddRangeIssues", parameters);
		}

		/// <summary>
		/// Пересобирает in-memory таблицу AddedIssues из БД. Нужно после отката транзакции
		/// переноса: AddIssuesRange успевает дописать строки в AddedIssues до отката, и
		/// таблица расходится с фактическим состоянием базы.
		/// </summary>
		public void RebuildAddedIssues()
		{
			InitAddedIssuesData();
		}

		public List<PresentationObject> DeleteIssuesRange(DateTime windowDate)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[Merlin.Classes.Action.ParamNames.ActionId] = _action.ActionId;
			parameters["issueDate"] = windowDate;
			parameters["rollerID"] = Roller.RollerId;
			parameters["positionId"] = (int)RollerPosition;
			parameters[Campaign.ParamNames.CampaignIds] = CampaignIdsParameter;
			if (Grantor != null)
				parameters["grantorID"] = Grantor.Id;
			DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters);
			_action.Recalculate();

			DataRow rowToDelete = null;
			foreach (DataRow row in AddedIssues.Rows)
			{
				DateTime issueDate = ParseHelper.GetDateTimeFromObject(row["issueDate"], DateTime.MinValue);
				int rollerId = ParseHelper.GetInt32FromObject(row["rollerID"], 0);
				int positionId = ParseHelper.GetInt32FromObject(row["positionID"], 0);
				if (issueDate == windowDate && rollerId == Roller.RollerId && positionId == (int)RollerPosition)
				{
					rowToDelete = row;
					break;
				}
			}

			if (rowToDelete == null)
				return new List<PresentationObject>();

			PresentationObject issue = new RollerIssue(rowToDelete);
			AddedIssues.Rows.Remove(rowToDelete);
			return new List<PresentationObject> { issue };
		}

        private void AddIssuesRange(DataGridViewCell cell)
	    {
			try
			{
				AddIssuesRange(GetTariffWindow(cell).WindowDate);
				MarkCellAsHavingCurrentCampaignIssues(cell.RowIndex, cell.ColumnIndex);
				ChangeIssuesCounter(cell.ColumnIndex, _massmediasCount);
				RefreshGrid();
			}
			catch (Exception e)
			{
				ErrorManager.PublishError(e);
			}
        }

	    private void MarkCells()
	    {
            for (int rowIndex = FIXED_ROWS; rowIndex < RawDataGridView.RowCount; rowIndex++)
                for (int columnIndex = FixedCols; columnIndex < RawDataGridView.ColumnCount; columnIndex++)
                {
                    TariffWindowWithRange window = GetTariffWindow(rowIndex, columnIndex) as TariffWindowWithRange;
                    if (window == null)
                        continue;

                    // Жирный — слот подходит под все заданные фильтры (позиция и/или ПР), как в линейной сетке.
                    if ((RollerPosition != RollerPositions.Undefined || _advertTypePresence != AdvertTypePresences.Undefined)
                        && IsPositionAvailable(window) && IsAdvertTypeMatched(window))
                        MarkCellAsNotOccupied(rowIndex, columnIndex);

					// Синий — выпуск акции есть у каждой выбранной кампании, ролики могут быть разными
					// (требование заказчика). Не по AddedIssues: там только ролики, общие для всех кампаний.
					if (window.HasCurrentActionIssuesAllCampaigns)
					{
						MarkCellAsHavingCurrentCampaignIssues(rowIndex, columnIndex);
                        continue;
                    }

                    var cell = GetCell(rowIndex, columnIndex);
                    if (window.HasCurrentActionIssues)
                    {
                        MarkCellAsHavingCurrentActionIssues(cell);
                        continue;
                    }

                    bool hasAllMassmediaIssues = HasAllMassmediaIssuesFlags(window);
					bool hasAnyIssues = hasAllMassmediaIssues || HasFirmIssuesFlags(window);

                    if (!hasAnyIssues)
                        continue;

                    
                    if (hasAllMassmediaIssues)
                        MarkCellAsHavingCurrentFirmIssues(cell);
                    else if (hasAnyIssues)
                        MarkCellAsHavingCurrentFirmIssuesAnyMassmedia(cell);
                }
	    }

	    private bool HasFirmIssuesFlags(TariffWindowWithRange window)
	    {
	        if (window == null) return false;

	        return window.HasIssues || window.HasCurrentActionIssues
	               || (ShowUnconfirmed && window.HasIssuesUnconfirmed);
	    }

	    private bool HasAllMassmediaIssuesFlags(TariffWindowWithRange window)
	    {
	        if (window == null) return false;

	        return window.HasIssuesAllMassmedia
	               || (ShowUnconfirmed && window.HasIssuesUnconfirmedAllMassmedia);
	    }

        /// <summary>
        /// Проверяет, доступна ли текущая позиция ролика (RollerPosition) в окне.
        /// Если позиция не определена (Undefined) — всегда доступно.
        /// </summary>
        private bool IsPositionAvailable(TariffWindowWithRange window)
        {
            if (_rollerPosition == RollerPositions.Undefined)
                return true;

            if (!ShowUnconfirmed)
                return IsConfirmedPositionNotOccupied(window);

            return IsConfirmedPositionNotOccupied(window)
                && IsUnconfirmedPositionsNotOccupied(window);
        }

        /// <summary>
        /// Фильтр «Предметы рекламы». Флаг слота — ПР есть хотя бы на одной станции:
        /// «Есть» подсвечивает слот, даже если такая станция одна, «Нет» — только если
        /// ПР нет ни на одной станции. Фильтр не задан — всегда true.
        /// </summary>
        private bool IsAdvertTypeMatched(TariffWindowWithRange window)
        {
            if (_advertTypePresence == AdvertTypePresences.Undefined)
                return true;

            bool found = ShowUnconfirmed ? window.HasAdvertTypeUnconfirmed : window.HasAdvertType;
            return found == (_advertTypePresence == AdvertTypePresences.Exist);
        }

        private bool IsUnconfirmedPositionsNotOccupied(TariffWindowWithRange window)
        {
            return (RollerPosition == RollerPositions.First && window.FirstPositionsUnconfirmed == 0) ||
                   (RollerPosition == RollerPositions.Second && window.SecondPositionsUnconfirmed == 0) ||
                   (RollerPosition == RollerPositions.Last && window.LastPositionsUnconfirmed == 0);
        }

        private bool IsConfirmedPositionNotOccupied(TariffWindowWithRange window)
        {
            return (RollerPosition == RollerPositions.First && !window.IsFirstPositionOccupied) ||
                   (RollerPosition == RollerPositions.Second && !window.IsSecondPositionOccupied) ||
                   (RollerPosition == RollerPositions.Last && !window.IsLastPositionOccupied);
        }

	    private void PopulateGridTable(DataTable dt)
		{
			_timeResolver = new Dictionary<string, string>();
			if (_tariffWindows != null && MaxBroadCast.HasValue)
			{
                foreach (DataRow row in dt.Rows)
                {
                    DataRow gridRow = dtGrid.NewRow();
                    int h = ParseHelper.GetInt32FromObject(row["h"], 0);
                    int m = ParseHelper.GetInt32FromObject(row["m"], 0);
					DateTime t = new DateTime(1, 1, 1, h, m, 0);
					t = t.AddMinutes(30);
                    gridRow[ColumnNames.TimeString] = string.Format("{0}-{1}", GetTimeString(h, m), GetTimeString(t.Hour, t.Minute));
                    _timeResolver.Add(gridRow[ColumnNames.TimeString].ToString(), GetTimeString(h, m));
                    dtGrid.Rows.Add(gridRow);
                }

				foreach (DataRow row in Data.Rows)
				{
					int h = ParseHelper.GetInt32FromObject(row["h"], 0);
                    int m = ParseHelper.GetInt32FromObject(row["m"], 0);
				    int? rowIndex = GetRow(h, m);
                    if (rowIndex.HasValue)
                    {
                        int iCol = ParseHelper.GetInt32FromObject(row["col"], 1) - 1;
                        dtGrid.Rows[rowIndex.Value][iCol + FixedCols] = GetCellContent(row);
                        _tariffWindows[rowIndex.Value - FIXED_ROWS, iCol] = new TariffWindowWithRange(row);
                    }
				}
			}
		}

	    private string GetTimeString(int h, int m)
	    {
	        return h >= MaxBroadCast.Value.Hour
	                   ? DateTimeUtils.Time2String(h, m)
	                   : DateTimeUtils.Time2String(h + 24, m);
	    }

	    private int? GetRow(int h, int m)
        {
	        int index = 0;
            foreach (DataRow row in dtGrid.Rows)
            {
				string key = row[ColumnNames.TimeString].ToString();
				if (_timeResolver.ContainsKey(key) && _timeResolver[key].Equals(GetTimeString(h, m)))
					return index;
                index++;
            }
	        return null;
        }

		protected virtual string GetCellContent(DataRow row)
		{
			DateTime windowDate = ParseHelper.GetDateTimeFromObject(row["date"], DateTime.MinValue);
			int timeWithConfirmed = ParseHelper.GetInt32FromObject(row["timeWithConfirmed"], 0);
			int timeWithUnConfirmed = ParseHelper.GetInt32FromObject(row["timeWithUnConfirmed"], 0);
			return BuildCellContent(windowDate, timeWithConfirmed, timeWithUnConfirmed);
		}

		private string BuildCellContent(DateTime windowDate, int timeWithConfirmed, int timeWithUnConfirmed)
		{
			if (showRollerNumbers)
			{
				string rollerNumbersText = GetRollerNumbersText(windowDate);
				if (rollerNumbersText != null)
					return rollerNumbersText;
			}

			return DateTimeUtils.Time2String(ShowUnconfirmed ? timeWithUnConfirmed : timeWithConfirmed);
		}

		// Номера роликов, размещённых в этом слоте (через запятую), или null, если слот свободен /
		// номер ролика не известен. Ролики "своей" акции — по возрастанию номера, с пометками:
		// Н — количество ролика не совпадает у всех выбранных кампаний (в т.ч. у кого-то его нет),
		// Д — хотя бы у одной кампании роликов этого номера больше одного (решение заказчика).
		// Источник — батч-кэш _partialRollerGroupsByDate (RangeSlotIssues отдаёт весь слот по
		// выбранным кампаниям, а не только частичный); AddedIssues — запасной путь, пока кэш не
		// загружен: он хранит пересечение слотов без разбивки по кампаниям, пометок из него не построить.
		private string GetRollerNumbersText(DateTime windowDate)
		{
			if (rollerNumbers == null || windowDate == DateTime.MinValue) return null;

			List<string> numbers = new List<string>();
			HashSet<string> covered = new HashSet<string>();

			if (_partialRollerGroupsByDate != null &&
			    _partialRollerGroupsByDate.TryGetValue(windowDate, out IList<SlotIssueGroup> groups))
			{
				numbers.AddRange(BuildRollerMarks(groups));
				foreach (SlotIssueGroup group in groups)
					covered.Add(group.RollerId + "/" + (int)group.Position);
			}
			else if (AddedIssues != null)
				foreach (DataRow issueRow in AddedIssues.Select(string.Format("[issueDate] = '{0}'", windowDate)))
				{
					int rollerId = ParseHelper.GetInt32FromObject(issueRow[Roller.ParamNames.RollerId], 0);
					int positionId = ParseHelper.GetInt32FromObject(issueRow[Issue.ParamNames.PositionId], 0);
					covered.Add(rollerId + "/" + positionId);
					if (rollerNumbers.TryGetValue(rollerId, out int number))
						numbers.Add(number.ToString());
				}

			// Бирюзовые/оранжевые слоты — чужая акция ТОЙ ЖЕ фирмы, т.е. ролики из того же
			// фирменного списка, и номера у них те же (требование заказчика). Неподтверждённые
			// чужие выпуски — только при «Учитывать неподтверждённые», иначе в ячейке без цвета
			// появился бы номер ролика — см. HasFirmIssuesFlags.
			if (_otherFirmRollersByDate != null &&
			    _otherFirmRollersByDate.TryGetValue(windowDate, out List<OtherFirmRoller> firmRollers))
				foreach (OtherFirmRoller firmRoller in firmRollers)
				{
					if (!firmRoller.HasConfirmed && !ShowUnconfirmed)
						continue;

					if (!covered.Add(firmRoller.RollerId + "/" + firmRoller.PositionId))
						continue;

					if (rollerNumbers.TryGetValue(firmRoller.RollerId, out int number))
						numbers.Add(number.ToString());
				}

			return numbers.Count > 0 ? string.Join(", ", numbers) : null;
		}

		// Сколько выпусков каждого ролика (по номеру, по возрастанию) у каждой кампании слота:
		// номер -> campaignId -> штук. Кампании без выпусков этого ролика в словаре нет (= 0).
		// Позиция роликов в счёт не идёт: дубль с другой позицией — обычный дубль.
		// Ролики без номера (не из списка фирмы) пропускаются.
		private SortedDictionary<int, Dictionary<int, int>> CountRollersByNumber(IList<SlotIssueGroup> groups)
		{
			SortedDictionary<int, Dictionary<int, int>> countsByNumber = new SortedDictionary<int, Dictionary<int, int>>();
			foreach (SlotIssueGroup group in groups)
			{
				if (!rollerNumbers.TryGetValue(group.RollerId, out int number))
					continue;

				if (!countsByNumber.TryGetValue(number, out Dictionary<int, int> countsByCampaign))
				{
					countsByCampaign = new Dictionary<int, int>();
					countsByNumber.Add(number, countsByCampaign);
				}

				foreach (int campaignId in group.CampaignIds)
				{
					countsByCampaign.TryGetValue(campaignId, out int count);
					countsByCampaign[campaignId] = count + 1;
				}
			}

			return countsByNumber;
		}

		/// <summary>
		/// Расклад роликов слота по кампаниям для подсказки к значкам Н/Д (см.
		/// <see cref="CountRollersByNumber"/>). null — номера роликов не показываются или в слоте
		/// нет выпусков своей акции.
		/// </summary>
		public SortedDictionary<int, Dictionary<int, int>> GetRollerCountsByNumber(DateTime windowDate)
		{
			if (!showRollerNumbers || rollerNumbers == null || _partialRollerGroupsByDate == null ||
			    !_partialRollerGroupsByDate.TryGetValue(windowDate, out IList<SlotIssueGroup> groups))
				return null;

			return CountRollersByNumber(groups);
		}

		// Номер ролика + пометки Н/Д для каждого ролика слота, по возрастанию номера.
		private List<string> BuildRollerMarks(IList<SlotIssueGroup> groups)
		{
			List<string> marks = new List<string>();
			foreach (KeyValuePair<int, Dictionary<int, int>> roller in CountRollersByNumber(groups))
			{
				List<int> counts = SelectedCampaignIds
					.Select(campaignId => roller.Value.TryGetValue(campaignId, out int count) ? count : 0)
					.ToList();
				bool incomplete = counts.Any(count => count != counts[0]);
				bool duplicated = counts.Any(count => count > 1);
				marks.Add(roller.Key + (incomplete ? "Н" : string.Empty) + (duplicated ? "Д" : string.Empty));
			}

			return marks;
		}

		// Перерисовать текст всех ячеек грида — нужно при включении/выключении ShowRollerNumbers.
		// При обычном RefreshGrid() (после клика добавления/удаления) тоже вызывается — см.
		// onGridPopulated — но там AddedIssues уже свежий на момент вызова. При включённом режиме
		// номеров роликов делает один батч-запрос за частичными группами текущей недели
		// (RefreshPartialRollerGroups) — без него на каждую красную ячейку был бы отдельный поход
		// в базу.
		public void RefreshCellTexts()
		{
			// Пустая сетка (0 выбранных кампаний, нет вещания — MinBroadCast/MaxBroadCast не
			// заданы, см. onGridPopulated) — _tariffWindows не создаётся, обновлять нечего.
			if (_tariffWindows == null)
			{
				_partialRollerGroupsByDate = null;
				return;
			}

			if (showRollerNumbers)
				RefreshPartialRollerGroups();
			else
				_partialRollerGroupsByDate = null;

			int rowCount = _tariffWindows.GetLength(0);
			int columnCount = _tariffWindows.GetLength(1);

			for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
				for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
					if (_tariffWindows[rowIndex, columnIndex] is TariffWindowWithRange window)
						UpdateGridCell(rowIndex + FIXED_ROWS, columnIndex + FixedCols, window);
		}

		private void RefreshPartialRollerGroups()
		{
			List<DateTime> dates = new List<DateTime>();
			int rowCount = _tariffWindows.GetLength(0);
			int columnCount = _tariffWindows.GetLength(1);
			for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
				for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
					if (_tariffWindows[rowIndex, columnIndex] is TariffWindowWithRange window)
						dates.Add(window.WindowDate);

			_partialRollerGroupsByDate = dates.Count > 0
				? GetSlotIssueGroups(dates)
				: new Dictionary<DateTime, IList<SlotIssueGroup>>();
		}

		private void UpdateGridCell(int rowIndex, int columnIndex, TariffWindowWithRange window)
		{
			dtGrid.Rows[rowIndex][columnIndex] = BuildCellContent(window.WindowDate, window.TimeWithConfirmed, window.TimeWithUnConfirmed);
		}

		protected override void SetNavigationCaption()
		{
			Caption.Caption = string.Format("{0} - {1}", StartDate.ToString("dd.MM.yyyy"), FinishDate.ToString("dd.MM.yyyy"));
		}

		protected override void InitializeGridColumns()
		{
			gridColumns = new[]
				{
					new GridColumn("Время", ColumnNames.TimeString),
					new GridColumn("Пн.", ColumnNames.Monday),
					new GridColumn("Вт.", ColumnNames.Tuesday),
					new GridColumn("Ср.", ColumnNames.Wednesday),
					new GridColumn("Чт.", ColumnNames.Thursday),
					new GridColumn("Пт.", ColumnNames.Friday),
					new GridColumn("Сб.", ColumnNames.Saturday),
					new GridColumn("Вс.", ColumnNames.Sunday),
					new GridColumn(ColumnNames.Time, ColumnNames.Time, true)
				};
		}

		public override Entity IssueEntity
		{
			get { return null; }
		}

		public Roller Roller { get; set; }

	    private RollerPositions _rollerPosition;

		public RollerPositions RollerPosition { get => _rollerPosition; set { _rollerPosition = value; RefreshGrid();} }
				
		public PresentationObject Module 
		{ 
			get { return null;} set { }
		}

		public void RefreshCurrentCell(bool hasCurrentCampaignIssues, TariffGridRefreshMode mode)
		{
			throw new NotImplementedException();
		}

        public SecurityManager.User Grantor { get; set; }

        private AdvertTypePresences _advertTypePresence = AdvertTypePresences.Undefined;
        private PresentationObject _advertType;

        public void SetAdvertTypePresence(AdvertTypePresences advertTypePresence, PresentationObject advertType)
        {
            _advertTypePresence = advertTypePresence;
            _advertType = advertType;
            RefreshGrid();
        }

        // ---------------------------------------------------------------
        // TimePeriod range generation support
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns the Monday that starts the ISO week containing <paramref name="date"/>.
        /// Handles Sunday correctly (DayOfWeek.Sunday == 0).
        /// </summary>
        private static DateTime GetWeekMonday(DateTime date)
        {
            int diff = (7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return date.Date.AddDays(-diff);
        }

        /// <summary>
        /// Loads all 30-min slots for the ISO week that contains <paramref name="weekMonday"/>
        /// from the TariffWindowWithRange stored procedure and caches them in
        /// <paramref name="slotsCache"/> (keyed by Monday date).
        /// </summary>
        private List<TariffWindowWithRange> GetSlotsForWeek(
            DateTime weekMonday,
            Dictionary<DateTime, List<TariffWindowWithRange>> slotsCache)
        {
            if (!slotsCache.TryGetValue(weekMonday, out List<TariffWindowWithRange> slots))
            {
                Dictionary<string, object> dict = DataAccessor.CreateParametersDictionary();
                dict.Add("dateStart", weekMonday);
                dict.Add("actionID", _action.ActionId);
                dict.Add(Campaign.ParamNames.CampaignIds, CampaignIdsParameter);
                DataSet ds = DataAccessor.LoadDataSet("TariffWindowWithRange", dict);
                slots = ds.Tables[0].Rows
                          .Cast<DataRow>()
                          .Select(r => new TariffWindowWithRange(r))
                          .ToList();
                slotsCache[weekMonday] = slots;
            }
            return slots;
        }

        /// <summary>
        /// Adds issues for all selected slots within the <paramref name="startTime"/>–
        /// <paramref name="finishTime"/> window on <paramref name="date"/> (both bounds
        /// inclusive, matching the Simple TimePeriod behaviour).
        ///
        /// When <paramref name="quantity"/> &gt; 0: takes the first N available slots
        /// chronologically regardless of prime.
        /// When <paramref name="quantity"/> == 0: fills <paramref name="quantityPrime"/>
        /// prime slots and <paramref name="quantityNonPrime"/> non-prime slots.
        ///
        /// Does NOT call _action.Recalculate() — the caller (FrmGenerator.finally) is
        /// responsible for the single end-of-generation recalculate.
        /// </summary>
        public TimePeriodAddResult AddIssuesRangeTimePeriod(
            DateTime date,
            DateTime startTime,
            DateTime finishTime,
            int quantity,
            int quantityPrime,
            int quantityNonPrime,
            bool ignoreWindowsWithTheSameFirmIssue,
            Dictionary<DateTime, List<TariffWindowWithRange>> slotsCache)
        {
            using (OperationScope.Start(
                $"AddIssuesRangeTimePeriod date={date:yyyy-MM-dd} " +
                $"q={quantity}/{quantityPrime}p/{quantityNonPrime}np"))
            {
                DateTime weekMonday = GetWeekMonday(date);
                List<TariffWindowWithRange> weekSlots = GetSlotsForWeek(weekMonday, slotsCache);

                // Filter: same calendar date, within the time window (both bounds inclusive)
                TimeSpan tsStart  = startTime.TimeOfDay;
                TimeSpan tsFinish = finishTime.TimeOfDay;

                var rnd = new Random();

                List<TariffWindowWithRange> slotsForDate = weekSlots
                    .Where(s => s.WindowDate.Date == date.Date
                             && s.WindowDate.TimeOfDay >= tsStart
                             && s.WindowDate.TimeOfDay < tsFinish
                             && (_rollerPosition == RollerPositions.Undefined || IsPositionAvailable(s))
                             && (!ignoreWindowsWithTheSameFirmIssue || !HasFirmIssuesFlags(s)))
                    .Select(s => new { w = s, rand = rnd.Next() })
                    .OrderBy(x => x.w)       // IComparable — сначала самые свободные (max TimeWithUnConfirmed)
                    .ThenBy(x => x.rand)     // среди одинаковых — рандом
                    .Select(x => x.w)
                    .ToList();

                var result = new TimePeriodAddResult();

                IEnumerable<TariffWindowWithRange> selectedSlots;
                if (quantity > 0)
                {
                    selectedSlots = slotsForDate.Take(quantity);
                    result.ExpectedCount = quantity;
                }
                else
                {
                    IEnumerable<TariffWindowWithRange> primeSlots    = slotsForDate.Where(s =>  s.IsPrime).Take(quantityPrime);
                    IEnumerable<TariffWindowWithRange> nonPrimeSlots = slotsForDate.Where(s => !s.IsPrime).Take(quantityNonPrime);
                    selectedSlots = primeSlots.Concat(nonPrimeSlots);
                    result.ExpectedCount = quantityPrime + quantityNonPrime;
                }

                foreach (TariffWindowWithRange slot in selectedSlots)
                {
                    try
                    {
                        DataRow row = AddIssuesRange(slot.WindowDate,
                            ignoreWindowsWithTheSameFirmIssue,
                            recalculate: false);
                        result.Rows.Add(row);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(ex);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Аналог <see cref="AddIssuesRangeTimePeriod"/>, но роликов несколько: вместо единого Roller
        /// на каждый выбранный слот берёт следующий ролик из <paramref name="takeRollersForToday"/> —
        /// колбэк получает "сколько слотов реально нашлось" и возвращает столько же роликов
        /// из общей случайно перемешанной очереди (отсортированных по убыванию длительности,
        /// см. RollerAllocationQueue). Если слотов сегодня меньше, чем нужно по дневной норме,
        /// колбэк заберёт из очереди меньше — остаток естественным образом уйдёт на следующий день.
        /// </summary>
        public TimePeriodAddResult AddIssuesRangeTimePeriodMultiRoller(
            DateTime date,
            DateTime startTime,
            DateTime finishTime,
            int quantity,
            int quantityPrime,
            int quantityNonPrime,
            bool ignoreWindowsWithTheSameFirmIssue,
            Dictionary<DateTime, List<TariffWindowWithRange>> slotsCache,
            Func<int, List<Roller>> takeRollersForToday)
        {
            using (OperationScope.Start(
                $"AddIssuesRangeTimePeriodMultiRoller date={date:yyyy-MM-dd} " +
                $"q={quantity}/{quantityPrime}p/{quantityNonPrime}np"))
            {
                DateTime weekMonday = GetWeekMonday(date);
                List<TariffWindowWithRange> weekSlots = GetSlotsForWeek(weekMonday, slotsCache);

                TimeSpan tsStart = startTime.TimeOfDay;
                TimeSpan tsFinish = finishTime.TimeOfDay;

                var rnd = new Random();

                List<TariffWindowWithRange> slotsForDate = weekSlots
                    .Where(s => s.WindowDate.Date == date.Date
                             && s.WindowDate.TimeOfDay >= tsStart
                             && s.WindowDate.TimeOfDay < tsFinish
                             && (_rollerPosition == RollerPositions.Undefined || IsPositionAvailable(s))
                             && (!ignoreWindowsWithTheSameFirmIssue || !HasFirmIssuesFlags(s)))
                    .Select(s => new { w = s, rand = rnd.Next() })
                    .OrderBy(x => x.w)
                    .ThenBy(x => x.rand)
                    .Select(x => x.w)
                    .ToList();

                var result = new TimePeriodAddResult();

                if (quantity > 0)
                {
                    List<TariffWindowWithRange> selectedSlots = slotsForDate.Take(quantity).ToList();
                    result.ExpectedCount = quantity;
                    PlaceRollersInSlots(selectedSlots, takeRollersForToday, ignoreWindowsWithTheSameFirmIssue, result);
                }
                else
                {
                    List<TariffWindowWithRange> primeSlots = slotsForDate.Where(s => s.IsPrime).Take(quantityPrime).ToList();
                    List<TariffWindowWithRange> nonPrimeSlots = slotsForDate.Where(s => !s.IsPrime).Take(quantityNonPrime).ToList();
                    result.ExpectedCount = quantityPrime + quantityNonPrime;
                    PlaceRollersInSlots(primeSlots, takeRollersForToday, ignoreWindowsWithTheSameFirmIssue, result);
                    PlaceRollersInSlots(nonPrimeSlots, takeRollersForToday, ignoreWindowsWithTheSameFirmIssue, result);
                }

                return result;
            }
        }

        private void PlaceRollersInSlots(
            List<TariffWindowWithRange> slots,
            Func<int, List<Roller>> takeRollersForToday,
            bool ignoreWindowsWithTheSameFirmIssue,
            TimePeriodAddResult result)
        {
            List<Roller> rollers = takeRollersForToday(slots.Count);
            for (int i = 0; i < rollers.Count; i++)
            {
                try
                {
                    DataRow row = AddIssuesRange(slots[i].WindowDate, rollers[i],
                        ignoreWindowsWithTheSameFirmIssue, recalculate: false);
                    result.Rows.Add(row);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex);
                }
            }
        }

        // helper to keep AddedIssues sorted by issueDate
        private void InsertIssueRowSorted(DataRow row)
        {
            DateTime newIssueDate = ParseHelper.GetDateTimeFromObject(row["issueDate"], DateTime.MinValue);
            int insertIndex = AddedIssues.Rows.Count;

            for (int i = 0; i < AddedIssues.Rows.Count; i++)
            {
                DateTime existingDate = ParseHelper.GetDateTimeFromObject(AddedIssues.Rows[i]["issueDate"], DateTime.MaxValue);
                if (existingDate > newIssueDate)
                {
                    insertIndex = i;
                    break;
                }
            }

            AddedIssues.Rows.InsertAt(row, insertIndex);
        }
	}
}
