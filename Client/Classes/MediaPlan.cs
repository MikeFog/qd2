using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Merlin.Classes
{
	/// <summary>
	/// Печать медиаплана в десктопе: диалоги (настройки печати, выбор роликов)
	/// и файл. Сам медиаплан строит <see cref="MediaPlanBuilder"/>, книгу —
	/// <see cref="OpenXmlExportDocument"/> (оба в FogSoft.Core, общие с вебом);
	/// Excel нужен только чтобы открыть готовый файл.
	/// </summary>
    internal class MediaPlan
	{
		private readonly MediaPlanBuilder _builder;
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

                ExportMediaPlan();

				Application.UseWaitCursor = false;
				if (!string.IsNullOrEmpty(_savedFilePath))
				{
					UserMessage.ShowCompleted($"Файл успешно сохранён: {_savedFilePath}");
				}
			}
			catch(Exception e)
			{
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

			var document = new OpenXmlExportDocument();
			// Нет данных — нет файла (раньше так же не открывался пустой Excel).
			if (!_builder.Build(document))
				return;

			string folder = UserSettings.Load("Path2SaveReports") ?? string.Empty;
			bool canSaveToDisk = _printSettings.SaveDirectlyToDisk
				&& !string.IsNullOrEmpty(folder)
				&& Directory.Exists(folder);

			byte[] content = document.ToArray();
			if (canSaveToDisk)
			{
				_savedFilePath = WriteFile(folder, _builder.FileName, content);
			}
			else
			{
				// Раньше книга открывалась в Excel несохранённой; теперь — файлом
				// из временной папки (решение по docs/tasks/web-mediaplan.md, 2.4).
				string tempFolder = Path.Combine(Path.GetTempPath(), "qd2");
				Directory.CreateDirectory(tempFolder);
				Process.Start(WriteFile(tempFolder, _builder.FileName, content));
			}
		}

		// Пишет файл; если файл с таким именем открыт (например, прошлый медиаплан
		// ещё в Excel), берёт «имя (2).xlsx», «имя (3).xlsx»…
		private static string WriteFile(string folder, string fileName, byte[] content)
		{
			string name = Path.GetFileNameWithoutExtension(fileName);
			string ext = Path.GetExtension(fileName);
			for (int i = 1; ; i++)
			{
				string path = Path.Combine(folder, i == 1 ? fileName : $"{name} ({i}){ext}");
				try
				{
					File.WriteAllBytes(path, content);
					return path;
				}
				catch (IOException) when (i < 20 && File.Exists(path))
				{
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
	}
}
