CREATE FUNCTION dbo.fn_GetMaxUserDiscount
(
    @userID INT,
    @startDate DATETIME,
    @finishDate DATETIME
)
RETURNS DECIMAL(18, 4)
AS
BEGIN
    DECLARE @maxRatio DECIMAL(18, 4);
    DECLARE @totalCoveredSeconds INT;
    DECLARE @targetDurationSeconds INT;
    DECLARE @result DECIMAL(18, 4);

    -- 1. Находим максимальный коэффициент из базы за этот период
    SELECT @maxRatio = MAX(maxRatio)
    FROM UserDiscount
    WHERE userID = @userID
      AND startDate < @finishDate
      AND finishDate > @startDate;

    -- 2. Считаем общую длительность нашего целевого периода в секундах
    SET @targetDurationSeconds = DATEDIFF(SECOND, @startDate, @finishDate);

    -- 3. Считаем сумму секунд, которые покрыты записями в базе
    SELECT @totalCoveredSeconds = SUM(DATEDIFF(SECOND, 
                CASE WHEN startDate < @startDate THEN @startDate ELSE startDate END, 
                CASE WHEN finishDate > @finishDate THEN @finishDate ELSE finishDate END
            ))
    FROM UserDiscount
    WHERE userID = @userID
      AND startDate < @finishDate 
      AND finishDate > @startDate;

    -- 4. Основная логика выбора
    -- Если записей нет или сумма секунд покрытия меньше длительности периода, значит есть "дырки"
    IF ISNULL(@totalCoveredSeconds, 0) < @targetDurationSeconds
    BEGIN
        -- Есть зазоры (коэффициент 1.0). Выбираем максимум между 1.0 и тем, что в базе
        IF @maxRatio > 1.0
            SET @result = @maxRatio;
        ELSE
            SET @result = 1.0;
    END
    ELSE
    BEGIN
        -- Дырок нет, всё покрыто. Берем максимум из базы
        SET @result = ISNULL(@maxRatio, 1.0);
    END

    RETURN @result;
END