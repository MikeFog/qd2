using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Data;
using System.Windows.Forms;

namespace Merlin.Classes
{
    internal class HeadCompanyWithActions : ObjectContainer
    {
        protected const string ShowActionsAction = "ShowActions";
        protected const string ShowFirmsAction = "ShowFirms";

        // Adding a constructor to fix CS1729 error
        public HeadCompanyWithActions(Entity entity) : base(entity) { }

        public HeadCompanyWithActions(Entity entity, DataRow row) : base(entity, row) { }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowActionsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.Action);
                FireContainerRefreshed();
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

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

    internal class HeadCompanyWithConfirmedActions : HeadCompanyWithActions
    {
        public HeadCompanyWithConfirmedActions() : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithConfirmedActions)) { }

        public HeadCompanyWithConfirmedActions(DataRow row) : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithConfirmedActions), row) { }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowFirmsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithConfirmedActions);
                FireContainerRefreshed();
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
    }

    internal class HeadCompanyWithUnconfirmedActions : HeadCompanyWithActions
    {
        public HeadCompanyWithUnconfirmedActions() : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithUnconfirmedActions)) { }

        public HeadCompanyWithUnconfirmedActions(DataRow row) : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithUnconfirmedActions), row) { }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowFirmsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithUnconfirmedActions);
                FireContainerRefreshed();
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
    }

    internal class HeadCompanyWithDeletedActions : HeadCompanyWithActions
    {
        public HeadCompanyWithDeletedActions() : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithDeletedActions)) { }

        public HeadCompanyWithDeletedActions(DataRow row) : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithDeletedActions), row) { }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (string.Equals(actionName, ShowFirmsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.FirmWithDeletedActions);
                FireContainerRefreshed();
            }
            else if (string.Equals(actionName, ShowActionsAction, StringComparison.OrdinalIgnoreCase))
            {
                ChildEntity = EntityManager.GetEntity((int)Entities.ActionDeleted);
                FireContainerRefreshed();
            }
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
    }
}
