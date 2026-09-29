using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Data;

namespace Merlin.Classes
{
    internal abstract partial class HeadCompanyWithActions : ObjectContainer
    {
        protected const string ShowActionsAction = "ShowActions";
        protected const string ShowFirmsAction = "ShowFirms";

        // Adding a constructor to fix CS1729 error
        public HeadCompanyWithActions(Entity entity) : base(entity) { }

        public HeadCompanyWithActions(Entity entity, DataRow row) : base(entity, row) { }

        // DoAction (все 4 класса) — в HeadCompanyWithActions.WinForms.cs; само переключение
        // вида узла — здесь (ShowActions/ShowFirms), его зовёт и десктоп, и веб.

        /// <summary>«Показать акции»: дочерние узлы — акции группы компаний.</summary>
        internal virtual void ShowActions()
        {
            ChildEntity = EntityManager.GetEntity((int)Entities.Action);
            FireContainerRefreshed();
        }

        /// <summary>«Показать фирмы»: дочерние узлы — фирмы группы с акциями этого журнала.</summary>
        internal void ShowFirms()
        {
            ChildEntity = EntityManager.GetEntity((int)FirmsEntity);
            FireContainerRefreshed();
        }

        /// <summary>Сущность фирм своего журнала (подтверждённые, макеты, удалённые).</summary>
        protected abstract Entities FirmsEntity { get; }

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

        protected override Entities FirmsEntity => Entities.FirmWithConfirmedActions;

    }

    internal partial class HeadCompanyWithUnconfirmedActions : HeadCompanyWithActions
    {
        public HeadCompanyWithUnconfirmedActions() : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithUnconfirmedActions)) { }

        public HeadCompanyWithUnconfirmedActions(DataRow row) : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithUnconfirmedActions), row) { }

        protected override Entities FirmsEntity => Entities.FirmWithUnconfirmedActions;

    }

    internal partial class HeadCompanyWithDeletedActions : HeadCompanyWithActions
    {
        public HeadCompanyWithDeletedActions() : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithDeletedActions)) { }

        public HeadCompanyWithDeletedActions(DataRow row) : base(EntityManager.GetEntity((int)Entities.HeadCompanyWithDeletedActions), row) { }

        protected override Entities FirmsEntity => Entities.FirmWithDeletedActions;

        /// <summary>В журнале удалённых акции — удалённые.</summary>
        internal override void ShowActions()
        {
            ChildEntity = EntityManager.GetEntity((int)Entities.ActionDeleted);
            FireContainerRefreshed();
        }
    }

    /// <summary>
    /// «Показать фирмы / Показать акции» у узла группы компаний в журналах акций — вход для
    /// веба: классы HeadCompanyWith* internal.
    /// </summary>
    public static class HeadCompanyView
    {
        public const string ShowFirmsAction = "ShowFirms";
        public const string ShowActionsAction = "ShowActions";

        public static void ShowFirms(PresentationObject headCompany) => ((HeadCompanyWithActions)headCompany).ShowFirms();

        public static void ShowActions(PresentationObject headCompany) => ((HeadCompanyWithActions)headCompany).ShowActions();
    }
}
