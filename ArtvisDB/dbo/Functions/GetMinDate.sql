


CREATE   FUNCTION dbo.GetMinDate()
RETURNS DATETIME
AS
BEGIN
	Return Convert(datetime, '19000101', 112)
END


