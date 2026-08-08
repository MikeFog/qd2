using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Protector.Properties;
using FogSoft.WinForm.Forms;

namespace Protector.Forms
{
	public partial class ChangePasswordFrm : Form
	{
		public ChangePasswordFrm()
		{
			InitializeComponent();
		}

		private void buttonOk_Click(object sender, EventArgs e)
		{
			if (string.Compare(textBoxPassword.Text, textBoxConfirm.Text) != 0)
			{
				UserMessage.ShowExclamation(Resources.ErrorChangePassword);
				DialogResult = DialogResult.None;
			}
			else
			{
				byte[] pass = SecurityManager.GetHash(textBoxPassword.Text);
				DataAccessor.ExecuteNonQuery("AdminChangePassword", new Dictionary<string, object>{{"password", pass}});
			}
		}
	}
}