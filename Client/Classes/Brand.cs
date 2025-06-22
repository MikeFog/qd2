using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public class Brand : ObjectContainer
	{
		public Brand() : base(GetBrandEntity())
		{
		}

		public Brand(DataRow row) : base(GetBrandEntity(), row)
		{
		}

		protected override void AssignExisting(IWin32Window owner)
		{
			PresentationObject firm = Firm.SelectFirm(owner);

			if (firm != null)
				AssignFirm(firm, owner);
		}

		protected override void AssignNew(IWin32Window owner)
		{
			PresentationObject firm = EntityManager.GetEntity((int) Entities.Firm).NewObject;

			if (firm.ShowPassport(owner))
			{
				Application.DoEvents();
				AssignFirm(firm, owner);
			}
		}

		private void AssignFirm(PresentationObject firm, IWin32Window owner)
		{
			Form ownerForm = (Form) owner;
			try
			{
				Application.DoEvents();
				ownerForm.Cursor = Cursors.WaitCursor;

                AssignFirm(firm);
			}
			finally
			{
				ownerForm.Cursor = Cursors.Default;
			}
		}

        public void AssignFirm(PresentationObject firm)
        {
            PresentationObject brandFirm = EntityManager.GetEntity((int)Entities.BrandFirm).NewObject;
            brandFirm.Parameters = firm.Parameters;
            brandFirm["brandID"] = IDs[0];
            brandFirm.IsNew = true;

            brandFirm.Update();
            OnObjectCreated(brandFirm);
        }

		private static Entity GetBrandEntity()
		{
			return EntityManager.GetEntity((int) Entities.Brand);
		}
	}
}