using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using FogSoft.LicenseGenerator.Properties;

namespace FogSoft.LicenseGenerator
{
	public static class KeyManager
	{
		public static readonly byte[] IV =
			{
				24, 168, 172, 120, 30, 176, 100, 37, 196, 5
			};

		public static readonly Encoding Encoding = Encoding.GetEncoding(new Settings().LicenceEncoding) ?? Encoding.ASCII;

		public static void GenerateKey(string keyPath)
		{
			TripleDESCryptoServiceProvider csp = new TripleDESCryptoServiceProvider();
			csp.GenerateKey();

			StringBuilder builder = new StringBuilder();
			builder.Append("author:").AppendLine(WindowsIdentity.GetCurrent().Name)
				.Append("creationTime:").AppendLine(DateTime.UtcNow.ToString("u"))
				.Append("key:").AppendLine(Convert.ToBase64String(csp.Key));

			File.WriteAllText(keyPath, builder.ToString(), Encoding);
		}

		public static byte[] GetKeyFromFile(string keyPath)
		{
			ColonLinesReader reader = new ColonLinesReader();
			Dictionary<string, string> properties = reader.GetPropertiesFromFile(keyPath);
			if (!properties.ContainsKey("key"))
				throw new ArgumentException(string.Format("File '{0}' does not contains 'key:' line.", keyPath));

			return GetKeyFromBase64(properties["key"]);
		}

		public static byte[] GetKeyFromBase64(string key)
		{
			return Convert.FromBase64String(key);
		}
	}
}
