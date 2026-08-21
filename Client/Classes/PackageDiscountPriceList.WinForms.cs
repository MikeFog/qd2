using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть PackageDiscountPriceList. ValidatePassportData и
	// ApplyPassportData — делегаты контракта UniversalPassportForm
	// (ValidateDataDelegate/ApplyChangesDelegate), поэтому их сигнатуры
	// (Dictionary<string,object> parameters) не меняются. Бизнес-часть записи
	// (ApplyRadioStationsAssignment) — в PackageDiscountPriceList.cs.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class PackageDiscountPriceList
	{
		protected override void AssignNew(IWin32Window owner)
		{
			if (GetContent().Rows.Count == 0)
				AssignMany((Form)owner);
			else
				base.AssignNew(owner);
		}

		private void AssignMany(Form owner)
		{
			DataTable dt = EntityManager.GetEntity((int)Entities.MassMedia).GetContent();
			dt.TableName = "massmedia";
			DataSet ds = new DataSet();
			ds.Tables.Add(dt.Copy());
			childrenChangesList.Clear();
			UniversalPassportForm frm = new UniversalPassportForm(this, "PackDiscountRadiostations", "Радиостанции", EntityManager.GetEntity((int)Entities.PackageDiscountMassmedia),
				ds, ValidatePassportData, ApplyPassportData);
			if (frm.ShowDialog(owner) == DialogResult.OK)
				FireContainerRefreshed();
		}

		private bool ValidatePassportData(Dictionary<string, object> parameters)
		{

			if (!(bool)parameters[ParamNames.isForType1] && !(bool)parameters[ParamNames.isForType2] && !(bool)parameters[ParamNames.isForType3])
			{
				UserMessage.ShowExclamation(Properties.Resources.NoCampaignTypeSelected);
				return false;
			}

			if (SelectedRadioStations.Count == 0)
			{
				UserMessage.ShowExclamation(Properties.Resources.NoRadiostationSelected);
				return false;
			}

			return true;
		}

		private void ApplyPassportData(Dictionary<string, object> parameters)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				ApplyRadioStationsAssignment(parameters);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}
	}
}
