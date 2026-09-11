using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Classes.Domain;
using Merlin.Controls;
using System;
using System.Collections.Generic;
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
				// Блок статистики (lstStat) стал компактным — 2 строки вместо 7, см.
				// ActionOnMassmedia.DisplayData. Освободившееся место отдаём чек-листу кампаний
				// (grdCampaigns), которому иначе почти ничего не остаётся.
				splitContainer3.SplitterDistance = lstStat.ItemHeight * 2 + 12;
				// SplitContainer.FixedPanel=Panel2, выставленный в Designer.cs, не переживает
				// первый реальный layout формы (там Panel2 ужимался до дизайнерского значения
				// ~74px вместо ожидаемого) — высоту grdCampaigns фиксируем здесь, когда форма
				// уже реально размещена и splitContainerCampaigns.Height настоящий.
				const int campaignsHeight = 180;
				int available = splitContainerCampaigns.Height - splitContainerCampaigns.SplitterWidth;
				if (available > campaignsHeight + splitContainerCampaigns.Panel1MinSize)
					splitContainerCampaigns.SplitterDistance = available - campaignsHeight;
                tbbTemplate.Visible = true;
                tbbTemplateUndo.Visible = true;
                grdCurrentCampaignIssues.Caption = "Добавленные выпуски";

                RefreshGrid();
				_tariffGrid.GridRefreshed += TariffGridRefreshed;
				_action.DisplayData(lstStat);
				grdCurrentCampaignIssues.Entity = EntityManager.GetEntity((int)Entities.MasterIssues);
				ShowCurrentIssues(_tariffGrid as TariffWithRangeGrid);
				EnableWindowSelectionActions();
				EnableRangeIssueDragDrop();

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
        /// Чек-лист линейных кампаний акции: с какими из них работает веер. По умолчанию
        /// отмечены все. Применяется кнопкой «Обновить» на тулбаре (см. RefreshGrid).
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
        /// При нуле выбранных кампаний тулбар погашен — вместе с кнопкой «Обновить», которой
        /// выбор и применяется. Чтобы не получился тупик, первую же поставленную галочку
        /// применяем сразу. Через BeginInvoke: при «отметить все» событие приходит на каждую
        /// строку, а перезабросить сетку нужно один раз и уже после всего цикла.
        /// </summary>
        private void CampaignSelectionChanged(object sender, System.Data.DataColumnChangeEventArgs e)
        {
            try
            {
                if (e.Column.ColumnName != FogSoft.WinForm.Controls.SmartGrid.COL_IsSelected)
                    return;
                if (IsToolbarEnabled || !(e.ProposedValue is bool) || !(bool)e.ProposedValue)
                    return;

                BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        if (!IsToolbarEnabled)
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
        /// «Обновить» на тулбаре применяет текущий выбор кампаний: пересобирает «Добавленные
        /// выпуски» (пересечение слотов считается по выбранным кампаниям) и перезабрасывает сетку.
        /// Выпуски, добавленные ранее по другим кампаниям, остаются в базе, но из веера уходят —
        /// это ожидаемое поведение.
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
        }

        protected override void ShowWindowIssues(ITariffWindow tariffWindow)
        {
            //base.ShowWindowIssues(tariffWindow);
            TariffGridRefreshed();
        }

	    private void ShowCurrentIssues(TariffWithRangeGrid grid)
	    {
			grdCurrentCampaignIssues.DataSource = grid.AddedIssues.DefaultView;
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
        /// Выпуски (master issues) берём из in-memory AddedIssues по дате окна — тем же
        /// сопоставлением, что и подсветка в TariffWithRangeGrid.MarkCells. Каждый удаляем через
        /// MasterIssue.Delete -> MasterIssueDelete (выпуск удаляется на всех радиостанциях акции).
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
            List<PresentationObject> issues = new List<PresentationObject>();
            // Частичные («красные») слоты: выпуск есть не во всех выбранных кампаниях, в
            // AddedIssues его нет — содержимое читаем из базы и удаляем по тем кампаниям,
            // где оно реально стоит.
            List<KeyValuePair<DateTime, TariffWithRangeGrid.SlotIssueGroup>> partialGroups =
                new List<KeyValuePair<DateTime, TariffWithRangeGrid.SlotIssueGroup>>();

            foreach (ITariffWindow window in windows)
            {
                System.Data.DataRow[] rows = rangeGrid.AddedIssues.Select(
                    string.Format("[issueDate] = '{0}'", window.WindowDate));
                if (rows.Length > 0)
                {
                    foreach (System.Data.DataRow row in rows)
                        issues.Add(masterEntity.CreateObject(row));
                    continue;
                }

                foreach (TariffWithRangeGrid.SlotIssueGroup group in rangeGrid.GetSlotIssueGroups(window.WindowDate))
                    partialGroups.Add(
                        new KeyValuePair<DateTime, TariffWithRangeGrid.SlotIssueGroup>(window.WindowDate, group));
            }

            int totalCount = issues.Count + partialGroups.Count;
            if (totalCount == 0)
            {
                UserMessage.ShowInformation("В выбранных окнах нет выпусков этой акции.");
                return;
            }

            if (UserMessage.ShowQuestion(
                    string.Format("Удалить выпуски в выбранных окнах по выбранным кампаниям? ({0} шт.)", totalCount)) != DialogResult.Yes)
                return;

            List<PresentationObject> deletedObjects = new List<PresentationObject>();
            int partialDeletedCount = 0;
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
                            deletedObjects.Add(issue);
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
                UserMessage.ShowInformation(string.Format("Удалено выпусков: {0}.",
                    deletedObjects.Count + partialDeletedCount));
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

            TariffWithRangeGrid rangeGrid = (TariffWithRangeGrid)_tariffGrid;
            List<System.Data.DataRow> rows = new List<System.Data.DataRow>(
                rangeGrid.AddedIssues.Select(string.Format("[issueDate] = '{0}'", slotDate)));

            RangeIssueDragPayload payload;
            if (rows.Count > 0)
            {
                payload = new RangeIssueDragPayload(slotDate, rows);
            }
            else
            {
                IList<TariffWithRangeGrid.SlotIssueGroup> groups = rangeGrid.GetSlotIssueGroups(slotDate);
                if (groups.Count == 0) return;
                payload = new RangeIssueDragPayload(slotDate, groups);
            }

            ((DataGridView)sender).DoDragDrop(payload, DragDropEffects.Move);
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

        private void RangeGrid_DragDrop(object sender, DragEventArgs e)
        {
            RangeIssueDragPayload payload = e.Data.GetData(typeof(RangeIssueDragPayload)) as RangeIssueDragPayload;
            if (payload == null) return;

            ITariffWindow target = GetWindowUnderDrag((DataGridView)sender, e);
            if (target == null || target.WindowDate == payload.SourceSlotDate) return;

            string targetDateStr = target.WindowDate.ToString("dd.MM.yyyy HH:mm");
            string scope = payload.PartialGroups != null
                ? "в тех кампаниях, где он есть"
                : "по выбранным кампаниям";
            string question = payload.Count == 1
                ? string.Format("Перенести выпуск в окно '{0}' {1}?", targetDateStr, scope)
                : string.Format("Перенести выпуски ({0} шт.) в окно '{1}' {2}?",
                    payload.Count, targetDateStr, scope);
            if (UserMessage.ShowQuestion(question) != DialogResult.Yes)
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

            foreach (System.Data.DataRow row in payload.IssueRows)
                rangeGrid.AddedIssues.Rows.Remove(row);
            RefreshGrid();
        }
	}
}
