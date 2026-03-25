CREATE FUNCTION [dbo].[fn_GetMaxUserDiscount]
(
    @userID INT,
    @startDate DATETIME,
    @finishDate DATETIME
)
RETURNS DECIMAL(18, 4)
AS
BEGIN
    DECLARE @maxRatio DECIMAL(18, 4);
    DECLARE @hasGap BIT = 0;
    DECLARE @result DECIMAL(18, 4);

    -- 1. Находим максимальный коэффициент из базы за этот период
    SELECT @maxRatio = MAX(maxRatio)
    FROM UserDiscount
    WHERE userID = @userID
      AND startDate <= @finishDate    -- ИСПРАВЛЕНО: было <
      AND finishDate >= @startDate;   -- ИСПРАВЛЕНО: было >

    -- 2. Проверяем наличие реальных гэпов (разрыв больше 1 дня)
    IF EXISTS (
        SELECT 1
        FROM UserDiscount ud1
        INNER JOIN UserDiscount ud2 ON ud1.userID = ud2.userID
            AND ud2.startDate > ud1.finishDate
            AND DATEDIFF(DAY, ud1.finishDate, ud2.startDate) > 1
        WHERE ud1.userID = @userID
            AND ud1.startDate <= @finishDate    -- ИСПРАВЛЕНО
            AND ud1.finishDate >= @startDate    -- ИСПРАВЛЕНО
            AND ud2.startDate <= @finishDate    -- ИСПРАВЛЕНО
            AND ud2.finishDate >= @startDate    -- ИСПРАВЛЕНО
    )
    BEGIN
        SET @hasGap = 1;
    END

    -- 3. Проверяем покрытие начала и конца периода
    IF NOT EXISTS (
        SELECT 1 FROM UserDiscount
        WHERE userID = @userID
            AND startDate <= @startDate
            AND finishDate >= @startDate
    )
    OR NOT EXISTS (
        SELECT 1 FROM UserDiscount
        WHERE userID = @userID
            AND startDate <= @finishDate
            AND finishDate >= @finishDate
    )
    BEGIN
        SET @hasGap = 1;
    END

    -- 4. Логика выбора результата
    IF @hasGap = 1
    BEGIN
        IF @maxRatio > 1.0
            SET @result = @maxRatio;
        ELSE
            SET @result = 1.0;
    END
    ELSE
    BEGIN
        SET @result = ISNULL(@maxRatio, 1.0);
    END

    RETURN @result;
END