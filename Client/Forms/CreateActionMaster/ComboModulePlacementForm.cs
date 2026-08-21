using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
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
	internal partial class ComboModulePlacementForm : Form
	{
		private const string SETTING_PERIOD_MODE = "ComboModulePlacementPeriodMode";

		private readonly Firm _firm;
		private readonly int _comboModuleID;
		private readonly string _comboModuleName;
		private readonly int _paymentTypeID;
		private readonly Dictionary<int, int> _agencyByMassmedia;

		/// <summary>
		/// Форма открыта на уже существующей акции (из её карточки), а не из мастера.
		/// В этом режиме кампании не создаются и не удаляются: их состав - дело акции,
		/// а не наше, мы правим только выпуски.
		/// </summary>
		private readonly bool _isExistingAction;

		private RollerPositions _position = RollerPositions.Undefined;

		/// <summary>
		/// Акция и кампании создаются лениво, по первому клику: пока менеджер ничего не
		/// разместил, в базе не должно оставаться пустой акции.
		/// </summary>
		private ActionOnMassmedia _action;

		private readonly Dictionary<int, Campaign> _campaignByMassmedia = new Dictionary<int, Campaign>();

		/// <summary>Выпуски акции, показанные в панели, - из них же берём удаляемые по Del.</summary>
		private DataTable _issues;

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
		}

		/// <summary>Размещение по комбо-модулю: акция и кампании появятся по первому клику.</summary>
		public ComboModulePlacementForm(Firm firm, SelectComboModuleStep step) : this()
		{
			_firm = firm;
			_comboModuleID = step.ComboModuleID;
			_comboModuleName = step.ComboModuleName;
			_paymentTypeID = step.PaymentTypeID;
			_agencyByMassmedia = step.AgencyByMassmedia;
		}

		/// <summary>
		/// Редактирование готовой акции из её карточки: строки грида - модули, уже
		/// размещённые в акции, комбо-модуль ни при чём.
		/// </summary>
		public ComboModulePlacementForm(ActionOnMassmedia action) : this()
		{
			_action = action;
			_firm = action.Firm;
			_comboModuleName = string.Format("акция №{0}", action.ActionId);
			_agencyByMassmedia = new Dictionary<int, int>();
			_isExistingAction = true;
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				base.OnLoad(e);

				Text = string.Format("Размещение комбо-модулями: {0} - {1}", _firm.Name, _comboModuleName);

				InitRollersList();
				InitAddedIssuesList();
				InitComboModuleGrid();
				ShowStatistics();   // у готовой акции она есть сразу, а не после первого клика
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void InitRollersList()
		{
			grdRollers.Entity = EntityManager.GetEntity((int) Entities.ActionRollers);
			grdRollers.DataSource = _firm.GetRollers().DefaultView;
		}

		private void InitAddedIssuesList()
		{
			grdAddedIssues.Entity = EntityManager.GetEntity((int) Entities.ComboModuleIssue);
			grdAddedIssues.ObjectDeleted += OnIssueDeleted;     // удалили одну строку
			grdAddedIssues.ObjectsDeleted += OnIssuesDeleted;   // удалили несколько
			grdAddedIssues.MultiSelect = true;   // Del по нескольким строкам умеет сам SmartGrid
		}

		private void InitComboModuleGrid()
		{
			comboModuleGrid.ComboModuleID = _comboModuleID;
			if (_action != null) comboModuleGrid.ActionID = _action.ActionId;
			comboModuleGrid.PeriodMode = LoadPeriodMode();
			comboModuleGrid.ShowUnconfirmed = tbbShowUnconfirmed.Checked;
			comboModuleGrid.CellClicked += OnCellClicked;
			comboModuleGrid.GridRefreshed += OnGridRefreshed;
			comboModuleGrid.RawDataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
			comboModuleGrid.RawDataGridView.KeyDown += ComboModuleGrid_KeyDown;
			UpdatePeriodModeCaption();
			comboModuleGrid.RefreshGrid();
		}

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

				AddModuleIssue(day, roller);
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
		/// Создание акции, кампании и выпуска - одной транзакцией: если выпуск не встал,
		/// не должно остаться ни пустой кампании, ни пустой акции.
		/// </summary>
		private void AddModuleIssue(ComboModuleDay day, PresentationObject roller)
		{
			bool actionCreated = false;
			bool campaignCreated = false;

			DataAccessor.BeginTransaction();
			try
			{
				actionCreated = EnsureAction();
				Campaign campaign = EnsureCampaign(day.MassmediaID, out campaignCreated);

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
					_action = null;
					_campaignByMassmedia.Clear();
				}
				else if (campaignCreated)
					_campaignByMassmedia.Remove(day.MassmediaID);

				throw;
			}
		}

		/// <summary>Создаёт акцию, если её ещё нет. Возвращает true, если создана сейчас.</summary>
		private bool EnsureAction()
		{
			if (_action != null) return false;

			_action = new ActionOnMassmedia(_firm);
			_action[Classes.Action.ParamNames.IsConfirmed] = false;
			_action.Update();
			return true;
		}

		/// <summary>Создаёт модульную кампанию на радиостанции модуля, если её ещё нет.</summary>
		private Campaign EnsureCampaign(int massmediaID, out bool created)
		{
			created = false;

			Campaign campaign;
			if (_campaignByMassmedia.TryGetValue(massmediaID, out campaign))
				return campaign;

			if (_isExistingAction)
				throw new InvalidOperationException(
					"В акции нет модульной кампании на этой радиостанции - выпуск добавить некуда.");

			int agencyID;
			if (!_agencyByMassmedia.TryGetValue(massmediaID, out agencyID))
				throw new InvalidOperationException("Для радиостанции модуля не выбрано агентство.");

			// Именно ModuleCampaign, а не общая CampaignOnMassmedia: процедуры CampaignIUD
			// привязаны к сущностям конкретных типов кампаний (91 линейная, 92 модульная,
			// 93 спонсорская, 171 пакетная) - так же выбирает сущность Campaign.SelectEntity.
			campaign = new Campaign(EntityManager.GetEntity((int) Entities.ModuleCampaign));
			campaign.Action = _action;
			campaign[Campaign.ParamNames.CampaignTypeId] = (int) Campaign.CampaignTypes.Module;
			campaign[Campaign.ParamNames.MassmediaId] = massmediaID;
			campaign[Campaign.ParamNames.PaymentTypeID] = _paymentTypeID;
			campaign[Campaign.ParamNames.AgencyID] = agencyID;
			campaign.Update();

			_campaignByMassmedia[massmediaID] = campaign;
			created = true;
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

		#endregion

		#region Удаление выпуска ------------------------------

		private void ComboModuleGrid_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.Delete) return;

			e.Handled = true;
			e.SuppressKeyPress = true;
			try
			{
				DeleteIssuesInSelectedCells();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
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
				selected.Add(MakeDayKey(day.ModuleID, day.Date));

			Entity issueEntity = EntityManager.GetEntity((int) Entities.ComboModuleIssue);
			List<PresentationObject> issues = new List<PresentationObject>();
			foreach (DataRow row in _issues.Rows)
			{
				string key = MakeDayKey(
					Convert.ToInt32(row[ComboModule.ParamNames.ModuleId]),
					Convert.ToDateTime(row[ComboModule.ParamNames.IssueDate]));

				if (selected.Contains(key))
					issues.Add(issueEntity.CreateObject(row));
			}
			return issues;
		}

		private static string MakeDayKey(int moduleID, DateTime date)
		{
			return string.Format("{0}|{1:yyyyMMdd}", moduleID, date.Date);
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
			// в готовой акции состав кампаний не наш - чистим только выпуски
			if (_action == null || _isExistingAction) return;

			DataTable issues = ComboModule.LoadIssues(_action.ActionId);

			DataAccessor.BeginTransaction();
			try
			{
				foreach (KeyValuePair<int, Campaign> pair in new List<KeyValuePair<int, Campaign>>(_campaignByMassmedia))
				{
					if (issues.Select(string.Format("{0} = {1}",
							Campaign.ParamNames.CampaignId, pair.Value.CampaignId)).Length > 0)
						continue;

					pair.Value.Delete(true);
					_campaignByMassmedia.Remove(pair.Key);
				}

				if (_campaignByMassmedia.Count == 0)
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
			DataTable issues = _action == null ? null : ComboModule.LoadIssues(_action.ActionId);
			_issues = issues;

			RememberCampaigns(issues);
			comboModuleGrid.MarkIssues(issues);
			ShowIssuesCount(issues);
			ShowAddedIssues(issues);
		}

		/// <summary>
		/// Кампании готовой акции запоминаем из её выпусков: там есть и радиостанция, и
		/// кампания. Так при редактировании выпуск ложится в существующую кампанию, а не
		/// создаёт новую.
		/// </summary>
		private void RememberCampaigns(DataTable issues)
		{
			if (issues == null) return;

			foreach (DataRow row in issues.Rows)
			{
				int massmediaID = Convert.ToInt32(row[ComboModule.ParamNames.MassmediaId]);
				if (_campaignByMassmedia.ContainsKey(massmediaID)) continue;

				_campaignByMassmedia[massmediaID] =
					new Campaign(Convert.ToInt32(row[Campaign.ParamNames.CampaignId]));
			}
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
				return;
			}

			_action.Refresh();
			_action.DisplayData(lstStat);
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
				tbbPosition.Text = e.ClickedItem.Text;
				_position = (RollerPositions) Enum.Parse(typeof(RollerPositions), e.ClickedItem.Tag.ToString());
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}
	}
}
