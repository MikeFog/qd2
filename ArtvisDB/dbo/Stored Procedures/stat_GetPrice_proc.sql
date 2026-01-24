CREATE   PROC [dbo].[stat_GetPrice_proc]
(
    @startDate datetime,
    @finishDate datetime,
    @loggedUserID smallint
)
AS
BEGIN
    SET NOCOUNT ON;

    ------------------------------------------------------------
    -- права доступа — считаем ОДИН раз
    ------------------------------------------------------------
    DECLARE @canForeign bit = dbo.fn_IsRightToViewForeignActions(@loggedUserID);
    DECLARE @canGroup   bit = dbo.fn_IsRightToViewGroupActions(@loggedUserID);

    ------------------------------------------------------------
    -- temp tables вместо table variables
    ------------------------------------------------------------
    CREATE TABLE #moduleMassmediaPrice
    (
        campaignID int NOT NULL,
        massmediaID smallint NOT NULL,
        moduleMassmediaPrice money NOT NULL,
        PRIMARY KEY (campaignID, massmediaID)
    );

    CREATE TABLE #modulePrice
    (
        campaignID int NOT NULL PRIMARY KEY,
        modulePrice money NOT NULL
    );

    CREATE TABLE #Result
    (
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

    CREATE CLUSTERED INDEX IX_Result
        ON #Result (campaignID, massmediaID, advertTypeID);

    ------------------------------------------------------------
    -- 1. Пакетные цены (campaignTypeID = 4)
    ------------------------------------------------------------
    INSERT INTO #moduleMassmediaPrice
    SELECT
        i.campaignID,
        m.massmediaID,
        SUM(mpl.price)
    FROM PackModuleIssue i
        JOIN Campaign c ON c.campaignID = i.campaignID AND c.campaignTypeID = 4
        JOIN PackModuleContent pmc ON i.priceListID = pmc.pricelistID
        JOIN ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
        JOIN Module m ON mpl.moduleID = m.moduleID
    WHERE i.issueDate BETWEEN @startDate AND @finishDate
    GROUP BY i.campaignID, m.massmediaID;

    INSERT INTO #modulePrice
    SELECT campaignID, SUM(moduleMassmediaPrice)
    FROM #moduleMassmediaPrice
    GROUP BY campaignID;

    ------------------------------------------------------------
    -- 2. Специальные акции
    ------------------------------------------------------------
    INSERT INTO #Result
    SELECT
        c.campaignID,
        NULL,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        c.price
    FROM Campaign c
        JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
    WHERE a.isSpecial = 1
      AND c.startDate <= @finishDate
      AND c.finishDate >= @startDate;

    ------------------------------------------------------------
    -- 3. Линейная
    ------------------------------------------------------------
    INSERT INTO #Result
    SELECT
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        CAST(ROUND(SUM(i.tariffPrice * i.ratio),2) AS money)
    FROM Campaign c
        JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
        JOIN Issue i ON i.campaignID = c.campaignID
        JOIN TariffWindow tw ON tw.windowId = i.originalWindowID
        JOIN Roller r ON r.rollerID = i.rollerID
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
    WHERE c.campaignTypeID = 1
      AND a.isSpecial = 0
      AND tw.dayOriginal BETWEEN @startDate AND @finishDate
    GROUP BY
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID;

    ------------------------------------------------------------
    -- 4. Модульная
    ------------------------------------------------------------
    INSERT INTO #Result
    SELECT
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        CAST(ROUND(SUM(i.tariffPrice * i.ratio),2) AS money)
    FROM Campaign c
        JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
        JOIN ModuleIssue i ON i.campaignID = c.campaignID
        JOIN Roller r ON r.rollerID = i.rollerID
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
    WHERE c.campaignTypeID = 3
      AND a.isSpecial = 0
      AND i.issueDate BETWEEN @startDate AND @finishDate
    GROUP BY
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        c.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID;

    ------------------------------------------------------------
    -- 5. Пакетная (campaignTypeID = 4)
    ------------------------------------------------------------
    INSERT INTO #Result
    SELECT
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        mmp.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID,
        CAST(ROUND(SUM(i.tariffPrice * i.ratio * mmp.moduleMassmediaPrice / mp.modulePrice),2) AS money)
    FROM Campaign c
        JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
        JOIN PackModuleIssue i ON i.campaignID = c.campaignID
        JOIN Roller r ON r.rollerID = i.rollerID
        JOIN #moduleMassmediaPrice mmp ON mmp.campaignID = c.campaignID
        JOIN #modulePrice mp ON mp.campaignID = c.campaignID
        JOIN MassMedia m ON m.massmediaID = mmp.massmediaID
    WHERE c.campaignTypeID = 4
      AND a.isSpecial = 0
      AND i.issueDate BETWEEN @startDate AND @finishDate
    GROUP BY
        c.campaignID,
        r.advertTypeID,
        c.actionID,
        mmp.massmediaID,
        c.paymentTypeID,
        c.campaignTypeID,
        c.agencyID,
        c.startDate,
        c.finishDate,
        c.finalPrice,
        a.userID,
        a.firmID,
        a.discount,
        m.massmediaGroupID;

------------------------------------------------------------
-- 3.5 Спонсорская (campaignTypeID = 2)  [ДОБАВЛЕНО]
-- Важно: у спонсорских может не быть записей в Issue.
-- Источник фактов: ProgramIssue + SponsorTariff/SponsorProgramPriceList
------------------------------------------------------------
INSERT INTO #Result
SELECT
    c.campaignID,
    pi.advertTypeID,
    c.actionID,
    c.massmediaID,
    c.paymentTypeID,
    c.campaignTypeID,
    c.agencyID,
    c.startDate,
    c.finishDate,
    c.finalPrice,
    a.userID,
    a.firmID,
    a.discount,
    m.massmediaGroupID,
    CAST(ROUND(SUM(pi.tariffPrice * pi.ratio), 2) AS money) AS price
FROM Campaign c
    JOIN Action a ON a.actionID = c.actionID AND a.isConfirmed = 1
    JOIN ProgramIssue pi ON pi.campaignID = c.campaignID
    JOIN MassMedia m ON m.massmediaID = c.massmediaID
WHERE c.campaignTypeID = 2
  AND a.isSpecial = 0
  AND c.startDate <= @finishDate
  AND c.finishDate >= @startDate
  AND EXISTS
  (
      SELECT 1
      FROM SponsorTariff st
          JOIN SponsorProgramPriceList pl ON st.priceListID = pl.priceListID
      WHERE st.tariffID = pi.tariffID
        AND
        (
            -- тот самый "привод к дню с учетом broadcastStart" как в fn_statGetPrice
            CONVERT(datetime,
                CONVERT(varchar(8),
                    DATEADD(minute, -DATEPART(minute, pl.broadcastStart),
                        DATEADD(hour, -DATEPART(hour, pl.broadcastStart), pi.issueDate)
                    ),
                112),
            112)
        ) BETWEEN @startDate AND @finishDate
  )
GROUP BY
    c.campaignID,
    pi.advertTypeID,
    c.actionID,
    c.massmediaID,
    c.paymentTypeID,
    c.campaignTypeID,
    c.agencyID,
    c.startDate,
    c.finishDate,
    c.finalPrice,
    a.userID,
    a.firmID,
    a.discount,
    m.massmediaGroupID;


    ------------------------------------------------------------
    -- 6. Права доступа + вывод
    ------------------------------------------------------------
    SELECT r.*
    FROM #Result r
    WHERE EXISTS (
        SELECT 1
        FROM fn_GetMassmediasForUser(@loggedUserID) mm
        WHERE mm.massmediaID = r.massmediaID
          AND (
                (r.userID = @loggedUserID AND mm.myMassmedia = 1)
             OR (r.userID <> @loggedUserID AND mm.foreignMassmedia = 1)
          )
    )
    AND (
            r.userID = @loggedUserID
         OR @canForeign = 1
         OR (
                @canGroup = 1
            AND EXISTS (
                SELECT 1
                FROM GroupMember gm
                JOIN fn_GetUserGroups(@loggedUserID) ug ON ug.id = gm.groupID
                WHERE gm.userID = r.userID
            )
         )
    );
END
