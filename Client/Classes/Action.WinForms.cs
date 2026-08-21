using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;
using Merlin.Reports;
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

		protected delegate void PrintAgencyDocument(Form owner, Agency agency, bool doExport);

		public void PrintContracts(Form owner, bool doExport)
		{
			PrintAgencyDocuments(owner, PrintContract, doExport);
		}

		public void PrintBillContracts(Form owner, bool doExport)
		{
			PrintAgencyDocuments(owner, PrintBillContract, doExport);
		}

		public void PrintSponsorContracts(Form owner, bool doExport)
		{
			PrintAgencyDocuments(owner, PrintSponsorContract, doExport);
		}

		public void PrintBills(Form owner, bool byMounth, bool doExport)
		{
			if (byMounth)
			{
				Refresh();
				Application.DoEvents();
				List<PresentationObject> agencies = Agency.SelectAgencies(this, Parameters, owner);
				Application.DoEvents();
				if (agencies == null) return;
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				IList<DateTime> months = GetSelectedMonths();
				if (months == null || months.Count <= 0)
					return;

				foreach (PresentationObject po in agencies)
				{
					PresentationObject bill = GetBill((Agency)po, owner);
					foreach (DateTime month in months)
					{
						Application.DoEvents();
						Agency agency = (Agency)po;
						BillReport report = new BillReport(this, agency, bill, month);
						string caption = string.Format("Счёт на предоплату, агенство '{0}' за месяц {1} {2} года", agency.Name
							, DateTimeFormatInfo.CurrentInfo.MonthNames[month.Month - 1], month.Year);

						string fileName = string.Format("{0} №{1} к акции {2} для {3}.doc",
							"Счёт на предоплату",
							bill[TableColumns.Bill.BillNo],
							ActionId,
							FirmName);
						report.Show(caption, fileName);
					}
				}
			}
			else
				PrintAgencyDocuments(owner, PrintBill, false);
		}

		private void ChangeFirm(Control owner)
		{
			if (IsChangeFirmPossible)
			{
				Firm newFirm = Firm.SelectFirm(owner);
				if (newFirm != null)
				{
					Application.DoEvents();
					owner.Cursor = Cursors.WaitCursor;

					this[ParamNames.FirmId] = newFirm.FirmId;
					Update();
					Refresh();
					OnObjectChanged(this);
					UserMessage.ShowInformation(Properties.Resources.FirmChangeSuccess);
				}
			}
			else
				UserMessage.ShowExclamation(MessageAccessor.GetMessage("ChangeFirmIsForbidden"));
		}

		private bool IsChangeFirmPossible
		{
			get
			{
				if (SecurityManager.LoggedUser.IsAdmin || SecurityManager.LoggedUser.IsBookKeeper || !IsConfirmed) return true;
				// если акция началась в предыдущем месяце или ранее, то нельзя
				if (new DateTime(StartDate.Year, StartDate.Month, 1) < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)) return false;
				// если начало в этом месяце, то не должна уже закончиться
				if (FinishDate < DateTime.Today) return false;

				return true;
			}
		}

		private void ChangeCreator(Control owner)
		{
			PresentationObject manager = Utils.SelectManager(owner);
			if (manager != null)
			{
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				parameters[ParamNames.NewCreatorId] = manager.IDs[0].ToString();
				Update();
				Refresh();
			}
		}

		private void PrintAgencyDocuments(Form owner, PrintAgencyDocument doc, bool doExport)
		{
			try
			{
				Refresh();
				Application.DoEvents();

				List<PresentationObject> agencies = Agency.SelectAgencies(this, Parameters, owner);
				Application.DoEvents();

				if (agencies == null) return;
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				foreach (PresentationObject po in agencies)
					doc(owner, po as Agency, doExport);
			}
			finally
			{
				owner.Cursor = Cursors.Default;
			}
		}

		protected virtual void PrintContract(Form owner, Agency agency, bool exportReport)
		{
			PresentationObject bill = GetBill(agency, owner);
			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;

			ContractReport report = new ContractReport(this, agency, bill);
			string fileName = string.Format("{0} №{1} к акции {2} для {3}.rtf",
				"Договор",
				bill[TableColumns.Bill.BillNo],
				ActionId,
				FirmName);
			report.Show("Договор", fileName);
		}

		protected virtual void PrintSponsorContract(Form owner, Agency agency, bool exportReport)
		{
			PresentationObject bill = GetBill(agency, owner);
			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;

			ContractReport report = new ContractReport(this, agency, bill, true);
			string fileName = string.Format("{0} №{1} к акции {2} для {3}.rtf",
				"Спонсорский договор",
				bill[TableColumns.Bill.BillNo],
				ActionId,
				FirmName);
			report.Show("Спонсорский договор", fileName);
		}

		private void PrintBillContract(Form owner, Agency agency, bool exportReport)
		{
			PresentationObject bill = GetBill(agency, owner);
			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;
			BillReport report = new BillContractReport(this, agency, bill);
			string fileName = string.Format("{0} №{1} к акции {2} для {3}.rtf",
				"Счёт-договор",
				bill[TableColumns.Bill.BillNo],
				ActionId,
				FirmName);
			report.Show("Счёт-договор", fileName);
		}

		protected virtual void PrintBill(Form owner, Agency agency, bool exportReport)
		{
			// Load Bill data
			PresentationObject bill = GetBill(agency, owner);
			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;
			BillReport report = new BillReport(this, agency, bill);
			string fileName = string.Format("{0} №{1} к акции {2} для {3}.doc",
				"Счёт",
				bill[TableColumns.Bill.BillNo],
				ActionId,
				FirmName);
			if (exportReport) report.Export(ReportExportFormat.WordForWindows);
			else report.Show("Счёт", fileName);
		}

		protected void SetAdvertTypeOrSubstituteRoller()
		{
			try
			{
				Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
				procParameters.Add(ParamNames.ActionId, ActionId);
				Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.ActionRollers),
					string.Format("Ролики рекламной акции № {0}", ActionId),
					procParameters, showModal: true);
				FireContainerRefreshed();
			}
			finally { Cursor.Current = Cursors.Default; }
		}
	}
}
