using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using System.Data;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть Massmedia: диспетчеризация действий. Переехала целиком —
	// IWin32Window в сигнатуре (структурное ограничение, §8 п.3 конвенции).
	// Логика не менялась. Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Massmedia
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch (actionName)
			{
				case ActionNames.AddSponsorProgram:
				case ActionNames.AddDisabledWindow:
				case ActionNames.AddPriceList:
				case ActionNames.AddModule:
                case ActionNames.AssignRelease:
                    base.DoAction(Constants.EntityActions.AssignNew, owner, interfaceObject);
					break;
				default:
					base.DoAction(actionName, owner, interfaceObject);
					break;
			}
		}

		/// <summary>Возвращает UI-тип PassportForm, поэтому здесь, а не в ядре
		/// (тот же случай, что базовый PresentationObject.GetPassportForm, этап 0.1).</summary>
		public override PassportForm GetPassportForm(DataSet ds)
		{
			return new MassmediaPassport(this, ds);
		}
	}
}
