using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Forms;
using Merlin.License;

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

		public override bool Update()
		{
			if (IsNew && !AdvertAgLicence.CheckLicenseMassmediasCountForAdd())
				return false;

			if (!base.Update())
				return false;

			// Submit children changes to database 
			foreach (ChildrenChanges childrenChanges in childrenChangesList)
			{
				foreach (PresentationObject po in childrenChanges.AddedObjects)
				{
					MassmediaAgency massmediaAgency =
						new MassmediaAgency(((Agency) po).AgencyId, MassmediaId);
					massmediaAgency.Update();
				}

				foreach (PresentationObject po in childrenChanges.DeletedObjects)
				{
					MassmediaAgency massmediaAgency =
						new MassmediaAgency(((Agency) po).AgencyId, MassmediaId);
					massmediaAgency.Delete(true);
				}
			}
			childrenChangesList.Clear();

			return true;
		}

		public static void LoadRadiostationsByGroup(LookUp cmbRadioStationGroup, SmartGrid grdRadiostations)
		{
            Dictionary<string, object> procParameters = DataAccessor.PrepareParameters(grdRadiostations.Entity);
            int groupId = ParseHelper.GetInt32FromObject(cmbRadioStationGroup.SelectedValue, 0);
            if (groupId > 0)
                procParameters.Add(Massmedia.ParamNames.GroupId, groupId);
            grdRadiostations.DataSource = ((DataSet)DataAccessor.DoAction(procParameters)).Tables[Constants.TableNames.Data].DefaultView;
        }
	}
}
