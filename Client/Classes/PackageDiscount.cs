using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public partial class PackageDiscount : ObjectContainer
	{
		// Перед записью — акции, в которых скидка уже посчитана и которые правка может задеть (DiscountChangeImpact)
		public override SaveWarning GetSaveWarning()
		{
			return DiscountChangeImpact.ForSave(this);
		}

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
