using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using unoidl.com.sun.star.frame.status;

namespace Merlin.Forms
{
    public partial class FrmTemplate2 : Form
    {
        private readonly IssueTemplate _template;

        public FrmTemplate2()
        {
            InitializeComponent();
        }

        public FrmTemplate2(string rollerName) : this()
        {
            _template = new IssueTemplate(DateTime.Today.AddDays(1), DateTime.Today.AddDays(1),
                new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 12, 0, 0),
                new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 15, 0, 0),
                2);

            lblRollerName.Text = rollerName;
        }

        private void FrmTemplate2_Load(object sender, EventArgs e)
        {
            dtStartDate.Value = _template.StartDate;
            dtFinishDate.Value = _template.FinishDate;
            dtStartTime.Value = _template.StartTime;
            dtFinishTime.Value = _template.FinishTime;
            txtQuantity.Value = _template.Quantity;

            clbWeekDays.Items.Clear();
            for (int i = 0; i < DateTimeUtils.WeekDayNames.Length; i++)
            {
                clbWeekDays.Items.Add(DateTimeUtils.WeekDayNames[i], true);
            }
        }

        public IssueTemplate Template
        {
            get => _template;
        }

        private void SaveData2Template()
        {
            _template.StartDate = dtStartDate.Value.Date;
            _template.FinishDate = dtFinishDate.Value.Date;
            _template.StartTime = dtStartTime.Value;
            _template.FinishTime = dtFinishTime.Value;
            _template.Quantity = ((int)txtQuantity.Value);

            _template.Mode = IssueTemplateMode.TimePeriod;
        }

        private void clbWeekDays_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            _template.WeekDays[e.Index] = e.NewValue == CheckState.Checked;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SaveData2Template();
            bool errorFlag = false;
            if(_template.StartDate > _template.FinishDate)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("TemplateStartFinishDateError"));
                errorFlag=true;
            }
            if (_template.StartTime >= _template.FinishTime)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("TemplateStartFinishTimeError"));
                errorFlag = true;
            }
            if (errorFlag)
                DialogResult = DialogResult.None;
        }
    }
}
