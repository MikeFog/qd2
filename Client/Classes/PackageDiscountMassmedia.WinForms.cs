using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть PackageDiscountMassmedia: станция — условие пакета, поэтому перед правкой и удалением
	// показываем акции, в которых прайс-лист уже посчитан (DiscountAffectedActionsForm).
	// Добавление станций идёт мимо этого класса (PackageDiscountPriceList.ApplyRadioStationsAssignment)
	// и посчитанные акции не задевает.
	internal partial class PackageDiscountMassmedia
	{
		public override bool Update()
		{
			return DiscountAffectedActionsForm.ConfirmSave(this) && base.Update();
		}

		protected override bool ConfirmDelete()
		{
			return base.ConfirmDelete() && DiscountAffectedActionsForm.ConfirmDelete(this);
		}
	}
}
