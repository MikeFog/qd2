using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Protector.License;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Protector.Forms
{
	public partial class LicenseLoader : Form
	{
		public LicenseLoader()
		{
			InitializeComponent();
			LoadLicence();
		}

		private void LoadLicence()
		{
			try
			{
				AdvertAgLicence licence = AdvertAgLicence.GetLicence();
				if (licence != null)
				{
					StringBuilder builder = new StringBuilder();
					builder.AppendFormat("Выдана на компанию: {0} {1}", licence.Company, Environment.NewLine);
					builder.AppendFormat("Срок действия лицензии: {0} {1}", licence.Expired.HasValue ? licence.Expired.Value.ToString("dd.MM.yyyy") : "Неограничен", Environment.NewLine);
					builder.AppendFormat("Максимальное количество разрешенных пользователей: {0} {1}", licence.UsersCount.HasValue ? licence.UsersCount.Value.ToString() : "Неограничено", Environment.NewLine);
					builder.AppendFormat("Максимальное количество разрешенных СМИ: {0} {1}", licence.MassmediasCount.HasValue ? licence.MassmediasCount.Value.ToString() : "Неограничено", Environment.NewLine);
					string res = licence.Check();
					if (!string.IsNullOrEmpty(res))
						builder.AppendFormat("Ошибка в лицензии: {0}", res);
					lblLicence.Text = builder.ToString();
				}
			}
			catch(Exception e)
			{
				ErrorManager.LogError("Error get licence", e);
			}
		}

		private void btnLoadLicense_Click(object sender, EventArgs e)
		{
			if (!File.Exists(textBoxLicensePath.Text))
				Globals.ShowExclamation("LicenceFileNotFind");
			else
			{
				try
				{
					string encodedLicense = File.ReadAllText(textBoxLicensePath.Text, AdvertAgLicence.Encoding);
					AdvertAgLicence licence = AdvertAgLicence.GetLicence(encodedLicense);
					if (licence == null)
						Globals.ShowExclamation("LicenceFileError");
					else
					{
						string res = licence.Check();
						if (!string.IsNullOrEmpty(res))
						{
							MessageBox.ShowExclamation(res);
						}
						else
						{
							byte[] licenceBytes = Convert.FromBase64String(encodedLicense);
							AdvertAgLicence.SaveLicence(licenceBytes, encodedLicense.Length);
							LoadLicence();
						}
					}
				}
				catch (Exception exp)
				{
					ErrorManager.LogError("Load licence", exp);
					Globals.ShowExclamation("LicenceFileError");
				}
			}
		}

		private void textBoxLicensePath_TextChanged(object sender, EventArgs e)
		{
			btnLoadLicense.Enabled = textBoxLicensePath.Text.Length > 0;
		}

		private void btnPathLicense_Click(object sender, EventArgs e)
		{
			if (frmBrowseLicence.ShowDialog(this) == DialogResult.OK)
			{
				textBoxLicensePath.Text = frmBrowseLicence.FileName;
			}
		}
	}
}