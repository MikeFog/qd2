CREATE        PROCEDURE [dbo].[MediaPlanRetrieve]
(
@campaignId int = null,
@campaignTypeId tinyint = null,
@isFact bit = 1,
@massmediaIDString VARCHAR(8000) = null,
@year smallint = null,
@month tinyint = null,
@actionId int = null,
@startDate datetime = null,
@finishDate datetime = null,
@onlyRollers bit = 0,
@rollerIDString VARCHAR(8000) = null
)
AS
SET NOCOUNT ON

CREATE TABLE #mm(massmediaID int)
CREATE TABLE #rr(rollerID int)

IF @massmediaIDString IS NOT NULL
	INSERT INTO #mm
	SELECT * FROM fn_CreateTableFromString(@massmediaIDString)
ELSE
	INSERT INTO #mm
	SELECT [massmediaID] FROM [MassMedia]

IF @rollerIDString IS NOT NULL
	INSERT INTO #rr
	SELECT * FROM fn_CreateTableFromString(@rollerIDString)

DECLARE @massmediaCount INT 

If @isFact = 1
	SELECT @massmediaCount = COUNT(DISTINCT tw.[massmediaID]) 
	FROM [#mm] mm 
		inner join TariffWindow tw on mm.massmediaID = tw.massmediaID
		INNER JOIN [Issue] i ON i.actualWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID
		left join #rr rr on i.rollerID = rr.rollerID
	WHERE i.campaignId = isnull(@campaignId, i.campaignID)
			and c.actionID = isnull(@actionID, c.actionID)
			and (@rollerIDString is null or rr.rollerID is not null)
else 
	select @massmediaCount = COUNT(DISTINCT tw.[massmediaID]) 
	FROM [#mm] mm 
		inner join TariffWindow tw on mm.massmediaID = tw.massmediaID
		INNER JOIN [Issue] i ON i.originalWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID
		left join #rr rr on i.rollerID = rr.rollerID
	WHERE i.campaignId = isnull(@campaignId, i.campaignID)
			and c.actionID = isnull(@actionID, c.actionID)
			and (@rollerIDString is null or rr.rollerID is not null)

Declare @issue Table(
	issueID INT,
	rollerId int,
	issueDate datetime,
	comment nvarchar(32),
	positionID SMALLINT,
	price MONEY,
	broadcast datetime,
	mmID smallint 
)

If @isFact = 1
	Insert Into @issue
	SELECT 
		i.issueID,
		i.rollerId,
		tw.windowDateActual,
		MAX(coalesce(t.comment, space(0))),
		i.positionId,
		tw.[price],
		pl.[broadcastStart],
		mm.massmediaID
	From
		Issue i WITH (NOLOCK)
		Inner Join TariffWindow tw On i.actualWindowID = tw.windowId 	
		LEFT Join Tariff t On t.tariffId = tw.tariffId
		INNER JOIN [Massmedia] mm ON tw.[massmediaID] = mm.[massmediaID]
		INNER JOIN [#mm] mmm ON mm.[massmediaID] = mmm.[massmediaID]
		INNER JOIN [Pricelist] pl ON mm.[massmediaID] = mm.[massmediaID]  
		inner join Campaign c on i.campaignID = c.campaignID
		left join #rr rr on i.rollerID = rr.rollerID
	Where 
		i.campaignId = isnull(@campaignId, i.campaignID)
		and c.actionID = isnull(@actionID, c.actionID)
		and (@year is null or @month is null or tw.dayActual between Convert(datetime, '01.' + cast(@month as varchar) + '.' + cast(@year as varchar), 104) and dbo.fn_LastDateOfMonth(Convert(datetime, '01.' + cast(@month as varchar) + '.' + cast(@year as varchar), 104)))
		and ((@startDate is null and @finishDate is null) or (tw.dayActual between @startDate and @finishDate))
		and (@rollerIDString is null or rr.rollerID is not null)
	GROUP BY i.issueID, i.[rollerID], tw.windowDateActual, tw.[price], i.[positionId], mm.massmediaID, pl.[broadcastStart]
	ORDER BY tw.windowDateActual, tw.[price]
Else
	Insert Into @issue
	SELECT 
		i.issueID,
		i.rollerId,
		tw.windowDateOriginal,
		MAX(coalesce(t.comment, space(0))),
		i.positionId,
		tw.[price],
		pl.[broadcastStart],
		mm.massmediaID
	From
		Issue i WITH (NOLOCK)
		Inner Join TariffWindow tw On i.originalWindowID = tw.windowId 	
		LEFT Join Tariff t On t.tariffId = tw.tariffId
		INNER JOIN [Massmedia] mm ON tw.[massmediaID] = mm.[massmediaID]
		INNER JOIN [#mm] mmm ON mm.[massmediaID] = mmm.[massmediaID]
		INNER JOIN [Pricelist] pl ON mm.[massmediaID] = mm.[massmediaID]  
		inner join Campaign c on i.campaignID = c.campaignID
		left join #rr rr on i.rollerID = rr.rollerID
	Where 
		i.campaignId = isnull(@campaignId, i.campaignID)
		and c.actionID = isnull(@actionID, c.actionID)
		and (@year is null or @month is null or tw.dayOriginal between Convert(datetime, '01.' + cast(@month as varchar) + '.' + cast(@year as varchar), 104) and dbo.fn_LastDateOfMonth(Convert(datetime, '01.' + cast(@month as varchar) + '.' + cast(@year as varchar), 104)))
		and ((@startDate is null and @finishDate is null) or (tw.dayOriginal between @startDate and @finishDate))
		and (@rollerIDString is null or rr.rollerID is not null)
	GROUP BY i.issueID, i.[rollerID], tw.[windowDateOriginal], tw.[price], i.[positionId], pl.[broadcastStart], mm.massmediaID
	ORDER BY tw.[windowDateOriginal], tw.[price]

Select 
	r.rollerId,
	r.[name], 
	r.advertTypeName,
	r.duration,
	count(*) as quantity
From
	@issue i
	Inner Join vRoller r On i.rollerId = r.rollerId
Group By
	r.[name], 
	r.advertTypeName,
	r.duration,
	r.rollerId

if @onlyRollers = 1
	return

if @campaignTypeId is not null and @campaignTypeId = 1
	SELECT
		dbo.[fn_GetTimeString](i.broadcast, i.issueDate) as [time],
		MAX(i.comment),
		CAST(i.price AS float) AS price,
		sum(r.duration)/@massmediaCount totalDuration
	From
		@issue i
		Inner Join Roller r on i.rollerId = r.rollerId
	Group By
		i.price, dbo.[fn_GetTimeString](i.broadcast, i.issueDate) 
	Order By
		dbo.[fn_GetTimeString](i.broadcast, i.issueDate)
ELSE 
	Select
		dbo.[fn_GetTimeString](i.broadcast, i.issueDate) as [time],
		MAX(i.comment),
		sum(r.duration)/@massmediaCount totalDuration
	From
		@issue i
		Inner Join Roller r on i.rollerId = r.rollerId
	Group By
		dbo.[fn_GetTimeString](i.broadcast, i.issueDate) 
	Order By
		dbo.[fn_GetTimeString](i.broadcast, i.issueDate) 

if @campaignTypeId is null or @campaignTypeId = 4
	Select 
		i.rollerId,
		Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, i.broadcast), DATEADD(hh, -DATEPART(hh, i.broadcast), i.issueDate)), 112), 112) AS issueDate,
		dbo.[fn_GetTimeString](i.broadcast, i.issueDate) as [time],
		i.positionID
	From
		@issue i
	where i.mmID = (select top 1 * from [#mm])
	Order By
		i.issueDate, dbo.[fn_GetTimeString](i.broadcast, i.issueDate)
else if @campaignTypeId is null or @campaignTypeId = 1	
	Select 
		i.rollerId,
		Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, i.broadcast), DATEADD(hh, -DATEPART(hh, i.broadcast), i.issueDate)), 112), 112) AS issueDate,
		dbo.[fn_GetTimeString](i.broadcast, i.issueDate) as [time],
		cast(i.price as float) as price,
		i.positionID
	From
		@issue i
	Order By
		i.issueDate, dbo.[fn_GetTimeString](i.broadcast, i.issueDate)
else 
	Select 
		i.rollerId,
		Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, i.broadcast), DATEADD(hh, -DATEPART(hh, i.broadcast), i.issueDate)), 112), 112) AS issueDate,
		dbo.[fn_GetTimeString](i.broadcast, i.issueDate) as [time],
		i.positionID
	From
		@issue i
	Order By
		i.issueDate, dbo.[fn_GetTimeString](i.broadcast, i.issueDate)
	
	
if @year is not null and @month is not null 
begin 
	declare @rescount table(c int)
	declare @day int, @count int
	set @day = 1
	while @day <= day([dbo].[fn_LastDateOfMonth](dateadd(mm,(@year-1900)* 12 + @month - 1,0)))
	begin
		select @count = COUNT(i.rollerId) from @issue i where day(DATEADD(mi, -DATEPART(mi, i.broadcast), DATEADD(hh, -DATEPART(hh, i.broadcast), i.issueDate))) = @day
		insert into @rescount (c) values (@count ) 
		set @day = @day + 1
	end
	
	select * from @rescount
end 
else
	Select
		COUNT(i.rollerId) AS [COUNT]
	From
		@issue i
	Group By
		Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, i.broadcast), DATEADD(hh, -DATEPART(hh, i.broadcast), i.issueDate)), 112), 112)
	Order By
		Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, i.broadcast), DATEADD(hh, -DATEPART(hh, i.broadcast), i.issueDate)), 112), 112)

-- ProgramIssues
if (@campaignTypeId is not null and @campaignTypeId = 2) or (@actionId is not null) --Sponsor Program
	Select 
		sp.[name],
		pri.issueDate
	From 
		ProgramIssue pri 
		Inner Join SponsorProgram sp On pri.programID = sp.sponsorProgramID
		inner join Campaign c on pri.campaignID = c.campaignID
		inner join #mm mm on c.massmediaID = mm.massmediaID
	Where pri.campaignId = coalesce(@campaignID, pri.campaignID)
		and c.actionID = coalesce(@actionID, c.actionID)
