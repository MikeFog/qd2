using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
    internal partial class CommonRoller : ActionRoller
    {
        public CommonRoller() : base(EntityManager.GetEntity((int)Entities.CommonRollers))
        {
        }

        // DoAction переехал в CommonRoller.WinForms.cs.

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
