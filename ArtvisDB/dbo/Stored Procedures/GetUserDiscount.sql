
CREATE PROCEDURE [dbo].[GetUserDiscount]
    @UserId SMALLINT,
    @CheckDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Discount FLOAT;
    
    -- Поиск скидки для пользователя на указанную дату
    SELECT TOP 1 @Discount = maxRatio
    FROM [dbo].[UserDiscount]
    WHERE userID = @UserId
      AND @CheckDate >= startDate
      AND @CheckDate <= finishDate
    ORDER BY maxRatio ASC; -- Если несколько скидок, берем минимальную (максимальную скидку для клиента)
    
    -- Если скидка не найдена, вернуть 1
    IF @Discount IS NULL
        SET @Discount = 1.0;
    
    SELECT @Discount AS Discount;
END
