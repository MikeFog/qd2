using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes.FakeContainers;

namespace Merlin.Classes
{
	// UI-часть BalanceStatRow: окно журнала акций; отбор по строке — ActionJournalFilter в ядре.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class BalanceStatRow
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName.Equals(OpenActionJournalAction, System.StringComparison.CurrentCultureIgnoreCase))
            {
                var container = new ActionContainer(RelationManager.GetScenario(RelationScenarios.ConfirmedAction),
                    "Журнал подтверждённых рекламные акции", Entities.FirmWithConfirmedActions, Entities.Action, 
                    Entities.HeadCompanyWithConfirmedActions);
                foreach (var pair in ActionJournalFilter())
                    container.Filter[pair.Key] = pair.Value;

                Globals.ShowBrowser(container, "Подтверждённые рекламные акции", Globals.MdiParent);
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
	}
}
