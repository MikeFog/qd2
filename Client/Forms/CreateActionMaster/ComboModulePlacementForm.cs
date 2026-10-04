using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using Merlin.Controls;

namespace Merlin.Forms.CreateActionMaster
{
	/// <summary>
	/// Третий шаг мастера размещения комбо-модулями: слева ролики фирмы, статистика акции и
	/// добавленные выпуски, справа грид остатков по модулям комбо-модуля.
	///
	/// Форма самостоятельная, а не наследник CampaignForm: та завязана на одну кампанию с её
	/// прайс-листом и тарифной сеткой, а здесь строки - модули разных радиостанций, и кампаний
	/// столько же, сколько модулей.
	/// </summary>
	internal partial class ComboModulePlacementForm : Form, IMediaControlContainer
	{
		private const string SETTING_PERIOD_MODE = "ComboModulePlacementPeriodMode";

		// колонки Campaigns, по которым подписывается строка грида
		private const string COLUMN_PAYMENT_TYPE_NAME = "paymentTypeName";
		private const string COLUMN_AGENCY_NAME = "agencyName";

		private readonly Firm _firm;
		private readonly int _comboModuleID;
		private readonly string _comboModuleName;
		private readonly int _paymentTypeID;
		private readonly string _paymentTypeName;
		private readonly Dictionary<int, int> _agencyByMassmedia;

		/// <summary>
		/// Форма открыта на уже существующей акции (из её карточки), а не из мастера.
		/// В этом режиме сама акция не создаётся и не удаляется: её состав - дело акции,
		/// а не наше.
		/// </summary>
		private readonly bool _isExistingAction;

		/// <summary>
		/// Готовая акция открыта с выбранным комбо-модулем: грид показывает весь его состав,
		/// по клику в модуль без кампании достраивается новая модульная кампания прямо в этой
		/// акции. false - либо мастер, либо правка готовой акции «как есть» (только уже
		/// размещённые модули, без создания кампаний).
		/// </summary>
		private readonly bool _reconstructFromComboModule;

		/// <summary>
		/// Кампании (campaignID), созданные в этой сессии. Только их можно молча удалить, если
		/// остались без выпусков: ранее существовавшие кампании акции трогать нельзя - их мог
		/// наполнять кто-то ещё.
		/// </summary>
		private readonly HashSet<int> _campaignsCreatedThisSession = new HashSet<int>();

		/// <summary>Модули выбранного комбо-модуля - для фильтра выпусков панели/счётчика.</summary>
		private HashSet<int> _comboModuleModuleIDs;

		private RollerPositions _position = RollerPositions.Undefined;
		private readonly MediaControl _mediaControl;

		/// <summary>
		/// Акция и кампании создаются лениво, по первому клику: пока менеджер ничего не
		/// разместил, в базе не должно оставаться пустой акции.
		/// </summary>
		private ActionOnMassmedia _action;

		/// <summary>
		/// Параметры несохранённой акции, пришедшей из карточки, - снимок до её вставки в
		/// EnsureAction. Объект общий с карточкой: если транзакция первого клика откатится,
		/// его надо вернуть в «несохранённое» состояние, иначе карточка останется с номером
		/// откаченной акции (Д-5).
		/// </summary>
		private Dictionary<string, object> _actionDraft;

		/// <summary>Объекты модульных кампаний акции, к которым уже обращались, - по campaignID.</summary>
		private readonly Dictionary<int, Campaign> _campaigns = new Dictionary<int, Campaign>();

		/// <summary>
		/// Модульные кампании акции (строки Campaigns: станция, тип оплаты, агентство и их
		/// названия). null - перечитать: сбрасывается, когда форма создаёт или удаляет кампанию.
		/// </summary>
		private DataTable _moduleCampaigns;

		/// <summary>Выпуски акции, показанные в панели, - из них же берём удаляемые по Del.</summary>
		private DataTable _issues;

		/// <summary>Выпуски, прочитанные при построении строк грида, - их же раздаёт OnGridRefreshed.</summary>
		private DataTable _issuesForGrid;

		/// <summary>
		/// Выпуски, добавленные последним действием (клик по ячейке или Insert по выделению) -
		/// определяются сравнением состава до и после (SnapshotIssueIDs / RememberAdded).
		/// Кнопка «Отменить» удаляет ровно их; любое удаление выпусков сбрасывает список.
		/// </summary>
		private readonly List<PresentationObject> _lastAddedIssues = new List<PresentationObject>();

		/// <summary>
		/// Выпуски, загруженные при открытии формы для выбора начальной даты. Панель к тому
		/// моменту ещё пуста - она заполняется внутри того же обновления грида, которому эта
		/// дата и нужна. Чтобы не читать их дважды, первое обновление берёт эту таблицу.
		/// </summary>
		private DataTable _issuesForFirstRefresh;

		/// <summary>
		/// Созданная в ходе размещения акция или null, если менеджер ничего не разместил.
		/// По ней мастер открывает карточку акции после закрытия формы.
		/// </summary>
		public ActionOnMassmedia Action
		{
			get { return _action; }
		}

		private ComboModulePlacementForm()
		{
			InitializeComponent();
			tbbRefresh.Image = Globals.GetImage(Constants.ActionsImages.Refresh);
			tbbStart.Image = Globals.GetImage(Constants.ActionsImages.Properties);
			tbbPlay.Image = Globals.GetImage(Constants.ActionsImages.Play);
			tsbStop.Image = Globals.GetImage(Constants.ActionsImages.Stop);
			tbbExcel.Image = Globals.GetImage(Constants.ActionsImages.ExportExcel);
			tbSetActionPrice.Image = Globals.GetIcon("Money.png");
			_mediaControl = new MediaControl(this);
			FormClosing += (s, e) => _mediaControl.Stop();
		}

		#region IMediaControlContainer Members -----------------

		public bool IsPlaying
		{
			set { tsbStop.Enabled = value; }
		}

		#endregion

		/// <summary>Размещение по комбо-модулю: акция и кампании появятся по первому клику.</summary>
		public ComboModulePlacementForm(Firm firm, SelectComboModuleStep step) : this()
		{
			_firm = firm;
			_comboModuleID = step.ComboModuleID;
			_comboModuleName = step.ComboModuleName;
			_paymentTypeID = step.PaymentTypeID;
			_paymentTypeName = step.PaymentTypeName;
			_agencyByMassmedia = step.AgencyByMassmedia;
		}

		/// <summary>
		/// Редактирование готовой акции из её карточки: строки грида - модули, уже
		/// размещённые в акции, комбо-модуль ни при чём. Кампании не создаются.
		/// </summary>
		public ComboModulePlacementForm(ActionOnMassmedia action) : this()
		{
			_action = action;
			_firm = action.Firm;
			_comboModuleName = string.Format("акция №{0}", action.ActionId);
			_agencyByMassmedia = new Dictionary<int, int>();
			_isExistingAction = true;
		}

		/// <summary>
		/// Редактирование готовой акции как комбо-модуля: строки грида - весь состав выбранного
		/// комбо-модуля, недостающие модули достраиваются. По клику в модуль без кампании
		/// создаётся новая модульная кампания прямо в этой акции (тип оплаты и агентство - с
		/// шага выбора). Связи «акция - комбо-модуль» в базе нет, поэтому комбо-модуль
		/// выбирается заново каждый раз.
		/// </summary>
		public ComboModulePlacementForm(ActionOnMassmedia action, SelectComboModuleStep step) : this()
		{
			_action = action;
			_firm = action.Firm;
			_comboModuleID = step.ComboModuleID;
			_comboModuleName = step.ComboModuleName;
			_paymentTypeID = step.PaymentTypeID;
			_paymentTypeName = step.PaymentTypeName;
			_agencyByMassmedia = step.AgencyByMassmedia;
			_isExistingAction = true;
			_reconstructFromComboModule = true;
		}

		protected override void OnLoad(EventArgs e)
		{
			// грид размещения строится синхронно (ролики, выпуски акции, остатки по модулям),
			// на большой акции это заметно - показываем песочные часы на время загрузки.
			// Cursor.Current, а не this.Cursor: форма ещё не показана, курсор сейчас над картой акции.
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				base.OnLoad(e);

				Text = string.Format("Размещение комбо-модулями: {0} - {1}", _firm.Name, _comboModuleName);

				InitRollersList();
				InitAddedIssuesList();
				InitComboModuleGrid();
				EnableIssueDragDrop();
				ShowStatistics();   // у готовой акции она есть сразу, а не после первого клика
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void InitRollersList()
		{
			grdRollers.Entity = EntityManager.GetEntity((int) Entities.ActionRollers);
			grdRollers.DataSource = _firm.GetRollers().DefaultView;
		}

		private void InitAddedIssuesList()
		{
			Entity issueEntity = (Entity) ModuleIssue.GetEntity().Clone();
			issueEntity.AttributeSelector = ModuleIssue.AttributeSelectorComboPlacement;
			grdAddedIssues.Entity = issueEntity;
			grdAddedIssues.ObjectDeleted += OnIssueDeleted;     // удалили одну строку
			grdAddedIssues.ObjectsDeleted += OnIssuesDeleted;   // удалили несколько
			grdAddedIssues.MultiSelect = true;   // Del по нескольким строкам умеет сам SmartGrid
		}

		private void InitComboModuleGrid()
		{
			comboModuleGrid.ComboModuleID = _comboModuleID;
			comboModuleGrid.RowsProvider = BuildGridRows;
			if (_action != null)
			{
				comboModuleGrid.ActionID = _action.ActionId;

				// открываемся на самом раннем выпуске модулей, а не на сегодня.
				// Action.startDate тут не годится: он считается по всем кампаниям акции,
				// включая линейные и спонсорские, которых в этой форме нет, и грид мог бы
				// открыться там, где модульных выпусков вообще не было
				DateTime? firstIssueDate = GetFirstModuleIssueDate();
				if (firstIssueDate.HasValue)
					comboModuleGrid.CurrentDate = firstIssueDate.Value;
			}
			comboModuleGrid.PeriodMode = LoadPeriodMode();
			comboModuleGrid.ShowUnconfirmed = tbbShowUnconfirmed.Checked;
			comboModuleGrid.CellClicked += OnCellClicked;
			comboModuleGrid.GridRefreshed += OnGridRefreshed;
			comboModuleGrid.RawDataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
			comboModuleGrid.RawDataGridView.KeyDown += ComboModuleGrid_KeyDown;
			UpdatePeriodModeCaption();
			comboModuleGrid.RefreshGrid();
		}

		/// <summary>Самый ранний выпуск модуля в акции - с него открывается грид.</summary>
		private DateTime? GetFirstModuleIssueDate()
		{
			_issuesForFirstRefresh = LoadActionModuleIssues();

			DateTime? first = null;
			foreach (DataRow row in _issuesForFirstRefresh.Rows)
			{
				DateTime date = Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]).Date;
				if (first == null || date < first.Value) first = date;
			}
			return first;
		}

		/// <summary>
		/// Выпуски модулей акции - панель «Добавленные выпуски», подсветка сетки и счётчик по
		/// дням. В режиме комбо-модуля на готовой акции ограничены модулями этого комбо-модуля:
		/// остальные модульные кампании акции в этой форме не показаны и не редактируются.
		/// </summary>
		private DataTable LoadActionModuleIssues()
		{
			DataTable issues = ComboModule.LoadIssues(_action.ActionId);
			if (!_reconstructFromComboModule) return issues;

			HashSet<int> moduleIDs = ComboModuleModuleIDs;
			DataTable filtered = issues.Clone();
			foreach (DataRow row in issues.Rows)
				if (moduleIDs.Contains(Convert.ToInt32(row[ComboModule.ParamNames.ModuleId])))
					filtered.ImportRow(row);
			return filtered;
		}

		private HashSet<int> ComboModuleModuleIDs
		{
			get
			{
				if (_comboModuleModuleIDs == null)
				{
					_comboModuleModuleIDs = new HashSet<int>();
					foreach (DataRow row in ComboModuleContent.Rows)
						_comboModuleModuleIDs.Add(Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]));
				}
				return _comboModuleModuleIDs;
			}
		}

		/// <summary>Состав комбо-модуля (модули и их станции) - читается один раз.</summary>
		private DataTable ComboModuleContent
		{
			get
			{
				if (_comboModuleContent == null)
					_comboModuleContent = ComboModule.LoadContent(_comboModuleID);
				return _comboModuleContent;
			}
		}
		private DataTable _comboModuleContent;

		#region Строки грида: модуль в разрезе кампаний (Д-7) --

		/// <summary>
		/// Строки грида - модуль в разрезе модульных кампаний акции: у станции их может быть
		/// несколько (UIX_Campaign различает тип оплаты и агентство), у каждой своя строка, и
		/// клик или перенос попадает в кампанию строки.
		/// - кампании, где модуль уже стоит, - из выпусков акции (оба режима карточки);
		/// - с комбо-модулем (мастер или ответ «Да») - ещё строка каждого модуля с типом оплаты
		///   и агентством из шага, если её нет среди уже размещённых; кампании под неё может
		///   ещё не быть - она создаётся по первому клику.
		/// Прочитанные здесь выпуски раздаёт OnGridRefreshed - второй раз не читаем.
		/// </summary>
		private DataTable BuildGridRows()
		{
			DataTable issues;
			if (_issuesForFirstRefresh != null)
			{
				issues = _issuesForFirstRefresh;
				_issuesForFirstRefresh = null;
			}
			else
				issues = _action == null ? null : LoadActionModuleIssues();
			_issuesForGrid = issues;

			DataTable rows = new DataTable();
			rows.Columns.Add(ComboModuleGrid.RowColumns.ModuleId, typeof(int));
			rows.Columns.Add(ComboModuleGrid.RowColumns.ModuleName, typeof(string));
			rows.Columns.Add(ComboModuleGrid.RowColumns.MassmediaId, typeof(int));
			rows.Columns.Add(ComboModuleGrid.RowColumns.MassmediaName, typeof(string));
			rows.Columns.Add(ComboModuleGrid.RowColumns.CampaignId, typeof(int));
			rows.Columns.Add(ComboModuleGrid.RowColumns.PaymentTypeId, typeof(int));
			rows.Columns.Add(ComboModuleGrid.RowColumns.AgencyId, typeof(int));
			rows.Columns.Add(ComboModuleGrid.RowColumns.PaymentTypeName, typeof(string));
			rows.Columns.Add(COLUMN_AGENCY_NAME, typeof(string));   // для подписи, если тип оплаты совпал

			DataTable campaigns = ModuleCampaigns;
			HashSet<string> added = new HashSet<string>();

			if (issues != null)
				foreach (DataRow issue in issues.Rows)
				{
					int moduleID = Convert.ToInt32(issue[ComboModule.ParamNames.ModuleId]);
					int campaignID = Convert.ToInt32(issue[Campaign.ParamNames.CampaignId]);
					if (!added.Add(moduleID + "|" + campaignID)) continue;

					DataRow campaign = FindCampaignRow(campaigns, campaignID);
					if (campaign == null) continue;

					rows.Rows.Add(moduleID, issue[ComboModule.ParamNames.ModuleName],
						issue[ComboModule.ParamNames.MassmediaId], issue[Campaign.ParamNames.MassmediaName],
						campaignID, campaign[Campaign.ParamNames.PaymentTypeID], campaign[Campaign.ParamNames.AgencyID],
						campaign[COLUMN_PAYMENT_TYPE_NAME], campaign[COLUMN_AGENCY_NAME]);
				}

			if (_comboModuleID > 0)
				foreach (DataRow module in ComboModuleContent.Rows)
				{
					int moduleID = Convert.ToInt32(module[ComboModule.ParamNames.ModuleId]);
					int massmediaID = Convert.ToInt32(module[ComboModule.ParamNames.MassmediaId]);
					int agencyID;
					_agencyByMassmedia.TryGetValue(massmediaID, out agencyID);

					DataRow campaign = FindModuleCampaignRow(campaigns, massmediaID, _paymentTypeID, agencyID);
					object campaignID = campaign == null ? (object) DBNull.Value : campaign[Campaign.ParamNames.CampaignId];
					if (campaign != null && added.Contains(moduleID + "|" + campaignID)) continue;

					rows.Rows.Add(moduleID, module[ComboModule.ParamNames.ModuleName], massmediaID,
						module[ComboModule.ParamNames.MassmediaName], campaignID, _paymentTypeID, agencyID,
						_paymentTypeName, campaign == null ? DBNull.Value : campaign[COLUMN_AGENCY_NAME]);
				}

			// один и тот же тип оплаты у двух строк модуля (кампании различаются агентством) -
			// подписываем агентство, иначе строки не отличить
			foreach (DataRow row in rows.Rows)
				if (rows.Select(string.Format("{0} = {1} AND {2} = '{3}'",
						ComboModuleGrid.RowColumns.ModuleId, row[ComboModuleGrid.RowColumns.ModuleId],
						ComboModuleGrid.RowColumns.PaymentTypeName,
						row[ComboModuleGrid.RowColumns.PaymentTypeName].ToString().Replace("'", "''"))).Length > 1)
				{
					if (row[COLUMN_AGENCY_NAME] == DBNull.Value)   // кампании ещё нет - имя агентства читаем только здесь
						row[COLUMN_AGENCY_NAME] = GetAgencyName(Convert.ToInt32(row[ComboModuleGrid.RowColumns.AgencyId]));
					row[ComboModuleGrid.RowColumns.PaymentTypeName] += ", " + row[COLUMN_AGENCY_NAME];
				}

			rows.DefaultView.Sort = string.Format("{0}, {1}, {2}", ComboModuleGrid.RowColumns.MassmediaName,
				ComboModuleGrid.RowColumns.ModuleName, ComboModuleGrid.RowColumns.PaymentTypeName);
			return rows.DefaultView.ToTable();
		}

		/// <summary>
		/// Модульные кампании акции - строки Campaigns (станция, тип оплаты, агентство, их
		/// названия). Пока акции в базе нет - null; кэш сбрасывается
		/// (<see cref="_moduleCampaigns"/> = null), когда форма создаёт или удаляет кампанию.
		/// </summary>
		private DataTable ModuleCampaigns
		{
			get
			{
				if (_action == null || _action.IsNew)
					return null;

				if (_moduleCampaigns == null)
				{
					DataTable all = _action.Campaigns(true);
					_moduleCampaigns = all.Clone();
					foreach (DataRow row in all.Rows)
						if (ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignTypeId], 0)
							== (int) Campaign.CampaignTypes.Module)
							_moduleCampaigns.ImportRow(row);
				}
				return _moduleCampaigns;
			}
		}

		private static DataRow FindCampaignRow(DataTable campaigns, int campaignID)
		{
			if (campaigns == null) return null;
			DataRow[] found = campaigns.Select(string.Format("{0} = {1}", Campaign.ParamNames.CampaignId, campaignID));
			return found.Length > 0 ? found[0] : null;
		}

		private static DataRow FindModuleCampaignRow(DataTable campaigns, int massmediaID, int paymentTypeID, int agencyID)
		{
			if (campaigns == null) return null;
			DataRow[] found = campaigns.Select(string.Format("{0} = {1} AND {2} = {3} AND {4} = {5}",
				Campaign.ParamNames.MassmediaId, massmediaID,
				Campaign.ParamNames.PaymentTypeID, paymentTypeID,
				Campaign.ParamNames.AgencyID, agencyID));
			return found.Length > 0 ? found[0] : null;
		}

		private static string GetAgencyName(int agencyID)
		{
			return agencyID == 0 ? string.Empty : Agency.GetAgencyByID(agencyID).Name;
		}

		#endregion

		#region Добавление выпуска ----------------------------

		private void OnCellClicked(ComboModuleDay day)
		{
			try
			{
				PresentationObject roller = grdRollers.SelectedObject;
				if (roller == null)
				{
					UserMessage.ShowExclamation("Выберите ролик, который нужно разместить.");
					return;
				}

				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				HashSet<int> before = SnapshotIssueIDs();
				AddModuleIssue(day, roller);
				RefreshAfterChange();
				RememberAdded(before);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Создание акции, кампании и выпуска - одной транзакцией: если выпуск не встал,
		/// не должно остаться ни пустой кампании, ни пустой акции.
		/// </summary>
		private void AddModuleIssue(ComboModuleDay day, PresentationObject roller)
		{
			bool actionCreated = false;
			bool campaignCreated = false;
			Campaign campaign = null;

			DataAccessor.BeginTransaction();
			try
			{
				actionCreated = EnsureAction();
				campaign = EnsureCampaign(day, out campaignCreated);

				ModuleIssue issue = campaign.AddModuleIssue(
					GetModule(day), roller, GetModulePricelist(day), day.Date, _position, null);

				if (issue == null)
					throw new InvalidOperationException(string.Format(
						"Модуль «{0}» ({1}) не выходит {2:dd.MM.yyyy} целиком, выпуск не создан.",
						day.ModuleName, day.MassmediaName, day.Date));

				_action.Recalculate();
				DataAccessor.CommitTransaction();
			}
			catch
			{
				DataAccessor.RollbackTransaction();

				// созданное внутри откаченной транзакции в базе не осталось - забываем и в памяти
				if (actionCreated)
				{
					if (_actionDraft != null)
					{
						// акция карточки: объект не бросаем, а возвращаем в «несохранённое»
						// состояние - следующий клик вставит её заново, и карточка её увидит
						_action.Parameters = _actionDraft;
						_action.IsNew = true;   // после Parameters: его сеттер помечает объект сохранённым
					}
					else
						_action = null;
					_campaigns.Clear();
					_campaignsCreatedThisSession.Clear();
				}
				else if (campaignCreated)
				{
					_campaigns.Remove(campaign.CampaignId);
					_campaignsCreatedThisSession.Remove(campaign.CampaignId);
				}
				_moduleCampaigns = null;

				throw;
			}
		}

		/// <summary>Создаёт акцию, если её ещё нет. Возвращает true, если создана сейчас.</summary>
		private bool EnsureAction()
		{
			// _action != null ещё не значит, что акция есть в БД: из карточки акции
			// (конструктор с SelectComboModuleStep) может прийти несохранённая новая
			// акция (IsNew) - её надо вставить здесь, иначе CampaignIUD получит
			// actionID = -1 и упрётся в FK_Campaign_Action.
			if (_action != null && !_action.IsNew) return false;

			if (_action == null)
				_action = new ActionOnMassmedia(_firm);
			else
				_actionDraft = _action.Parameters;   // акция карточки - снимок на случай отката (копия)

			_action[Classes.Action.ParamNames.IsConfirmed] = false;
			_action.Update();
			return true;
		}

		/// <summary>
		/// Кампания строки грида. У строки комбо-модуля с типом оплаты из шага кампании может
		/// ещё не быть (или она появилась после построения грида) - тогда ищем по станции, типу
		/// оплаты и агентству строки, а не найдя, создаём.
		/// </summary>
		private Campaign EnsureCampaign(ComboModuleDay day, out bool created)
		{
			created = false;

			int? campaignID = day.CampaignID;
			if (!campaignID.HasValue)
			{
				DataRow existing = FindModuleCampaignRow(ModuleCampaigns, day.MassmediaID, day.PaymentTypeID, day.AgencyID);
				if (existing != null)
					campaignID = Convert.ToInt32(existing[Campaign.ParamNames.CampaignId]);
			}
			if (campaignID.HasValue)
				return GetCampaign(campaignID.Value);

			// правка готовой акции «как есть» - кампании не создаём (их состав не наш);
			// в режиме комбо-модуля недостающую кампанию, наоборот, достраиваем
			if (_isExistingAction && !_reconstructFromComboModule)
				throw new InvalidOperationException(
					"В акции нет модульной кампании на этой радиостанции - выпуск добавить некуда.");

			if (day.AgencyID == 0)
				throw new InvalidOperationException("Для радиостанции модуля не выбрано агентство.");

			// Именно ModuleCampaign, а не общая CampaignOnMassmedia: процедуры CampaignIUD
			// привязаны к сущностям конкретных типов кампаний (91 линейная, 92 модульная,
			// 93 спонсорская, 171 пакетная) - так же выбирает сущность Campaign.SelectEntity.
			Campaign campaign = new Campaign(EntityManager.GetEntity((int) Entities.ModuleCampaign));
			campaign.Action = _action;
			campaign[Campaign.ParamNames.CampaignTypeId] = (int) Campaign.CampaignTypes.Module;
			campaign[Campaign.ParamNames.MassmediaId] = day.MassmediaID;
			campaign[Campaign.ParamNames.PaymentTypeID] = day.PaymentTypeID;
			campaign[Campaign.ParamNames.AgencyID] = day.AgencyID;
			campaign.Update();

			_campaigns[campaign.CampaignId] = campaign;
			_campaignsCreatedThisSession.Add(campaign.CampaignId);
			_moduleCampaigns = null;
			created = true;
			return campaign;
		}

		private Campaign GetCampaign(int campaignID)
		{
			Campaign campaign;
			if (!_campaigns.TryGetValue(campaignID, out campaign))
			{
				campaign = new Campaign(campaignID);
				_campaigns[campaignID] = campaign;
			}
			return campaign;
		}

		// Модуль и прайс-лист собираем из данных ячейки: ModuleIssue берёт у них только
		// идентификаторы и цену, поэтому лишний поход в базу за ними не нужен.
		private static Module GetModule(ComboModuleDay day)
		{
			Module module = new Module();
			module[Module.ParamNames.ModuleId] = day.ModuleID;
			module.IsNew = false;
			return module;
		}

		private static ModulePricelist GetModulePricelist(ComboModuleDay day)
		{
			ModulePricelist pricelist = new ModulePricelist();
			pricelist[ModulePricelist.ParamNames.ModulePriceListID] = day.ModulePriceListID;
			pricelist[ModulePricelist.ParamNames.Price] = day.Price;
			pricelist.IsNew = false;
			return pricelist;
		}

		/// <summary>
		/// Массовое добавление: выбранный ролик расставляется в каждую выделенную ячейку
		/// сетки - по одному вызову уже существующего AddModuleIssue на ячейку, в цикле,
		/// как для одиночного клика. Новых хранимых процедур не потребовалось.
		///
		/// Выделение доступно только в режиме просмотра (RawDataGridView.MultiSelect
		/// включается только там, см. ComboModuleGrid.EditMode) - тот же режим, что и у
		/// массового удаления по Del.
		/// </summary>
		private void AddIssuesInSelectedCells()
		{
			PresentationObject roller = grdRollers.SelectedObject;
			if (roller == null)
			{
				UserMessage.ShowExclamation("Выберите ролик, который нужно разместить.");
				return;
			}

			IList<ComboModuleDay> days = comboModuleGrid.GetSelectedDays();
			if (days.Count == 0) return;

			HashSet<int> before = SnapshotIssueIDs();
			int addedCount = 0;
			DataTable addErrors = SmartGrid.CreateDeleteErrorsTable();
			int errorRowNumber = 1;
			try
			{
				Cursor = Cursors.WaitCursor;
				foreach (ComboModuleDay day in days)
				{
					string objectName = string.Format("{0}, {1:dd.MM.yyyy}", day.ModuleName, day.Date);
					try
					{
						AddModuleIssue(day, roller);
						addedCount++;
					}
					catch (Exception ex)
					{
						SmartGrid.AddDeleteError(addErrors, errorRowNumber++, objectName,
							ErrorManager.GetErrorMessage(ex));
					}
				}
			}
			finally
			{
				Cursor = Cursors.Default;
			}

			if (addedCount > 0)
			{
				RefreshAfterChange();
				RememberAdded(before);
			}

			if (addErrors.Rows.Count > 0)
				SmartGrid.ShowDeleteErrors(addErrors, "Ошибки массового добавления");
			else
				UserMessage.ShowInformation(string.Format("Добавлено выпусков: {0}.", addedCount));
		}

		#endregion

		#region Отмена последнего добавления ------------------

		/// <summary>
		/// moduleIssueID выпусков акции сейчас. Снимок делается до добавления, а после -
		/// сравнивается с новым составом: чего не было, то и добавили этим действием.
		/// Через объект, который возвращает Campaign.AddModuleIssue, не выйдет: процедура
		/// ModuleIssueIUD настроена как NO_DATA, identity нового ряда в объект не попадает.
		/// </summary>
		private HashSet<int> SnapshotIssueIDs()
		{
			HashSet<int> ids = new HashSet<int>();
			if (_issues != null)
				foreach (DataRow row in _issues.Rows)
					ids.Add(Convert.ToInt32(row[Issue.ParamNames.ModuleIssueId]));
			return ids;
		}

		/// <summary>
		/// Запоминает для кнопки «Отменить» выпуски, появившиеся после снимка <paramref name="before"/>.
		/// Объекты строятся из строк уже перечитанной панели (ComboModuleIssuesRetrieve отдаёт
		/// moduleIssueID) - те же, что удаляет Del по ячейкам, так что Delete(true) на них работает.
		/// </summary>
		private void RememberAdded(HashSet<int> before)
		{
			_lastAddedIssues.Clear();

			Entity issueEntity = ModuleIssue.GetEntity();
			if (_issues != null)
				foreach (DataRow row in _issues.Rows)
				{
					int id = Convert.ToInt32(row[Issue.ParamNames.ModuleIssueId]);
					if (!before.Contains(id))
						_lastAddedIssues.Add(issueEntity.CreateObject(row));
				}

			tbbUndo.Enabled = _lastAddedIssues.Count > 0;
		}

		private void ClearLastAdded()
		{
			_lastAddedIssues.Clear();
			tbbUndo.Enabled = false;
		}

		/// <summary>
		/// Отмена последнего добавления: удаляем ровно те выпуски, что запомнили при клике или
		/// массовом Insert. Дальше - общий хвост удаления (пересчёт акции, чистка пустых
		/// кампаний), тот же паттерн, что у DeleteIssuesInSelectedCells.
		/// </summary>
		private void tbbUndo_Click(object sender, EventArgs e)
		{
			try
			{
				if (_lastAddedIssues.Count == 0) return;

				if (UserMessage.ShowQuestion(string.Format(
						"Отменить последнее добавление выпусков? ({0} шт.)", _lastAddedIssues.Count)) != DialogResult.Yes)
					return;

				List<PresentationObject> issues = new List<PresentationObject>(_lastAddedIssues);
				ClearLastAdded();

				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				List<PresentationObject> deleted = new List<PresentationObject>();
				DataTable deleteErrors = SmartGrid.CreateDeleteErrorsTable();
				int errorRowNumber = 1;
				foreach (PresentationObject issue in issues)
				{
					string objectName = string.IsNullOrEmpty(issue.Name) ? "<без названия>" : issue.Name;
					try
					{
						if (issue.Delete(true))
							deleted.Add(issue);
						else
							SmartGrid.AddDeleteError(deleteErrors, errorRowNumber++, objectName,
								string.Format("Не удалось удалить выпуск '{0}'.", objectName));
					}
					catch (Exception ex)
					{
						SmartGrid.AddDeleteError(deleteErrors, errorRowNumber++, objectName,
							ErrorManager.GetErrorMessage(ex));
					}
				}

				if (deleted.Count > 0)
					AfterIssuesDeleted();

				if (deleteErrors.Rows.Count > 0)
					SmartGrid.ShowDeleteErrors(deleteErrors, "Ошибки отмены добавления");
				else
					UserMessage.ShowInformation(string.Format("Отменено выпусков: {0}.", deleted.Count));
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		#endregion

		#region Горячие клавиши грида: Insert/Delete по ячейкам, PgUp/PgDown - период

		private void ComboModuleGrid_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Insert:
				case Keys.Delete:
					e.Handled = true;
					e.SuppressKeyPress = true;
					try
					{
						if (e.KeyCode == Keys.Insert)
							AddIssuesInSelectedCells();
						else
							DeleteIssuesInSelectedCells();
					}
					catch (Exception ex)
					{
						ErrorManager.PublishError(ex);
					}
					break;

				case Keys.PageUp:
				case Keys.PageDown:
					// Гасим клавишу, иначе DataGridView вдобавок попробует прокрутить
					// строки на страницу - уже по новым, пересобранным под другой период
					// координатам.
					e.Handled = true;
					e.SuppressKeyPress = true;
					try
					{
						if (e.KeyCode == Keys.PageUp)
							comboModuleGrid.GoToPreviousPeriod();
						else
							comboModuleGrid.GoToNextPeriod();
					}
					catch (Exception ex)
					{
						ErrorManager.PublishError(ex);
					}
					break;
			}
		}

		/// <summary>
		/// Массовое удаление выпусков в выделенных ячейках сетки - как Del по окнам тарифной
		/// сетки обычной кампании. Выпуски берём из уже загруженной таблицы панели, удаляем
		/// по одному (ModuleIssue.Delete пересчитывает акцию сам), ошибки копим и показываем.
		/// </summary>
		private void DeleteIssuesInSelectedCells()
		{
			if (_action == null) return;

			IList<ComboModuleDay> days = comboModuleGrid.GetSelectedDays();
			if (days.Count == 0 || _issues == null) return;

			List<PresentationObject> issues = GetIssuesInDays(days);
			if (issues.Count == 0)
			{
				UserMessage.ShowInformation("В выбранных ячейках нет выпусков этой акции.");
				return;
			}

			if (UserMessage.ShowQuestion(string.Format(
					"Удалить выпуски в выбранных ячейках? ({0} шт.)", issues.Count)) != DialogResult.Yes)
				return;

			List<PresentationObject> deletedObjects = new List<PresentationObject>();
			DataTable deleteErrors = SmartGrid.CreateDeleteErrorsTable();
			int errorRowNumber = 1;
			try
			{
				Cursor = Cursors.WaitCursor;
				foreach (PresentationObject issue in issues)
				{
					string objectName = string.IsNullOrEmpty(issue.Name) ? "<без названия>" : issue.Name;
					try
					{
						if (issue.Delete(true))
							deletedObjects.Add(issue);
						else
							SmartGrid.AddDeleteError(deleteErrors, errorRowNumber++, objectName,
								string.Format("Не удалось удалить выпуск '{0}'.", objectName));
					}
					catch (Exception ex)
					{
						SmartGrid.AddDeleteError(deleteErrors, errorRowNumber++, objectName,
							ErrorManager.GetErrorMessage(ex));
					}
				}
			}
			finally
			{
				Cursor = Cursors.Default;
			}

			if (deletedObjects.Count > 0)
				AfterIssuesDeleted();

			if (deleteErrors.Rows.Count > 0)
				SmartGrid.ShowDeleteErrors(deleteErrors);
			else
				UserMessage.ShowInformation(string.Format("Удалено выпусков: {0}.", deletedObjects.Count));
		}

		private List<PresentationObject> GetIssuesInDays(IList<ComboModuleDay> days)
		{
			HashSet<string> selected = new HashSet<string>();
			foreach (ComboModuleDay day in days)
				if (day.CampaignID.HasValue)   // у строки без кампании выпусков нет
					selected.Add(MakeDayKey(day.ModuleID, day.CampaignID.Value, day.Date));

			Entity issueEntity = ModuleIssue.GetEntity();
			List<PresentationObject> issues = new List<PresentationObject>();
			foreach (DataRow row in _issues.Rows)
			{
				string key = MakeDayKey(
					Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]),
					Convert.ToInt32(row[Campaign.ParamNames.CampaignId]),
					Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]));

				if (selected.Contains(key))
					issues.Add(issueEntity.CreateObject(row));
			}
			return issues;
		}

		private static string MakeDayKey(int moduleID, int campaignID, DateTime date)
		{
			return string.Format("{0}|{1}|{2:yyyyMMdd}", moduleID, campaignID, date.Date);
		}


		/// <summary>
		/// Выпуски удаляет сам SmartGrid (ModuleIssue.Delete -> ModuleIssueIUD с пересчётом
		/// акции). Нам остаётся убрать то, что осталось пустым: кампанию без выпусков, а следом
		/// и акцию без кампаний - иначе они полезут в счета и статистику с нулём.
		/// </summary>
		private void OnIssueDeleted(PresentationObject presentationObject)
		{
			AfterIssuesDeleted();
		}

		private void OnIssuesDeleted(IList<PresentationObject> presentationObjects)
		{
			AfterIssuesDeleted();
		}

		/// <summary>
		/// Общий хвост удаления - откуда бы оно ни пришло: контекстное меню, Del по строкам
		/// панели, Del по ячейкам сетки.
		///
		/// Пересчёт вызываем сами, как это делает CampaignForm.ProcessCurrentCampaignIssuesDelete.
		/// Полагаться на ModuleIssue.Delete нельзя: там переопределён Delete() без параметров,
		/// а SmartGrid и массовое удаление зовут Delete(true) - другой виртуальный метод, и
		/// пересчёт в нём не выполняется.
		/// </summary>
		private void AfterIssuesDeleted()
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				if (_action != null) _action.Recalculate();
				DeleteEmptyCampaignsAndAction();
				ClearLastAdded();   // после любого удаления запомненное «последнее добавление» неактуально
				RefreshAfterChange();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void DeleteEmptyCampaignsAndAction()
		{
			if (_action == null) return;

			// правка готовой акции «как есть» - состав кампаний не наш, чистим только выпуски
			if (_isExistingAction && !_reconstructFromComboModule) return;

			DataTable issues = ComboModule.LoadIssues(_action.ActionId);

			DataAccessor.BeginTransaction();
			try
			{
				// трогаем только кампании, созданные в этой сессии: ранее существовавшие мог
				// наполнять кто-то ещё (в мастере других кампаний у акции и нет)
				foreach (int campaignID in new List<int>(_campaignsCreatedThisSession))
				{
					if (issues.Select(string.Format("{0} = {1}",
							Campaign.ParamNames.CampaignId, campaignID)).Length > 0)
						continue;

					GetCampaign(campaignID).Delete(true);
					_campaigns.Remove(campaignID);
					_campaignsCreatedThisSession.Remove(campaignID);
					_moduleCampaigns = null;
				}

				// саму акцию удаляем только если это мы её завели (мастер), а не открыли готовую
				if (!_isExistingAction && _campaignsCreatedThisSession.Count == 0)
				{
					_action.Delete(true);
					_action = null;
				}

				DataAccessor.CommitTransaction();
			}
			catch
			{
				DataAccessor.RollbackTransaction();
				throw;
			}
		}

		#endregion

		#region Drag-and-drop переноса выпуска между ячейками -

		/// <summary>
		/// Груз переноса: модуль, кампания и день ячейки-источника + строки выпусков акции из неё.
		/// Единый тип для обоих источников drag - строки панели «Добавленные выпуски»
		/// (один выпуск) и синей ячейки грида (все выпуски акции в этой ячейке).
		/// </summary>
		private class IssueDragPayload
		{
			public readonly int SourceModuleID;
			public readonly int SourceCampaignID;
			public readonly DateTime SourceDate;
			public readonly List<DataRow> IssueRows;

			public IssueDragPayload(int sourceModuleID, int sourceCampaignID, DateTime sourceDate, List<DataRow> issueRows)
			{
				SourceModuleID = sourceModuleID;
				SourceCampaignID = sourceCampaignID;
				SourceDate = sourceDate;
				IssueRows = issueRows;
			}
		}

		private DataRow _dragSourceRow;
		private ComboModuleDay _dragCandidateDay;
		private Point _dragStartPoint;

		/// <summary>
		/// Перенос выпуска мышкой в любую ячейку грида (в т.ч. другой модуль/радиостанцию) -
		/// по образцу тарифной сетки обычной кампании (CampaignForm.EnableIssueDragDrop).
		/// Тащить можно строку панели «Добавленные выпуски» либо синюю ячейку грида (тогда
		/// переносятся все выпуски акции в этой ячейке).
		/// </summary>
		private void EnableIssueDragDrop()
		{
			DataGridView sourceGrid = grdAddedIssues.InternalGrid;
			sourceGrid.MouseDown += AddedIssues_MouseDown;
			sourceGrid.MouseMove += AddedIssues_MouseMove;

			DataGridView targetGrid = comboModuleGrid.RawDataGridView;
			targetGrid.AllowDrop = true;
			targetGrid.MouseDown += ComboGrid_MouseDown;
			targetGrid.MouseMove += ComboGrid_MouseMove;
			targetGrid.DragEnter += ComboGrid_DragEnter;
			targetGrid.DragOver += ComboGrid_DragOver;
			targetGrid.DragDrop += ComboGrid_DragDrop;
		}

		private void AddedIssues_MouseDown(object sender, MouseEventArgs e)
		{
			_dragSourceRow = null;
			if (e.Button != MouseButtons.Left || _issues == null) return;

			DataGridView grid = (DataGridView) sender;
			DataGridView.HitTestInfo hit = grid.HitTest(e.X, e.Y);
			if (hit.RowIndex < 0) return;

			DataRowView drv = grid.Rows[hit.RowIndex].DataBoundItem as DataRowView;
			if (drv == null) return;

			_dragSourceRow = drv.Row;
			_dragStartPoint = e.Location;
		}

		private void AddedIssues_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || _dragSourceRow == null || !DragThresholdExceeded(e)) return;

			DataRow row = _dragSourceRow;
			_dragSourceRow = null;

			IssueDragPayload payload = new IssueDragPayload(
				Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]),
				Convert.ToInt32(row[Campaign.ParamNames.CampaignId]),
				Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]).Date,
				new List<DataRow> { row });
			grdAddedIssues.InternalGrid.DoDragDrop(payload, DragDropEffects.Move);
		}

		/// <summary>
		/// Старт переноса прямо из синей ячейки грида (в ней есть выпуски акции). Только в
		/// режиме просмотра: в режиме добавления клик по ячейке ставит выпуск, а выделение
		/// прямоугольником нужно для массового Del - трогаем жест лишь на синей ячейке.
		/// </summary>
		private void ComboGrid_MouseDown(object sender, MouseEventArgs e)
		{
			_dragCandidateDay = null;
			if (e.Button != MouseButtons.Left || comboModuleGrid.EditMode || _issues == null) return;

			DataGridView grid = (DataGridView) sender;
			DataGridView.HitTestInfo hit = grid.HitTest(e.X, e.Y);
			ComboModuleDay day = comboModuleGrid.GetDayAt(hit.RowIndex, hit.ColumnIndex);
			if (day == null || GetIssueRowsInDay(day).Count == 0) return;

			_dragCandidateDay = day;
			_dragStartPoint = e.Location;
		}

		private void ComboGrid_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left || _dragCandidateDay == null || !DragThresholdExceeded(e)) return;

			ComboModuleDay day = _dragCandidateDay;
			_dragCandidateDay = null;

			List<DataRow> rows = GetIssueRowsInDay(day);
			if (rows.Count == 0) return;

			IssueDragPayload payload = new IssueDragPayload(day.ModuleID, day.CampaignID.Value, day.Date.Date, rows);
			((DataGridView) sender).DoDragDrop(payload, DragDropEffects.Move);
		}

		private bool DragThresholdExceeded(MouseEventArgs e)
		{
			Size dragSize = SystemInformation.DragSize;
			return Math.Abs(e.X - _dragStartPoint.X) > dragSize.Width
				|| Math.Abs(e.Y - _dragStartPoint.Y) > dragSize.Height;
		}

		/// <summary>Строки панели с выпусками акции в этой ячейке (тот же модуль, кампания и день).</summary>
		private List<DataRow> GetIssueRowsInDay(ComboModuleDay day)
		{
			List<DataRow> rows = new List<DataRow>();
			if (_issues == null || !day.CampaignID.HasValue) return rows;

			foreach (DataRow row in _issues.Rows)
			{
				if (Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]) != day.ModuleID) continue;
				if (Convert.ToInt32(row[Campaign.ParamNames.CampaignId]) != day.CampaignID.Value) continue;
				if (Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]).Date != day.Date.Date) continue;
				rows.Add(row);
			}
			return rows;
		}

		private void ComboGrid_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(typeof(IssueDragPayload))
				? DragDropEffects.Move
				: DragDropEffects.None;
		}

		private void ComboGrid_DragOver(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.None;
			IssueDragPayload payload = e.Data.GetData(typeof(IssueDragPayload)) as IssueDragPayload;
			if (payload == null) return;

			if (ResolveDropTarget(payload, e) != null)
				e.Effect = DragDropEffects.Move;
		}

		private void ComboGrid_DragDrop(object sender, DragEventArgs e)
		{
			try
			{
				IssueDragPayload payload = e.Data.GetData(typeof(IssueDragPayload)) as IssueDragPayload;
				if (payload == null) return;

				ComboModuleDay target = ResolveDropTarget(payload, e);
				if (target == null) return;

				string where = string.Format("модуль «{0}» ({1}, {2}), {3:dd.MM.yyyy}",
					target.ModuleName, target.MassmediaName, target.PaymentTypeName, target.Date);
				string question = payload.IssueRows.Count == 1
					? string.Format("Перенести выпуск в {0}?", where)
					: string.Format("Перенести выпуски ({0} шт.) в {1}?", payload.IssueRows.Count, where);
				if (UserMessage.ShowQuestion(question) != DialogResult.Yes) return;

				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				MoveIssuesToCell(payload, target);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Ячейка под курсором, если на неё можно перенести груз: любой модуль/день, где
		/// модуль в этот день выходит (ячейка не пустая), кроме самой ячейки-источника.
		/// Иначе null.
		/// </summary>
		private ComboModuleDay ResolveDropTarget(IssueDragPayload payload, DragEventArgs e)
		{
			DataGridView grid = comboModuleGrid.RawDataGridView;
			Point pt = grid.PointToClient(new Point(e.X, e.Y));
			DataGridView.HitTestInfo hit = grid.HitTest(pt.X, pt.Y);

			ComboModuleDay target = comboModuleGrid.GetDayAt(hit.RowIndex, hit.ColumnIndex);
			if (target == null) return null;

			// та же ячейка (тот же модуль, кампания и день) - переносить некуда
			if (target.ModuleID == payload.SourceModuleID && target.CampaignID == payload.SourceCampaignID
				&& target.Date.Date == payload.SourceDate.Date)
				return null;

			return target;
		}

		/// <summary>
		/// Перенос выпусков в любую ячейку - одной транзакцией: удаляем исходные ModuleIssue
		/// и создаём такие же в целевой ячейке (ролик и позиция - из исходного выпуска,
		/// модуль/прайс-лист/цена - целевой ячейки), затем пересчёт акции. Паттерн
		/// BeginTransaction+Delete+AddModuleIssue+Recalculate тот же, что в
		/// CampaignForm.MoveIssuesToWindow.
		///
		/// Целевой модуль может быть на другой радиостанции - тогда это перенос в другую
		/// модульную кампанию: недостающую кампанию заводит EnsureCampaign (как при клике),
		/// а опустевшую исходную убирает DeleteEmptyCampaignsAndAction (как после удаления).
		/// Оба шага уже используются формой, здесь только собраны вместе.
		/// </summary>
		private void MoveIssuesToCell(IssueDragPayload payload, ComboModuleDay target)
		{
			Entity issueEntity = ModuleIssue.GetEntity();
			List<int> campaignsCreated = new List<int>();

			DataAccessor.BeginTransaction();
			try
			{
				foreach (DataRow row in payload.IssueRows)
				{
					Roller roller = new Roller(Convert.ToInt32(row[Roller.ParamNames.RollerId]));
					RollerPositions position = (RollerPositions)
						ParseHelper.GetInt32FromObject(row[Issue.ParamNames.PositionId], 0);

					if (!issueEntity.CreateObject(row).Delete(true))
						throw new InvalidOperationException("Не удалось удалить выпуск из исходной ячейки.");

					// в кампанию строки, куда отпустили (Д-7): между строками одного модуля
					// выпуск меняет кампанию - это видно по колонке «Тип оплаты»
					bool campaignCreated;
					Campaign campaign = EnsureCampaign(target, out campaignCreated);
					if (campaignCreated) campaignsCreated.Add(campaign.CampaignId);

					ModuleIssue moved = campaign.AddModuleIssue(
						GetModule(target), roller, GetModulePricelist(target), target.Date, position, null);
					if (moved == null)
						throw new InvalidOperationException(string.Format(
							"Модуль «{0}» ({1}) не выходит {2:dd.MM.yyyy} целиком, выпуск не перенесён.",
							target.ModuleName, target.MassmediaName, target.Date));
				}

				_action.Recalculate();
				DataAccessor.CommitTransaction();
			}
			catch
			{
				DataAccessor.RollbackTransaction();

				// кампании, заведённые в откаченной транзакции, в базе не остались - забываем и в памяти
				foreach (int campaignID in campaignsCreated)
				{
					_campaigns.Remove(campaignID);
					_campaignsCreatedThisSession.Remove(campaignID);
				}
				_moduleCampaigns = null;
				throw;
			}

			DeleteEmptyCampaignsAndAction();   // источник мог остаться пустым
			ClearLastAdded();                  // перенос - не «последнее добавление», кнопка «Отменить» его не касается
			RefreshAfterChange();
		}

		#endregion

		#region Обновление после изменений --------------------

		private void RefreshAfterChange()
		{
			comboModuleGrid.RefreshGrid();   // выпуски раздаст OnGridRefreshed
			ShowStatistics();
		}

		/// <summary>
		/// Грид перестроился - в том числе при листании стрелками. Выпуски акции нужны и
		/// списку, и самому гриду (подсветка и счётчик по дням), поэтому грузим их один раз.
		/// </summary>
		private void OnGridRefreshed()
		{
			// выпуски прочитаны при построении строк грида (BuildGridRows)
			DataTable issues = _issuesForGrid;
			_issues = issues;

			comboModuleGrid.MarkIssues(issues);
			ShowIssuesCount(issues);
			ShowAddedIssues(issues);
		}

		private void ShowAddedIssues(DataTable issues)
		{
			grdAddedIssues.DataSource = issues == null ? null : issues.DefaultView;
		}

		private void ShowIssuesCount(DataTable issues)
		{
			Dictionary<DateTime, int> countByDate = new Dictionary<DateTime, int>();
			if (issues != null)
				foreach (DataRow row in issues.Rows)
				{
					DateTime date = Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]).Date;
					int count;
					countByDate.TryGetValue(date, out count);
					countByDate[date] = count + 1;
				}

			for (DateTime date = comboModuleGrid.StartDate; date <= comboModuleGrid.FinishDate; date = date.AddDays(1))
			{
				int count;
				countByDate.TryGetValue(date, out count);
				comboModuleGrid.SetIssuesCount(date, count);
			}
		}

		private void ShowStatistics()
		{
			if (_action == null)
			{
				lstStat.Items.Clear();
				tbSetActionPrice.Enabled = false;
				return;
			}

			_action.Refresh();
			_action.DisplayData(lstStat);
			tbSetActionPrice.Enabled = _action.TariffPrice != 0;
		}

		#endregion

		#region Режим периода ---------------------------------

		private ComboModulePeriodMode LoadPeriodMode()
		{
			return UserSettings.Load(SETTING_PERIOD_MODE) == ComboModulePeriodMode.Month.ToString()
				? ComboModulePeriodMode.Month
				: ComboModulePeriodMode.Week;
		}

		private void UpdatePeriodModeCaption()
		{
			tbbPeriodMode.Text = comboModuleGrid.PeriodMode == ComboModulePeriodMode.Month ? "Месяц" : "Неделя";
		}

		private void tbbPeriodMode_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ComboModulePeriodMode mode = (ComboModulePeriodMode)
					Enum.Parse(typeof(ComboModulePeriodMode), e.ClickedItem.Tag.ToString());
				if (mode == comboModuleGrid.PeriodMode) return;

				comboModuleGrid.PeriodMode = mode;
				UpdatePeriodModeCaption();
				UserSettings.Save(SETTING_PERIOD_MODE, mode.ToString());
				RefreshAfterChange();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		#endregion

		private void tbbShowUnconfirmed_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				comboModuleGrid.ShowUnconfirmed = tbbShowUnconfirmed.Checked;
				RefreshAfterChange();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// «Номера роликов» — переключает ячейки грида между остатком времени и номерами
		/// роликов акции, размещённых в модуле. Логика 1 в 1 с CampaignForm: карта строится
		/// из списка роликов (номер = позиция строки, как в колонке «№»), грид сам
		/// перерисовывает тексты без похода в БД, поэтому RefreshAfterChange здесь не нужен.
		/// </summary>
		private void tbbShowRollerNumbers_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				comboModuleGrid.SetRollerNumbersMode(
					tbbShowRollerNumbers.Checked,
					tbbShowRollerNumbers.Checked ? BuildRollerNumbersMap() : null);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		// rollerID -> номер ролика, ровно тот, что показывает колонка «№» grdRollers
		// (SmartGrid.ShowRowNumbers) в её текущем порядке строк на этот момент.
		private Dictionary<int, int> BuildRollerNumbersMap()
		{
			Dictionary<int, int> map = new Dictionary<int, int>();
			DataView view = grdRollers.DataSource;
			if (view == null) return map;

			for (int i = 0; i < view.Count; i++)
			{
				int rollerId = ParseHelper.GetInt32FromObject(view[i][Roller.ParamNames.RollerId], 0);
				if (rollerId != 0)
					map[rollerId] = i + 1;
			}
			return map;
		}

		private void tbbRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				tbbStart.Checked = false;
				RefreshAfterChange();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbbJump_Click(object sender, EventArgs e)
		{
			try
			{
				if (!comboModuleGrid.SelectDate2Jump()) return;

				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				RefreshAfterChange();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbbStart_CheckedChanged(object sender, EventArgs e)
		{
			try
			{
				comboModuleGrid.EditMode = tbbStart.Checked;
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void tbbPosition_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				tbbPosition.Text = e.ClickedItem.Text;
				_position = (RollerPositions) Enum.Parse(typeof(RollerPositions), e.ClickedItem.Tag.ToString());

				// от позиции зависит подсветка жирным, поэтому грид надо перестроить
				comboModuleGrid.RollerPosition = _position;
				RefreshAfterChange();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		#region Ролики: пустышка, прослушивание, предметы рекламы, экспорт ----

		/// <summary>Логика 1 в 1 с CampaignForm.CreateMuteRoller, только без гейта по типу кампании.</summary>
		private void tsbMuteRoller_Click(object sender, EventArgs e)
		{
			try
			{
				RollerMuteSelect frm = new RollerMuteSelect();
				if (frm.ShowDialog(this) == DialogResult.OK && frm.TimeDuration > 0)
				{
					Cursor.Current = Cursors.WaitCursor;
					PresentationObject newRoller = MuteRoller.GetRoller(frm.TimeDuration, _firm.FirmId, null);
					ActionRoller roller = new ActionRoller(newRoller);

					grdRollers.AddRow(roller);
					grdRollers.SelectedObject = roller;
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void tbbPlay_Click(object sender, EventArgs e)
		{
			_mediaControl.Play(grdRollers.SelectedObject);
		}

		private void tsbStop_Click(object sender, EventArgs e)
		{
			_mediaControl.Stop();
		}

		/// <summary>
		/// Логика диалога 1 в 1 с CampaignForm.tbbAdvertType_DropDownItemClicked. Критерий -
		/// как с позиционированием: предмет рекламы должен выполняться во всех окнах модуля
		/// (ComboModuleGrid.AdvertType/AdvertTypePresence, MarkFilteredCells).
		/// </summary>
		private void tbbAdvertType_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				AdvertTypePresences presence =
					(AdvertTypePresences) Enum.Parse(typeof(AdvertTypePresences), e.ClickedItem.Tag.ToString());

				if (presence != AdvertTypePresences.Undefined)
				{
					TreeViewSelector tvSelector = new TreeViewSelector(
						RelationManager.GetScenario(RelationScenarios.AdvertTypes), "Предметы рекламы");
					if (tvSelector.ShowDialog(this) != DialogResult.OK) return;

					comboModuleGrid.AdvertType = tvSelector.SelectedObject;
					comboModuleGrid.AdvertTypePresence = presence;
					tbbAdvertType.Text = (presence == AdvertTypePresences.Exist ? "Есть " : "Нет ")
						+ tvSelector.SelectedObject.Name;
				}
				else
				{
					comboModuleGrid.AdvertType = null;
					comboModuleGrid.AdvertTypePresence = AdvertTypePresences.Undefined;
					tbbAdvertType.Text = e.ClickedItem.Text;
				}

				RefreshAfterChange();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ExportManager.ExportExcel(comboModuleGrid.RawDataGridView, null);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void tbSetActionPrice_Click(object sender, EventArgs e)
		{
			try
			{
				if (_action == null)
				{
					UserMessage.ShowExclamation("В акции ещё нет ни одного размещённого выпуска.");
					return;
				}

				if (ActionForm.SetActionPrice(_action, this))
					ShowStatistics();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				UseWaitCursor = false;
			}
		}

		#endregion
	}
}
