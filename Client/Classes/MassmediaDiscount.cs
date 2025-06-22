using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public class MassmediaDiscount : ObjectContainer
	{
		private struct Actions
		{
			public const string AssignRelease = "AssignRelease";
		}

		public MassmediaDiscount()
			: base(EntityManager.GetEntity((int)Entities.MassmediaDiscount))
		{
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (string.Compare(actionName, Actions.AssignRelease) == 0)
			{
				Entity child = ChildEntity;
				ChildEntity = EntityManager.GetEntity((int)Entities.DiscountRelease);
				base.DoAction(Constants.EntityActions.AssignNew, owner, interfaceObject);
				ChildEntity = child;
				FireContainerRefreshed();
			}
			else 
				base.DoAction(actionName, owner, interfaceObject);
		}

        public override string Name
        {
            get
            {
				if (StringUtil.IsNullOrEmpty(GroupName))
					return base.Name;
                return base.Name + " (" + GroupName + ")";
            }
        }

        public string GroupName
        {
            get { return this[Massmedia.ParamNames.GroupName].ToString(); }
        }
    }
}
