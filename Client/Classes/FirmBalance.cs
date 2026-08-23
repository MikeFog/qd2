using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public abstract partial class FirmBalance : PresentationObject
	{
		public FirmBalance(Entity entity)
			: base(entity)
		{
		}

		public FirmBalance(Entity entity, DataRow row)
			: base(entity, row)
		{
		}

		// DoAction и объявление Jump2FirmBalanceJournal(IWin32Window) переехали
		// в FirmBalance.WinForms.cs — оба типизированы на UI.


		public int FirmID
		{
			get { return int.Parse(parameters["firmID"].ToString()); }
		}
	}
}