using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using System.Data;

namespace Merlin.Classes
{
	// Радиостанция прайс-листа пакетной скидки (сущность 190). До предупреждения о затронутых
	// акциях жила голым PresentationObject. UI-часть — в PackageDiscountMassmedia.WinForms.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class PackageDiscountMassmedia : PresentationObject
	{
		// Перед записью — акции, в которых скидка уже посчитана и которые правка может задеть (DiscountChangeImpact)
		public override SaveWarning GetSaveWarning()
		{
			return DiscountChangeImpact.ForSave(this);
		}

		public override SaveWarning GetDeleteWarning()
		{
			return DiscountChangeImpact.ForDelete(this);
		}

		public PackageDiscountMassmedia() : base(EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia))
		{
		}

		public PackageDiscountMassmedia(Entity entity, DataRow row) : base(entity, row)
		{
		}
	}
}
