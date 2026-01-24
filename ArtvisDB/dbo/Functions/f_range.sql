CREATE FUNCTION [dbo].[f_range] 
(
@start int,
@end int
)
RETURNS 
@n TABLE 
(
n int primary key
)
AS
BEGIN

	DECLARE @i int = @start

	WHILE @i <= @end
	BEGIN
		INSERT @n VALUES (@i)

		SET @i = @i + 1
	END	
RETURN 
END
