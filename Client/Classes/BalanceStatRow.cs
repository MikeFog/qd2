using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Classes.FakeContainers;
using System;
using System.Windows.Forms;

namespace Merlin.Classes
{
    internal class BalanceStatRow : PresentationObject
    {
        public BalanceStatRow() : base(EntityManager.GetEntity((int)Entities.StatBonuses))
        {

        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName.Equals("OpenActionJournal", System.StringComparison.CurrentCultureIgnoreCase))
            {
                var container = new ActionContainer(RelationManager.GetScenario(RelationScenarios.ConfirmedAction),
                    "Журнал подтверждённых рекламные акции", Entities.FirmWithConfirmedActions, Entities.Action, 
                    Entities.HeadCompanyWithConfirmedActions);
                container.Filter["firmID2"] = parameters["FirmId"];
                container.Filter["massmediaGroupID"] = parameters["massmediaGroupID"];
                container.Filter["userID"] = parameters["userID"];
                var startDate = parameters["periodStartDate"];
                var finishDate = parameters["periodFinishDate"];
                if ((bool)parameters["selectByCreateDate"])
                {
                    container.Filter["createDateBegin"] = startDate;
                    container.Filter["createDateEnd"] = finishDate;
                    container.Filter["startOfInterval"] = DBNull.Value;
                }
                else
                {
                    container.Filter["startOfInterval"] = startDate;
                    container.Filter["endOfInterval"] = finishDate;
                }

                Globals.ShowBrowser(container, "Подтверждённые рекламные акции", Globals.MdiParent);
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
    }
}
