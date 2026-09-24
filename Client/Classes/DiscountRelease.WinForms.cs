using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть DiscountRelease: диспетчеризация действия «Клонировать» (паспорт с
	// предзаполненными значениями). Бизнес-часть (CreateCloneDraft) — в DiscountRelease.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class DiscountRelease
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.EntityActions.Clone)
				CloneRelease(owner);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		// Перед записью — акции, в которых набор уже посчитан и которые правка может задеть
		public override bool Update()
		{
			return DiscountAffectedActionsForm.ConfirmSave(this) && base.Update();
		}

		private void CloneRelease(IWin32Window owner)
		{
			PresentationObject draft = CreateCloneDraft();
			if (draft.ShowPassport(owner))
				OnObjectCloned(draft);
		}
	}
}
