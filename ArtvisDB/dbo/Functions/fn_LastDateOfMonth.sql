



CREATE    FUNCTION dbo.fn_LastDateOfMonth
(
@date datetime
)
RETURNS datetime
AS
BEGIN
	Return DateAdd(m, 1, dbo.fn_FirstDateOfMonth(@date)) - 1

END



