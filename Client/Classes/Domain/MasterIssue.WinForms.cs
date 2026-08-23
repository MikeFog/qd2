using System.Windows.Forms;
using FogSoft.WinForm;

namespace Merlin.Classes.Domain
{
	// UI-часть MasterIssue: диспетчеризация действий. Переехала целиком —
	// IWin32Window в сигнатуре (структурное ограничение, §8 п.3 конвенции).
	// Логика не менялась. Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class MasterIssue
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.EntityActions.Delete)
				Delete();
		}
	}
}
