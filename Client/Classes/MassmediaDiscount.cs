using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public partial class MassmediaDiscount : ObjectContainer
	{
		private struct Actions
		{
			public const string AssignRelease = "AssignRelease";
		}

		public MassmediaDiscount()
			: base(EntityManager.GetEntity((int)Entities.MassmediaDiscount))
		{
		}

		// DoAction переехал в MassmediaDiscount.WinForms.cs.

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
