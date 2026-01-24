using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;
using Merlin.Classes;

namespace Merlin.Forms
{
	internal partial class FrmWindowTariffTemplate : FogSoft.WinForm.Passport.Forms.PassportForm
	{
		private readonly string[] strWeeksParams = {
		                                           	"monday", "tuesday", "wednesday", "thursday", "friday", "saturday",
		                                           	"sunday"
		                                           };

		private readonly Massmedia mm;

		public FrmWindowTariffTemplate()
		{
			InitializeComponent();
		}

		public FrmWindowTariffTemplate(DateTime dayStart, Massmedia mm)
			: this(dayStart, 0, 0, mm)
		{
		}

		public FrmWindowTariffTemplate(DateTime dayStart, int duration, int durationToatl, Massmedia mm)
			: base(PassportLoader.Load("TrafficTemplate"))
		{
			InitializeComponent();
			btnApply.Visible = false;
			AlwaysAffectDisabled = true;
			this.mm = mm;
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters["startDate"] = dayStart.Date;
			parameters["finishDate"] = dayStart.Date;
			parameters["time"] = dayStart;
			parameters["duration"] = duration;
            parameters["duration_total"] = durationToatl;
            foreach (string weeksParam in strWeeksParams)
				parameters[weeksParam] = true;
			pageContext = new PageContext(new DataSet(), parameters);
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				DataSet dsErrors = Save();
				if (dsErrors.Tables[0].Rows.Count > 0)
				{
					ShowErrors(dsErrors.Tables[0].Rows);
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

        private void ShowErrors(DataRowCollection rows)
        {
            DataTable tableErrors = ErrorManager.CreateErrorsTable();
            foreach (DataRow row in rows)
			{
                ErrorManager.AddErrorRow(tableErrors,(DateTime)row["windowDate"], MessageAccessor.GetMessage(row["errorMessage"].ToString()));
            }
            Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ErrTmplGen), "Ошибки добавления рекламных окон", tableErrors);
        }

        private DataSet Save()
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>();

			parameters["startDate"] = ((DateTimePicker)FindControl("startDate")).Value;
			parameters["finishDate"] = ((DateTimePicker)FindControl("finishDate")).Value;
			parameters["time"] = ((DateTimePicker)FindControl("time")).Value;
			parameters["duration"] = ((TimeDuration)FindControl("duration")).Value;
            parameters["duration_total"] = ((TimeDuration)FindControl("duration_total")).Value;
            parameters["massmediaID"] = mm.MassmediaId;
			foreach (string weeksParam in strWeeksParams)
				parameters[weeksParam] = ((CheckBox)FindControl(weeksParam)).Checked;
			return DataAccessor.LoadDataSet("GenerateTariffWindowByTemplate", parameters);
		}
	}
}

