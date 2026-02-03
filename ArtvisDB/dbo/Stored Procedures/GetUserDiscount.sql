CREATE PROCEDURE [dbo].[GetUserDiscount]
    @UserId SMALLINT,
    @startDate DATETIME,
    @finishDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Discount FLOAT;
    
    -- Поиск минимальной скидки (maxRatio) для пользователя на указанном диапазоне дат
    SELECT TOP 1 @Discount = maxRatio
    FROM [dbo].[UserDiscount]
    WHERE userID = @UserId
      AND (startDate <= @finishDate AND finishDate >= @startDate)
    ORDER BY maxRatio ASC; -- Берем минимальную maxRatio (максимальную скидку для клиента)
    
    -- Если скидка не найдена, вернуть 1
    IF @Discount IS NULL
        SET @Discount = 1.0;
    
    SELECT @Discount AS Discount;
END
