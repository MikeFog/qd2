using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;
using static Merlin.Classes.TableColumns;

namespace Merlin.Classes
{
	// UI-часть Action: диспетчеризация, построение медиаплана, счёт по
	// агентству. Дословный перенос, логика не менялась. PrintMediaPlan
	// перенесён целиком — делегирует в MediaPlan.Show, отдельная область
	// (docs/tasks/web-migration-dialogs.md, §6). Конвенция —
	// docs/tasks/web-migration-dialogs.md.
	public abstract partial class Action
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			try
			{
				if (actionName == ActionNames.ChangeFirm)
					ChangeFirm((Form)owner);
				else if (actionName == ActionNames.ChangeCreator)
					ChangeCreator((Form)owner);
				else if (actionName == ActionNames.PrintContract || actionName == ActionNames.ExportContract)
					PrintContracts((Form)owner, actionName == ActionNames.ExportContract);
				else if (actionName == ActionNames.PrintSponsorContract)
					PrintSponsorContracts((Form)owner, false);
				else if (actionName == ActionNames.PrintBill)
					PrintBills((Form)owner, false, false);
				else if (actionName == ActionNames.PrintBillByMounth)
					PrintBills((Form)owner, true, false);
				else if (actionName == ActionNames.PrintMediaPlan)
					PrintMediaPlan(ActionMediaPlanType.Simple, false);
				else if (actionName == ActionNames.PrintMediaPlanMonth)
					PrintMediaPlan(ActionMediaPlanType.Month, false);
				else if (actionName == ActionNames.PrintMediaPlanByPeriod)
					PrintMediaPlan(ActionMediaPlanType.Period, false);
				else if (actionName == ActionNames.PrintSelectivelyMediaPlan)
					PrintMediaPlan(ActionMediaPlanType.Simple, true);
				else if (actionName == ActionNames.PrintSelectivelyMediaPlanMonth)
					PrintMediaPlan(ActionMediaPlanType.Month, true);
				else if (actionName == ActionNames.PrintSelectivelyMediaPlanByPeriod)
					PrintMediaPlan(ActionMediaPlanType.Period, true);
				else if (actionName == ActionNames.SetAdvertType)
					SetAdvertTypeOrSubstituteRoller();
				else if (actionName == ActionNames.PrintBillContract)
					PrintBillContracts((Form)owner, false);
				else
					base.DoAction(actionName, owner, interfaceObject);
			}
			finally
			{
				((Control)owner).Cursor = Cursors.Default;
			}
		}

		public void PrintMediaPlan(ActionMediaPlanType type, bool selectively)
		{
			Refresh();
			//DataSet ds = Campaigns;
			//if (ds.Tables.Count > 0)
			//{
			switch (type)
			{
				case ActionMediaPlanType.Massmedias:
					MediaPlan.CreateInstance(this, selectively).Show(true);
					break;
				case ActionMediaPlanType.Simple:
					MediaPlan.CreateInstance(GetCampaigns(Campaigns()), selectively).Show(true);
					break;
				case ActionMediaPlanType.Month:
					IList<DateTime> months = GetSelectedMonths();
					if (months == null)
						return;
					MediaPlan.CreateInstance(GetCampaigns(Campaigns()), months, selectively).Show(true);
					break;
				case ActionMediaPlanType.Period:
					FrmDateSelector selector = new FrmDateSelector(StartDate, FinishDate, "Выбор периода");
					if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
						MediaPlan.CreateInstance(GetCampaigns(Campaigns()), selector.StartDate, selector.FinishDate, selectively).Show(true);
					break;
			}
			//}
		}

		private IList<DateTime> GetSelectedMonths()
		{
			Dictionary<object, object> dMonthsToShow = SelectMonthsToShow();
			FrmMonths f = new FrmMonths(dMonthsToShow, false);
			if (f.ShowDialog(Globals.MdiParent) == DialogResult.Cancel)
				return null;
			IList<DateTime> months = new List<DateTime>();
			foreach (KeyValuePair<object, object> item in f.CheckedItems)
				months.Add((DateTime)item.Key);
			return months;
		}

		private PresentationObject GetBill(Agency agency, Form owner)
		{
			PresentationObject bill = GetBill(agency.AgencyId, EntityManager.GetEntity((int)Entities.GeneralBill));
			return CreateBill(agency, owner, EntityManager.GetEntity((int)Entities.GeneralBill), bill);
		}

		protected PresentationObject CreateBill(Agency agency, Form owner, Entity entityBill, PresentationObject bill)
		{
			Dictionary<string, object> procParameters =
				new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

			procParameters[ParamNames.ActionId] = ActionId;
			procParameters[Agency.ParamNames.AgencyId] = agency.AgencyId;
			if (bill != null)
				procParameters[TableColumns.Bill.BillNo] = bill[TableColumns.Bill.BillNo];

			DateTime billDate = (bill != null) ? ParseHelper.GetDateTimeFromObject(bill[TableColumns.Bill.BillDate], DateTime.Today) : DateTime.Today;

			FrmBill fBill = new FrmBill(entityBill, billDate, procParameters);
			return fBill.ShowDialog(owner) == DialogResult.Cancel ? null : fBill.Bill;
		}
	}
}
