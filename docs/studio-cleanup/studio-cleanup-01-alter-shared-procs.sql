/***************************************************************************************************
  Зачистка модуля «Производство роликов» (Studio) + RolStyle — ШАГ 1 из 2.

  Правит 9 общих процедур и вью vRoller: убирает из них студийную примесь (ссылки на
  Studio/StudioOrder/RolStyle), НЕ удаляя сами процедуры. Источник — текущее состояние
  master (коммит 229ddbd, слит в master d51e10e);
  AgencyIUD — с NVARCHAR-параметрами (48bc201, 24.09.2026): с ними процедура безопасна и там, где
  колонки Agency ещё VARCHAR, а VARCHAR-версия на базе после NVARCHAR портила бы «ñ».
  Сверено с ArtvisDB 02.10.2026: все 10 объектов совпадают с репозиторием.
  Уже применено на ArtvisDev и проде Artvis (10.09.2026); этот файл — для остальных баз (Belgorod, Tumen, Univer и т. д.).

  Запускать ЦЕЛИКОМ, ПЕРЕД studio-cleanup-02-drop-deploy.sql (тот скрипт удаляет
  Studio-объекты и колонку Roller.rolStyleID, на которые здесь ещё могут быть ссылки).
  CREATE OR ALTER — безопасно перезапускать повторно.

  Перед запуском на проде — бэкап:
    BACKUP DATABASE <база> TO DISK = N'...' WITH COPY_ONLY, INIT;

  См. docs/studio-cleanup/investigation.md, docs/studio-cleanup/drop-plan.sql.
***************************************************************************************************/
SET NOCOUNT ON;
GO

CREATE OR ALTER              PROC [dbo].[agencies]
(
@agencyID smallint = null,
@ShowActive bit = 1,
@showUsed bit = 0
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

CREATE TABLE #agency (agencyID smallint)

if @showUsed = 1
 insert into #agency ( agencyID )
		select distinct c.agencyID from dbo.Campaign c
else
	INSERT INTO
		#agency(agencyID)
	SELECT distinct
		ag.agencyID
	FROM
		[Agency] ag
	WHERE
		ag.agencyID = COALESCE(@agencyID, ag.agencyID)
		and dbo.f_IsActiveChildFilter(@agencyID, ag.isActive, @ShowActive) = 1

EXEC sl_agencies
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[agencies] TO PUBLIC
    AS [dbo];
GO

CREATE OR ALTER           PROCEDURE [dbo].[AgencyIUD]
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

CREATE OR ALTER          PROC [dbo].[agencyPassport]
(
@agencyID smallint = NULL
)
AS

SET NOCOUNT ON

SELECT
	mm.isActive,
	mm.[massmediaID],
	mm.[name],
	mm.groupName as groupName2,
	Cast(
		CASE
			WHEN am.massmediaID Is NULL then 0
			ELSE 1
		END As Bit) isObjectSelected
FROM
	[vMassmedia] mm
	LEFT JOIN AgencyMassmedia am ON am.massmediaID = mm.massmediaID
		AND am.agencyID = @agencyID
ORDER BY
	isObjectSelected desc, mm.[name]
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[agencyPassport] TO PUBLIC
    AS [dbo];
GO

CREATE OR ALTER procedure LookupUsedAgency
as
begin

	select a.agencyID as [id], LTRIM(RTRIM(a.name)) as [name] from
	(
		select distinct c.agencyID from dbo.Campaign c
	) x
	inner join dbo.Agency a on x.agencyID = a.agencyID
	order by [name]

end
GO

CREATE OR ALTER       PROCEDURE [dbo].[sl_Agencies]
as
SET NOCOUNT ON
SELECT
	ag.*,
	ag.agencyID as id,
	bn.name as bankName
FROM
	[Agency] ag
	INNER JOIN #Agency ta ON ta.agencyID = ag.agencyID
	LEFT JOIN bank bn ON bn.bankID = ag.bankID
ORDER BY
	ag.[name]
GO

CREATE OR ALTER  PROCEDURE [dbo].[RollerIUD]
(
@rollerID int = null out,
@name nvarchar(64) = NULL,
@duration int = NULL,
@firmID smallint = NULL,
@rolTypeID smallint = NULL,
@path nvarchar(1024) = NULL,
@isEnabled tinyint = NULL,
@actionName varchar(32),
@createDate datetime = NULL,
@isCommon BIT = NULL,
@rolActionTypeID TINYINT = NULL,
@loggedUserID INT = NULL,
@compositionName nvarchar(512) = null,
@compositionAuthor nvarchar(512) = null,
@advertTypeID smallint = NULL
)
WITH EXECUTE AS OWNER
AS
BEGIN
SET NOCOUNT ON
IF @actionName = 'AddItem' OR @actionName = 'UpdateItem'
BEGIN
	IF (@isEnabled = 1 AND @duration <= 0)
	BEGIN
		RAISERROR('Roller_NullDuration', 16, 1)
		RETURN
	END

	if exists(select * from Roller where [name] = @name and rolActionTypeID = @rolActionTypeID
		and (@rollerID is null or rollerID <> @rollerID) and isMute = 0)
	BEGIN
		RAISERROR('RollerName_Unique', 16, 1)
		RETURN
	END
END

IF @actionName = 'AddItem' BEGIN
	IF @isCommon = 1 AND dbo.f_IsAdmin(@loggedUserID) = 0
	BEGIN
		RAISERROR('RollerIsCommon', 16, 1)
		RETURN
	END

	INSERT INTO [Roller]([name], duration, firmID, rolTypeID, path, isEnabled, isCommon, rolActionTypeID, compositionName, compositionAuthor, advertTypeID)
	VALUES(@name, @duration, @firmID, @rolTypeID, @path, @isEnabled, @isCommon, @rolActionTypeID, @compositionName, @compositionAuthor, @advertTypeID)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return
	end

	SET @rollerID = SCOPE_IDENTITY()

	EXEC Rollers @rollerID = @rollerID
END
ELSE IF @actionName = 'DeleteItem'
	Begin
	DELETE FROM [Roller] WHERE parentID = @rollerID
	DELETE FROM [Roller] WHERE rollerID = @rollerID
	End
ELSE IF @actionName = 'UpdateItem'
BEGIN
	If @advertTypeID Is Null And dbo.IsRollerInUseByActivatedAction(@rollerId) = 1
		Begin
			raiserror('IX_RollerMustHaveAdvertType', 16, 1)
			return
		End

	DECLARE @oldIsCommon bit, @canEditVideoName bit

	set @canEditVideoName = dbo.IsActionEnabled(@loggedUserID, 681) -- Roller - ChangeVideoRollerName

	SELECT @oldIsCommon = isCommon FROM [Roller] WHERE [rollerID] = @rollerID

	IF @oldIsCommon <> @isCommon AND dbo.f_IsAdmin(@loggedUserID) = 0
	BEGIN
		RAISERROR('RollerIsCommon', 16, 1)
		RETURN
	end

	if exists(select *
				from Issue i
					inner join Roller r on i.rollerID = r.rollerID
				where i.rollerID = @rollerID
					and ((r.name <> @name and @canEditVideoName <> 1)
					or r.duration <> @duration
					or r.rolTypeID <> @rolTypeID
					or r.rolActionTypeID <> @rolActionTypeID
					or r.rolTypeID <> @rolTypeID))
	begin
		RAISERROR('RollerCannotChange', 16, 1)
		RETURN
	end

	if exists(select *
				from ModulePriceList mpl
					inner join Roller r on mpl.rollerID = r.rollerID
				where r.rollerID = @rollerID
					and (r.name <> @name
					or @isCommon = 0
					or @rolActionTypeID not in (2,3)
					or @isEnabled = 0))
		or exists(select *
				from PackModulePriceList pmpl
					inner join Roller r on pmpl.rollerID = r.rollerID
				where r.rollerID = @rollerID
					and (r.name <> @name
					or @isCommon = 0
					or @rolActionTypeID not in (2,3)
					or @isEnabled = 0
					or r.rolTypeID <> @rolTypeID))
	begin
		RAISERROR('RollerCannotChangeWithPriceListPackAndModule', 16, 1)
		RETURN
	end

	UPDATE	[Roller]
	SET
		[name] = @name,
		duration = @duration,
		firmID = @firmID,
		rolTypeID = @rolTypeID,
		path = @path,
		isEnabled = @isEnabled,
		isCommon = @isCommon,
		rolActionTypeID = @rolActionTypeID,
		compositionName = @compositionName,
		compositionAuthor = @compositionAuthor,
		advertTypeID = @advertTypeID
	WHERE
		rollerID = @RollerID

	EXEC Rollers @rollerID = @rollerID
END

END
GO

CREATE OR ALTER        PROC [dbo].[RollerPassport] (
@RollerId int = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

-- 1. Roller type
SELECT rt.rolTypeID as id, rt.name, rt.isLoadable
FROM iRolType rt
ORDER BY rt.name

-- 2. roller style — справочник «стиль ролика» удалён вместе с модулем
--    «Производство роликов»; паспорт ролика поле стиля не показывает.
--    Пустой набор нужной формы, чтобы не сдвигать позиции iTableAlias.
SELECT CAST(NULL AS smallint) as id, CAST(NULL AS nvarchar(64)) as name
WHERE 1 = 0

-- 3. firms
CREATE TABLE #Firm(firmID int)
INSERT INTO #Firm SELECT firmID FROM Firm
EXEC sl_Firms

-- 4. Roller ActionType
SELECT rat.rolActionTypeID AS id, rat.NAME AS name FROM dbo.iRollerActionType rat
GO

CREATE OR ALTER PROC [dbo].[ActionRollerSetAdvertType]
(
@rollerID int,
@advertTypeID int,
@isCommon bit,
@isMute bit,
@actionID int = null,
@firmID int = null,
@changeFlag bit = 0,
@duration int,
@newRollerId int output
)
AS

If @firmID Is Null And @actionID Is Null
	Begin
	Raiserror('FirmAndActionAreNull', 16, 1)
	Return
	End

If @firmID Is Null
	Select @firmID = firmID From Action Where actionID = @actionID

-- Если это ролик "для всех фирм", который используется в модульных тарифах, то надо сделать клон этого ролика и уже ему назначить предмет рекламы
-- Усли клон уже есть - вернуть его
-- Это может быть уже "клон", на котором нажали назначить или сменить предмет рекламы

If @isCommon = 1 Or Exists (Select 1 From Roller Where rollerID = @rollerID And parentID Is Not Null)
	Begin
	If @isCommon = 1
		Begin
		Select @newRollerId = rollerID From Roller Where parentID = @rollerID And advertTypeID = @advertTypeID --And firmID = @firmID
		If @newRollerId Is Null
			Begin
			INSERT INTO [Roller]([name],[duration],[path],[isEnabled],[rolActionTypeID],[isCommon],[isMute],[advertTypeID],[parentID])
			Select [name],[duration],[path],[isEnabled],[rolActionTypeID],0,0,@advertTypeID,@rollerID From Roller Where rollerID = @rollerID

			Set @newRollerId = @@IDENTITY
			End
		End
	Else
		Begin
		Select
			@newRollerId = r1.rollerID
		From
			Roller r1
			Inner Join Roller r2 On r1.parentID = r2.parentID And r1.firmID = r2.firmID
		Where
			r2.rollerID = @rollerID And r1.advertTypeID = @advertTypeID

		If @newRollerId Is Null
			Begin
			INSERT INTO [Roller]([name],[duration],[path],[isEnabled],[rolActionTypeID],[isCommon],[isMute],[advertTypeID],[parentID])
			Select [name],[duration],[path],[isEnabled],[rolActionTypeID],0,0,@advertTypeID,parentID From Roller Where rollerID = @rollerID

			Set @newRollerId = @@IDENTITY
			End
		End
	End
Else If @isMute = 1
	Begin
	-- это случай ролика-пустышки. Тут надо поискать, вдруг у данной фирмы уже есть пустышка с таким временем и предметом рекламы
	Select @newRollerId = rollerID From Roller Where firmID = @firmID And isMute = 1 And advertTypeID = @advertTypeID And duration = @duration

	If @newRollerId Is Null
		Begin
		Update Roller Set advertTypeID = @advertTypeID Where rollerID = @rollerID
		Set @newRollerId = @rollerID
		End
	End
Else -- это обычный ролик
	Begin
	Update Roller Set advertTypeID = @advertTypeID, firmID = @firmID Where rollerID = @rollerID
	Set @newRollerId = @rollerID
	End

If @changeFlag = 1 And @newRollerId <> @rollerID
	Begin
		Update
			[PackModuleIssue] Set [rollerID] = @newRollerId
		From
			PackModuleIssue pmi Inner Join Campaign c On c.campaignID = pmi.campaignID
		Where
			c.actionID = @actionID
			And pmi.rollerID = @rollerID

		Update
			[ModuleIssue] Set [rollerID] = @newRollerId
		From
			ModuleIssue mi Inner Join Campaign c On c.campaignID = mi.campaignID
		Where
			c.actionID = @actionID
			And mi.rollerID = @rollerID

		Update
			[Issue] Set [rollerID] = @newRollerId
		From
			Issue i Inner Join Campaign c On c.campaignID = i.campaignID
		Where
			c.actionID = @actionID
			And i.rollerID = @rollerID
	End
GO

CREATE OR ALTER PROC [dbo].[SetAdvertTypeForCommmonRoller]
(
@rollerID int,
@advertTypeID int,
@issueDate datetime,
@moduleID int = null,
@pricelistID int = null, -- это packModulePricelistID
@campaignID int
)
AS

If @moduleID Is Null and @pricelistID Is Null
	Begin
	Raiserror('Для выполнения операции необходима информация о модуле или пакетном модуле', 16, 1)
	Return
	End

-- Если это ролик "для всех фирм", который используется в модульных тарифах, то надо сделать клон этого ролика и уже ему назначить предмет рекламы
-- Усли клон уже есть - вернуть его



Declare @newRollerId int, @isCommon bit
Select @isCommon = isCommon From Roller Where rollerID = @rollerID

-- Это либо ролик "для всех фирм, либо его клон"

If @isCommon = 1
	Begin
	Select @newRollerId = rollerID From Roller Where parentID = @rollerID And advertTypeID = @advertTypeID
	If @newRollerId Is Null
		Begin
		INSERT INTO [Roller]([name],[duration],[path],[isEnabled],[rolActionTypeID],[isCommon],[isMute],[advertTypeID],[parentID])
		Select [name],[duration],[path],[isEnabled],[rolActionTypeID],0,0,@advertTypeID,@rollerID From Roller Where rollerID = @rollerID

		Set @newRollerId = @@IDENTITY
		End
	End
Else
	Begin
	Select @newRollerId = r1.rollerID
	From
		Roller r1
		Inner Join Roller r2 On r1.parentID = r2.parentID
	Where
		r2.rollerID = @rollerID And r1.advertTypeID = @advertTypeID
	If @newRollerId Is Null
		Begin
		INSERT INTO [Roller]([name],[duration],[path],[isEnabled],[rolActionTypeID],[isCommon],[isMute],[advertTypeID],[parentID])
		Select [name],[duration],[path],[isEnabled],[rolActionTypeID],0,0,@advertTypeID,parentID From Roller Where rollerID = @rollerID

		Set @newRollerId = @@IDENTITY
		End
	End

If @pricelistID Is Not Null
	Begin
	Update
		[Issue] Set [rollerID] = @newRollerId
	From
		PackModuleIssue pmi
	Where
		pmi.packModuleIssueID = Issue.packModuleIssueID
		and pmi.campaignID = @campaignID
		and pmi.rollerID = @rollerID
		and pmi.issueDate = @issueDate
		and pmi.pricelistID = @pricelistID

	Update
		[PackModuleIssue] Set [rollerID] = @newRollerId
	Where
		campaignID = @campaignID
		and rollerID = @rollerID
		and issueDate = @issueDate
		and pricelistID = @pricelistID
	End

If @moduleID Is Not Null
	Begin
	Update
		[Issue] Set [rollerID] = @newRollerId
	From
		ModuleIssue mi
	Where
		mi.moduleIssueID = Issue.moduleIssueID
		and mi.campaignID = @campaignID
		and mi.rollerID = @rollerID
		and mi.issueDate = @issueDate
		and mi.moduleID = @moduleID

	Update
		[ModuleIssue] Set [rollerID] = @newRollerId
	Where
		campaignID = @campaignID
		and rollerID = @rollerID
		and issueDate = @issueDate
		and moduleID = @moduleID
	End
GO

-- vRoller — БЕЗ хвоста sp_addextendedproperty (иначе Msg 15233 при ALTER VIEW не первой инструкцией)
CREATE OR ALTER VIEW [dbo].[vRoller]
AS
SELECT   dbo.Roller.rollerID, dbo.Roller.name, dbo.Roller.duration, dbo.Roller.firmID, dbo.Roller.rolTypeID, dbo.Roller.path, dbo.Roller.createDate, dbo.Roller.isEnabled, dbo.Roller.rolActionTypeID, dbo.Roller.isCommon, dbo.Roller.isMute, dbo.Roller.compositionName, dbo.Roller.compositionAuthor, dbo.Roller.advertTypeID,
             dbo.Roller.parentID, dbo.AdvertType.name AS advertTypeName
FROM     dbo.Roller LEFT OUTER JOIN
             dbo.AdvertType ON dbo.Roller.advertTypeID = dbo.AdvertType.advertTypeID
GO

PRINT '=== ШАГ 1 готово: 9 процедур + vRoller перевыпущены без студийной примеси ===';
