using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Controls
{

    enum TariffGridRefreshMode
    {
        None,
        WithAdd,
        WithDelete
    }
    internal interface IRollerGrid
	{
		Roller Roller { get; set;}
		RollerPositions RollerPosition { get; set;}
		PresentationObject Module { get;set;}
		void RefreshCurrentCell(bool hasCurrentCampaignIssues, TariffGridRefreshMode mode);
		SecurityManager.User Grantor { get; set;}

		void SetAdvertTypePresence(AdvertTypePresences advertTypePresence, PresentationObject advertType);
	}
}