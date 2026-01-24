/*
Получает данные для показа дней и выпусков в виде дерева
*/
CREATE   PROC [dbo].[CampaignDaysTreePassport]
(
@campaignID int,
@campaignTypeID tinyint,
@objectID int = NULL, 
@positionID int = NULL
)
AS
SET NOCOUNT ON

-- It must be original day
declare @days table (id varchar(20), parentID varchar(20), issueDate datetime, [image] varchar(50), [name] nvarchar(128))

if (@campaignTypeID in (1,2))
	begin 
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
		i.[campaignID] = @campaignID 
		And i.rollerID = IsNull(@objectID, i.rollerID)
		And i.positionId = IsNull(@positionID, i.positionId)
	order by
		tw.dayOriginal

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		i.issueID,
		convert(varchar, tw.dayOriginal, 104),
		'Issue.png',
		dbo.fn_GetTimeString(pl.broadcastStart, tw.windowDateOriginal) + ' ' + r.name +
		Case i.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		tw.dayOriginal
	from 
		Issue i 
		Inner Join Roller r On i.rollerID = r.rollerID
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
		inner join Tariff t on tw.tariffId = t.tariffID
		inner join Pricelist pl on t.pricelistID = pl.pricelistID
		left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
		left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
		left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
	where 
		i.[campaignID] = @campaignID
		And i.rollerID = IsNull(@objectID, i.rollerID)
		And i.positionId = IsNull(@positionID, i.positionId)
	order 
		by dbo.fn_GetTimeString(pl.broadcastStart, tw.windowDateOriginal)
end 
else if @campaignTypeID = 3
	Begin
	insert into @days(id, issueDate,[image], [name])
	select distinct	
		convert(varchar, mi.issueDate, 104),
		mi.issueDate,
		'Day.png',
		convert(varchar, mi.issueDate, 104)
	from 
		[ModuleIssue] mi 
	where 
		mi.[campaignID] = @campaignID 
		And mi.moduleID = IsNull(@objectID, mi.moduleID)
		And mi.positionId = IsNull(@positionID, mi.positionId)
	order by
		mi.issueDate

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		mi.moduleIssueID,
		convert(varchar, mi.issueDate, 104),
		'Module.png',
		m.name + ' - ' + r.name +
		Case mi.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		mi.issueDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join Roller r on r.rollerId = mi.rollerId
	where 
		mi.[campaignID] = @campaignID
		And mi.moduleID = IsNull(@objectID, mi.moduleID)
		And mi.positionId = IsNull(@positionID, mi.positionId)
	End
else if @campaignTypeID = 4
	Begin
	insert into @days(id, issueDate,[image], [name])
	select distinct	
		convert(varchar, pmi.issueDate, 104),
		pmi.issueDate,
		'Day.png',
		convert(varchar, pmi.issueDate, 104)
	from 
		[PackModuleIssue] pmi 
		INNER JOIN [PackModulePriceList] pl ON pmi.[pricelistID] = pl.[priceListID]
	where 
		pmi.[campaignID] = @campaignID 
		And pl.packModuleID = IsNull(@objectID, pl.packModuleID)
		And pmi.positionId = IsNull(@positionID, pmi.positionId)
	order by
		pmi.issueDate

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		pmi.[packModuleIssueID],
		convert(varchar, pmi.issueDate, 104),
		'PackModule.png',
		pm.name + ' - ' + r.name+
		Case pmi.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		pmi.issueDate
	FROM 
		[PackModuleIssue] pmi
		INNER JOIN [PackModulePriceList] pl ON pmi.[pricelistID] = pl.[priceListID]
		INNER JOIN [PackModule] pm ON pl.[packModuleID] = pm.[packModuleID]
		INNER JOIN [Roller] r ON pmi.[rollerID] = r.[rollerID]
	where 
		pmi.[campaignID] = @campaignID
		And pl.packModuleID = IsNull(@objectID, pl.packModuleID)
		And pmi.positionId = IsNull(@positionID, pmi.positionId)
	End
else if @campaignTypeID = 100 -- такого типа нет, это для выпусков программ спонсорской кампании
	Begin
	insert into @days(id, issueDate,[image], [name])
	select distinct	
		convert(varchar, mi.issueDate, 104),
		mi.issueDate,
		'Day.png',
		convert(varchar, mi.issueDate, 104)
	from 
		[ProgramIssue] mi 
	where 
		mi.[campaignID] = @campaignID 
		And mi.programID = IsNull(@objectID, mi.programID)
	order by
		2

	insert into @days(id, parentID, [image], [name], issueDate)
	SELECT 
		pi.[issueID],
		Convert(varchar, pi.issueDate, 104),
		'SponsorProgram.png',
		sp.name + COALESCE(' - ' + adv.name, ''),
		pi.[issueDate]
	FROM 
		[ProgramIssue] pi
		INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
		LEFT JOIN AdvertType adv On adv.advertTypeID = pi.advertTypeID
	where 
		pi.[campaignID] = @campaignID
		And pi.programID = IsNull(@objectID, pi.programID)
	End

select * from @days

select positionId as Id, description as name from iIssuePosition Where positionId In(-20, -10, 0, 10)
