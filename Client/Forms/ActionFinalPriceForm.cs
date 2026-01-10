using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System;
using System.Data;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class ActionFinalPriceForm : Form
    {
        private readonly ActionOnMassmedia _action;
        private readonly DataTable _campaignsTable;
        public decimal FinalPrice { get; private set; }
        public decimal ManagerDiscount { get; private set; }
        public bool IsManagerDiscount { get; private set; }

        public DateTime SelectedDate { get; private set; }

        public ActionFinalPriceForm()
        {
            InitializeComponent();
            AttachTextChangedEvent(txtRatio, txtRatio_ValueChanged);
            chkCurrentDate.Visible = dtCurrentDate.Visible = SecurityManager.LoggedUser.IsBookKeeper || SecurityManager.LoggedUser.IsAdmin;
            txtFinalPrice.Maximum = decimal.MaxValue;
        }

        public ActionFinalPriceForm(ActionOnMassmedia action) : this()
        {
            txtFinalPrice.Value = action.TotalPrice;
            txtRatio.Value = 0;
            _action = action;
            _campaignsTable = action.Campaigns();
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

        private void BtnOK_Click(object sender, EventArgs e)
        {
            IsManagerDiscount = rbRatio.Checked;
            ManagerDiscount = txtRatio.Value;
            FinalPrice = txtFinalPrice.Value;
            SelectedDate = dtCurrentDate.Enabled && dtCurrentDate.Visible ? dtCurrentDate.Value : DateTime.Today;
        }

        private void chkCurrentDate_CheckedChanged(object sender, EventArgs e)
        {
            dtCurrentDate.Enabled = chkCurrentDate.Checked;
        }

        private void txtRatio_ValueChanged(object sender, EventArgs e)
        {
            if (ActiveControl == txtRatio && txtRatio.Text.Length > 0 && _action != null)
            {
                TextBox textBox = (TextBox)sender;
                if (decimal.TryParse(textBox.Text, out decimal ratioValue))
                    txtFinalPrice.Value = CalculateFinalPrice(ratioValue);
            }
        }

        private void CheckedChanged(object sender, EventArgs e)
        {
            txtFinalPrice.Enabled = radioButton2.Checked;
            txtRatio.Enabled = rbRatio.Checked;
        }

        private decimal CalculateFinalPrice(decimal ratio)
        {
            decimal finalPrice = 0;
            foreach (DataRow row in _campaignsTable.Rows)
            {
                Campaign campaign = new Campaign(row);
                finalPrice += campaign.Discount * campaign.PackDiscount * campaign.TariffPrice * ratio;
            }
            return finalPrice;
        }
    }
}
