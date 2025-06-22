using System.Text;

namespace Merlin.Classes.GridExport.VideoDJSerializer
{
	class VideoDJParam
	{
		public const string JingleStart = "JINGLE: REKLAMA_START.WAV";
		public const string JingleEnd = "JINGLE: REKLAMA_END.WAV";
		public const string RollerFormat = "SPOT {0} 0, 0={1}";

		public static Encoding Encoding
		{
			get { return Encoding.GetEncoding("windows-1251"); }
		}
	}
}