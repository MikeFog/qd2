using System;
using System.Windows.Forms;
using FogSoft.WinForm.Passport.Forms;

namespace FogSoft.WinForm.Classes
{
	// UI-часть FakeContainer. Основная часть класса — в FakeContainer.cs, она
	// компилируется также в сборку без UI. Логика не менялась, код перенесён
	// как есть — тот же паттерн, что ObjectContainer.WinForms.cs (этап 0.1).
	// См. docs/tasks/web-migration-dialogs.md, §10.
	public partial class FakeContainer : IActionHandler
	{
		public virtual void DoAction(
			string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.EntityActions.AddNew:
					PresentationObject newObject = childEntity.NewObject;
					if(newObject.ShowPassport(owner) && ObjectCreated != null)
					{
						IObjectContainer oc = newObject as IObjectContainer;
						if(oc != null)
							oc.RelationScenario = relationScenario;
						ObjectCreated(newObject);
					}
					break;

				case Constants.EntityActions.Refresh:
					FireContainerRefreshed();
					break;
			}
		}

		public virtual void ShowFilter(IWin32Window owner)
		{
            try
            {
				if(!IsFilterable)
					return;

                Cursor.Current = Cursors.WaitCursor;
                Application.DoEvents();

                FilterForm frm = new FilterForm(relationScenario.StartingEntity, Globals.PrepareForFilter(RootEntity), _filter, relationScenario.XmlFilter);
					
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
