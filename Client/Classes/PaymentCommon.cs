using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms;

namespace Merlin.Classes
{
	public class PaymentCommon : Payment
	{
		private struct ActionNames
		{
			public const string SelectActionsToPay = "SelectActionsToPay";
		}

		public PaymentCommon()
			: base(EntityManager.GetEntity((int) Entities.PaymentCommon))
		{
		}

		public PaymentCommon(DataRow row)
			: base(EntityManager.GetEntity((int) Entities.PaymentCommon), row)
		{
		}

		public override Entity ProfitEntity
		{
			get { return EntityManager.GetEntity((int) Entities.PaymentCommonAction); }
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.SelectActionsToPay)
				SelectActions(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ActionNames.SelectActionsToPay)
				return bool.Parse(this["isEnabled"].ToString()) && base.IsActionEnabled(actionName, type)
				       && (Consumed < Summa);
			return base.IsActionEnabled(actionName, type);
		}

		private void SelectActions(IWin32Window owner)
		{
			// Load Candidates
			Entity entityPaymentCandidate =
				EntityManager.GetEntity((int) Entities.ActionPaymentCandidate);
			Dictionary<string, object> procParameters =
				DataAccessor.PrepareParameters(entityPaymentCandidate);

			procParameters[ParamNames.PaymentID] = PaymentId;
			DataSet ds = DataAccessor.DoAction(procParameters) as DataSet;

			PaymentCandidatesForm candidates =
				new PaymentCandidatesForm(this, entityPaymentCandidate, ds.Tables[Constants.TableNames.Data],
				                          "Акции на оплату");
			if (candidates.ShowDialog(owner) == DialogResult.OK)
			{
				Refresh();
				FireContainerRefreshed();
			}
		}
	}
}