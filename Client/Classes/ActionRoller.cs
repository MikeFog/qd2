using FogSoft.WinForm.Classes;
using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm;
using Microsoft.Office.Core;
using unoidl.com.sun.star.sheet;

namespace Merlin.Classes
{
    internal class ActionRoller : Roller
    {
        public ActionRoller() : base(EntityManager.GetEntity((int)Entities.ActionRollers))
        {
        }

        protected ActionRoller(Entity entity) : base(entity) { }

        public ActionRoller(PresentationObject roller) : this()
        {
            parameters = roller.Parameters;
            isNew = false;
        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName.Equals(Action.ActionNames.SetAdvertType, System.StringComparison.OrdinalIgnoreCase))
                SetAdvertType(owner, true);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        protected void SetAdvertType(IWin32Window owner, bool changeFlag)
        {
            try
            {
                Entity entity = EntityManager.GetEntity((int)Entities.AdvertTypeChild);
                SelectionForm form = new SelectionForm(entity, entity.GetContent().DefaultView, "Выбор предмета рекламы");
                if (form.ShowDialog(owner) == DialogResult.OK)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
                    procParameters[Roller.ParamNames.RollerId] = this[Roller.ParamNames.RollerId];
                    procParameters[Action.ParamNames.ActionId] = this[Action.ParamNames.ActionId];
                    procParameters[Firm.ParamNames.FirmId] = this[Firm.ParamNames.FirmId];
                    procParameters[AdvertType.ParamNames.AdvertTypeId] = form.SelectedObject.IDs[0];
                    procParameters[Roller.ParamNames.IsCommon] = this[Roller.ParamNames.IsCommon];
                    procParameters[Roller.ParamNames.IsMute] = this[Roller.ParamNames.IsMute];
                    procParameters[Roller.ParamNames.Duration] = this[Roller.ParamNames.Duration];
                    procParameters["changeFlag"] = changeFlag;

                    DataAccessor.ExecuteNonQuery("ActionRollerSetAdvertType", procParameters);
                    int newRollerId = int.Parse(procParameters["newRollerID"].ToString());
                    
                    // если была информация о том сколько раз использовался этот ролик, надо ее сохранить и добавить в новый объект
                    int count = -1;
                    if (parameters.ContainsKey("count"))
                        count = (int)parameters["count"];

                    // если назначили предмет рекламы ролику "для всех фирм", то создастся его "клон" и вернется ID нового ролика
                    if (RollerId == newRollerId)
                    {
                        Refresh();
                        if(count >= 0) this["count"] = count;
                        OnObjectChanged(this);
                    }
                    else
                    {
                        Roller roller = new Roller(newRollerId);
                        // скопируем из старого ролика количество выходов
                        if (count >= 0) roller["count"] = count;

                        OnObjectCloned(CreateNewRoller(roller));
                        OnObjectDeleted(this);
                    }
                }
            }
            finally { Cursor.Current = Cursors.Default; }
        }

        protected virtual ActionRoller CreateNewRoller(Roller roller)
        {
            return new ActionRoller
            {
                parameters = roller.Parameters,
                isNew = false
            };
        }
    }
}
