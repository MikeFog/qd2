using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

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

		private void CloneRelease(IWin32Window owner)
		{
			DiscountRelease draft = CreateCloneDraft();
			if (draft.ShowPassport(owner))
				OnObjectCloned(draft);
		}
	}
}
