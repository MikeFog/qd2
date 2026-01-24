


CREATE   FUNCTION dbo.fn_FirstDateOfMonth
(
@date datetime
)
RETURNS datetime
AS
BEGIN
	Return Convert(datetime, '01.' + Ltrim(DatePart(m, @date)) + '.' + Ltrim(DatePart(yyyy, @date)), 104)
END


