using FogSoft.WinForm.Classes;
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
                grdCurrentCampaignIssues.Caption = "Добавленные выпуски";

                RefreshGrid();
				_tariffGrid.GridRefreshed += TariffGridRefreshed;
				_action.DisplayData(lstStat);
				grdCurrentCampaignIssues.Entity = EntityManager.GetEntity((int)Entities.MasterIssues);
				ShowCurrentIssues(_tariffGrid as TariffWithRangeGrid);
				grdCurrentCampaignIssues.ObjectDeleted += GrdCurrentCampaignIssues_ObjectDeleted;
				EnableWindowSelectionDelete();

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

		private void GrdCurrentCampaignIssues_ObjectDeleted(PresentationObject presentationObject)
		{
            ProcessCurrentCampaignIssuesDelete(new List<PresentationObject> { presentationObject });
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
	}
}
