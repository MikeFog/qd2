using System.IO;
using CrystalDecisions.Shared;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.GridExport
{
	public class ExportHelper
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

		public static ExportFormatType CrystalExportFormatType
		{
			get
			{
				return ConfigurationUtil.GetEnumSettings("CrystalExportFormatType", ExportFormatType.WordForWindows);
			}
		}

		public static string CrystalExportFormatTypeExtension
		{
			get
			{
				return ConfigurationUtil.GetSettings("CrystalExportFormatTypeExtension", ".doc");
			}
		}
	}
}