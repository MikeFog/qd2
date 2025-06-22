using System;
using System.Collections.Generic;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.License;
using Protector.Properties;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Protector.License
{
	public class AdvertAgLicence : License<AdvertAgLicence>
	{
		public int? MassmediasCount { get; set; }

		protected override void Init(string content)
		{
			base.Init(content);

			ColonLinesReader reader = new ColonLinesReader();
			Dictionary<string, string> properties = reader.GetProperties(content);
			foreach (KeyValuePair<string, string> keyValuePair in properties)
			{
				if (string.Compare(keyValuePair.Key, "MassmediasCount") == 0)
					MassmediasCount = string.Compare(keyValuePair.Value, NULLVALUE) == 0
					                  	? null
					                  	: (int?) ParseHelper.ParseToInt32(keyValuePair.Value);
			}
		}

		public override string Check()
		{
			string errMsg = base.Check();
			if (!string.IsNullOrEmpty(errMsg))
				return errMsg;
			if (string.Compare(Product, Globals.ApplicationName) != 0)
				return string.Format(Resources.LicenseErrorProduct, Environment.NewLine);
			if (MassmediasCount.HasValue)
			{
				int massmediasCount = GetMassmediasCount();
				if (massmediasCount > MassmediasCount.Value)
					return string.Format(Resources.LicenseErrorMassmediaCount, Environment.NewLine);
			}
			return null;
		}

		public int GetMassmediasCount()
		{
			return ParseHelper.GetInt32FromObject(DataAccessor.LoadDataSet("massmediaList", new Dictionary<string, object>()).Tables[0].Rows.Count, MassmediasCount.Value + 1);
		}

		public static bool CheckLicenseUsersCountForAdd()
		{
			AdvertAgLicence licence = GetLicence();
			if (licence != null)
			{
				string checkLicense = licence.Check();
				if (!string.IsNullOrEmpty(checkLicense))
				{
					MessageBox.ShowExclamation(checkLicense);
					return false;
				}

				if (licence.UsersCount.HasValue && licence.GetUsersCount() + 1 > licence.UsersCount.Value)
				{
					MessageBox.ShowExclamation(Resources.LicenseErrorCannotAddUser);
					return false;
				}
				return true;
			}

			return false;
		}
	}
}