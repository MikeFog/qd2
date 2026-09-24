/*
    ДЕПЛОЙ: человеческий текст в Unicode (VARCHAR -> NVARCHAR). Задача: docs/tasks/web-i18n.md, трек U.

    ЗАЧЕМ   сопоставление базы Cyrillic_General_CI_AS: в VARCHAR кодовая страница 1251, и á é ñ ¿
            молча превращаются в «?». Испанская установка не могла бы хранить названия фирм.
            Русский текст не меняется — NVARCHAR хранит его без потерь.

    ЧТО ДЕЛАЕТ (одна транзакция)
      1. Тип doubleString (23 колонки: названия/адреса фирм, групп компаний, предметов рекламы,
         реквизиты станций и агентств, тема уведомления) -> NVARCHAR(256). Имя типа сохраняется
         (новый тип + sp_rename): по нему паспорт рисует многострочное поле, код не меняется.
      2. 14 колонок VARCHAR/TEXT -> NVARCHAR той же длины (TEXT -> NVARCHAR(MAX)):
         MassMedia.name, MassmediaGroup.name, Firm.director, Agency.director, Agency.bookkeeper, Agency.prefix, Agency.directorSignature, Agency.bookkeeperSignature, Agency.reportPlace, BlockType.name, ReportType.name, iMonthName.name, ReportPartText.description, ReportPartText.reportText.
         Коды (ИНН, КПП, БИК, телефоны, e-mail, codeName, пути) не трогаются.
      3. NULL/NOT NULL и collation каждой колонки берутся из её текущего описания.
      4. Индексы UK_MassmediaGroup_Name, UIX_Firm_name, UIX_AdvertType_Parent_Name пересоздаются
         с прежними параметрами; автостатистики на этих колонках удаляются (пересоздадутся сами).
      5. 10 объектов, через которые этот текст шёл в VARCHAR: fn_ProductListByRollerId, ActionActivate, AgencyIUD, FirmIUD, Firms, MassmediaGroupIUD, MassmediaIUD, Rollers, rpt_GenericBill, stat_Bonuses.
      6. sp_refreshview для всех представлений.

    КОГДА           вне рабочего времени: ALTER COLUMN переписывает строки (Firm ~8,5 тыс., секунды)
                    и держит блокировку схемы.
    ИДЕМПОТЕНТНОСТЬ повторный запуск безопасен (уже NVARCHAR — пропускается).
    КЛИЕНТ          перезапуск не нужен; Crystal-отчёты с этими полями проверить после наката.
    ОТКАТ           обратное ALTER COLUMN ... VARCHAR(n) (текст, которого нет в 1251, потеряется).
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
BEGIN TRANSACTION;
GO

-- ===== 1. Колонки =====
IF OBJECT_ID('tempdb..#target') IS NOT NULL DROP TABLE #target;
CREATE TABLE #target (tbl sysname, col sysname, newType nvarchar(64));
INSERT INTO #target (tbl, col, newType) VALUES
    ('MassMedia', 'name', 'nvarchar(64)'),
    ('MassmediaGroup', 'name', 'nvarchar(250)'),
    ('Firm', 'director', 'nvarchar(50)'),
    ('Agency', 'director', 'nvarchar(32)'),
    ('Agency', 'bookkeeper', 'nvarchar(32)'),
    ('Agency', 'prefix', 'nvarchar(64)'),
    ('Agency', 'directorSignature', 'nvarchar(256)'),
    ('Agency', 'bookkeeperSignature', 'nvarchar(256)'),
    ('Agency', 'reportPlace', 'nvarchar(64)'),
    ('BlockType', 'name', 'nvarchar(50)'),
    ('ReportType', 'name', 'nvarchar(128)'),
    ('iMonthName', 'name', 'nvarchar(50)'),
    ('ReportPartText', 'description', 'nvarchar(128)'),
    ('ReportPartText', 'reportText', 'nvarchar(max)');
-- все колонки типа doubleString, если тип ещё VARCHAR
IF EXISTS (SELECT 1 FROM sys.types WHERE name = 'doubleString' AND is_user_defined = 1
                                     AND system_type_id = TYPE_ID('varchar'))
    INSERT INTO #target (tbl, col, newType)
    SELECT OBJECT_NAME(c.object_id), c.name, 'dbo.doubleStringN'
    FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
    WHERE c.user_type_id = TYPE_ID('dbo.doubleString');
-- уже NVARCHAR — пропускаем
DELETE tg FROM #target tg
JOIN sys.columns c ON c.object_id = OBJECT_ID('dbo.' + tg.tbl) AND c.name = tg.col
WHERE c.system_type_id = TYPE_ID('nvarchar') AND tg.newType NOT LIKE 'dbo.%';
DECLARE @n int = (SELECT COUNT(*) FROM #target);
PRINT CONCAT('Колонок к переводу: ', @n);
GO

-- ===== 2. Индексы и автостатистики на этих колонках =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась', 16, 1); SET NOEXEC ON; END
GO
IF INDEXPROPERTY(OBJECT_ID('dbo.MassmediaGroup'), 'UK_MassmediaGroup_Name', 'IndexID') IS NOT NULL
    DROP INDEX [UK_MassmediaGroup_Name] ON [dbo].[MassmediaGroup];
IF INDEXPROPERTY(OBJECT_ID('dbo.Firm'), 'UIX_Firm_name', 'IndexID') IS NOT NULL
    DROP INDEX [UIX_Firm_name] ON [dbo].[Firm];
IF INDEXPROPERTY(OBJECT_ID('dbo.AdvertType'), 'UIX_AdvertType_Parent_Name', 'IndexID') IS NOT NULL
    DROP INDEX [UIX_AdvertType_Parent_Name] ON [dbo].[AdvertType];

DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'DROP STATISTICS ' + QUOTENAME(OBJECT_SCHEMA_NAME(s.object_id)) + N'.'
             + QUOTENAME(OBJECT_NAME(s.object_id)) + N'.' + QUOTENAME(s.name) + N';' + CHAR(10)
FROM sys.stats s
JOIN sys.stats_columns sc ON sc.object_id = s.object_id AND sc.stats_id = s.stats_id
JOIN sys.columns c ON c.object_id = sc.object_id AND c.column_id = sc.column_id
JOIN #target tg ON OBJECT_ID('dbo.' + tg.tbl) = c.object_id AND tg.col = c.name
WHERE s.auto_created = 1 OR s.user_created = 1
GROUP BY s.object_id, s.name;
EXEC sp_executesql @sql;
GO

-- ===== 3. Новый тип и смена типа колонок =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась', 16, 1); SET NOEXEC ON; END
GO
IF EXISTS (SELECT 1 FROM #target WHERE newType = 'dbo.doubleStringN') AND TYPE_ID('dbo.doubleStringN') IS NULL
    CREATE TYPE [dbo].[doubleStringN] FROM NVARCHAR (256) NULL;
GO
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE [dbo].' + QUOTENAME(tg.tbl) + N' ALTER COLUMN ' + QUOTENAME(tg.col) + N' '
             + tg.newType
             + CASE WHEN tg.newType LIKE 'dbo.%' THEN N'' ELSE N' COLLATE ' + c.collation_name END
             + CASE WHEN c.is_nullable = 1 THEN N' NULL' ELSE N' NOT NULL' END + N';' + CHAR(10)
FROM #target tg
JOIN sys.columns c ON c.object_id = OBJECT_ID('dbo.' + tg.tbl) AND c.name = tg.col;
PRINT @sql;
EXEC sp_executesql @sql;
GO
-- Колонки представлений (vMassmedia и др.) ссылаются на старый тип — без обновления
-- DROP TYPE откажет.
DECLARE @v nvarchar(300);
DECLARE views CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(OBJECT_SCHEMA_NAME(v.object_id)) + N'.' + QUOTENAME(v.name)
    FROM sys.views v JOIN sys.sql_modules m ON m.object_id = v.object_id WHERE m.is_schema_bound = 0;
OPEN views;
FETCH NEXT FROM views INTO @v;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_refreshview @v;
    FETCH NEXT FROM views INTO @v;
END
CLOSE views; DEALLOCATE views;
GO
IF TYPE_ID('dbo.doubleStringN') IS NOT NULL
BEGIN
    DROP TYPE [dbo].[doubleString];
    EXEC sp_rename N'dbo.doubleStringN', N'doubleString', N'USERDATATYPE';
END
GO
GRANT REFERENCES ON TYPE::[dbo].[doubleString] TO PUBLIC;
GO

-- ===== 4. Индексы обратно =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась', 16, 1); SET NOEXEC ON; END
GO
CREATE UNIQUE NONCLUSTERED INDEX [UK_MassmediaGroup_Name]
    ON [dbo].[MassmediaGroup]([name] ASC) WITH (FILLFACTOR = 90);
CREATE UNIQUE NONCLUSTERED INDEX [UIX_Firm_name]
    ON [dbo].[Firm]([name] ASC, [inn] ASC);
CREATE NONCLUSTERED INDEX [UIX_AdvertType_Parent_Name]
    ON [dbo].[AdvertType]([parentID] ASC, [name] ASC);
GO

-- ===== 5. Процедуры и функции: текст шёл через VARCHAR =====

-- ----- fn_ProductListByRollerId -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — fn_ProductListByRollerId и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER FUNCTION [dbo].[fn_ProductListByRollerId]
(
@rollerId int
)
RETURNS nvarchar(4000)
AS
BEGIN

Declare @productList nvarchar(1000)
Set @productList = ''

Select	
	@productList = at.name
From 
	AdvertType at
	Inner Join Roller ra ON ra.advertTypeID = at.advertTypeID
WHERE	
	ra.rollerID = @rollerId

Return @productList

END
GO

-- ----- ActionActivate -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — ActionActivate и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[ActionActivate]
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

-- ----- AgencyIUD -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — AgencyIUD и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER OFF;
GO
CREATE OR ALTER PROCEDURE [dbo].[AgencyIUD]
(
@agencyID smallint = null out,
@name nvarchar(32) = NULL,
@address nvarchar(128) = NULL,
@phone varchar(32) = NULL,
@fax varchar(32) = NULL,
@email varchar(256) = NULL,
@account varchar(32) = NULL,
@inn varchar(32) = NULL,
@kpp varchar(16) = NULL,
@okpo varchar(32) = NULL,
@okonh varchar(32) = NULL,
@egrn varchar(16) = NULL,
@okved varchar(16) = NULL,
@bankID smallint = NULL,
@director nvarchar(32) = NULL,
@bookkeeper nvarchar(32) = NULL,
@prefix nvarchar(64) = NULL,
@isActive tinyint = 1,
@directorSignature nvarchar(255) = NULL,
@bookkeeperSignature nvarchar(255) = NULL,
@fullPrefix nvarchar(256) = NULL,
@reportString nvarchar(256) = NULL,
@registration nvarchar(256) = NULL,
@actionName varchar(32),
@withResultset bit = 1,
@painting image = null,
@reportPlace nvarchar(64),
@path2proposalTemplate varchar(128) = null
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Agency](name, address, phone, fax, email, account, inn, okpo, okonh, bankID, director, 
		bookkeeper, prefix, isActive, directorSignature, bookkeeperSignature, fullPrefix, reportString, 
		registration, kpp, egrn, okved, painting, reportPlace, path2proposalTemplate)
	VALUES(@name, @address, @phone, @fax, @email, @account, @inn, @okpo, @okonh, @bankID, @director, 
		@bookkeeper, @prefix, @isActive, @directorSignature, @bookkeeperSignature, @fullPrefix, @reportString, 
		@registration, @kpp, @egrn, @okved, @painting, @reportPlace, @path2proposalTemplate)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @agencyID = SCOPE_IDENTITY()	
	IF @withResultset = 1 EXEC agencies @agencyID = @agencyID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM [Agency] WHERE agencyID = @agencyID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[Agency]
	SET			
		name = @name, 
		address = @address, 
		phone = @phone, 
		fax = @fax, 
		email = @email,
		account = @account, 
		inn = @inn, 
		kpp = @kpp,
		okpo = @okpo, 
		okonh = @okonh, 
		egrn = @egrn,
		okved = @okved,
		bankID = @bankID, 
		director = @director, 
		bookkeeper = @bookkeeper, 
		prefix = @prefix, 
		isActive = @isActive, 
		directorSignature = @directorSignature, 
		bookkeeperSignature = @bookkeeperSignature, 
		fullPrefix = @fullPrefix, 
		reportString = @reportString, 
		registration = @registration,
		painting = @painting,
		reportPlace = @reportPlace,
		path2proposalTemplate = @path2proposalTemplate
	WHERE		
		AgencyID = @AgencyID

	IF @withResultset = 1 EXEC agencies @agencyID = @agencyID
END
GO

-- ----- FirmIUD -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — FirmIUD и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[FirmIUD]
(
@firmID           SMALLINT     OUT,
@headCompanyID    INT         = NULL,        
@name             NVARCHAR(256) = NULL,
@address          NVARCHAR(256)  = NULL,
@phone            VARCHAR(32)   = NULL,
@fax              VARCHAR(32)   = NULL,
@account          VARCHAR(20)   = NULL,
@email            VARCHAR(256) = NULL,
@inn              VARCHAR(20)   = NULL,
@kpp              VARCHAR(16)   = NULL,
@okonh            VARCHAR(20)   = NULL,
@okpo             VARCHAR(20)   = NULL,
@egrn             VARCHAR(16)   = NULL,
@okved            VARCHAR(16)   = NULL,
@bankID           SMALLINT      = NULL,
@prefix           NVARCHAR(16)  = NULL,
@isIdle           TINYINT       = 0,
@director         NVARCHAR(32)   = NULL,
@reportString     NVARCHAR(256) = NULL,
@registration     NVARCHAR(256) = NULL,
@actionName       VARCHAR(32)
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    -- Проверка уникальности
    IF @actionName IN ('AddItem', 'UpdateItem') 
       AND EXISTS(
           SELECT 1 
           FROM Firm 
           WHERE ISNULL(@firmID, -1) <> firmID 
             AND inn = @inn 
             AND ISNULL(kpp, '') = ISNULL(@kpp, '') 
             AND ISNULL(account, '') = ISNULL(@account, '')
       )
    BEGIN
        RAISERROR('FirmInnUnique', 16, 1);
        RETURN;
    END

    IF @actionName = 'AddItem'
    BEGIN
        -- 1) Создаём головную организацию если ее не выбрали
		If @headCompanyID Is Null
			EXEC [dbo].[HeadCompanyIUD]
				@headCompanyID = @headCompanyID OUTPUT,
				@name          = @name,
				@actionName    = 'AddItem';

        -- 2) Вставляем фирму с привязкой к HeadCompany
        IF @firmID IS NOT NULL
        BEGIN
            SET IDENTITY_INSERT [Firm] ON;

            INSERT INTO [Firm]
                (firmID, headCompanyID, name, address, phone, fax, email, account, inn, okonh, okpo, bankID, prefix, 
                 isIdle, kpp, egrn, okved, director, reportString, registration)
            VALUES
                (@firmID, @headCompanyID, @name, @address, @phone, @fax, @email, @account, @inn, @okonh, @okpo, 
                 @bankID, @prefix, @isIdle, @kpp, @egrn, @okved, @director, @reportString, @registration);

            SET IDENTITY_INSERT [Firm] OFF;
        END
        ELSE
        BEGIN
            INSERT INTO [Firm]
                (headCompanyID, name, address, phone, fax, email, account, inn, okonh, okpo, bankID, prefix, 
                 isIdle, kpp, egrn, okved, director, reportString, registration)
            VALUES
                (@headCompanyID, @name, @address, @phone, @fax, @email, @account, @inn, @okonh, @okpo, 
                 @bankID, @prefix, @isIdle, @kpp, @egrn, @okved, @director, @reportString, @registration);

            IF @@ROWCOUNT <> 1
            BEGIN
                RAISERROR('InternalError', 16, 1);
                RETURN;
            END

            SET @firmID = SCOPE_IDENTITY();
        END

        EXEC [dbo].[Firms]
            @firmID        = @firmID,
            @ShowActive    = 1,
            @ShowInactive  = 1
    END
    ELSE IF @actionName = 'DeleteItem'
    BEGIN
        DELETE FROM [Firm] WHERE firmID = @firmID;
		DELETE FROM HeadCompany WHERE NOT EXISTS (SELECT 1 FROM Firm WHERE HeadCompany.headCompanyID = Firm.headCompanyID)
    END
    ELSE IF @actionName = 'UpdateItem'
    BEGIN
        -- Обновляем саму фирму
        UPDATE [Firm]
        SET headCompanyID = COALESCE(@headCompanyID, headCompanyID),
            name          = @name,
            address       = @address,
            phone         = @phone,
            fax           = @fax,
            email         = @email,
            account       = @account,
            inn           = @inn,
            kpp           = @kpp,
            okonh         = @okonh,
            okpo          = @okpo,
            egrn          = @egrn,
            okved         = @okved,
            bankID        = @bankID,
            prefix        = @prefix,
            isIdle        = @isIdle,
            director      = @director,
            reportString  = @reportString,
            registration  = @registration
        WHERE firmID = @firmID;

		DELETE FROM HeadCompany WHERE NOT EXISTS (SELECT 1 FROM Firm WHERE HeadCompany.headCompanyID = Firm.headCompanyID)

        -- Обновляем журнал фирм с фильтрацией по HeadCompany
        EXEC [dbo].[Firms]
            @firmID        = @firmID,
            @ShowActive    = 1,
            @ShowInactive  = 1
    END
END
GO

-- ----- Firms -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — Firms и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
-- OPTION (RECOMPILE) на основном SELECT: процедура — «catch-all» с набором
-- (@param IS NULL OR col = @param) и подзапросом ai, который при @userId IS NULL
-- ранжирует ВСЕ подтверждённые Action по фирме. Один закэшированный план не
-- годится для всех комбинаций параметров: 31.08.2026 залипший план дал 7,5 с и
-- 53 тыс. чтений на вызов (× сотни вызовов), сервер встал. Компиляция этого
-- запроса — единицы мс, вызывается он редко (открытие диалога выбора фирм),
-- поэтому per-call recompile дешевле любого риска plan-sniffing.
CREATE OR ALTER PROCEDURE [dbo].[Firms]
(
@firmID           SMALLINT    = NULL,
@headCompanyID    INT         = NULL,   -- новый параметр
@ShowActive       BIT         = 1,
@ShowInactive     BIT         = 0,
@lastDateBefore   DATETIME    = NULL,
@lastDateAfter    DATETIME    = NULL,
@userId           INT         = NULL,
@ShowWithAction   BIT         = 1,
@ShowWithoutAction BIT        = 1,
@name nvarchar(256) = null
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

	If @firmID Is Not Null
		Select @ShowActive = isIdle, @ShowInactive = ~isIdle From Firm where firmID = @firmID

    SELECT 
        f.*,
        ai.finishDate AS lastDate,
        u.userName   AS lastManager,
		hc.name as headCompanyName
    FROM 
        [Firm] f
		Left Join HeadCompany hc on hc.headCompanyID = f.headCompanyID
        LEFT JOIN 
        (
            SELECT ActionId, FirmID, userID, finishDate
            FROM (
                SELECT
                    ActionId,
                    userID,
                    FirmID,
                    finishDate,
                    ROW_NUMBER() OVER (PARTITION BY FirmID ORDER BY finishDate DESC) AS rn
                FROM Action 
                WHERE userID = ISNULL(@userId, userID) and isConfirmed = 1
            ) AS RankedActions
            WHERE rn = 1
        ) AS ai 
            ON f.firmID = ai.firmID
        LEFT JOIN [User] u 
            ON u.userID = ai.userID
    WHERE
        f.firmID = COALESCE(@firmID, f.firmID)
        AND (@headCompanyID IS NULL OR f.HeadCompanyID = @headCompanyID)   -- фильтрация по новой колонке
        AND ((f.isIdle = 1 AND @ShowActive   = 1) OR (f.isIdle = 0 AND @ShowInactive = 1))
        AND ((ai.userID IS NULL AND @ShowWithoutAction = 1) OR (ai.userID IS NOT NULL AND @ShowWithAction = 1))
        AND (@lastDateBefore IS NULL OR @lastDateBefore > finishDate)
        AND (@lastDateAfter  IS NULL OR @lastDateAfter  < finishDate)
        AND (@userId IS NULL OR ai.userID = ISNULL(@userId, ai.userID))
		AND (@name IS NULL OR f.name LIKE '%' + @name + '%') 
    ORDER BY
        [name]
    OPTION (RECOMPILE);
END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Firms] TO PUBLIC
    AS [dbo];
GO

-- ----- MassmediaGroupIUD -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — MassmediaGroupIUD и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[MassmediaGroupIUD]
(
	@massmediaGroupID int = null out,
	@name nvarchar(250) = null,
	@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

    IF @actionName = 'AddItem' BEGIN
		INSERT INTO MassmediaGroup([name]) VALUES(@name)

		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @massmediaGroupID = SCOPE_IDENTITY()

		EXEC MassmediaGroups @massmediaGroupID = @massmediaGroupID
	END
	ELSE IF @actionName = 'DeleteItem' BEGIN
		DELETE FROM MassmediaGroup WHERE massmediaGroupID = @massmediaGroupID
	END
	ELSE IF @actionName = 'UpdateItem' BEGIN
		update MassmediaGroup set [name] = @name where massmediaGroupID = @massmediaGroupID

		EXEC MassmediaGroups @massmediaGroupID = @massmediaGroupID
	end 
END
GO

-- ----- MassmediaIUD -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — MassmediaIUD и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[MassmediaIUD]
(
@massmediaID smallint = NULL,
@name nvarchar(32) = NULL,
@prefix nvarchar(256) = NULL,
@roltypeID smallint = NULL,
@deadLine datetime = NULL,
@isActive bit = NULL,
@director nvarchar(256) = NULL,
@fullPrefix nvarchar(256) = NULL,
@reportString nvarchar(256) = NULL,
@actionName varchar(32),
@rollerEnterPath NVARCHAR(255) = NULL,
@rollerExitPath NVARCHAR(255) = NULL,
@rollerEtcPath NVARCHAR(255) = NULL,
@rollerPath NVARCHAR(255) = NULL,
@rollerEnterMax smallint = NULL,
@rollerExitMax smallint = NULL,
@rollerEtcMax smallint = null,
@rollerEnterMin smallint = NULL,
@rollerExitMin smallint = NULL,
@rollerEtcMin smallint = null,
@massmediaGroupID int = null,
@exportName nvarchar(255) = null,
@loggedUserID smallint,
@mediaPlusMassmediaID smallint = null,
@painting image = null,
@certificateIssued nvarchar(256),
@volume_c decimal(5,2),
@volume_n decimal(5,2),
@volume_p decimal(5,2),
@volume_m decimal(5,2),
@volume_j decimal(5,2),
@agitationLocalRollerID int = null,
@agitationAnnounceRollerID int = null,
@agitationFederalRollerID int = null,
@agitationExcludeIntervals nvarchar(256) = null
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on

if @mediaPlusMassmediaID = 0
	set @mediaPlusMassmediaID = null

-- Интервалы-исключения политической агитации: проверяем формат строки
-- (необязательные дни недели + ЧЧ:ММ-ЧЧ:ММ через ';'), см. fn_AgitationExcludeIntervals
if @actionName in ('AddItem', 'UpdateItem')
	and exists (select 1 from dbo.fn_AgitationExcludeIntervals(@agitationExcludeIntervals) where startMin is null)
begin
	raiserror('AgitationIntervalsInvalid', 16, 1)
	return
end

if @actionName in ('AddItem', 'UpdateItem') and @mediaPlusMassmediaID is not null
begin 
	if exists(select * from MassMedia mm where (@massmediaID is null or @massmediaID <> mm.massmediaID) and mm.mediaPlusMassmediaID = @mediaPlusMassmediaID)
	begin 
		raiserror('MassmediaWithSameMediaPlusIdExist', 16, 1)
		return
	end 
end 

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Massmedia](roltypeID, deadLine, isActive, rollerEnterPath, 
		rollerExitPath, rollerEtcPath, rollerPath, rollerEnterMax, rollerExitMax, rollerEtcMax, rollerEnterMin, rollerExitMin, rollerEtcMin, massmediaGroupID, 
		exportName,mediaPlusMassmediaID, [name], director, painting, prefix, [fullPrefix], [reportString], certificateIssued,
		volume_c, volume_n, volume_p, volume_m, volume_j,
		agitationLocalRollerID, agitationAnnounceRollerID, agitationFederalRollerID, agitationExcludeIntervals)
	VALUES(@roltypeID, @deadLine, @isActive, @rollerEnterPath, @rollerExitPath,
		@rollerEtcPath, @rollerPath, @rollerEnterMax, @rollerExitMax, @rollerEtcMax, @rollerEnterMin, @rollerExitMin, @rollerEtcMin, @massmediaGroupID,
		@exportName,@mediaPlusMassmediaID, @name, @director, @painting, @prefix, @fullPrefix, @reportString, @certificateIssued,
		@volume_c, @volume_n, @volume_p, @volume_m, @volume_j,
		@agitationLocalRollerID, @agitationAnnounceRollerID, @agitationFederalRollerID, @agitationExcludeIntervals)
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @massmediaID = SCOPE_IDENTITY()

	--Exec massmediaList @massmediaID = @massmediaID, @loggedUserID = @loggedUserID

	insert into UserMassmedia (userID,	massmediaID, canWork, canAdd) 
	values (@loggedUserID, @massmediaID, 1, 1) 

	select * from MassMedia where massmediaID = @massmediaID
END
ELSE IF @actionName = 'DeleteItem' begin
	DELETE FROM [Massmedia] WHERE massmediaID = @massmediaID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[Massmedia]
	SET
		[name] = @name,
		roltypeID = @roltypeID, 
		deadLine = @deadLine, 
		isActive = Coalesce(@isActive, isActive),
		rollerEnterPath = @rollerEnterPath, 
		rollerExitPath = @rollerExitPath, 
		rollerEtcPath = @rollerEtcPath, 
		rollerPath = @rollerPath, 
		rollerEnterMax = @rollerEnterMax, 
		rollerExitMax = @rollerExitMax, 
		rollerEtcMax = @rollerEtcMax,
		rollerEnterMin = @rollerEnterMin, 
		rollerExitMin = @rollerExitMin, 
		rollerEtcMin = @rollerEtcMin,
		massmediaGroupID = @massmediaGroupID, 
		exportName = @exportName,
		mediaPlusMassmediaID = @mediaPlusMassmediaID,
		director = @director,
		painting = @painting,
		prefix=@prefix,
		[fullPrefix] = @fullPrefix,
		[reportString] = @reportString,
		certificateIssued = @certificateIssued,
		volume_c = @volume_c,
		volume_n = @volume_n,
		volume_p = @volume_p,
		volume_m = @volume_m,
		volume_j = @volume_j,
		agitationLocalRollerID = @agitationLocalRollerID,
		agitationAnnounceRollerID = @agitationAnnounceRollerID,
		agitationFederalRollerID = @agitationFederalRollerID,
		agitationExcludeIntervals = @agitationExcludeIntervals
	WHERE
		massmediaID = @massmediaID

	Exec massmediaList @massmediaID = @massmediaID, @loggedUserID = @loggedUserID
END
GO

-- ----- Rollers -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — Rollers и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[Rollers]
(
@firmID smallint = NULL,
@rollerID int = NULL,
@createDateStart DATETIME = null,
@createDateFinish DATETIME = null,
@rollerName NVARCHAR(32) = NULL,
@rollerCheckName NVARCHAR(32) = NULL,
@ShowActive BIT = 1,
@ShowInactive BIT = 0,
@isCommonOnly BIT = 0,
@showSimpleRollers BIT = 1,
@withoutID int = null,
@showUsed BIT = 1,
@showUnused BIT = 1,
@advertTypeID smallint = NULL,
@showMuteRollers bit = 1
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
CREATE TABLE #Roller(rollerID int)

If @rollerID Is Not Null
	INSERT INTO #Roller Values(@rollerID)
Else
	Begin
	if @showUnused != 0 or @showUsed != 0
	begin
		INSERT INTO #Roller
		SELECT 
			r.rollerID 
		FROM 
			[Roller] r
			LEFT JOIN Firm f ON f.firmID = r.firmID
			LEFT JOIN AdvertType advt on advt.advertTypeID = r.advertTypeID
		where
			r.rollerID = Coalesce(@rollerID, r.rollerID) AND
			((@ShowActive = 1 AND r.[isEnabled] = 1) OR (@ShowInactive = 1 AND r.[isEnabled] = 0)) and
			r.[createDate] >= COALESCE(@createDateStart, r.[createDate]) AND
			r.[createDate] <= DATEADD(DAY, 1, COALESCE(@createDateFinish, r.[createDate])) and
			r.NAME LIKE ISNULL(@rollerName, '%') + '%' AND 
			r.NAME LIKE '%' + ISNULL(@rollerCheckName, '%') + '%'
			And (@firmID IS NULL OR (r.[firmID] = @firmID))
			AND (@isCommonOnly = 0 OR r.[isCommon] = 1)
			AND (@showSimpleRollers = 1 OR r.[rolActionTypeID] <> 1)
			and (@withoutID is null or (r.rollerID <> @withoutID))
			And (@advertTypeID IS NULL OR (r.advertTypeID = @advertTypeID OR advt.parentID = @advertTypeID))
			And r.parentID Is Null
		ORDER BY 
			r.name
	end

	if @showUsed = 1 and @showUnused = 0
		begin
			delete from r
			from #Roller r
				left join (
					select distinct i.rollerId from Issue i where i.rollerID is not null
					union 
					select distinct mi.rollerId from ModuleIssue mi where mi.rollerID is not null
					union 
					select distinct mpl.rollerId from ModulePriceList mpl where mpl.rollerID is not null
					union 
					select distinct pmi.rollerId from PackModuleIssue pmi where pmi.rollerID is not null
					union 
					select distinct pmpl.rollerId from PackModulePriceList pmpl where pmpl.rollerID is not null
				) as x on r.rollerID = x.rollerID
			where x.rollerID is null 
		end
	else if @showUsed = 0 and @showUnused = 1
		begin
			delete from r
			from #Roller r
				left join (
					select distinct i.rollerId from Issue i where i.rollerID is not null
					union 
					select distinct mi.rollerId from ModuleIssue mi where mi.rollerID is not null
					union 
					select distinct mpl.rollerId from ModulePriceList mpl where mpl.rollerID is not null
					union 
					select distinct pmi.rollerId from PackModuleIssue pmi where pmi.rollerID is not null
					union 
					select distinct pmpl.rollerId from PackModulePriceList pmpl where pmpl.rollerID is not null
				) as x on r.rollerID = x.rollerID
			where x.rollerID is not null
		end
	End
/*
declare @showMuteRollers bit 
set @showMuteRollers = case when @rollerID is not null then 1 else 0 end 
*/
EXEC sl_Rollers @showMuteRollers = @showMuteRollers
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Rollers] TO PUBLIC
    AS [dbo];
GO

-- ----- rpt_GenericBill -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — rpt_GenericBill и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[rpt_GenericBill]
(
@actionId int,
@agencyId smallint,
@beginDate datetime = null,
@endDate datetime = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare @invoiceTableText nvarchar(1024), @invoiceTableTextSponsor nvarchar(1024)
select @invoiceTableText = reportText from [dbo].[ReportPartText] where codeName='invoice1'
select @invoiceTableTextSponsor = reportText from [dbo].[ReportPartText] where codeName='InvoiceSponsor'

declare @isByMonth bit
select @isByMonth = case when @beginDate is not null and @endDate is not null then 1 else 0 end

Declare @billDate datetime
if (@isByMonth = 1)
	Select @billDate = @endDate
else
	Select @billDate = billDate
	From [Bill]
	Where	actionID = @actionId And agencyID = @agencyId

declare @res Table (
	[name] NVARCHAR(255),
	[price] decimal(18,2) NOT null,
	[taxPrice] decimal(18,2) not null
)

declare cur_campaigns cursor local fast_forward
for 
select c.campaignID, c.campaignTypeID, c.startDate, c.finishDate
from Campaign c 
	Inner Join Paymenttype pt On pt.PaymenttypeId = c.PaymenttypeId
		and pt.IsHidden = 0
where 
	c.Actionid = @ActionId and
	c.AgencyId = @AgencyId and
	((@beginDate is null and @endDate is null) or 
	(c.startDate <= @endDate and c.finishDate >=  @beginDate))

declare @campaignID int, @campaignTypeID tinyint, @price decimal(18,2), @startDate datetime, @finishDate datetime,@taxPrice decimal(18,2)
open cur_campaigns

fetch next from cur_campaigns into @campaignID, @campaignTypeID, @startDate, @finishDate

while @@fetch_status = 0
begin
	if (@isByMonth = 0)
	begin
		select @beginDate = @startDate, @endDate = @finishDate
	end 

	-- Кампания без единого выпуска в периоде счёта в счёт не попадает (решение заказчика
	-- от 15.09.2026). Раньше такая кампания давала либо ошибку 515 (GetPriceByPeriod на
	-- раннем выходе не присваивает @taxPrice, в @res.taxPrice уходил NULL), либо строку с
	-- нулевой ценой и НДС соседней кампании — переменные цикла между итерациями не сбрасывались.
	-- Проверяем факт выпусков, а не даты кампании: у части кампаний startDate/finishDate пусты
	-- при живых выпусках (акция 185835), по датам такие кампании молча исчезли бы из счёта.
	-- Пустой период (@beginDate is null) здесь означает «вся кампания».
	-- Типы 2 и 4 не проверяем: там вставка идёт запросом с GROUP BY, при отсутствии выпусков
	-- он сам не даёт ни одной строки.
	if @campaignTypeID in (1,3)
		and not exists (
			select 1
			from Issue i
				inner join TariffWindow tw on i.originalWindowID = tw.windowID
			where @campaignTypeID = 1 and i.campaignID = @campaignID
				and (@beginDate is null or tw.dayOriginal between @beginDate and @endDate))
		and not exists (
			select 1
			from ModuleIssue mi
			where @campaignTypeID = 3 and mi.campaignID = @campaignID
				and (@beginDate is null or mi.issueDate between @beginDate and @endDate))
	begin
		fetch next from cur_campaigns into @campaignID, @campaignTypeID, @startDate, @finishDate
		continue
	end

	-- Сбрасываем на каждой кампании: GetPriceByPeriod на ранних выходах присваивает не все
	-- out-параметры, и без сброса в счёт уходит цена или НДС предыдущей кампании.
	select @price = null, @taxPrice = null

	if (@campaignTypeID in (1,3))
	begin 
		exec GetPriceByPeriod
			@campaignID = @campaignID,
			@campaignTypeID = @campaignTypeID, 
			@startDate = @beginDate,
			@finishDate = @endDate,
			@price = @price OUT,
			@showBlack = 0,
			@withTax = 1,
			@taxPrice = @taxPrice out
		
		insert into @res 
		select replace(replace(@invoiceTableText, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), '{группа радиостанций}', 
			IsNull(mm.groupName, '{группа радиостанций}')) as name,
			@price as price, @taxPrice
		from Campaign c 
			inner join vMassmedia mm on c.massmediaID = mm.massmediaID
		where c.campaignID = @campaignID
	end 
	else if (@campaignTypeID in (2))
	begin 
		insert into @res
		Select	
			--'Реклама в программе ''' + p.[name] + '''' AS NAME,
			replace(replace(replace(@invoiceTableTextSponsor, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), 
			'{группа радиостанций}', IsNull(mm.groupName, '{группа радиостанций}')),  
			'{программа}', p.[name]) as name,
			Sum(i.[tariffPrice] * i.[ratio]) as price,
			sum(case when coalesce(at.divisor, 0) < 0.0000001 then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join SponsorProgram p on i.programID = p.sponsorProgramID 
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPricelist pl on st.priceListID = pl.pricelistID
			inner join vMassmedia mm on c.massmediaID = mm.massmediaID
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)), 112), 112) between at.startDate and at.finishDate
		Where		
			i.campaignID = @campaignID and 
			i.issueDate between DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @beginDate)) and dateadd(ss, -1, DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), dateadd(day, 1, @endDate))))
		GROUP BY 
			p.[name], mm.reportString, mm.groupName
	end 
	else if (@campaignTypeID in (4))
	begin 
		Exec GetPriceByPeriod 
			@campaignId, @campaignTypeID, @beginDate, @endDate, @price out, @withTax = 1, @taxPrice = @taxPrice out
		
		CREATE TABLE #tmp(massmediaID SMALLINT, price decimal(18,2), taxPrice decimal(18,2))--, tariffPrice decimal(18,2))
		INSERT INTO #tmp
		SELECT 
			m.[massmediaID], sum(mpl.[price]),--, i.[tariffPrice]
			sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		FROM [PackModuleIssue] i 
			inner join dbo.Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (pt.IsHidden = 0)
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
		WHERE 
			i.campaignID = @campaignID
			and i.issueDate between @beginDate and @finishDate
		group by m.massmediaID

		declare @sumPrice decimal(18,2)
		SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1
		
		INSERT INTO @res 
		select replace(replace(@invoiceTableText, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), '{группа радиостанций}', IsNull(mm.groupName, '{группа радиостанций}')) as name,
				(@price * sum(t1.price)/ @sumPrice) as price,
				(@taxPrice * sum(t1.price)/ @sumPrice) as taxPrice
		from #tmp as t1
			inner join vMassmedia mm on t1.massmediaID = mm.massmediaID
		group by t1.massmediaID, mm.groupName, mm.reportString
		
		drop table #tmp 
		
	end 		
	
	fetch next from cur_campaigns into @campaignID, @campaignTypeID, @startDate, @finishDate
end

close cur_campaigns
deallocate cur_campaigns

declare @res2 Table (
	[name] NVARCHAR(255),
	[quantity] int NOT null,
	[tax] decimal(18,2) NOT null,
	[price] decimal(18,2) not null,
	dirPainting image,
	qrCode image
)

-- Result
Insert into @res2 ([name], [quantity], [tax], [price])
SELECT 
	[name],
	COUNT(*) AS [quantity],
	sum([taxPrice])	as tax,	
	SUM([price]) AS [price]
FROM 	   
	@res
GROUP BY 
	[name]

Update @res2 Set dirPainting = painting
From Agency Where agencyID = @agencyId

select * from @res2
GO

-- ----- stat_Bonuses -----
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — stat_Bonuses и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[stat_Bonuses]
(
    @periodStartDate         DATETIME,
    @periodFinishDate        DATETIME,
    @userID                  SMALLINT = NULL,
    @massmediaGroupID        INT = NULL,
    @minBonusPercentage      DECIMAL(5, 2) = NULL,
    @showBlack               BIT = 1,
    @withZeroRegularCostOnly BIT = 0,
    @selectByCreateDate      BIT = 0,         -- если 1, отбор по Action.createDate, цена = finalPrice
    @isGroupByFirm           BIT = 1          -- 1 = группировка по фирме (как раньше); 0 = по головной организации (HeadCompany) — тогда firmID/firmName в рекордсете отсутствуют
)
AS
BEGIN
    SET NOCOUNT ON

    IF @periodStartDate IS NULL OR @periodFinishDate IS NULL
    BEGIN
        RAISERROR('Start date and finish date are required', 16, 1)
        RETURN
    END

    SET @periodStartDate  = CAST(CAST(@periodStartDate  AS DATE) AS DATETIME)
    SET @periodFinishDate = CAST(CAST(@periodFinishDate AS DATE) AS DATETIME)

    -- isSpecial-акции (ручная договорная цена, Campaign.price) в этот отчёт
    -- не входят ни в одном режиме — это отдельная категория записей, не
    -- связанная с обычным учётом оплаты/бонусов по выпускам.

    IF @selectByCreateDate = 1
    BEGIN
        IF @isGroupByFirm = 1
        BEGIN
            SELECT
                CONCAT(a.[firmID], '_', ISNULL(mg.[massmediaGroupID], 0), '_', ISNULL(a.[userID], 0)) AS UniqueKey,
                a.[firmID],
                f.[name]                         AS firmName,
                mg.[massmediaGroupID],
                mg.[name]                        AS massmediaGroupName,
                a.[userID],
                ISNULL(vu.[userName], 'Unknown') AS userName,
                COUNT(c.[campaignID])            AS CampaignCount,
                SUM(CASE WHEN c.[paymentTypeID] = 26
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS TotalBonusCost,
                SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS RegularCost,
                SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 1
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS HiddenCost,
                CASE
                    WHEN SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                  THEN c.[finalPrice]
                                  ELSE 0 END) = 0 THEN NULL
                    ELSE ROUND(
                        SUM(CASE WHEN c.[paymentTypeID] = 26
                                 THEN c.[finalPrice]
                                 ELSE 0 END)
                        / SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                   THEN c.[finalPrice]
                                   ELSE 0 END) * 100, 2)
                END                              AS BonusPercentage,
                @periodStartDate                 AS PeriodStartDate,
                @periodFinishDate                AS PeriodFinishDate,
                @selectByCreateDate              AS SelectByCreateDate
            FROM
                [dbo].[Campaign] c
                INNER JOIN [dbo].[Action] a         ON c.[actionID]         = a.[actionID]
                INNER JOIN [dbo].[Firm] f            ON a.[firmID]           = f.[firmID]
                INNER JOIN [dbo].[PaymentType] pt    ON pt.[PaymenttypeId]   = c.[paymenttypeId]
                LEFT  JOIN [dbo].[MassMedia] m       ON c.[massmediaID]      = m.[massmediaID]
                LEFT  JOIN [dbo].[MassmediaGroup] mg ON m.[massmediaGroupID] = mg.[massmediaGroupID]
                LEFT  JOIN [dbo].[vUser] vu          ON a.[userID]           = vu.[userID]
            WHERE
                a.[createDate] >= @periodStartDate
                AND a.[createDate] <  DATEADD(DAY, 1, @periodFinishDate)
                AND a.[isConfirmed] = 1
                AND a.[isSpecial] = 0
                AND (@userID IS NULL OR a.[userID] = @userID)
                AND (@massmediaGroupID IS NULL OR mg.[massmediaGroupID] = @massmediaGroupID)
                AND (c.[finalPrice] <> 0
                     OR (c.[campaignTypeID] = 1 AND EXISTS(SELECT 1 FROM [dbo].[Issue] i WHERE i.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 2 AND EXISTS(SELECT 1 FROM [dbo].[ProgramIssue] pi WHERE pi.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 3 AND EXISTS(SELECT 1 FROM [dbo].[ModuleIssue] mi WHERE mi.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 4 AND EXISTS(SELECT 1 FROM [dbo].[PackModuleIssue] pmi WHERE pmi.[campaignID] = c.[campaignID])))
            GROUP BY
                a.[firmID], f.[name],
                mg.[massmediaGroupID], mg.[name],
                a.[userID], vu.[userName]
            HAVING
                (@minBonusPercentage IS NULL
                 OR ROUND(
                    SUM(CASE WHEN c.[paymentTypeID] = 26
                             THEN c.[finalPrice]
                             ELSE 0 END)
                    / NULLIF(SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                      THEN c.[finalPrice]
                                      ELSE 0 END), 0) * 100, 2
                    ) > @minBonusPercentage)
                AND (@withZeroRegularCostOnly = 0
                     OR SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                 THEN c.[finalPrice]
                                 ELSE 0 END) = 0)
            ORDER BY
                f.[name] ASC, mg.[name] ASC, vu.[userName] ASC, RegularCost DESC
        END
        ELSE
        BEGIN
            SELECT
                CONCAT('H', f.[headCompanyID], '_', ISNULL(mg.[massmediaGroupID], 0), '_', ISNULL(a.[userID], 0)) AS UniqueKey,
                f.[headCompanyID],
                hc.[name]                        AS headCompanyName,
                mg.[massmediaGroupID],
                mg.[name]                        AS massmediaGroupName,
                a.[userID],
                ISNULL(vu.[userName], 'Unknown') AS userName,
                COUNT(c.[campaignID])            AS CampaignCount,
                SUM(CASE WHEN c.[paymentTypeID] = 26
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS TotalBonusCost,
                SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS RegularCost,
                SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 1
                         THEN c.[finalPrice]
                         ELSE 0 END)             AS HiddenCost,
                CASE
                    WHEN SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                  THEN c.[finalPrice]
                                  ELSE 0 END) = 0 THEN NULL
                    ELSE ROUND(
                        SUM(CASE WHEN c.[paymentTypeID] = 26
                                 THEN c.[finalPrice]
                                 ELSE 0 END)
                        / SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                   THEN c.[finalPrice]
                                   ELSE 0 END) * 100, 2)
                END                              AS BonusPercentage,
                @periodStartDate                 AS PeriodStartDate,
                @periodFinishDate                AS PeriodFinishDate,
                @selectByCreateDate              AS SelectByCreateDate
            FROM
                [dbo].[Campaign] c
                INNER JOIN [dbo].[Action] a         ON c.[actionID]         = a.[actionID]
                INNER JOIN [dbo].[Firm] f            ON a.[firmID]           = f.[firmID]
                LEFT  JOIN [dbo].[HeadCompany] hc    ON f.[headCompanyID]    = hc.[headCompanyID]
                INNER JOIN [dbo].[PaymentType] pt    ON pt.[PaymenttypeId]   = c.[paymenttypeId]
                LEFT  JOIN [dbo].[MassMedia] m       ON c.[massmediaID]      = m.[massmediaID]
                LEFT  JOIN [dbo].[MassmediaGroup] mg ON m.[massmediaGroupID] = mg.[massmediaGroupID]
                LEFT  JOIN [dbo].[vUser] vu          ON a.[userID]           = vu.[userID]
            WHERE
                a.[createDate] >= @periodStartDate
                AND a.[createDate] <  DATEADD(DAY, 1, @periodFinishDate)
                AND a.[isConfirmed] = 1
                AND a.[isSpecial] = 0
                AND (@userID IS NULL OR a.[userID] = @userID)
                AND (@massmediaGroupID IS NULL OR mg.[massmediaGroupID] = @massmediaGroupID)
                AND (c.[finalPrice] <> 0
                     OR (c.[campaignTypeID] = 1 AND EXISTS(SELECT 1 FROM [dbo].[Issue] i WHERE i.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 2 AND EXISTS(SELECT 1 FROM [dbo].[ProgramIssue] pi WHERE pi.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 3 AND EXISTS(SELECT 1 FROM [dbo].[ModuleIssue] mi WHERE mi.[campaignID] = c.[campaignID]))
                     OR (c.[campaignTypeID] = 4 AND EXISTS(SELECT 1 FROM [dbo].[PackModuleIssue] pmi WHERE pmi.[campaignID] = c.[campaignID])))
            GROUP BY
                f.[headCompanyID], hc.[name],
                mg.[massmediaGroupID], mg.[name],
                a.[userID], vu.[userName]
            HAVING
                (@minBonusPercentage IS NULL
                 OR ROUND(
                    SUM(CASE WHEN c.[paymentTypeID] = 26
                             THEN c.[finalPrice]
                             ELSE 0 END)
                    / NULLIF(SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                      THEN c.[finalPrice]
                                      ELSE 0 END), 0) * 100, 2
                    ) > @minBonusPercentage)
                AND (@withZeroRegularCostOnly = 0
                     OR SUM(CASE WHEN c.[paymentTypeID] != 26 AND pt.[isHidden] = 0
                                 THEN c.[finalPrice]
                                 ELSE 0 END) = 0)
            ORDER BY
                hc.[name] ASC, mg.[name] ASC, vu.[userName] ASC, RegularCost DESC
        END

        RETURN
    END

    -- =====================================================================
    -- Стандартный режим: цена по периоду вычисляется set-based напрямую
    -- (Issue/ProgramIssue/ModuleIssue/PackModuleIssue), без курсора и без
    -- построчных EXEC GetPriceByPeriod. Логика 4 типов кампаний воспроизводит
    -- GetPriceByPeriod максимально точно, включая то, что для campaignTypeID=4
    -- итог пропорционально делится между массмедиа. isSpecial-акции исключены
    -- из #CampaignBase целиком (см. фильтр ниже) — они не относятся к этому
    -- отчёту.
    --
    -- hasPlacementInPeriod: отдельно от @showBlack-гейта фиксирует, было ли у
    -- кампании хоть одно реальное размещение (Issue/ProgramIssue/ModuleIssue/
    -- PackModuleIssue) в периоде. Нужно, чтобы SUM() по costInPeriod не путал
    -- "кампания без выпусков в периоде" (NULL, суммой игнорируется) с
    -- "реальная стоимость равна нулю" — иначе @withZeroRegularCostOnly ловит
    -- фирмы без единого выпуска в периоде наравне с фирмами, которые реально
    -- потратили 0 обычных денег.
    -- =====================================================================

    CREATE TABLE #CampaignCosts
    (
        [campaignID]           INT,
        [firmID]               SMALLINT,
        [firmName]             NVARCHAR(MAX),
        [headCompanyID]        INT,
        [headCompanyName]      NVARCHAR(256),
        [massmediaGroupID]     INT,
        [massmediaGroupName]   NVARCHAR(250),
        [userID]               SMALLINT,
        [userName]             NVARCHAR(MAX),
        [paymentTypeID]        SMALLINT,
        [isHidden]             BIT,
        [costInPeriod]         DECIMAL(18, 2),
        [hasPlacementInPeriod] TINYINT
    )

    SELECT DISTINCT
        c.[campaignID], c.[campaignTypeID], c.[paymentTypeID],
        a.[firmID], a.[userID] AS actionUserID,
        f.[name] AS firmName,
        f.[headCompanyID], hc.[name] AS headCompanyName,
        pt.[isHidden],
        mg.[massmediaGroupID], mg.[name] AS massmediaGroupName,
        ISNULL(vu.[userName], 'Unknown') AS userName
    INTO #CampaignBase
    FROM [dbo].[Campaign] c
        INNER JOIN [dbo].[Action] a ON c.[actionID] = a.[actionID]
        INNER JOIN [dbo].[Firm] f ON a.[firmID] = f.[firmID]
        LEFT JOIN [dbo].[HeadCompany] hc ON f.[headCompanyID] = hc.[headCompanyID]
        INNER JOIN [dbo].[PaymentType] pt ON pt.[PaymenttypeId] = c.[paymenttypeId]
        LEFT JOIN [dbo].[MassMedia] m ON c.[massmediaID] = m.[massmediaID]
        LEFT JOIN [dbo].[MassmediaGroup] mg ON m.[massmediaGroupID] = mg.[massmediaGroupID]
        LEFT JOIN [dbo].[vUser] vu ON a.[userID] = vu.[userID]
    WHERE
        c.[startDate] <= @periodFinishDate
        AND c.[finishDate] >= @periodStartDate
        AND a.[isConfirmed] = 1
        AND a.[isSpecial] = 0
        AND (@userID IS NULL OR a.[userID] = @userID)
        AND (@massmediaGroupID IS NULL OR mg.[massmediaGroupID] = @massmediaGroupID OR c.[campaignTypeID] = 4)

    -- Тип 1 (линейные ролики). Одна строка на кампанию всегда (LEFT JOIN),
    -- независимо от showBlack — как в оригинале (INSERT после EXEC
    -- выполняется безусловно). costInPeriod = NULL, если showBlack блокирует
    -- платёжный тип (соответствует поведению GetPriceByPeriod: тот же самый
    -- (@showBlack=1 OR pt.isHidden=0) джойн проваливается что для
    -- обычного расчёта — то есть результат NULL).
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
           cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               ELSE SUM(CASE WHEN tw.[windowId] IS NOT NULL THEN i.[tariffPrice] * i.[ratio] END)
           END AS costInPeriod,
           MAX(CASE WHEN tw.[windowId] IS NOT NULL THEN 1 ELSE 0 END) AS hasPlacementInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[Issue] i ON i.[campaignID] = cb.[campaignID]
        LEFT JOIN [dbo].[TariffWindow] tw ON i.[originalWindowID] = tw.[windowId]
            AND tw.[dayOriginal] BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 1
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
             cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden]

    -- Тип 2 (спонсорские программы)
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
           cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               ELSE SUM(CASE WHEN pl.[priceListID] IS NOT NULL THEN i.[tariffPrice] * i.[ratio] END)
           END AS costInPeriod,
           MAX(CASE WHEN pl.[priceListID] IS NOT NULL THEN 1 ELSE 0 END) AS hasPlacementInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[ProgramIssue] i ON i.[campaignID] = cb.[campaignID]
        LEFT JOIN [dbo].[SponsorTariff] st ON i.[tariffID] = st.[tariffID]
        LEFT JOIN [dbo].[SponsorProgramPriceList] pl ON st.[priceListID] = pl.[priceListID]
            AND CONVERT(DATETIME, CONVERT(VARCHAR(8),
                DATEADD(MINUTE, -DATEPART(MINUTE, pl.[broadcastStart]),
                    DATEADD(HOUR, -DATEPART(HOUR, pl.[broadcastStart]), i.[issueDate])), 112), 112)
                BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 2
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
             cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden]

    -- Тип 3 (модульные)
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
           cb.[massmediaGroupID], cb.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               ELSE SUM(i.[tariffPrice] * i.[ratio])
           END AS costInPeriod,
           MAX(CASE WHEN i.[moduleIssueID] IS NOT NULL THEN 1 ELSE 0 END) AS hasPlacementInPeriod
    FROM #CampaignBase cb
        LEFT JOIN [dbo].[ModuleIssue] i ON i.[campaignID] = cb.[campaignID]
            AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
    WHERE cb.[campaignTypeID] = 3
    GROUP BY cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
             cb.[massmediaGroupID], cb.[massmediaGroupName],
             cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden]

    -- Тип 4 (пакетные): состав пар campaignID×massmediaID определяется ТОЛЬКО
    -- датой выпуска (как massmedia_cursor в оригинале — без фильтра по
    -- showBlack), а сама стоимость — через CASE как выше. Для
    -- пропорционального деления вес считается отдельно (T4Weight), но набор
    -- строк (T4Set) не зависит от showBlack, чтобы не терять строки.
    -- T4Set уже строится через INNER JOIN PackModuleIssue с фильтром по
    -- периоду, поэтому любая строка, попавшая в эту вставку, по построению
    -- имеет реальное размещение в периоде — hasPlacementInPeriod = 1 всегда.
    ;WITH T4Set AS (
        SELECT DISTINCT cb.[campaignID], m.[massmediaID], mg2.[massmediaGroupID], mg2.[name] AS massmediaGroupName
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
            INNER JOIN [dbo].[PackModuleContent] pmc ON i.[priceListID] = pmc.[pricelistID]
            INNER JOIN [dbo].[ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
            INNER JOIN [dbo].[Module] m ON mpl.[moduleID] = m.[moduleID]
            LEFT JOIN [dbo].[MassMedia] mm2 ON m.[massmediaID] = mm2.[massmediaID]
            LEFT JOIN [dbo].[MassmediaGroup] mg2 ON mm2.[massmediaGroupID] = mg2.[massmediaGroupID]
        WHERE cb.[campaignTypeID] = 4
    ),
    T4Weight AS (
        SELECT cb.[campaignID], m.[massmediaID], SUM(mpl.[price]) AS mmPrice
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
            INNER JOIN [dbo].[PackModuleContent] pmc ON i.[priceListID] = pmc.[pricelistID]
            INNER JOIN [dbo].[ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
            INNER JOIN [dbo].[Module] m ON mpl.[moduleID] = m.[moduleID]
        WHERE cb.[campaignTypeID] = 4 AND (@showBlack = 1 OR cb.[isHidden] = 0)
        GROUP BY cb.[campaignID], m.[massmediaID]
    ),
    T4Total AS (
        SELECT cb.[campaignID], SUM(i.[tariffPrice] * i.[ratio]) AS packModulePrice
        FROM #CampaignBase cb
            INNER JOIN [dbo].[PackModuleIssue] i ON i.[campaignID] = cb.[campaignID]
                AND i.[issueDate] BETWEEN @periodStartDate AND @periodFinishDate
        WHERE cb.[campaignTypeID] = 4 AND (@showBlack = 1 OR cb.[isHidden] = 0)
        GROUP BY cb.[campaignID]
    ),
    T4Sum AS (
        SELECT [campaignID], SUM([mmPrice]) AS sumPrice
        FROM T4Weight
        GROUP BY [campaignID]
    )
    INSERT INTO #CampaignCosts
    SELECT cb.[campaignID], cb.[firmID], cb.[firmName], cb.[headCompanyID], cb.[headCompanyName],
           t4s.[massmediaGroupID], t4s.[massmediaGroupName],
           cb.[actionUserID], cb.[userName], cb.[paymentTypeID], cb.[isHidden],
           CASE
               WHEN NOT (@showBlack = 1 OR cb.[isHidden] = 0) THEN NULL
               ELSE tot.[packModulePrice] * w.[mmPrice] / s.[sumPrice]
           END AS costInPeriod,
           1 AS hasPlacementInPeriod
    FROM T4Set t4s
        INNER JOIN #CampaignBase cb ON cb.[campaignID] = t4s.[campaignID]
        LEFT JOIN T4Total tot ON tot.[campaignID] = t4s.[campaignID]
        LEFT JOIN T4Weight w ON w.[campaignID] = t4s.[campaignID] AND w.[massmediaID] = t4s.[massmediaID]
        LEFT JOIN T4Sum s ON s.[campaignID] = t4s.[campaignID]
    WHERE (@massmediaGroupID IS NULL OR t4s.[massmediaGroupID] = @massmediaGroupID)

    IF @isGroupByFirm = 1
    BEGIN
        SELECT
            CONCAT([firmID], '_', ISNULL([massmediaGroupID], 0), '_', ISNULL([userID], 0)) AS UniqueKey,
            [firmID],
            [firmName],
            [massmediaGroupID],
            [massmediaGroupName],
            [userID],
            [userName],
            COUNT([campaignID]) AS CampaignCount,
            SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END) AS TotalBonusCost,
            SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) AS RegularCost,
            SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 1 THEN [costInPeriod] ELSE 0 END) AS HiddenCost,
            CASE
                WHEN SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0 THEN NULL
                ELSE ROUND(
                    (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                    / SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END)) * 100, 2)
            END AS BonusPercentage,
            @periodStartDate    AS PeriodStartDate,
            @periodFinishDate   AS PeriodFinishDate,
            @selectByCreateDate AS SelectByCreateDate
        FROM #CampaignCosts
        GROUP BY
            [firmID], [firmName],
            [massmediaGroupID], [massmediaGroupName],
            [userID], [userName]
        HAVING
            (@minBonusPercentage IS NULL
             OR ROUND(
                (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                / NULLIF(SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END), 0)) * 100, 2
                ) > @minBonusPercentage)
            AND (@withZeroRegularCostOnly = 0
                 OR (SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0
                     AND MAX([hasPlacementInPeriod]) = 1))
        ORDER BY
            [firmName] ASC, [massmediaGroupName] ASC, [userName] ASC, RegularCost DESC
    END
    ELSE
    BEGIN
        SELECT
            CONCAT('H', [headCompanyID], '_', ISNULL([massmediaGroupID], 0), '_', ISNULL([userID], 0)) AS UniqueKey,
            [headCompanyID],
            [headCompanyName],
            [massmediaGroupID],
            [massmediaGroupName],
            [userID],
            [userName],
            COUNT([campaignID]) AS CampaignCount,
            SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END) AS TotalBonusCost,
            SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) AS RegularCost,
            SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 1 THEN [costInPeriod] ELSE 0 END) AS HiddenCost,
            CASE
                WHEN SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0 THEN NULL
                ELSE ROUND(
                    (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                    / SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END)) * 100, 2)
            END AS BonusPercentage,
            @periodStartDate    AS PeriodStartDate,
            @periodFinishDate   AS PeriodFinishDate,
            @selectByCreateDate AS SelectByCreateDate
        FROM #CampaignCosts
        GROUP BY
            [headCompanyID], [headCompanyName],
            [massmediaGroupID], [massmediaGroupName],
            [userID], [userName]
        HAVING
            (@minBonusPercentage IS NULL
             OR ROUND(
                (SUM(CASE WHEN [paymentTypeID] = 26 THEN [costInPeriod] ELSE 0 END)
                / NULLIF(SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END), 0)) * 100, 2
                ) > @minBonusPercentage)
            AND (@withZeroRegularCostOnly = 0
                 OR (SUM(CASE WHEN [paymentTypeID] != 26 AND [isHidden] = 0 THEN [costInPeriod] ELSE 0 END) = 0
                     AND MAX([hasPlacementInPeriod]) = 1))
        ORDER BY
            [headCompanyName] ASC, [massmediaGroupName] ASC, [userName] ASC, RegularCost DESC
    END

    DROP TABLE #CampaignCosts
    DROP TABLE #CampaignBase
END
GO

SET QUOTED_IDENTIFIER ON;
GO

-- ===== 6. Представления =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась', 16, 1); SET NOEXEC ON; END
GO
DECLARE @v nvarchar(300);
DECLARE views CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(OBJECT_SCHEMA_NAME(v.object_id)) + N'.' + QUOTENAME(v.name)
    FROM sys.views v JOIN sys.sql_modules m ON m.object_id = v.object_id WHERE m.is_schema_bound = 0;
OPEN views;
FETCH NEXT FROM views INTO @v;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_refreshview @v;
    FETCH NEXT FROM views INTO @v;
END
CLOSE views; DEALLOCATE views;
GO

IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — ничего не применено', 16, 1); SET NOEXEC ON; END
GO
COMMIT TRANSACTION;
PRINT 'Переход на NVARCHAR применён.';
GO
SET NOEXEC OFF;
GO
