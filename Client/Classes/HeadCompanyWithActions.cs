using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Data;

namespace Merlin.Classes
{
    internal partial class HeadCompanyWithActions : ObjectContainer
    {
        protected const string ShowActionsAction = "ShowActions";
        protected const string ShowFirmsAction = "ShowFirms";

        // Adding a constructor to fix CS1729 error
        public HeadCompanyWithActions(Entity entity) : base(entity) { }

        public HeadCompanyWithActions(Entity entity, DataRow row) : base(entity, row) { }

        // DoAction (все 4 класса) переехали в HeadCompanyWithActions.WinForms.cs.

        public override bool IsActionEnabled(string actionName, ViewType type)
        {
            if (string.Equals(actionName, ShowActionsAction, StringComparison.OrdinalIgnoreCase))
            {
                return ChildEntity.Id != (int)Entities.Action && ChildEntity.Id != (int)Entities.ActionDeleted;
            }
            else if (string.Equals(actionName, ShowFirmsAction, StringComparison.OrdinalIgnoreCase))
            {
                return ChildEntity.Id != (int)Entities.FirmWithConfirmedActions && ChildEntity.Id != (int)Entities.FirmWithUnconfirmedActions && ChildEntity.Id != (int)Entities.FirmWithDeletedActions;
            }
            return base.IsActionEnabled(actionName, type);
        }
    }

    internal partial class HeadCompanyWithConfirmedActions : HeadCompanyWithActions
    {
        public HeadCompanyWithConfirmedActions() : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithConfirmedActions)) { }

        public HeadCompanyWithConfirmedActions(DataRow row) : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithConfirmedActions), row) { }

    }

    internal partial class HeadCompanyWithUnconfirmedActions : HeadCompanyWithActions
    {
        public HeadCompanyWithUnconfirmedActions() : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithUnconfirmedActions)) { }

        public HeadCompanyWithUnconfirmedActions(DataRow row) : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithUnconfirmedActions), row) { }

    }

    internal partial class HeadCompanyWithDeletedActions : HeadCompanyWithActions
    {
        public HeadCompanyWithDeletedActions() : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithDeletedActions)) { }

        public HeadCompanyWithDeletedActions(DataRow row) : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithDeletedActions), row) { }

    }
}
