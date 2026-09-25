using CrystalDecisions.Shared;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes.GridExport
{
	// Десктопная часть ExportHelper: формат, в котором Crystal выгружает сетку (Word).
	public partial class ExportHelper
	{
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
