using System;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Forms.FilterForm;

namespace Merlin.Classes.FakeContainers
{
	// UI-часть StudioOrderActionContainer. Дословный перенос, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class StudioOrderActionContainer
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == ActionNames.ShowActions)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.StudioOrderAction);
				FireContainerRefreshed();
			}
			else if (actionName == ActionNames.ShowFirms)
			{
				ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithOrders);
				FireContainerRefreshed();
			}
			else if (actionName == Constants.EntityActions.ShowFilters)
			{
				ShowFilter(owner);
			}

			base.DoAction(actionName, owner, interfaceObject);
		}

		public override void ShowFilter(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				FilterForm frm = XmlFilter != null ? new SOActionJournalFilter(RootEntity, XmlFilter, Globals.PrepareForFilter(RootEntity), _filter)
									: new SOActionJournalFilter(RootEntity, Globals.PrepareForFilter(RootEntity), _filter);

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
