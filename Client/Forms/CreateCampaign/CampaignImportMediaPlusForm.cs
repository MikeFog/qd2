using System;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin.Classes.Exchange.MediaPlus;
using Merlin.Properties;

namespace Merlin.Forms.CreateCampaign
{
	public partial class CampaignImportMediaPlusForm : Form
	{
		private readonly CampaignPassportFormBaseController controller;

		public CampaignImportMediaPlusForm()
		{
			InitializeComponent();
			controller = new CampaignPassportFormBaseController(this);
		}

		private void CampaignImportForm_Load(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				controller.Init(null, cmbPaymentType, null, grdAgency, null);
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
				ParseHelper.GetInt32FromObject(cmbPaymentType.SelectedValue, 0) > 0
				&& !string.IsNullOrEmpty(textBoxFilePath.Text) && ImportData != null && ImportData.Massmedia != null;
		}
        
		private void btnFilePath_Click(object sender, EventArgs e)
		{
			openFileDialog.FileName = textBoxFilePath.Text;
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				textBoxFilePath.Text = openFileDialog.FileName;
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

		internal Import ImportData { get; private set; }

		private void btnLoad_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ImportData = new Import();

				bool isLoaded = ImportData.LoadData(textBoxFilePath.Text);

				lblMassmedia.Text = (isLoaded && ImportData.Massmedia != null) ? ImportData.Massmedia.Name : "Радиостанция не найдена";
				grdAgency.DataSource = (isLoaded && ImportData.Massmedia != null) ? ImportData.Massmedia.Agencies.DefaultView : null;

				CheckOkButton();

				if (!isLoaded)
				{
					ImportData = null;
					FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Resources.CannotReadImportFile);
					return;
				}
			}
			catch(Exception exp)
			{
				ErrorManager.PublishError(exp);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}
	}
}