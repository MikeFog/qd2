

CREATE FUNCTION [dbo].[fn_GetTariffWindowDateRangeStr]
(
	@startDate DATETIME = null,
	@finishDate DATETIME = null,
	@broadcastStart DATETIME,
	@lang VARCHAR(10) -- язык интерфейса веба (docs/tasks/web-i18n.md); 'ru' — исходный текст
)
RETURNS NVARCHAR(255)
AS
BEGIN
	DECLARE @str NVARCHAR(255)
	
	IF (@startDate IS NULL)
		BEGIN
			SET @str = dbo.fn_Translate(@lang, N'нет тарифных окон')
		END
	ELSE
		BEGIN
			IF CAST(@finishDate AS TIME) < CAST(@broadcastStart AS TIME)
				Set @finishDate = DATEADD(dd, -1, @finishDate)	
			SET @str = dbo.fn_Translate(@lang, N'тарифные окна: ') + CONVERT(VARCHAR(255), @startDate, 104) + ' - ' + CONVERT(VARCHAR(255), @finishDate, 104)
		END
		
	RETURN @str
END

