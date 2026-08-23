using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Data;

namespace Merlin.Classes.FakeContainers
{
	public partial class ActionContainer : FakeContainer
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

		// DoAction/ShowFilter переехали в ActionContainer.WinForms.cs.


        // NEW: пример использования hook из ObjectsIterator
		protected override PresentationObject ProcessCreatedChildObject(PresentationObject childObject, DataRow row)
		{
            // Пример: переименовываем только детей типа "акция"
            if (ChildEntity != null &&
                (ChildEntity.Id == (int)Entities.Action || ChildEntity.Id == (int)Entities.ActionDeleted))
            {
                string actionName = ParseHelper.GetStringFromObject(childObject[Constants.Parameters.Name], string.Empty);
                ((Action)childObject).SetName(Action.CreateNameWithFirmAndStartDatePeriod(actionName, row));
            }
            return childObject;
        }
	}
}