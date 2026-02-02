using System;
using System.Configuration;
using System.Reflection;
using log4net;

namespace FogSoft.WinForm.Classes
{
	/// <summary>
	/// Provides application configuration (e.g. connection strings by names).
	/// </summary>
	/// <remarks>Now temporarily use all keys like "%ConnectionString" from appSettings section.</remarks>
	public static class ConfigurationUtil
	{
		private static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static ConnectionStringSettings ConnectionStringMain
		{
			get { return GetConnectionString(MainConnectionString); }
		}

        public static bool HasLegacyDBConnectionString
        {
            get { return GetConnectionStringSilent(LegacyDBConnectionString) != null; }
        }

        public static ConnectionStringSettings ConnectionStringLegacyDB
        {
            get { return GetConnectionString(LegacyDBConnectionString); }
        }

		public static ConnectionStringSettings GetConnectionString(string connectionStringName)
		{
            ConnectionStringSettings settings = GetConnectionStringSilent(connectionStringName);
			if (settings == null)
			{
                throw new InvalidOperationException(
                        string.Format("Connection string '{0}' not found!", connectionStringName));
			}
			return settings;
		}

        public static string MainConnectionString
        {
            get { return GetSettings("MainConnectionString", "Main"); }
        }

        public static bool IsTestMode
		{
			get { return GetBooleanSettings("TestMode", false); }
		}

        public static bool IsPrintContactPerson
        {
            get { return GetBooleanSettings("PrintContactPersonInfo", true); }
        }

        public static int StoredProcExecutionTimeThreshold
        {
			get { return GetInt32Settings("StoredProcExecutionTimeThresholdMS", 1000); }
		}

        public static string LegacyDBConnectionString
        {
            get { return GetSettings("LegacyDBConnectionString", "LegacyDB"); }
        }

		public static bool IsShowBackgroung
		{
			get { return GetBooleanSettings("ShowBackgroung", true); }
		}

		public static bool IsRegUserSave
		{
			get { return GetBooleanSettings("RegUserSave", true); }
		}

		public static string RegUserSavePath
		{
			get { return GetSettings("RegUserSavePath", @"SOFTWARE\FogSoft\"); }
		}

		public static bool IsFullLoadDictionaries
		{
			get { return GetBooleanSettings("FullLoadDictionaries", true); }
		}

		public static int MinimalSplashScreenVisibleInMs
		{
			get { return GetInt32Settings("MinimalSplashScreenVisibleInMs", 2000);}
		}

		public static bool IsUseCustomTransaction
		{
			get { return GetBooleanSettings("IsUseCustomTransaction", false); }
		}

		public static int WorkingDbVesion
		{
			get { return GetInt32Settings("WorkingDbVesion", 0); }
		}

		public static bool IsJournalLoadWhenEmptyFilter
		{
			get { return GetBooleanSettings("JournalLoadWhenEmptyFilter", true); }
		}

		public static bool IsUseSimpleCache
		{
			get { return GetBooleanSettings("IsUseSimpleCache", true); }
		}

        public static string Title
        {
            get { return GetSettings("Title", null); }
        }

		public static string GetSettings(string name, string defaultValue)
		{
			string result = ConfigurationManager.AppSettings[name];
			if (StringUtil.IsNullOrEmpty(result))
				return defaultValue;

			return result;
		}

		public static T GetEnumSettings<T>(string name, T defaultValue)
		{
			string value = GetSettings(name, null);

			if (StringUtil.IsNullOrEmpty(value))
				return defaultValue;

			try
			{
				return (T)Enum.Parse(typeof(T), value);
			}
			catch (Exception ex)
			{
				if (Log.IsErrorEnabled)
				{
					string message = string.Format(
						"Failed to parse configured value '{1}' for {0}." +
							" Using {2} as default value.", name, value, defaultValue);
					Log.Error(message, ex);
				}
				return defaultValue;
			}
		}

		public static int GetInt32Settings(string name, int defaultValue)
		{
			return ParseHelper.ParseToInt32(ConfigurationManager.AppSettings[name], defaultValue);
		}

		public static bool GetBooleanSettings(string name, bool defaultValue)
		{
			return ParseHelper.ParseToBoolean(ConfigurationManager.AppSettings[name], defaultValue);
		}

		private static ConnectionStringSettingsCollection ConnectionStrings
		{
			get { return ConfigurationManager.ConnectionStrings; }
		}

        private static ConnectionStringSettings GetConnectionStringSilent(string connectionStringName)
        {
            ConnectionStringSettings settings = ConnectionStrings[connectionStringName];
            if (settings == null)
            {
                // Try to find secure connection string
                string settingCSCrypt = ConfigurationManager.AppSettings[string.Format("ConnectionString_{0}", connectionStringName)];
                if (settingCSCrypt == null)
                {
                    return null;
                }
                return new ConnectionStringSettings(connectionStringName, ConnectionCryptor.Decrypt(settingCSCrypt));
            }
            return settings;
        }
	}
}