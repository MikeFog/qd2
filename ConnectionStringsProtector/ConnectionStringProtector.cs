using System;
using System.Configuration;

namespace ConnectionStringsProtector
{
	internal enum ConfigurationFileType
	{
		Web,
		WinForms
	}

	internal class ConnectionStringProtector
	{
		public static void ProtectConnectionString(string filePath, string connectionName)
		{
			ToggleConnectionStringProtection(filePath, true, connectionName, ConfigurationFileType.WinForms);
		}

		public static void UnprotectConnectionString(string filePath, string connectionName)
		{
			ToggleConnectionStringProtection(filePath, false, connectionName, ConfigurationFileType.WinForms);
		}

		public static void ToggleConnectionStringProtection(string pathName, bool protect, string connectionName, ConfigurationFileType fileType)
		{
			try
			{
				Configuration oConfiguration = fileType == ConfigurationFileType.Web ? System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(pathName) 
						: ConfigurationManager.OpenExeConfiguration(pathName);

				if (oConfiguration != null)
				{
					ConnectionStringsSection oSection = oConfiguration.GetSection("connectionStrings") as ConnectionStringsSection;
					if (oSection != null)
					{
						string keyConnectionStringProtected = string.Format("ConnectionString_{0}", connectionName);
						if (protect)
						{
							if (oSection.ConnectionStrings[connectionName] != null)
							{
								string connectionString = oSection.ConnectionStrings[connectionName].ConnectionString;
								oSection.ConnectionStrings.Remove(connectionName);
								oConfiguration.AppSettings.Settings.Add(keyConnectionStringProtected,
								                                        ConnectionCryptor.Crypt(connectionString));
							}
						}
						else
						{
							if (oConfiguration.AppSettings.Settings[keyConnectionStringProtected] != null)
							{
								string crypt =
									oConfiguration.AppSettings.Settings[keyConnectionStringProtected].Value;
								if (!string.IsNullOrEmpty(crypt))
								{
									oSection.ConnectionStrings.Add(new ConnectionStringSettings(connectionName, ConnectionCryptor.Decrypt(crypt)));
								}
								oConfiguration.AppSettings.Settings.Remove(keyConnectionStringProtected);
							}
						}
					}
					if (oSection != null)
					{
						oSection.SectionInformation.ForceSave = true;
						oConfiguration.Save();
					}
				}
			}
			catch (Exception ex)
			{
				throw (ex);
			}
		}
	}
}
