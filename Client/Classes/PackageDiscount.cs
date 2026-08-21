using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public partial class PackageDiscount : ObjectContainer
	{
		private struct Actions
		{
			//public const string AssignMassmedia = "AssignMassmedia";
			public const string AssignPriceList = "AssignPriceList";
		}

		public PackageDiscount() 
			: base(EntityManager.GetEntity((int)Entities.PackageDiscount))
		{
		}

		// DoAction переехал в PackageDiscount.WinForms.cs.
	}
}
