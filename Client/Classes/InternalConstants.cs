namespace Merlin
{
	public enum RollerPositions
	{
		Undefined = 0,
		First = -20,
		FirstTransferred = -15,
		Second = -10,
		SecondTransferred = -5,
		LastTransferred = 5,
		Last = 10
	}

    public enum AdvertTypePresences
    {
        Undefined = 0,
        Exist = 5,
        NotExist = 10
    }

    public enum Entities
	{
		Bank = 2,
		RolStyle = 4,
		PaymentType = 5,
		Agency = 8,
		MassMedia = 9,
		DisabledWindow = 10,
		SponsorProgram = 11,
		SponsorPricelist = 12,
		SponsorTariff = 14,
		Brand = 15,
		Firm = 16,
		AdvertType = 17,
		ProductionStudio = 19,
		Roller = 20,
		MassmediaDiscount = 21,
		DiscountRelease = 22,
		DiscountValue = 23,
		Action = 77,
		CampaignOnMassmedia = 78,
		CampaignPart = 79,
		Pricelist = 80,
		Tariff = 81,
		Module = 82,
		ModulePricelist = 83,
		ModuleTariff = 84,
		ProgramPart = 86,
		RollerPart = 87,
		SponsorCampaignDay = 88,
		SponsorCampaignProgram = 89,
		ProgramIssue = 90,
		GeneralCampaign = 91,
		ModuleCampaign = 92,
		SponsorCampaign = 93,
		CampaignDay = 94,
		CampaignRoller = 95,
		CampaignModule = 96,
		CampaignRollerInsideDay = 97,
		Issue = 98,
		MassmediaAgency = 99,
		GridCell = 100,
		StudioPricelist = 113,
		StudioOrderAction = 116,
		StudioOrder = 117,
		FirmWithConfirmedActions = 118,
		FirmWithOrders = 119,
		BrandFirm = 120,
		FirmBrand = 121,
		StudioAgency = 123,
		PaymentStudioOrder = 124,
		PaymentStudioOrderAction = 125,
		StudioOrderActionPaymentCandidate = 126,
		BalanceStudioOrder = 127,
		StudioOrderBill = 128,
		ConfirmationHistory = 129,
		ModuleIssue = 130,
		PackModule = 133,
		PackModulePricelist = 134,
		PackModuleContent = 135,
		CampaignDayForRoller = 136,
		FirmWithUnconfirmedActions = 137,
		RollerIssueActivated = 138,
		RollerStatistic = 139,
		TransferLog = 141,
		AgencyTax = 143,
		TariffWindow = 144,
		PaymentCommon = 145,
		PaymentCommonAction = 146,
		ActionPaymentCandidate = 147,
		GeneralBill = 148,
		ModuleCampaignDay = 154,
		ActJournalRow = 156,
		ErrTmplGen = 157,
		StatsVolumeofRealization = 158,
		StatsVolumeofRealization4Rollers = 159,
		StatsBalance = 160,
		StatsBalanceGroup = 185,
		StatsBalanceAgency = 161,
		StatsFillPercentage = 163,
		StatsBalanceManager = 168,
		StatsBalanceManagerOrder = 169,
		StatsSponsorBusiness = 170,
		StatsRollersCreated = 186,
        StatsFactorAnalysis = 225,
		PackModuleCampaign = 171,
		PackModuleIssue = 175,
		PackModuleInCampaign = 177,
		Announcement = 179,
        PackCampaignDay = 180,
        CampaignModuleDay = 181,
		SponsorCampaignDayInProgramm = 182,
		SponsorCampaignProgramInDay = 183,
		BalanceIssues = 184,
		SpecialTariffWindow = 187,
		MassmediasWithCampaigns = 188,
		PackageDiscount = 189,
		PackageDiscountMassmedia = 190,
		PackageDiscountPriceLists = 191,
		LogDeletedIssue = 193,
        TariffWindowTM = 194,
		MassmediaGroup = 195,
		MassmediaGroupMember = 196,
		User = 199,
		StudioOrderActJournal = 200,
		StatVolumeOfRealiztionByMonth = 201,
		StatModuleLoading = 202,
		StatModuleFinancy = 203,
		StatPackModuleLoading = 204,
		StatPackModuleFinancy = 205,
		CampaignIssuesTransfers = 206,
		SpecialAction = 207,
		SpecialStudioOrderAction = 208,
		ActionRollersStat = 209,
		MuteRoller = 211,
		RollerUnSubtitude = 212,
		CampaignModuleRollerInsideDay = 215,
		CampaignModuleRollerIssue = 216,
		ImportedIssuesResult = 217,
		StatAvgDiscount = 218,
		StatVolumeOfRealizationSec = 219,
		StatVolumeByPaymentType = 222,
		MasterIssues = 226,
        ImportFirms = 227,
        ImportRollers = 228,
		ReportPartText = 1227,
        FirmWithDeletedActions = 1229,
		ActionDeleted = 1236,
		AdvertTypeChild = 1243,
        ActionRollers = 1244,
        CommonRollers = 1246,
        PackModuleIssueInCampaignForm = 1247,
        HeadCompany = 1248,
    }

	public struct RelationScenarios
	{
		public const string StoredProcedures = "Stored procedures";
		public const string Tariff = "Tariff";
		public const string SponsorProgramm = "Sponsor programm";
		public const string AdvertTypes = "Предметы рекламы";
		public const string DisabledWindows = "Disabled windows";
		public const string Discount = "Discount";
		public const string PackageDiscount = "PackageDiscount";
		public const string ConfirmedAction = "ConfirmedAction";
        public const string UnconfirmedAction = "UnconfirmedAction";
        public const string DeletedAction = "DeletedAction";
		public const string Module = "Module";
		public const string UsedSponsorPrograms = "Used Sponsor Programs";
		public const string StudioTariff = "Тарифы на производство роликов";
		public const string ProductionAction = "ProductionAction";
		public const string MassmediaAndCampaign = "Massmedia and Campaigns";
		public const string Massmedia = "Massmedia";
		public const string ModuleIssues = "Module Issues";
		public const string PackModules = "Pack Modules";
		public const string TariffWindows = "Tariff Windows";
	}

    internal enum SelectionMode
    {
        Split,
        Clone,
        Agency
    }
}