using System.Data;
using System.Windows.Forms;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;
using FogSoft.WinForm.Classes;

namespace Protector.Domain
{
	public class GroupMember : User
	{
		public GroupMember() : base(EntityManager.GetEntity((int)Entities.GroupMember)) { }
		public GroupMember(DataRow row) : base(EntityManager.GetEntity((int)Entities.GroupMember), row) { }
		public GroupMember(Entity entity, DataRow row) : base(entity, row) { }	

		private const string DETACH_PROMPT = "Удалить пользователя '{0}' из состава группы?";

		public override void Detach()
		{
			if(MessageBox.ShowQuestion(string.Format(DETACH_PROMPT, Name)) == DialogResult.Yes)
				base.Detach(true);
		}
	}
}