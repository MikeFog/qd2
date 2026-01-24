

CREATE  FUNCTION dbo.GetMaxDate()
RETURNS DATETIME
AS
BEGIN
	Return Convert(datetime, '99991231', 112)
END

