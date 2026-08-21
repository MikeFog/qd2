using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;
using System.Data;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть Tariff: диспетчеризация действий (клонирование тарифа через
	// паспорт). Переехала целиком — IWin32Window в сигнатуре (§8 п.3).
	// Логика не менялась. Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Tariff
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			switch(actionName)
			{
				case Constants.Actions.Clone:
                    Tariff tariff = new Tariff
                    {
                        parameters = Parameters
                    };
                    tariff.parameters[ParamNames.TariffId] = null;
					tariff.parameters[Constants.ParamNames.ActionName] = Constants.Actions.AddItem;

					if (tariff.ShowPassport(owner))
						//OnObjectCreated(tariff);
						OnObjectCloned(tariff);
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
			return new TariffPassport(this, ds);
		}
	}
}
