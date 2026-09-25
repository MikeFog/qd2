using System.IO;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.GridExport
{
	// Форматы Crystal (выгрузка сеток в Word из десктопа) — в ExportHelper.WinForms.cs.
	public partial class ExportHelper
	{
		public static string RemoveInvalidFileNameChars(string mmName)
		{
			foreach (char ch in Path.GetInvalidFileNameChars())
				mmName = mmName.Replace(ch, '_');
			return mmName;
		}

		public static bool OpenFolderOnFinish
		{
			get
			{
				return ConfigurationUtil.GetBooleanSettings("ExportOpenFolderOnFinish", true);
			}
		}
	}
}