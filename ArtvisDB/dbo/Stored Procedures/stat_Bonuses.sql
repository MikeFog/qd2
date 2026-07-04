CREATE PROC [dbo].[stat_Bonuses]
(
    @periodStartDate         DATETIME,
    @periodFinishDate        DATETIME,
    @userID                  SMALLINT = NULL,
    @massmediaGroupID        INT = NULL,
    @minBonusPercentage      DECIMAL(5, 2) = NULL,
    @showBlack               BIT = 1,
    @withZeroRegularCostOnly BIT = 0,
    @selectByCreateDate      BIT = 0          -- если 1, отбор по Action.createDate, цена = finalPrice
)
AS
BEGIN
    SET NOCOUNT ON

    IF @periodStartDate IS NULL OR @periodFinishDate IS NULL
    BEGIN
        RAISERROR('Start date and finish date are required', 16, 1)
        RETURN
    END

    SET @periodStartDate  = CAST(CAST(@periodStartDate  AS DATE) AS DATETIME)
    SET @periodFinishDate = CAST(CAST(@periodFinishDate AS DATE) AS DATETIME)

    IF @selectByCreateDate = 1
    BEGIN
        SELECT
            CONCAT(a.[firmID], '_', ISNULL(mg.[massmediaGroupID], 0), '_', ISNULL(a.[userID], 0)) AS UniqueKey,
            a.[firmID],
            f.[name]                         AS firmName,
            mg.[massmediaGroupID],
            mg.[name]                        AS massmediaGroupName,
            a.[userID],
            ISNULL(vu.[userName], 'Unknown') AS userName,
            COUNT(c.[campaignID])            AS CampaignCount,
            SUM(CASE WHEN c.[paymentTypeID] = 26
                     THEN CASE c.[campaignTypeID]
                              WHEN 4 THEN c.[finalPrice]
                              ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                          END
                     ELSE 0 END)             AS TotalBonusCost,
            SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                     THEN CASE c.[campaignTypeID]
                              WHEN 4 THEN c.[finalPrice]
                              ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                          END
                     ELSE 0 END)             AS RegularCost,
            SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 1
                     THEN CASE c.[campaignTypeID]
                              WHEN 4 THEN c.[finalPrice]
                              ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                          END
                     ELSE 0 END)             AS HiddenCost,
            CASE
                WHEN SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                              THEN CASE c.[campaignTypeID]
                                       WHEN 4 THEN c.[finalPrice]
                                       ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                                   END
                              ELSE 0 END) = 0 THEN NULL
                ELSE ROUND(
                    SUM(CASE WHEN c.[paymentTypeID] = 26
                             THEN CASE c.[campaignTypeID]
                                      WHEN 4 THEN c.[finalPrice]
                                      ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                                  END
                             ELSE 0 END)
                    / SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                               THEN CASE c.[campaignTypeID]
                                        WHEN 4 THEN c.[finalPrice]
                                        ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                                    END
                               ELSE 0 END) * 100, 2)
            END                              AS BonusPercentage,
            @periodStartDate                 AS PeriodStartDate,
            @periodFinishDate                AS PeriodFinishDate,
            @selectByCreateDate              AS SelectByCreateDate
        FROM
            [dbo].[Campaign] c
            INNER JOIN [dbo].[Action] a         ON c.[actionID]         = a.[actionID]
            INNER JOIN [dbo].[Firm] f            ON a.[firmID]           = f.[firmID]
            INNER JOIN [dbo].[PaymentType] pt    ON pt.[PaymenttypeId]   = c.[paymenttypeId]
            LEFT  JOIN [dbo].[MassMedia] m       ON c.[massmediaID]      = m.[massmediaID]
            LEFT  JOIN [dbo].[MassmediaGroup] mg ON m.[massmediaGroupID] = mg.[massmediaGroupID]
            LEFT  JOIN [dbo].[vUser] vu          ON a.[userID]           = vu.[userID]
        WHERE
            a.[createDate] >= @periodStartDate
            AND a.[createDate] <  DATEADD(DAY, 1, @periodFinishDate)
            AND a.[isConfirmed] = 1
            AND (@userID IS NULL OR a.[userID] = @userID)
            AND (@massmediaGroupID IS NULL OR mg.[massmediaGroupID] = @massmediaGroupID)
        GROUP BY
            a.[firmID], f.[name],
            mg.[massmediaGroupID], mg.[name],
            a.[userID], vu.[userName]
        HAVING
            (@minBonusPercentage IS NULL
             OR ROUND(
                SUM(CASE WHEN c.[paymentTypeID] = 26
                         THEN CASE c.[campaignTypeID]
                                  WHEN 4 THEN c.[finalPrice]
                                  ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                              END
                         ELSE 0 END)
                / NULLIF(SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                  THEN CASE c.[campaignTypeID]
                                           WHEN 4 THEN c.[finalPrice]
                                           ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                                       END
                                  ELSE 0 END), 0) * 100, 2
                ) > @minBonusPercentage)
            AND (@withZeroRegularCostOnly = 0
                 OR SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                             THEN CASE c.[campaignTypeID]
                                      WHEN 4 THEN c.[finalPrice]
                                      ELSE CAST(c.[finalPrice] * a.[discount] AS DECIMAL(9,2))
                                  END
                             ELSE 0 END) = 0)
        ORDER BY
            f.[name] ASC, mg.[name] ASC, vu.[userName] ASC, RegularCost DESC

        RETURN
    END

    -- =====================================================================
    -- Стандартный режим: цена по периоду вычисляется set-based напрямую
    -- (Issue/ProgramIssue/ModuleIssue/PackModuleIssue), без курсора и без
    -- построчных EXEC GetPriceByPeriod. Логика 4 типов кампаний и isSpecial
    -- воспроизводит GetPriceByPeriod максимально точно, включая то, что для
    -- isSpecial-кампаний период игнорируется (берётся Campaign.price), и что
    -- для campaignTypeID=4 итог пропорционально делится между массмедиа.
    -- =====================================================================

    CREATE TABLE #CampaignCosts
    (
        [campaignID]         INT,
        [firmID]             SMALLINT,
        [firmName]           NVARCHAR(MAX),
        [massmediaGroupID]   INT,
        [massmediaGroupName] VARCHAR(250),
        [userID]             SMALLINT,
        [userName]           NVARCHAR(MAX),
        [paymentTypeID]      SMALLINT,
        [isHidden]           BIT,
        [costInPeriod]       DECIMAL(18, 2)
    )

    SELECT DISTINCT
        c.[campaignID], c.[campaignTypeID], c.[paymentTypeID], c.[price] AS specialPrice,
        a.[firmID], a.[isSpecial], a.[userID] AS actionUserID,
        f.[name] AS firmName,
        pt.[isHidden],
        mg.[massmediaGroupID], mg.[name] AS massmediaGroupName,
        ISNULL(vu.[userName], 'Unknown') AS userName
    INTO #CampaignBase
    FROM [dbo].[Campaign] c
        INNER JOIN [dbo].[Action] a ON c.[actionID] = a.[actionID]
        INNER JOIN [dbo].[Firm] f ON a.[firmID] = f.[firmID]
        INNER JOIN [dbo].[PaymentType] pt ON pt.[PaymenttypeId] = c.[paymenttypeId]
        LEFT JOIN [dbo].[MassMedia] m ON c.[massmediaID] = m.[massmediaID]
        LEFT JOIN [dbo].[MassmediaGroup] mg ON m.[massmediaGroupID] = mg.[massmediaGroupID]
        LEFT JOIN [dbo].[vUser] vu ON a.[userID] = vu.[userID]
    WHERE
        c.[startDate] <= @periodFinishDate
        AND c.[finishDate] >= @periodStartDate
        AND a.[isConfirmed] = 1
        AND (@userID IS NULL OR a.[userID] = @userID)
        AND (@massmediaGroupID IS NULL OR mg.[massmediaGroupID] = @massmediaGroupID OR c.[campaignTypeID] = 4)

    -- Тип 1 (линейные ролики). Одна строка на кампанию всегда (LEFT JOIN),
    -- независимо от isSpecial/showBlack — как в оригинале (INSERT после EXEC
    -- выполняется безусловно). costInPeriod = NULL, если showBlack блокирует
    -- платёжный тип (соответствует поведению GetPriceByPeriod: тот же самый
    -- (@showBlack=1 OR pt.isHidden=0) джойн проваливается что для isSpecial-
    -- проверки, что для обычного расчёта — то есть результат один и тот же NULL).
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               WHEN cb.[isSpecial] = 1 THEN MAX(cb.[specialPrice])
               ELSE SUM(CASE WHEN tw.[windowId] IS NOT NULL THEN i.[tariffPrice] * i.[ratio] END)
           END AS costInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[Issue] i ON i.[campaignID] = cb.[campaignID]
        LEFT JOIN [dbo].[TariffWindow] tw ON i.[originalWindowID] = tw.[windowId]
            AND tw.[dayOriginal] BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 1
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden], cb.[isSpecial]

    -- Тип 2 (спонсорские программы)
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               WHEN cb.[isSpecial] = 1 THEN MAX(cb.[specialPrice])
               ELSE SUM(CASE WHEN pl.[priceListID] IS NOT NULL THEN i.[tariffPrice] * i.[ratio] END)
           END AS costInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[ProgramIssue] i ON i.[campaignID] = cb.[campaignID]
        LEFT JOIN [dbo].[SponsorTariff] st ON i.[tariffID] = st.[tariffID]
        LEFT JOIN [dbo].[SponsorProgramPriceList] pl ON st.[priceListID] = pl.[priceListID]
            AND CONVERT(DATETIME, CONVERT(VARCHAR(8),
                DATEADD(MINUTE, -DATEPART(MINUTE, pl.[broadcastStart]),
                    DATEADD(HOUR, -DATEPART(HOUR, pl.[broadcastStart]), i.[issueDate])), 112), 112)
                BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 2
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden], cb.[isSpecial]

    -- Тип 3 (модульные)
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               WHEN cb.[isSpecial] = 1 THEN MAX(cb.[specialPrice])
               ELSE SUM(i.[tariffPrice] * i.[ratio])
           END AS costInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[ModuleIssue] i ON i.[campaignID] = cb.[campaignID]
            AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 3
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden], cb.[isSpecial]

    -- Тип 4 (пакетные): состав пар campaignID×massmediaID определяется ТОЛЬКО
    -- датой выпуска (как massmedia_cursor в оригинале — без фильтра по
    -- isSpecial/showBlack), а сама стоимость — через CASE как выше. Для
    -- пропорционального деления вес считается отдельно (T4Weight), но набор
    -- строк (T4Set) не зависит от showBlack, чтобы не терять строки.
    ;WITH T4Set AS (
        SELECT DISTINCT cb.[campaignID], m.[massmediaID], mg2.[massmediaGroupID], mg2.[name] AS massmediaGroupName
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
            INNER JOIN [dbo].[PackModuleContent] pmc ON i.[priceListID] = pmc.[pricelistID]
            INNER JOIN [dbo].[ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
            INNER JOIN [dbo].[Module] m ON mpl.[moduleID] = m.[moduleID]
            LEFT JOIN [dbo].[MassMedia] mm2 ON m.[massmediaID] = mm2.[massmediaID]
            LEFT JOIN [dbo].[MassmediaGroup] mg2 ON mm2.[massmediaGroupID] = mg2.[massmediaGroupID]
        WHERE cb.[campaignTypeID] = 4
    ),
    T4Weight AS (
        SELECT cb.[campaignID], m.[massmediaID], SUM(mpl.[price]) AS mmPrice
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
            INNER JOIN [dbo].[PackModuleContent] pmc ON i.[priceListID] = pmc.[pricelistID]
            INNER JOIN [dbo].[ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
            INNER JOIN [dbo].[Module] m ON mpl.[moduleID] = m.[moduleID]
        WHERE cb.[campaignTypeID] = 4 AND (@showBlack = 1 OR cb.[isHidden] = 0)
        GROUP BY cb.[campaignID], m.[massmediaID]
    ),
    T4Total AS (
        SELECT cb.[campaignID], SUM(i.[tariffPrice] * i.[ratio]) AS packModulePrice
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
        WHERE cb.[campaignTypeID] = 4 AND (@showBlack = 1 OR cb.[isHidden] = 0)
        GROUP BY cb.[campaignID]
    ),
    T4Sum AS (
        SELECT [campaignID], SUM([mmPrice]) AS sumPrice
        FROM T4Weight
        GROUP BY [campaignID]
    )
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName],
           t4s.[massmediaGroupID], t4s.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               WHEN cb.[isSpecial] = 1 THEN cb.[specialPrice]
               ELSE tot.[packModulePrice] * w.[mmPrice] / s.[sumPrice]
           END AS costInPeriod
    FROM T4Set t4s
        INNER JOIN #CampaignBase cb ON cb.[campaignID] = t4s.[campaignID]
        LEFT JOIN T4Total tot ON tot.[campaignID] = t4s.[campaignID]
        LEFT JOIN T4Weight w ON w.[campaignID] = t4s.[campaignID] AND w.[massmediaID] = t4s.[massmediaID]
        LEFT JOIN T4Sum s ON s.[campaignID] = t4s.[campaignID]
    WHERE (@massmediaGroupID IS NULL OR t4s.[massmediaGroupID] = @massmediaGroupID)

    SELECT
        CONCAT([firmID], '_', ISNULL([massmediaGroupID], 0), '_', ISNULL([userID], 0)) AS UniqueKey,
        [firmID],
        [firmName],
        [massmediaGroupID],
        [massmediaGroupName],
        [userID],
        [userName],
        COUNT([campaignID]) AS CampaignCount,
        SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END) AS TotalBonusCost,
        SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) AS RegularCost,
        SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 1 THEN [costInPeriod] ELSE 0 END) AS HiddenCost,
        CASE
            WHEN SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0 THEN NULL
            ELSE ROUND(
                (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                / SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END)) * 100, 2)
        END AS BonusPercentage,
        @periodStartDate    AS PeriodStartDate,
        @periodFinishDate   AS PeriodFinishDate,
        @selectByCreateDate AS SelectByCreateDate
    FROM #CampaignCosts
    GROUP BY
        [firmID], [firmName],
        [massmediaGroupID], [massmediaGroupName],
        [userID], [userName]
    HAVING
        (@minBonusPercentage IS NULL
         OR ROUND(
            (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
            / NULLIF(SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END), 0)) * 100, 2
            ) > @minBonusPercentage)
        AND (@withZeroRegularCostOnly = 0
             OR SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0)
    ORDER BY
        [firmName] ASC, [massmediaGroupName] ASC, [userName] ASC, RegularCost DESC

    DROP TABLE #CampaignCosts
    DROP TABLE #CampaignBase
END