using System;
using System.Collections.Generic;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	static class AdvertAgUser
	{
		public static bool IsRightToEditForeignActions(this SecurityManager.User user)
		{
			return ParseHelper.ParseToBoolean(user.DataRow["isRightToEditForeignActions"].ToString());
		}

		public static bool IsRightToEditGroupActions(this SecurityManager.User user)
		{
			return ParseHelper.ParseToBoolean(user.DataRow["isRightToEditGroupActions"].ToString());
		}

		public static bool IsRightToViewForeignActions(this SecurityManager.User user)
		{
			return ParseHelper.ParseToBoolean(user.DataRow["isRightToViewForeignActions"].ToString());
		}

		public static bool IsRightToViewGroupActions(this SecurityManager.User user)
		{
			return ParseHelper.ParseToBoolean(user.DataRow["isRightToViewGroupActions"].ToString());
		}

		public static bool IsRightToEditForeignSOActions(this SecurityManager.User user)
		{
			return ParseHelper.ParseToBoolean(user.DataRow["isRightToEditForeignSOActions"].ToString());
		}

		public static bool IsRightToEditGroupSOActions(this SecurityManager.User user)
		{
			return ParseHelper.ParseToBoolean(user.DataRow["isRightToEditGroupSOActions"].ToString());
		}

		public static bool IsRightToViewForeignSOActions(this SecurityManager.User user)
		{
			return ParseHelper.ParseToBoolean(user.DataRow["isRightToViewForeignSOActions"].ToString());
		}

		public static bool IsRightToViewGroupSOActions(this SecurityManager.User user)
		{
			return ParseHelper.ParseToBoolean(user.DataRow["isRightToViewGroupSOActions"].ToString());
		}

		public static bool IsAcceptRatioForUser(this SecurityManager.User user, decimal ratio, DateTime startDate, DateTime finishDate)
		{
			Dictionary<string, object> parametersDictionary = DataAccessor.CreateParametersDictionary();
			parametersDictionary.Add(SecurityManager.ParamNames.UserId, user.Id);
			parametersDictionary.Add("ratio", ratio);
			parametersDictionary.Add("startDate", startDate);
			parametersDictionary.Add("finishDate", finishDate);
			return (bool) DataAccessor.ExecuteScalar("CheckRatioForUser", parametersDictionary);
		}
	}
}
