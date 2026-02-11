CREATE PROCEDURE [dbo].[pc_GetStationsPriceModel]
(
    @MassmediaGroupID  int = null,
    @DateFrom          date,
    @DateTo            date,
    @loggedUserID      int
)
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID('tempdb..#Segments') IS NOT NULL DROP TABLE #Segments;
    IF OBJECT_ID('tempdb..#SegPrices') IS NOT NULL DROP TABLE #SegPrices;

    CREATE TABLE #Segments
    (
        massmediaID   smallint     NOT NULL,
        [name]        varchar(64)  NOT NULL,
        pricelistID   smallint     NOT NULL,
        SegmentStart  date         NOT NULL,
        SegmentEnd    date         NOT NULL,
		extraChargeFirstRoller tinyint NOT NULL,
		extraChargeSecondRoller tinyint NOT NULL,
		extraChargeLastRoller tinyint NOT NULL
    );

    CREATE TABLE #SegPrices
    (
        massmediaID                  smallint     NOT NULL,
        [name]                       varchar(64)  NOT NULL,
        pricelistID                  smallint     NOT NULL,
        SegmentStart                 date         NOT NULL,
        SegmentEnd                   date         NOT NULL,

        PrimePricePerSecWeekday      money        NOT NULL,
        NonPrimePricePerSecWeekday   money        NOT NULL,
        PrimePricePerSecWeekend      money        NOT NULL,
        NonPrimePricePerSecWeekend   money        NOT NULL,
		extraChargeFirstRoller tinyint NOT NULL,
		extraChargeSecondRoller tinyint NOT NULL,
		extraChargeLastRoller tinyint NOT NULL
    );

    ;WITH Stations AS
    (
        SELECT m.massmediaID, m.nameWithGroup as [name]
        FROM dbo.vMassmedia m
        WHERE m.isActive = 1
          AND m.massmediaGroupID = IsNull(@MassmediaGroupID, m.massmediaGroupID)
    )
    INSERT INTO #Segments(massmediaID, [name], pricelistID, SegmentStart, SegmentEnd, extraChargeFirstRoller, extraChargeSecondRoller, extraChargeLastRoller)
    SELECT
        s.massmediaID,
        s.[name],
        p.pricelistID,
        SegmentStart = CASE WHEN CONVERT(date, p.startDate)  < @DateFrom THEN @DateFrom ELSE CONVERT(date, p.startDate) END,
        SegmentEnd   = CASE WHEN CONVERT(date, p.finishDate) > @DateTo   THEN @DateTo   ELSE CONVERT(date, p.finishDate) END,
		p.extraChargeFirstRoller,
		p.extraChargeSecondRoller,
		p.extraChargeLastRoller
    FROM Stations s
    JOIN dbo.Pricelist p ON p.massmediaID = s.massmediaID
    WHERE p.finishDate >= @DateFrom
      AND p.startDate  <= @DateTo
      AND (CASE WHEN CONVERT(date, p.startDate)  < @DateFrom THEN @DateFrom ELSE CONVERT(date, p.startDate) END)
        <=
          (CASE WHEN CONVERT(date, p.finishDate) > @DateTo   THEN @DateTo   ELSE CONVERT(date, p.finishDate) END);

    ;WITH TariffCats AS
    (
        -- Размножаем тариф в 2 “категории”, если он подходит и туда и туда
        SELECT
            t.pricelistID,
            t.price,
            cat = v.cat
        FROM dbo.Tariff t
        CROSS APPLY (VALUES
            (CASE WHEN (t.monday=1 OR t.tuesday=1 OR t.wednesday=1 OR t.thursday=1 OR t.friday=1) THEN 'WD' END),
            (CASE WHEN (t.saturday=1 OR t.sunday=1) THEN 'WE' END)
        ) v(cat)
        WHERE v.cat IS NOT NULL
          -- если нужно исключить “служебные” тарифы:
          -- AND ISNULL(t.isForModuleOnly, 0) = 0
          -- если нужно убрать нулевые цены:
          -- AND t.price > 0
    ),
    PriceRanks AS
    (
        SELECT
            pricelistID,
            cat,
            price,
            dr = DENSE_RANK() OVER (PARTITION BY pricelistID, cat ORDER BY price DESC)
        FROM TariffCats
    ),
    PLPrices AS
    (
        SELECT
            pricelistID,

            PrimePricePerSecWeekday    = MAX(CASE WHEN cat='WD' AND dr=1 THEN price END),
            NonPrimePricePerSecWeekday = MAX(CASE WHEN cat='WD' AND dr=2 THEN price END),

            PrimePricePerSecWeekend    = MAX(CASE WHEN cat='WE' AND dr=1 THEN price END),
            NonPrimePricePerSecWeekend = MAX(CASE WHEN cat='WE' AND dr=2 THEN price END)
        FROM PriceRanks
        WHERE dr IN (1,2)
        GROUP BY pricelistID
    )
    INSERT INTO #SegPrices
    (
        massmediaID, [name], pricelistID, SegmentStart, SegmentEnd,
        PrimePricePerSecWeekday, NonPrimePricePerSecWeekday,
        PrimePricePerSecWeekend, NonPrimePricePerSecWeekend,
		extraChargeFirstRoller, extraChargeSecondRoller, extraChargeLastRoller
    )
    SELECT
        s.massmediaID,
        s.[name],
        s.pricelistID,
        s.SegmentStart,
        s.SegmentEnd,

        -- если в категории “не-прайм” нет 2-й цены — приравниваем к прайму
        PrimePricePerSecWeekday =
            COALESCE(NULLIF(p.PrimePricePerSecWeekday, 0), NULLIF(p.PrimePricePerSecWeekend, 0), 0),

        NonPrimePricePerSecWeekday =
            COALESCE(NULLIF(p.NonPrimePricePerSecWeekday, 0),
                     NULLIF(p.PrimePricePerSecWeekday, 0),
                     NULLIF(p.PrimePricePerSecWeekend, 0),
                     0),

        PrimePricePerSecWeekend =
            COALESCE(NULLIF(p.PrimePricePerSecWeekend, 0), NULLIF(p.PrimePricePerSecWeekday, 0), 0),

        NonPrimePricePerSecWeekend =
            COALESCE(NULLIF(p.NonPrimePricePerSecWeekend, 0),
                     NULLIF(p.PrimePricePerSecWeekend, 0),
                     NULLIF(p.PrimePricePerSecWeekday, 0),
                     0),

		s.extraChargeFirstRoller, s.extraChargeSecondRoller, s.extraChargeLastRoller
    FROM #Segments s
    JOIN PLPrices p ON p.pricelistID = s.pricelistID;

    -- Resultset #1: 1 строка на станцию (для грида)
    SELECT
        s.massmediaID,
        s.[name],
        m.groupName,
        m.name as shortName,
        PrimePricePerSecWeekday    = MAX(PrimePricePerSecWeekday),
        NonPrimePricePerSecWeekday = MAX(NonPrimePricePerSecWeekday),

        PrimePricePerSecWeekend    = MAX(PrimePricePerSecWeekend),
        NonPrimePricePerSecWeekend = MAX(NonPrimePricePerSecWeekend)
    FROM #SegPrices s
        INNER JOIN vMassMedia m ON m.massmediaID = s.massmediaID
    GROUP BY s.massmediaID, s.[name], m.groupName, m.name
    ORDER BY [name];

    -- Resultset #2: по сегментам прайслистов
    SELECT
        massmediaID,
        [name],
        pricelistID,
        SegmentStart,
        SegmentEnd,

        PrimePricePerSecWeekday,
        NonPrimePricePerSecWeekday,
        PrimePricePerSecWeekend,
        NonPrimePricePerSecWeekend,
		extraChargeFirstRoller,
		extraChargeSecondRoller,
		extraChargeLastRoller
    FROM #SegPrices
    ORDER BY [name], SegmentStart;

    -- Resultset #3: информация о менеджерском коэффициенте
    SELECT * FROM UserDiscount WHERE userID = @loggedUserID
END
