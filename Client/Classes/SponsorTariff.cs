using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	// ShowPassport переехал в SponsorTariff.WinForms.cs. Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class SponsorTariff : PresentationObject
	{
		public SponsorTariff() : base(EntityManager.GetEntity((int)Entities.SponsorTariff))
		{
		}

        public SponsorTariff(DataRow row) : base(EntityManager.GetEntity((int)Entities.SponsorTariff), row)
        {
        }

		public int TariffId
		{
			get { return int.Parse(this[Tariff.ParamNames.TariffId].ToString()); }
		}

        internal decimal Price
        {
            get { return decimal.Parse(this[Tariff.ParamNames.Price].ToString()); }
        }

		// ShowPassport переехал в SponsorTariff.WinForms.cs.
	}
}
