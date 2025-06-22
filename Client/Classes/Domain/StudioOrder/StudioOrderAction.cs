using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Reports;

namespace Merlin.Classes.Domain.StudioOrder
{
	public class StudioOrderAction : Action
	{
		public enum Status
		{
			New = 1,
			Paid = 2,
			Disscussed = 3,
			Finished = 5,
			Completed = 4
		}

		#region Constructor	-----------------------------------

		public StudioOrderAction()
			: base(EntityManager.GetEntity((int) Entities.StudioOrderAction))
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.StudioOrder);
			this[ParamNames.TariffPrice] = this[ParamNames.TotalPrice] = 0;
			isNew = true;
		}

		private StudioOrderAction(int actionID)
			: base(EntityManager.GetEntity((int) Entities.StudioOrderAction), actionID)
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.StudioOrder);
			isNew = false;
		}

		public StudioOrderAction(DataRow row)
			: base(EntityManager.GetEntity((int) Entities.StudioOrderAction), row)
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.StudioOrder);
		}

		public StudioOrderAction(PresentationObject firm)
			: base(EntityManager.GetEntity((int) Entities.StudioOrderAction), firm)
		{
			ChildEntity = EntityManager.GetEntity((int) Entities.StudioOrder);
		}

		#endregion

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.PrintAgreement || actionName == ActionNames.ExportAgreement)
				PrintContracts(owner as Form, actionName == ActionNames.ExportAgreement);
			else if (actionName == ActionNames.PrintBill)
				PrintBills(owner as Form, false, false);
			else if (actionName == "ChangeOwner")
				ChangeOwner(owner);
			else if (actionName == Constants.Actions.AddItem)
				AddOrderItem(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void AddOrderItem(IWin32Window owner)
		{
			StudioOrder orderItem = new StudioOrder(this);

			if (orderItem.ShowPassport(owner))
				FireContainerRefreshed();
		}

		private void ChangeOwner(IWin32Window owner)
		{
			SelectionForm selector = new SelectionForm(EntityManager.GetEntity((int) Entities.User), "Менеджеры");
			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				this[SecurityManager.ParamNames.UserId] = selector.SelectedObject.IDs[0];
				Update();
				Refresh();
			}
		}

		protected override void PrintBill(Form owner, Agency agency, bool exportReport)
		{
			// Load Bill data
			PresentationObject bill = GetStudioOrderBill(agency, owner);

			if (bill == null) return;

			Application.DoEvents();
			owner.Cursor = Cursors.WaitCursor;

			StudioOrderActionBillReport report =
				new StudioOrderActionBillReport(this, agency, Firm, bill);
			if (exportReport) report.Export(ReportExportFormat.WordForWindows);
			else report.Show("Счёт на предоплату");
		}

		private PresentationObject GetStudioOrderBill(Agency agency, Form owner)
		{
			PresentationObject bill =
				GetBill(agency.AgencyId, EntityManager.GetEntity((int) Entities.StudioOrderBill));

			bill = CreateBill(agency, owner, EntityManager.GetEntity((int)Entities.StudioOrderBill), bill);
			return bill;
		}

		protected override void PrintContract(Form owner, Agency agency, bool exportReport)
		{
			PresentationObject bill = GetStudioOrderBill(agency, owner);

			if (bill == null) return;

			DateTime billDate = ParseHelper.GetDateTimeFromObject(bill["billDate"], DateTime.Now) ;

			StudioOrderContractReport report = new StudioOrderContractReport(this, agency, Firm, billDate);
			if (exportReport) report.Export(ReportExportFormat.WordForWindows);
			else report.Show("Договор на производство роликов");
		}

		public static StudioOrderAction GetOrderActionByID(int actionID)
		{
			StudioOrderAction action = new StudioOrderAction(actionID);
			action.Refresh();
			return action;
		}

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (!CheckLoggedUserRight(actionName, this))
				return true;

			return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (!CheckLoggedUserRight(actionName, this))
				return false;

			return base.IsActionEnabled(actionName, type);
		}

		public static bool CheckLoggedUserRight(string actionName, StudioOrderAction action)
		{
			if (SecurityManager.LoggedUser.Id != action.UserID
				&& !SecurityManager.LoggedUser.IsRightToEditForeignSOActions()
				&& (!SecurityManager.LoggedUser.IsRightToEditGroupSOActions() || action.User == null 
					|| !SecurityManager.LoggedUser.IsInGroup(action.User.Groups))
				&& (new List<string> { Constants.EntityActions.Edit, 
					Constants.EntityActions.Delete, Constants.EntityActions.AddNew,
					Constants.EntityActions.AssignExisting, Constants.EntityActions.AssignNew, 
					ActionNames.MarkAsReady, ActionNames.MarkAsNotReady, 
					StudioOrder.ActionNames.ChangeAgency, StudioOrder.ActionNames.ChangePaymentType,
					StudioOrder.ActionNames.SetFinalPrice, ActionNames.ChangeOwner, Constants.Actions.AddItem, 
					ActionNames.ChangeFirm, Constants.EntityActions.ShowPassport}).Contains(actionName))
				return false;
			return true;
		}

		public int UserID
		{
			get { return ParseHelper.ParseToInt32(parameters[ParamNames.ManagerID].ToString()); }
		}

		private SecurityManager.User user;

		public SecurityManager.User User
		{
			get
			{
				if (user == null)
					user = SecurityManager.GetUser(UserID);
				return user;
			}
		}

		public override bool Refresh()
		{
			user = null;
			return base.Refresh();
		}

		public DataTable LoadBillData(Agency agency, DateTime? mounth)
		{
			Dictionary<string, object> procParameters =
				new Dictionary<string, object>(2, StringComparer.InvariantCultureIgnoreCase);

			procParameters[ParamNames.ActionId] = ActionId;
			procParameters[Agency.ParamNames.AgencyId] = agency.AgencyId;

			return DataAccessor.LoadDataSet("rpt_OrderActionBill", procParameters).Tables[0];
		}
	}
}