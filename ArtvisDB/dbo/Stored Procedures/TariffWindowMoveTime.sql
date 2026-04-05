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
    

    UPDATE tw 
    SET tw.windowDateActual = 
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
        -- Фильтр по дням недели (исправлено с учетом DATEFIRST 1)
        AND (
            (@monday = 1 AND DATEPART(dw, tw.dayOriginal) = 1) OR    -- Monday
            (@tuesday = 1 AND DATEPART(dw, tw.dayOriginal) = 2) OR   -- Tuesday
            (@wednesday = 1 AND DATEPART(dw, tw.dayOriginal) = 3) OR -- Wednesday
            (@thursday = 1 AND DATEPART(dw, tw.dayOriginal) = 4) OR  -- Thursday
            (@friday = 1 AND DATEPART(dw, tw.dayOriginal) = 5) OR    -- Friday
            (@saturday = 1 AND DATEPART(dw, tw.dayOriginal) = 6) OR  -- Saturday
            (@sunday = 1 AND DATEPART(dw, tw.dayOriginal) = 7)       -- Sunday
        )
    
END