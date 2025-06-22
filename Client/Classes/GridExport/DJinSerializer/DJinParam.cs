using System.Text;

namespace Merlin.Classes.GridExport.DJinSerializer
{
	class DJinParam
	{
		public const string strFilterDialog = "DJin (*.TXT)|*.txt";

		public const string strBlockComment = "{0} в {1}";
		public const string strAdvert = "Реклама";
		public const string strBlockEnd = "E";
		//public const string strBlockStart = "BT";
		public const string strEtc = "m";
		public const string strJingle = "j";
		public const string strLine = "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"\r\n";
		public const string strRoller = "c";
		public const string strRollerNews = "n";
		public const string strRollerProgram = "p";
		public const string strDefaultExt = ".mp3";

		public static Encoding Encoding
		{
			get { return Encoding.GetEncoding("windows-1251"); }
		}
	}
}