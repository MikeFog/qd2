

CREATE FUNCTION [dbo].[fn_GetTariffWindowDateRangeStr]
(
	@startDate DATETIME = null,
	@finishDate DATETIME = null,
	@broadcastStart DATETIME
)
RETURNS VARCHAR(255)
AS
BEGIN
	DECLARE @str VARCHAR(255)
	
	IF (@startDate IS NULL)
		BEGIN
			SET @str = 'нет тарифных окон'
		END
	ELSE
		BEGIN
			IF CAST(@finishDate AS TIME) < CAST(@broadcastStart AS TIME)
				Set @finishDate = DATEADD(dd, -1, @finishDate)	
			SET @str = 'тарифные окна: ' + CONVERT(VARCHAR(255), @startDate, 104) + ' - ' + CONVERT(VARCHAR(255), @finishDate, 104)
		END
		
	RETURN @str
END

