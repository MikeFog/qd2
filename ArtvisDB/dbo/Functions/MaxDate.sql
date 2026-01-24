
CREATE  FUNCTION dbo.MaxDate
(
@param1 datetime,
@param2 datetime
)
RETURNS datetime
AS
BEGIN
IF @param1 Is Null RETURN @param2
IF @param2 Is Null RETURN @param1
IF @param1 > @param2 RETURN @param1
RETURN @param2

END


