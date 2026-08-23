using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Passport.Forms;

namespace FogSoft.WinForm.Classes
{
	// UI-часть ObjectContainer. Основная часть класса — в ObjectContainer.cs,
	// она компилируется также в сборку без UI. Логика не менялась,
	// код перенесён как есть. См. docs/tasks/web-migration.md, этап 0.
	public partial class ObjectContainer
	{
		public override void DoAction(
			string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.EntityActions.AssignNew:
					AssignNew(owner);
					break;

				case Constants.EntityActions.AssignExisting:
					AssignExisting(owner);
					break;

				case Constants.EntityActions.Refresh:
					Refresh(interfaceObject);
					ClearCache();
					iterator.ClearCache();
					FireContainerRefreshed();
					break;

				case Constants.EntityActions.ShowFilters:
					ShowFilter(owner);
					break;

				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		protected virtual void AssignExisting(IWin32Window owner)
		{
			if (iterator.ChildEntity == null)
				return;

			SelectionForm selector = new SelectionForm(iterator.ChildEntity, "??????? ??????");
			if(selector.ShowDialog(owner) == DialogResult.OK)
			{
				PresentationObject presentationObject = selector.SelectedObject;
				for(int i = 0; i < iterator.ChildEntity.PKColumns.Length; i++)
					presentationObject[iterator.ChildEntity.PKColumns[i]] =
						parameters[iterator.ChildEntity.PKColumns[i]];
				OnObjectCreated(selector.SelectedObject);
			}
		}

		/// <summary>
		/// Creates object of given entity and assignes it to the current oject
		protected virtual void AssignNew(IWin32Window owner)
		{
			if (iterator.ChildEntity == null)
				return;

			PresentationObject newObject = iterator.ChildEntity.NewObject;

			for(int i = 0; i < entity.PKColumns.Length; i++)
				newObject[entity.PKColumns[i]] = parameters[entity.PKColumns[i]];

			newObject[Constants.Parameters.ParentName] = Name;

			if(newObject.ShowPassport(owner))
			{
                if (newObject is IObjectContainer objectContainer)
                {
                    objectContainer.RelationScenario = iterator.RelationScenario;
                    objectContainer.Filter = ObjectsIterator.CacheFilterValues(iterator.Filter);
                }
                newObject.Refresh();
				OnObjectCreated(newObject);
			}
		}

		// Shows filter form and fire ContainerRefreshed event 
		public void ShowFilter(IWin32Window owner)
		{
			// load data to display filter
			Dictionary<string, object> clonedParameters = Parameters;
			DataAccessor.PrepareParameters(clonedParameters, iterator.ChildEntity,
			                               InterfaceObjects.FilterPage, Constants.Actions.Load);

			DataSet ds = null;
			if(DataAccessor.IsProcedureExist(clonedParameters))
			{
				ds = DataAccessor.DoAction(clonedParameters) as DataSet;
			}


            ///TODO: ?????????? ????????? ??????????????? ?????? - ??????? ??? ?????? ?????? ? ?????? - ????? ?? ??? ???? ?? ????????? ???? ? ????
            FilterForm fFilter = new FilterForm(iterator.ChildEntity, ds, iterator.Filter, iterator.ChildEntity.XmlFilter);
			if(fFilter.ShowDialog(owner) == DialogResult.OK)
				FireContainerRefreshed();
		}
	}
}
