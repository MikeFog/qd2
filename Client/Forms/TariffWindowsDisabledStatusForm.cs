using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class TariffWindowsDisabledStatusForm : PassportForm
	{
		public struct Procedures
		{
			public const string DisableWindows = "sp_ChangeTariffWindowDisabledStatus";
            public const string MarkWindows = "sp_ChangeTariffWindowMarkedStatus";
        }

		public TariffWindowsDisabledStatusForm()
		{
			InitializeComponent();
		}

		private readonly bool _flag = false;
		private readonly Pricelist _priceList;
		private readonly string _procedure;

		public TariffWindowsDisabledStatusForm(Pricelist pl, bool flag, string procedure)
			: base(PassportLoader.Load("TariffWindowsStatusChange"))
		{
			btnApply.Visible = false;
			Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                ["startDate"] = pl.StartDate,
                ["endDate"] = pl.FinishDate
            };
			pageContext = new PageContext(new DataSet(), parameters);
            AlwaysAffectDisabled = true;
            _flag = flag;
			_priceList = pl;
			_procedure = procedure;
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			if (_priceList == null)
				return;
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				parameters["startDate"] = ((DateTimePicker)FindControl("startDate")).Value;
				parameters["endDate"] = ((DateTimePicker)FindControl("endDate")).Value;
				DateTime time = ((DateTimePicker)FindControl("time")).Value;
				parameters["time"] = new DateTime(1900, 1, 1, time.Hour, time.Minute, 0); 
				parameters["flag"] = _flag;
				parameters["priceListID"] = _priceList.PricelistId;
				foreach (DayOfWeek day in Enum.GetValues(typeof (DayOfWeek)))
				{
					string dayName = day.ToString("g").ToLower();
					parameters[dayName] = ((CheckBox)FindControl(dayName)).Checked;
				}
				DataAccessor.ExecuteNonQuery(_procedure, parameters);
			}
			catch (Exception e)
			{
				ErrorManager.PublishError(e);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}
	}
}