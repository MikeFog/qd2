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

                if (!string.IsNullOrEmpty(actionName))
                {
                    childObject[Constants.Parameters.Name] = string.Format(
                        "{0} {1}",
                        actionName,
                        GetStartDatePeriodString(row[Action.ParamNames.StartDate], row[Action.ParamNames.FinishDate]));
                }
            }

            return childObject;
        }

        private string GetStartDatePeriodString(object startDateObj, object finishDateObj)
        {
            if (startDateObj != null && startDateObj != DBNull.Value && DateTime.TryParse(startDateObj.ToString(), out DateTime startDate) &&
                finishDateObj != null && finishDateObj != DBNull.Value && DateTime.TryParse(finishDateObj.ToString(), out DateTime finishDate))
            {
                return string.Format("[{0:dd.MM.yy}-{1:dd.MM.yy}]", startDate, finishDate);
            }

            return string.Empty;
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
