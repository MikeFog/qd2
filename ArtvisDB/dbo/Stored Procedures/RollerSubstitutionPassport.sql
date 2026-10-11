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
	
-- Этап 5 (docs/tasks/window-actual-switch.md): дни и окна — по окну выхода; день — день модуля/пакета или день
-- окна выхода, как в RollerSubstitute (ключ #days должен совпадать с её курсором). Формат id не менять: его
-- разбирает RollerChangeAdvertTypeForm.
declare @days table (id varchar(20), parentID varchar(20), windowID int, timeString char(5), issueDate datetime, [image] varchar(50), [name] varchar(20))

insert into @days(id, issueDate,[image], [name])
select distinct
	convert(varchar, dd.issueDay, 104),
	dd.issueDay,
	'Day.png',
	convert(varchar, dd.issueDay, 104)
from
	Issue i
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
	cross apply (select issueDay = coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal)) dd
where
	i.[campaignID] = @campaignID AND
	i.rollerID = @rollerID
	and (@moduleID is null or mi.moduleID = @moduleID)
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
order by
	dd.issueDay

if (@campaignTypeID in (1,2))
begin 
	insert into @days(id, parentID, windowID,[image], [name],issueDate)
	select distinct
		-- id — ключ слота (время по расписанию окна выхода, без дублей); подпись — фактическое время выхода
		convert(varchar, tw.dayOriginal, 104) + CONVERT(varchar(5), tw.windowDateOriginal, 108),
		convert(varchar, tw.dayOriginal, 104),
		tw.windowId,
		'Issue.png',
		CONVERT(varchar(5), tw.windowDateActual, 108),
		tw.dayOriginal
	from
		Issue i
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
		left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
		left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
	where 
		i.[campaignID] = @campaignID AND
		i.rollerID = @rollerID  
		and (@moduleID is null or mi.moduleID = @moduleID) 
		and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
	order by CONVERT(varchar(5), tw.windowDateActual, 108)
end 

select * from @days order by issueDate