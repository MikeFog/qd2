/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE            PROCEDURE [dbo].[rpt_Grid]
(
@theDate  datetime,
@massMediaID smallint,
@userID smallint = null,
@loggedUserID smallint 
)
AS
SET NOCOUNT ON
SET DATEFIRST 1

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

SET @theDate = dbo.ToShortDate(@theDate)

-- select all rollers issues -------------------------------------
-- find a current pricelist for passed Date
Declare	@pricelistID smallint, @broadcastStart smalldatetime
Select @pricelistID = dbo.fn_GetPricelistIDByDate(@massMediaID, @theDate, 1)
SELECT @broadcastStart = broadcastStart FROM Pricelist WHERE pricelistID = @pricelistID

Declare @grid1 Table (
	[tariffID] int,
	[time] datetime,
	[tariffTime] varchar(5),
	[cellRealDuration] int,
	[cellRealTime] varchar(5),
	[suffix] NVARCHAR(16),
	[needExt] BIT,
	[needInJingle] BIT,
	[needOutJingle] BIT,
	[comment] NVARCHAR(128),
	[tariffUnionID] int,
	windowId int,
	[windowNextId] int,
	windowPrevId int, 
	[notEarly] varchar(1),
	[notLater] varchar(1),
	[openBlock] varchar(1),
	[openPhonogram] varchar(1),
	blockType varchar(1), 
	durationTotal smallint
)

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID) as x
where x.massmediaID = @massMediaID

Insert Into @grid1
select
	t.tariffID,
	tw.dayActual,
	dbo.[fn_GetTimeString](@broadcastStart, tw.[windowDateActual]),
	tw.[duration],
	'',
	coalesce(t.suffix, ''),
	coalesce(t.needExt, 1),
	coalesce(t.needInJingle, 1),
	coalesce(t.needOutJingle, 1),
	coalesce(t.[comment], ''),
	tu.tariffUnionID,
	tw.windowId,
	tw.windowNextId,
	tw.windowPrevId,
	Case When t.[notEarly]=1 Then 'W' Else '' End,
	Case When t.[notLater]=1 Then 'A' Else '' End,
	Case When t.[openBlock]=1 Then 'K' Else '' End,
	Case When t.[openPhonogram]=1 Then 'H' Else '' End,
	IsNull(bt.code, ''),
	tw.duration_total
FROM
	[TariffWindow] tw
	inner join @massmedias mm on tw.massmediaID = mm.massmediaID
	left JOIN [Tariff] t ON tw.[tariffId] = t.[tariffID]
	left join TariffUnion tu on t.tariffID = tu.tariffID
	left join BlockType bt On bt.[blockTypeID] = t.[blockTypeID]
WHERE
	tw.dayActual = @theDate

--select * from @grid1 where windowNextId Is Not Null or windowPrevId Is Not Null
	
Declare @issue TABLE (
	issueId int primary key not null,
	issueDate datetime,
	rollerID int,
	positionId FLOAT,
	[tariffId] INT,
	moduleIssueID INT, 
	packModuleIssueID INT,
	windowId int
)

Insert Into @issue
select distinct
	i.issueId,
	tw.windowDateActual,
	i.rollerID,
	i.positionId,
	tw.[tariffId],
	i.[moduleIssueID],
	i.[packModuleIssueID],
	i.actualWindowID
From		
	Issue i
	inner join TariffWindow tw on i.actualWindowID = tw.windowID
	Inner Join Campaign c On i.campaignID = c.campaignID 
	Inner Join [Action] a On c.actionID = a.actionID
	inner join @massmedias mm on tw.massmediaID = mm.massmediaID
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
Where		
	tw.dayActual = @theDate and
	a.userID = IsNull(@userID, a.userID) and
	i.[isConfirmed] = 1 and
	((a.userID = @loggedUserID and mm.myMassmedia = 1) or (a.userID <> @loggedUserID and mm.foreignMassmedia = 1 and (@isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null))) ) 

Declare @grid2 Table (
	[tariffID] int,
	[time] datetime,
	[tariffTime] varchar(5),
	[cellRealDuration] int,
	[positionId] float,
	[description] nvarchar(256),
	[rollerDurationString] varchar(8),
	[rollerDuration] SMALLINT,
	[path] NVARCHAR(1024),
	[name] NVARCHAR(64),
	[fullDuration] INT,
	[suffix] NVARCHAR(16),
	[rolActionTypeID] TINYINT,
	[needExt] BIT,
	[needInJingle] BIT,
	[needOutJingle] BIT,
	[isAlive] BIT,
	[currentPath] NVARCHAR(255),
	[comment] NVARCHAR(128),
	[tariffUnionID] int,
	[position] nvarchar(8),
	broadcastStart smalldatetime,
	windowNextId int,
	windowPrevId int,
	advertTypeId int,
	[notEarly] varchar(1),
	[notLater] varchar(1),
	[openBlock] varchar(1),
	[openPhonogram] varchar(1),
	blockType varchar(1),
	durationTotal smallint
)

Insert Into @grid2
Select	
	g1.tariffID,
	g1.time,
	Case g1.cellRealTime
		When '' Then g1.tariffTime
		Else g1.cellRealTime
	End as tariffTime, 
	g1.cellRealDuration,
	i.positionId,
	case when ip.positionId = -10 then ip.shortDescription + ' ' + r.[name] else r.[name] + ' ' + ip.shortDescription end as [description],
	dbo.fn_Int2Time(r.duration) as rollerDurationString,
	r.duration as rollerDuration,
	r.[path],
	r.NAME,
	g1.cellRealDuration,
	g1.suffix,
	r.rolActionTypeID,
	g1.needExt,
	g1.needInJingle,
	g1.needOutJingle,
	0,
	dbo.fn_GetPathForPackModueleAndModule(pm.packModuleID, pmpl.pricelistID, m.moduleID, @massMediaID),
	g1.comment,
	g1.tariffUnionID,
	ip.shortDescription,
	@broadcastStart,
	g1.windowNextId,
	g1.windowPrevId,
	r.advertTypeID,
	g1.[notEarly],
	g1.[notLater],
	g1.[openBlock],
	g1.[openPhonogram],
	g1.blockType,
	g1.durationTotal
From		
	@grid1 g1
	left join @issue i on dbo.[fn_GetTimeString](@broadcastStart, i.issueDate) = g1.tariffTime and g1.windowId = i.windowId
	left join roller r on i.rollerID = r.rollerID
	left join iIssuePosition ip ON ip.positionId = i.positionId
	LEFT JOIN [ModuleIssue] mi ON i.moduleIssueID = mi.[moduleIssueID]
	LEFT JOIN [Module] m ON mi.[moduleID] = m.[moduleID]
	LEFT JOIN [PackModuleIssue] pmi ON pmi.[packModuleIssueID] = i.packModuleIssueID
	LEFT JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
	LEFT JOIN [PackModule] pm ON pmpl.[packModuleID] = pm.[packModuleID]
	--LEFT JOIN PackModuleContent pmc On pmpl.priceListID = pmc.pricelistID
	--LEFT JOIN Module m2 On pmc.moduleID = m2.moduleID and m2.massmediaID = @massMediaID

--select tariffID, tariffTime, windowNextId, windowPrevId from @grid2 where windowNextId Is Not Null or windowPrevId Is Not Null
	
IF (@userID IS NULL)
	-- Выходы которые не были проспонсированы (исправлено, почему то дублировали)
	update g2 
	set 
		g2.[time] = t.TIME,
		--g2.[tariffTime] = dbo.[fn_GetTimeString](@broadcastStart, t.[time]), закомментировал, так как была ошибка для окон, которые перенес трафик-менеджер
		g2.[cellRealDuration] = t.duration,
		g2.[positionId] = 0,
		g2.[description] = CASE 
				WHEN r_pm.name IS NOT NULL THEN r_pm.name
				ELSE r_m.name  
			 END,
		g2.[rollerDurationString] = CASE 
				WHEN r_pm.[duration] IS NOT NULL THEN dbo.fn_Int2Time(r_pm.[duration])
				ELSE dbo.fn_Int2Time(r_m.[duration])
			 END,
		g2.[rollerDuration] = CASE 
				WHEN r_pm.[duration] IS NOT NULL THEN r_pm.[duration]
				ELSE r_m.[duration]  
			 END,
		g2.[path] = CASE 
				WHEN r_pm.[path] IS NOT NULL THEN r_pm.[path]
				ELSE r_m.[path]  
			 END,
		g2.[name] = CASE 
				WHEN r_pm.name IS NOT null  and len(r_pm.[path]) > 0 THEN r_pm.name
				ELSE r_m.name  
			 END,
		g2.[fullDuration] = tw.duration,
		g2.[suffix] = t.[suffix],
		g2.[rolActionTypeID] = CASE 
				WHEN r_pm.[rolActionTypeID] IS NOT NULL THEN r_pm.[rolActionTypeID]
				ELSE r_m.[rolActionTypeID]  
			 END,
		g2.[needExt] = t.[needExt],
		g2.[needInJingle] = t.[needInJingle],
		g2.[needOutJingle] = t.[needOutJingle],
		g2.[isAlive] = 0,
		g2.[currentPath] = CASE 
				WHEN m.[path] IS NOT NULL THEN m.[path] 
				ELSE pm.[path] 
			 END,
		g2.[comment] = m.name/*
				 CASE 
					WHEN pm.name IS NOT NULL THEN pm.name
					ELSE m.name  
				 end*/,
		g2.[tariffUnionID] = tu.tariffUnionID,
		g2.[position] =  '',
		g2.broadcastStart = @broadcastStart
	from @grid2 g2 
		inner join [TariffWindow] tw on tw.tariffId = g2.tariffID
		INNER JOIN [Tariff] t ON tw.tariffID = t.tariffID
		left join TariffUnion tu on t.tariffID = tu.tariffID
		LEFT JOIN [ModuleTariff] mt ON t.[tariffID] = mt.[tariffID]
		LEFT JOIN [ModulePriceList] mpl ON mpl.[modulePriceListID] = mt.[modulePriceListID] and @theDate between mpl.startDate and mpl.finishDate
		LEFT JOIN [Module] m ON mpl.[moduleID] = m.[moduleID]
		LEFT JOIN [Roller] r_m ON mpl.[rollerID] = r_m.[rollerID]
		LEFT JOIN [PackModuleContent] pmc ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
		LEFT JOIN [PackModulePriceList] pmpl ON pmc.[pricelistID] = pmpl.[priceListID] and @theDate between pmpl.startDate and pmpl.finishDate
		LEFT JOIN [PackModule] pm ON pmpl.[packModuleID] = pm.[packModuleID]
		LEFT JOIN [Roller] r_pm ON pmpl.[rollerID] = r_pm.[rollerID]
		left join Issue i on i.[actualWindowID] = tw.[windowId]
	WHERE 
		mpl.isStandAlone is not null and mpl.isStandAlone = 1
		AND tw.dayActual = @theDate
		AND tw.massmediaID = @massMediaID 
		AND tw.[maxCapacity] > 0
		AND (r_pm.[rolActionTypeID] IS NOT NULL OR r_m.[rolActionTypeID] IS NOT NULL)
		and (i.issueID is null or i.isConfirmed = 0)

-- пустые окна
INSERT INTO @grid2 
SELECT 
	g1.tariffID,
	g1.time,
	Case g1.cellRealTime
		When '' Then g1.tariffTime
		Else g1.cellRealTime
	End as tariffTime, 
	g1.cellRealDuration,
	0,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	g1.cellRealDuration,
	g1.suffix,
	NULL,
	g1.needExt,
	g1.needInJingle,
	g1.needOutJingle,
	0,
	'',
	g1.comment,
	g1.tariffUnionID,
	'',
	@broadcastStart,
	g1.windowNextId,
	g1.windowPrevId,
	null,
	g1.[notEarly],
	g1.[notLater],
	g1.[openBlock],
	g1.[openPhonogram],
	g1.blockType,
	g1.durationTotal
From		
	@grid1 g1
	inner join @grid2 g2 on g2.[time] = g1.[time]
WHERE g2.[time] is null

-- now select all program issues --------------------------
Insert	Into @grid2
Select 	
	null,
	t.[time],
	dbo.[fn_GetTimeString](pl.broadcastStart, t.[time]),
	0,
	0,
	sp.[name] + ' [' + f.[name] + ']',
	dbo.fn_Int2Time(t.[duration]),
	t.[duration],
	'',
	coalesce(t.comment, sp.[name]),
	t.[duration],
	t.[suffix],
	3,
	t.[needExt],
	t.[needInJingle],
	t.[needOutJingle],
	t.isAlive,
	t.[path],
	t.[comment],
	null,
	'',
	pl.broadcastStart,
	null,
	null,
	null,
	null,
	null,
	null,
	null,
	null,
	0
from	
	programIssue i
	inner join [SponsorProgram] sp on i.[programID] =  sp.[sponsorProgramID] and sp.[massmediaID] = @massMediaID
	Inner Join SponsorTariff t On t.tariffID = i.tariffID
	inner join SponsorProgramPricelist pl on t.priceListID = pl.pricelistID and @theDate between pl.[startDate] and pl.[finishDate]
	Inner Join Campaign c ON i.campaignID = c.campaignID
	Inner Join [Action] a ON a.actionID = c.actionID
	inner join @massmedias mm on c.massmediaID = mm.massmediaID
	Inner Join Firm f ON f.firmID = a.firmID
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
Where		
	((a.userID = @loggedUserID and mm.myMassmedia = 1) or (a.userID <> @loggedUserID and mm.foreignMassmedia = 1 and (@isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null))) ) and
	i.issueDate between DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate)) and DATEADD(ss, -1, DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate + 1))) and
	Convert(Varchar(5), i.issueDate, 108) =	Convert(Varchar(5), t.time, 108) and
	i.[isConfirmed] = 1 and 
	a.userID = IsNull(@userID, a.userID)

IF @userID IS NULL
	-- Программы которые не были проспонсированы
	Insert	Into @grid2
	SELECT 
		null,
		st.[time],
		dbo.[fn_GetTimeString](sppl.broadcastStart, st.time),
		0,
		0,
		sp.[name],
		dbo.fn_Int2Time(st.[duration]),
		st.[duration],
		'',
		coalesce(st.comment, sp.[name]),
		st.[duration],
		st.[suffix],
		3,
		st.[needExt],
		st.[needInJingle],
		st.[needOutJingle],
		st.isAlive,
		st.[path],
		st.comment,
		null,
		'',
		sppl.broadcastStart,
		null,
		null,
		null,
		null,
		null,
		null,
		null,
		null,
		0
	FROM [SponsorProgram] sp 
		INNER JOIN [SponsorProgramPricelist] sppl ON sp.[sponsorProgramID] = sppl.[sponsorProgramID]
		INNER JOIN [SponsorTariff] st ON sppl.[pricelistID] = st.[pricelistID]
		inner join @massmedias mm on sp.massmediaID = mm.massmediaID
	WHERE sppl.isStandAlone = 1 AND @theDate BETWEEN sppl.[startDate] AND sppl.[finishDate] AND
		((st.[time] >= sppl.broadcastStart 
			and ((datepart(dw, @theDate) = 1 and st.monday = 1 )
				or (datepart(dw, @theDate) = 2 and st.thursday = 1 )
				or (datepart(dw, @theDate) = 3 and st.wednesday = 1 )
				or (datepart(dw, @theDate) = 4 and st.thursday = 1 )
				or (datepart(dw, @theDate) = 5 and st.friday = 1 )
				or (datepart(dw, @theDate) = 6 and st.saturday = 1 )
				or (datepart(dw, @theDate) = 7 and st.sunday = 1 )))
		or  (st.[time] < sppl.broadcastStart 
			and ((datepart(dw, @theDate) = 7 and st.monday = 1 )
				or (datepart(dw, @theDate) = 1 and st.thursday = 1 )
				or (datepart(dw, @theDate) = 2 and st.wednesday = 1 )
				or (datepart(dw, @theDate) = 3 and st.thursday = 1 )
				or (datepart(dw, @theDate) = 4 and st.friday = 1 )
				or (datepart(dw, @theDate) = 5 and st.saturday = 1 )
				or (datepart(dw, @theDate) = 6 and st.sunday = 1 ))))
		and NOT EXISTS (SELECT * 
					FROM [ProgramIssue] i Inner Join SponsorTariff t On t.tariffID = i.tariffID
							inner join SponsorProgramPricelist pl on t.priceListID = pl.pricelistID 
					WHERE i.[isConfirmed] = 1 AND 
						i.[issueDate] between DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate)) and DATEADD(ss, -1, DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate + 1))) and
						Convert(Varchar(5), i.issueDate, 108) = Convert(Varchar(5), st.time, 108))
	
SELECT * 
FROM (
select 	
	tariffID,
	tariffTime, 
	[Description], 
	rollerDurationString,
	rollerDuration, 
	cellRealDuration,
	PATH,
	NAME,
	dbo.fn_Int2Time([fullDuration]) AS [fullDuration],
	suffix,
	[rolActionTypeID],
	[needExt],
	[needInJingle],
	[needOutJingle],
	[isAlive],
	[currentPath],
	TIME,
	positionId,
	[comment],
	CASE WHEN CAST(CAST(tariffTime AS NVARCHAR(2)) AS INT) < 24 THEN 1 ELSE 0 END AS isToday,
	tariffUnionID,
	position,
	case when [rolActionTypeID] <> 1 then 0 else rollerDuration end as rollerDurationSum,
	broadcastStart,
	windowNextId,
	windowPrevId,
	advertTypeId,
	[notEarly],
	[notLater],
	[openBlock],
	[openPhonogram],
	blockType,
	durationTotal
from 	
	@grid2
where	
	([rolActionTypeID] = 1 or [rolActionTypeID] is null)
UNION ALL
select distinct
	tariffID,
	tariffTime, 
	[Description], 
	rollerDurationString,
	rollerDuration, 
	cellRealDuration,
	[PATH],
	[NAME],
	dbo.fn_Int2Time([fullDuration]) AS [fullDuration],
	suffix,
	[rolActionTypeID],
	[needExt],
	[needInJingle],
	[needOutJingle],
	[isAlive],
	[currentPath],
	[TIME],
	positionId,
	[comment],
	CASE WHEN CAST(CAST(tariffTime AS NVARCHAR(2)) AS INT) < 24 THEN 1 ELSE 0 END AS isToday,
	tariffUnionID,
	position,
	case when [rolActionTypeID] <> 1 then 0 else rollerDuration end as rollerDurationSum,
	broadcastStart,
	windowNextId,
	windowPrevId,
	advertTypeId,
	[notEarly],
	[notLater],
	[openBlock],
	[openPhonogram],
	blockType,
	durationTotal
from 	
	@grid2
where	
	([rolActionTypeID] = 2 OR [rolActionTypeID] >= 3)) X
order by 	
		CASE
			WHEN [Time] < broadcastStart THEN '1'
			ELSE '0'
		END + [tariffTime],
	positionId,
	windowPrevId

