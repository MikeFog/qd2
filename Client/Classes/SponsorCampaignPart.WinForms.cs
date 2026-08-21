using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	// UI-часть SponsorCampaignProgram: DoAction/DeleteIssues. Дословный перенос.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class SponsorCampaignProgram
	{
        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName == Campaign.ActionNames.DeleteIssues)
                DeleteIssues((Form)owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private void DeleteIssues(Form owner)
        {
            Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters[OBJECT_ID] = this[ParamNames.ProgramId];
            if(Campaign.DeleteIssues(owner, true, parameters, isFireEvent: false))
				FireContainerRefreshed();	
        }
	}
}
