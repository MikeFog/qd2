using System.Collections.Generic;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
    /// <summary>
    /// Provides access to per-user settings stored in the database (UserSetting table).
    /// </summary>
    public static class UserSettings
    {
        /// <summary>
        /// Loads a setting value for the currently logged-in user.
        /// Returns <c>null</c> when the setting does not exist.
        /// </summary>
        public static string Load(string settingName)
        {
            int userID = SecurityManager.LoggedUser.Id;
            var parameters = new Dictionary<string, object>
            {
                { "userID",      userID },
                { "settingName", settingName }
            };
            var ds = DataAccessor.LoadDataSet("UserSettingRetrieve", parameters);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                return ds.Tables[0].Rows[0]["settingValue"]?.ToString();
            return null;
        }

        /// <summary>
        /// Saves (inserts or updates) a setting value for the currently logged-in user.
        /// </summary>
        public static void Save(string settingName, string value)
        {
            int userID = SecurityManager.LoggedUser.Id;
            var parameters = new Dictionary<string, object>
            {
                { "userID",        userID },
                { "settingName",   settingName },
                { "settingValue",  value }
            };
            DataAccessor.ExecuteNonQuery("UserSettingIUD", parameters);
        }
    }
}
