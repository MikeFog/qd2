-- Калькулятор цены, этап К.0 (docs/tasks/web-price-calculator.md, §4.3 п. 1–2). Обе процедуры зовёт только калькулятор
-- десктопа («Рекламный отдел -> Калькулятор цены») и оценка цены Шаблона №3; веб их пока не зовёт.
--
-- 1. pc_GetStationsPriceModel:
--    - новый необязательный @userID: третья выборка (периоды сезонного коэффициента UserDiscount) — только этого
--      пользователя. Было SELECT * FROM UserDiscount без отбора: каждый, кто открывал калькулятор, получал коэффициенты
--      всех менеджеров, а клиент отбирал нужного у себя (К-17). Без @userID (старый клиент) — как раньше, все;
--    - название станции во временных таблицах nvarchar(317), как vMassmedia.nameWithGroup: varchar(64) терял символы
--      вне cp1251 (К-17). Первая и вторая выборки по составу и порядку прежние.
-- 2. pc_PackageDiscountCalculateModel (пакетная скидка калькулятора и Шаблона №3):
--    - станции без выходов (durationSec = 0 — 0 выходов или «Пр-ть ролика» 0) в пакете не участвуют, как пустые
--      кампании в hlp_ActionDiscountCalculate: раньше отмеченная станция с 0 выходов входила в число станций и в среднюю
--      длительность, и пакет срывался (К-5);
--    - при равных скидках берётся прайс-лист пакетной скидки с меньшим ID, как в hlp_ActionDiscountCalculate.
--
-- Новый клиент qd2 передаёт @userID; со старой процедурой параметр молча отбрасывается (кэш параметров DataAccessor),
-- старый клиент с новой процедурой получает все периоды, как раньше — порядок наката клиента и скрипта любой.
--
-- Порядок: в любое время, независимо от других скриптов; идемпотентен (CREATE OR ALTER).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 53_price-calculator-k0.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[pc_GetStationsPriceModel]
(
    @MassmediaGroupID  int = null,
    @DateFrom          date,
    @DateTo            date,
    @userID            int = null   -- чьи периоды коэффициента отдать третьей выборкой; NULL — всех, как раньше
)
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID('tempdb..#Segments') IS NOT NULL DROP TABLE #Segments;
    IF OBJECT_ID('tempdb..#SegPrices') IS NOT NULL DROP TABLE #SegPrices;

    CREATE TABLE #Segments
    (
        massmediaID   smallint     NOT NULL,
        [name]        nvarchar(317) NOT NULL,   -- как vMassmedia.nameWithGroup: varchar терял символы вне cp1251
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
        [name]                       nvarchar(317) NOT NULL,
        pricelistID                  smallint     NOT NULL,
        SegmentStart                 date         NOT NULL,
        SegmentEnd                   date         NOT NULL,

        PrimePricePerSecWeekday      decimal(18,2)        NOT NULL,
        NonPrimePricePerSecWeekday   decimal(18,2)        NOT NULL,
        PrimePricePerSecWeekend      decimal(18,2)        NOT NULL,
        NonPrimePricePerSecWeekend   decimal(18,2)        NOT NULL,
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
    SELECT * FROM UserDiscount WHERE @userID IS NULL OR userID = @userID
END
GO

CREATE OR ALTER PROC [dbo].[pc_PackageDiscountCalculateModel]
(
    @startDate datetime,
    @campaignTypeID tinyint,     -- у вас всегда 1
    @priceTotal decimal(18,2),           -- сумма по выбранным станциям (аналог Sum(Campaign.price))
    @sel dbo.pc_SelectedMassmedia READONLY,
    @discountValue decimal(9,4) OUTPUT,
    @packageDiscountPriceListID int OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @campaignsCount int;
	Declare @avgDurationSec float;
    -- станции без выходов в пакете не участвуют, как пустые кампании в hlp_ActionDiscountCalculate: иначе они
    -- тянули вниз среднюю длительность и завышали число станций, и пакет срывался
    SELECT @campaignsCount = COUNT(DISTINCT massmediaID), @avgDurationSec = AVG(CAST(durationSec as float)) FROM @sel WHERE durationSec > 0;

    SET @discountValue = 1;
    SET @packageDiscountPriceListID = NULL;

    ;WITH t AS
    (
        SELECT
            m.packageDiscountPriceListID,
            campaignsCount = COUNT(DISTINCT s.massmediaID)
        FROM @sel s
        JOIN PackageDiscountMassmedia m
            ON m.massmediaID = s.massmediaID
           AND (
                (@campaignTypeID=1 AND m.isForType1=1) OR
                (@campaignTypeID=2 AND m.isForType2=1) OR
                (@campaignTypeID=3 AND m.isForType3=1)
           )
        JOIN PackageDiscountPriceList p
            ON p.packageDiscountPriceListID = m.packageDiscountPriceListID
           AND @startDate BETWEEN p.startDate AND p.finishDate
           AND p.value <= @priceTotal
        WHERE
            s.durationSec > 0
            AND CAST(s.durationSec as float) >= @avgDurationSec * p.eachVolume / 100.0
        GROUP BY
            m.packageDiscountPriceListID
        HAVING
            -- пакет должен включать ровно столько станций, сколько выбрано (как в hlp_ActionDiscountCalculate)
            COUNT(DISTINCT m.massmediaID) = @campaignsCount
            AND COUNT(DISTINCT s.massmediaID) = @campaignsCount
    )
    SELECT TOP 1
        @discountValue = COALESCE(pl.discount, 1),
        @packageDiscountPriceListID = pl.packageDiscountPriceListID
    FROM t
    JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID = t.packageDiscountPriceListID
    JOIN PackageDiscount d ON d.packageDiscountId = pl.packageDiscountID
    WHERE
        d.count = t.campaignsCount
    ORDER BY
        pl.discount ASC,  -- самый выгодный клиенту (минимальный коэффициент)
        pl.packageDiscountPriceListID;  -- при равных — меньший ID, как в hlp_ActionDiscountCalculate
END
GO

-- Проверка: новые версии обеих процедур применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.pc_GetStationsPriceModel')) LIKE N'%@userID IS NULL OR userID = @userID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.pc_GetStationsPriceModel')) LIKE N'%nvarchar(317)%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.pc_PackageDiscountCalculateModel')) LIKE N'%FROM @sel WHERE durationSec > 0%'
    PRINT N'ГОТОВО: pc_GetStationsPriceModel (@userID, nvarchar) и pc_PackageDiscountCalculateModel (без станций с 0 выходов).';
ELSE
    RAISERROR(N'53: новая версия применена не ко всем процедурам.', 16, 1);
GO
