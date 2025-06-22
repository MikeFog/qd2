using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;
using Merlin.Reports;
using static FogSoft.WinForm.Constants;
using static Merlin.Classes.TableColumns;

namespace Merlin.Classes
{
	public class Firm : Organization
	{
		#region Constructors ----------------------------------

		public Firm(int firmID)
			: base(EntityManager.GetEntity((int) Entities.Firm))
		{
			this[ParamNames.FirmId] = firmID;
			isNew = false;
		}

		public Firm() : base(EntityManager.GetEntity((int) Entities.Firm))
		{
		}

		public Firm(DataRow row) : base(EntityManager.GetEntity((int) Entities.Firm), row)
		{
		}

        public Firm(Dictionary<string, object> parameters)
            : base(EntityManager.GetEntity((int)Entities.Firm), parameters)
		{
		}

		public Firm(Entity entity, DataRow row) : base(entity, row)
		{
		}

        #endregion

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
            SelectCampaignsForm fSelector = new SelectCampaignsForm(EntityManager.GetEntity((int)Entities.Agency));
			if(fSelector.ShowDialog(owner) == DialogResult.OK)
			{
                Entity entityBill = EntityManager.GetEntity((int)Entities.GeneralBill);
                foreach (var item in fSelector.SelectedItems)
				{
					Dictionary<string, object> parameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
                    {
                        [TableColumns.Bill.BillDate] = item.date
                    };
                    PresentationObject bill = entityBill.CreateObject(parameters);

                    ContractReport report = new ContractReport(this, (Agency)item.presentationObject, bill, isSponsor);
                    report.Show(isSponsor ? "Спонсорский договор" : "Договор");
                }
            }
        }

        protected override void AssignNew(IWin32Window owner)
		{
			// Create new brand
			PresentationObject brand = EntityManager.GetEntity((int) Entities.Brand).NewObject;

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
				new SelectionForm(EntityManager.GetEntity((int) Entities.Brand), "Брэнды");

			// and assign it to the firm
			if (fSelector.ShowDialog(owner) == DialogResult.OK)
			{
				Application.DoEvents();
				AssignBrand(fSelector.SelectedObject, owner);
			}
		}

		public DataTable GetRollers()
		{
			DataAccessor.PrepareParameters(parameters, EntityManager.GetEntity((int) Entities.Roller),
			                               InterfaceObjects.SimpleJournal, Constants.Actions.Load);
			parameters["ShowInactive"] = false;
			return ((DataSet) DataAccessor.DoAction(parameters)).Tables[Constants.TableNames.Data];
		}

		private void AssignBrand(PresentationObject brand, IWin32Window owner)
		{
			Form ownerForm = (Form) owner;
			try
			{
				ownerForm.Cursor = Cursors.WaitCursor;
				PresentationObject firmBrand = EntityManager.GetEntity((int) Entities.FirmBrand).NewObject;

				firmBrand.Parameters = brand.Parameters;
				firmBrand[ParamNames.FirmId] = IDs[0];
				firmBrand.IsNew = true;

				firmBrand.Update();
				OnObjectCreated(firmBrand);
			}
			finally
			{
				ownerForm.Cursor = Cursors.Default;
			}
		}

		public int FirmId
		{
			get { return int.Parse(IDs[0].ToString()); }
		}

		public static Firm GetFirmById(int firmId)
		{
			Firm firm = new Firm(firmId);
			firm.Refresh();
			return firm;
		}

		public static Firm SelectFirm(IWin32Window owner)
		{
			try
			{
                Application.DoEvents();
                //Cursor.Current = Cursors.WaitCursor;

                Entity entity = EntityManager.GetEntity((int) Entities.Firm);
				Dictionary<string, object> filterValues =
					new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
				if (entity.IsFilterable)
					Globals.ResolveFilterInitialValues(filterValues, entity.XmlFilter);
				SelectionForm fSelector =
					new SelectionForm(entity, entity.GetContent(filterValues).DefaultView, "Фирма-заказчик");

				if (fSelector.ShowDialog(owner) == DialogResult.OK) 
					return (Firm) fSelector.SelectedObject;

				return null;
			}
			finally
			{
				Application.DoEvents();
			}
		}
    }
}