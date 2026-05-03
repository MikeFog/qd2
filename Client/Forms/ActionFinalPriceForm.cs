using DocumentFormat.OpenXml.Office2010.Excel;
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
        public int? ManagerDiscountReasonId { get; private set; }

        public DateTime SelectedDate { get; private set; }

        public ActionFinalPriceForm()
        {
            InitializeComponent();
            AttachTextChangedEvent(txtRatio, txtRatio_ValueChanged);
            //chkCurrentDate.Visible = dtCurrentDate.Visible = SecurityManager.LoggedUser.IsBookKeeper || SecurityManager.LoggedUser.IsAdmin;
            txtFinalPrice.Maximum = decimal.MaxValue;
        }

        public ActionFinalPriceForm(ActionOnMassmedia action) : this()
        {
            txtFinalPrice.Value = action.TotalPrice;
            txtRatio.Value = 0;
            _action = action;
            _campaignsTable = action.Campaigns();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!(SecurityManager.LoggedUser.IsBookKeeper || SecurityManager.LoggedUser.IsAdmin))
                Utils.HideTableLayoutRow(tableLayoutPanelMain, 2);
            if (SecurityManager.LoggedUser.Id != _action.UserID)
            {
                Entity entity = EntityManager.GetEntity((int)Entities.ManagerDiscountReason);
                entity.ClearCache();
                DataTable dataTable = entity.GetContent().Copy();
                DataRow row = dataTable.NewRow();
                row[0] = 0;
                row[1] = "";
                dataTable.Rows.InsertAt(row, 0);
                cmbReason.ColumnWithID = "ManagerDiscountReasonId";
                cmbReason.DataSource = dataTable.DefaultView;
            }
            else
                Utils.HideTableLayoutRow(tableLayoutPanelMain, 3);
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
            if (!VerifyInput())
            {
                DialogResult = DialogResult.None;
                return;
            }

            IsManagerDiscount = rbRatio.Checked;
            ManagerDiscount = txtRatio.Value;
            FinalPrice = txtFinalPrice.Value;
            SelectedDate = dtCurrentDate.Enabled && dtCurrentDate.Visible ? dtCurrentDate.Value : DateTime.Today;
            ManagerDiscountReasonId = cmbReason.SelectedValue != null
                ? (int?)Convert.ToInt32(cmbReason.SelectedValue)
                : null;
        }

        public bool VerifyInput()
        {
            if (cmbReason.Visible && Convert.ToInt32(cmbReason.SelectedValue) == 0)
            {
                MessageBox.Show("Необходимо выбрать причину выдачи скидки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
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
