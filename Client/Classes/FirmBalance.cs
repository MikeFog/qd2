using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public abstract class FirmBalance : PresentationObject
	{
		public FirmBalance(Entity entity)
			: base(entity)
		{
		}

		public FirmBalance(Entity entity, DataRow row)
			: base(entity, row)
		{
		}

		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == "Jump2FirmBalance")
				Jump2FirmBalanceJournal(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		protected abstract void Jump2FirmBalanceJournal(IWin32Window owner);

		public int FirmID
		{
			get { return int.Parse(parameters["firmID"].ToString()); }
		}
	}
}