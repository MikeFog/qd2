-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 02.02.2009
-- Description:	Шаблонный перенос времени
-- =============================================
CREATE PROCEDURE [dbo].[TariffWindowMoveTime]
(
    @time datetime,
    @newtime datetime,
    @startdate datetime,
    @finishdate datetime,
    @pricelistid int,
    @monday bit = 0,
    @tuesday bit = 0,
    @wednesday bit = 0,
    @thursday bit = 0,
    @friday bit = 0,
    @saturday bit = 0,
    @sunday bit = 0
)
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFIRST 1; -- Понедельник = 1

    DECLARE @needaddday bit

    IF EXISTS(
        SELECT *
        FROM Pricelist pl
        WHERE pl.PricelistID = @pricelistID
            AND @time < pl.broadcastStart
    )
        SET @needaddday = 1
    ELSE
        SET @needaddday = 0

    -- Окна под перенос + их будущее фактическое время выхода.
    DECLARE @moved TABLE (windowId int PRIMARY KEY, newActual datetime NOT NULL);

    INSERT INTO @moved (windowId, newActual)
    SELECT
        tw.windowId,
        CONVERT(datetime,
            LEFT(CONVERT(varchar, CASE @needaddday WHEN 1 THEN DATEADD(day, 1, tw.dayOriginal) ELSE tw.dayOriginal END, 120), 11)
            + RIGHT(CONVERT(varchar, @newtime, 120), 8),
        120)
    FROM TariffWindow tw
        INNER JOIN Pricelist pl ON tw.massmediaID = pl.massmediaID
            AND pl.pricelistID = @pricelistid
    WHERE tw.dayOriginal BETWEEN @startdate AND @finishdate
        AND tw.windowDateOriginal = CONVERT(datetime,
                LEFT(CONVERT(varchar, CASE @needaddday WHEN 1 THEN DATEADD(day, 1, tw.dayOriginal) ELSE tw.dayOriginal END, 120), 11)
                + RIGHT(CONVERT(varchar, @time, 120), 8),
            120)
        AND (
            (@monday    = 1 AND DATEPART(dw, tw.dayOriginal) = 1) OR
            (@tuesday   = 1 AND DATEPART(dw, tw.dayOriginal) = 2) OR
            (@wednesday = 1 AND DATEPART(dw, tw.dayOriginal) = 3) OR
            (@thursday  = 1 AND DATEPART(dw, tw.dayOriginal) = 4) OR
            (@friday    = 1 AND DATEPART(dw, tw.dayOriginal) = 5) OR
            (@saturday  = 1 AND DATEPART(dw, tw.dayOriginal) = 6) OR
            (@sunday    = 1 AND DATEPART(dw, tw.dayOriginal) = 7)
        );

    -- Соседи переносимых окон по объединению, см. docs/window-merging.md §3 (#1):
    -- цепочка окон (windowPrevId/windowNextId) и объединение тарифов (TariffUnion —
    -- окно тарифа и окно его тарифа-продолжения в тот же день).
    -- Драйвер — @moved (мало строк), соседи — по PK и (tariffId, dayOriginal).
    -- Полусвязи цепочки не ловятся.
    DECLARE @links TABLE (windowId int NOT NULL, neighborId int NOT NULL, isNext bit NOT NULL, isTariffUnion bit NOT NULL);

    INSERT INTO @links (windowId, neighborId, isNext, isTariffUnion)
    SELECT m.windowId, cur.windowPrevId, 0, 0
    FROM @moved m
        INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
    WHERE cur.windowPrevId IS NOT NULL
    UNION ALL
    SELECT m.windowId, cur.windowNextId, 1, 0
    FROM @moved m
        INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
    WHERE cur.windowNextId IS NOT NULL
    UNION ALL
    SELECT m.windowId, p.windowId, 0, 1
    FROM @moved m
        INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
        INNER JOIN TariffUnion tu ON tu.tariffUnionID = cur.tariffId
        INNER JOIN TariffWindow p ON p.tariffId = tu.tariffID AND p.dayOriginal = cur.dayOriginal
    UNION ALL
    SELECT m.windowId, n.windowId, 1, 1
    FROM @moved m
        INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
        INNER JOIN TariffUnion tu ON tu.tariffID = cur.tariffId
        INNER JOIN TariffWindow n ON n.tariffId = tu.tariffUnionID AND n.dayOriginal = cur.dayOriginal;

    -- Окна, перенос которых нарушил бы порядок объединённых окон по будущему факт.
    -- времени (предыдущее строго раньше, следующее строго позже), не переносятся;
    -- остальные переносятся. На окно — одна строка: первый нарушенный сосед.
    DECLARE @skipped TABLE (windowId int PRIMARY KEY, neighborId int NOT NULL, neighborActual datetime NOT NULL,
        isNext bit NOT NULL, isTariffUnion bit NOT NULL);

    INSERT INTO @skipped (windowId, neighborId, neighborActual, isNext, isTariffUnion)
    SELECT windowId, neighborId, neighborActual, isNext, isTariffUnion
    FROM (
        SELECT l.windowId, l.neighborId, l.isNext, l.isTariffUnion,
            COALESCE(mn.newActual, n.windowDateActual) AS neighborActual,
            ROW_NUMBER() OVER (PARTITION BY l.windowId ORDER BY l.isTariffUnion, l.isNext) AS rn
        FROM @links l
            INNER JOIN @moved m ON m.windowId = l.windowId
            INNER JOIN TariffWindow n ON n.windowId = l.neighborId
            LEFT JOIN @moved mn ON mn.windowId = n.windowId
        WHERE (l.isNext = 0 AND COALESCE(mn.newActual, n.windowDateActual) >= m.newActual)
            OR (l.isNext = 1 AND m.newActual >= COALESCE(mn.newActual, n.windowDateActual))
    ) v
    WHERE v.rn = 1;

    DELETE m
    FROM @moved m
        INNER JOIN @skipped s ON s.windowId = m.windowId;

    UPDATE tw
    SET tw.windowDateActual = m.newActual
    FROM TariffWindow tw
        INNER JOIN @moved m ON m.windowId = tw.windowId;

    SELECT movedCount = COUNT(*) FROM @moved;

    -- Не перенесённые окна и нарушенный сосед (время оригинальное — как в сетке
    -- трафика, у соседа и фактическое).
    SELECT
        cur.windowDateOriginal,
        neighborDateOriginal = n.windowDateOriginal,
        s.neighborActual,
        s.isNext,
        s.isTariffUnion
    FROM @skipped s
        INNER JOIN TariffWindow cur ON cur.windowId = s.windowId
        INNER JOIN TariffWindow n ON n.windowId = s.neighborId
    ORDER BY cur.windowDateOriginal;
END
