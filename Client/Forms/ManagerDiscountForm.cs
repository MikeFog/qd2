using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class ManagerDiscountForm : Form
	{
        public decimal FinalPrice { get; private set; }
        public int? ManagerDiscountReasonId { get; private set; }
        private readonly decimal tariffPrice;
        private readonly decimal multiplyDiscount = 1;
		private SecurityManager.User grantor;
		private readonly DateTime startDate;
		private readonly DateTime finishDate;
		private DateTime _currentDate;
        private readonly Campaign _campaign;


        public ManagerDiscountForm()
		{
			InitializeComponent();
            AttachTextChangedEvent(txtRatio, txtRatio_TextChanged);
            AttachTextChangedEvent(txtFinalPrice, txtFinalPrice_TextChanged);
			txtFinalPrice.Maximum = decimal.MaxValue;
        }

		internal ManagerDiscountForm(Campaign campaign) : this()
		{
            _campaign = campaign;
            tariffPrice = campaign.TariffPrice;
            multiplyDiscount = _campaign.Discount * _campaign.PackDiscount;
            startDate = _campaign.StartDate;
            finishDate = _campaign.FinishDate;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            lblTariffPrice.Text = _campaign.TariffPrice.ToString("c");
            lblDiscount.Text = _campaign.Discount.ToString("F");
            lblPackDiscount.Text = _campaign.PackDiscount.ToString("F");
            txtFinalPrice.Maximum = decimal.MaxValue;
            txtFinalPrice.Value = _campaign.FullPrice;
            txtRatio.Value = Math.Min(Math.Max(_campaign.ManagerDiscount, 0), 1000);
            if (!(SecurityManager.LoggedUser.IsBookKeeper || SecurityManager.LoggedUser.IsAdmin))
                Utils.HideTableLayoutRow(tableLayoutPanelMain, 6);
            //chkCurrentDate.Visible = dtCurrentDate.Visible = SecurityManager.LoggedUser.IsBookKeeper || SecurityManager.LoggedUser.IsAdmin;
            if (SecurityManager.LoggedUser.IsAdmin)
            {
                Entity entity = EntityManager.GetEntity((int)Entities.ManagerDiscountReason);
                entity.ClearCache();
                cmbReason.ColumnWithID = "ManagerDiscountReasonId";
                cmbReason.DataSource = entity.GetContent().Copy().DefaultView;
            }
            else
                Utils.HideTableLayoutRow(tableLayoutPanelMain, 5);
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
            ManagerDiscountReasonId = cmbReason.SelectedValue != null
                ? (int?)Convert.ToInt32(cmbReason.SelectedValue)
                : null;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
			dtCurrentDate.Enabled = chkCurrentDate.Checked;
        }
    }
}