using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Forms.CreateActionMaster;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Merlin.Classes.Domain
{
	public class MasterIssue : PresentationObject
	{
		public MasterIssue(DataRow row) : base(GetEntity(), row)
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

        public override bool Delete()
        {
            ActionOnMassmedia a = new ActionOnMassmedia((int)parameters[Action.ParamNames.ActionId]);
            //DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters);
			//a.Recalculate();	
            return true;
        }
	}
}
