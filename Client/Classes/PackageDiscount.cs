using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public partial class PackageDiscount : ObjectContainer
	{
		// Открыто ради веба: веб-обработчик действия ссылается на это имя, как у
		// ActionContainer.ActionNames. Само действие — в PackageDiscount.WinForms.cs.
		public struct ActionNames
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
