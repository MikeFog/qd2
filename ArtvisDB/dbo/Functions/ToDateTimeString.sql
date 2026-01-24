

CREATE    FUNCTION [dbo].[ToDateTimeString](@date as datetime)
RETURNS VARCHAR(16)
AS
BEGIN
	Return Convert(varchar(10), @date, 104) + ' ' + Convert(varchar(5), @date, 114)
END

