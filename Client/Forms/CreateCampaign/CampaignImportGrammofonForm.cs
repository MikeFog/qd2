using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using Merlin.Classes.Exchange.Grammofon;
using Merlin.Properties;

namespace Merlin.Forms.CreateCampaign
{
	internal partial class CampaignImportGrammofonForm : Form
	{
		private readonly CampaignPassportFormBaseController controller;

		public CampaignImportGrammofonForm()
		{
			InitializeComponent();
			controller = new CampaignPassportFormBaseController(this);
		}

		private void CampaignImportGrammofonForm_Load(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				controller.Init(null, cmbPaymentType, lookUpRolType, grdAgency, grdMassmedia);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
			controller.OnCheckOkButton += CheckOkButton;
		}

		private void CheckOkButton()
		{
			btnOk.Enabled =
				cmbPaymentType.SelectedValue != null &&
				(grdMassmedia.SelectedObject != null || !grdMassmedia.Enabled)
				&& SelectedSheetIndex >= 0;
		}

		private void btnFilePath_Click(object sender, EventArgs e)
		{
			openFileDialog.FileName = textBoxFilePath.Text;
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				textBoxFilePath.Text = openFileDialog.FileName;
			}
		}

		internal Import ImportData { get; private set; }

		private void btnLoad_Click(object sender, EventArgs e)
		{
			if (!File.Exists(textBoxFilePath.Text))
				FogSoft.WinForm.Forms.MessageBox.ShowExclamation("Файл не найден.");

			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ImportData = new Import();

				bool isLoaded = (bool)ProgressForm.Show(this, DoLoad, "Загружается...", null);
				
				InitSheets();

				CheckOkButton();

				if (!isLoaded)
				{
					ImportData = null;
					FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Resources.CannotReadImportFile);
					return;
				}
			}
			catch (Exception exp)
			{
				ErrorManager.PublishError(exp);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void DoLoad(object sender, DoWorkEventArgs e)
		{
			e.Result = ImportData.LoadData(textBoxFilePath.Text);
		}

		private void InitSheets()
		{
			cbSheets.Enabled = false;
			cbSheets.Items.Clear();
			if (ImportData.Sheets != null && ImportData.Sheets.Count > 0)
			{
				foreach (KeyValuePair<int, Import.IssuesData> pair in ImportData.Sheets)
					cbSheets.Items.Add(pair.Value.Name);
				cbSheets.Enabled = true;
				cbSheets.SelectedIndex = 0;
			}
		}

		public int PaymentTypeID
		{
			get { return controller.PaymentTypeID; }
		}

		public Agency Agency
		{
			get { return controller.Agency; }
		}

		public Massmedia Massmedia
		{
			get { return controller.Massmedia; }
		}

		public int SelectedSheetIndex
		{
			get { return cbSheets.SelectedIndex; }
		}
    }
}