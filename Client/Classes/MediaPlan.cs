using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Merlin.Classes
{
	/// <summary>
	/// Печать медиаплана в десктопе: диалоги (настройки печати, выбор роликов),
	/// Excel через COM и сохранение файла. Сам медиаплан строит
	/// <see cref="MediaPlanBuilder"/> (входит в FogSoft.Core).
	/// </summary>
    internal class MediaPlan
	{
		private readonly MediaPlanBuilder _builder;
        private bool exportStarted = false;
		private PrintSettings _printSettings;
		private string _savedFilePath;

		#region Singleton

        private MediaPlan(Action action, IList<Campaign> campaigns, IList<DateTime> monthes, DateTime? from, DateTime? to, bool selectively, IList<Action> actions = null)
		{
			_builder = new MediaPlanBuilder(action, campaigns, monthes, from, to, selectively, actions);
		}

        public static MediaPlan CreateInstance(IList<Action> actions, bool selectively)
		{
			return new MediaPlan(null, null, null, null, null, selectively, actions);
		}

        public static MediaPlan CreateInstance(Campaign campaign, IList<DateTime> monthes, bool selectively)
		{
			IList<Campaign> campaigns = new List<Campaign> {campaign};
            return new MediaPlan(null, campaigns, monthes, null, null, selectively);
		}

        public static MediaPlan CreateInstance(Campaign campaign, bool selectively)
		{
			IList<Campaign> campaigns = new List<Campaign> {campaign};
            return new MediaPlan(null, campaigns, null, null, null, selectively);
		}

        public static MediaPlan CreateInstance(Campaign campaign, DateTime dtFrom, DateTime dtTo, bool selectively)
		{
			IList<Campaign> campaigns = new List<Campaign> {campaign};
            return new MediaPlan(null, campaigns, null, dtFrom, dtTo, selectively);
		}

        public static MediaPlan CreateInstance(IList<Campaign> campaigns, IList<DateTime> monthes, bool selectively)
		{
            return new MediaPlan(null, campaigns, monthes, null, null, selectively);
		}

        public static MediaPlan CreateInstance(IList<Campaign> campaigns, bool selectively)
		{
            return new MediaPlan(null, campaigns, null, null, null, selectively);
		}

        public static MediaPlan CreateInstance(IList<Campaign> campaigns, DateTime dtFrom, DateTime dtTo, bool selectively)
		{
            return new MediaPlan(null, campaigns, null, dtFrom, dtTo, selectively);
		}

        public static MediaPlan CreateInstance(Action action, bool selectively)
		{
            return new MediaPlan(action, null, null, null, null, selectively);
		}

		#endregion

		// Медиаплан всегда строится по фактическим окнам выпусков (@isFact = 1 в
		// процедурах). Раньше Show принимал isFact и тут же перезаписывал его на true.
		public void Show()
		{
			_savedFilePath = null;
            CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;
            // Проверка пути сохранения может уходить в недоступный сетевой каталог и висеть
            // несколько секунд, поэтому курсор ожидания ставим до неё.
            Application.UseWaitCursor = true;
            Application.DoEvents();
            try
			{
                string savedPath = UserSettings.Load("Path2SaveReports");
				bool pathIsSet = !string.IsNullOrWhiteSpace(savedPath) && Directory.Exists(savedPath);
				var frmSettings = new Forms.PrintMediaPlanSettings(pathIsSet);

				// В самом диалоге курсор обычный, ожидание возобновляем на время экспорта.
				Application.UseWaitCursor = false;
				if(frmSettings.ShowDialog(Globals.MdiParent) == DialogResult.Cancel) return;
				_printSettings = frmSettings.Settings;
				_builder.Settings = _printSettings;

				Application.UseWaitCursor = true;
				Application.DoEvents();

                // Экспорт идёт синхронно на UI-потоке (STA): Excel создаётся и
                // освобождается на одном апартаменте — без маршалинга между потоками,
                // из-за которого процесс EXCEL.EXE раньше зависал в памяти.
                ExportMediaPlan();

				Application.UseWaitCursor = false;
				if (!string.IsNullOrEmpty(_savedFilePath))
				{
					UserMessage.ShowCompleted($"Файл успешно сохранён: {_savedFilePath}");
				}
			}
			catch(Exception e)
			{
				// Экспорт мог упасть на середине, когда Excel ещё скрыт (ScreenUpdating=false).
				// Показываем то, что успело записаться, чтобы не оставлять окно без владельца.
				if (exportStarted)
				{
					try { ExportManager.Application.FinishExport(); }
					catch { }
				}
				ErrorManager.LogError("Error to show media plan", e);
			}
			finally
			{
                Thread.CurrentThread.CurrentCulture = oldCulture;
                Application.UseWaitCursor = false;
			}
		}

        private void ExportMediaPlan()
		{
            if (_builder.Selectively && !SelectRollers())
                return;

			_builder.Build(new ExcelDocument(this));
            if (exportStarted)
            {
				string folder = UserSettings.Load("Path2SaveReports") ?? string.Empty;
				bool canSaveToDisk = _printSettings.SaveDirectlyToDisk
					&& !string.IsNullOrEmpty(folder)
					&& Directory.Exists(folder);

				if (canSaveToDisk)
				{
					string filePath = Path.Combine(folder, _builder.FileName);

					ExportManager.Application.SaveToDisk(filePath);
					_savedFilePath = filePath;
				}
				else
				{
					ExportManager.Application.FinishExport();
				}
            }
		}

		private bool SelectRollers()
        {
            DataTable dataTable = _builder.GetRollers();

            bool cancelled = true;

            var selectRollersAction = new System.Action(() =>
            {
                SelectionForm selectRollers = new SelectionForm(EntityManager.GetEntity((int)Entities.Roller), dataTable.DefaultView, "Выберите ролики", true);
                if (selectRollers.ShowDialog(Globals.MdiParent) == DialogResult.OK && selectRollers.AddedItems.Count > 0)
                {
                    string[] rollerIDs = new string[selectRollers.AddedItems.Count];
                    for (int i = 0; i < selectRollers.AddedItems.Count; i++)
                    {
                        rollerIDs[i] = selectRollers.AddedItems[i].Key;
                    }
                    _builder.SelectedRollers = string.Join(",", rollerIDs) + ",";
                    cancelled = false;
                }
            });

            // Список роликов грузится под курсором ожидания (его включает Show),
            // а в самом диалоге курсор обычный — как у диалога настроек печати.
            Application.UseWaitCursor = false;
            try
            {
                if (Globals.MdiParent.InvokeRequired)
                {
                    Globals.MdiParent.Invoke(selectRollersAction);
                }
                else
                {
                    selectRollersAction();
                }
            }
            finally
            {
                Application.UseWaitCursor = true;
                Application.DoEvents();
            }

            return !cancelled;
        }

        private void VerifyExportManager()
        {
            if (!exportStarted)
            {
                ExportManager.StartNewApplication();
                ExportManager.Application.StartExport();
                exportStarted = true;
            }
        }

		// Excel запускается только на первом листе с данными — как и раньше:
		// нет данных — нет пустого окна Excel.
		private sealed class ExcelDocument : IExportDocument
		{
			private readonly MediaPlan _owner;

			public ExcelDocument(MediaPlan owner)
			{
				_owner = owner;
			}

			public IDocumentSheet GetNewSheet(string name, string fontName, int fontSize)
			{
				_owner.VerifyExportManager();
				return ExportManager.Application.GetNewSheet(name, fontName, fontSize);
			}

			public void StartExport() => ExportManager.Application.StartExport();
			public void FinishExport() => ExportManager.Application.FinishExport();
			public void OnAppQuit() => ExportManager.Application.OnAppQuit();
			public bool Visible() => ExportManager.Application.Visible();
			public void SaveToDisk(string filePath) => ExportManager.Application.SaveToDisk(filePath);
		}
	}
}
