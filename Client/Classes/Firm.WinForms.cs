using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Forms;
using Merlin.Reports;
using static Merlin.Classes.TableColumns;

namespace Merlin.Classes
{
	// UI-часть Firm: диспетчеризация, генерация договора (отчёт), назначение
	// бренда, выбор фирмы. Бизнес-часть (ApplyBrandAssignment,
	// GetFirmCandidates) — в Firm.cs. PrintContract перенесён целиком:
	// генерация отчёта — отдельная область (docs/tasks/web-migration.md,
	// этап 4). Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class Firm
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName.Equals(Action.ActionNames.PrintContract, StringComparison.InvariantCultureIgnoreCase))
				PrintContract(owner, false);
			else if (actionName.Equals(Action.ActionNames.PrintSponsorContract, StringComparison.InvariantCultureIgnoreCase))
				PrintContract(owner, true);
			else
				base.DoAction(actionName, owner, interfaceObject);
		}

		private void PrintContract(IWin32Window owner, bool isSponsor)
		{
			try
			{
				SelectCampaignsForm fSelector = new SelectCampaignsForm(EntityManager.GetEntity((int)Entities.Agency));
				if (fSelector.ShowDialog(owner) == DialogResult.OK)
				{
					((Form)owner).UseWaitCursor = true;
					Application.DoEvents();

					Entity entityBill = EntityManager.GetEntity((int)Entities.GeneralBill);
					foreach (var item in fSelector.SelectedItems)
					{
						Dictionary<string, object> parameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
						{
							[TableColumns.Bill.BillDate] = item.date
						};
						PresentationObject bill = entityBill.CreateObject(parameters);

						ContractReport report = new ContractReport(this, (Agency)item.presentationObject, bill, isSponsor);
						string fileName = string.Format("{0} от {1} для {2}.rtf",
							isSponsor ? "Спонсорский договор" : "Договор",
							((DateTime)bill[TableColumns.Bill.BillDate]).ToString("dd.MM.yyyy"),
							Name);
						report.Show(isSponsor ? "Спонсорский договор" : "Договор", fileName);
					}
				}
			}
			finally { ((Form)owner).UseWaitCursor = false; }
		}

		protected override void AssignNew(IWin32Window owner)
		{
			// Create new brand
			PresentationObject brand = EntityManager.GetEntity((int)Entities.Brand).NewObject;

			// and assign it to the firm
			if (brand.ShowPassport(owner))
			{
				Application.DoEvents();
				AssignBrand(brand, owner);
			}
		}

		protected override void AssignExisting(IWin32Window owner)
		{
			// Show existing brands
			SelectionForm fSelector =
				new SelectionForm(EntityManager.GetEntity((int)Entities.Brand), "Брэнды");

			// and assign it to the firm
			if (fSelector.ShowDialog(owner) == DialogResult.OK)
			{
				Application.DoEvents();
				AssignBrand(fSelector.SelectedObject, owner);
			}
		}

		private void AssignBrand(PresentationObject brand, IWin32Window owner)
		{
			Form ownerForm = (Form)owner;
			try
			{
				ownerForm.Cursor = Cursors.WaitCursor;
				ApplyBrandAssignment(brand);
			}
			finally
			{
				ownerForm.Cursor = Cursors.Default;
			}
		}

		public static Firm SelectFirm(IWin32Window owner)
		{
			try
			{
				Application.DoEvents();
				//Cursor.Current = Cursors.WaitCursor;

				SelectionForm fSelector =
					new SelectionForm(EntityManager.GetEntity((int)Entities.Firm), GetFirmCandidates().DefaultView, "Фирма-заказчик");

				if (fSelector.ShowDialog(owner) == DialogResult.OK)
					return (Firm)fSelector.SelectedObject;

				return null;
			}
			finally
			{
				Application.DoEvents();
			}
		}
	}
}
