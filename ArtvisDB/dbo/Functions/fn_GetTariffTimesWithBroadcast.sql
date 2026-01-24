
-- =============================================
--Добавляет 1 день к времени тарифа, если он ДО broadcast start
-- =============================================
CREATE FUNCTION [dbo].[fn_GetTariffTimesWithBroadcast]
(	
	@time DATETIME,
	@broadcastStart DATETIME
)
RETURNS smalldatetime
AS
BEGIN
	IF @time < @broadcastStart
		Set @time = dateadd(day, 1, @time)

	Return @time
END
