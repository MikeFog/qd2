using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Merlin.Classes
{
    internal class ActionRollerInStatJournal : PresentationObject
    {
        public ActionRollerInStatJournal() : base(EntityManager.GetEntity((int)Entities.RollerStatistic))
        {
        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName.Equals(Action.ActionNames.SetAdvertType, System.StringComparison.OrdinalIgnoreCase))
                SetAdvertType(owner);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }
        private void SetAdvertType(IWin32Window owner)
        {
            try
            {
                if(bool.Parse( parameters[Roller.ParamNames.IsCommon].ToString()) || parameters[Roller.ParamNames.ParentId] != DBNull.Value)
                {
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.ImpossibleSetAdvertType);
                    return;
                }

                Entity entity = EntityManager.GetEntity((int)Entities.AdvertTypeChild);
                SelectionForm form = new SelectionForm(entity, entity.GetContent().DefaultView, "Выбор предмета рекламы");
                if (form.ShowDialog(owner) == DialogResult.OK)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
                    procParameters[Roller.ParamNames.RollerId] = parameters[Roller.ParamNames.RollerId];
                    procParameters[Firm.ParamNames.FirmId] = parameters[Firm.ParamNames.FirmId];
                    procParameters[AdvertType.ParamNames.AdvertTypeId] = form.SelectedObject.IDs[0];
                    procParameters[Roller.ParamNames.IsCommon] = parameters[Roller.ParamNames.IsCommon];
                    procParameters[Roller.ParamNames.IsMute] = parameters[Roller.ParamNames.IsMute];
                    procParameters[Roller.ParamNames.Duration] = parameters[Roller.ParamNames.Duration];

                    DataAccessor.ExecuteNonQuery("ActionRollerSetAdvertType", procParameters);
                    this[Roller.ParamNames.AdvertTypeName] = form.SelectedObject.Name;
                    OnObjectChanged(this);
                }
            }
            finally { Cursor.Current = Cursors.Default; }
        }
    }
}
