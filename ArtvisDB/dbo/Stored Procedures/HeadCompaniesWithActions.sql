
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
    @rollerId int = null,
    @moduleID smallint = null,
    @packModuleID smallint = null,
    @showDeleted BIT = 0,
    @issueDate datetime = null,
    @issueDay datetime = null,
    @campaignFinishDate datetime = null,
    @withoutActionsSince datetime = null,
    @managerDiscount decimal(8,2) = null,
    @changeStartOfInterval datetime = null,
    @changeEndOfInterval datetime = null
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; -- Важно для продакшена

    SELECT 
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
    WHERE (@headCompanyID IS NULL OR hc.headCompanyID = @headCompanyID)
      AND EXISTS (
        -- Начинаем проверку условий "вглубь"
        SELECT 1 
        FROM Firm f
            INNER JOIN Action a ON f.firmID = a.firmID
            INNER JOIN Campaign c ON a.actionID = c.actionID
            INNER JOIN PaymentType pt ON c.paymentTypeID = pt.paymentTypeID
            LEFT JOIN Issue i ON c.campaignID = i.campaignID
			LEFT JOIN TariffWindow tw ON i.originalWindowID = tw.windowId
            left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
            left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
			left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
        WHERE f.headCompanyID = hc.headCompanyID
          AND (@issueDate is null or ((datepart(hh, tw.windowDateOriginal) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateOriginal) = datepart(minute, @issueDate))) )
		  AND (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)))
          and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
          AND c.finishDate = Coalesce(@campaignFinishDate, c.finishDate)
          AND (@rollerId is null or i.rollerID = coalesce(@rollerId, i.rollerID))
          AND (@moduleID is null or mi.moduleID = @moduleID)
          AND (@withoutActionsSince is null or not exists(select top 1 a1.actionID
												from [Action] a1
													inner join [Firm] f1 on a1.firmID = f1.firmID
												where f1.headCompanyID = hc.headCompanyID
													and a1.finishDate >= @withoutActionsSince
													and (@startOfInterval is null or a1.startDate < @startOfInterval)))
        and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
          -- Фильтры дат (SARGable)
          AND (@startOfInterval IS NULL OR a.finishDate >= @startOfInterval)
          AND (@endOfInterval IS NULL OR a.startDate <= @endOfInterval)
          AND (@createDateBegin IS NULL OR a.createDate >= @createDateBegin)
          AND (@createDateEnd IS NULL OR a.createDate <= @createDateEnd)
		  AND (@changeStartOfInterval IS NULL OR a.modDate >= @changeStartOfInterval)
          AND (@changeEndOfInterval IS NULL OR a.modDate <= @changeEndOfInterval)
          
          -- Фильтры фирмы и действий
          AND (@firmId2 IS NULL OR a.firmID = @firmId2)
          AND (@actionID IS NULL OR a.actionID = @actionID)
          AND (@userID IS NULL OR a.userID = @userID)
          
          -- Белый/Черный нал
          AND ((@showBlack = 1 AND pt.IsHidden = 1) OR (@showWhite = 1 AND pt.IsHidden = 0))
          
          -- Состояние активации/удаления
          AND (
                (@isShowActivate = 0 AND @isShowNotActivate = 0 AND @showDeleted = 0)
                OR (@isShowActivate = 1 AND a.isConfirmed = 1 AND a.deleteDate IS NULL)
                OR (@isShowNotActivate = 1 AND a.isConfirmed = 0 AND a.deleteDate IS NULL)
                OR (@showDeleted = 1 AND a.deleteDate IS NOT NULL)
          )

          -- Фильтры кампании
          AND (@agencyID IS NULL OR c.agencyID = @agencyID)
          AND (@campaignTypeID IS NULL OR c.campaignTypeID = @campaignTypeID)
          AND (@paymentTypeID IS NULL OR c.paymentTypeID = @paymentTypeID)

          -- Сложная логика MassMedia
          AND (
            @massmediaID IS NULL 
            OR (c.campaignTypeID <> 4 AND c.massmediaID = @massmediaID)
            OR (c.campaignTypeID = 4 AND EXISTS (
                -- Проверяем наличие медиа в пакете только если кампания - пакет
                SELECT 1 FROM PackModuleIssue pmi
                INNER JOIN PackModulePriceList pmpl ON pmi.pricelistID = pmpl.priceListID
                INNER JOIN PackModuleContent pmc ON pmpl.priceListID = pmc.pricelistID
                INNER JOIN Module m ON pmc.moduleID = m.moduleID
                WHERE pmi.campaignID = c.campaignID AND m.massmediaID = @massmediaID
            ))
          )

          -- Сложная логика MassMediaGroup
          AND (
            @massmediaGroupID IS NULL
            OR (c.campaignTypeID <> 4 AND EXISTS (SELECT 1 FROM MassMedia mm WHERE mm.massmediaID = c.massmediaID AND mm.massmediaGroupID = @massmediaGroupID))
            OR (c.campaignTypeID = 4 AND EXISTS (
                SELECT 1 FROM PackModuleIssue pmi
                INNER JOIN PackModulePriceList pmpl ON pmi.pricelistID = pmpl.priceListID
                INNER JOIN PackModuleContent pmc ON pmpl.priceListID = pmc.pricelistID
                INNER JOIN Module m ON pmc.moduleID = m.moduleID
                INNER JOIN MassMedia mm2 ON m.massmediaID = mm2.massmediaID
                WHERE pmi.campaignID = c.campaignID AND mm2.massmediaGroupID = @massmediaGroupID
            ))
          )
    )
    ORDER BY hc.name;
END