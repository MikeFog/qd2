using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;

namespace Merlin.Classes
{
	// UI-часть ActionRoller: диспетчеризация, диалог замены ролика во всей акции
	// и диалог смены предмета рекламы. Запись (ApplyActionSubstitution,
	// ApplyAdvertTypeChange) — в ActionRoller.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ActionRoller
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName.Equals(Action.ActionNames.SetAdvertType, System.StringComparison.OrdinalIgnoreCase))
				SetAdvertType((Form)owner, true);
			else if (string.Compare(actionName, Constants.Actions.Substitute, StringComparison.OrdinalIgnoreCase) == 0)
				SubstituteRoller((Form)owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void SubstituteRoller(Form owner)
		{
			try
			{
				Entity entity = EntityManager.GetEntity((int)Entities.Roller);
				ActionOnMassmedia action = ActionOfRoller();

				SelectionForm form = new SelectionForm(entity, action.Firm.GetRollers().DefaultView, "Замена ролика");
				if (form.ShowDialog(owner) == DialogResult.OK)
				{
					owner.UseWaitCursor = true;
					Application.DoEvents();

					decimal price = action.TotalPrice;
					// Журналы незаменённых роликов — по разу на каждую кампанию/модуль/пакет,
					// как и раньше; теперь после записи всех, а не между ними.
					foreach (DataTable unsubstituted in ApplyActionSubstitution(action, new Roller((int)form.SelectedObject.IDs[0])))
						CampaignRoller.ShowUnsubstitutedRollers(unsubstituted);
					action.Recalculate();
					OnDataNeedRefresh();
					CampaignPart.ShowPriceChangeMessage(price, action.TotalPrice);
				}
			}
			finally { owner.UseWaitCursor = false; }
		}

		protected void SetAdvertType(Form owner, bool changeFlag)
		{
			try
			{
				Entity entity = EntityManager.GetEntity((int)Entities.AdvertTypeChild);
				SelectionForm form = new SelectionForm(entity, entity.GetContent().DefaultView, "Выбор предмета рекламы");
				if (form.ShowDialog(owner) == DialogResult.OK)
				{
					owner.UseWaitCursor = true;
					Application.DoEvents();

					ApplyAdvertTypeChange(form.SelectedObject.IDs[0], changeFlag);
				}
			}
			finally { owner.UseWaitCursor = false; }
		}
	}
}
