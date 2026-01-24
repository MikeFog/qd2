/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - replace @moduleIssueID and @packModuleIssueID on @moduleID and @packModuleID
*/
CREATE     PROC [dbo].[RollerSubstitutionPassport]
(
@campaignID int,
@campaignTypeID int,
@rollerID int,
@moduleID int = null,
@packModuleID int = null 
)
AS
SET NOCOUNT ON

SELECT COUNT(*) as issues FROM Issue i
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
WHERE i.campaignID = @campaignID AND i.rollerID = @rollerID 
	and (@moduleID is null or mi.moduleID = @moduleID) 
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)

-- Rollers
DECLARE @massmediaID smallint

SELECT DISTINCT 
	r.rollerID as [id],
	r.name
FROM 
	Roller r 
	inner join Roller ro on ro.rollerID = @rollerID and r.rollerID <> @rollerID
	INNER JOIN [Action] a ON r.firmID = a.firmID
	INNER JOIN Campaign c ON c.actionID = a.actionID and c.campaignID = @campaignID
where
	r.isEnabled = 1	AND 
	r.isMute = 0 And
	r.parentID Is Null
ORDER BY 
	r.[name]
	
-- It must be original day
declare @days table (id varchar(20), parentID varchar(20), windowID int, timeString char(5), issueDate datetime, [image] varchar(50), [name] varchar(20))

insert into @days(id, issueDate,[image], [name])
select distinct	
	convert(varchar, tw.dayOriginal, 104),
	tw.dayOriginal,
	'Day.png',
	convert(varchar, tw.dayOriginal, 104)
from 
	Issue i 
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
where 
	i.[campaignID] = @campaignID AND
	i.rollerID = @rollerID  
	and (@moduleID is null or mi.moduleID = @moduleID) 
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
order by
	tw.dayOriginal

if (@campaignTypeID in (1,2))
begin 
	insert into @days(id, parentID, windowID,[image], [name],issueDate)
	select distinct
		convert(varchar, tw.dayOriginal, 104) + dbo.fn_GetTimeString(pl.broadcastStart, tw.windowDateOriginal),
		convert(varchar, tw.dayOriginal, 104),
		tw.windowId,
		'Issue.png',
		dbo.fn_GetTimeString(pl.broadcastStart, tw.windowDateOriginal),
		tw.dayOriginal
	from 
		Issue i 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
		inner join Tariff t on tw.tariffId = t.tariffID
		inner join Pricelist pl on t.pricelistID = pl.pricelistID
		left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
		left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
		left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
	where 
		i.[campaignID] = @campaignID AND
		i.rollerID = @rollerID  
		and (@moduleID is null or mi.moduleID = @moduleID) 
		and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
	order by dbo.fn_GetTimeString(pl.broadcastStart, tw.windowDateOriginal)
end 

select * from @days
