using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class ManagerDiscountForm : Form
	{
        public decimal FinalPrice { get; private set; }
        private readonly decimal tariffPrice;
        private readonly decimal multiplyDiscount = 1;
		private SecurityManager.User grantor;
		private readonly DateTime startDate;
		private readonly DateTime finishDate;
		private DateTime _currentDate;

        public ManagerDiscountForm()
		{
			InitializeComponent();
            AttachTextChangedEvent(txtRatio, txtRatio_TextChanged);
            AttachTextChangedEvent(txtFinalPrice, txtFinalPrice_TextChanged);
			txtFinalPrice.Maximum = decimal.MaxValue;
        }

		internal ManagerDiscountForm(Campaign campaign) : this()
		{
			tariffPrice = campaign.TariffPrice;
			lblTariffPrice.Text = campaign.TariffPrice.ToString("c");
		    lblDiscount.Text = campaign.Discount.ToString("F");
		    lblPackDiscount.Text = campaign.PackDiscount.ToString("F");
			txtFinalPrice.Maximum = decimal.MaxValue;
            txtFinalPrice.Value = campaign.FullPrice;
		    txtRatio.Value = Math.Min(Math.Max(campaign.ManagerDiscount, 0), 1000);
		    multiplyDiscount = campaign.Discount * campaign.PackDiscount;
			startDate = campaign.StartDate;
			finishDate = campaign.FinishDate;
			chkCurrentDate.Visible = dtCurrentDate.Visible = SecurityManager.LoggedUser.IsBookKeeper || SecurityManager.LoggedUser.IsAdmin;
		}

        private void AttachTextChangedEvent(NumericUpDown numericUpDown, EventHandler handler)
        {
            foreach (Control ctrl in numericUpDown.Controls)
            {
                if (ctrl is TextBox textBox)
                {
                    textBox.TextChanged += handler;
                    break;
                }
            }
        }

		public DateTime CurrentDate
		{
			get
			{
				if (SecurityManager.LoggedUser.IsBookKeeper || SecurityManager.LoggedUser.IsAdmin)
					return _currentDate;
				return DateTime.Today;
			}
		}

		public SecurityManager.User Grantor
		{
			get { return grantor; }
		}

        private void txtRatio_TextChanged(object sender, EventArgs e)
        {
            if (ActiveControl == txtRatio && txtRatio.Text.Length > 0)
            {
                TextBox textBox = (TextBox)sender;
				if (decimal.TryParse(textBox.Text, out decimal ratioValue))
					txtFinalPrice.Value = ratioValue * multiplyDiscount * tariffPrice;
            }
        }

        private void txtFinalPrice_TextChanged(object sender, EventArgs e)
        {
            if (ActiveControl == txtFinalPrice && txtFinalPrice.Text.Length > 0)
            {
                TextBox textBox = (TextBox)sender;
                if (decimal.TryParse(textBox.Text, out decimal finalPrice))
                    txtRatio.Value = (finalPrice / (tariffPrice * multiplyDiscount));
            }
        }

        private void txtRatio_Leave(object sender, EventArgs e)
		{
			if(txtRatio.Text.Length == 0) txtRatio.Text = "0";
		}

		private void txtFinalPrice_Leave(object sender, EventArgs e)
		{
			if(txtFinalPrice.Text.Length == 0) txtFinalPrice.Text = "0";
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if(!SecurityManager.LoggedUser.IsAdmin && !SecurityManager.LoggedUser.IsAcceptRatioForUser(txtRatio.Value, startDate, finishDate))
			{
				grantor = Utils.AskConfirmation(this);
				if(grantor != null)
					FinalPrice = txtFinalPrice.Value;
				else
					DialogResult = DialogResult.None;
			}
			else
			{
				FinalPrice = txtFinalPrice.Value;
			}
			_currentDate = dtCurrentDate.Value;
		}

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
			dtCurrentDate.Enabled = chkCurrentDate.Checked;
        }
    }
}