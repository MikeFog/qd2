using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Classes.Domain;
using Merlin.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Merlin.Forms.CreateActionMaster
{
	internal partial class EditIssuesForm : CampaignForm
	{
        private readonly ActionOnMassmedia _action;
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

		protected override void SetFormCaption()
		{
			//base.SetFormCaption();
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				base.OnLoad(e);
				

                tbbTemplate.Visible = true;
                tbbTemplateUndo.Visible = true;
                grdCurrentCampaignIssues.Caption = "Добавленные выпуски";

                RefreshGrid();
				_tariffGrid.GridRefreshed += TariffGridRefreshed;
				_action.DisplayData(lstStat);
				grdCurrentCampaignIssues.Entity = EntityManager.GetEntity((int)Entities.MasterIssues);
				ShowCurrentIssues(_tariffGrid as TariffWithRangeGrid);
				EnableWindowSelectionDelete();
				EnableRangeIssueDragDrop();

            }
			catch (Exception ex)
			{
                ErrorManager.PublishError(ex);
            }
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
            foreach (ITariffWindow window in windows)
            {
                foreach (System.Data.DataRow row in rangeGrid.AddedIssues.Select(
                    string.Format("[issueDate] = '{0}'", window.WindowDate)))
                    issues.Add(masterEntity.CreateObject(row));
            }

            if (issues.Count == 0)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowInformation("В выбранных окнах нет добавленных выпусков.");
                return;
            }

            if (FogSoft.WinForm.Forms.MessageBox.ShowQuestion(
                    string.Format("Удалить выпуски в выбранных окнах на всех радиостанциях акции? ({0} шт.)", issues.Count)) != DialogResult.Yes)
                return;

            List<PresentationObject> deletedObjects = new List<PresentationObject>();
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
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            // Если удалён хотя бы один — событие чистит AddedIssues, пересчитывает акцию и обновляет сетку.
            if (deletedObjects.Count > 0)
                grdCurrentCampaignIssues.RaiseObjectsDeleted(deletedObjects);

            if (deleteErrors.Rows.Count > 0)
                FogSoft.WinForm.Controls.SmartGrid.ShowDeleteErrors(deleteErrors);
            else
                FogSoft.WinForm.Forms.MessageBox.ShowInformation(string.Format("Удалено выпусков: {0}.", deletedObjects.Count));
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

            public RangeIssueDragPayload(DateTime sourceSlotDate, List<System.Data.DataRow> issueRows)
            {
                SourceSlotDate = sourceSlotDate;
                IssueRows = issueRows;
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
            if (!_tariffGrid.CellHasCurrentCampaignIssues(hit.RowIndex, hit.ColumnIndex)) return;

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
            if (rows.Count == 0) return;

            ((DataGridView)sender).DoDragDrop(new RangeIssueDragPayload(slotDate, rows), DragDropEffects.Move);
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
            string question = payload.IssueRows.Count == 1
                ? string.Format("Перенести выпуск в окно '{0}' на всех радиостанциях акции?", targetDateStr)
                : string.Format("Перенести выпуски ({0} шт.) в окно '{1}' на всех радиостанциях акции?",
                    payload.IssueRows.Count, targetDateStr);
            if (FogSoft.WinForm.Forms.MessageBox.ShowQuestion(question) != DialogResult.Yes)
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
