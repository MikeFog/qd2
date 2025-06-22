using FogSoft.WinForm.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Merlin.Classes
{
    internal class ActionDeleted : ActionOnMassmedia
    {
        public ActionDeleted() : base(EntityManager.GetEntity((int)Entities.ActionDeleted)) { }
    }
}
