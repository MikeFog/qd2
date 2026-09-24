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
		public PackageDiscountMassmedia() : base(EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia))
		{
		}

		public PackageDiscountMassmedia(Entity entity, DataRow row) : base(entity, row)
		{
		}
	}
}
