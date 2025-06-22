using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;
using Merlin.Reports;

namespace Merlin.Classes.Domain.StudioOrder
{
	public class StudioOrder : PresentationObject
	{
		private struct ParamNames
		{
			public const string IsReady = "isComplete";
			public const string GrantorID = "grantorID";
		}

		public struct ActionNames
		{
			public const string PrintAct = "PrintAct";
			public const string ExportAct = "ExportAct";
			public const string ChangeAgency = "ChangeAgency";
			public const string ChangePaymentType = "ChangePaymentType";
			public const string SetFinalPrice = "SetFinalPrice";

			public const string MarkAsNotReady = "MarkAsNotReady";
			public const string MarkAsReady = "MarkAsReady";
		}

		internal StudioOrder(Action action)
			: this()
		{
			this[StudioOrderAction.ParamNames.ActionId] = action.ActionId;
		}

		public StudioOrder()
			: base(EntityManager.GetEntity((int) Entities.StudioOrder))
		{
		}

		public StudioOrder(DataRow row)
			:
				base(EntityManager.GetEntity((int) Entities.StudioOrder), row)
		{
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			Application.DoEvents();

			try
			{
				if (actionName == ActionNames.MarkAsReady)
					SetComplete(true);
				else if (actionName == ActionNames.MarkAsNotReady)
					SetComplete(false);
				if (actionName == ActionNames.ChangeAgency)
					ChangeAgency(owner);
				else if (actionName == ActionNames.ChangePaymentType)
					ChangePaymentType(owner);
				else if (actionName == ActionNames.SetFinalPrice)
					SetFinalPrice(owner);
				else if (actionName == ActionNames.ExportAct)
					PrintAct((Form) owner, true);
				else if (actionName == ActionNames.PrintAct)
					PrintAct((Form) owner, false);
				else
					base.DoAction(actionName, owner, interfaceObject);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		public decimal TariffPrice
		{
			get { return decimal.Parse(parameters["price"].ToString()); }
		}

		public decimal FinalPrice
		{
			get { return decimal.Parse(parameters["finalPrice"].ToString()); }
		}

		public decimal Ratio
		{
			set { parameters["ratio"] = value.ToString(); }
		}

		public DateTime? FinishDate
		{
			get { return (DateTime?) (StringUtil.IsDBNullOrNull(parameters["finishDate"]) ? null : parameters["finishDate"]); }
		}

		public DateTime CreatedDate
		{
			get { return (DateTime)parameters["createDate"]; }
		}

		private int ActionID
		{
			get { return int.Parse(parameters["actionID"].ToString()); }
		}

		private StudioOrderAction action;

		private StudioOrderAction Action
		{
			get
			{
				if (action == null)
					action = StudioOrderAction.GetOrderActionByID(ActionID);
				return action;
			}
		}

		public override bool Refresh()
		{
			action = null;
			return base.Refresh();
		}

		public StudioOrderAction OrderAction
		{
			get { return StudioOrderAction.GetOrderActionByID(ActionID); }
		}

		private void SetFinalPrice(IWin32Window owner)
		{
			ManagerDiscountForm fDiscount = new ManagerDiscountForm(this);
			if (fDiscount.ShowDialog(owner) == DialogResult.OK)
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;
				Ratio = fDiscount.Ratio;
				if (fDiscount.Grantor != null) this[ParamNames.GrantorID] = fDiscount.Grantor.Id;
				Update();
				OnObjectChanged(this);
			}
		}

		private void ChangeAgency(IWin32Window owner)
		{
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(EntityManager.GetEntity((int) Entities.Agency));
			procParameters["needStudioID"] = this["studioID"];

			DataSet ds = (DataSet)DataAccessor.DoAction(procParameters);

			SelectionForm selector = new SelectionForm(
				EntityManager.GetEntity((int) Entities.Agency), ds.Tables[Constants.TableNames.Data].DefaultView, "Агентства");

			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				this["agencyID"] = selector.SelectedObject.IDs[0];
				Update();
				Refresh();
			}
		}

		private void ChangePaymentType(IWin32Window owner)
		{
			SelectionForm selector = new SelectionForm(
				EntityManager.GetEntity((int) Entities.PaymentType), "Тип оплаты");

			if (selector.ShowDialog(owner) == DialogResult.OK)
			{
				parameters["paymentTypeID"] = selector.SelectedObject.IDs[0].ToString();
				Update();
				Refresh();
			}
		}

		private void PrintAct(Control owner, bool doExport)
		{
			try
			{
				Application.DoEvents();
				owner.Cursor = Cursors.WaitCursor;

				StudioOrderActReport report = new StudioOrderActReport(
					Action,
					Agency.GetAgencyByID(int.Parse(this[Agency.ParamNames.AgencyId].ToString())),
					this);
				if (doExport) report.Export(ReportExportFormat.WordForWindows);
				else report.Show("Акт выполненных работ");
			}
			finally
			{
				owner.Cursor = Cursors.Default;
			}
		}

		public bool IsReady
		{
			get { return bool.Parse(parameters[ParamNames.IsReady].ToString()); }
		}

		public override bool IsActionHidden(string actionName, ViewType type)
		{
			if (!StudioOrderAction.CheckLoggedUserRight(actionName, Action))
				return true;

			return base.IsActionHidden(actionName, type);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (!StudioOrderAction.CheckLoggedUserRight(actionName, Action))
				return false;

			if (actionName == ActionNames.MarkAsNotReady)
				return base.IsActionEnabled(actionName, type) &&  IsReady;
			if (actionName == ActionNames.MarkAsReady)
				return	 base.IsActionEnabled(actionName, type) && !IsReady;
			return base.IsActionEnabled(actionName, type);
		}

		private void SetComplete(bool completeFlag)
		{
			parameters[ParamNames.IsReady] = completeFlag;
			Update();
			Refresh();
			if (completeFlag)
				OpenJournalWithCreatedRollers();
		}

		private void OpenJournalWithCreatedRollers()
		{
			Dictionary<string, object> filterValues = new Dictionary<string, object>();
			filterValues.Add("rollerID", parameters["rollerID"]);
			Globals.ShowSimpleJournal(EntityManager.GetEntity((int)Entities.Roller), "Созданный ролик", filterValues);
		}
	}
}