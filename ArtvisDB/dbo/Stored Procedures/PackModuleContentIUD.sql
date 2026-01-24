CREATE    PROCEDURE [dbo].[PackModuleContentIUD]
(
@packModuleContentID smallint = NULL,
@pricelistID smallint = NULL,
@moduleID smallint = NULL,
@modulePriceListID smallint = null,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT on

if exists(select * from PackModuleIssue where pricelistID = @pricelistID)
begin 
	raiserror('CannotChangePackModuleContent', 16, 1)
	return
end 

-- проверка типа рекламы у добавляемого массмедиа
-- в пакете может быть только один тип рекламы
IF (@actionName IN ('AddItem', 'UpdateItem'))
BEGIN
	DECLARE @count INT
	DECLARE @rolTypeInsertedID INT

	-- вычисляем кол-во типов рекламы
	SET @count = (
		SELECT 
			COUNT(DISTINCT([roltypeID]))
		FROM 
			[MassMedia] AS mm
			INNER JOIN [Module] ON mm.[massmediaID] = [Module].[massmediaID]
			INNER JOIN [PackModuleContent] ON [PackModuleContent].[moduleID] = [Module].[moduleID] 
		WHERE 
			[PackModuleContent].[pricelistID] = @pricelistID
	)
	
	-- если уже есть записи	
	IF (@count = 1)
	BEGIN
		-- вычисляем тип рекламы вставляемого пакета
		SET @rolTypeInsertedID = (
			SELECT 
				DISTINCT([roltypeID])
			FROM 
				[MassMedia] AS mm
				INNER JOIN [Module] ON mm.[massmediaID] = [Module].[massmediaID]
			WHERE 
				[Module].[moduleID] = @moduleID
		)
		
		-- вычисляем кол-во типов рекламы отличное от типа вставляемого объекта		
		SET @count = (
			SELECT 
				COUNT(DISTINCT([roltypeID]))
			FROM 
				[MassMedia] AS mm
				INNER JOIN [Module] ON mm.[massmediaID] = [Module].[massmediaID]
				INNER JOIN [PackModuleContent] ON [PackModuleContent].[moduleID] = [Module].[moduleID] 
			WHERE 
				[PackModuleContent].[pricelistID] = @pricelistID AND
				[roltypeID] <> @rolTypeInsertedID
		)
		
		-- если кол-во типов рекламы отличных от вставляемого больше нуля, то значит такой тип 
		-- рекламного пакета нам не подходит.
		IF (@count > 0)
		BEGIN
			RAISERROR ('PackModuleContent_IncorrectMediaType', 16, 1)
			return 
		END
	end
	
	-- Проверка на сроки действия прайс-листов
	declare @pmStartDate datetime, @pmFinishDate datetime 
	select @pmStartDate = startDate, @pmFinishDate = finishDate from PackModulePriceList where priceListID = @pricelistID
	
	if exists(select * 
			from ModulePriceList pl 
			where pl.modulePriceListID = @modulePriceListID 
				and (@pmStartDate < pl.startDate or @pmFinishDate > pl.finishDate))
	begin
		raiserror('PMCModulePriceListDatesError', 16, 1)
		return
	end 
	
	declare @isMaxCapacity bit 
	
	select @isMaxCapacity = case when max(t.maxCapacity) > 0 then 1 else 0 end
	from ModulePriceList pl 
		inner join ModuleTariff mt on pl.modulePriceListID = mt.modulePriceListID
		inner join Tariff t on mt.tariffID = t.tariffID
	where pl.modulePriceListID = @modulePriceListID

	if exists (select * 
				from PackModuleContent pmc 
					inner join ModuleTariff mt on pmc.modulePriceListID = mt.modulePriceListID
					inner join Tariff t on mt.tariffID = t.tariffID
				where pmc.priceListID = @pricelistID and ((t.maxCapacity > 0 and @isMaxCapacity = 0) or (t.maxCapacity = 0 and @isMaxCapacity = 1)))
	begin 
		raiserror('PMCCannotContentTariffDifferentType', 16, 1)
		return
	end 
end

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [PackModuleContent](pricelistID, moduleID,modulePriceListID)
	VALUES(@pricelistID, @moduleID,@modulePriceListID)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @packModuleContentID = SCOPE_IDENTITY()

	EXEC PackModuleContentRetrieve @packModuleContentID = @packModuleContentID
END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [PackModuleContent] 
	WHERE packModuleContentID = @packModuleContentID
ELSE IF @actionName = 'UpdateItem' 
begin
	UPDATE	[PackModuleContent]
	SET		[moduleID] = @moduleID,
			[modulePriceListID] = @modulePriceListID
	WHERE	packModuleContentID = @packModuleContentID
	EXEC PackModuleContentRetrieve @packModuleContentID = @packModuleContentID
END
