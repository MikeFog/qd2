using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public partial class Brand : ObjectContainer
	{
		public Brand() : base(GetBrandEntity())
		{
		}

		public Brand(DataRow row) : base(GetBrandEntity(), row)
		{
		}

		// AssignExisting/AssignNew/AssignFirm(2 арг.) переехали в Brand.WinForms.cs.



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