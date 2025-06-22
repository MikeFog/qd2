using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FogSoft.LicenseGenerator
{
	public class LicenseWriter
	{
		// TODO: create paramter, add to form (with default value)
		private const int GUID_LICENSE_COUNT = 1000;
		private readonly IProgress _progress;
		private readonly byte[] _key;

		public LicenseWriter(string keyPath, IProgress progress)
			: this(KeyManager.GetKeyFromFile(keyPath), progress)
		{
		}

		public LicenseWriter(byte[] key, IProgress progress)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			
			_key = key;
			
			if (progress != null)
			{
				_progress = progress;
				_progress.StepCount = 5;
			}
		}

		private void StepCompleted()
		{
			if (_progress != null)
				_progress.StepCompleted();
		}

		public void WriteLicense(string templateFileName, string licenseFileName)
		{
			StepCompleted();

			string template = File.ReadAllText(templateFileName, KeyManager.Encoding);

			StepCompleted();
			string license;

			if (template.StartsWith("Guid:{0}", StringComparison.InvariantCultureIgnoreCase))
			{
				StringBuilder builder = new StringBuilder(GUID_LICENSE_COUNT * (template.Length+2));
				for (int i = 0; i < GUID_LICENSE_COUNT; i++ )
				{
					builder.AppendLine(GetLicense(string.Format(template, Guid.NewGuid())));
				}

				license = builder.ToString();
			}
			else
			{
				license = GetLicense(template);
			}
			
			
			File.WriteAllText(licenseFileName, license, KeyManager.Encoding);
			if(_progress != null)
				_progress.WorkCompleted();
		}

		public string GetLicense(string licenseContent)
		{
			string license;
			using (MemoryStream ms = new MemoryStream(licenseContent.Length + licenseContent.Length/2))
			{
				byte[] bytes = KeyManager.Encoding.GetBytes(licenseContent);
				using (CryptoStream cs = new CryptoStream
					(ms, new TripleDESCryptoServiceProvider().CreateEncryptor(_key, KeyManager.IV),
					 CryptoStreamMode.Write))
				{
					cs.Write(bytes, 0, bytes.Length);
					StepCompleted();
				}
				license = Convert.ToBase64String(ms.ToArray());
				StepCompleted();
			}
			return license;
		}
	}
}