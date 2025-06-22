using System;
using System.IO;
using System.Security.Cryptography;

namespace FogSoft.LicenseGenerator
{
	public class LicenseReader
	{
		private readonly byte[] _key;

		public LicenseReader(string keyPath) :
			this(KeyManager.GetKeyFromFile(keyPath))
		{
		}

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
			byte[] bytes; 

			using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(encodedLicense)))
			{
				using (CryptoStream cs = new CryptoStream
					(ms, new TripleDESCryptoServiceProvider().CreateDecryptor(_key, KeyManager.IV),
					 CryptoStreamMode.Read))
				{
					using (BinaryReader reader = new BinaryReader(cs))
					{
						// byte count cannot exceeded a base64 string length
						bytes = reader.ReadBytes(encodedLicense.Length);
					}
				}
			}
			
			return KeyManager.Encoding.GetString(bytes);
		}
	}
}