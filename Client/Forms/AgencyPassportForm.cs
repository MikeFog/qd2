using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class AgencyPassportForm : PassportForm
	{
		private bool _isEventsCreated = false;
		private readonly Agency _agency;
		private ObjectPicker2 _bankPicker;
		private TextBox _tbAccount;

		private const string AgencyBankTable = "agencyBank";

		public AgencyPassportForm(Agency ag, DataSet ds) : base(ag, ds)
		{
			InitializeComponent();
			_agency = ag;
		}

		// Расчетный счет — счет агентства в выбранном банке (AgencyBank, agencyPassport).
		// При смене банка в поле встает сохраненный счет этого банка; введенное для прежнего
		// банка теряется (решение Миши 03.11.2026). Пустой счет при выбранном банке
		// отклоняет AgencyIUD (AgencyBankAccountRequired).
		protected override void FormBuildCompleted()
		{
			_bankPicker = FindControl(Organization.ParamNames.BankId) as ObjectPicker2;
			_tbAccount = FindControl(Organization.ParamNames.Account) as TextBox;
			if (_bankPicker == null || _tbAccount == null)
				return;

			// колонки в таблице Agency больше нет — длину берем у AgencyBank.account
			_tbAccount.MaxLength = 32;
			_bankPicker.ObjectSelected += bank => _tbAccount.Text = FindAccountRow(bank.IDs[0])?[Organization.ParamNames.Account].ToString() ?? string.Empty;
			_bankPicker.SelectionCleared += () => _tbAccount.Text = string.Empty;
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			base.ApplyChanges(clickedButton);

			// «Применить» прошло (кнопка погасла) — форма остается открытой, и сохраненный
			// счет нужен ей при возврате к этому банку
			if (clickedButton == btnApply && !btnApply.Enabled && _bankPicker?.SelectedObject != null)
				RememberAccount(_bankPicker.SelectedObject.IDs[0], _tbAccount.Text.Trim());
		}

		private DataRow FindAccountRow(object bankId)
		{
			DataTable accounts = pageContext.DataSource?.Tables[AgencyBankTable];
			if (accounts != null)
				foreach (DataRow row in accounts.Rows)
					if (Convert.ToInt32(row[Organization.ParamNames.BankId]) == Convert.ToInt32(bankId))
						return row;
			return null;
		}

		private void RememberAccount(object bankId, string account)
		{
			DataRow row = FindAccountRow(bankId);
			if (row == null)
			{
				DataTable accounts = pageContext.DataSource?.Tables[AgencyBankTable];
				if (accounts == null)
					return;
				row = accounts.NewRow();
				row[Organization.ParamNames.BankId] = bankId;
				accounts.Rows.Add(row);
			}
			row[Organization.ParamNames.Account] = account;
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			if (_isEventsCreated) return;

			foreach (PageControl ctl in pages[pages.Count-1].PageControls)
			{
                if (ctl is PageFieldImage ctlImage)
                {
                    ctlImage.SetImage(_agency.Signature);
                }
            }

			_isEventsCreated = true;
		}
	}
}