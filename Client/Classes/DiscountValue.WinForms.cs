using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть DiscountValue: порог — содержимое набора скидок, поэтому перед записью и удалением
	// показываем акции, в которых набор уже посчитан (DiscountAffectedActionsForm).
	internal partial class DiscountValue
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
