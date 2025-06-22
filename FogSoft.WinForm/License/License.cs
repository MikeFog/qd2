using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Properties;

namespace FogSoft.WinForm.License
{
	public interface ILicense
	{
		string Check();
	}

	public abstract class License<T> : ILicense
		where T: License<T>, new()
	{
		protected const string NULLVALUE = "NULL";

		private static readonly byte[] key = {
		                                     	208, 146, 206, 81, 212, 126, 246, 183, 116, 146, 49, 56, 252, 163, 185, 159, 252
		                                     	, 212, 199, 233, 177,
		                                     	14, 226, 152
		                                     };

		public static Encoding Encoding
		{
			get { return KeyManager.Encoding; }
		}

		public static void SaveLicence(byte[] license, int lenght)
		{
			if (license.Length > 500)
				throw new ArgumentOutOfRangeException("license", "License must be less 500");
			byte[] bytes = new byte[500];
			for (int i = license.Length - 1; i >= 0; i--)
			{
				bytes[500 - (license.Length - i)] = license[i];
			}
			DataAccessor.ExecuteNonQuery("LicenseLoad",
			                             new Dictionary<string, object> {{"license", bytes}, {"licenseLenght", lenght}});
		}

		private static byte[] GetLicenceFromDB(out int lenght)
		{
			lenght = 0;
			DataSet ds = DataAccessor.LoadDataSet("LicenseGet", new Dictionary<string, object>());
			if (ds == null || ds.Tables.Count != 2 || ds.Tables[0].Rows.Count != 1 || ds.Tables[1].Rows.Count != 1)
				return null;

			byte[] res = ds.Tables[0].Rows[0][0] as byte[];
			int? l = (int?) ds.Tables[1].Rows[0][0];
			if (res == null || !l.HasValue || l.Value == 0)
				return null;

			lenght = l.Value;

			int i = 0;
			while (res[i] == 0)
				i++;
			byte[] lic = new byte[500 - i];
			for (int j = i; j < 500; j++)
				lic[j - i] = res[j];
			return lic;
		}

		public static T GetLicence()
		{
			int lenght;
			byte[] licenceB = GetLicenceFromDB(out lenght);
			if (licenceB == null)
				return null;

			LicenseReader reader = new LicenseReader(key);
			string licenceContent = reader.ReadLicense(licenceB, lenght);
			if (string.IsNullOrEmpty(licenceContent))
				return null;

			T license = new T();
			license.Init(licenceContent);
			return license;
		}

		public static T GetLicence(string content)
		{
			try
			{
				LicenseReader reader = new LicenseReader(key);
				string licenceContent = reader.ReadLicense(content);
				if (string.IsNullOrEmpty(licenceContent))
					return null;

				T license = new T();
				license.Init(licenceContent);
				return license;
			}
			catch(Exception e)
			{
				ErrorManager.LogError("License error", e);
				return null;
			}
		}

		public DateTime? Expired { get; set; }
		public string Product { get; set; }
		public string Company { get; set; }
		public int? UsersCount { get; set; }

		protected virtual void Init(string content)
		{
			ColonLinesReader reader = new ColonLinesReader();
			Dictionary<string, string> properties = reader.GetProperties(content);
			foreach (KeyValuePair<string, string> keyValuePair in properties)
			{
				if (string.Compare(keyValuePair.Key, "Expired") == 0)
					Expired = string.Compare(keyValuePair.Value, NULLVALUE) == 0
					          	? null
					          	: (DateTime?) DateTime.ParseExact(keyValuePair.Value, "yyyyMMdd", CultureInfo.CurrentCulture);
				else if (string.Compare(keyValuePair.Key, "Company") == 0)
					Company = keyValuePair.Value;
				else if (string.Compare(keyValuePair.Key, "Product") == 0)
					Product = keyValuePair.Value;
				else if (string.Compare(keyValuePair.Key, "UsersCount") == 0)
					UsersCount = string.Compare(keyValuePair.Value, NULLVALUE) == 0
									? null
									: (int?)ParseHelper.ParseToInt32(keyValuePair.Value);
			}
		}

		public static string CheckLicense()
		{
			try
			{
				T license = GetLicence();
				if (license == null)
					return Resources.LicenceError;
				return license.Check();
			}
			catch(Exception exp)
			{
				ErrorManager.LogError("Check license", exp);
				return Resources.LicenceError;
			}
		}

		public virtual string Check()
		{
			if (Expired.HasValue && DateTime.Compare(Expired.Value, DateTime.Now) < 0)
				return Resources.LicenseExpired;
			if (UsersCount.HasValue)
			{
				int massmediasCount = GetUsersCount();
				if (massmediasCount > UsersCount.Value)
					return string.Format(Resources.LicenseErrorUsersCount, Environment.NewLine);
			}
			return null;
		}

		public int GetUsersCount()
		{
			return ParseHelper.GetInt32FromObject(DataAccessor.LoadDataSet("Users", new Dictionary<string, object>()).Tables[0].Rows.Count, UsersCount.Value + 1);
		}
	}
}