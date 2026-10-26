using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes.FakeContainers;

namespace Merlin.Classes
{
	// UI-часть BillRegisterRow: окно журнала акций; отбор по строке — ActionJournalFilter в ядре.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class BillRegisterRow
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName.Equals(OpenActionJournalAction, System.StringComparison.CurrentCultureIgnoreCase))
			{
				// Активированная акция — в журнал подтверждённых, макет — в журнал макетов (как пункты меню).
				ActionContainer container = IsActionConfirmed
					? new ActionContainer(RelationManager.GetScenario(RelationScenarios.ConfirmedAction),
						"Журнал подтверждённых рекламные акции", Entities.FirmWithConfirmedActions, Entities.Action,
						Entities.HeadCompanyWithConfirmedActions)
					: new ActionContainer(RelationManager.GetScenario(RelationScenarios.UnconfirmedAction),
						"Макеты рекламных акций", Entities.FirmWithUnconfirmedActions, Entities.Action,
						Entities.HeadCompanyWithUnconfirmedActions);
				foreach (var pair in ActionJournalFilter())
					container.Filter[pair.Key] = pair.Value;

				Globals.ShowBrowser(container,
					IsActionConfirmed ? "Подтверждённые рекламные акции" : "Макеты рекламных акций", Globals.MdiParent);
			}
			else
				base.DoAction(actionName, owner, interfaceObject);
		}
	}
}
