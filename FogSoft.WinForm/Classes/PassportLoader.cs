using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Classes
{
	public static class PassportLoader
	{
		private static readonly Dictionary<string, string> passports = new Dictionary<string, string>();

		public static string Load(string codeName)
		{
			if(!passports.ContainsKey(codeName))
			{
				if (ConfigurationUtil.IsFullLoadDictionaries)
				{
					if (_dsData == null)
						FullLoadDictionaries();
					if (_dsData != null)
					{
						DataRow[] rows = _dsData.Tables[0].Select(string.Format("codeName = '{0}'", codeName));
						if (rows.Length > 0)
							passports.Add(codeName, rows[0]["passport"].ToString());
					}
				}
				else
				{
					Dictionary<string, object> parameters =
						new Dictionary<string, object>(1, StringComparer.InvariantCultureIgnoreCase);
					parameters.Add("codeName", codeName);
					DataSet ds = DataAccessor.LoadDataSet("PassportRetrieve", parameters);
					passports.Add(codeName, ds.Tables[0].Rows[0]["passport"].ToString());
				}
			}
			return passports[codeName];
		}

		#region Full Load

		private static DataSet _dsData = null;

		public static void FullLoadDictionaries()
		{
			_dsData = DataAccessor.LoadDataSet("PassportRetrieve", new Dictionary<string, object>());
		}

		#endregion

		public static void ClearHash()
		{
			passports.Clear();
		}
	}
}