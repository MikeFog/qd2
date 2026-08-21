using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть SponsorProgramPart: DoAction. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	partial class SponsorProgramPart
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if(actionName != Constants.EntityActions.Refresh)
				behindClass.DoAction(actionName, owner, interfaceObject);

			if (actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowDays ||
				actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowPrograms ||
				 actionName == ProgramPartOfSponsorCampaign.ActionNames.ShowRollers ||
					actionName == Constants.EntityActions.Refresh)
			{
				ChildEntity = behindClass.ChildEntity;
				FireContainerRefreshed();
			}
		}
	}
}
