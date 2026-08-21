using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm
{
	// Часть Interfaces.cs, завязанная на WinForms. Вынесена отдельным файлом,
	// чтобы Interfaces.cs можно было компилировать в сборку без UI
	// (см. FogSoft.Core, docs/tasks/web-migration.md, этап 0).

	public interface IActionHandler
	{
		event ObjectDelegate ObjectCreated;

		Entity.Action[] ActionList { get; }
		bool IsActionEnabled(string actionName, ViewType type);
		bool IsActionHidden(string actionName, ViewType type);
		void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject);
	}

	public delegate Image IconLoaderDelegate(string iconName);
}
