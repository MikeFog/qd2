using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Controls;
using Merlin.Forms;
using Merlin.Reports;

namespace Merlin.Classes
{
	// UI-часть CampaignPackModule/CampaignPartPackModule. Дословный перенос,
	// логика не менялась. PrintOnAirInquire — генерация отчёта, отдельная
	// область (docs/tasks/web-migration.md, этап 4), не диалоговый паттерн.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class CampaignPackModule
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.EntityActions.Edit)
				EditRollerIssues(owner, new PackModuleGrid());
			else if (actionName == ActionNames.ShowDays)
				ShowDays();
			else if (actionName == PackActionNames.ShowPackModules)
				ShowPackModules();
			else if (actionName == ActionNames.PrintOnAirInquire)
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

				owner.Cursor = Cursors.WaitCursor;
				Application.DoEvents();

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
					if (parameters.ContainsKey("packmodulemassmediaID") && parameters["packmodulemassmediaID"] != null)
					{
						int mmId = ParseHelper.ParseToInt32(parameters["packmodulemassmediaID"].ToString(), -1);
						if (mmId > 0)
						{
							Massmedia massmedia = Massmedia.GetMassmediaByID(mmId);
							DataSet rs = GetOnAirInquireReport(mmId, CampaignId, (DateTime)dMonths[dm], ((DateTime)dMonths[dm]).AddMonths(1).AddDays(-1));
							OnAirInquireReport report = new OnAirInquireReport(this, Agency, rs, f.IsOptionChecked, massmedia, (DateTime)dMonths[dm]);
							string fileName = string.Format("{0} {1} {2} к акции {3} для {4}.doc",
								"Эфирная справка",
								massmedia.Name,
								((DateTime)dMonths[dm]).ToString("MMMM yyyy"),
								ActionId,
								Action.FirmName);
							report.Show("Эфирная справка", fileName);
						}
					}
					else
					{
						DataSet dsMM = Massmedias;
						if (dsMM != null)
						{
							DataTable table = dsMM.Tables[Constants.TableNames.Data];
							foreach (DataRow row in table.Rows)
							{
								int mmId = ParseHelper.ParseToInt32(row["packmodulemassmediaID"].ToString(), -1);
								if (mmId > 0)
								{
									Massmedia massmedia = Massmedia.GetMassmediaByID(mmId);
									DataSet rs = GetOnAirInquireReport(mmId, CampaignId, (DateTime)dMonths[dm], ((DateTime)dMonths[dm]).AddMonths(1).AddDays(-1));
									OnAirInquireReport report = new OnAirInquireReport(this, Agency, rs, f.IsOptionChecked, massmedia, (DateTime)dMonths[dm]);
									string fileName = string.Format("{0} {1} {2} к акции {3} для {4}.doc",
										"Эфирная справка",
										massmedia.Name,
										((DateTime)dMonths[dm]).ToString("MMMM yyyy"),
										ActionId,
										Action.FirmName);
									report.Show("Эфирная справка", fileName);
								}
							}
						}
					}
				}
			}
			finally
			{
				owner.Cursor = Cursors.Default;
			}
		}
	}

	internal partial class CampaignPartPackModule
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Campaign.ActionNames.DeleteIssues)
				DeleteIssues((Form)owner);
			else if (actionName == Constants.Actions.ChangePositions)
				ChangePositions((Form)owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void DeleteIssues(Form owner)
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters[CampaignPart.OBJECT_ID] = this[PackModule.ParamNames.PackModuleId];
			if (Campaign.DeleteIssues(owner, false, parameters, isFireEvent: false))
				FireContainerRefreshed();
		}
	}
}
