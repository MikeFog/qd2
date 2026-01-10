using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Merlin.Classes
{
    public class HeadCompany : ObjectContainer
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

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if(actionName == "EditFirms")
                EditFirms(owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        private void EditFirms(IWin32Window owner)
        {
            Cursor.Current = Cursors.WaitCursor;
            Entity entity = EntityManager.GetEntity((int)Entities.Firm);
            Dictionary<string, object> filterValues = DataAccessor.CreateParametersDictionary();

            SelectionForm fSelector = new SelectionForm(entity, entity.GetContent(filterValues).DefaultView, "Фирмы-заказчики", true);
            if(fSelector.ShowDialog(owner) == DialogResult.OK)
            {
                foreach (var item in fSelector.AddedItems)
                {
                    int oldId = (int)item[Firm.ParamNames.HeadCompanyID];
                    var hc = HeadCompany.GetObjectById(oldId);
                    item[Firm.ParamNames.HeadCompanyID] = IDs[0];
                    item.Update();
                    
                    if(HeadCompany.GetObjectById(oldId) == null) OnObjectDeleted(hc);
                }
                OnObjectChanged(this);
            }
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
