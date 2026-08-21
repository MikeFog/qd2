using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть PaymentCommon: диспетчеризация и диалог выбора акций на оплату.
	// Применение выбора — внутри PaymentCandidatesForm; бизнес-часть на этой
	// стороне — только подготовка кандидатов (GetPaymentCandidates, в
	// PaymentCommon.cs). Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class PaymentCommon
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.SelectActionsToPay)
				SelectActions(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void SelectActions(IWin32Window owner)
		{
			Entity entityPaymentCandidate = EntityManager.GetEntity((int)Entities.ActionPaymentCandidate);
			PaymentCandidatesForm candidates =
				new PaymentCandidatesForm(this, entityPaymentCandidate, GetPaymentCandidates(),
										  "Акции на оплату");
			if (candidates.ShowDialog(owner) == DialogResult.OK)
			{
				Refresh();
				FireContainerRefreshed();
			}
		}
	}
}
