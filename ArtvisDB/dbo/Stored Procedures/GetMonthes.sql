CREATE PROCEDURE [dbo].[GetMonthes]
(
	@campaignID int = null,
	@actionID int = null,
	@isFact bit = 0
)
AS
BEGIN
	SET NOCOUNT ON;

	declare @res table (MonthDate tinyint, MonthYear smallint)
	declare @camps table (campaignID int primary key)

	-- Кампании отбираем отдельным запросом с RECOMPILE: isnull(@x, c.col) без него не даёт поиска по индексу,
	-- оценка кампаний завышена, и запрос ниже сканирует всю TariffWindow (~2 млн строк, 0,7-1,9 с).
	insert into @camps
	select c.campaignID from Campaign c
	where c.campaignId = isnull(@campaignId, c.campaignID)
		and c.actionID = isnull(@actionID, c.actionID)
	option (recompile)

	if @isFact = 1
	begin
		insert into @res
		SELECT DISTINCT MonthDate = month(tw.dayActual), MonthYear = YEAR(tw.dayActual)  FROM 
			@camps c 
			inner join Issue i on i.campaignId = c.campaignId
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
	end 
	else 
	begin 
		insert into @res
		SELECT DISTINCT MonthDate = month(tw.dayOriginal), MonthYear = YEAR(tw.dayOriginal)  FROM 
			@camps c 
			inner join Issue i on i.campaignId = c.campaignId
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
	end
	
	-- Get Month and Year from Sponsor Campigns (if exists - inner)
	insert into @res 
	SELECT DISTINCT MonthDate = month(i.issueDate), MonthYear = YEAR(i.issueDate)  FROM 
		@camps c 
		inner join ProgramIssue i on i.campaignId = c.campaignId
	
	select distinct * from @res order by MonthYear, MonthDate
END
