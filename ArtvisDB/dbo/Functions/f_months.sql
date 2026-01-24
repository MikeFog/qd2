CREATE FUNCTION [dbo].[f_months] 
(
@startDate datetime,
@finishDate datetime
)
RETURNS 
@n TABLE 
(
startDate datetime, 
finishDate datetime,
y smallint,
m tinyint,
primary key clustered (startDate, finishDate),
INDEX i1 UNIQUE (y, m)
)
AS
BEGIN
	DECLARE @startDay tinyint, @finishDay tinyint, @startMonth tinyint, @finishMonth tinyint, @startYear smallint, @finishYear smallint

	SET @startDay = datepart(day, @startDate)
	SET @startMonth = datepart(month, @startDate)
	SET @startYear = datepart(year, @startDate)
	SET @finishDay = datepart(day, @finishDate)
	SET @finishMonth = datepart(month, @finishDate)
	SET @finishYear = datepart(year, @finishDate)

	IF @startYear = @finishYear
	BEGIN
		INSERT @n 
		SELECT convert(datetime,CASE WHEN n = @startMonth THEN CAST(@startDay AS varchar) + '.' ELSE '01.' END + cast(n as varchar) + '.' + cast(@startYear as varchar), 104),
			CASE 
				WHEN n = @finishMonth 
				THEN convert(datetime,CAST(@finishDay AS varchar) + '.' + cast(n as varchar) + '.' + cast(@finishYear as varchar), 104)
				ELSE dateadd(day, -1, dateadd(month, 1, convert(datetime,'01.' + cast(n as varchar) + '.' + cast(@finishYear as varchar), 104)))
			END,
			@startYear, 
			n
		FROM f_range(@startMonth, @finishMonth)
	END
	ELSE
	BEGIN
		INSERT @n 
		SELECT convert(datetime,CASE WHEN n = @startMonth THEN CAST(@startDay AS varchar) + '.' ELSE '01.' END + cast(n as varchar) + '.' + cast(@startYear as varchar), 104),
			dateadd(day, -1, dateadd(month, 1, convert(datetime,'01.' + cast(n as varchar) + '.' + cast(@startYear as varchar), 104))),
			@startYear, 
			n
		FROM f_range(@startMonth, 12)

		INSERT @n 
		SELECT 
			convert(datetime,'01.' + cast(b.n as varchar) + '.' + cast(a.n as varchar), 104),
			dateadd(day, -1, dateadd(month, 1, convert(datetime,'01.' + cast(b.n as varchar) + '.' + cast(a.n as varchar), 104))),
			a.n, 
			b.n
		FROM f_range(@startYear+1,@finishYear-1) a
			CROSS JOIN f_range(1, 12) b

		INSERT @n 
		SELECT 
			convert(datetime,'01.' + cast(n as varchar) + '.' + cast(@finishYear as varchar), 104),
			CASE 
				WHEN n = @finishMonth 
				THEN convert(datetime,CAST(@finishDay AS varchar) + '.' + cast(n as varchar) + '.' + cast(@finishYear as varchar), 104)
				ELSE dateadd(day, -1, dateadd(month, 1, convert(datetime,'01.' + cast(n as varchar) + '.' + cast(@finishYear as varchar), 104)))
			END,
			@finishYear, 
			n
		FROM f_range(1, @finishMonth)
	END

	RETURN 
END
