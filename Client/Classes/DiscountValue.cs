using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System.Data;

namespace Merlin.Classes
{
	// Порог набора объёмной скидки (сущность 23, «Сумма скидки»). До предупреждения о затронутых
	// акциях жил голым PresentationObject. UI-часть — в DiscountValue.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class DiscountValue : PresentationObject
	{
		public DiscountValue() : base(EntityManager.GetEntity((int)Entities.DiscountValue))
		{
		}

		public DiscountValue(Entity entity, DataRow row) : base(entity, row)
		{
		}
	}
}
