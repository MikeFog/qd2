using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// UI-часть AdvertType: AssignNew (открывает паспорт). Дословный перенос.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class AdvertType
	{
        protected override void AssignNew(IWin32Window owner)
        {
			PresentationObject newObject = iterator.ChildEntity.NewObject;

			newObject[ParamNames.ParentId] = parameters[entity.PKColumns[0]];

			if (newObject.ShowPassport(owner))
			{
				newObject.Refresh();
				OnObjectCreated(newObject);
			}
		}
	}
}
