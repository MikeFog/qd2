

CREATE  FUNCTION dbo.ToShortDate(@date as datetime)
RETURNS DATETIME
AS
BEGIN
	Return Convert(datetime, Convert(varchar(8), @date, 112), 112)
END

