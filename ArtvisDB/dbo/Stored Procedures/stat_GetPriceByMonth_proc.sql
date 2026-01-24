
CREATE   PROC [dbo].[stat_GetPriceByMonth_proc]
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
    -- Пакетные цены по месяцам (campaignTypeID = 4)
    ------------------------------------------------------------
    CREATE TABLE #moduleMassmediaPrice
    (
        y smallint NOT NULL,
        m tinyint NOT NULL,
        campaignID int NOT NULL,
        massmediaID smallint NOT NULL,
        moduleMassmediaPrice money NOT NULL,
        PRIMARY KEY (y, m, campaignID, massmediaID)
    );

    CREATE TABLE #modulePrice
    (
        y smallint NOT NULL,
        m tinyint NOT NULL,
        campaignID int NOT NULL,
        modulePrice money NOT NULL,
        PRIMARY KEY (y, m, campaignID)
    );

    CREATE TABLE #Result
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

    CREATE UNIQUE CLUSTERED INDEX IX_Result
        ON #Result (y, m, campaignID, massmediaID, advertTypeID);

    ------------------------------------------------------------
    -- moduleMassmediaPrice (type 4) по mn
    ------------------------------------------------------------
    INSERT INTO #moduleMassmediaPrice (y, m, campaignID, massmediaID, moduleMassmediaPrice)
    SELECT
        mn.y,
        mn.m,
        i.campaignID,
        m.massmediaID,
        SUM(mpl.price) AS moduleMassmediaPrice
    FROM PackModuleIssue i
        JOIN Campaign c ON c.campaignID = i.campaignID AND c.campaignTypeID = 4
        JOIN PackModuleContent pmc ON i.priceListID = pmc.pricelistID
        JOIN ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
        JOIN Module m ON mpl.moduleID = m.moduleID
        JOIN dbo.f_months(@startDate, @finishDate) mn
            ON i.issueDate BETWEEN mn.startDate AND mn.finishDate
    WHERE i.issueDate BETWEEN @startDate AND @finishDate
    GROUP BY mn.y, mn.m, i.campaignID, m.massmediaID;

    INSERT INTO #modulePrice (y, m, campaignID, modulePrice)
    SELECT y, m, campaignID, SUM(moduleMassmediaPrice)
    FROM #moduleMassmediaPrice
    GROUP BY y, m, campaignID;

    ------------------------------------------------------------
    -- 1) Линейная (campaignTypeID=1) — 1:1 как в fn_statGetPriceByMonth
    ------------------------------------------------------------
    INSERT INTO #Result
    (
        y, m,
        campaignID, advertTypeID, actionID, massmediaID, paymentTypeID, campaignTypeID, agencyID,
        startDate, finishDate, finalPrice, userID, firmID, discount, massmediaGroupID, price
    )
    SELECT
        mn.y,
        mn.m,
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
        CAST(ROUND(SUM(i.tariffPrice * i.ratio), 2) AS money) AS price
    FROM dbo.f_months(@startDate, @finishDate) mn
        JOIN Campaign c
            ON c.campaignTypeID = 1
           AND c.startDate <= @finishDate
           AND c.finishDate >= @startDate
        JOIN Action a
            ON a.actionID = c.actionID
           AND a.isConfirmed = 1
           AND a.isSpecial = 0
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
        JOIN Issue i ON i.campaignID = c.campaignID
        JOIN Roller r ON r.rollerID = i.rollerID
    WHERE EXISTS
    (
        SELECT 1
        FROM TariffWindow tw
        WHERE tw.windowId = i.originalWindowID
          AND tw.dayOriginal BETWEEN mn.startDate AND mn.finishDate
    )
    GROUP BY
        mn.y, mn.m,
        c.campaignID,
        r.advertTypeID,
        c.actionID, c.massmediaID, c.paymentTypeID, c.campaignTypeID, c.agencyID,
        c.startDate, c.finishDate, c.finalPrice,
        a.userID, a.firmID, a.discount,
        m.massmediaGroupID;

    ------------------------------------------------------------
    -- 2) Спонсорская (campaignTypeID=2) — "радиодень" через broadcastStart (1:1)
    ------------------------------------------------------------
    INSERT INTO #Result
    (
        y, m,
        campaignID, advertTypeID, actionID, massmediaID, paymentTypeID, campaignTypeID, agencyID,
        startDate, finishDate, finalPrice, userID, firmID, discount, massmediaGroupID, price
    )
    SELECT
        mn.y,
        mn.m,
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
        mm.massmediaGroupID,
        CAST(ROUND(SUM(pi.tariffPrice * pi.ratio), 2) AS money) AS price
    FROM Campaign c
        JOIN Action a
            ON a.actionID = c.actionID
           AND a.isConfirmed = 1
           AND a.isSpecial = 0
        JOIN MassMedia mm ON mm.massmediaID = c.massmediaID
        JOIN ProgramIssue pi ON pi.campaignID = c.campaignID
        JOIN SponsorTariff st ON pi.tariffID = st.tariffID
        JOIN SponsorProgramPriceList pl ON st.priceListID = pl.priceListID
        JOIN dbo.f_months(@startDate, @finishDate) mn
            ON CONVERT(datetime,
                    CONVERT(varchar(8),
                        DATEADD(mi,
                            -DATEPART(mi, pl.broadcastStart),
                            DATEADD(hh,
                                -DATEPART(hh, pl.broadcastStart),
                                pi.issueDate
                            )
                        ),
                        112
                    ),
                    112
               ) BETWEEN mn.startDate AND mn.finishDate
    WHERE c.campaignTypeID = 2
      AND c.startDate <= @finishDate
      AND c.finishDate >= @startDate
    GROUP BY
        mn.y, mn.m,
        c.campaignID,
        pi.advertTypeID,
        c.actionID, c.massmediaID, c.paymentTypeID, c.campaignTypeID, c.agencyID,
        c.startDate, c.finishDate, c.finalPrice,
        a.userID, a.firmID, a.discount,
        mm.massmediaGroupID;

    ------------------------------------------------------------
    -- 3) Модульная (campaignTypeID=3) — 1:1 как fn_statGetPriceByMonth
    ------------------------------------------------------------
    INSERT INTO #Result
    (
        y, m,
        campaignID, advertTypeID, actionID, massmediaID, paymentTypeID, campaignTypeID, agencyID,
        startDate, finishDate, finalPrice, userID, firmID, discount, massmediaGroupID, price
    )
    SELECT
        mn.y,
        mn.m,
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
        CAST(ROUND(SUM(mi.tariffPrice * mi.ratio), 2) AS money) AS price
    FROM Campaign c
        JOIN Action a
            ON a.actionID = c.actionID
           AND a.isConfirmed = 1
           AND a.isSpecial = 0
        JOIN MassMedia m ON m.massmediaID = c.massmediaID
        JOIN ModuleIssue mi ON mi.campaignID = c.campaignID
        JOIN dbo.f_months(@startDate, @finishDate) mn
            ON mi.issueDate BETWEEN mn.startDate AND mn.finishDate
        JOIN Roller r ON r.rollerID = mi.rollerID
    WHERE c.campaignTypeID = 3
      AND c.startDate <= @finishDate
      AND c.finishDate >= @startDate
    GROUP BY
        mn.y, mn.m,
        c.campaignID,
        r.advertTypeID,
        c.actionID, c.massmediaID, c.paymentTypeID, c.campaignTypeID, c.agencyID,
        c.startDate, c.finishDate, c.finalPrice,
        a.userID, a.firmID, a.discount,
        m.massmediaGroupID;

    ------------------------------------------------------------
    -- 4) Пакетная (campaignTypeID=4) — 1:1 как fn_statGetPriceByMonth
    ------------------------------------------------------------
    INSERT INTO #Result
    (
        y, m,
        campaignID, advertTypeID, actionID, massmediaID, paymentTypeID, campaignTypeID, agencyID,
        startDate, finishDate, finalPrice, userID, firmID, discount, massmediaGroupID, price
    )
    SELECT
        mn.y,
        mn.m,
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
        mm.massmediaGroupID,
        CAST(ROUND(SUM(pmi.tariffPrice * pmi.ratio * mmp.moduleMassmediaPrice / mp.modulePrice), 2) AS money) AS price
    FROM Campaign c
        JOIN Action a
            ON a.actionID = c.actionID
           AND a.isConfirmed = 1
           AND a.isSpecial = 0
        JOIN PackModuleIssue pmi ON pmi.campaignID = c.campaignID
        JOIN dbo.f_months(@startDate, @finishDate) mn
            ON pmi.issueDate BETWEEN mn.startDate AND mn.finishDate
        JOIN Roller r ON r.rollerID = pmi.rollerID
        JOIN #moduleMassmediaPrice mmp
            ON mmp.y = mn.y AND mmp.m = mn.m AND mmp.campaignID = c.campaignID
        JOIN #modulePrice mp
            ON mp.y = mn.y AND mp.m = mn.m AND mp.campaignID = c.campaignID
        JOIN MassMedia mm
            ON mm.massmediaID = mmp.massmediaID
    WHERE c.campaignTypeID = 4
      AND c.startDate <= @finishDate
      AND c.finishDate >= @startDate
    GROUP BY
        mn.y, mn.m,
        c.campaignID,
        mmp.massmediaID,
        r.advertTypeID,
        c.actionID, c.paymentTypeID, c.campaignTypeID, c.agencyID,
        c.startDate, c.finishDate, c.finalPrice,
        a.userID, a.firmID, a.discount,
        mm.massmediaGroupID;

    ------------------------------------------------------------
    -- Права доступа + вывод (как в твоей исходной fn_* версии)
    ------------------------------------------------------------
    SELECT r.*
    FROM #Result r
    WHERE EXISTS
    (
        SELECT 1
        FROM fn_GetMassmediasForUser(@loggedUserID) mmu
        WHERE mmu.massmediaID = r.massmediaID
          AND (
                (r.userID = @loggedUserID AND mmu.myMassmedia = 1)
             OR (r.userID <> @loggedUserID AND mmu.foreignMassmedia = 1)
          )
    )
    AND
    (
           r.userID = @loggedUserID
        OR @canForeign = 1
        OR (
                @canGroup = 1
            AND EXISTS
            (
                SELECT 1
                FROM GroupMember gm
                JOIN fn_GetUserGroups(@loggedUserID) ug ON ug.id = gm.groupID
                WHERE gm.userID = r.userID
            )
        )
    );
END
