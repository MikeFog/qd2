using System.ComponentModel;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Controls
{
	internal abstract partial class TariffGridWithCampaignIssues : TariffGrid
	{
		protected RollerPositions rollerPosition = RollerPositions.Undefined;
        protected AdvertTypePresences _advertTypePresence = AdvertTypePresences.Undefined;
		protected PresentationObject _advertType;
        protected bool isPopUpMenuAllowed = true;
		protected bool excludeSpecialTariffs = true;

        protected TariffGridWithCampaignIssues()
		{
			InitializeComponent();
		}

		public abstract Campaign Campaign { get; set; }

		public bool ExcludeSpecialTariffs
		{
			get { return excludeSpecialTariffs; }
			set { excludeSpecialTariffs = value; }
		}

		[Browsable(false)]
		public RollerPositions RollerPosition
		{
			get { return rollerPosition; }
			set
			{
				rollerPosition = value;
				RefreshGrid();
			}
		}

        public void SetAdvertTypePresence(AdvertTypePresences advertTypePresence, PresentationObject advertType)
        {

			_advertTypePresence = advertTypePresence;
			_advertType = advertType;
			RefreshGrid(); 
        }

        public bool IsPopUpMenuAllowed
		{
			set { isPopUpMenuAllowed = value; }
		}
	}
}