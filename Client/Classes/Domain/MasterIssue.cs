using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms.CreateActionMaster;

namespace Merlin.Classes.Domain
{
	public class MasterIssue : PresentationObject
	{
		public MasterIssue(DataRow row)
			: base(GetEntity(), row)
		{
		}

		public MasterIssue() : base(GetEntity())
		{
		}

		private static Entity GetEntity()
		{
			return EntityManager.GetEntity((int)Entities.MasterIssues);
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.EntityActions.Delete)
			{
				EditIssuesForm form = owner as EditIssuesForm;
				if (form != null)
				{
					form.DeleteIssue(this);
				}
			}
		}
	}
}
