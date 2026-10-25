using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Forms;

namespace Merlin.Forms
{
	public partial class FirmPassportForm : PassportForm
	{
		public FirmPassportForm()
		{
			InitializeComponent();
		}

		public FirmPassportForm(PresentationObject presentationObject, DataSet ds)
			: base(presentationObject, ds)
		{
			InitializeComponent();
		}

		protected override void FormBuildCompleted()
		{
			// пробелы в реквизитах не нужны ни в каком виде (сервер их тоже убирает, FirmIUD)
			foreach (string name in RequisiteNames)
			{
				TextBox textBox = FindControl(name) as TextBox;
				if (textBox != null)
					textBox.Validating += (sender, e) => RemoveWhiteSpace((TextBox)sender);
			}
		}

		private static readonly string[] RequisiteNames = { "inn", "kpp", "egrn", "account" };

		private static string WithoutWhiteSpace(string text)
		{
			return Regex.Replace(text, @"[\s ]+", string.Empty);
		}

		private static void RemoveWhiteSpace(TextBox textBox)
		{
			string clean = WithoutWhiteSpace(textBox.Text);
			if (clean != textBox.Text)
				textBox.Text = clean;
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			bool needSave = true;
			foreach (string name in RequisiteNames)
			{
				TextBox textBox = FindControl(name) as TextBox;
				if (textBox != null)
					RemoveWhiteSpace(textBox);
			}
			TextBox tbINN = FindControl("inn") as TextBox;
			if (!StringUtil.IsNullOrEmpty(tbINN.Text))
			{
				needSave = CheckINN(tbINN);
			}
			if (needSave)
				base.ApplyChanges(clickedButton);
			else
				DialogResult = DialogResult.None;
		}

		private bool CheckINN(TextBox tbINN)
		{
			Match match = Regex.Match(WithoutWhiteSpace(tbINN.Text), @"^(\d)+");
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters["inn"] = match.Value;
			if (!pageContext.PresentationObject.IsNew && pageContext.PresentationObject.IDs.Length > 0)
				parameters["firmID"] = pageContext.PresentationObject.IDs[0];
			DataSet ds = DataAccessor.LoadDataSet("sp_CheckFirmINN", parameters);
			DataTable table = ds.Tables[0];
			if (table.Rows.Count <= 0)
				return true;
			StringBuilder firmNames = new StringBuilder();
			foreach (DataRow row in table.Rows)
			{
				if (firmNames.Length > 0)
					firmNames.Append(", ");
				firmNames.Append(row["name"]);
			}
			string frmNames = firmNames.ToString();
			if (frmNames.Length > 1024)
				frmNames = frmNames.Substring(0, 1024) + "...";
			Dictionary<string, object> messageParams = new Dictionary<string, object>();
			string firmsDouble = frmNames;
			messageParams["firms"] = firmsDouble;
			bool fContinue = (Globals.ShowQuestion("FirmInnUnique", messageParams) == DialogResult.Yes);
			if (fContinue)
				SayToAdminFirmDoubleINN(firmsDouble);
			return fContinue;
		}

		private void SayToAdminFirmDoubleINN(string firmsDouble)
		{
			TextBox tbName = FindControl("name") as TextBox;
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters["firmName"] = tbName == null ? string.Empty : tbName.Text;
			parameters["firmsDouble"] = firmsDouble;
			parameters[SecurityManager.ParamNames.UserId] = SecurityManager.LoggedUser.Id;
			DataAccessor.ExecuteNonQuery("sp_SayAdminThatFirmInnDouble", parameters);
		}
	}
}