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

	if @isFact = 1
	begin
		insert into @res
		SELECT DISTINCT MonthDate = month(tw.dayActual), MonthYear = YEAR(tw.dayActual)  FROM 
			Campaign c 
			inner join Issue i on i.campaignId = c.campaignId
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
		WHERE c.campaignId = isnull(@campaignId, c.campaignID)
			and c.actionID = isnull(@actionID, c.actionID)
	end 
	else 
	begin 
		insert into @res
		SELECT DISTINCT MonthDate = month(tw.dayOriginal), MonthYear = YEAR(tw.dayOriginal)  FROM 
			Campaign c 
			inner join Issue i on i.campaignId = c.campaignId
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
		WHERE c.campaignId = isnull(@campaignId, c.campaignID)
			and c.actionID = isnull(@actionID, c.actionID)
	end
	
	-- Get Month and Year from Sponsor Campigns (if exists - inner)
	insert into @res 
	SELECT DISTINCT MonthDate = month(i.issueDate), MonthYear = YEAR(i.issueDate)  FROM 
		Campaign c 
		inner join ProgramIssue i on i.campaignId = c.campaignId
	WHERE c.campaignId = isnull(@campaignId, c.campaignID)
		and c.actionID = isnull(@actionID, c.actionID)
	
	select distinct * from @res order by MonthYear, MonthDate
END
