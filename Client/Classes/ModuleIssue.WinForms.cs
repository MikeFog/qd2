using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть ModuleInCampaign: DoAction/DeleteIssues. Дословный перенос.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class ModuleInCampaign
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
			if (actionName == Campaign.ActionNames.DeleteIssues)
				DeleteIssues((Form)owner);
            else if (actionName == Constants.Actions.ChangePositions)
                ChangePositions((Form)owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private void DeleteIssues(Form owner)
        {
            Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters[CampaignPart.OBJECT_ID] = this[Module.ParamNames.ModuleId];
			if (Campaign.DeleteIssues(owner, false, parameters, isFireEvent: false))
				FireContainerRefreshed();
        }
	}

	// UI-часть ModuleIssue: DoAction. Дословный перенос.
	internal partial class ModuleIssue
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
			if (string.Compare(actionName, Constants.Actions.Substitute) == 0)
				SubstituteRollerForSingleIssue(Roller);
			else
				base.DoAction(actionName, owner, interfaceObject);
        }
	}
}
