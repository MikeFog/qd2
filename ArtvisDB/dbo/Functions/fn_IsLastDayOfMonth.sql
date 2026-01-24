
CREATE  FUNCTION dbo.fn_IsLastDayOfMonth
(
@theDate datetime
)
RETURNS INT
AS
BEGIN

Declare	@nextDay datetime
Set	@nextDay = DateAdd(dd, 1, @theDate)

If	Month(@nextDay) = Month(@theDate) return 0
return 1

END



