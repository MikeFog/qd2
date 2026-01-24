CREATE PROC [dbo].[Grid]
(
@massmediaID smallint,
@startDate datetime,
@finishDate datetime,
@showUnconfirmed bit,
@campaignID int = -1,
@position smallint = 0,
@moduleID int = null
)
AS

SET NOCOUNT ON
SET DATEFIRST 1

declare @a datetime
declare @b datetime
set	@a = dbo.ToShortDate(@startDate)
set	@b = dateadd(day, -1, dbo.ToShortDate(@finishDate))

declare @res table(rollerDuration int, 
	windowDateOriginal datetime, 
	timeString varchar(10), 
	[weekday] tinyint, 
	campaignID int, 
	positionId smallint, 
	originalWindowID int, 
	moduleID int, 
	rollerID int)

insert into @res (
	rollerDuration,
	windowDateOriginal,
	timeString,
	[weekday],
	campaignID,
	positionId,
	originalWindowID,
	moduleID,
	rollerID)
SELECT 
	r.duration as rollerDuration, 
	tw.windowDateOriginal,
	CONVERT(varchar(5), tw.windowDateOriginal, 108) as timeString,
	DatePart(dw,tw.dayOriginal) as [weekday],
	i.campaignID,
	i.positionId,
	i.originalWindowID,
	mi.moduleID,
	i.rollerID
FROM 
	Issue i WITH (NOLOCK)
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	inner join Roller r  on i.rollerID = r.rollerID
	INNER JOIN Campaign c ON c.campaignID = i.campaignID 
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
where
	c.massmediaID = @massmediaID AND
	tw.dayOriginal between @a And @b AND
	i.campaignID = Coalesce(@campaignID, i.campaignID) AND
	(i.isConfirmed = 1 OR @showUnconfirmed = 1 OR i.campaignID = @campaignID)

select * from @res as r where (@moduleID is null or r.moduleID = @moduleID) 

select [weekday], count(*) as [count]
from @res
group by [weekday]

declare @firmId int
select @firmId = a.firmId From Campaign c inner join Action a on a.actionID = c.actionID where c.campaignID = @campaignID

select 
	distinct tw.windowId
from 
	Issue i WITH (NOLOCK)
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	INNER JOIN Campaign c ON c.campaignID = i.campaignID 
	Inner Join Action a on a.actionID = c.actionID
where
	c.massmediaID = @massmediaID 
	And tw.dayOriginal between @a And @b
	And a.firmID = @firmId
	And (i.isConfirmed = 1 OR @showUnconfirmed = 1)
	And a.deleteDate Is Null
