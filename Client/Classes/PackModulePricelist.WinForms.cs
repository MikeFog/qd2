using System.Windows.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть PackModulePricelist: открытие редактора содержимого пакета.
	// Дословный перенос из PackModulePricelist.cs, логика не менялась.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class PackModulePricelist
	{
		public void EditContent(Form owner)
		{
			FrmPackModuleContent fContent = new FrmPackModuleContent(this);
			fContent.ShowDialog(owner);
		}
	}
}
