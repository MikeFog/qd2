using System.Text;

namespace Merlin.Classes.GridExport.DJinSerializer
{
	class DJinParam
	{
		public const string strFilterDialog = "DJin (*.TXT)|*.txt";

		public const string strBlockComment = "{0} в {1}"; // i18n-ok: содержимое файла для эфирной программы DJin, не интерфейс
		public const string strAdvert = "Реклама"; // i18n-ok: содержимое файла для эфирной программы DJin, не интерфейс
		public const string strBlockEnd = "E";
		//public const string strBlockStart = "BT";
		public const string strEtc = "m";
		public const string strJingle = "j";
		// Временная метка джингла влёта (In): нужна BlockManager для порядка в блоке, в файл пишется как strJingle
		public const string strJingleIn = "j-in";
		// Метка места влёта: окно склейки, которому положен влёт, без рекламы (пустое или только промо
		// без спонсора, 9) - своего влёта у него нет. Строка без файла, длительность 0: по ней добивщик
		// ставит анонс туда, где был бы влёт, и вычищает её перед DJin
		public const string strJingleInPlace = "fake-in";
		public const string strLine = "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"\r\n";
        public const string strLine2 = "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"";
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