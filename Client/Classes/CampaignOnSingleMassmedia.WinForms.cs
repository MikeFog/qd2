using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;
using Merlin.Reports;

namespace Merlin.Classes
{
	// UI-часть CampaignOnSingleMassmedia. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	partial class CampaignOnSingleMassmedia
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.PrintOnAirInquire)
				PrintOnAirInquire((Form)owner);
			else if (actionName == Constants.Actions.ChangePositions)
				ChangePositions((Form)owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		public override void PrintOnAirInquire(Form owner)
		{
			try
			{
				Refresh();
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				Dictionary<string, object> procParameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
				procParameters[ParamNames.CampaignId] = CampaignId;
				DataSet ds = DataAccessor.LoadDataSet("sl_Months", procParameters);

				Dictionary<object, object> dMonthsToShow = new Dictionary<object, object>();
				Dictionary<object, object> dMonths = new Dictionary<object, object>();

				foreach (DataRow row in ds.Tables[0].Rows)
				{
					int month = ParseHelper.ParseToInt32(row["MonthDate"].ToString(), -1);
					int year = ParseHelper.ParseToInt32(row["MonthYear"].ToString(), -1);
					if (month >= 0 && year >= 0)
					{
						DateTime date = new DateTime(year, month, 1);
						dMonthsToShow.Add(date, date.ToString("MMMM yyy"));
						dMonths.Add(date, date);
					}
				}

				FrmMonths f = new FrmMonths(dMonthsToShow);
				if (f.ShowDialog(owner) == DialogResult.Cancel) return;

				foreach (object dm in f.CheckedItems.Keys)
				{
					DataSet rs = GetOnAirInquireReport(MassmediaId, CampaignId, (DateTime)dMonths[dm], ((DateTime)dMonths[dm]).AddMonths(1).AddDays(-1));
					OnAirInquireReport report = new OnAirInquireReport(this, Agency, rs, f.IsOptionChecked, Massmedia, (DateTime)dMonths[dm]);
					string fileName = string.Format("{0} для {1} к акции {2} для {3}.rtf",
						"Эфирная справка",
						MassmediaName,
						ActionId,
						Action.FirmName);
					report.Show("Эфирная справка", fileName);
				}
			}
			finally
			{
				owner.Cursor = Cursors.Default;
			}
		}
	}
}
