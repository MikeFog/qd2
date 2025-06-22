using System;
using System.ComponentModel;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Properties;
using Microsoft.Win32;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace FogSoft.WinForm.Controls
{
	public class LoginEventArgs
	{
		public LoginEventArgs(LoginCtl.LoginRes res)
		{
			Res = res;
		}

		public LoginEventArgs(LoginCtl.LoginRes res, string err)
		{
			Res = res;
			ErrorMsg = err;
		}

		public LoginCtl.LoginRes Res {get;set;}
		public string ErrorMsg { get; set; }
	}

	public class SuccesLoginEventArgs
	{
		private readonly BackgroundWorker worker;

		public SuccesLoginEventArgs(BackgroundWorker worker)
		{
			this.worker = worker;
		}

		public void ReportProgress(string text)
		{
			worker.ReportProgress(0, text);
		}
	}

	public class LoginCtl
	{
		public enum LoginRes
		{
			Ok,
			Error,
			MaxTries
		}

        private const int MaxLoginTries = 3;

		private int loginTries;

		public LoginEventArgs Login(string user, string password)
		{
			return Login(user, password, false);
		}

		public LoginEventArgs Login(string user, string password, bool quiet)
		{
			try
			{
				SecurityManager.Login(user, password);
			}
			catch(Exception exp)
			{
				ErrorManager.LogError("LoginCtl: Login", exp);
				return new LoginEventArgs(LoginRes.Error, string.Format(Resources.CannotCheckUser, Environment.NewLine));
			}

			if (SecurityManager.LoggedUser != null)
			{
				SaveLastLoggedUser(user);
				return new LoginEventArgs(LoginRes.Ok);
			}
			
			if (!quiet)
				MessageBox.ShowExclamation(string.Format(Resources.ErrorLogin, Environment.NewLine));

			loginTries++;
			return new LoginEventArgs((loginTries < MaxLoginTries) ? LoginRes.Error : LoginRes.MaxTries, string.Format(Resources.ErrorLogin, Environment.NewLine));
		}

		public void GetRegUserSaves(out string user, out string password, out bool autologin)
		{
			autologin = false;
			user = string.Empty;
			password = string.Empty;
			try
			{
				RegistryKey currentUser = Registry.CurrentUser;
				RegistryKey key = currentUser.CreateSubKey(ConfigurationUtil.RegUserSavePath);
				if (key != null)
				{
					user = key.GetValue("user") as string;
					password = key.GetValue("password") as string;
					string auto = key.GetValue("autologin") as string;
					if (!string.IsNullOrEmpty(auto))
						autologin = ParseHelper.ParseToInt32(auto, 0) != 0;
				}
			}
			catch (Exception exp)
			{
				ErrorManager.LogError("Try to Load Reg User Data", exp);
			}
		}

		public void SaveLastLoggedUser(string user)
		{
			if (!ConfigurationUtil.IsRegUserSave)
				return;

			try
			{
				RegistryKey currentUser = Registry.CurrentUser;
				RegistryKey key = currentUser.CreateSubKey(ConfigurationUtil.RegUserSavePath);
				if (key != null)
					key.SetValue("user", user);
			}
			catch (Exception e)
			{
				ErrorManager.LogError("Try to save last logged user", e);
			}
		}
	}
}
