using System;
using System.IO;
using System.Security.Cryptography;

namespace FogSoft.WinForm.License
{
	internal class LicenseReader
	{
		private readonly byte[] _key;

		public LicenseReader(byte[] key)
		{
			_key = key;
		}

		public string ReadLicenseFromFile(string licenseFileName)
		{
			string encodedLicense = File.ReadAllText(licenseFileName, KeyManager.Encoding);
			return ReadLicense(encodedLicense);
		}

		public string ReadLicense(string encodedLicense)
		{
			return ReadLicense(Convert.FromBase64String(encodedLicense), encodedLicense.Length);
		}

		public string ReadLicense(byte[] encodedLicense, int lenght)
		{
			byte[] bytes;

			using (MemoryStream ms = new MemoryStream(encodedLicense))
			{
				using (CryptoStream cs = new CryptoStream
					(ms, new TripleDESCryptoServiceProvider().CreateDecryptor(_key, KeyManager.IV),
					 CryptoStreamMode.Read))
				{
					using (BinaryReader reader = new BinaryReader(cs))
					{
						// byte count cannot exceeded a base64 string length
						bytes = reader.ReadBytes(lenght);
					}
				}
			}

			return KeyManager.Encoding.GetString(bytes);
		}
	}
}