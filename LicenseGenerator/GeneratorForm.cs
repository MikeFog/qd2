using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FogSoft.LicenseGenerator.Properties;

namespace FogSoft.LicenseGenerator
{
	public partial class GeneratorForm : Form, IProgress
	{
		private string _keyFile;

		public GeneratorForm()
		{
			InitializeComponent();
			Settings settings = new Settings();
			openTemplateDialog.InitialDirectory = settings.TemplatePath;
			openLicenseDialog.InitialDirectory = settings.LicensePath;
			_keyFile = settings.KeyPath;
		}

		public void StepCompleted()
		{
			progressBar.PerformStep();
		}

		public void WorkCompleted()
		{
			progressBar.Value = progressBar.Maximum;
			Thread.Sleep(200);
			Application.DoEvents();
			Thread.Sleep(200);
			progressBar.Value = progressBar.Minimum;
		}


		private bool CheckPath(string sourceFile, string destinationFile)
		{
			if (!File.Exists(_keyFile))
			{
				MessageBox.Show(this, "Key file does not exists.\n"
					+ "Please, provide a valid path to key file within FogSoft.LicenseGenerator.exe.config.\n"
					+ "You can do this, by changing setting with name 'KeyPath'.", Text, MessageBoxButtons.OK,
								MessageBoxIcon.Warning);
				return false;
			}

			if (!File.Exists(sourceFile))
			{
				MessageBox.Show(this, "Input file does not exists.", Text, MessageBoxButtons.OK,
				                MessageBoxIcon.Warning);
				return false;
			}

			if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
			{
				MessageBox.Show(this, "Output directory does not exists.", Text, MessageBoxButtons.OK,
				                MessageBoxIcon.Warning);
				return false;
			}
			if (File.Exists(destinationFile))
			{
				if (MessageBox.Show(this, "Do you want to override existing file?", Text,
				                    MessageBoxButtons.OKCancel,
				                    MessageBoxIcon.Question) == DialogResult.OK)
					return true;
				return false;
			}
			return true;
		}

		private void SaveSettings()
		{
			Settings settings = new Settings();
			if (templatePath.Text.Length > 0)
				settings.TemplatePath = Path.GetDirectoryName(templatePath.Text);
			if (licensePath.Text.Length > 0)
				settings.LicensePath = Path.GetDirectoryName(licensePath.Text);
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void browseTemplate_Click(object sender, EventArgs e)
		{
			if (openTemplateDialog.ShowDialog(this) == DialogResult.OK)
			{
				templatePath.Text = openTemplateDialog.FileName;
				if (licensePath.Text.Length == 0)
					licensePath.Text = Path.ChangeExtension(templatePath.Text, "lic");
			}
		}

		private void browseLicense_Click(object sender, EventArgs e)
		{
			if (openLicenseDialog.ShowDialog(this) == DialogResult.OK)
			{
				licensePath.Text = openLicenseDialog.FileName;
				if (templatePath.Text.Length == 0)
					templatePath.Text = Path.ChangeExtension(licensePath.Text, "ltmpl");
			}
		}

		private void generateLicense_Click(object sender, EventArgs e)
		{
			if (!CheckPath(templatePath.Text, licensePath.Text))
				return;
			SaveSettings();

			new LicenseWriter(_keyFile, this).WriteLicense(templatePath.Text, licensePath.Text);
		}

		private void restoreTemplate_Click(object sender, EventArgs e)
		{
			StepCount = 5;
			if (!CheckPath(licensePath.Text, templatePath.Text))
				return;
			SaveSettings();
			StepCompleted();

			string license = new LicenseReader(_keyFile).ReadLicenseFromFile(licensePath.Text);
			StepCompleted();

			File.WriteAllText(templatePath.Text, license, KeyManager.Encoding);
			WorkCompleted();
		}

		public int StepCount
		{
			get { return progressBar.Maximum; }
			set { progressBar.Maximum = value; }
		}
	}
}