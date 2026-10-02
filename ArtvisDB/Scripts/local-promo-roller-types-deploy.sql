-- Локальное промо: два новых типа роликов. Развёртывание.
--
-- Типы (iRollerActionType):
--   8 - Локальное промо со спонсором: в выгрузке DJin сразу за джинглом влёта (In),
--       без влёта - сразу за промо без спонсора;
--   9 - Локальное промо без спонсора: в выгрузке DJin перед влётом (после ручного
--       идентификатора локального СМИ, тип 4, если он есть). Рекламой не считается:
--       блок, где кроме него (и идентификаторов СМИ) ничего нет, выгружается без In/Out.
-- Порядок в блоке собирает клиент (BlockManager); со стороны базы промо ведёт себя
-- как обычный рекламный ролик:
--   - hlp_IssueVerify, ActionActivate, IssueTransfer: запрет DisabledInsertSimpleRoller
--     (окна с ограничением по количеству) распространён на 8/9;
--   - stat_FillPercentage: 8/9 входят в процент заполнения;
--   - rpt_Grid_v3: 8/9 идут в ветку без DISTINCT - число промо в окне не ограничено,
--     один ролик может стоять дважды.
--
-- Скрипт идемпотентен. Совместим со старым клиентом (старый Merlin.exe выгрузит промо
-- как обычные ролики, без перестановки). Новые типы появятся в карточке ролика после
-- перезапуска qd2.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i local-promo-roller-types-deploy.sql

SET NOCOUNT ON;
GO

-------------------------------------------------------------------------------
-- 1. Типы роликов
-------------------------------------------------------------------------------
SET IDENTITY_INSERT [dbo].[iRollerActionType] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[iRollerActionType] WHERE rolActionTypeId = 8)
	INSERT INTO [dbo].[iRollerActionType] (rolActionTypeId, name)
	VALUES (8, N'Локальное промо со спонсором');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iRollerActionType] WHERE rolActionTypeId = 9)
	INSERT INTO [dbo].[iRollerActionType] (rolActionTypeId, name)
	VALUES (9, N'Локальное промо без спонсора');

SET IDENTITY_INSERT [dbo].[iRollerActionType] OFF;
GO

-------------------------------------------------------------------------------
-- 2. Процедуры (копии ArtvisDB/dbo/Stored Procedures)
-------------------------------------------------------------------------------
-- hlp_IssueVerify
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[hlp_IssueVerify]
(
@issueID int,
@actionName varchar(32),
@massmediaID smallint,
@DeadLine datetime,
@windowID int,
@issueDate datetime,
@rollerDuration int,
@rightToGoBack bit,
@isAdmin bit,
@isTrafficManager bit,
@rightForMinus bit,
@campaignFinishDate datetime,
@campaignTypeID tinyint,
@isConfirmed bit,
@positionId smallint,
@timeBonus int,
@issuesDuration int,
@sumCapacity INT, 
@rollerActionTypeID TINYINT,
@msgError varchar(64) out,
@modulePricelistID int = null,
@needByTypeVerify bit = 0,
@packModulePriceListId int = null
)
AS
SET NOCOUNT ON

declare @tomorrow datetime
select @msgError = null, @tomorrow = dateadd(day, 1, Convert(datetime, Convert(varchar(8),getdate(), 112), 112)), @issueDate = Convert(datetime, Convert(varchar(8),@issueDate, 112), 112)

IF @sumCapacity IS NULL 
 set @sumCapacity = 1
 
-- check Disabled Windows 
if ((@needByTypeVerify = 0 or @campaignTypeID in (1,2,3)) 
		and exists (select * 
						from DisabledWindow dw 
							inner join TariffWindow tw on tw.windowID = @windowID and tw.massmediaID = @MassMediaId
								and dw.massmediaID = @MassMediaId and tw.windowDateActual between dw.startDate And dw.finishDate)
	or (@needByTypeVerify = 1 and @campaignTypeID = 4 and exists(select * from [PackModuleContent]  pmc
																	INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
																	INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
																	INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID] and tw.dayActual = @issueDate
																	inner join DisabledWindow dw on tw.windowDateActual between dw.startDate And dw.finishDate and tw.massmediaID = dw.massmediaID
																	where pmc.pricelistID = @packModulePriceListId ) ) )
begin
	set @msgError = 'DisabledWindowInsert'
	return 1
end


--set @msgError = @rollerActionTypeID
--RETURN 1

-- В окне может быть только один открывающий (тип 4 или 44) и один закрывающий (тип 5 или 55)
-- идентификатор СМИ; 44/55 - авто-обрамление политической агитации, пары к ручным 4/5
If @rollerActionTypeID In (4, 44) And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.originalWindowID = @windowID
		And r.rolActionTypeID In (4, 44)
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType4AlreadyExistInWindow'
		RETURN 1
	End

If @rollerActionTypeID In (5, 55) And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.originalWindowID = @windowID
		And r.rolActionTypeID In (5, 55)
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType5AlreadyExistInWindow'
		RETURN 1
	End

-- Размещение агитации сразу подтверждённым выпуском (в уже активированной акции)
-- требует заполненных роликов обвязки в карточке станции - иначе обвязку не создать.
-- В черновике проверка не нужна: её выполнит активация (ActionActivate).
If @rollerActionTypeID = 6 And @isConfirmed = 1
	And Exists (
		Select 1 From MassMedia mm
		Where mm.massmediaID = @MassMediaId
			And (mm.agitationLocalRollerID Is Null
				Or mm.agitationAnnounceRollerID Is Null
				Or mm.agitationFederalRollerID Is Null)
	)
	Begin
		set @msgError = 'AgitationStationRollersNotSet'
		RETURN 1
	End

-- Анонс политической агитации (тип 7) в окне может быть только один
If @rollerActionTypeID = 7 And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.originalWindowID = @windowID
		And r.rolActionTypeID = 7
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType7AlreadyExistInWindow'
		RETURN 1
	End

-- Для политической агитации и её авто-обрамления спецпозиционирование не применяется
If @rollerActionTypeID In (6, 7, 44, 55) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'AgitationPositionForbidden'
		RETURN 1
	End

if	@campaignFinishDate < @tomorrow and @campaignTypeID <> 2 And @RightToGoBack <> 1 And @isTrafficManager = 0
	set @msgError = 'CampaignAlreadyFinished'
else if @campaignTypeID = 2 
	Begin
	Declare
		@extraChargeFirst tinyint,
		@extraChargeSecond tinyint,
		@extraChargeLast tinyint

	Select
		@extraChargeFirst = IsNull(extraChargeFirstRoller, 0),
		@extraChargeSecond = IsNull(extraChargeSecondRoller, 0),
		@extraChargeLast = IsNull(extraChargeLastRoller, 0)
	From 
		Pricelist p
		Inner Join Tariff t on p.pricelistID = t.pricelistID
		Inner Join TariffWindow tw on tw.tariffId = t.tariffID
	Where 
		tw.windowId = @windowID

	If @timeBonus < @issuesDuration + dbo.f_GetSponsorDuration(@rollerDuration, @positionId, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
		Begin
		set @msgError = 'TimeBonusExceed'
		return 1
		End
	End
if (@needByTypeVerify = 0 or @campaignTypeID in (1,2))
	select top 1 @msgError = 
		case
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And @deadLine is not null and tw.dayActual <= @deadLine And @isAdmin = 0 And @isTrafficManager = 0 then 'DeadLineViolation' 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And tw.dayOriginal < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
			when tw.isDisabled = 1 then 'DisabledInsertRoller'
			when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
			when (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
					or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
			when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
			when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
			else null 
		end 
	from [TariffWindow] tw
	where tw.[windowId] = @windowID 
else if @needByTypeVerify = 1 and @campaignTypeID = 3
	Begin
	select top 1 @msgError = 
		case 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And @deadLine is not null and tw.dayActual <= @deadLine And @isAdmin = 0 And @isTrafficManager = 0 then 'DeadLineViolation' 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And tw.dayOriginal < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
			when tw.isDisabled = 1 then 'DisabledInsertRoller'
			when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
			when (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
					or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
			when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
			when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
			else null 
		end 
	from [TariffWindow] tw
		Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
		Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID
	where 
		mt.modulePriceListID = @modulePricelistID
		And tw.dayActual = @issueDate
	order by 1 desc
	if @msgError Is Null And @positionId <> 0
	Begin
	Select @msgError = 'MaxCapacityModuleSetPositionError'
	From
		ModuleTariff mt
		Inner Join Tariff t On t.tariffID = mt.tariffID
	Where
		mt.modulePriceListID = @modulePriceListId
		And t.maxCapacity > 0 And t.maxCapacity < 4
	End
	End 
else if @needByTypeVerify = 1 and @campaignTypeID = 4
	Begin
	If (@actionName = 'AddItem' Or @isConfirmed = 1) And @isAdmin = 0 And @isTrafficManager = 0 And 
		Exists
		(
		Select 1 
		from 
			[PackModuleContent]  pmc
			INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on tw.massmediaID = mm.massmediaID
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayActual = @issueDate
			AND @issueDate <= mm.deadLine
		)
		Set @msgError = 'DeadLineViolation'

	If @msgError Is Null
		select top 1 @msgError = 
			case 
				when tw.isDisabled = 1 then 'DisabledInsertRoller'
				when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
				when @positionId <> 0 and (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
						or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
						or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
				when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
				when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
				when (@actionName = 'AddItem' Or @isConfirmed = 1) And @issueDate < @tomorrow And @RightToGoBack <> 1And @isTrafficManager = 0 then 'IncorrectIssueDate'
				else null 
			end 
		from [PackModuleContent]  pmc
			INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
			INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on tw.massmediaID = tw.massmediaID
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayActual = @issueDate
		order by 1 desc
	if @msgError Is Null And @positionId <> 0
		Begin
		Select @msgError = 'MaxCapacityPackModuleSetPositionError'
		From
			PackModuleContent pmc
			Inner Join ModuleTariff mt On mt.modulePriceListID = pmc.modulePriceListID
			Inner Join Tariff t On t.tariffID = mt.tariffID
		Where
			pmc.pricelistID = @packModulePriceListId
			And t.maxCapacity > 0 And t.maxCapacity < 4
		End
	End
if @msgError is not null 
	return 1

return 0
GO

-- ActionActivate
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[ActionActivate]
(
@actionID int,
@loggedUserID smallint,
@isTestActivate bit,
@tryTransferFailedIssues bit = 0,
@allowDifferentWindowPrice bit = 0,
@transferAttemptCount int = 0,
@avoidFirmRollerWindows bit = 1
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

declare @firmID smallint
select @firmID = firmID from [Action] where actionID = @actionID

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
			when (r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0) then 'DisabledInsertSimpleRoller' 
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
	advertTypeName nvarchar(256)
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

-- Политическая агитация: у станций всех активируемых роликов типа 6 должны быть
-- заполнены три ролика обвязки в карточке, иначе обвязку не из чего создать
if exists (
	select 1
	from @issue it
		inner join Roller r on r.rollerID = it.rollerID
		inner join MassMedia mm on mm.massmediaID = it.massmediaID
	where r.rolActionTypeID = 6
		and (mm.agitationLocalRollerID is null
			or mm.agitationAnnounceRollerID is null
			or mm.agitationFederalRollerID is null))
begin
	update it set it.statusDescription = 'AgitationStationRollersNotSet'
	from @issue it
		inner join Roller r on r.rollerID = it.rollerID
		inner join MassMedia mm on mm.massmediaID = it.massmediaID
	where r.rolActionTypeID = 6
		and (mm.agitationLocalRollerID is null
			or mm.agitationAnnounceRollerID is null
			or mm.agitationFederalRollerID is null)

	SET @blockActivation = 1;
	Insert Into @fatalErrors(name) values('AgitationStationRollersNotSet')
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
				AND tw.isDisabled = 0
				AND NOT EXISTS (SELECT 1 FROM Tariff t WHERE t.tariffID = tw.tariffId AND t.isForModuleOnly = 1)

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
				AND tw.isDisabled = 0
				AND NOT EXISTS (SELECT 1 FROM Tariff t WHERE t.tariffID = tw.tariffId AND t.isForModuleOnly = 1)
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
				AND (@allowDifferentWindowPrice = 1 OR tw.price = @transferSourcePrice)
				AND (@avoidFirmRollerWindows = 0
					OR (NOT EXISTS (
							SELECT 1
							FROM Issue fi
								INNER JOIN Campaign fc ON fc.campaignID = fi.campaignID
								INNER JOIN [Action] fa ON fa.actionID = fc.actionID
							WHERE fi.originalWindowID = tw.windowId
								AND fa.firmID = @firmID
								AND fa.deleteDate IS NULL
								AND fi.isConfirmed = 1)
						AND NOT EXISTS (
							SELECT 1 FROM @issue it2
							WHERE it2.windowId = tw.windowId
								AND it2.statusDescription = 'OK')))
				AND NOT (c.finishDate < @tomorrow and c.campaignTypeID <> 2 and @rightToGoBack <> 1)
				AND NOT (r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0)
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
		originalWindowID = pt.newWindowID,
		actualWindowID = pt.newWindowID,
		tariffPrice = dbo.fn_GetIssuePrice(
			r.duration,
			twNew.price,
			1,
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

	-- Политическая агитация: авто-вставка обвязки (44/7/55) в окна с подтверждённым
	-- типом 6; выпуски создаются в служебной акции, минус по времени окна допустим
	if exists (
		select 1 from @issue it
			inner join Roller r on r.rollerID = it.rollerID
		where it.statusDescription = 'OK' and r.rolActionTypeID = 6)
	begin
		exec AgitationFraming
			@actionName = 'InsertForAction',
			@actionID = @actionID,
			@loggedUserID = @loggedUserID
	end

	UPDATE [Action] Set isConfirmed = 1 WHERE actionID = @actionID

	exec ActionRecalculate
		@actionID = @actionID
END
GO

-- IssueTransfer
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[IssueTransfer]
(
@issueID int,
@campaignID int,
@newWindowId int,
@newDate datetime,
@newPosition smallint = null,
@loggedUserID smallint,
@massmediaID smallint,
@isConfirmed bit
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare 
	@isAdmin bit,	
	@IsTrafficManager bit,	
	@rightForMinus bit, 
	@rightToGoBack bit

-- Проверка на возможность размещения
if exists(select * 
		from DisabledWindow dw 
			inner join TariffWindow tw on tw.massmediaID = dw.massmediaID
				and tw.windowDateActual between dw.startDate And dw.finishDate
			where tw.windowID = @newWindowId)
begin
	raiserror('DisabledWindowTransfer',16,1)
	return
end 

declare @msgError varchar(50)
EXEC hlp_GetMainUserCredentials

	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT

select top 1 @msgError = 
	case
		when mm.deadLine is not null and tw.dayActual <= mm.deadLine and @isAdmin = 0 and @IsTrafficManager = 0 then 'DeadLineViolationTransfer' 
		when tw.isDisabled = 1 then 'DisabledInsertRoller'
		when r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
		when (( tw.isFirstPositionOccupied = 1 And @newPosition = -20) 
				or (tw.isSecondPositionOccupied = 1	And @newPosition = -10)
				or (tw.isLastPositionOccupied = 1	And @newPosition = 10)) then 'FirstLastIssueErrorTransfer'
		when @rightForMinus = 0 AND i.isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1)) < 0) then 'WindowMaxCapacityOverflowTransfer'
		when @rightForMinus = 0 AND i.isConfirmed = 1 and tw.[timeInUseConfirmed] + r.duration > tw.duration then 'WindowOverflowTransfer'
		else null 
	end 
from 
	Issue i 
	inner join Roller r on i.rollerID = r.rollerID
	inner join [TariffWindow] tw on tw.windowId = @newWindowId
	inner join MassMedia mm on tw.massmediaID = mm.massmediaID
where i.issueID = @issueID 
order by 1 desc

if @msgError is not null 
begin 
	raiserror(@msgError,16,1)
	return
end 

-- Политическая агитация: запоминаем окно и тип ролика до переноса, чтобы после
-- переноса снять обвязку со старого окна и создать в новом (см. хвост процедуры)
Declare @agitOldWindowID int, @agitRolActionTypeID tinyint, @agitIsConfirmed bit
Select
	@agitOldWindowID = i.actualWindowID,
	@agitRolActionTypeID = r.rolActionTypeID,
	@agitIsConfirmed = i.isConfirmed
From Issue i Inner Join Roller r On r.rollerID = i.rollerID
Where i.issueID = @issueID

Declare
	@oldDate datetime

Select
	@oldDate = tw.windowDateActual
From
	Issue i 
	inner join TariffWindow tw on i.actualWindowID = tw.windowID
Where 
	i.issueId = @issueId

Update 
	TariffWindow
Set
	timeInUseConfirmed = 
		Case 
			When i.isConfirmed = 1 AND [maxCapacity] = 0 
				Then timeInUseConfirmed - r.duration
			Else timeInUseConfirmed
		End,
	timeInUseUnconfirmed = 
		Case 
			When i.isConfirmed = 0 AND [maxCapacity] = 0
				Then timeInUseUnconfirmed - r.duration
			Else timeInUseUnconfirmed
		End,
	capacityInUseConfirmed = 
		Case 
			When (i.isConfirmed = 1 AND [maxCapacity] > 0) 
				Then capacityInUseConfirmed - 1
			Else capacityInUseConfirmed
		End,
	capacityInUseUnconfirmed = 
		Case  
			When (i.isConfirmed = 0 AND [maxCapacity] > 0)
				Then capacityInUseUnconfirmed - 1
			Else capacityInUseUnconfirmed
		End,
	isFirstPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = -20 Then 0
			Else isFirstPositionOccupied
		End,
	isSecondPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = -10 Then 0
			Else isSecondPositionOccupied
		End,
	isLastPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = 10 Then 0
			Else isLastPositionOccupied
		End,
	firstPositionsUnconfirmed = 
		Case  
			When i.isConfirmed = 0 And i.positionId = -20 Then firstPositionsUnconfirmed - 1
			Else firstPositionsUnconfirmed
		End,
	secondPositionsUnconfirmed = 
		Case 
			When i.isConfirmed = 0 And i.positionId = -10 Then secondPositionsUnconfirmed - 1
			Else secondPositionsUnconfirmed
		End,
	lastPositionsUnconfirmed = 
		Case	
			When i.isConfirmed = 0 And i.positionId = 10 Then lastPositionsUnconfirmed - 1
			Else	lastPositionsUnconfirmed
		End
From
	Issue i 
	Inner Join Roller r On r.rollerId = i.rollerId
Where
	TariffWindow.windowId = i.actualWindowId
	and i.issueID = @issueID

UPDATE Issue
SET	actualWindowID = @newWindowId,
	positionId = @newPosition
WHERE issueID = @issueID

Update 
	TariffWindow
Set
	timeInUseConfirmed = 
		Case 
			When i.isConfirmed = 1 AND [maxCapacity] = 0 
				Then timeInUseConfirmed + r.duration
			Else timeInUseConfirmed
		End,
	timeInUseUnconfirmed = 
		Case 
			When i.isConfirmed = 0 AND [maxCapacity] = 0 
				Then timeInUseUnconfirmed + r.duration
			Else timeInUseUnconfirmed
		End,
	capacityInUseConfirmed = 
		Case 
			When (i.isConfirmed = 1 AND [maxCapacity] > 0) 
				Then capacityInUseConfirmed + 1
			Else capacityInUseConfirmed
		End,
	capacityInUseUnconfirmed = 
		Case  
			When (i.isConfirmed = 0 AND [maxCapacity] > 0)
				Then capacityInUseUnconfirmed + 1
			Else capacityInUseUnconfirmed
		End,
	isFirstPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = -20 Then 1
			Else isFirstPositionOccupied
		End,
	isSecondPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = -10 Then 1
			Else isSecondPositionOccupied
		End,
	isLastPositionOccupied = 
		Case 
			When i.isConfirmed = 1 And i.positionId = 10 Then 1
			Else isLastPositionOccupied
		End,
	firstPositionsUnconfirmed = 
		Case  
			When i.isConfirmed = 0 And i.positionId = -20 Then firstPositionsUnconfirmed + 1
			Else firstPositionsUnconfirmed
		End,
	secondPositionsUnconfirmed = 
		Case 
			When i.isConfirmed = 0 And i.positionId = -10 Then secondPositionsUnconfirmed + 1
			Else secondPositionsUnconfirmed
		End,
	lastPositionsUnconfirmed = 
		Case	
			When i.isConfirmed = 0 And i.positionId = 10 Then lastPositionsUnconfirmed + 1
			Else	lastPositionsUnconfirmed
		End
From
	Issue i
	Inner Join Roller r On r.rollerId = i.rollerId
Where
	TariffWindow.windowId = i.actualWindowId
	and i.issueID = @issueID

declare @actionID int
select  @actionID = c.actionID from Campaign c where c.campaignID = @campaignID

-- Insert record to transfer log journal
-- Write Log
INSERT INTO [TransferLog]([userID], [oldDate], [newDate], [actionID], [issueID])
Values(@loggedUserID, @oldDate, @newDate, @actionID, @issueID)

-- Политическая агитация: перенос подтверждённого ролика типа 6 переносит и обвязку -
-- снимаем её со старого окна (если там больше нет агитации) и создаём в новом
If @agitRolActionTypeID = 6 And @agitIsConfirmed = 1
Begin
	Exec AgitationFraming
		@actionName = 'CleanupWindow',
		@windowID = @agitOldWindowID,
		@loggedUserID = @loggedUserID

	Exec AgitationFraming
		@actionName = 'InsertForWindow',
		@windowID = @newWindowId,
		@loggedUserID = @loggedUserID
End
GO

-- stat_FillPercentage
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- ЧАСТЬ 2: Обновлённая процедура

CREATE OR ALTER PROCEDURE [dbo].[stat_FillPercentage]
(
    @StartDay datetime = null, 
    @FinishDay datetime = null,
    @StartTime datetime = NULL,
    @FinishTime datetime = NULL,
    @MassMediaID smallint = default,
    @massmediaGroupID int = default,
    @PaymentTypeID smallint = default,
    @campaignTypeID tinyint = default,
    @IsGroupByPaymentType bit = 0,
    @IsGroupByCampaignType bit = 0,
    @IsGroupByDay bit = 0,
    @IsGroupByTariffWindow bit = 0,
    @IncludeEmptyBlocks bit = 1,
    @loggedUserID smallint,
    @Monday bit = 1,
    @Tuesday bit = 1,
    @Wednesday bit = 1,
    @Thursday bit = 1,
    @Friday bit = 1,
    @Saturday bit = 1,
    @Sunday bit = 1
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    -- НЕ ТРОГАЕМ!
    DECLARE @MinDate datetime = CAST('19000101' AS datetime);
    DECLARE @MaxDate datetime = CAST('22001231' AS datetime);
    DECLARE @MinTime time(7) = CAST('00:00:00.0000000' AS time(7));
    DECLARE @MaxTime time(7) = CAST('23:59:59.9999999' AS time(7));

    -- Предварительно вычисляем для оптимизации
    DECLARE @StartTimeCoalesced time(0) = CAST(COALESCE(CAST(@StartTime AS time), @MinTime) AS time(0));
    DECLARE @FinishTimeCoalesced time(0) = CAST(COALESCE(CAST(@FinishTime AS time), @MaxTime) AS time(0));

    DECLARE @sql nvarchar(4000);

    CREATE TABLE #massmedias
    (
        massmediaID smallint PRIMARY KEY,
        myMassmedia bit,
        foreignMassmedia bit
    );

    CREATE TABLE #available
    (
        mmid smallint,
        fulltime int,
        PRIMARY KEY (mmid)
    );

    CREATE TABLE #availableByDays
    (
        mmid smallint,
        [date] datetime,
        fulltime int,
        PRIMARY KEY (mmid, [date])
    );

    CREATE TABLE #issues
    (
        mmid smallint,
        campaigntypeid tinyint,
        paymenttypeid tinyint,
        [date] datetime,
        tariffTime time(0) not null,
        duration int,
        fulltime int,
        PRIMARY KEY (mmid, [date], tariffTime, campaigntypeid, paymenttypeid)
    );

    CREATE TABLE #availableResult
    (
        mmid smallint,
        [date] datetime,
        tariffTime time(0),
        fulltime int,
        PRIMARY KEY (mmid, [date], tariffTime)
    );

    CREATE TABLE #sold
    (
        mmid smallint,
        [date] datetime,
        tariffTime time(0),
        campaigntypeid tinyint,
        paymenttypeid tinyint,
        duration int,
        PRIMARY KEY (mmid, [date], tariffTime, campaigntypeid, paymenttypeid)
    );

    if (@StartDay is not null)
        set @StartDay = dbo.ToShortDate(@StartDay);

    if (@FinishDay is not null)
        set @FinishDay = dbo.ToShortDate(@FinishDay);

    INSERT INTO #massmedias (massmediaID, myMassmedia, foreignMassmedia)
    SELECT *
    FROM dbo.fn_GetMassmediasForUser(@loggedUserID);

    IF @IsGroupByDay = 0
    BEGIN
        IF @IsGroupByTariffWindow = 0
        BEGIN
            insert into #available
            select
                tw.massmediaID as mmid,
                sum(tw.duration) as fulltime
            from TariffWindow tw
                inner join MassMedia mm On mm.massmediaID = tw.massmediaID
            where tw.tariffId is not null
                and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                and tw.massmediaID IN (select massmediaID from #massmedias)
                AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
                and (mm.massmediaGroupID = coalesce(@massmediaGroupID, mm.massmediaGroupID) or (mm.massmediaGroupID is null and @massmediaGroupID is null))
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
            group by tw.massmediaID;

            insert into #issues (mmid, campaigntypeid, paymenttypeid, [date], tariffTime, duration, fulltime)
            select
                tw.massmediaID as mmid,
                c.campaignTypeID as campaigntypeid,
                c.paymentTypeID as paymenttypeid,
                tw.dayActual as [date],
                cast('00:00:00' as time(0)) as tariffTime,
                sum((r.duration / (case when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0 then tw.capacityInUseConfirmed else 1 end))) as duration,
                MAX(r1.fulltime)
            from TariffWindow tw
                inner join Issue i on i.actualWindowID = tw.windowId
                inner join Campaign c on i.campaignID = c.campaignID
                inner join [Action] a on c.actionID = a.actionID
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID in (1, 8, 9)
                inner join #available r1 ON r1.mmid = tw.massmediaID
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
                and EXISTS
                (
                    select 1
                    from #massmedias umm
                    where umm.massmediaID = tw.massmediaID
                        and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
                )
                AND (a.userID = @loggedUserID
                        OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1
                        OR
                        (
                            dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1
                            AND EXISTS
                            (
                                SELECT 1
                                FROM GroupMember gm
                                    JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
                                WHERE a.userID = gm.userID
                            )
                        )
                    )
            group by tw.massmediaID, tw.dayActual, c.campaignTypeID, c.paymentTypeID;
        END
        ELSE
        BEGIN
            insert into #availableResult
            select
                tw.massmediaID as mmid,
                @MinDate as [date],
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)) as tariffTime,
                sum(tw.duration) as fulltime
            from TariffWindow tw
                inner join MassMedia mm On mm.massmediaID = tw.massmediaID
            where tw.tariffId is not null
                and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                and tw.massmediaID IN (select massmediaID from #massmedias)
                AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
                and (mm.massmediaGroupID = coalesce(@massmediaGroupID, mm.massmediaGroupID) or (mm.massmediaGroupID is null and @massmediaGroupID is null))
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
            group by
                tw.massmediaID,
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0));

            insert into #sold (mmid, [date], tariffTime, campaigntypeid, paymenttypeid, duration)
            select
                tw.massmediaID as mmid,
                @MinDate as [date],
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)) as tariffTime,
                c.campaignTypeID as campaigntypeid,
                c.paymentTypeID as paymenttypeid,
                sum(
                    r.duration /
                    (case
                        when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0
                            then tw.capacityInUseConfirmed
                        else 1
                    end)
                ) as duration
            from TariffWindow tw
                inner join Issue i on i.actualWindowID = tw.windowId
                inner join Campaign c on i.campaignID = c.campaignID
                inner join [Action] a on c.actionID = a.actionID
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID in (1, 8, 9)
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
                and EXISTS
                (
                    select 1
                    from #massmedias umm
                    where umm.massmediaID = tw.massmediaID
                        and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
                )
                AND
                (
                    a.userID = @loggedUserID
                    OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1
                    OR
                    (
                        dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1
                        AND EXISTS
                        (
                            SELECT 1
                            FROM GroupMember gm
                                JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
                            WHERE a.userID = gm.userID
                        )
                    )
                )
            group by
                tw.massmediaID,
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)),
                c.campaignTypeID,
                c.paymentTypeID;
        END
    END
    ELSE
    BEGIN
        IF @IsGroupByTariffWindow = 0
        BEGIN
            insert into #availableByDays
            select
                tw.massmediaID as mmid,
                tw.dayActual as [date],
                sum(tw.duration) as fulltime
            from TariffWindow tw
            where tw.tariffId is not null
                and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                and tw.massmediaID IN (select massmediaID from #massmedias)
                AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
            group by tw.massmediaID, tw.dayActual;

            insert into #issues (mmid, campaigntypeid, paymenttypeid, [date], tariffTime, duration, fulltime)
            select
                tw.massmediaID as mmid,
                c.campaignTypeID as campaigntypeid,
                c.paymentTypeID as paymenttypeid,
                tw.dayActual as [date],
                cast('00:00:00' as time(0)) as tariffTime,
                sum((r.duration / (case when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0 then tw.capacityInUseConfirmed else 1 end))) as duration,
                MAX(r1.fulltime)
            from TariffWindow tw
                inner join Issue i on i.actualWindowID = tw.windowId
                inner join Campaign c on i.campaignID = c.campaignID
                inner join [Action] a on c.actionID = a.actionID
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID in (1, 8, 9)
                inner join #availableByDays r1 ON r1.mmid = tw.massmediaID and r1.[date] = tw.dayActual
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
                and EXISTS
                (
                    select 1
                    from #massmedias umm
                    where umm.massmediaID = tw.massmediaID
                        and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
                )
                AND (a.userID = @loggedUserID
                        OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1
                        OR
                        (
                            dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1
                            AND EXISTS
                            (
                                SELECT 1
                                FROM GroupMember gm
                                    JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
                                WHERE a.userID = gm.userID
                            )
                        )
                    )
            group by tw.massmediaID, tw.dayActual, c.campaignTypeID, c.paymentTypeID;
        END
        ELSE
        BEGIN
            insert into #availableResult
            select
                tw.massmediaID as mmid,
                tw.dayActual as [date],
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)) as tariffTime,
                sum(tw.duration) as fulltime
            from TariffWindow tw
            where tw.tariffId is not null
                and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                and tw.massmediaID IN (select massmediaID from #massmedias)
                AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
            group by
                tw.massmediaID,
                tw.dayActual,
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0));

            insert into #sold (mmid, [date], tariffTime, campaigntypeid, paymenttypeid, duration)
            select
                tw.massmediaID as mmid,
                tw.dayActual as [date],
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)) as tariffTime,
                c.campaignTypeID as campaigntypeid,
                c.paymentTypeID as paymenttypeid,
                sum(
                    r.duration /
                    (case
                        when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0
                            then tw.capacityInUseConfirmed
                        else 1
                    end)
                ) as duration
            from TariffWindow tw
                inner join Issue i on i.actualWindowID = tw.windowId
                inner join Campaign c on i.campaignID = c.campaignID
                inner join [Action] a on c.actionID = a.actionID
                inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID in (1, 8, 9)
            where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)
                and i.isConfirmed = 1
                and tw.dayActual BETWEEN COALESCE(@StartDay, @MinDate) AND COALESCE(@FinishDay, @MaxDate)
                and tw.windowTime BETWEEN @StartTimeCoalesced AND @FinishTimeCoalesced
                AND (
                    (@Sunday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 1) OR
                    (@Monday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 2) OR
                    (@Tuesday   = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 3) OR
                    (@Wednesday = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 4) OR
                    (@Thursday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 5) OR
                    (@Friday    = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 6) OR
                    (@Saturday  = 1 AND DATEPART(WEEKDAY, tw.dayActual) = 7)
                )
                and EXISTS
                (
                    select 1
                    from #massmedias umm
                    where umm.massmediaID = tw.massmediaID
                        and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
                )
                AND
                (
                    a.userID = @loggedUserID
                    OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1
                    OR
                    (
                        dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1
                        AND EXISTS
                        (
                            SELECT 1
                            FROM GroupMember gm
                                JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
                            WHERE a.userID = gm.userID
                        )
                    )
                )
            group by
                tw.massmediaID,
                tw.dayActual,
                cast(dateadd(minute, datediff(minute, 0, tw.windowDateActual), 0) as time(0)),
                c.campaignTypeID,
                c.paymentTypeID;
        END
    END

    IF @IsGroupByTariffWindow = 1
    BEGIN
        set @sql = 'select row_number() over(order by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' b.date, ';

        set @sql = @sql + ' b.tariffTime, mm.name) as RowNum, ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + 'b.date as [date], ';

        set @sql = @sql + ' convert(char(5), b.tariffTime, 108) as [tariffTime], ';
        set @sql = @sql + ' mm.[name] as [massmedia], mm.groupName, ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' pt.[name] as [paymentType], ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' ct.[name] as [campaignType], ';

        set @sql = @sql + ' dbo.fn_Int2Time(sum(isnull(s.duration, 0))) as [realTime], 
            cast(sum(cast(isnull(s.duration, 0) as float)) / max(cast(b.fulltime as float)) * 100 as decimal(5,2)) as [fill]
        from #availableResult b
            inner join vMassmedia mm on b.mmid = mm.massmediaID
            left join #sold s on s.mmid = b.mmid
                             and s.[date] = b.[date]
                             and s.tariffTime = b.tariffTime ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' left join PaymentType pt on s.paymenttypeid = pt.paymentTypeID ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' left join iCampaignType ct on s.campaigntypeid = ct.campaignTypeID ';

        set @sql = @sql + ' WHERE 1 = 1 ';

        if (@PaymentTypeID IS NOT NULL)
            set @sql = @sql + ' AND (s.paymentTypeID = ' + CAST(@PaymentTypeID AS varchar) + ' OR s.paymentTypeID IS NULL) ';

        if (@campaignTypeID IS NOT NULL)
            set @sql = @sql + ' AND (s.campaignTypeID = ' + CAST(@campaignTypeID AS varchar) + ' OR s.campaignTypeID IS NULL) ';

        set @sql = @sql + ' group by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' b.date, ';

        set @sql = @sql + ' b.tariffTime, mm.massmediaID, mm.[name], mm.groupName ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' , pt.paymentTypeID, pt.[name] ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' , ct.campaignTypeID, ct.[name] ';

        set @sql = @sql + ' order by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' b.date, ';

        set @sql = @sql + ' b.tariffTime, mm.name ';
    END
    ELSE
    BEGIN
        set @sql = 'select row_number() over(order by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' i.date, ';

        if (@IsGroupByTariffWindow = 1)
            set @sql = @sql + ' i.tariffTime, ';

        set @sql = @sql + ' mm.name) as RowNum, ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + 'i.date as [date], ';

        if (@IsGroupByTariffWindow = 1)
            set @sql = @sql + ' convert(char(5), i.tariffTime, 108) as [tariffTime], ';

        set @sql = @sql + ' mm.[name] as [massmedia], mm.groupName, ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + 'pt.[name] as [paymentType], ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + 'ct.[name] as [campaignType], ';

        set @sql = @sql + 'dbo.fn_Int2Time(sum(i.duration)) as [realTime], 
                cast(sum(cast(i.duration as float)) / max(cast(i.fulltime as float)) * 100 as decimal(5,2)) as [fill]
            from #issues i
                inner join vMassmedia mm on i.mmid = mm.massmediaID ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' inner join PaymentType pt on i.paymenttypeid = pt.paymentTypeID ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' inner join iCampaignType ct on i.campaigntypeid = ct.campaignTypeID ';

        set @sql = @sql + ' WHERE 1 = 1 ';

        if (@PaymentTypeID IS NOT NULL)
            set @sql = @sql + ' AND i.paymentTypeID = ' + CAST(@PaymentTypeID AS varchar);

        if (@campaignTypeID IS NOT NULL)
            set @sql = @sql + ' AND i.campaignTypeID = ' + CAST(@campaignTypeID AS varchar);

        set @sql = @sql + ' group by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' i.date, ';

        if (@IsGroupByTariffWindow = 1)
            set @sql = @sql + ' i.tariffTime, ';

        set @sql = @sql + ' mm.massmediaID, mm.[name], mm.groupName ';

        if (@IsGroupByPaymentType = 1)
            set @sql = @sql + ' , pt.paymentTypeID, pt.[name] ';

        if (@IsGroupByCampaignType = 1)
            set @sql = @sql + ' , ct.campaignTypeID, ct.[name] ';

        set @sql = @sql + ' order by ';

        if (@IsGroupByDay = 1)
            set @sql = @sql + ' i.date, ';

        if (@IsGroupByTariffWindow = 1)
            set @sql = @sql + ' i.tariffTime, ';

        set @sql = @sql + ' mm.name ';
    END

    exec sp_executeSQL @sql;

    DROP TABLE #issues;
    DROP TABLE #availableResult;
    DROP TABLE #sold;
    DROP TABLE #massmedias;
    DROP TABLE #available;
    DROP TABLE #availableByDays;
END
GO

-- rpt_Grid_v3
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/* 
rpt_Grid_v3:
- убрана проверка прав (параметр @loggedUserID удалён)
- добавлены замеры времени по шагам (@debug = 1)
- @userID (2026-09-25): фильтр «Менеджер» сетки вещания. При удалении проверки прав
  пропал вместе с ней, и сетка всегда показывала выпуски всех менеджеров. Вернули
  как в rpt_Grid: только выпуски и спонсорские программы акций этого менеджера,
  без «ничьих» строк (модули без выпуска, непроспонсированные программы).
  NULL — все, как раньше; выгрузка для эфира его не передаёт.
*/

CREATE OR ALTER PROCEDURE [dbo].[rpt_Grid_v3]
(
    @theDate      datetime,
    @massMediaID  smallint,
    @userID       smallint = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; -- Добавляем сюда
    SET DATEFIRST 1;

    -- локальная "функция" логирования
    -- (через однотипные куски кода, чтобы не плодить UDF)
    DECLARE @dummy int = 0;

    SET @theDate = dbo.ToShortDate(@theDate);

    -------------------------------------------------------------------------
    -- find current pricelist for passed date
    -------------------------------------------------------------------------
    DECLARE @pricelistID smallint, @broadcastStart smalldatetime;
    SELECT @pricelistID = dbo.fn_GetPricelistIDByDate(@massMediaID, @theDate, 1);
    SELECT @broadcastStart = broadcastStart FROM Pricelist WHERE pricelistID = @pricelistID;

    -------------------------------------------------------------------------
    -- GRID1: all tariff windows for day (by TariffWindow)
    -------------------------------------------------------------------------
    DECLARE @grid1 TABLE
    (
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
    );

    INSERT INTO @grid1
    SELECT
        t.tariffID,
        tw.dayActual,
        dbo.[fn_GetTimeString](@broadcastStart, tw.[windowDateActual]),
        tw.[duration],
        '',
        COALESCE(t.suffix, ''),
        COALESCE(t.needExt, 1),
        COALESCE(t.needInJingle, 1),
        COALESCE(t.needOutJingle, 1),
        COALESCE(t.[comment], ''),
        tu.tariffUnionID,
        tw.windowId,
        tw.windowNextId,
        tw.windowPrevId,
        CASE WHEN t.[notEarly]=1 THEN 'W' ELSE '' END,
        CASE WHEN t.[notLater]=1 THEN 'A' ELSE '' END,
        CASE WHEN t.[openBlock]=1 THEN 'K' ELSE '' END,
        CASE WHEN t.[openPhonogram]=1 THEN 'H' ELSE '' END,
        ISNULL(bt.code, ''),
        tw.duration_total
    FROM
        [TariffWindow] tw
        LEFT JOIN [Tariff] t ON tw.[tariffId] = t.[tariffID]
        LEFT JOIN TariffUnion tu ON t.tariffID = tu.tariffID
        LEFT JOIN BlockType bt ON bt.[blockTypeID] = t.[blockTypeID]
    WHERE
        tw.dayActual = @theDate
        AND tw.massmediaID = @massMediaID;

    -------------------------------------------------------------------------
    -- ISSUE: confirmed issues (no rights filtering now)
    -------------------------------------------------------------------------
    DECLARE @issue TABLE
    (
        issueId int primary key not null,
        issueDate datetime,
        rollerID int,
        positionId FLOAT,
        [tariffId] INT,
        moduleIssueID INT,
        packModuleIssueID INT,
        windowId int
    );

    INSERT INTO @issue
    SELECT DISTINCT
        i.issueId,
        tw.windowDateActual,
        i.rollerID,
        i.positionId,
        tw.[tariffId],
        i.[moduleIssueID],
        i.[packModuleIssueID],
        i.actualWindowID
    FROM
        Issue i
        INNER JOIN TariffWindow tw ON i.actualWindowID = tw.windowID
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
        INNER JOIN [Action] a ON c.actionID = a.actionID
    WHERE
        tw.dayActual = @theDate
        AND tw.massmediaID = @massMediaID
        AND i.[isConfirmed] = 1
        AND (@userID IS NULL OR a.userID = @userID);

    -------------------------------------------------------------------------
    -- GRID2: result rows (windows + issues)
    -------------------------------------------------------------------------
    DECLARE @grid2 TABLE
    (
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
    );

    INSERT INTO @grid2
    SELECT
        g1.tariffID,
        g1.[time],
        CASE g1.cellRealTime WHEN '' THEN g1.tariffTime ELSE g1.cellRealTime END as tariffTime,
        g1.cellRealDuration,
        i.positionId,
        r.[name] + ' ' + ip.shortDescription as [description],
        dbo.fn_Int2Time(r.duration) as rollerDurationString,
        r.duration as rollerDuration,
        r.[path],
        r.[name],
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
    FROM
        @grid1 g1
        LEFT JOIN @issue i
            ON dbo.[fn_GetTimeString](@broadcastStart, i.issueDate) = g1.tariffTime
            AND g1.windowId = i.windowId
        LEFT JOIN roller r ON i.rollerID = r.rollerID
        LEFT JOIN iIssuePosition ip ON ip.positionId = i.positionId
        LEFT JOIN [ModuleIssue] mi ON i.moduleIssueID = mi.[moduleIssueID]
        LEFT JOIN [Module] m ON mi.[moduleID] = m.[moduleID]
        LEFT JOIN [PackModuleIssue] pmi ON pmi.[packModuleIssueID] = i.packModuleIssueID
        LEFT JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
        LEFT JOIN [PackModule] pm ON pmpl.[packModuleID] = pm.[packModuleID];

    -------------------------------------------------------------------------
    -- Standalone modules in TariffWindow (если окно занято модулем и не подтверждено)
    -- (логика из pasted.txt, блок "IF (@userID IS NULL) ... update g2 ...")
    -------------------------------------------------------------------------
    UPDATE g2
    SET
        g2.[time] = t.TIME,
        g2.[cellRealDuration] = t.duration,
        g2.[positionId] = 0,
        g2.[description] = CASE WHEN r_pm.name IS NOT NULL THEN r_pm.name ELSE r_m.name END,
        g2.[rollerDurationString] = CASE WHEN r_pm.[duration] IS NOT NULL THEN dbo.fn_Int2Time(r_pm.[duration]) ELSE dbo.fn_Int2Time(r_m.[duration]) END,
        g2.[rollerDuration] = CASE WHEN r_pm.[duration] IS NOT NULL THEN r_pm.[duration] ELSE r_m.[duration] END,
        g2.[path] = CASE WHEN r_pm.[path] IS NOT NULL THEN r_pm.[path] ELSE r_m.[path] END,
        g2.[name] = CASE WHEN r_pm.name IS NOT NULL AND LEN(r_pm.[path]) > 0 THEN r_pm.name ELSE r_m.name END,
        g2.[fullDuration] = tw.duration,
        g2.[suffix] = t.[suffix],
        g2.[rolActionTypeID] = CASE WHEN r_pm.[rolActionTypeID] IS NOT NULL THEN r_pm.[rolActionTypeID] ELSE r_m.[rolActionTypeID] END,
        g2.[needExt] = t.[needExt],
        g2.[needInJingle] = t.[needInJingle],
        g2.[needOutJingle] = t.[needOutJingle],
        g2.[isAlive] = 0,
        g2.[currentPath] = CASE WHEN m.[path] IS NOT NULL THEN m.[path] ELSE pm.[path] END,
        g2.[comment] = m.name,
        g2.[tariffUnionID] = tu.tariffUnionID,
        g2.[position] = '',
        g2.broadcastStart = @broadcastStart
    FROM
        @grid2 g2
        INNER JOIN [TariffWindow] tw ON tw.tariffId = g2.tariffID
        INNER JOIN [Tariff] t ON tw.tariffID = t.tariffID
        LEFT JOIN TariffUnion tu ON t.tariffID = tu.tariffID
        LEFT JOIN [ModuleTariff] mt ON t.[tariffID] = mt.[tariffID]
        LEFT JOIN [ModulePriceList] mpl ON mpl.[modulePriceListID] = mt.[modulePriceListID]
            AND @theDate BETWEEN mpl.startDate AND mpl.finishDate
        LEFT JOIN [Module] m ON mpl.[moduleID] = m.[moduleID]
        LEFT JOIN [Roller] r_m ON mpl.[rollerID] = r_m.[rollerID]
        LEFT JOIN [PackModuleContent] pmc ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
        LEFT JOIN [PackModulePriceList] pmpl ON pmc.[pricelistID] = pmpl.[priceListID]
            AND @theDate BETWEEN pmpl.startDate AND pmpl.finishDate
        LEFT JOIN [PackModule] pm ON pmpl.[packModuleID] = pm.[packModuleID]
        LEFT JOIN [Roller] r_pm ON pmpl.[rollerID] = r_pm.[rollerID]
        LEFT JOIN Issue i ON i.[actualWindowID] = tw.[windowId]
    WHERE
        @userID IS NULL -- модуль без выпуска ничей: при отборе по менеджеру не показываем
        AND mpl.isStandAlone IS NOT NULL AND mpl.isStandAlone = 1
        AND tw.dayActual = @theDate
        AND tw.massmediaID = @massMediaID
        AND tw.[maxCapacity] > 0
        AND (r_pm.[rolActionTypeID] IS NOT NULL OR r_m.[rolActionTypeID] IS NOT NULL)
        AND (i.issueID IS NULL OR i.isConfirmed = 0);

    -------------------------------------------------------------------------
    -- пустые окна (как в pasted.txt; ОБРАТИ ВНИМАНИЕ: там был странный join.
    -- здесь делаю логично: вставляем те строки grid1, которых нет в grid2 по time)
    -------------------------------------------------------------------------
    INSERT INTO @grid2
    SELECT
        g1.tariffID,
        g1.[time],
        CASE g1.cellRealTime WHEN '' THEN g1.tariffTime ELSE g1.cellRealTime END as tariffTime,
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
        NULL,
        g1.[notEarly],
        g1.[notLater],
        g1.[openBlock],
        g1.[openPhonogram],
        g1.blockType,
        g1.durationTotal
    FROM
        @grid1 g1
        LEFT JOIN @grid2 g2 ON g2.[time] = g1.[time]
    WHERE
        g2.[time] IS NULL;

    -------------------------------------------------------------------------
    -- program issues (sponsored programs фактические)
    -------------------------------------------------------------------------
    INSERT INTO @grid2
    SELECT
        NULL,
        t.[time],
        dbo.[fn_GetTimeString](pl.broadcastStart, t.[time]),
        0,
        0,
        sp.[name] + ' [' + f.[name] + ']',
        dbo.fn_Int2Time(t.[duration]),
        t.[duration],
        '',
        COALESCE(NULLIF(t.comment, ''), sp.[name]),
        t.[duration],
        t.[suffix],
        3,
        t.[needExt],
        t.[needInJingle],
        t.[needOutJingle],
        t.isAlive,
        t.[path],
        t.[comment],
        NULL,
        '',
        pl.broadcastStart,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        0
    FROM
        programIssue i
        INNER JOIN [SponsorProgram] sp ON i.[programID] = sp.[sponsorProgramID] AND sp.[massmediaID] = @massMediaID
        INNER JOIN SponsorTariff t ON t.tariffID = i.tariffID
        INNER JOIN SponsorProgramPricelist pl ON t.priceListID = pl.pricelistID AND @theDate BETWEEN pl.[startDate] AND pl.[finishDate]
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
        INNER JOIN [Action] a ON a.actionID = c.actionID
        INNER JOIN Firm f ON f.firmID = a.firmID
    WHERE
        i.[isConfirmed] = 1
        AND i.issueDate BETWEEN
            DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate))
            AND DATEADD(ss, -1, DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate + 1)))
        AND CONVERT(varchar(5), i.issueDate, 108) = CONVERT(varchar(5), t.time, 108)
        AND (@userID IS NULL OR a.userID = @userID);

    -------------------------------------------------------------------------
    -- Программы которые не были проспонсированы (standalone sponsor pricelist)
    -------------------------------------------------------------------------
    INSERT INTO @grid2
    SELECT
        NULL,
        st.[time],
        dbo.[fn_GetTimeString](sppl.broadcastStart, st.time),
        0,
        0,
        sp.[name],
        dbo.fn_Int2Time(st.[duration]),
        st.[duration],
        '',
        COALESCE(NULLIF(st.comment, ''), sp.[name]),
        st.[duration],
        st.[suffix],
        3,
        st.[needExt],
        st.[needInJingle],
        st.[needOutJingle],
        st.isAlive,
        st.[path],
        st.comment,
        NULL,
        '',
        sppl.broadcastStart,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        0
    FROM
        [SponsorProgram] sp
        INNER JOIN [SponsorProgramPricelist] sppl ON sp.[sponsorProgramID] = sppl.[sponsorProgramID]
        INNER JOIN [SponsorTariff] st ON sppl.[pricelistID] = st.[pricelistID]
    WHERE
        @userID IS NULL -- непроспонсированная программа ничья: при отборе по менеджеру не показываем
        AND sp.massmediaID = @massMediaID
        AND sppl.isStandAlone = 1
        AND @theDate BETWEEN sppl.[startDate] AND sppl.[finishDate]
        AND
        (
            (st.[time] >= sppl.broadcastStart
                AND (
                    (DATEPART(dw, @theDate) = 1 AND st.monday = 1)
                 OR (DATEPART(dw, @theDate) = 2 AND st.thursday = 1)   -- как в исходнике (да, странно)
                 OR (DATEPART(dw, @theDate) = 3 AND st.wednesday = 1)
                 OR (DATEPART(dw, @theDate) = 4 AND st.thursday = 1)
                 OR (DATEPART(dw, @theDate) = 5 AND st.friday = 1)
                 OR (DATEPART(dw, @theDate) = 6 AND st.saturday = 1)
                 OR (DATEPART(dw, @theDate) = 7 AND st.sunday = 1)
                )
            )
            OR
            (st.[time] < sppl.broadcastStart
                AND (
                    (DATEPART(dw, @theDate) = 7 AND st.monday = 1)
                 OR (DATEPART(dw, @theDate) = 1 AND st.thursday = 1)   -- как в исходнике (да, странно)
                 OR (DATEPART(dw, @theDate) = 2 AND st.wednesday = 1)
                 OR (DATEPART(dw, @theDate) = 3 AND st.thursday = 1)
                 OR (DATEPART(dw, @theDate) = 4 AND st.friday = 1)
                 OR (DATEPART(dw, @theDate) = 5 AND st.saturday = 1)
                 OR (DATEPART(dw, @theDate) = 6 AND st.sunday = 1)
                )
            )
        )
        AND NOT EXISTS
        (
            SELECT 1
            FROM [ProgramIssue] i
                INNER JOIN SponsorTariff t ON t.tariffID = i.tariffID
                INNER JOIN SponsorProgramPricelist pl ON t.priceListID = pl.pricelistID
            WHERE
                i.[isConfirmed] = 1
                AND i.issueDate BETWEEN
                    DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate))
                    AND DATEADD(ss, -1, DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @theDate + 1)))
                AND CONVERT(varchar(5), i.issueDate, 108) = CONVERT(varchar(5), st.time, 108)
        );

    -------------------------------------------------------------------------
    -- финальный SELECT (как в исходнике)
    -------------------------------------------------------------------------
    SELECT *
    FROM
    (
        SELECT
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
            CASE WHEN [rolActionTypeID] <> 1 THEN 0 ELSE rollerDuration END AS rollerDurationSum,
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
        FROM @grid2
        -- Тип 6 (политическая агитация) - обычный оплаченный ролик: он может стоять
        -- в окне несколько раз, поэтому идёт в ветку БЕЗ DISTINCT. Иначе два
        -- одинаковых ролика кандидата в одном окне схлопывались бы в один выход.
        -- Типы 8/9 (локальное промо со спонсором / без) - по той же причине:
        -- число промо в окне не ограничено, один ролик может стоять дважды
        WHERE ([rolActionTypeID] IN (1, 6, 8, 9) OR [rolActionTypeID] IS NULL)

        UNION ALL

        SELECT DISTINCT
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
            CASE WHEN [rolActionTypeID] <> 1 THEN 0 ELSE rollerDuration END AS rollerDurationSum,
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
        FROM @grid2
        -- DISTINCT здесь защищает служебные строки (новости, программы,
        -- идентификаторы СМИ, анонс агитации) - они по одной на окно
        WHERE ([rolActionTypeID] = 2 OR ([rolActionTypeID] >= 3 AND [rolActionTypeID] NOT IN (6, 8, 9)))
    ) X
    ORDER BY
        CASE WHEN [Time] < broadcastStart THEN '1' ELSE '0' END + [tariffTime],
        positionId,
        windowPrevId;

END
GO

-------------------------------------------------------------------------------
-- 3. Проверка
-------------------------------------------------------------------------------
SELECT rolActionTypeId, name FROM [dbo].[iRollerActionType] WHERE rolActionTypeId IN (8, 9);
GO
