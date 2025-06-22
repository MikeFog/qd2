using System;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class FrmTemplate : Form
	{
		private IssueTemplate template;

		public IssueTemplate Template
		{
			get
			{
				template.StartDate = dtStartDate.Value;
				template.FinishDate = dtFinishDate.Value;
				template.IsOdd = rbOdd.Checked;
				template.Mode = (rbNumber.Checked) ? IssueTemplateMode.OddEvenDays : IssueTemplateMode.WeekDays;
				return template;
			}
		}

		public FrmTemplate(IssueTemplate template)
		{
			this.template = template ?? new IssueTemplate();
			this.template.Mode = IssueTemplateMode.WeekDays;
			InitializeComponent();
		}

		public FrmTemplate() : this(null)
		{
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
				clbWeekDays.Items.Add(DateTimeUtils.WeekDayNames[i], template.WeekDays[i]);
			}
			rbDays.Checked = template.Mode == IssueTemplateMode.WeekDays;
			rbOdd.Checked = template.IsOdd;
			rbEven.Checked = !template.IsOdd;

			SetDateTimeValue(dtStartDate, template.StartDate);
			SetDateTimeValue(dtFinishDate, template.FinishDate);

			SetEnabled();
		}

		private void clbWeekDays_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			template.WeekDays[e.Index] = e.NewValue == CheckState.Checked;
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
	}
}