using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using unoidl.com.sun.star.sheet;

namespace FogSoft.WinForm.Classes
{
	public static class SecurityManager
	{
		public struct ParamNames
		{
			public const string UserId = "userID";
		}

		public class User
		{
			public int Id { get; private set; }
			public string FirstName { get; private set;}
			public string LastName { get; private set; }
			public string FullName { get; private set; }
			public bool IsAdmin { get; private set; }
            public bool IsTrafficManager { get; private set; }
            public bool IsGrantor { get; private set; }
			public DataRow DataRow { get; private set;}
			public IDictionary<int, string> Groups { get; private set; }
            public string Phone { get; private set; }
            public string Email { get; private set; }
            public bool IsBookKeeper { get; private set; }
			public string LoginName { get; set; }

			public decimal GetDiscount(DateTime startDate, DateTime finishDate) 
			{
				var parameters = DataAccessor.CreateParametersDictionary();
                parameters[ParamNames.UserId] = Id;
                parameters["startDate"] = startDate;
                parameters["finishDate"] = finishDate;
                DataSet ds = DataAccessor.LoadDataSet("GetUserDiscount", parameters);
				return decimal.Parse(ds.Tables[0].Rows[0]["discount"].ToString());
            }

            internal User(DataSet ds)
			{
				DataRow = ds.Tables[0].Rows[0];
				Id = ParseHelper.ParseToInt32(DataRow[ParamNames.UserId].ToString());
				FirstName = DataRow["firstName"].ToString();
				LastName = DataRow["lastName"].ToString();
				FullName = string.Format("{0} {1}", LastName, FirstName);
				IsAdmin = ParseHelper.ParseToBoolean(DataRow["isAdmin"].ToString());
                IsTrafficManager = ParseHelper.ParseToBoolean(DataRow["IsTrafficManager"].ToString());
                IsGrantor = ParseHelper.ParseToBoolean(DataRow["isGrantor"].ToString());
                Phone = DataRow["phone"].ToString();
                Email = DataRow["email"].ToString();
                IsBookKeeper = ParseHelper.ParseToBoolean(DataRow["IsBookKeeper"].ToString());

                Groups = new Dictionary<int, string>();
				if (ds.Tables.Count > 1)
				{
					foreach (DataRow row in ds.Tables[1].Rows)
						Groups.Add(ParseHelper.ParseToInt32(row["groupID"].ToString()), row["name"].ToString());
				}
			}

			public bool IsInGroup(IDictionary<int, string> groups)
			{
				foreach (KeyValuePair<int, string> group in groups)
				{
					foreach (KeyValuePair<int, string> uGroup in Groups)
					{
						if (group.Key == uGroup.Key && group.Value == uGroup.Value)
							return true;
					}
				}
				return false;
			}

            public string ContactInfo
            {
                get
                {
                    return string.Format("{0} {1} {2} {3}", LastName, FirstName, Phone, Email);
                }
            }

			public DataTable Agencies
			{
				get
				{
					return DataAccessor.LoadDataSet("UserAgencies", DataAccessor.CreateParametersDictionary()).Tables[0];
				}
			}
        }
        
		#region Static

		private static User loggedUser;

		public static void Login(string login, string password)
		{
			loggedUser = GetUser(login, password);
			if (loggedUser != null)
                loggedUser.LoginName = login;
        }

		public static void Clear()
		{
			loggedUser = null;
		}

		public static User LoggedUser
		{
			get { return loggedUser; }
		}

		public static byte[] GetHash(string text)
		{
			MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
			byte[] needHash = Encoding.Unicode.GetBytes(text);
			return provider.ComputeHash(needHash);
		}

		public static User GetUser(string login, string password)
		{
			DataSet ds = DataAccessor.LoadDataSet("GetUserData", new Dictionary<string, object> { { "loginName", login }, { "password", GetHash(password) } });
			return ds.Tables[0].Rows.Count > 0 ? new User(ds) : null;
		}

		public static User GetUser(int userID)
		{
			DataSet ds = DataAccessor.LoadDataSet("GetUserData", new Dictionary<string, object> { { SecurityManager.ParamNames.UserId, userID }});
			return ds.Tables[0].Rows.Count > 0 ? new User(ds) : null;
		}
		#endregion
	}
}