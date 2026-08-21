using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Forms.FilterForm;

namespace Merlin.Classes.FakeContainers
{
	// UI-часть ActionContainer. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class ActionContainer
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			try
			{
				if (actionName == ActionNames.ShowActions)
				{
					ChildEntity = _actionEntity;
					FireContainerRefreshed();
				}
				else if (actionName == ActionNames.ShowFirms)
				{
					ChildEntity = _firmEntity;
					FireContainerRefreshed();
				}
                else if (actionName == ActionNames.ShowHeadCompanies)
                {
                    ChildEntity = _headCompanyEntity;
                    FireContainerRefreshed();
                }
                else if (actionName == Constants.EntityActions.ShowFilters)
				{
					ShowFilter(owner);
				}

				base.DoAction(actionName, owner, interfaceObject);
			}
            catch (Exception e)
            {
                ErrorManager.PublishError(e);
            }
        }

        public override void ShowFilter(IWin32Window owner)
        {
            try
            {
                Application.DoEvents();
                Cursor.Current = Cursors.WaitCursor;

                FilterForm frm = new ActionJournalFilter(relationScenario.StartingEntity, Globals.PrepareForFilter(RootEntity), _filter, relationScenario.XmlFilter);

                if (frm.ShowDialog(owner) == DialogResult.OK)
                    FireContainerRefreshed();
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
	}
}
