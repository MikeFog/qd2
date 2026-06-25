
CREATE PROC [dbo].[ActionActivate]
(
@actionID int,
@loggedUserID smallint,
@isTestActivate bit,
@tryTransferFailedIssues bit = 0,
@allowDifferentWindowPrice bit = 0,
@transferAttemptCount int = 0
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON	

DECLARE @isAdmin bit, @rightForMinus bit, @rightToGoBack bit, @IsTrafficManager bit
DECLARE @blockActivation bit = 0;
	
EXEC hlp_GetMainUserCredentials
	@loggedUserId = @loggedUserID, @rightToGoBack = @rightToGoBack out, @isAdmin = @isAdmin out, @rightForMinus = @rightForMinus out,  @IsTrafficManager = @IsTrafficManager out

declare @issue table (
	name nvarchar(64) not null,
	advertTypeName nvarchar(256) null,
	statusDescription nvarchar(64) not null,
	duration int not null,
	positionID int not null,
	issueDate datetime not null,
	issueID int primary key,
	rollerID int not null,
	moduleIssueID int,
	packModuleIssueID int,
	campaignID int not null,
	campaignTypeID tinyint not null,
	massmediaID int not null, 
	windowId int not null,
	oldWindowId int not null,
	oldIssueDate datetime not null,
	oldWindowPrice decimal(18, 2) not null,
	grantorID smallint null,
	isTransferred bit not null default 0,
	transferStatus nvarchar(64) null
)

declare @fatalErrors table (name nvarchar(64) not null)
declare @plannedTransfers table (
	issueID int primary key,
	oldWindowID int not null,
	newWindowID int not null,
	oldIssueDate datetime not null,
	newIssueDate datetime not null
)

declare @tomorrow datetime
select @tomorrow = dateadd(day, 1, CONVERT(date, getdate()))

DECLARE cur_issues CURSOR FOR
Select i.issueId From Issue i inner join Campaign c on c.campaignID = i.campaignID  where c.actionID = @actionID and i.isConfirmed = 0
declare @issueId int

OPEN cur_issues
FETCH NEXT FROM cur_issues INTO @issueId

WHILE @@FETCH_STATUS = 0
BEGIN
	insert into @issue([name], advertTypeName, statusDescription,duration,positionID,issueDate,issueID,rollerID,moduleIssueID,packModuleIssueID,campaignID,campaignTypeID,massmediaID, windowId, oldWindowId, oldIssueDate, oldWindowPrice, grantorID)
	select 
		r.[name],
		r.advertTypeName,
		case 
			when c.finishDate < @tomorrow and c.campaignTypeID <> 2 and @rightToGoBack <> 1 then 'CampaignAlreadyFinished' 
			when (tw.isDisabled = 1) then 'DisabledInsertRoller' 
			when (r.rolActionTypeID = 1 and tw.maxCapacity > 0) then 'DisabledInsertSimpleRoller' 
			when ((tw.isFirstPositionOccupied = 1 And i.positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And i.positionId = -10)
					or (tw.isLastPositionOccupied = 1	And i.positionId = 10)) then 'FirstLastIssueError' 
			when (tw.dayActual < @tomorrow) and @rightToGoBack <> 1 And @IsTrafficManager = 0 then 'IncorrectIssueDate' 
			when (@rightForMinus = 0 
				and (tw.[timeInUseConfirmed] + r.duration + (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId) > tw.duration) 
				and (i.grantorID is null or (dbo.fn_IsRightForMinus(i.grantorID) = 0)))
				then 'WindowOverflow' 
			when (@rightForMinus = 0 and (tw.[maxCapacity] > 0 
				AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1 + (Select count(*) From @issue it2 Where it2.windowId = tw.windowId))) < 0) 
				and (i.grantorID is null or (dbo.fn_IsRightForMinus(i.grantorID) = 0))) 
				then 'WindowMaxCapacityOverflow' 
			when r.advertTypeID Is Null then 'RollerWithoutActionType' 
			when tw.dayOriginal <= mm.deadLine And @isAdmin = 0 And @IsTrafficManager = 0  then 'DeadLineViolation'
			else 'OK'
		end,
		r.duration, i.positionId, tw.windowDateActual, i.issueID, i.rollerID, i.moduleIssueID, i.packModuleIssueID, i.campaignID, c.campaignTypeID, mm.massmediaID, tw.windowId, tw.windowId, tw.windowDateActual, tw.price, i.grantorID
	from Issue i 
		inner join vRoller r on i.rollerID = r.rollerID
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Campaign c on c.campaignID = i.campaignID 
		inner join MassMedia mm on mm.massmediaID = tw.massmediaID
	where i.issueID = @issueId

	FETCH NEXT FROM cur_issues INTO @issueId
END

CLOSE cur_issues
DEALLOCATE cur_issues

declare @programmissue table (
	[name] nvarchar(64) not null,
	statusDescription nvarchar(64) not null,
	duration int not null,
	issueID int primary key,
	campaignID int not null,
	issueDate datetime not null,
	advertTypeName varchar(256)
)

insert into @programmissue (
	[name],
	statusDescription,
	duration,
	issueDate,
	issueID,
	campaignID,
	advertTypeName
) 
select 
	COALESCE(NULLIF(LTRIM(RTRIM(st.comment)), ''), sp.[name]),
case 
    when i.advertTypeID Is Null then 'RollerWithoutActionType'
    when (i.issueDate < @tomorrow) and @rightToGoBack <> 1 and @IsTrafficManager = 0 then 'IncorrectIssueDate'
    when i2.issueID is null then 'OK' 
    else 'AlreadySponsered' 
end,
	st.duration,
	i.issueDate,
	i.issueID,
	i.campaignID,
	adv.name
from ProgramIssue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join SponsorProgram sp on i.programID = sp.sponsorProgramID
	inner join SponsorTariff st on i.tariffID = st.tariffID
	left join AdvertType adv on adv.advertTypeID = i.advertTypeID
	left join ProgramIssue i2 on i.issueID <> i2.issueID and i.programID = i2.programID 
		and i.tariffID = i2.tariffID and i.issueDate = i2.issueDate and i2.isConfirmed = 1 
where c.actionID = @actionID and i.isConfirmed = 0

update i2 set i2.statusDescription = 'PartOfModule' 
from @issue i INNER JOIN @issue i2 ON i.moduleIssueID = i2.moduleIssueID
	where i.statusDescription <> 'OK' and i2.statusDescription like 'OK' 

update i2 set i2.statusDescription = 'PartOfPackModule' 
from @issue i INNER JOIN @issue i2 ON i.packModuleIssueID = i2.packModuleIssueID
	where i.statusDescription <> 'OK' and i2.statusDescription like 'OK' 

if exists(
		select * from 
		(
			select c.campaignID, SUM(dbo.f_GetSponsorDuration(r.[duration], i.positionId, pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller)) as sumDuration 
			from [Issue] i 
				inner join @issue i2 on i.issueID = i2.issueID and i2.statusDescription like 'OK'
				inner join Campaign c on i.campaignID = c.campaignID
				inner join TariffWindow tw on i.originalWindowID = tw.windowId
				Inner Join Tariff t on tw.tariffId = t.tariffID
				Inner Join Pricelist pl On pl.pricelistID = t.pricelistID
				inner join Roller r on i.rollerID = r.rollerID
				inner join MassMedia mm on tw.massmediaID = mm.massmediaID
			where 
				c.actionID = @actionID and c.campaignTypeID = 2
			group by 
				c.campaignID
		) as x
		left join (
			select c.campaignID, sum(pl.bonus) as sumBonus
			from [ProgramIssue] i 
				inner join @programmissue i2 on i.issueID = i2.issueID and i2.statusDescription like 'OK'
				inner join Campaign c on c.campaignID = i.campaignID
				inner join SponsorTariff st on i.tariffID = st.tariffID
				inner join [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
			where c.actionID = @actionID and c.campaignTypeID = 2
			group by c.campaignID
		) as y on x.campaignID = y.campaignID 
		where y.sumBonus is null or y.sumBonus < x.sumDuration 
	)
begin 
    UPDATE it
      SET it.statusDescription = 'TimeBonusExceed'
    FROM @issue it
    INNER JOIN Campaign c ON c.campaignID = it.campaignID
    WHERE c.actionID = @actionID
      AND c.campaignTypeID = 2
      AND it.statusDescription = 'OK';

    SET @blockActivation = 1;
	Insert Into @fatalErrors(name) values('CannotPerfomActivationSponsor')
end 

IF @isTestActivate = 0
	AND @blockActivation = 0
	AND @tryTransferFailedIssues = 1
	AND @transferAttemptCount > 0
BEGIN
	DECLARE
		@transferIssueID int,
		@transferSourceWindowID int,
		@transferSourceDate datetime,
		@transferSourcePrice decimal(18, 2),
		@transferPositionID int,
		@transferDuration int,
		@transferGrantorID smallint,
		@candidateWindowID int,
		@candidateIssueDate datetime

	DECLARE cur_transfer CURSOR LOCAL FOR
		SELECT issueID, oldWindowId, oldIssueDate, oldWindowPrice, positionID, duration, grantorID
		FROM @issue
		WHERE statusDescription <> 'OK'
			AND campaignTypeID = 1
			AND moduleIssueID IS NULL
			AND packModuleIssueID IS NULL

	OPEN cur_transfer
	FETCH NEXT FROM cur_transfer INTO @transferIssueID, @transferSourceWindowID, @transferSourceDate,
		@transferSourcePrice, @transferPositionID, @transferDuration, @transferGrantorID

	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @candidateWindowID = NULL
		SET @candidateIssueDate = NULL

		;WITH rankedCandidates AS
		(
			SELECT
				tw.windowId,
				tw.windowDateActual,
				attemptNo = ROW_NUMBER() OVER (ORDER BY tw.windowDateActual DESC)
			FROM TariffWindow tw
			WHERE tw.windowDateActual < @transferSourceDate
				AND tw.massmediaID = (SELECT massmediaID FROM @issue WHERE issueID = @transferIssueID)
				AND tw.dayActual = (SELECT dayActual FROM TariffWindow WHERE windowId = @transferSourceWindowID)
				AND tw.windowId <> @transferSourceWindowID
				AND (@allowDifferentWindowPrice = 1 OR tw.price = @transferSourcePrice)

			UNION ALL

			SELECT
				tw.windowId,
				tw.windowDateActual,
				attemptNo = ROW_NUMBER() OVER (ORDER BY tw.windowDateActual ASC)
			FROM TariffWindow tw
			WHERE tw.windowDateActual > @transferSourceDate
				AND tw.massmediaID = (SELECT massmediaID FROM @issue WHERE issueID = @transferIssueID)
				AND tw.dayActual = (SELECT dayActual FROM TariffWindow WHERE windowId = @transferSourceWindowID)
				AND tw.windowId <> @transferSourceWindowID
				AND (@allowDifferentWindowPrice = 1 OR tw.price = @transferSourcePrice)
		),
		validCandidates AS
		(
			SELECT
				rc.windowId,
				rc.windowDateActual,
				rc.attemptNo,
				freeTime = tw.duration - tw.timeInUseConfirmed
					- (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK')
			FROM rankedCandidates rc
				INNER JOIN TariffWindow tw ON tw.windowId = rc.windowId
				INNER JOIN Issue i ON i.issueID = @transferIssueID
				INNER JOIN Roller r ON r.rollerID = i.rollerID
				INNER JOIN Campaign c ON c.campaignID = i.campaignID
				INNER JOIN MassMedia mm ON mm.massmediaID = tw.massmediaID
			WHERE rc.attemptNo <= @transferAttemptCount
				AND NOT (c.finishDate < @tomorrow and c.campaignTypeID <> 2 and @rightToGoBack <> 1)
				AND tw.isDisabled = 0
				AND NOT (r.rolActionTypeID = 1 and tw.maxCapacity > 0)
				AND NOT ((tw.isFirstPositionOccupied = 1 And @transferPositionID = -20)
					or (tw.isSecondPositionOccupied = 1 And @transferPositionID = -10)
					or (tw.isLastPositionOccupied = 1 And @transferPositionID = 10))
				AND NOT EXISTS (
					SELECT 1
					FROM @issue it2
					WHERE it2.windowId = tw.windowId
						AND it2.statusDescription = 'OK'
						AND it2.positionID = @transferPositionID
						AND @transferPositionID IN (-20, -10, 10)
				)
				AND NOT ((tw.dayActual < @tomorrow) and @rightToGoBack <> 1 And @IsTrafficManager = 0)
				AND NOT (@rightForMinus = 0
					and (tw.[timeInUseConfirmed] + @transferDuration
						+ (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK') > tw.duration)
					and (@transferGrantorID is null or (dbo.fn_IsRightForMinus(@transferGrantorID) = 0)))
				AND NOT (@rightForMinus = 0
					and (tw.[maxCapacity] > 0
						AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1
							+ (Select count(*) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK'))) < 0)
					and (@transferGrantorID is null or (dbo.fn_IsRightForMinus(@transferGrantorID) = 0)))
				AND r.advertTypeID Is Not Null
				AND NOT (tw.dayOriginal <= mm.deadLine And @isAdmin = 0 And @IsTrafficManager = 0)
		)
		SELECT TOP 1
			@candidateWindowID = windowId,
			@candidateIssueDate = windowDateActual
		FROM validCandidates
		ORDER BY attemptNo, freeTime DESC, windowDateActual

		IF @candidateWindowID IS NOT NULL
		BEGIN
			UPDATE @issue
			SET
				statusDescription = 'OK',
				windowId = @candidateWindowID,
				issueDate = @candidateIssueDate,
				isTransferred = 1,
				transferStatus = 'Transferred'
			WHERE issueID = @transferIssueID

			INSERT INTO @plannedTransfers(issueID, oldWindowID, newWindowID, oldIssueDate, newIssueDate)
			VALUES(@transferIssueID, @transferSourceWindowID, @candidateWindowID, @transferSourceDate, @candidateIssueDate)
		END

		FETCH NEXT FROM cur_transfer INTO @transferIssueID, @transferSourceWindowID, @transferSourceDate,
			@transferSourcePrice, @transferPositionID, @transferDuration, @transferGrantorID
	END

	CLOSE cur_transfer
	DEALLOCATE cur_transfer
END

SELECT 
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription, 
	i2.[name], 
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
FROM
	@ISSUE i2
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
WHERE i2.statusDescription like 'OK'
	and i2.isTransferred = 0
union all
select 
	'pi_' + cast(i.issueID as  varchar) as issueID,
	am.message as statusDescription, 
	i.[name], 
	i.advertTypeName,
	dbo.fn_Int2Time(i.duration) as duration,
	null as issuePosition,
	i.issueDate,
	m.name as radiostationName,
	m.groupName
from @programmissue i
	Inner Join Campaign c on c.campaignID = i.campaignID
	Inner Join vMassmedia m On m.massmediaID = c.massmediaID
	LEFT JOIN iMessageToActivate am ON i.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i.statusDescription like 'OK'
ORDER BY 
	issueDate 

select 
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription,	
	i2.[name],
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
from
	@ISSUE i2
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i2.statusDescription <> 'OK'
union all
select 
	'pi_' + cast(i.issueID as  varchar),
	am.message as statusDescription, 
	i.[name], 
	i.advertTypeName,
	dbo.fn_Int2Time(i.duration) as duration,
	null as issuePosition,
	i.issueDate as issueDate,
	m.name as radiostationName,
	m.groupName
from 
	@programmissue i
	Inner Join Campaign c on c.campaignID = i.campaignID
	Inner Join vMassmedia m On m.massmediaID = c.massmediaID
	LEFT JOIN iMessageToActivate am ON i.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i.statusDescription <> 'OK'
order by  
	issueDate 

Select am.message as errorMessage from @fatalErrors fe LEFT JOIN iMessageToActivate am ON fe.name COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT

SELECT
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription,
	i2.[name],
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.oldIssueDate,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
FROM
	@ISSUE i2
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
WHERE i2.statusDescription like 'OK'
	and i2.isTransferred = 1
ORDER BY
	i2.issueDate

IF @isTestActivate = 0 AND @blockActivation = 0
begin
--	INSERT INTO [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
--	select @loggedUserID, @actionID, i.rollerID, it.issueDate, tw.massmediaID 
--	from @issue it inner join Issue i on it.issueID = i.issueID inner join TariffWindow tw on i.originalWindowID = tw.windowId where it.statusDescription <> 'OK'
/*
	delete from i from @issue it inner join Issue i on it.issueID = i.issueID where it.statusDescription <> 'OK'
	delete from mi from @issue it inner join ModuleIssue mi on it.moduleIssueID = mi.moduleIssueID where it.statusDescription <> 'OK'
	delete from pmi from @issue it inner join PackModuleIssue pmi on it.packModuleIssueID = pmi.packModuleIssueID where it.statusDescription <> 'OK'
	delete from i from @programmissue it inner join ProgramIssue i on it.issueID = i.issueID where it.statusDescription <> 'OK'
*/

	Update
		TariffWindow
	Set
		timeInUseUnconfirmed =
			Case
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseUnconfirmed =
			Case
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - 1
				Else capacityInUseUnconfirmed
			End,
		firstPositionsUnconfirmed =
			Case
				When i.positionId = -20 Then firstPositionsUnconfirmed - 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed =
			Case
				When i.positionId = -10 Then secondPositionsUnconfirmed - 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed =
			Case
				When i.positionId = 10 Then lastPositionsUnconfirmed - 1
				Else lastPositionsUnconfirmed
			End
	From
		@plannedTransfers pt
		Inner Join Issue i On i.issueID = pt.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
	Where
		TariffWindow.windowId = pt.oldWindowID

	Update i
	Set
		actualWindowID = pt.newWindowID,
		tariffPrice = dbo.fn_GetIssuePrice(
			r.duration,
			twNew.price,
			i.ratio,
			i.positionId,
			IsNull(pl.extraChargeFirstRoller, 0),
			IsNull(pl.extraChargeSecondRoller, 0),
			IsNull(pl.extraChargeLastRoller, 0))
	From
		Issue i
		Inner Join @plannedTransfers pt On pt.issueID = i.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
		Inner Join TariffWindow twNew On twNew.windowId = pt.newWindowID
		Left Join Tariff t On t.tariffID = twNew.tariffId
		Left Join Pricelist pl On pl.pricelistID = t.pricelistID

	Update
		TariffWindow
	Set
		timeInUseUnconfirmed =
			Case
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed + r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseUnconfirmed =
			Case
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed + 1
				Else capacityInUseUnconfirmed
			End,
		firstPositionsUnconfirmed =
			Case
				When i.positionId = -20 Then firstPositionsUnconfirmed + 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed =
			Case
				When i.positionId = -10 Then secondPositionsUnconfirmed + 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed =
			Case
				When i.positionId = 10 Then lastPositionsUnconfirmed + 1
				Else lastPositionsUnconfirmed
			End
	From
		@plannedTransfers pt
		Inner Join Issue i On i.issueID = pt.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
	Where
		TariffWindow.windowId = pt.newWindowID

	INSERT INTO [TransferLog]([userID], [oldDate], [newDate], [actionID], [issueID])
	SELECT @loggedUserID, oldIssueDate, newIssueDate, @actionID, issueID
	FROM @plannedTransfers

-- 1. Удаление обычных выпусков (Issue)
	DECLARE @delIssueID int;
	DECLARE cur_del_issue CURSOR LOCAL FOR
		SELECT issueID FROM @issue WHERE statusDescription <> 'OK';
	
	OPEN cur_del_issue;
	FETCH NEXT FROM cur_del_issue INTO @delIssueID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[IssueIUD]
			@issueID = @delIssueID,
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_issue INTO @delIssueID;
	END
	CLOSE cur_del_issue;
	DEALLOCATE cur_del_issue;


	-- 2. Удаление ProgramIssue (через соответствующую процедуру)
	DECLARE @delProgIssueID int;
	DECLARE cur_del_prog CURSOR LOCAL FOR
		SELECT issueID FROM @programmissue WHERE statusDescription <> 'OK';
	
	OPEN cur_del_prog;
	FETCH NEXT FROM cur_del_prog INTO @delProgIssueID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[ProgramIssueIUD] -- Укажи правильное имя процедуры!
			@issueID = @delProgIssueID,
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_prog INTO @delProgIssueID;
	END
	CLOSE cur_del_prog;
	DEALLOCATE cur_del_prog;


	-- 3. Удаление ModuleIssue (уникальные ID, чтобы не дергать процу дважды для одного модуля)
	DECLARE @delModuleID int;
	DECLARE cur_del_mod CURSOR LOCAL FOR
		SELECT DISTINCT moduleIssueID FROM @issue WHERE statusDescription <> 'OK' AND moduleIssueID IS NOT NULL;
	
	OPEN cur_del_mod;
	FETCH NEXT FROM cur_del_mod INTO @delModuleID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[ModuleIssueIUD] -- Укажи правильное имя процедуры!
			@moduleIssueID = @delModuleID, -- или @issueID, в зависимости от того, как проца принимает параметр
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_mod INTO @delModuleID;
	END
	CLOSE cur_del_mod;
	DEALLOCATE cur_del_mod;


	-- 4. Удаление PackModuleIssue 
	DECLARE @delPackID int;
	DECLARE cur_del_pack CURSOR LOCAL FOR
		SELECT DISTINCT packModuleIssueID FROM @issue WHERE statusDescription <> 'OK' AND packModuleIssueID IS NOT NULL;
	
	OPEN cur_del_pack;
	FETCH NEXT FROM cur_del_pack INTO @delPackID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[PackModuleIssueID] -- Укажи правильное имя процедуры!
			@packModuleIssueID = @delPackID, -- параметр твоей процы
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_pack INTO @delPackID;
	END
	CLOSE cur_del_pack;
	DEALLOCATE cur_del_pack;

	update i set isConfirmed = 1, activationDate = getdate()
	from Issue i inner join @Issue it on i.issueID = it.issueID and it.statusDescription = 'OK' 

	update i set i.isConfirmed = 1 from ProgramIssue i inner join Campaign c on i.campaignID = c.campaignID inner join @programmissue ii on i.issueID = ii.issueID where c.actionID = @actionID 

	update mi set mi.isConfirmed = 1 from ModuleIssue mi 
		inner join @Issue i on mi.moduleIssueID = i.moduleIssueID and i.statusDescription = 'OK' 
		
	update pmi set pmi.isConfirmed = 1 from PackModuleIssue pmi 
		inner join @Issue i on pmi.packModuleIssueID = i.packModuleIssueID and i.statusDescription = 'OK'  
		
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then timeInUseConfirmed + t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed + t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssues
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 1
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 1
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 1
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCount,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCount,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCount
	from
		(select i.actualWindowID as windowID, 
			sum(r.duration) as duration, 
			count(i.issueID) as countIssues,
			sum(coalesce(case when i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.positionId = 10 then 1 else 0 end, 0)) as lastCount
		 from 
			@issue i0 
			inner join Issue i on i0.issueID = i.issueID
			Inner Join Roller r On r.rollerId = i.rollerId
		where i0.statusDescription = 'OK'
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
	/*	
	Update 
		TariffWindow
	Set
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssues
				Else capacityInUseUnconfirmed
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - t1.firstCount,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - t1.secondCount,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - t1.lastCount
	From
		(select i.actualWindowID as windowID, 
			sum(r.duration) as duration, 
			count(i.issueID) as countIssues,
			sum(coalesce(case when i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.positionId = 10 then 1 else 0 end, 0)) as lastCount
		 from 
			@issue i0 
			inner join Issue i on i0.issueID = i.issueID
			Inner Join Roller r On r.rollerId = i.rollerId
		where i0.statusDescription <> 'OK'
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
	*/	
	UPDATE [Action] Set isConfirmed = 1 WHERE actionID = @actionID
	
	exec ActionRecalculate
		@actionID = @actionID
END
