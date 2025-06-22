using System.Text;

namespace FogSoft.WinForm.License
{
	internal static class KeyManager
	{
		public static readonly byte[] IV =
			{
				24, 168, 172, 120, 30, 176, 100, 37, 196, 5
			};

		public static readonly Encoding Encoding = Encoding.UTF8;
	}
}