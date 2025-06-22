namespace MetadataEditor
{
	public sealed class Settings
	{
		private const string CONNECTION_KEY = "ConnString";

		/// <summary>
		/// Get connection string from app.config.</summary>
		public static string ConnectionString
		{
			get
			{
				return System.Configuration.ConfigurationSettings.AppSettings[CONNECTION_KEY];
			}
		}
	}
}
