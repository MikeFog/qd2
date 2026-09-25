using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using FogSoft.WinForm;

namespace Merlin.Classes.GridExport
{
	// UI-часть ExportDocument: выгрузка с выбором папки (FolderBrowserDialog). Бизнес-часть —
	// ExportDocument.cs. Конвенция — docs/tasks/web-migration-dialogs.md.
	abstract partial class ExportDocument
	{
		public void Export(DataTable data, Massmedia mm, DateTime date)
		{
			try
			{
				if (mm == null)
					return;

				MassmediaPricelist pl = (MassmediaPricelist) mm.GetPriceList(date);
				string broadcastTime = pl.BroadcastStart.ToString("HHmm");

				FolderBrowserDialog dlg = new FolderBrowserDialog();
				Application.DoEvents();
				if (dlg.ShowDialog(Globals.MdiParent) == DialogResult.OK)
				{
					string mmName = mm.Name;
					mmName = ExportHelper.RemoveInvalidFileNameChars(mmName);
					string fileName = string.Format("{0}{1}{2}", dlg.SelectedPath, Path.DirectorySeparatorChar, mmName);

					foreach (ExportFile file in Build(mm, date, fileName, data, broadcastTime))
						File.WriteAllBytes(file.Name, file.Content);
					if (ExportHelper.OpenFolderOnFinish)
						Process.Start(dlg.SelectedPath);
				}
			}
			catch(Exception e)
			{
				Log.Error("CouldNotToExport", e);
				Globals.ShowExclamation("CouldNotToExport");
			}
		}

	}
}
