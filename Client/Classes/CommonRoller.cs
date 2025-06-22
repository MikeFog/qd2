using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Merlin.Classes
{
    internal class CommonRoller : ActionRoller
    {
        public CommonRoller() : base(EntityManager.GetEntity((int)Entities.CommonRollers))
        {
        }

        public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
        {
            if (actionName.Equals(Action.ActionNames.SetAdvertType, System.StringComparison.OrdinalIgnoreCase))
                SetAdvertType(owner, false);
            else
                base.DoAction(actionName, owner, interfaceObject);
        }

        protected override ActionRoller CreateNewRoller(Roller roller)
        {
            return new CommonRoller
            {
                parameters = roller.Parameters,
                isNew = false
            };
        }
    }
}
