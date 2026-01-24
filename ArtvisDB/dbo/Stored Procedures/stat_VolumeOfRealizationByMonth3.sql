
CREATE Procedure [dbo].[stat_VolumeOfRealizationByMonth3]
(
    @StartDay DATETIME = NULL,
    @FinishDay DATETIME = NULL,
    @FirmID smallint = NULL, 
    @headCompanyID smallint = NULL, 
    @MassmediaID smallint = NULL, 
    @PaymentTypeID smallint = NULL,
    @CampaignTypeID tinyint = NULL,
    @ManagerID smallint = NULL,
    @AgencyID smallint = NULL, 
    @massmediaGroupID int = NULL,
    @advertTypeID smallint = NULL,
    @IsGroupByMassmedia bit = 0,
    @IsGroupByPaymentType bit = 0,
    @IsGroupByCampaignType bit = 0,
    @IsGroupByManager bit = 0,
    @IsGroupByAgency bit = 0,
    @IsGroupByFirm bit = 0,
    @IsGroupByHeadCompany bit = 0, 
    @IsGroupByMassmediaGroupType bit = 0,
    @IsGroupByAdvertType bit = 0,
    @IsGroupByAdvertTypeTop bit = 0,
    @ShowWhite bit = 1,
    @ShowBlack bit = 1,
    @loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    IF @IsGroupByHeadCompany = 1 SET @IsGroupByFirm = 0;

    IF @StartDay IS NULL OR @FinishDay IS NULL
    BEGIN
        RAISERROR('FilterStartFinishDays', 16, 1);
        RETURN;
    END

    SET @StartDay  = dbo.ToShortDate(@StartDay);
    SET @FinishDay = dbo.ToShortDate(@FinishDay);

    --------------------------------------------------------------------
    -- Источник данных: 1:1 с fn_statGetPriceByMonth
    --------------------------------------------------------------------
    CREATE TABLE #Campaign
    (
        y smallint NOT NULL,
        m tinyint NOT NULL,
        campaignID int NOT NULL, 
        advertTypeID smallint NULL,
        actionID int NOT NULL, 
        massmediaID smallint NOT NULL, 
        paymentTypeID smallint NOT NULL, 
        campaignTypeID tinyint NOT NULL, 
        agencyID smallint NULL,
        startDate datetime NOT NULL,
        finishDate datetime NOT NULL,
        finalPrice money NULL,
        userID smallint NOT NULL,
        firmID smallint NOT NULL,
        discount float NULL,
        massmediaGroupID int NULL,
        price money NOT NULL
    );

    INSERT INTO #Campaign
    EXEC dbo.stat_GetPriceByMonth_proc
         @StartDay,
         @FinishDay,
         @loggedUserID;

    CREATE UNIQUE CLUSTERED INDEX IX_Campaign
        ON #Campaign (y, m, campaignID, massmediaID, advertTypeID);

    --------------------------------------------------------------------
    -- output ---------------------------------------------------------
    --------------------------------------------------------------------
    DECLARE @SQLString NVARCHAR(MAX), @IsStarted int;

    SET @SQLString = N'
DECLARE @Summa money;
SET @Summa = 0;

SELECT @Summa = ISNULL(SUM(d.price), 0)
FROM #Campaign d
INNER JOIN Firm f ON f.firmId = d.firmId
';

    IF @ShowWhite = 0 OR @ShowBlack = 0
        SET @SQLString += N'INNER JOIN Paymenttype pt ON d.PaymentTypeID = pt.PaymenttypeID
';

    IF @advertTypeID IS NOT NULL OR @IsGroupByAdvertTypeTop <> 0
        SET @SQLString += N'LEFT JOIN AdvertType at ON at.advertTypeID = d.advertTypeID
';

    SET @SQLString += N'WHERE 1=1
';

    IF @ShowWhite = 0 AND @ShowBlack <> 0 SET @SQLString += N' AND pt.isHidden <> 0
';
    IF @ShowBlack = 0 AND @ShowWhite <> 0 SET @SQLString += N' AND pt.isHidden = 0
';
    IF @FirmID IS NOT NULL SET @SQLString += N' AND d.firmID = @FirmID
';
    IF @headCompanyID IS NOT NULL SET @SQLString += N' AND f.headCompanyID = @headCompanyID
';
    IF @MassmediaID IS NOT NULL SET @SQLString += N' AND d.massmediaID = @MassmediaID
';
    IF @PaymentTypeID IS NOT NULL SET @SQLString += N' AND d.paymentTypeID = @PaymentTypeID
';
    IF @CampaignTypeID IS NOT NULL SET @SQLString += N' AND d.campaignTypeID = @CampaignTypeID
';
    IF @ManagerID IS NOT NULL SET @SQLString += N' AND d.userID = @ManagerID
';
    IF @AgencyID IS NOT NULL SET @SQLString += N' AND d.agencyID = @AgencyID
';
    IF @massmediaGroupID IS NOT NULL SET @SQLString += N' AND d.massmediaGroupID = @massmediaGroupID
';
    IF @advertTypeID IS NOT NULL SET @SQLString += N' AND @advertTypeID IN(at.parentID, d.advertTypeID)
';

    --------------------------------------------------------------------
    -- SELECT часть
    --------------------------------------------------------------------
    SET @SQLString += N'
SELECT
    ROW_NUMBER() OVER(ORDER BY d.y, d.m) AS RowNum,
    MAX(iMonthName.name) + SPACE(1) + CAST(d.[y] AS varchar) + '' г.'' AS [period],
';

    IF @IsGroupByMassmedia <> 0
        SET @SQLString += N'vMassMedia.NameWithGroup as mmName,  ';
    IF @IsGroupByFirm <> 0
        SET @SQLString += N'f.Name as firmName,';
    IF @IsGroupByHeadCompany <> 0
        SET @SQLString += N'hc.Name as head_company,';
    IF @IsGroupByManager <> 0
        SET @SQLString += N'COALESCE(u.LastName, '''') + COALESCE(SPACE(1) + u.FirstName, '''') as manager,';
    IF @IsGroupByAgency <> 0
        SET @SQLString += N'ag.Name as agencyName,';
    IF @IsGroupByPaymentType <> 0
        SET @SQLString += N'pt.Name as payment_type,';
    IF @IsGroupByCampaignType <> 0
        SET @SQLString += N'ct.Name as campaign_type,';
    IF @IsGroupByMassmediaGroupType <> 0
        SET @SQLString += N'mg.Name as massmedia_group,';
    IF @IsGroupByAdvertType <> 0
        SET @SQLString += N'adt.Name as adverttype,';
    IF @IsGroupByAdvertTypeTop <> 0
        SET @SQLString += N'atTop.Name as topAdverttype,';

    SET @SQLString += N'
    ISNULL(SUM(d.price), 0) as price,
    COUNT(DISTINCT d.actionID) as action_cnt,
    COUNT(DISTINCT f.headCompanyID) as headcompany_cnt,
	ISNULL(SUM(d.price), 0) / COUNT(DISTINCT d.actionID) as average_bill_action,
	ISNULL(SUM(d.price), 0) / COUNT(DISTINCT f.headCompanyID) as average_bill_head_company
FROM #Campaign d
INNER JOIN iMonthName ON d.m = iMonthName.number
INNER JOIN Firm f ON f.firmId = d.firmId
';

    -- JOIN’ы только при необходимости колонок/групп
    IF @IsGroupByMassmediaGroupType <> 0
        SET @SQLString += N'INNER JOIN MassmediaGroup mg ON d.massmediaGroupID = mg.massmediaGroupID
';
    IF @IsGroupByPaymentType <> 0 OR @ShowWhite = 0 OR @ShowBlack = 0
        SET @SQLString += N'INNER JOIN Paymenttype pt ON d.paymentTypeID = pt.paymentTypeID
';
    IF @IsGroupByCampaignType <> 0
        SET @SQLString += N'INNER JOIN iCampaignType ct ON d.campaignTypeID = ct.CampaignTypeID
';
    IF @IsGroupByMassmedia <> 0
        SET @SQLString += N'INNER JOIN vMassMedia ON d.massmediaID = vMassMedia.massmediaID
';
    IF @IsGroupByManager <> 0
        SET @SQLString += N'INNER JOIN [User] u ON d.userID = u.UserID
';
    IF @IsGroupByAgency <> 0
        SET @SQLString += N'INNER JOIN Agency ag ON d.AgencyID = ag.AgencyID
';
    IF @IsGroupByHeadCompany <> 0
        SET @SQLString += N'INNER JOIN HeadCompany hc ON f.headCompanyID = hc.headCompanyID
';
    IF @IsGroupByAdvertType <> 0
        SET @SQLString += N'LEFT JOIN AdvertType adt ON d.advertTypeID = adt.AdvertTypeID
';
    IF @IsGroupByAdvertTypeTop <> 0
        SET @SQLString += N'LEFT JOIN AdvertType adt2 ON d.advertTypeID = adt2.AdvertTypeID
LEFT JOIN AdvertType atTop ON adt2.parentID = atTop.AdvertTypeID
';

    -- фильтр "price <> 0" как у тебя
    SET @SQLString += N'WHERE d.price <> 0
';


	-- ===== APPLY SAME FILTERS AS FOR @Summa =====

	-- White/Black (используем pt, т.к. join pt у тебя уже добавляется когда надо)
	IF @ShowWhite = 0 AND @ShowBlack <> 0
		SET @SQLString += N' AND pt.isHidden <> 0
	';
	IF @ShowBlack = 0 AND @ShowWhite <> 0
		SET @SQLString += N' AND pt.isHidden = 0
	';
	IF @ShowWhite = 0 AND @ShowBlack = 0
		SET @SQLString += N' AND 1=0
	';

	IF @FirmID IS NOT NULL
		SET @SQLString += N' AND d.firmID = @FirmID
	';

	IF @headCompanyID IS NOT NULL
		SET @SQLString += N' AND f.headCompanyID = @headCompanyID
	';

	IF @MassmediaID IS NOT NULL
		SET @SQLString += N' AND d.massmediaID = @MassmediaID
	';

	IF @PaymentTypeID IS NOT NULL
		SET @SQLString += N' AND d.paymentTypeID = @PaymentTypeID
	';

	IF @CampaignTypeID IS NOT NULL
		SET @SQLString += N' AND d.campaignTypeID = @CampaignTypeID
	';

	IF @ManagerID IS NOT NULL
		SET @SQLString += N' AND d.userID = @ManagerID
	';

	IF @AgencyID IS NOT NULL
		SET @SQLString += N' AND d.agencyID = @AgencyID
	';

	IF @massmediaGroupID IS NOT NULL
		SET @SQLString += N' AND d.massmediaGroupID = @massmediaGroupID
	';

	IF @advertTypeID IS NOT NULL
	BEGIN
		-- у тебя в part 2 нет join at/atFilter; есть только adt/adt2/atTop для группировок.
		-- поэтому фильтр по parentID делаем через EXISTS (без доп. join'ов)
		SET @SQLString += N'
	 AND EXISTS (
		 SELECT 1
		 FROM AdvertType a
		 WHERE a.advertTypeID = d.advertTypeID
		   AND (a.advertTypeID = @advertTypeID OR a.parentID = @advertTypeID)
	 )
	';
	END

    -- Group by
    SET @SQLString += N'GROUP BY d.y, d.m';

    IF @IsGroupByMassmediaGroupType <> 0 SET @SQLString += N', mg.Name';
    IF @IsGroupByPaymentType <> 0 SET @SQLString += N', pt.Name';
    IF @IsGroupByCampaignType <> 0 SET @SQLString += N', ct.Name';
    IF @IsGroupByMassmedia <> 0 SET @SQLString += N', vMassMedia.NameWithGroup';
    IF @IsGroupByFirm <> 0 SET @SQLString += N', f.Name';
    IF @IsGroupByHeadCompany <> 0 SET @SQLString += N', hc.Name';
    IF @IsGroupByManager <> 0 SET @SQLString += N', u.lastName, u.firstName';
    IF @IsGroupByAgency <> 0 SET @SQLString += N', ag.Name';
    IF @IsGroupByAdvertType <> 0 SET @SQLString += N', adt.Name';
    IF @IsGroupByAdvertTypeTop <> 0 SET @SQLString += N', atTop.Name';

    EXECUTE sp_executesql @SQLString,
        N'@startDate datetime, @finishDate datetime, @loggedUserID smallint,
          @FirmID smallint, @MassmediaID smallint, @PaymentTypeID smallint, @CampaignTypeID tinyint,
          @ManagerID smallint, @AgencyID smallint, @massmediaGroupID int, @advertTypeID smallint, @headCompanyID smallint,
		  @ShowWhite bit, @ShowBlack bit',
        @startDate = @StartDay, 
        @finishDate = @FinishDay, 
        @loggedUserID = @loggedUserID, 
        @FirmID = @FirmID,
        @MassmediaID = @MassmediaID,
        @PaymentTypeID = @PaymentTypeID,
        @CampaignTypeID = @CampaignTypeID,
        @ManagerID = @ManagerID,
        @AgencyID = @AgencyID,
        @massmediaGroupID = @massmediaGroupID,
        @advertTypeID = @advertTypeID,
        @headCompanyID = @headCompanyID,
		@ShowWhite = @ShowWhite,
		@ShowBlack = @ShowBlack;
END
