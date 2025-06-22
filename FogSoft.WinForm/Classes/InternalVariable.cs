using System;
using System.Collections.Generic;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Classes
{
	internal class InternalVariable
	{
		private static bool CheckDBVersion(int version)
		{
			try
			{
				Dictionary<string, object> parameters = new Dictionary<string, object> {{"version", version}};
				return ParseHelper.ParseToBoolean(DataAccessor.ExecuteScalar("sp_DBVersion", parameters).ToString(), false);
			}
			catch (Exception e)
			{
				ErrorManager.LogError("InternalVariable : CheckDBVersion()", e);
				return false;
			}
		}

		public static bool CheckDBVersion()
		{
			return CheckDBVersion(Globals.DBVersion);
		}
	}

}