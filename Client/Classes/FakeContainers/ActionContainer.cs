using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Forms.FilterForm;
using System;
using System.Data;
using System.Windows.Forms;
using System.Xml.XPath;

namespace Merlin.Classes.FakeContainers
{
	public class ActionContainer : FakeContainer
	{
		#region Constants -------------------------------------

		private struct ActionNames
		{
			public const string ShowActions = "ShowActions";
			public const string ShowFirms = "ShowFirms";
            public const string ShowHeadCompanies = "ShowHeadCompanies";
        }

		#endregion

		#region Members ---------------------------------------
		private readonly Entity _firmEntity;
        private readonly Entity _actionEntity;
        private readonly Entity _headCompanyEntity;

        private static readonly Entity.Action[] menu = new[]
		{
		new Entity.Action(ActionNames.ShowHeadCompanies, "Акции c разбивкой по группам компаний"),	
		new Entity.Action(ActionNames.ShowFirms, "Акции с разбивкой на фирмы", Constants.ActionsImages.Firm),
		new Entity.Action(ActionNames.ShowActions, "Акции без разбивки на фирмы"),
		new Entity.Action(null, "-"),
		new Entity.Action(Constants.EntityActions.ShowFilters, "Установить фильтр", Constants.ActionsImages.Filter),
		new Entity.Action(Constants.EntityActions.Refresh, "Обновить", Constants.ActionsImages.Refresh)
		};

		#endregion

		#region Constructors ----------------------------------

		public ActionContainer(RelationScenario relationScenario, string caption, Entities firmEntity, Entities actionEntity, Entities headCompanyEntity)
			: base(caption, menu, relationScenario)
		{
			ChildEntity = RootEntity;
			ResolveFilterInitialValues();

			_firmEntity = EntityManager.GetEntity((int)firmEntity);
            _actionEntity = EntityManager.GetEntity((int)actionEntity);
			_headCompanyEntity = EntityManager.GetEntity((int)headCompanyEntity);	
        }

		#endregion

		public override bool IsActionEnabled(string actionName, ViewType type)
		{
			if (actionName == ActionNames.ShowActions)
				return ChildEntity.Id != (int) Entities.Action && ChildEntity.Id != (int)Entities.ActionDeleted;
			if (actionName == ActionNames.ShowFirms)
				return ChildEntity.Id != (int)Entities.FirmWithConfirmedActions && ChildEntity.Id != (int)Entities.FirmWithUnconfirmedActions && ChildEntity.Id != (int)Entities.FirmWithDeletedActions;
			if (actionName == ActionNames.ShowHeadCompanies)
				return ChildEntity.Id != (int)Entities.HeadCompanyWithConfirmedActions && ChildEntity.Id != (int)Entities.HeadCompanyWithUnconfirmedActions && ChildEntity.Id != (int)Entities.HeadCompanyWithDeletedActions;
            return true;
		}

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

        private string GetStartDatePeriodString(object startDateObj, object finishDateObj) 
        {
            if (startDateObj != null && startDateObj != DBNull.Value && DateTime.TryParse(startDateObj.ToString(), out DateTime startDate) &&
                finishDateObj != null && finishDateObj != DBNull.Value && DateTime.TryParse(finishDateObj.ToString(), out DateTime finishDate))
            {
                string formattedStartDate = startDate.ToString("dd.MM.yy");
                string formattedFinishDate = finishDate.ToString("dd.MM.yy");
                return $"[{formattedStartDate}-{formattedFinishDate}]";
            }
            return string.Empty;
        }

        // NEW: пример использования hook из ObjectsIterator
		protected override PresentationObject ProcessCreatedChildObject(PresentationObject childObject, DataRow row)
		{
            // Пример: переименовываем только детей типа "акция"
            if (ChildEntity != null &&
                (ChildEntity.Id == (int)Entities.Action || ChildEntity.Id == (int)Entities.ActionDeleted))
            {
                string actionName = ParseHelper.GetStringFromObject(childObject[Constants.Parameters.Name], string.Empty);
                string firmName = row.Table.Columns.Contains(Merlin.Classes.Action.ParamNames.FirmName)
                    ? ParseHelper.GetStringFromObject(row[Merlin.Classes.Action.ParamNames.FirmName], string.Empty)
                    : string.Empty;

                if (!string.IsNullOrEmpty(firmName) && !string.IsNullOrEmpty(actionName))
                    childObject[Constants.Parameters.Name] = string.Format("{0} ({1}) {2}",
                        actionName,
                        firmName,
                        GetStartDatePeriodString(row[Action.ParamNames.StartDate], row[Action.ParamNames.FinishDate])
                        );
            }
            return childObject;
        }
	}
}