
CREATE PROC [dbo].[IsModuleExist]
(
@modulePriceListID INT,
@date DATETIME
)

AS
SET NOCOUNT on

DECLARE @res INT, @count1 INT, @count2 INT 

SELECT @count1 =COUNT(*) FROM [ModuleTariff] WHERE [modulePriceListID] = @modulePriceListID

SELECT @count2 = COUNT(*)
FROM 
	[ModuleTariff] mt
	INNER JOIN [TariffWindow] w ON mt.[tariffID] = w.[tariffId]
		AND w.dayOriginal = @date
WHERE 
	mt.[modulePriceListID] = @modulePriceListID		

IF @count1 = @count2
	SELECT 1
ELSE
	SELECT 0

