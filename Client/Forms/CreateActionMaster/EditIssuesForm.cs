using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Classes.Domain;
using Merlin.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FogSoft.WinForm.Forms;

namespace Merlin.Forms.CreateActionMaster
{
	internal partial class EditIssuesForm : CampaignForm
	{
        // Селектор атрибутов сущности 91 для чек-листа кампаний веера
        // (ArtvisDB/Scripts/veer-campaign-selection-seed.sql): радиостанция, тип оплаты, агентство.
        private const int VeerCampaignListSelector = 4;

        private readonly ActionOnMassmedia _action;
        private System.Data.DataView _campaignsView;
        private DateTime? _dragSourceSlotDate;
        private System.Data.DataRow _draggingAddedIssueRow;
        private bool _checklistRefreshPending;
        private ToolStripButton _tbbDeleteDuplicates;
        private ToolStripButton _tbbFillToIntersection;

        private EditIssuesForm()
		{
			InitializeComponent();
		}

		public EditIssuesForm(Firm firm, ActionOnMassmedia action, int massmediasCount)
			: this()
		{
			_firm = firm;
            _action = action;
            _tariffGrid = new TariffWithRangeGrid(action, massmediasCount);
            //SetTariffGrid(new TariffWithRangeGrid(action, massmediasCount));
            HelpFileName = "veer.html";
		}

		protected override Firm Firm
		{
			get { return _firm; }
		}

		protected override ActionOnMassmedia CampaignAction
		{
			get { return _action; }
		}

		protected override void RefreshAfterActionPriceChange()
		{
			_action.DisplayData(lstStat);
		}

		protected override void SetFormCaption()
		{
			//base.SetFormCaption();
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				// Чек-лист заполняется до base.OnLoad: базовая форма по ходу загрузки уже
				// строит сетку, а она должна строиться в контексте выбранных кампаний.
				InitCampaignsChecklist();

				base.OnLoad(e);

				// Remove All Issues Grid
				splitContainer4.Panel1Collapsed = true;
				// Дизайнерская высота splitContainer3 (285px) рассчитана с запасом; 7 строк
				// статистики (lstStat.ItemHeight=25) реально занимают ~187px — лишнее место
				// отдаём вниз, «Добавленным выпускам».
				splitContainer3.SplitterDistance = lstStat.ItemHeight * 7 + 12;
				// SplitContainer.FixedPanel=Panel2, выставленный в Designer.cs, не переживает
				// первый реальный layout формы (там Panel2 ужимался до дизайнерского значения
				// ~74px вместо ожидаемого) — высоту grdCampaigns фиксируем здесь, когда форма
				// уже реально размещена и splitContainerCampaigns.Height настоящий. 220px хватает
				// на заголовок + 5-6 строк чек-листа без лишнего запаса под «Добавленные выпуски».
				const int campaignsHeight = 220;
				int available = splitContainerCampaigns.Height - splitContainerCampaigns.SplitterWidth;
				if (available > campaignsHeight + splitContainerCampaigns.Panel1MinSize)
					splitContainerCampaigns.SplitterDistance = available - campaignsHeight;
                tbbTemplate.Visible = true;
                tbbTemplateUndo.Visible = true;
                grdCurrentCampaignIssues.Caption = "Добавленные выпуски";

				// Страховка: массовая замена роликов доступна только при видимых номерах
				// роликов в ячейках — иначе пользователь меняет ролики вслепую, не видя,
				// что стоит в выделенных окнах. Удаление дублей и выравнивание роликов — по той же
				// причине: они чинят то, что видно только по значкам Н/Д.
				AddRollerMismatchButtons();
				SetRollerNumberActionsEnabled();
				btnShowRollerNumbers.CheckedChanged += (s, args) => SetRollerNumberActionsEnabled();

                RefreshGrid();
				_tariffGrid.GridRefreshed += TariffGridRefreshed;
				_action.DisplayData(lstStat);
				// Колонка «№» — номер ролика из списка «Ролики» (ShowCurrentIssues кладёт его в
				// колонку таблицы); служебная колонка SmartGrid, метаданные не нужны.
				grdCurrentCampaignIssues.ShowRowNumbers = true;
				grdCurrentCampaignIssues.RowNumberSource = ActionOnMassmedia.RollerNumberColumn;
				grdCurrentCampaignIssues.Entity = EntityManager.GetEntity((int)Entities.MasterIssues);
				ShowCurrentIssues(_tariffGrid as TariffWithRangeGrid);
				EnableWindowSelectionActions();
				EnableRangeIssueDragDrop();
				EnableCellTooltips();

				// Веер работает только с линейными кампаниями. Если в акции их нет (модульная/
				// спонсорская), сетка пустая — гасим тулбар, чтобы его кнопки не падали на пустоте.
				if (_campaignsView.Count == 0 || !((TariffWithRangeGrid)_tariffGrid).HasSlots)
				{
					DisableToolbar();
					UserMessage.ShowInformation(
						"В акции нет линейных кампаний. Веерное размещение доступно только для линейных кампаний — " +
						"модульные и спонсорские размещаются отдельно.");
				}
            }
			catch (Exception ex)
			{
                ErrorManager.PublishError(ex);
            }
		}

        /// <summary>
        /// Чек-лист получает данные в OnLoad, до раскладки формы, когда его строк ещё не видно:
        /// режим DisplayedCells тогда подгоняет служебные колонки (галочки, иконка) под пустые
        /// заголовки, а сами галочки не прорисовываются до первого клика. Теперь строки видны —
        /// пересчитываем ширины и перерисовываем.
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            DataGridView grid = grdCampaigns.InternalGrid;
            grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            grid.Invalidate();
        }

        /// <summary>
        /// Чек-лист линейных кампаний акции: с какими из них работает веер. По умолчанию
        /// отмечены все. Каждое изменение галочки применяется сразу (см. CampaignSelectionChanged).
        /// </summary>
        private void InitCampaignsChecklist()
        {
            Entity entity = (Entity)EntityManager.GetEntity((int)Entities.GeneralCampaign).Clone();
            entity.AttributeSelector = VeerCampaignListSelector;
            grdCampaigns.Entity = entity;

            _campaignsView = new System.Data.DataView(_action.Campaigns())
            {
                RowFilter = string.Format("{0} = {1}",
                    Campaign.ParamNames.CampaignTypeId, (int)Campaign.CampaignTypes.Simple)
            };
            grdCampaigns.DataSource = _campaignsView;

            foreach (System.Data.DataRowView rowView in _campaignsView)
                rowView[FogSoft.WinForm.Controls.SmartGrid.COL_IsSelected] = true;

            // Слушаем таблицу, а не ObjectChecked грида: «отметить все» в шапке SmartGrid
            // пишет колонку напрямую и события не поднимает.
            _campaignsView.Table.ColumnChanged += CampaignSelectionChanged;
        }

        /// <summary>
        /// Каждая поставленная или снятая галочка сразу применяет выбор кампаний (RefreshGrid),
        /// без отдельного нажатия «Обновить». «Отметить все»/«снять все» в шапке списка пишет
        /// колонку напрямую по каждой строке — событие прилетает по разу на строку; чтобы не
        /// уйти в базу отдельным запросом на каждую, сворачиваем всю пачку в один RefreshGrid
        /// через BeginInvoke (он выполнится один раз, уже после того как синхронный цикл
        /// изменений колонки закончится).
        /// </summary>
        private void CampaignSelectionChanged(object sender, System.Data.DataColumnChangeEventArgs e)
        {
            try
            {
                if (e.Column.ColumnName != FogSoft.WinForm.Controls.SmartGrid.COL_IsSelected)
                    return;
                if (_checklistRefreshPending)
                    return;

                _checklistRefreshPending = true;
                BeginInvoke((MethodInvoker)delegate
                {
                    _checklistRefreshPending = false;
                    try
                    {
                        RefreshGrid();
                    }
                    catch (Exception ex)
                    {
                        ErrorManager.PublishError(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        /// <summary>
        /// Отмеченные кампании. Читаем колонку чекбоксов напрямую, а не Added2Checked:
        /// последний наполняется только кликами пользователя и при предотмеченных строках пуст.
        /// </summary>
        private List<int> GetCheckedCampaignIds()
        {
            List<int> ids = new List<int>();
            if (_campaignsView == null)
                return ids;

            foreach (System.Data.DataRowView rowView in _campaignsView)
            {
                object isSelected = rowView[FogSoft.WinForm.Controls.SmartGrid.COL_IsSelected];
                if (isSelected is bool && (bool)isSelected)
                    ids.Add(Convert.ToInt32(rowView[Campaign.ParamNames.CampaignId]));
            }
            return ids;
        }

        /// <summary>
        /// Применяет текущий выбор кампаний: пересобирает «Добавленные выпуски» (пересечение
        /// слотов считается по выбранным кампаниям) и перезабрасывает сетку. Вызывается сама при
        /// каждом изменении чек-листа (см. CampaignSelectionChanged) и по-прежнему доступна с
        /// тулбара («Обновить») для обычного обновления сетки. Выпуски, добавленные ранее по
        /// другим кампаниям, остаются в базе, но из веера уходят — это ожидаемое поведение.
        /// </summary>
        protected override void RefreshGrid()
        {
            TariffWithRangeGrid rangeGrid = _tariffGrid as TariffWithRangeGrid;
            if (rangeGrid != null && _campaignsView != null)
            {
                List<int> checkedIds = GetCheckedCampaignIds();
                rangeGrid.SetSelectedCampaigns(checkedIds, checkedIds.Count);
                SetToolbarEnabled(checkedIds.Count > 0);
            }

            base.RefreshGrid();
        }

        protected override void ProcessToolbar()
        {
            base.ProcessToolbar();
			tsbMuteRoller.Enabled = true;
			tbMarkPrimeWindows.Visible = true;
			tbbReplaceRoller.Visible = true;
        }

        protected override void ShowWindowIssues(ITariffWindow tariffWindow)
        {
            //base.ShowWindowIssues(tariffWindow);
            TariffGridRefreshed();
        }

	    private void ShowCurrentIssues(TariffWithRangeGrid grid)
	    {
			FillAddedIssuesRollerNumbers(grid.AddedIssues);
			grdCurrentCampaignIssues.DataSource = grid.AddedIssues.DefaultView;
	    }

        /// <summary>
        /// Колонка «№» в «Добавленных выпусках» — те же номера, что в гриде «Ролики» фирмы (и в
        /// ячейках при включённых номерах роликов). Считаются на лету при каждом показе списка:
        /// если пользователь пересортировал «Ролики», список подхватит новые номера при ближайшем
        /// обновлении. Ролик, которого нет в списке фирмы, остаётся с пустым номером.
        /// </summary>
        private void FillAddedIssuesRollerNumbers(System.Data.DataTable addedIssues)
        {
            Dictionary<int, int> rollerNumbers = BuildRollerNumbersMap();
            foreach (System.Data.DataRow row in addedIssues.Rows)
            {
                object number = DBNull.Value;
                if (int.TryParse(row[Roller.ParamNames.RollerId] as string, out int rollerId) &&
                    rollerNumbers.TryGetValue(rollerId, out int rollerNumber))
                    number = rollerNumber;

                if (!Equals(row[ActionOnMassmedia.RollerNumberColumn], number))
                    row[ActionOnMassmedia.RollerNumberColumn] = number;
            }
        }

	    private void TariffGridRefreshed()
        {
            ShowCurrentIssues(((TariffWithRangeGrid)_tariffGrid));
            _action.DisplayData(lstStat);
        }

        protected override void ProcessCurrentCampaignIssuesDelete(IList<PresentationObject> presentationObjects)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                foreach (PresentationObject presentationObject in presentationObjects)
                    RemoveIssuesFromAdded(presentationObject);

                TariffWithRangeGrid rangeGrid = (TariffWithRangeGrid)_tariffGrid;
                _action.Recalculate();
                rangeGrid.RefreshGrid();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Веер держит отдельную in-memory таблицу AddedIssues — чистим её и обновляем через
        /// уже существующий путь ObjectsDeleted -> ProcessCurrentCampaignIssuesDelete
        /// (RemoveIssuesFromAdded + Recalculate + RefreshGrid), а не напрямую.
        /// </summary>
        protected override void OnTemplateUndoCompleted(List<PresentationObject> deletedObjects)
        {
            grdCurrentCampaignIssues.RaiseObjectsDeleted(deletedObjects);
        }

        private void RemoveIssuesFromAdded(PresentationObject presentationObject)
        {
            MasterIssue issue = (MasterIssue)presentationObject;
            TariffWithRangeGrid rangeGrid = (TariffWithRangeGrid)_tariffGrid;
            foreach (System.Data.DataRow row in rangeGrid.AddedIssues.Select(
                string.Format("RowNum = '{0}'", issue["RowNum"])))
                rangeGrid.AddedIssues.Rows.Remove(row);
        }

        /// <summary>
        /// Массовое добавление выбранного ролика в выбранные окна веерного размещения (Insert).
        /// Каждое окно — существующий путь AddIssuesRange (AddRangeIssues пишет ролик на все
        /// радиостанции акции сразу), пересчёт акции — один после всего пакета. Ошибки
        /// (переполнение окна и т. п.) собираем и показываем, как при массовом удалении.
        /// </summary>
        protected override void AddIssuesInSelectedWindows()
        {
            TariffWithRangeGrid rangeGrid = _tariffGrid as TariffWithRangeGrid;
            if (rangeGrid == null)
                return;

            if (rangeGrid.Roller == null)
            {
                UserMessage.ShowExclamation(MessageAccessor.GetMessage("RollerNotSelected"));
                return;
            }

            IList<ITariffWindow> windows = _tariffGrid.GetSelectedTariffWindows();
            if (windows.Count == 0)
                return;

            if (UserMessage.ShowQuestion(
                    string.Format("Разместить ролик в выбранных окнах по выбранным кампаниям? ({0} шт.)", windows.Count)) != DialogResult.Yes)
                return;

            int addedCount = 0;
            System.Data.DataTable addErrors = FogSoft.WinForm.Controls.SmartGrid.CreateDeleteErrorsTable();
            int errorRowNumber = 1;
            try
            {
                Cursor = Cursors.WaitCursor;
                foreach (ITariffWindow window in windows)
                {
                    try
                    {
                        rangeGrid.AddIssuesRange(window.WindowDate, false, recalculate: false);
                        addedCount++;
                    }
                    catch (Exception ex)
                    {
                        FogSoft.WinForm.Controls.SmartGrid.AddDeleteError(addErrors, errorRowNumber++,
                            window.WindowDate.ToString("dd.MM.yyyy HH:mm"), ErrorManager.GetErrorMessage(ex));
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            if (addedCount > 0)
            {
                _action.Recalculate();
                RefreshGrid();
            }

            if (addErrors.Rows.Count > 0)
                FogSoft.WinForm.Controls.SmartGrid.ShowDeleteErrors(addErrors, "Ошибки массового добавления");
            else
                UserMessage.ShowInformation(string.Format("Добавлено выпусков: {0}.", addedCount));
        }

        /// <summary>
        /// Массовое удаление выпусков в выбранных окнах веерного размещения (Del).
        /// Синие выпуски (master issues, полностью по выбранным кампаниям) берём из in-memory
        /// AddedIssues по дате окна и удаляем через MasterIssue.Delete -> MasterIssueDelete
        /// (выпуск удаляется на всех радиостанциях акции). Красные (частичные) группы и всё, что
        /// осталось сверх синих копий, — отдельно через DeleteSlotIssueGroup (несколько проходов,
        /// см. SplitIntoDeletePasses). Одно нажатие Delete очищает выбранные окна полностью;
        /// если в них больше одного ролика — только от роликов, отмеченных в чек-листе (SelectRollers).
        /// Часть может не удалиться (прошлое/дедлайн у подтверждённых) — ошибки собираем и
        /// показываем (паттерн SmartGrid.DeleteSelectedObjects). Очистка AddedIssues + Recalculate
        /// + RefreshGrid выполняются в ProcessCurrentCampaignIssuesDelete через ObjectsDeleted.
        /// </summary>
        protected override void DeleteIssuesInSelectedWindows()
        {
            TariffWithRangeGrid rangeGrid = _tariffGrid as TariffWithRangeGrid;
            if (rangeGrid == null)
                return;

            IList<ITariffWindow> windows = _tariffGrid.GetSelectedTariffWindows();
            if (windows.Count == 0)
                return;

            Entity masterEntity = EntityManager.GetEntity((int)Entities.MasterIssues);
            // Синие строки AddedIssues; объекты MasterIssue создаются из них после выбора роликов.
            List<System.Data.DataRow> blueIssueRows = new List<System.Data.DataRow>();
            // Частичные («красные») слоты: выпуск есть не во всех выбранных кампаниях, в
            // AddedIssues его нет — содержимое читаем из базы и удаляем по тем кампаниям,
            // где оно реально стоит.
            List<KeyValuePair<DateTime, TariffWithRangeGrid.SlotIssueGroup>> partialGroups =
                new List<KeyValuePair<DateTime, TariffWithRangeGrid.SlotIssueGroup>>();

            // Сколько синих строк (AddedIssues) приходится на пару «ролик/позиция» в каждом окне:
            // столько копий на каждую кампанию снимет синее удаление, остальное — красным ниже.
            Dictionary<DateTime, Dictionary<string, int>> blueRowsByDate = new Dictionary<DateTime, Dictionary<string, int>>();
            List<DateTime> windowDates = new List<DateTime>();
            foreach (ITariffWindow window in windows)
            {
                windowDates.Add(window.WindowDate);

                System.Data.DataRow[] rows = rangeGrid.AddedIssues.Select(
                    string.Format("[issueDate] = '{0}'", window.WindowDate));
                if (rows.Length == 0)
                    continue;

                Dictionary<string, int> blueRowsByKey = new Dictionary<string, int>();
                foreach (System.Data.DataRow row in rows)
                {
                    blueIssueRows.Add(row);
                    int rollerId = ParseHelper.GetInt32FromObject(row[Roller.ParamNames.RollerId], 0);
                    int positionId = ParseHelper.GetInt32FromObject(row[Issue.ParamNames.PositionId], 0);
                    string key = rollerId + "/" + positionId;
                    blueRowsByKey.TryGetValue(key, out int blueRows);
                    blueRowsByKey[key] = blueRows + 1;
                }
                blueRowsByDate[window.WindowDate] = blueRowsByKey;
            }

            // Один и тот же получас может одновременно содержать и полностью пересекающийся
            // (синий) выпуск, и частичный (красный) — разными парами «ролик/позиция», а у одной
            // пары — ещё и лишние копии у части кампаний сверх синих. Поэтому группы читаем по
            // ВСЕМ выделенным окнам одним батч-запросом и из каждой берём то, что не снимут синие.
            foreach (KeyValuePair<DateTime, IList<TariffWithRangeGrid.SlotIssueGroup>> byWindow
                     in rangeGrid.GetSlotIssueGroups(windowDates))
            {
                blueRowsByDate.TryGetValue(byWindow.Key, out Dictionary<string, int> blueRowsByKey);

                foreach (TariffWithRangeGrid.SlotIssueGroup group in byWindow.Value)
                {
                    int blueRows = 0;
                    if (blueRowsByKey != null)
                        blueRowsByKey.TryGetValue(group.RollerId + "/" + (int)group.Position, out blueRows);

                    foreach (TariffWithRangeGrid.SlotIssueGroup pass in SplitIntoDeletePasses(group, blueRows))
                        partialGroups.Add(
                            new KeyValuePair<DateTime, TariffWithRangeGrid.SlotIssueGroup>(byWindow.Key, pass));
                }
            }

            if (blueIssueRows.Count + partialGroups.Count == 0)
            {
                UserMessage.ShowInformation("В выбранных окнах нет выпусков этой акции.");
                return;
            }

            // Несколько разных роликов в выбранных окнах — пусть пользователь отметит, какие
            // удалять (как при массовой замене). Выбор диалога и есть подтверждение.
            // Фильтр по ролику согласован с blueRowsByDate: ключ «ролик/позиция» включает ролик,
            // так что синие и красные части одного ролика остаются или уходят вместе.
            bool askConfirmation = true;
            List<int> rollerIds = blueIssueRows
                .Select(row => ParseHelper.GetInt32FromObject(row[Roller.ParamNames.RollerId], 0))
                .Concat(partialGroups.Select(partial => partial.Value.RollerId))
                .Distinct()
                .ToList();
            if (rollerIds.Count > 1)
            {
                List<int> chosenIds = SelectRollers(rollerIds, "Какие ролики удалить в выбранных окнах?");
                if (chosenIds == null)
                    return;
                blueIssueRows.RemoveAll(row =>
                    !chosenIds.Contains(ParseHelper.GetInt32FromObject(row[Roller.ParamNames.RollerId], 0)));
                partialGroups.RemoveAll(partial => !chosenIds.Contains(partial.Value.RollerId));
                askConfirmation = false;
            }

            List<PresentationObject> issues = blueIssueRows.Select(row => masterEntity.CreateObject(row)).ToList();

            // "Штук" — не групп (issues.Count + partialGroups.Count), а реальных записей Issue
            // в базе: MasterIssueDelete удаляет по одной записи на каждую станцию из
            // @campaignIDs (см. ArtvisDB/.../MasterIssueDelete.sql). Синий выпуск бьёт по всем
            // выбранным кампаниям сразу, частичный — только по кампаниям своей группы.
            int selectedCampaignsCount = rangeGrid.SelectedCampaignIds != null ? rangeGrid.SelectedCampaignIds.Count : 0;
            int realTotalCount = issues.Count * selectedCampaignsCount;
            foreach (KeyValuePair<DateTime, TariffWithRangeGrid.SlotIssueGroup> partial in partialGroups)
                realTotalCount += partial.Value.CampaignIds.Count;

            if (askConfirmation && UserMessage.ShowQuestion(
                    string.Format("Удалить выпуски в выбранных окнах по выбранным кампаниям? ({0} шт.)", realTotalCount)) != DialogResult.Yes)
                return;

            List<PresentationObject> deletedObjects = new List<PresentationObject>();
            int partialDeletedCount = 0;
            int realDeletedCount = 0;
            System.Data.DataTable deleteErrors = FogSoft.WinForm.Controls.SmartGrid.CreateDeleteErrorsTable();
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
                        {
                            deletedObjects.Add(issue);
                            realDeletedCount += selectedCampaignsCount;
                        }
                        else
                            FogSoft.WinForm.Controls.SmartGrid.AddDeleteError(deleteErrors, errorRowNumber++, objectName,
                                string.Format("Не удалось удалить выпуск '{0}'.", objectName));
                    }
                    catch (Exception ex)
                    {
                        FogSoft.WinForm.Controls.SmartGrid.AddDeleteError(deleteErrors, errorRowNumber++, objectName, ErrorManager.GetErrorMessage(ex));
                    }
                }

                foreach (KeyValuePair<DateTime, TariffWithRangeGrid.SlotIssueGroup> partial in partialGroups)
                {
                    string objectName = string.Format("{0} — {1}",
                        partial.Key.ToString("dd.MM.yyyy HH:mm"), partial.Value.RollerName);
                    try
                    {
                        rangeGrid.DeleteSlotIssueGroup(partial.Value, partial.Key);
                        partialDeletedCount++;
                        realDeletedCount += partial.Value.CampaignIds.Count;
                    }
                    catch (Exception ex)
                    {
                        FogSoft.WinForm.Controls.SmartGrid.AddDeleteError(deleteErrors, errorRowNumber++, objectName, ErrorManager.GetErrorMessage(ex));
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            // Если удалён хотя бы один — событие чистит AddedIssues, пересчитывает акцию и обновляет сетку.
            if (deletedObjects.Count > 0)
                grdCurrentCampaignIssues.RaiseObjectsDeleted(deletedObjects);
            else if (partialDeletedCount > 0)
            {
                // Частичные слоты в AddedIssues не лежат — чистить нечего, но пересчитать
                // акцию и перерисовать сетку всё равно нужно.
                _action.Recalculate();
                rangeGrid.RefreshGrid();
            }

            if (deleteErrors.Rows.Count > 0)
                FogSoft.WinForm.Controls.SmartGrid.ShowDeleteErrors(deleteErrors);
            else
                UserMessage.ShowInformation(string.Format("Удалено выпусков: {0}.", realDeletedCount));
        }

        // MasterIssueDelete снимает по одному выпуску на кампанию за вызов, поэтому если у
        // кампании в группе несколько копий, за один вызов группа не очистится. В проход k
        // входят кампании, у которых после blueRows синих копий остаётся не меньше k штук —
        // так каждый проход удаляет ровно len(CampaignIds) реальных выпусков (диалог считает по ним).
        private static IEnumerable<TariffWithRangeGrid.SlotIssueGroup> SplitIntoDeletePasses(
            TariffWithRangeGrid.SlotIssueGroup group, int blueRows)
        {
            Dictionary<int, int> copiesByCampaign = new Dictionary<int, int>();
            foreach (int campaignId in group.CampaignIds)
            {
                copiesByCampaign.TryGetValue(campaignId, out int copies);
                copiesByCampaign[campaignId] = copies + 1;
            }

            for (int pass = 1; ; pass++)
            {
                TariffWithRangeGrid.SlotIssueGroup part = new TariffWithRangeGrid.SlotIssueGroup
                {
                    RollerId = group.RollerId,
                    RollerName = group.RollerName,
                    Duration = group.Duration,
                    DurationString = group.DurationString,
                    Position = group.Position
                };
                part.CampaignIds.AddRange(
                    copiesByCampaign.Where(pair => pair.Value - blueRows >= pass).Select(pair => pair.Key));
                if (part.CampaignIds.Count == 0)
                    yield break;

                yield return part;
            }
        }

        /// <summary>
        /// Одна пара (кампания, старый ролик) и набор дней/окон, где его нужно заменить —
        /// единица вызова RollerSubstitute (она принимает ровно одну кампанию и один старый
        /// ролик за раз).
        /// </summary>
        private class ReplaceGroup
        {
            public int CampaignId;
            public int RollerId;
            public readonly System.Data.DataTable Days = CreateDaysTable();

            private static System.Data.DataTable CreateDaysTable()
            {
                // Имя таблицы и имена/типы колонок — контракт с RollerSubstitute.sql
                // (создаёт #days такой же формы через SqlBulkCopyHelper.CopyToSqlTempTable).
                System.Data.DataTable table = new System.Data.DataTable("days");
                table.Columns.Add("windowID", typeof(int));
                table.Columns.Add("issueDate", typeof(DateTime));
                return table;
            }
        }

        /// <summary>
        /// Массовая замена ролика в выделенных окнах (Ctrl+R / кнопка "Заменить ролики") на
        /// ролик, выбранный в списке "Ролики" — во всех кампаниях, отмеченных чек-листом, где
        /// в этих окнах реально стоят выпуски (включая частичные "красные" слоты). Переиспользует
        /// готовую RollerSubstitute (проверка дедлайна/прошлого/агитации, пересчёт цены,
        /// корректировка TariffWindow.timeInUse) через CampaignRoller.ApplyRollerSubstitutionForDays
        /// — ту же обвязку, что и диалог замены ролика по кампании. Один вызов SP — одна пара
        /// (кампания, старый ролик); группируем по ней, а не зовём по одному выпуску.
        /// </summary>
        protected override void ReplaceRollerInSelectedWindows()
        {
            TariffWithRangeGrid rangeGrid = _tariffGrid as TariffWithRangeGrid;
            if (rangeGrid == null)
                return;

            // Кнопка тоже гасится по этому условию (см. OnLoad) — здесь то же самое для Ctrl+R,
            // который её Enabled не учитывает.
            if (!btnShowRollerNumbers.Checked)
            {
                UserMessage.ShowExclamation(
                    "Замена роликов доступна только при включённом показе номеров роликов " +
                    "(кнопка \"Номера роликов\") — так видно, что вы меняете.");
                return;
            }

            if (rangeGrid.Roller == null)
            {
                UserMessage.ShowExclamation(MessageAccessor.GetMessage("RollerNotSelected"));
                return;
            }

            Roller newRoller = rangeGrid.Roller;

            IList<ITariffWindow> windows = _tariffGrid.GetSelectedTariffWindows();
            if (windows.Count == 0)
                return;

            Dictionary<string, ReplaceGroup> groups = new Dictionary<string, ReplaceGroup>();
            int totalCount = 0;

            // Один батч-запрос на все выделенные окна сразу (не по одному на окно) — иначе
            // при выделении в десятки окон это была заметная пауза перед диалогом
            // подтверждения (см. RangeSlotIssues.sql).
            List<DateTime> windowDates = new List<DateTime>();
            foreach (ITariffWindow window in windows)
                windowDates.Add(window.WindowDate);

            List<TariffWithRangeGrid.SlotIssueRow> slotRows = new List<TariffWithRangeGrid.SlotIssueRow>();
            List<int> oldRollerIds = new List<int>();
            foreach (TariffWithRangeGrid.SlotIssueRow row in rangeGrid.GetSlotIssueRows(windowDates))
            {
                // Тот же самый ролик уже стоит — заменять нечего.
                if (row.RollerId == newRoller.RollerId)
                    continue;

                slotRows.Add(row);
                if (!oldRollerIds.Contains(row.RollerId))
                    oldRollerIds.Add(row.RollerId);
            }

            if (slotRows.Count == 0)
            {
                UserMessage.ShowInformation("В выделенных окнах нечего заменять.");
                return;
            }

            // Несколько разных роликов на замену — пусть пользователь отметит, какие менять.
            // Выбор диалога и есть подтверждение, отдельный вопрос тогда не задаём.
            bool askConfirmation = true;
            if (oldRollerIds.Count > 1)
            {
                List<int> chosenIds = SelectRollers(oldRollerIds,
                    string.Format("Какие ролики заменить на «{0}» ({1})?", newRoller.Name, newRoller.DurationString));
                if (chosenIds == null)
                    return;
                slotRows.RemoveAll(r => !chosenIds.Contains(r.RollerId));
                askConfirmation = false;
            }

            foreach (TariffWithRangeGrid.SlotIssueRow row in slotRows)
            {
                string key = row.CampaignId + "/" + row.RollerId;
                if (!groups.TryGetValue(key, out ReplaceGroup group))
                {
                    group = new ReplaceGroup { CampaignId = row.CampaignId, RollerId = row.RollerId };
                    groups.Add(key, group);
                }

                if (group.Days.Select(string.Format("windowID = {0}", row.OriginalWindowId)).Length == 0)
                    group.Days.Rows.Add(row.OriginalWindowId, row.WindowDayOriginal);

                totalCount++;
            }

            if (askConfirmation && UserMessage.ShowQuestion(string.Format(
                    "Заменить ролики на «{0}» ({1}) в выделенных окнах? ({2} шт.)",
                    newRoller.Name, newRoller.DurationString, totalCount)) != DialogResult.Yes)
                return;

            System.Data.DataTable unsubstituted = null;
            System.Data.DataTable groupErrors = FogSoft.WinForm.Controls.SmartGrid.CreateDeleteErrorsTable();
            int errorRowNumber = 1;

            try
            {
                Cursor = Cursors.WaitCursor;
                foreach (ReplaceGroup group in groups.Values)
                {
                    Campaign campaign = Campaign.GetCampaignById(group.CampaignId);
                    Roller oldRoller = new Roller(group.RollerId);
                    try
                    {
                        System.Data.DataTable unsub = CampaignRoller.ApplyRollerSubstitutionForDays(
                            campaign, oldRoller, newRoller, group.Days, null, null);
                        if (unsub != null && unsub.Rows.Count > 0)
                        {
                            if (unsubstituted == null)
                                unsubstituted = unsub.Clone();
                            foreach (System.Data.DataRow row in unsub.Rows)
                                unsubstituted.ImportRow(row);
                        }
                    }
                    catch (Exception ex)
                    {
                        FogSoft.WinForm.Controls.SmartGrid.AddDeleteError(groupErrors, errorRowNumber++,
                            string.Format("{0} — {1}", campaign[Campaign.ParamNames.MassmediaName], oldRoller.Name),
                            ErrorManager.GetErrorMessage(ex));
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            // Roller ID у уже отрисованных ячеек (AddedIssues) могли поменяться — пересобираем
            // из базы, как после отката переноса (см. RebuildAddedIssues).
            rangeGrid.RebuildAddedIssues();
            _action.Recalculate();
            rangeGrid.RefreshGrid();

            // Незаменённые по бизнес-правилам (дедлайн/прошлое/...) — тот же журнал, что и у
            // одиночной замены ролика по кампании (CampaignRoller.Substitute).
            if (unsubstituted != null && unsubstituted.Rows.Count > 0)
                CampaignRoller.ShowUnsubstitutedRollers(unsubstituted);

            // Группы, упавшие целиком (агитация/нулевая длительность/...) — отдельный журнал.
            if (groupErrors.Rows.Count > 0)
                FogSoft.WinForm.Controls.SmartGrid.ShowDeleteErrors(groupErrors, "Ошибки массовой замены роликов");
            else if (unsubstituted == null || unsubstituted.Rows.Count == 0)
                UserMessage.ShowInformation(string.Format("Заменено роликов: {0}.", totalCount));
        }

        /// <summary>
        /// «Добавить до полного пересечения» и «Удалить дубли» создаются здесь, а не в дизайнере
        /// базовой формы, — они есть только в веере; тулбар приватный у CampaignForm — берём его
        /// через соседнюю кнопку. Заодно раскладывает тулбар на два кластера, разделённых одинарной
        /// чертой: три шаблона и четыре действия над выделенными окнами
        /// («Заменить ролики», «Добавить до полного пересечения», «Удалить дубли», «Отменить»).
        /// «Отменить» в базовой форме стоит сразу за шаблонами (там она относится к ним же) —
        /// здесь переносится в кластер действий, обычная кампания остаётся как была.
        /// </summary>
        private void AddRollerMismatchButtons()
        {
            _tbbFillToIntersection = new ToolStripButton("Добавить до полного пересечения")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = Globals.GetIcon("build.png"),
                ToolTipText = "Добавить ролики до полного пересечения"
            };
            _tbbFillToIntersection.Click += (s, e) => RunToolbarAction(FillRollersToIntersectionInSelectedWindows);

            _tbbDeleteDuplicates = new ToolStripButton("Удалить дубли")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = Globals.GetIcon("delete2.png"),
                ToolTipText = "Удалить дубли роликов из выделенных окон"
            };
            _tbbDeleteDuplicates.Click += (s, e) => RunToolbarAction(DeleteDuplicatesInSelectedWindows);

            ToolStrip toolbar = tbbReplaceRoller.Owner;

            // Слева от шаблонов черта уже есть (toolStripSeparator3), справа от кластера —
            // тоже (toolStripSeparator2); не хватает только черты между шаблонами и кластером.
            toolbar.Items.Insert(toolbar.Items.IndexOf(tbbReplaceRoller), new ToolStripSeparator());

            tbbTemplateUndo.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tbbTemplateUndo.Image = Globals.GetIcon("backward.png");
            toolbar.Items.Remove(tbbTemplateUndo);
            int index = toolbar.Items.IndexOf(tbbReplaceRoller);
            toolbar.Items.Insert(index + 1, _tbbFillToIntersection);
            toolbar.Items.Insert(index + 2, _tbbDeleteDuplicates);
            toolbar.Items.Insert(index + 3, tbbTemplateUndo);

            HideRedundantToolbarSeparators();
        }

        private void SetRollerNumberActionsEnabled()
        {
            tbbReplaceRoller.Enabled = _tbbDeleteDuplicates.Enabled = _tbbFillToIntersection.Enabled =
                btnShowRollerNumbers.Checked;
        }

        private static void RunToolbarAction(System.Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        /// <summary>Сколько лишних выпусков одного ролика снять у одной кампании в одном окне.</summary>
        private class DuplicateRemoval
        {
            public DateTime WindowDate;
            public int CampaignId;
            public int RollerId;
            public string RollerName;
            public int PositionId;
            public int ExtraCount;
        }

        /// <summary>
        /// Окно, пропущенное удалением дублей: у дублей ролика в какой-то кампании разное
        /// позиционирование — неизвестно, какой оставить. Conflicts — по строке на пару
        /// (кампания, ролик) с таким конфликтом.
        /// </summary>
        private class SkippedWindow
        {
            public DateTime WindowDate;
            public readonly List<TariffWithRangeGrid.SlotIssueRow> Conflicts = new List<TariffWithRangeGrid.SlotIssueRow>();
        }

        /// <summary>Один недостающий выпуск: куда, какой ролик и с какой позицией ставить.</summary>
        private class RollerAddition
        {
            public DateTime WindowDate;
            public int CampaignId;
            public int RollerId;
            public string RollerName;
            public int Duration;
            public int PositionId;
        }

        /// <summary>
        /// План удаления дублей: в каждом окне у каждой кампании каждый ролик — не больше одного
        /// выпуска. Окно, где у дублей одного ролика одной кампании разные позиции, не трогается
        /// целиком и попадает в skipped. Позиции роликов без дублей на решение не влияют.
        /// </summary>
        private static List<DuplicateRemoval> PlanDuplicateRemovals(IEnumerable<TariffWithRangeGrid.SlotIssueRow> rows,
            IList<int> campaignOrder, List<SkippedWindow> skipped)
        {
            List<DuplicateRemoval> result = new List<DuplicateRemoval>();
            foreach (IGrouping<DateTime, TariffWithRangeGrid.SlotIssueRow> window in
                     rows.GroupBy(r => r.WindowDate).OrderBy(g => g.Key))
            {
                List<DuplicateRemoval> windowRemovals = new List<DuplicateRemoval>();
                SkippedWindow skip = null;
                foreach (var issues in window
                             .GroupBy(r => new { r.CampaignId, r.RollerId })
                             .OrderBy(g => GetCampaignOrderIndex(campaignOrder, g.Key.CampaignId))
                             .ThenBy(g => g.Key.RollerId))
                {
                    List<TariffWithRangeGrid.SlotIssueRow> list = issues.ToList();
                    if (list.Count < 2)
                        continue;

                    if (list.Select(r => r.PositionId).Distinct().Count() > 1)
                    {
                        if (skip == null)
                            skip = new SkippedWindow { WindowDate = window.Key };
                        skip.Conflicts.Add(list[0]);
                        continue;
                    }

                    windowRemovals.Add(new DuplicateRemoval
                    {
                        WindowDate = window.Key,
                        CampaignId = list[0].CampaignId,
                        RollerId = list[0].RollerId,
                        RollerName = list[0].RollerName,
                        PositionId = list[0].PositionId,
                        ExtraCount = list.Count - 1
                    });
                }

                if (skip != null)
                    skipped.Add(skip);
                else
                    result.AddRange(windowRemovals);
            }
            return result;
        }

        /// <summary>
        /// План выравнивания: в каждом окне по каждому ролику цель — наибольшее количество среди
        /// выбранных кампаний (нуль считается). Лидер — первая по порядку веера кампания с
        /// наибольшим количеством; недостающим кампаниям достаются позиции лидера за вычетом тех,
        /// что у них уже есть. Если таких позиций больше дефицита — сначала особые (первая,
        /// вторая, последняя), потом без позиции (решение владельца).
        /// </summary>
        private static List<RollerAddition> PlanRollerAdditions(IEnumerable<TariffWithRangeGrid.SlotIssueRow> rows,
            IList<int> campaignOrder)
        {
            List<RollerAddition> result = new List<RollerAddition>();
            if (campaignOrder.Count == 0)
                return result;

            foreach (IGrouping<DateTime, TariffWithRangeGrid.SlotIssueRow> window in
                     rows.GroupBy(r => r.WindowDate).OrderBy(g => g.Key))
            {
                foreach (IGrouping<int, TariffWithRangeGrid.SlotIssueRow> roller in
                         window.GroupBy(r => r.RollerId).OrderBy(g => g.Key))
                {
                    Dictionary<int, List<int>> positionsByCampaign = campaignOrder.ToDictionary(id => id, id => new List<int>());
                    foreach (TariffWithRangeGrid.SlotIssueRow row in roller)
                        if (positionsByCampaign.TryGetValue(row.CampaignId, out List<int> positions))
                            positions.Add(row.PositionId);

                    int max = positionsByCampaign.Values.Max(p => p.Count);
                    List<int> leaderPositions = positionsByCampaign[campaignOrder.First(id => positionsByCampaign[id].Count == max)];
                    TariffWithRangeGrid.SlotIssueRow sample = roller.First();

                    foreach (int campaignId in campaignOrder)
                    {
                        List<int> own = positionsByCampaign[campaignId];
                        int deficit = max - own.Count;
                        if (deficit == 0)
                            continue;

                        List<int> missing = new List<int>(leaderPositions);
                        foreach (int position in own)
                            missing.Remove(position);

                        foreach (int position in missing.OrderBy(p => p == 0 ? 1 : 0).ThenBy(p => p).Take(deficit))
                            result.Add(new RollerAddition
                            {
                                WindowDate = window.Key,
                                CampaignId = campaignId,
                                RollerId = roller.Key,
                                RollerName = sample.RollerName,
                                Duration = sample.Duration,
                                PositionId = position
                            });
                    }
                }
            }
            return result;
        }

        // Отказы IssueIUD «позиция в окне занята»: подтверждённым выпуском (hlp_IssueVerify) или
        // выпуском этой же акции — в черновике занятость своей акцией приходит только вторым ключом.
        private static readonly HashSet<string> PositionBusyErrors =
            new HashSet<string> { "FirstLastIssueError", "PositionErrorForTheSameAction" };

        private static int GetCampaignOrderIndex(IList<int> campaignOrder, int campaignId)
        {
            int index = campaignOrder.IndexOf(campaignId);
            return index < 0 ? int.MaxValue : index;
        }

        /// <summary>
        /// Порядок кампаний «в веере» — порядок строк чек-листа (с учётом сортировки списка),
        /// только из применённого выбора, с которым работает сетка и RangeSlotIssues.
        /// </summary>
        private List<int> GetVeerCampaignOrder(TariffWithRangeGrid rangeGrid)
        {
            List<int> ordered = GetCheckedCampaignIds();
            if (rangeGrid.SelectedCampaignIds != null)
                ordered.RemoveAll(id => !rangeGrid.SelectedCampaignIds.Contains(id));
            return ordered;
        }

        /// <summary>
        /// Удаление дублей в выделенных окнах. Каждый лишний выпуск — отдельный вызов
        /// MasterIssueDelete по одной кампании (своя транзакция): отказ одного (прошлое/дедлайн)
        /// не откатывает остальные. Пересчёт акции — один в конце.
        /// </summary>
        private void DeleteDuplicatesInSelectedWindows()
        {
            TariffWithRangeGrid rangeGrid = _tariffGrid as TariffWithRangeGrid;
            if (rangeGrid == null)
                return;

            IList<ITariffWindow> windows = _tariffGrid.GetSelectedTariffWindows();
            if (windows.Count == 0)
            {
                UserMessage.ShowInformation("Выделите окна в сетке.");
                return;
            }

            List<int> campaignOrder = GetVeerCampaignOrder(rangeGrid);
            List<SkippedWindow> skipped = new List<SkippedWindow>();
            List<DuplicateRemoval> removals = PlanDuplicateRemovals(
                WithWaitCursor(() => rangeGrid.GetSlotIssueRows(windows.Select(w => w.WindowDate))), campaignOrder, skipped);
            int plannedCount = removals.Sum(r => r.ExtraCount);
            Dictionary<int, int> rollerNumbers = rangeGrid.RollerNumbers ?? BuildRollerNumbersMap();

            System.Data.DataTable problems = FogSoft.WinForm.Controls.SmartGrid.CreateDeleteErrorsTable();
            int problemNumber = 1;
            foreach (SkippedWindow skip in skipped)
                FogSoft.WinForm.Controls.SmartGrid.AddDeleteError(problems, problemNumber++,
                    skip.WindowDate.ToString("dd.MM.yyyy HH:mm"),
                    "Окно пропущено: у дублей разное позиционирование — " + string.Join("; ", skip.Conflicts.Select(
                        c => FormatCampaignTitle(c.CampaignId) + " – " + FormatRollerLabel(c.RollerId, c.RollerName, rollerNumbers))));

            if (plannedCount == 0)
            {
                ReportRollerMismatchFix(skipped.Count > 0
                        ? "Удалять нечего: все окна с дублями пропущены."
                        : "В выделенных окнах дублей нет.",
                    problems, "Удаление дублей: пропущенные окна");
                return;
            }

            string question = string.Format(
                "Удалить дубли в выделенных окнах, оставив у каждой кампании по одному выпуску каждого ролика? ({0} шт.)",
                plannedCount);
            if (skipped.Count > 0)
                question += Environment.NewLine + string.Format(
                    "Окон будет пропущено из-за разного позиционирования дублей: {0}.", skipped.Count);
            if (UserMessage.ShowQuestion(question) != DialogResult.Yes)
                return;

            int deletedCount = 0;
            try
            {
                Cursor = Cursors.WaitCursor;
                foreach (DuplicateRemoval removal in removals)
                {
                    TariffWithRangeGrid.SlotIssueGroup group = new TariffWithRangeGrid.SlotIssueGroup
                    {
                        RollerId = removal.RollerId,
                        Position = (RollerPositions)removal.PositionId
                    };
                    group.CampaignIds.Add(removal.CampaignId);

                    for (int i = 0; i < removal.ExtraCount; i++)
                    {
                        try
                        {
                            rangeGrid.DeleteSlotIssueGroup(group, removal.WindowDate);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            // MasterIssueDelete при повторе выберет тот же выпуск — ошибка будет той же.
                            FogSoft.WinForm.Controls.SmartGrid.AddDeleteError(problems, problemNumber++,
                                FormatIssueTarget(removal.WindowDate, removal.CampaignId, removal.RollerId, removal.RollerName, rollerNumbers),
                                string.Format("Не удалено {0} шт.: {1}", removal.ExtraCount - i, ErrorManager.GetErrorMessage(ex)));
                            break;
                        }
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            try
            {
                if (deletedCount > 0)
                    RefreshAfterRollerMismatchFix(rangeGrid);
            }
            finally
            {
                ReportRollerMismatchFix(string.Format("Удалено лишних выпусков: {0}.", deletedCount),
                    problems, "Удаление дублей: пропущено и не удалено");
            }
        }

        /// <summary>
        /// Добавление роликов до полного пересечения в выделенных окнах. Best-effort: каждый
        /// выпуск — отдельный вызов AddRangeIssues по одной кампании (своя транзакция); занятая
        /// позиция — повтор без позиции; любой другой отказ — выпуск пропускается и попадает в
        /// итог. Пересчёт акции — один в конце.
        /// </summary>
        private void FillRollersToIntersectionInSelectedWindows()
        {
            TariffWithRangeGrid rangeGrid = _tariffGrid as TariffWithRangeGrid;
            if (rangeGrid == null)
                return;

            IList<ITariffWindow> windows = _tariffGrid.GetSelectedTariffWindows();
            if (windows.Count == 0)
            {
                UserMessage.ShowInformation("Выделите окна в сетке.");
                return;
            }

            List<RollerAddition> additions = PlanRollerAdditions(
                WithWaitCursor(() => rangeGrid.GetSlotIssueRows(windows.Select(w => w.WindowDate))),
                GetVeerCampaignOrder(rangeGrid));
            if (additions.Count == 0)
            {
                UserMessage.ShowInformation("В выделенных окнах количество роликов уже совпадает у всех выбранных кампаний.");
                return;
            }

            if (UserMessage.ShowQuestion(string.Format(
                    "Добавить недостающие ролики в выделенных окнах до полного пересечения? ({0} шт.)",
                    additions.Count)) != DialogResult.Yes)
                return;

            Dictionary<int, int> rollerNumbers = rangeGrid.RollerNumbers ?? BuildRollerNumbersMap();
            System.Data.DataTable problems = FogSoft.WinForm.Controls.SmartGrid.CreateDeleteErrorsTable();
            int problemNumber = 1;
            int addedCount = 0;
            int withoutPositionCount = 0;
            try
            {
                Cursor = Cursors.WaitCursor;
                foreach (RollerAddition addition in additions)
                {
                    TariffWithRangeGrid.SlotIssueGroup group = new TariffWithRangeGrid.SlotIssueGroup
                    {
                        RollerId = addition.RollerId,
                        Duration = addition.Duration,
                        Position = (RollerPositions)addition.PositionId
                    };
                    group.CampaignIds.Add(addition.CampaignId);

                    try
                    {
                        try
                        {
                            rangeGrid.AddSlotIssueGroup(group, addition.WindowDate);
                        }
                        catch (Exception ex) when (group.Position != RollerPositions.Undefined
                                                   && PositionBusyErrors.Contains(ex.Message))
                        {
                            group.Position = RollerPositions.Undefined;
                            rangeGrid.AddSlotIssueGroup(group, addition.WindowDate);
                            withoutPositionCount++;
                        }
                        addedCount++;
                    }
                    catch (Exception ex)
                    {
                        FogSoft.WinForm.Controls.SmartGrid.AddDeleteError(problems, problemNumber++,
                            FormatIssueTarget(addition.WindowDate, addition.CampaignId, addition.RollerId, addition.RollerName, rollerNumbers),
                            ErrorManager.GetErrorMessage(ex));
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            string summary = string.Format("Добавлено выпусков: {0} из {1}.", addedCount, additions.Count);
            if (withoutPositionCount > 0)
                summary += Environment.NewLine + string.Format(
                    "Из них без позиционирования (позиция в окне занята): {0}.", withoutPositionCount);

            try
            {
                if (addedCount > 0)
                    RefreshAfterRollerMismatchFix(rangeGrid);
            }
            finally
            {
                ReportRollerMismatchFix(summary, problems, "Добавление до пересечения: не добавлено");
            }
        }

        // Счётчики роликов в слотах поменялись — AddedIssues (пересечение по кампаниям)
        // пересобираем из базы, как после массовой замены роликов.
        private void RefreshAfterRollerMismatchFix(TariffWithRangeGrid rangeGrid)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                rangeGrid.RebuildAddedIssues();
                _action.Recalculate();
                rangeGrid.RefreshGrid();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // Чтение слотов перед диалогом подтверждения и пересчёт с обновлением сетки после цикла
        // заметно долгие — без курсора ожидания окно выглядит зависшим.
        private T WithWaitCursor<T>(Func<T> read)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                return read();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private static void ReportRollerMismatchFix(string summary, System.Data.DataTable problems, string caption)
        {
            if (problems.Rows.Count == 0)
            {
                UserMessage.ShowInformation(summary);
                return;
            }

            UserMessage.ShowInformation(summary + Environment.NewLine + "Что не выполнено и почему — в следующем окне.");
            FogSoft.WinForm.Controls.SmartGrid.ShowDeleteErrors(problems, caption);
        }

        // «18.09.2026 10:00 — Станция Группа (тип оплаты) — ролик 5»
        private string FormatIssueTarget(DateTime windowDate, int campaignId, int rollerId, string rollerName,
            Dictionary<int, int> rollerNumbers)
        {
            return string.Format("{0} — {1} — {2}", windowDate.ToString("dd.MM.yyyy HH:mm"),
                FormatCampaignTitle(campaignId), FormatRollerLabel(rollerId, rollerName, rollerNumbers));
        }

        // Номер из списка «Ролики», как в ячейке; ролика в списке нет — по названию.
        private static string FormatRollerLabel(int rollerId, string rollerName, Dictionary<int, int> rollerNumbers)
        {
            return rollerNumbers != null && rollerNumbers.TryGetValue(rollerId, out int number)
                ? "ролик " + number
                : string.Format("ролик «{0}»", rollerName);
        }

        /// <summary>
        /// Груз drag-and-drop переноса для веера: дата слота-источника и строки AddedIssues
        /// (master-выпуски) этого слота. Свой тип, отличный от линейного IssueDragPayload:
        /// в веере единица переноса — слот на всех радиостанциях акции, а не Issue одной станции.
        /// </summary>
        private class RangeIssueDragPayload
        {
            public readonly DateTime SourceSlotDate;
            public readonly List<System.Data.DataRow> IssueRows;
            // Частичный («красный») слот: выпуск есть не во всех выбранных кампаниях, в
            // AddedIssues его нет. Тогда переезжают группы, прочитанные из базы, и ровно в
            // том составе кампаний, в котором стояли.
            public readonly IList<TariffWithRangeGrid.SlotIssueGroup> PartialGroups;

            public RangeIssueDragPayload(DateTime sourceSlotDate, List<System.Data.DataRow> issueRows)
            {
                SourceSlotDate = sourceSlotDate;
                IssueRows = issueRows;
            }

            public RangeIssueDragPayload(DateTime sourceSlotDate,
                IList<TariffWithRangeGrid.SlotIssueGroup> partialGroups)
            {
                SourceSlotDate = sourceSlotDate;
                IssueRows = new List<System.Data.DataRow>();
                PartialGroups = partialGroups;
            }

            public int Count
            {
                get { return PartialGroups != null ? PartialGroups.Count : IssueRows.Count; }
            }
        }

        /// <summary>
        /// Drag-and-drop перенос выпусков веерного размещения между окнами тарифной сетки.
        /// Два источника: «синяя» ячейка сетки (переезжают все выпуски слота) и строка списка
        /// «Добавленные выпуски» (переезжает один выпуск). Из ячейки — только в режиме
        /// просмотра: в режиме редактирования клик добавляет выпуски, а прямоугольное
        /// выделение (Del-удаление) на прочих ячейках не задевается — жест перехватывается
        /// только со стартом на синей ячейке.
        /// </summary>
        private void EnableRangeIssueDragDrop()
        {
            DataGridView listGrid = grdCurrentCampaignIssues.InternalGrid;
            listGrid.MouseDown += AddedIssuesGrid_MouseDown;
            listGrid.MouseMove += AddedIssuesGrid_MouseMove;

            DataGridView grid = _tariffGrid.InternalGrid;
            grid.AllowDrop = true;
            grid.MouseDown += RangeGrid_MouseDown;
            grid.MouseMove += RangeGrid_MouseMove;
            grid.DragEnter += RangeGrid_DragEnter;
            grid.DragOver += RangeGrid_DragOver;
            grid.DragDrop += RangeGrid_DragDrop;
        }

        /// <summary>
        /// Подсказки при наведении на цветные ячейки:
        /// синяя/красная при включённых номерах роликов — расшифровка значков Н/Д: каких
        /// роликов и сколько не хватает каждой кампании и где стоят дубли (BuildRollerMismatchTooltip);
        /// красная без номеров роликов — каких из отмеченных галочкой кампаний не хватает в этом
        /// слоте;
        /// бирюзовая/оранжевая — какие чужие акции той же фирмы стоят в этом слоте (номер и
        /// статус подтверждения), без похода в базу — данные уже загружены вместе с раскраской
        /// окон (см. TariffWithRangeGrid.GetOtherFirmActions). На синей молчит — там «везде»
        /// тривиально верно и без подсказки.
        /// </summary>
        private void EnableCellTooltips()
        {
            DataGridView grid = _tariffGrid.InternalGrid;
            grid.ShowCellToolTips = true;
            grid.CellToolTipTextNeeded += RangeGrid_CellToolTipTextNeeded;
        }

        private void RangeGrid_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            try
            {
                TariffWithRangeGrid rangeGrid = _tariffGrid as TariffWithRangeGrid;
                if (rangeGrid == null || !rangeGrid.HasSlots)
                    return;

                ITariffWindow window = _tariffGrid.GetTariffWindowAt(e.RowIndex, e.ColumnIndex);
                if (window == null)
                    return;

                string rollersTooltip = BuildRollerMismatchTooltip(rangeGrid, window.WindowDate);
                if (rollersTooltip != null)
                    e.ToolTipText = rollersTooltip;
                else if (_tariffGrid.CellHasCurrentActionIssues(e.RowIndex, e.ColumnIndex))
                    e.ToolTipText = BuildMissingCampaignsTooltip(rangeGrid, window.WindowDate);
                else if (_tariffGrid.CellHasOtherFirmIssues(e.RowIndex, e.ColumnIndex))
                    e.ToolTipText = BuildOtherFirmActionsTooltip(rangeGrid, window.WindowDate);
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        /// <summary>
        /// «В этом окне:», дальше каждая чужая акция той же фирмы с новой строки — номер,
        /// статус подтверждения и владелец (по аналогии с текстом диалога подтверждения
        /// переноса).
        /// </summary>
        private string BuildOtherFirmActionsTooltip(TariffWithRangeGrid rangeGrid, DateTime windowDate)
        {
            IList<TariffWithRangeGrid.OtherFirmAction> actions = rangeGrid.GetOtherFirmActions(windowDate);
            if (actions.Count == 0)
                return null;

            List<string> lines = new List<string>();
            foreach (TariffWithRangeGrid.OtherFirmAction action in actions)
            {
                string status = action.HasConfirmed ? "подтверждена" : "не подтверждена";
                string details = string.IsNullOrEmpty(action.OwnerName) ? status : status + ", " + action.OwnerName;
                lines.Add(string.Format("Акция №{0} ({1})", action.ActionId, details));
            }

            return "В этом окне:" + Environment.NewLine + string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// «Отсутствуют:», дальше каждая кампания с новой строки — отмеченные галочкой
        /// кампании минус те, что реально нашлись в слоте (GetSlotIssueGroups — тот же
        /// запрос, что и для удаления/переноса/замены ролика в частичных слотах).
        /// </summary>
        private string BuildMissingCampaignsTooltip(TariffWithRangeGrid rangeGrid, DateTime windowDate)
        {
            IList<int> selected = rangeGrid.SelectedCampaignIds;
            if (selected == null || selected.Count == 0)
                return null;

            HashSet<int> present = new HashSet<int>();
            foreach (TariffWithRangeGrid.SlotIssueGroup group in rangeGrid.GetSlotIssueGroups(windowDate))
                foreach (int campaignId in group.CampaignIds)
                    present.Add(campaignId);

            List<string> missing = new List<string>();
            foreach (int campaignId in selected)
                if (!present.Contains(campaignId))
                    missing.Add(FormatCampaignTitle(campaignId));

            return missing.Count == 0 ? null : "Отсутствуют:" + Environment.NewLine + string.Join(Environment.NewLine, missing);
        }

        // «Станция Группа (тип оплаты, агентство)» — группа (город) отличает одноимённые станции
        // разных городов, тип оплаты и агентство — несколько кампаний одной станции в акции
        // (см. UIX_Campaign: различаются paymentTypeID и/или agencyID) — без них несколько строк
        // могли выглядеть одинаково.
        // Агентство у кампании может быть не заполнено (LEFT JOIN в Campaigns.sql) —
        // тогда в скобках только тип оплаты, без лишней запятой.
        private string FormatCampaignTitle(int campaignId)
        {
            foreach (System.Data.DataRowView row in _campaignsView)
            {
                if (ParseHelper.GetInt32FromObject(row[Campaign.ParamNames.CampaignId], 0) != campaignId)
                    continue;

                // В базе у названий станций бывает пробел на конце («Русское радио »).
                string massmedia = StringUtil.GetStringOrEmpty(row[Campaign.ParamNames.MassmediaName]).Trim();
                string group = StringUtil.GetStringOrEmpty(row["groupName"]).Trim();
                string paymentType = StringUtil.GetStringOrEmpty(row["paymentTypeName"]);
                string agency = StringUtil.GetStringOrEmpty(row["agencyName"]);
                string details = string.IsNullOrEmpty(agency) ? paymentType : paymentType + ", " + agency;
                string station = string.IsNullOrEmpty(group) ? massmedia : massmedia + " " + group;

                return string.Format("{0} ({1})", station, details);
            }
            return "#" + campaignId;
        }

        /// <summary>
        /// Расшифровка значков Н/Д в ячейке. Блок «Отсутствуют:» — по каждой кампании, каких
        /// роликов и сколько штук не хватает до кампании с наибольшим количеством этого ролика;
        /// блок «Дубли:» — какие ролики у кампании стоят больше одного раза и сколько. Номера
        /// роликов по возрастанию; пустые блоки не выводятся, null — показывать нечего
        /// (номера роликов выключены, слот без выпусков своей акции или всё совпадает и без дублей).
        /// </summary>
        private string BuildRollerMismatchTooltip(TariffWithRangeGrid rangeGrid, DateTime windowDate)
        {
            SortedDictionary<int, Dictionary<int, int>> countsByNumber = rangeGrid.GetRollerCountsByNumber(windowDate);
            IList<int> campaignIds = rangeGrid.SelectedCampaignIds;
            if (countsByNumber == null || campaignIds == null || campaignIds.Count == 0)
                return null;

            List<string> missingLines = new List<string>();
            List<string> duplicateLines = new List<string>();
            foreach (int campaignId in campaignIds)
            {
                List<string> missing = new List<string>();
                List<string> duplicates = new List<string>();
                foreach (KeyValuePair<int, Dictionary<int, int>> roller in countsByNumber)
                {
                    int own = GetRollerCount(roller.Value, campaignId);
                    int max = campaignIds.Max(id => GetRollerCount(roller.Value, id));
                    if (own < max)
                        missing.Add(FormatRollerCount(roller.Key, max - own, missing.Count == 0));
                    if (own > 1)
                        duplicates.Add(FormatRollerCount(roller.Key, own, duplicates.Count == 0));
                }

                if (missing.Count > 0)
                    missingLines.Add(FormatCampaignTitle(campaignId) + " – " + string.Join(", ", missing));
                if (duplicates.Count > 0)
                    duplicateLines.Add(FormatCampaignTitle(campaignId) + " – " + string.Join(", ", duplicates));
            }

            List<string> sections = new List<string>();
            if (missingLines.Count > 0)
                sections.Add("Отсутствуют:" + Environment.NewLine + string.Join(Environment.NewLine, missingLines));
            if (duplicateLines.Count > 0)
                sections.Add("Дубли:" + Environment.NewLine + string.Join(Environment.NewLine, duplicateLines));

            return sections.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, sections);
        }

        private static int GetRollerCount(Dictionary<int, int> countsByCampaign, int campaignId)
        {
            return countsByCampaign.TryGetValue(campaignId, out int count) ? count : 0;
        }

        // «ролик 5 (3 шт.)» у первого ролика строки, дальше без слова «ролик» — как в ТЗ.
        private static string FormatRollerCount(int rollerNumber, int count, bool isFirst)
        {
            return string.Format("{0}{1} ({2} шт.)", isFirst ? "ролик " : string.Empty, rollerNumber, count);
        }

        private void AddedIssuesGrid_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _draggingAddedIssueRow = null;
            DataGridView grid = (DataGridView)sender;
            DataGridView.HitTestInfo hit = grid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0) return;
            System.Data.DataRowView drv = grid.Rows[hit.RowIndex].DataBoundItem as System.Data.DataRowView;
            if (drv == null) return;
            _draggingAddedIssueRow = drv.Row;
            _dragStartPoint = e.Location;
        }

        private void AddedIssuesGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _draggingAddedIssueRow == null || !DragThresholdExceeded(e)) return;

            System.Data.DataRow row = _draggingAddedIssueRow;
            _draggingAddedIssueRow = null;

            DateTime slotDate = ParseHelper.GetDateTimeFromObject(row["issueDate"], DateTime.MinValue);
            if (slotDate == DateTime.MinValue) return;

            ((DataGridView)sender).DoDragDrop(
                new RangeIssueDragPayload(slotDate, new List<System.Data.DataRow> { row }),
                DragDropEffects.Move);
        }

        private void RangeGrid_MouseDown(object sender, MouseEventArgs e)
        {
            _dragSourceSlotDate = null;
            if (e.Button != MouseButtons.Left || _tariffGrid.EditMode != EditMode.View) return;

            DataGridView grid = (DataGridView)sender;
            DataGridView.HitTestInfo hit = grid.HitTest(e.X, e.Y);
            // Синяя ячейка — выпуск во всех выбранных кампаниях, красная — в части из них.
            // Переносить можно и то и другое, состав кампаний сохраняется.
            if (!_tariffGrid.CellHasCurrentCampaignIssues(hit.RowIndex, hit.ColumnIndex)
                && !_tariffGrid.CellHasCurrentActionIssues(hit.RowIndex, hit.ColumnIndex)) return;

            ITariffWindow window = _tariffGrid.GetTariffWindowAt(hit.RowIndex, hit.ColumnIndex);
            if (window == null) return;

            _dragSourceSlotDate = window.WindowDate;
            _dragStartPoint = e.Location;
        }

        private void RangeGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragSourceSlotDate == null || !DragThresholdExceeded(e)) return;

            DateTime slotDate = _dragSourceSlotDate.Value;
            _dragSourceSlotDate = null;

            // Из ячейки переезжает весь получас: каждая группа «ролик + позиция» в своём составе
            // кампаний, из базы. Не AddedIssues — там только общие для всех кампаний ролики, и
            // в смешанном слоте остальные оставались бы на месте.
            TariffWithRangeGrid rangeGrid = (TariffWithRangeGrid)_tariffGrid;
            IList<TariffWithRangeGrid.SlotIssueGroup> groups = rangeGrid.GetSlotIssueGroups(slotDate);
            if (groups.Count == 0) return;

            ((DataGridView)sender).DoDragDrop(new RangeIssueDragPayload(slotDate, groups), DragDropEffects.Move);
        }

        private void RangeGrid_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(RangeIssueDragPayload))
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }

        private void RangeGrid_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            RangeIssueDragPayload payload = e.Data.GetData(typeof(RangeIssueDragPayload)) as RangeIssueDragPayload;
            if (payload == null) return;

            ITariffWindow target = GetWindowUnderDrag((DataGridView)sender, e);
            if (target != null && target.WindowDate != payload.SourceSlotDate)
                e.Effect = DragDropEffects.Move;
        }

        private ITariffWindow GetWindowUnderDrag(DataGridView grid, DragEventArgs e)
        {
            System.Drawing.Point pt = grid.PointToClient(new System.Drawing.Point(e.X, e.Y));
            DataGridView.HitTestInfo hit = grid.HitTest(pt.X, pt.Y);
            return _tariffGrid.GetTariffWindowAt(hit.RowIndex, hit.ColumnIndex);
        }

        /// <summary>
        /// Текст подтверждения переноса. Если в целевом окне уже есть выпуск этой же фирмы
        /// (из любой акции — см. TariffWithRangeGrid.CheckFirmConflict), предупреждение о
        /// конфликте идёт первой фразой того же диалога, а не отдельным вторым окном; вызывающий
        /// код по hasFirmConflict показывает диалог с иконкой предупреждения, а не вопроса.
        /// </summary>
        private string BuildMoveConfirmationQuestion(TariffWithRangeGrid rangeGrid, RangeIssueDragPayload payload,
            DateTime targetDate, out bool hasFirmConflict)
        {
            string targetDateStr = targetDate.ToString("dd.MM.yyyy HH:mm");
            string scope = payload.PartialGroups != null
                ? "в тех кампаниях, где он есть"
                : "по выбранным кампаниям";
            string question = payload.Count == 1
                ? string.Format("Перенести выпуск в окно '{0}' {1}?", targetDateStr, scope)
                : string.Format("Перенести выпуски ({0} шт.) в окно '{1}' {2}?",
                    payload.Count, targetDateStr, scope);

            TariffWithRangeGrid.FirmConflictInfo conflict =
                rangeGrid.CheckFirmConflict(GetMovingCampaignIds(rangeGrid, payload), targetDate);
            hasFirmConflict = conflict.HasConflict;
            if (!conflict.HasConflict)
                return question;

            string confirmedText = conflict.AnyConfirmed ? "акция подтверждена" : "акция ещё не подтверждена";
            return string.Format("В этом окне уже есть выпуск фирмы «{0}» ({1}). {2}",
                _action.FirmName, confirmedText, question);
        }

        // Кампании — участники переноса: для красного слота это кампании конкретных групп
        // (только те, где выпуск реально стоит), для синего — весь текущий выбор чек-листа.
        private List<int> GetMovingCampaignIds(TariffWithRangeGrid rangeGrid, RangeIssueDragPayload payload)
        {
            if (payload.PartialGroups != null)
            {
                HashSet<int> ids = new HashSet<int>();
                foreach (TariffWithRangeGrid.SlotIssueGroup group in payload.PartialGroups)
                    foreach (int campaignId in group.CampaignIds)
                        ids.Add(campaignId);
                return new List<int>(ids);
            }

            return rangeGrid.SelectedCampaignIds != null
                ? new List<int>(rangeGrid.SelectedCampaignIds)
                : new List<int>();
        }

        private void RangeGrid_DragDrop(object sender, DragEventArgs e)
        {
            RangeIssueDragPayload payload = e.Data.GetData(typeof(RangeIssueDragPayload)) as RangeIssueDragPayload;
            if (payload == null) return;

            ITariffWindow target = GetWindowUnderDrag((DataGridView)sender, e);
            if (target == null || target.WindowDate == payload.SourceSlotDate) return;

            TariffWithRangeGrid rangeGrid = (TariffWithRangeGrid)_tariffGrid;
            bool hasFirmConflict;
            string question = BuildMoveConfirmationQuestion(rangeGrid, payload, target.WindowDate, out hasFirmConflict);
            DialogResult confirmResult = hasFirmConflict
                ? UserMessage.ShowWarningQuestion(question)
                : UserMessage.ShowQuestion(question);
            if (confirmResult != DialogResult.Yes)
                return;

            try
            {
                Cursor = Cursors.WaitCursor;
                MoveRangeIssuesToSlot(payload, target.WindowDate);
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
        /// Перенос всех master-выпусков слота в другое окно: удаление через MasterIssueDelete и
        /// добавление через AddRangeIssues на каждый выпуск, один пересчёт акции — всё в одной
        /// транзакции (паттерн линейного MoveIssuesToWindow). Ролик и позиция сохраняются от
        /// исходного выпуска. Проверки валидности целевого окна делают хранимые процедуры.
        /// </summary>
        private void MoveRangeIssuesToSlot(RangeIssueDragPayload payload, DateTime targetSlotDate)
        {
            TariffWithRangeGrid rangeGrid = (TariffWithRangeGrid)_tariffGrid;
            Entity masterEntity = EntityManager.GetEntity((int)Entities.MasterIssues);

            DataAccessor.BeginTransaction();
            try
            {
                if (payload.PartialGroups != null)
                {
                    // Частичный слот: удаляем и ставим ровно в тех кампаниях, где выпуск был.
                    // Если в целевом окне места нет хотя бы для одной из них, AddRangeIssues
                    // ругается и вся транзакция откатывается — перенос не состоится.
                    foreach (TariffWithRangeGrid.SlotIssueGroup group in payload.PartialGroups)
                        rangeGrid.DeleteSlotIssueGroup(group, payload.SourceSlotDate);

                    foreach (TariffWithRangeGrid.SlotIssueGroup group in payload.PartialGroups)
                        rangeGrid.AddSlotIssueGroup(group, targetSlotDate);
                }
                else
                {
                    foreach (System.Data.DataRow row in payload.IssueRows)
                    {
                        PresentationObject issue = masterEntity.CreateObject(row);
                        if (!issue.Delete(true))
                            throw new InvalidOperationException("Не удалось удалить выпуск из исходного окна.");
                    }

                    foreach (System.Data.DataRow row in payload.IssueRows)
                    {
                        Roller roller = new Roller(ParseHelper.GetInt32FromObject(row[Roller.ParamNames.RollerId], 0));
                        RollerPositions position =
                            (RollerPositions)ParseHelper.GetInt32FromObject(row[Issue.ParamNames.PositionId], 0);
                        rangeGrid.AddIssuesRange(targetSlotDate, roller, position, false, recalculate: false);
                    }
                }

                _action.Recalculate();
                DataAccessor.CommitTransaction();
            }
            catch
            {
                DataAccessor.RollbackTransaction();
                // AddIssuesRange успевает дописать строки в in-memory AddedIssues до отката —
                // пересобираем таблицу из БД, чтобы сетка и список не разошлись с базой.
                rangeGrid.RebuildAddedIssues();
                RefreshGrid();
                throw;
            }

            if (payload.PartialGroups != null)
                // Группы могли включать и общие для всех кампаний ролики — их строки в
                // AddedIssues теперь на старой дате, пересобираем из базы.
                rangeGrid.RebuildAddedIssues();
            else
                foreach (System.Data.DataRow row in payload.IssueRows)
                    rangeGrid.AddedIssues.Rows.Remove(row);
            RefreshGrid();
        }
	}
}
