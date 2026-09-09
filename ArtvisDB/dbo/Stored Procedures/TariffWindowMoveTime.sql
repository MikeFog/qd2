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

    -- Порядок цепочки по будущему факт. времени: голова строго раньше хвоста.
    -- Драйвер — @moved (мало строк), соседи — по PK. Полусвязи не ловятся.
    IF EXISTS (
        SELECT 1
        FROM @moved m
            INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
            LEFT JOIN TariffWindow p  ON p.windowId = cur.windowPrevId
            LEFT JOIN @moved mp       ON mp.windowId = p.windowId
            LEFT JOIN TariffWindow n  ON n.windowId = cur.windowNextId
            LEFT JOIN @moved mn       ON mn.windowId = n.windowId
        WHERE
            (p.windowId IS NOT NULL
                AND COALESCE(mp.newActual, p.windowDateActual) >= m.newActual)
            OR
            (n.windowId IS NOT NULL
                AND m.newActual >= COALESCE(mn.newActual, n.windowDateActual))
    )
    BEGIN
        RAISERROR('LinkedWindowsWrongOrder', 16, 1);
        RETURN;
    END

    UPDATE tw
    SET tw.windowDateActual = m.newActual
    FROM TariffWindow tw
        INNER JOIN @moved m ON m.windowId = tw.windowId;
END
