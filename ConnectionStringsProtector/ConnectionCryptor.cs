using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ConnectionStringsProtector
{
	internal static class ConnectionCryptor
	{
		private static readonly byte[] _key = {
		                                      	241, 170, 225, 152, 226, 174, 72, 105, 80, 244, 151, 111, 6, 161, 163, 253, 111
		                                      	, 29, 114, 92, 232, 197, 60, 207
		                                      };

		private static readonly byte[] _IV = {176, 227, 160, 222, 130, 99, 26, 220};

		public static string Crypt(string str)
		{
			using (ICryptoTransform ct = new TripleDESCryptoServiceProvider().CreateEncryptor(_key, _IV))
			{
				byte[] b = Encoding.UTF8.GetBytes(str);
				using (MemoryStream m = new MemoryStream())
				{
					using (CryptoStream c = new CryptoStream(m, ct, CryptoStreamMode.Write))
					{
						c.Write(b, 0, b.Length);
					}

					return Convert.ToBase64String(m.ToArray());
				}
			}
		}

		public static string Decrypt(string str)
		{
			using (ICryptoTransform ct = new TripleDESCryptoServiceProvider().CreateDecryptor(_key, _IV))
			{
				byte[] b = Convert.FromBase64String(str);
				using (MemoryStream m = new MemoryStream())
				{
					using (CryptoStream c = new CryptoStream(m, ct, CryptoStreamMode.Write))
					{
						c.Write(b, 0, b.Length);
					}
					return Encoding.UTF8.GetString(m.ToArray());
				}
			}
		}
	}
}