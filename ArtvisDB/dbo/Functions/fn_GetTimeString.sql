-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 05.03.2008
-- Description:	<Description, ,>
-- =============================================
CREATE FUNCTION fn_GetTimeString
(
	@broadcastTime DATETIME,
	@date DATETIME 
)
RETURNS VARCHAR(5)
AS
BEGIN
	DECLARE @hour INT, @minute INT 
	SET @hour = DATEPART(hh, @date)
	SET @minute = DATEPART(mi, @date)
	
	DECLARE @hourBr INT, @minuteBr INT 
	SET @hourBr = DATEPART(hh, @broadcastTime)
	SET @minuteBr = DATEPART(mi, @broadcastTime)
	
	IF ((@minute + @hour * 60) < (@minuteBr + @hourBr * 60))
		SET @hour = @hour + 24


	RETURN
		CASE WHEN (@hour < 10) THEN '0' ELSE '' END + CAST(@hour AS VARCHAR(2))
		+ ':' +
		CASE WHEN (@minute < 10) THEN '0' ELSE '' END + CAST(@minute AS VARCHAR(2))
END
