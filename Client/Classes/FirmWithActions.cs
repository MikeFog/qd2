using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Data;

namespace Merlin.Classes
{
    internal abstract class FirmWithActions : ObjectContainer
    {
        protected FirmWithActions(Entity entity) : base(entity)
        {
        }

        protected FirmWithActions(Entity entity, DataRow row) : base(entity, row)
        {
        }

        protected override PresentationObject ProcessCreatedChildObject(PresentationObject childObject, DataRow row)
        {
            if (ChildEntity != null &&
                (ChildEntity.Id == (int)Entities.Action || ChildEntity.Id == (int)Entities.ActionDeleted))
            {
                string actionName = ParseHelper.GetStringFromObject(childObject[Constants.Parameters.Name], string.Empty);
                ((Action)childObject).SetName(Action.CreateNameWithStartDatePeriod(actionName, row));
            }

            return childObject;
        }
    }

    internal class FirmWithConfirmedActions : FirmWithActions
    {
        public FirmWithConfirmedActions() : base(EntityManager.GetEntity((int)Entities.FirmWithConfirmedActions))
        {
        }

        public FirmWithConfirmedActions(Entity entity) : base(entity)
        {
        }

        public FirmWithConfirmedActions(Entity entity, DataRow row) : base(entity, row)
        {
        }
    }

    internal class FirmWithUnconfirmedActions : FirmWithActions
    {
        public FirmWithUnconfirmedActions() : base(EntityManager.GetEntity((int)Entities.FirmWithUnconfirmedActions))
        {
        }

        public FirmWithUnconfirmedActions(Entity entity) : base(entity)
        {
        }

        public FirmWithUnconfirmedActions(Entity entity, DataRow row) : base(entity, row)
        {
        }
    }

    internal class FirmWithDeletedActions : FirmWithActions
    {
        public FirmWithDeletedActions() : base(EntityManager.GetEntity((int)Entities.FirmWithDeletedActions))
        {
        }

        public FirmWithDeletedActions(Entity entity) : base(entity)
        {
        }

        public FirmWithDeletedActions(Entity entity, DataRow row) : base(entity, row)
        {
        }
    }
}
