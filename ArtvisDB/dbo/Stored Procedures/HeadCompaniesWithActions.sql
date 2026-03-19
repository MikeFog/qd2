CREATE PROC [dbo].[HeadCompaniesWithActions]
    @startOfInterval datetime = NULL,
    @endOfInterval datetime = NULL,
    @createDateBegin datetime = NULL,
    @createDateEnd datetime = NULL,
    @firmId2 smallint = NULL,
    @showBlack BIT = 0,
    @showWhite BIT = 0,
    @actionID int = NULL,
    @headCompanyID int = NULL,
    @userID smallint = NULL,
    @agencyID smallint = NULL,
    @massmediaID smallint = NULL,
    @massmediaGroupID int = NULL,
    @campaignTypeID tinyint = NULL,
    @paymentTypeID smallint = NULL,
    @isShowActivate BIT = 0,
    @isShowNotActivate BIT = 0,
    @showDeleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT DISTINCT
        hc.*,
        @userID AS userID,
        @startOfInterval AS startOfInterval,
        @endOfInterval AS endOfInterval,
        @actionID AS actionID,
        @massmediaGroupID AS massmediaGroupID,
        @showDeleted AS showDeleted,
        @isShowActivate AS isShowActivate,
        @isShowNotActivate AS isShowNotActivate
    FROM HeadCompany hc
        INNER JOIN Firm f
            ON hc.headCompanyID = f.headCompanyID
        INNER JOIN Action a
            ON f.firmID = a.firmID
        INNER JOIN Campaign c
            ON a.actionID = c.actionID
        INNER JOIN PaymentType pt
            ON c.paymentTypeID = pt.paymentTypeID
        LEFT JOIN MassMedia mm
            ON c.massmediaID = mm.massmediaID
        LEFT JOIN
        (
            SELECT 
                pmi.campaignID,
                m.massmediaID,
                mm2.massmediaGroupID
            FROM PackModuleIssue pmi
                INNER JOIN PackModulePriceList pmpl
                    ON pmi.pricelistID = pmpl.priceListID
                INNER JOIN PackModuleContent pmc
                    ON pmpl.priceListID = pmc.pricelistID
                INNER JOIN Module m
                    ON pmc.moduleID = m.moduleID
                INNER JOIN MassMedia mm2
                    ON m.massmediaID = mm2.massmediaID
        ) pack_media
            ON c.campaignID = pack_media.campaignID
           AND c.campaignTypeID = 4
    WHERE
        a.finishDate >= COALESCE(@startOfInterval, a.finishDate)
        AND a.startDate <= COALESCE(@endOfInterval, a.startDate)

        AND (@createDateBegin IS NULL OR a.createDate >= @createDateBegin)
        AND (@createDateEnd IS NULL OR a.createDate <= @createDateEnd)

        AND (@firmId2 IS NULL OR a.firmID = @firmId2)

        AND
        (
            (@showBlack = 1 AND pt.IsHidden = 1)
            OR (@showWhite = 1 AND pt.IsHidden = 0)
        )

        AND (@actionID IS NULL OR a.actionID = @actionID)
        AND (@headCompanyID IS NULL OR hc.headCompanyID = @headCompanyID)
        AND (@userID IS NULL OR a.userID = @userID)
        AND (@agencyID IS NULL OR c.agencyID = @agencyID)
        AND (@campaignTypeID IS NULL OR c.campaignTypeID = @campaignTypeID)
        AND (@paymentTypeID IS NULL OR c.paymentTypeID = @paymentTypeID)

        AND
        (
            @massmediaID IS NULL
            OR (c.campaignTypeID <> 4 AND c.massmediaID = @massmediaID)
            OR (c.campaignTypeID = 4 AND pack_media.massmediaID = @massmediaID)
        )

        AND
        (
            @massmediaGroupID IS NULL
            OR (c.campaignTypeID <> 4 AND mm.massmediaGroupID = @massmediaGroupID)
            OR (c.campaignTypeID = 4 AND pack_media.massmediaGroupID = @massmediaGroupID)
        )

        AND
        (
            (@isShowActivate = 0 AND @isShowNotActivate = 0 AND @showDeleted = 0)
            OR (@isShowActivate = 1 AND a.isConfirmed = 1 AND a.deleteDate IS NULL)
            OR (@isShowNotActivate = 1 AND a.isConfirmed = 0 AND a.deleteDate IS NULL)
            OR (@showDeleted = 1 AND a.deleteDate IS NOT NULL)
        )
    ORDER BY
        hc.name;
END