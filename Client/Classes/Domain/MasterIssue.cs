using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
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
				Delete();
		}

		public override bool Delete(bool silenceFlag)
		{
			if (!silenceFlag && !ConfirmDelete())
				return false;

			DataAccessor.ExecuteNonQuery("MasterIssueDelete", parameters);
			OnObjectDeleted(this);
			return true;
		}
	}
}
