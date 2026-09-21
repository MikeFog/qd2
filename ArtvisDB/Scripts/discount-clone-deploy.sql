/*
    ПРОД-ДЕПЛОЙ: клонирование наборов скидок радиостанции и прайс-листов пакетных скидок.
    Ветка feature/discount-clone.

    ЧТО ДЕЛАЕТ
      1. dbo.DiscountReleaseIUD — новая ветка @actionName = 'Clone' и параметр
         @sourceDiscountReleaseID: копия набора скидок радиостанции на новую дату принятия
         (те же суммы/проценты DiscountValue); цепочка finishDate выравнивается как при добавлении.
      2. dbo.PackageDiscountPriceListIUD — ветка 'Clone' и параметр @sourcePackageDiscountPriceListId:
         копия прайс-листа пакетной скидки на новый период вместе с радиостанциями
         (PackageDiscountMassmedia); проверка пересечения периодов действует и на клон.
      3. dbo.DiscountReleases — сортировка ORDER BY startDate DESC (новые наборы скидок сверху в дереве).
      4. Метаданные: привязка Clone к процедурам (iModuleProcedure), действие «Создать копию» (Clone) у сущностей 22 (DiscountRelease) и
         191 (PackageDiscountPriceList), права — тем же группам, что могут добавлять объект;
         iEntity.22 переключается на класс Merlin.Classes.DiscountRelease; сообщение
         DiscountReleaseStartDateExists.
      Клиент: нужен новый Merlin.exe. Старые вызовы процедур без Clone работают как раньше.
      После наката клиентов qd2 перезапустить: сообщения (iMessage) и привязки процедур
      (iModuleProcedure) читаются один раз при старте.

    ИДЕМПОТЕНТНОСТЬ  повторный запуск безопасен. Права на процедуры сохраняются.
    ОТКАТ            процедуры — из предыдущего коммита через CREATE OR ALTER;
                     метаданные: DELETE iModuleProcedure по Clone для 22/191; DELETE iEntityAction WHERE name='Clone' AND entityID IN (22,191);
                     UPDATE iEntity SET className='FogSoft.WinForm.Classes.ObjectContainer', assemblyName=NULL WHERE entityID=22.
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO
IF OBJECT_ID('dbo.DiscountReleaseIUD') IS NULL OR OBJECT_ID('dbo.PackageDiscountPriceListIUD') IS NULL
   OR OBJECT_ID('dbo.DiscountValue') IS NULL OR OBJECT_ID('dbo.PackageDiscountMassmedia') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: нет DiscountReleaseIUD / PackageDiscountPriceListIUD / DiscountValue / PackageDiscountMassmedia. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
CREATE OR ALTER PROCEDURE [dbo].[DiscountReleaseIUD]
(
@discountReleaseID smallint = NULL,
@massmediaID smallint = NULL,
@startDate datetime = NULL,
@isForType1 bit = 0,
@isForType2 bit = 0,
@isForType3 bit = 0,
@sourceDiscountReleaseID smallint = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
set nocount on
DECLARE 
	@Id int,
	@date datetime

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [DiscountRelease](massmediaID, startDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @isForType1, @isForType2, @isForType3)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	-- Set finish date for previous discount release
	SELECT TOP 1 
		@Id = discountReleaseID		
	FROM	
		DiscountRelease
	WHERE	
		massmediaID = @massmediaID AND
		startDate	< @startDate
	ORDER BY 
		startDate DESC

	IF @Id IS NOT NULL
		UPDATE DiscountRelease SET finishDate = @startDate WHERE discountReleaseID = @Id
	SET @id = NULL 
	-- May be this discount release has finishDate 
	SELECT TOP 1 
		@Id = discountReleaseID,
		@date = startDate
	FROM	
		DiscountRelease
	WHERE	
		massmediaID = @massmediaID AND
		startDate	> @startDate
	ORDER BY 
		startDate 

	IF @Id IS NOT NULL
		UPDATE DiscountRelease SET finishDate = @startDate WHERE discountReleaseID = @DiscountReleaseID


	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'Clone' BEGIN
	-- Копия набора скидок радиостанции на новую дату принятия:
	-- те же суммы и проценты (DiscountValue), флаги типов кампаний берутся из паспорта.
	SELECT @massmediaID = massmediaID FROM DiscountRelease WHERE discountReleaseID = @sourceDiscountReleaseID

	IF @massmediaID IS NULL
	BEGIN
		raiserror('InternalError', 16, 1)
		return
	END

	IF EXISTS(SELECT * FROM DiscountRelease WHERE massmediaID = @massmediaID AND startDate = @startDate)
	BEGIN
		raiserror('DiscountReleaseStartDateExists', 16, 1)
		return
	END

	-- Конец нового периода — начало следующего набора (если он уже есть)
	SELECT TOP 1 @date = startDate
	FROM DiscountRelease
	WHERE massmediaID = @massmediaID AND startDate > @startDate
	ORDER BY startDate

	-- Набор, его суммы и конец предыдущего периода — одним целым
	SET XACT_ABORT ON
	BEGIN TRANSACTION

	INSERT INTO [DiscountRelease](massmediaID, startDate, finishDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @date, @isForType1, @isForType2, @isForType3)

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	-- Предыдущий набор заканчивается там, где начинается новый
	UPDATE DiscountRelease SET finishDate = @startDate
	WHERE discountReleaseID = (SELECT TOP 1 discountReleaseID
	                           FROM DiscountRelease
	                           WHERE massmediaID = @massmediaID AND startDate < @startDate
	                           ORDER BY startDate DESC)

	INSERT INTO [DiscountValue](discountReleaseID, summa, discount)
	SELECT @DiscountReleaseID, summa, discount
	FROM DiscountValue
	WHERE discountReleaseID = @sourceDiscountReleaseID

	COMMIT TRANSACTION

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	SELECT @date = finishDate	FROM DiscountRelease
	WHERE	DiscountReleaseID = @DiscountReleaseID

	DELETE FROM [DiscountRelease] WHERE DiscountReleaseID = @DiscountReleaseID

	UPDATE DiscountRelease SET finishDate = @date 
	WHERE	massmediaID = @massmediaID AND finishDate = @startDate	
	
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[DiscountRelease]
	SET			
		startDate = @startDate,
		isForType1 = @isForType1,
		isForType2 = @isForType2,
		isForType3 = @isForType3
	WHERE		
		discountReleaseID = @discountReleaseID

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID

END
GO
-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 01.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[PackageDiscountPriceListIUD]
(
	@packageDiscountPriceListId INT = NULL,
	@packageDiscountId INT = NULL,
	@startDate DATETIME = NULL,
	@finishDate datetime = null,
	@value decimal(18,2) = NULL,
	@discount decimal(9,4) = NULL,
	@eachVolume TINYINT = NULL,
	@sourcePackageDiscountPriceListId INT = NULL,
	@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
begin
SET NOCOUNT on
DECLARE 
	@Id int,
	@date datetime
IF @actionName = 'Clone'
	SELECT @packageDiscountId = packageDiscountID
	FROM PackageDiscountPriceList
	WHERE packageDiscountPriceListID = @sourcePackageDiscountPriceListId
IF @actionName IN('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('StartFinishDateError', 16, 1)
		RETURN
	end
END
	-- При клонировании @packageDiscountPriceListId — исходный прайс-лист, из проверки его исключать нельзя
	if @actionName in ('AddItem', 'UpdateItem', 'Clone') 
		and exists(select * 
	          from PackageDiscountPriceList pdpl 
		           where pdpl.packageDiscountID = @packageDiscountId and 
				(@actionName = 'Clone' or @packageDiscountPriceListId is null or pdpl.packageDiscountPriceListID <> @packageDiscountPriceListId)
				and (pdpl.startDate <= @finishDate
				and pdpl.finishDate >= @startDate))
	begin
		raiserror('PackageDiscountsCross',16,1)
		return 
	end

	IF @actionName = 'AddItem' BEGIN
		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume)
		VALUES(@packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume)

		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @packageDiscountPriceListId = SCOPE_IDENTITY()
		
		EXEC [PackageDiscountPriceLists] @packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'Clone' BEGIN
		-- Копия прайс-листа пакетной скидки на новый период: те же радиостанции и типы кампаний,
		-- значения (сумма, скидка, процент заполнения) берутся из паспорта.
		IF @packageDiscountId IS NULL
		BEGIN
			raiserror('InternalError', 16, 1)
			return 
		END

		-- Прайс-лист и его радиостанции — одним целым
		SET XACT_ABORT ON
		BEGIN TRANSACTION

		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume)
		VALUES(@packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume)

		SET @packageDiscountPriceListId = SCOPE_IDENTITY()

		INSERT INTO [PackageDiscountMassmedia](packageDiscountPriceListID, massmediaID, isForType1, isForType2, isForType3)
		SELECT @packageDiscountPriceListId, massmediaID, isForType1, isForType2, isForType3
		FROM PackageDiscountMassmedia
		WHERE packageDiscountPriceListID = @sourcePackageDiscountPriceListId

		COMMIT TRANSACTION

		EXEC [PackageDiscountPriceLists] @packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'DeleteItem' BEGIN
		SELECT @date = finishDate	FROM PackageDiscountPriceList
		WHERE	packageDiscountPriceListId = @packageDiscountPriceListId

		DELETE FROM PackageDiscountPriceList WHERE packageDiscountPriceListId = @packageDiscountPriceListId

		UPDATE PackageDiscountPriceList SET finishDate = @date 
		WHERE [packageDiscountID] = @packageDiscountID AND finishDate = @startDate	
	END
	ELSE IF @actionName = 'UpdateItem' BEGIN

		UPDATE	
			PackageDiscountPriceList
		SET			
			startDate = @startDate,
			finishDate = @finishDate, 
			[value] = @value,
			discount = @discount,
			eachVolume = @eachVolume
		WHERE		
			packageDiscountPriceListId = @packageDiscountPriceListId

		EXEC PackageDiscountPriceLists @packageDiscountPriceListId = @packageDiscountPriceListId
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[DiscountReleases]
(
@massmediaID smallint = NULL,
@discountReleaseID smallint = NULL,
@hideDiscountsInThePast bit = 0
)
as
set nocount on
SELECT 
	dr.*,
	'Скидки от ' + Convert(varchar(10), dr.[startDate], 104) + CASE WHEN dr.[finishDate] IS NOT NULL THEN ' до ' + Convert(varchar(10), dr.[finishDate], 104) ELSE '' END as name
FROM 
	[DiscountRelease] dr
WHERE
	dr.[massmediaID] = Coalesce(@massmediaID, dr.[massmediaID])
	AND dr.[discountReleaseID] = Coalesce(@discountReleaseID, dr.[discountReleaseID])
	And (@hideDiscountsInThePast = 0 or dr.finishDate > GETDATE() or dr.finishDate Is Null)
ORDER BY
	dr.startDate DESC
GO

-------------------------------------------------------------------------------
-- Метаданные
-------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 22 AND tableName = 'DiscountRelease')
   OR NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 191 AND tableName = 'PackageDiscountPriceList')
BEGIN
    RAISERROR('Сущности 22 / 191 не найдены или изменены. Метаданные не заливаются.', 16, 1);
    SET NOEXEC ON;
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'DiscountReleaseStartDateExists')
    INSERT INTO dbo.iMessage (name, message)
    VALUES ('DiscountReleaseStartDateExists', N'У радиостанции уже есть набор скидок с такой датой принятия. Операция прервана.');

UPDATE dbo.iEntity
SET className = 'Merlin.Classes.DiscountRelease', assemblyName = 'Merlin'
WHERE entityID = 22 AND (className <> 'Merlin.Classes.DiscountRelease' OR assemblyName IS NULL OR assemblyName <> 'Merlin');

-- Привязка действия Clone к процедуре (DataAccessor резолвит процедуру по ключу сущность_действие_модуль)
INSERT INTO dbo.iModuleProcedure (storedProcedureID, entityID, moduleID, actionNameID, connectionTimeout)
SELECT sp.storedProcedureID, v.entityID, 0, an.actionNameID, 60
FROM (VALUES (22, 'DiscountReleaseIUD'), (191, 'PackageDiscountPriceListIUD')) v(entityID, procName)
JOIN dbo.iStoredProcedure sp ON sp.name = v.procName
JOIN dbo.iActionName an ON an.name = 'Clone'
WHERE NOT EXISTS (SELECT 1 FROM dbo.iModuleProcedure mp
                  WHERE mp.entityID = v.entityID AND mp.moduleID = 0 AND mp.actionNameID = an.actionNameID);

-- (сущность, позиция в меню, имя действия-образца для прав: кто может добавлять, тот может копировать)
DECLARE @def TABLE (entityID INT, ordinal SMALLINT, sampleAction VARCHAR(64));
INSERT INTO @def VALUES (22, 13, 'AssignNew'), (191, 22, 'AssignNew');

INSERT INTO dbo.iEntityAction (entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
SELECT d.entityID, N'Создать копию', 'Clone', d.ordinal, 0, 1, NULL, NULL
FROM @def d
WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAction a WHERE a.entityID = d.entityID AND a.name = 'Clone');

INSERT INTO dbo.GroupRight (groupID, entityActionID)
SELECT gr.groupID, c.entityActionID
FROM @def d
JOIN dbo.iEntityAction s ON s.entityID = d.entityID AND s.name = d.sampleAction
JOIN dbo.iEntityAction c ON c.entityID = d.entityID AND c.name = 'Clone'
JOIN dbo.GroupRight gr ON gr.entityActionID = s.entityActionID
WHERE NOT EXISTS (SELECT 1 FROM dbo.GroupRight x WHERE x.groupID = gr.groupID AND x.entityActionID = c.entityActionID);
GO
SELECT [объект] = 'iEntityAction Clone (22, 191)', [строк] = COUNT(*), [ожидается] = '2'
FROM dbo.iEntityAction WHERE name = 'Clone' AND entityID IN (22, 191)
UNION ALL SELECT 'GroupRight на них', COUNT(*), N'как у AssignNew этих сущностей'
FROM dbo.GroupRight gr JOIN dbo.iEntityAction a ON a.entityActionID = gr.entityActionID
WHERE a.name = 'Clone' AND a.entityID IN (22, 191)
UNION ALL SELECT 'iModuleProcedure Clone (22, 191)', COUNT(*), '2'
FROM dbo.iModuleProcedure mp JOIN dbo.iActionName an ON an.actionNameID = mp.actionNameID
WHERE an.name = 'Clone' AND mp.entityID IN (22, 191)
UNION ALL SELECT 'iEntity 22 className', COUNT(*), '1'
FROM dbo.iEntity WHERE entityID = 22 AND className = 'Merlin.Classes.DiscountRelease'
UNION ALL SELECT 'iMessage', COUNT(*), '1' FROM dbo.iMessage WHERE name = 'DiscountReleaseStartDateExists';
GO
DECLARE @msg nvarchar(300) = 'Процедуры после: ' +
    CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.DiscountReleaseIUD')) LIKE '%sourceDiscountReleaseID%'
          AND OBJECT_DEFINITION(OBJECT_ID('dbo.PackageDiscountPriceListIUD')) LIKE '%sourcePackageDiscountPriceListId%'
         THEN 'OK — версии с Clone' ELSE 'ОШИБКА — старые версии' END;
PRINT @msg;
GO
SET NOEXEC OFF;
GO
