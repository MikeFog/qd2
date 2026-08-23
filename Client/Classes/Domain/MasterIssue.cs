using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System.Data;

namespace Merlin.Classes.Domain
{
	// UI-часть (DoAction) — в MasterIssue.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class MasterIssue : PresentationObject
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

		// DoAction переехал в MasterIssue.WinForms.cs (IWin32Window в сигнатуре).

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
