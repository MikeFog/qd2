using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System.Collections.Generic;
using System.Data;

namespace Merlin.Classes
{
    // UI-часть (DoAction, EditFirms) — в HeadCompany.WinForms.cs.
    // Конвенция — docs/tasks/web-migration-dialogs.md.
    public partial class HeadCompany : ObjectContainer
    {
        public HeadCompany() : base(GetEntity())
        {
            ChildEntity = EntityManager.GetEntity((int)Entities.Firm);
        }

        public HeadCompany(DataRow row) : base(GetEntity(), row)
        {
            ChildEntity = EntityManager.GetEntity((int)Entities.Firm);
        }

        protected HeadCompany(Entity entity, DataRow row) : base(entity, row)
        {
        }

        private HeadCompany(int headCompanyID) : base(GetEntity())
        {
            this[Firm.ParamNames.HeadCompanyID] = headCompanyID;
            isNew = false;
        }

        protected HeadCompany(Entity entity) : base(entity)
        {
        }

        public override DataTable GetContent()
        {
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(ChildEntity);
            procParameters.Add("headCompanyID", IDs[0]);
            procParameters.Add("ShowInactive", 1);

            return ((DataSet)DataAccessor.DoAction(procParameters)).Tables[0];
        }

        // DoAction и EditFirms переехали в HeadCompany.WinForms.cs.

        /// <summary>Кандидаты на переприкрепление к этой головной компании.</summary>
        public DataTable GetFirmsForReassign()
        {
            Entity entity = EntityManager.GetEntity((int)Entities.Firm);
            Dictionary<string, object> filterValues = DataAccessor.CreateParametersDictionary();
            return entity.GetContent(filterValues);
        }

        /// <summary>
        /// Переприкрепляет выбранные фирмы к этой головной компании. true — какая-то из
        /// прежних групп опустела и исчезла (веб перечитывает дерево от родителя).
        /// </summary>
        public bool ApplyFirmsReassign(IList<PresentationObject> items)
        {
            bool anyGroupRemoved = false;
            foreach (var item in items)
            {
                int oldId = (int)item[Firm.ParamNames.HeadCompanyID];
                var hc = HeadCompany.GetObjectById(oldId);
                item[Firm.ParamNames.HeadCompanyID] = IDs[0];
                item.Update();

                if (HeadCompany.GetObjectById(oldId) == null)
                {
                    anyGroupRemoved = true;
                    OnObjectDeleted(hc);
                }
            }
            OnObjectChanged(this);
            return anyGroupRemoved;
        }

        private static Entity GetEntity()
        {
            return EntityManager.GetEntity((int)Entities.HeadCompany);
        }

        public static HeadCompany GetObjectById(int headCompanyId)
        {
            HeadCompany obj = new HeadCompany(headCompanyId);
            return obj.Refresh() ? obj : null;
        }
    }
}
