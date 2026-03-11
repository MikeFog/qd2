using System;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class FrmTemplate : Form
	{
		private readonly IssueTemplate _template;

		public FrmTemplate(IssueTemplate template)
		{
			this._template = template ?? new IssueTemplate();
			this._template.Mode = IssueTemplateMode.WeekDays;
			InitializeComponent();
		}

		public FrmTemplate() : this(null)
		{
		}

        public IssueTemplate Template
        {
            get
            {
                _template.StartDate = dtStartDate.Value;
                _template.FinishDate = dtFinishDate.Value;
                _template.IsOdd = rbOdd.Checked;
                _template.Mode = (rbNumber.Checked) ? IssueTemplateMode.OddEvenDays : IssueTemplateMode.WeekDays;
                return _template;
            }
        }

        private static void SetDateTimeValue(DateTimePicker control, DateTime date)
		{
			if (control.MinDate < date && control.MaxDate > date)
				control.Value = date;
		}

		private void FrmTemplate_Load(object sender, EventArgs e)
		{
			clbWeekDays.Items.Clear();
			for (	int i = 0; i < DateTimeUtils.WeekDayNames.Length; i++)
			{
				clbWeekDays.Items.Add(DateTimeUtils.WeekDayNames[i], _template.WeekDays[i]);
			}
			rbDays.Checked = _template.Mode == IssueTemplateMode.WeekDays;
			rbOdd.Checked = _template.IsOdd;
			rbEven.Checked = !_template.IsOdd;

			SetDateTimeValue(dtStartDate, _template.StartDate);
			SetDateTimeValue(dtFinishDate, _template.FinishDate);

			SetEnabled();
		}

		private void clbWeekDays_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			_template.WeekDays[e.Index] = e.NewValue == CheckState.Checked;
		}

		private void SetEnabled()
		{
			rbEven.Enabled = rbNumber.Checked;
			rbOdd.Enabled = rbNumber.Checked;
			clbWeekDays.Enabled = rbDays.Checked;
		}

		private void groupButton_CheckChanged(object sender, EventArgs e)
		{
			RadioButton senderRB = (RadioButton)sender;
			senderRB.Checked = !senderRB.Checked;
			if (sender == rbNumber)
				rbDays.Checked = !senderRB.Checked;
			else if (sender == rbDays)
				rbNumber.Checked = !senderRB.Checked;

			SetEnabled();
		}

        private void btnOk_Click(object sender, EventArgs e)
        {
			if (!rbModeAdd.Checked && !rbModeDelete.Checked)
			{
				MessageBox.Show("Надо выбрать что вы собираетесь делать: добавить или удалить рекламные выпуски!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				this.DialogResult = DialogResult.None;
            }
			_template.IsModeAdd = rbModeAdd.Checked;
        }

		public void SetModeAdd() 
		{ 
			rbModeAdd.Checked = true; 
			rbModeDelete.Checked = false;
			rbModeAdd.Enabled = rbModeDelete.Enabled = false;
		}
    }
}