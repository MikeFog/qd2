



CREATE           PROC [dbo].[PackModuleTariffWindowsRetrieve]
(
@pricelistId SMALLINT,
@startDate DATETIME,
@finishDate DATETIME,
@showUnconfirmed BIT = 0
)
AS

SET NOCOUNT ON

DECLARE @tariffCount SMALLINT, @windowsCount SMALLINT 

DECLARE @res TABLE (
	date DATETIME, duration INT, 
	isFirstBusy BIT NOT NULL, isSecondBusy BIT NOT NULL, isLastBusy BIT NOT NULL, maxCapacity SMALLINT, freeCapacity SMALLINT)

DECLARE 
	@currentDate DATETIME,
	@isPresent BIT,
	@dayOfWeek SMALLINT,
	@freeTimeConfirmed INT, @freeTimeUnConfirmed INT,
	@isFirstBusyConfirmed BIT, 	@isSecondBusyConfirmed BIT, 	@isLastBusyConfirmed BIT,
	@isFirstBusyUnConfirmed BIT, 	@isSecondBusyUnConfirmed BIT, 	@isLastBusyUnConfirmed BIT,
	@freeCapacityConfirmed SMALLINT, @freeCapacityUnConfirmed SMALLINT, @maxCapacity SMALLINT

SET @currentDate = @startDate
WHILE @currentDate <= @finishDate 
	BEGIN
	
	SELECT @tariffCount = COUNT(*) 
	FROM 
		[PackModuleContent]  pmc
		INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
	WHERE	
		pmc.[pricelistID] = @pricelistId	
	
	SELECT @windowsCount = COUNT(*) FROM 
				[PackModuleContent]  pmc
				INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
				INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			WHERE 
				pmc.[pricelistID] = @pricelistId
				AND tw.dayOriginal = @currentDate
		
	IF @windowsCount = @tariffCount AND @tariffCount > 0
				
	BEGIN
		SELECT 
			@freeTimeConfirmed = MIN(tw.duration - tw.timeInUseConfirmed),
			@freeTimeUnConfirmed = MIN(tw.duration - tw.timeInUseUnconfirmed - tw.timeInUseConfirmed),
			@isFirstBusyConfirmed = MAX(CAST(isFirstPositionOccupied AS INT)),
			@isSecondBusyConfirmed = MAX(CAST([isSecondPositionOccupied] AS INT)),
			@isLastBusyConfirmed = MAX(CAST(isLastPositionOccupied AS INT)),
			@isFirstBusyUnConfirmed = MAX([firstPositionsUnconfirmed]),
			@isSecondBusyUnConfirmed = MAX([secondPositionsUnconfirmed]),
			@isLastBusyUnConfirmed = MAX([lastPositionsUnconfirmed]),			
			@freeCapacityConfirmed = MIN(tw.[maxCapacity] - tw.[capacityInUseConfirmed]),
			@freeCapacityUnConfirmed = MIN(tw.[maxCapacity] - tw.[capacityInUseUnConfirmed] - tw.[capacityInUseConfirmed]),			
			@maxCapacity = MIN(tw.[maxCapacity])
		FROM 
			[PackModuleContent]  pmc
			INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join Tariff t on tw.tariffID  = t.tariffID 
			inner join Pricelist pl on pl.pricelistID = t.priceListID
		WHERE 
			pmc.[pricelistID] = @pricelistId
			and tw.dayOriginal = @currentDate
	
		INSERT INTO @res ([date], [duration], [isFirstBusy], [isSecondBusy], [isLastBusy], [maxCapacity], [freeCapacity]) 
		VALUES (
			@currentDate, 
			CASE @showUnconfirmed
				WHEN 1 THEN @freeTimeUnConfirmed
				ELSE @freeTimeConfirmed
			END, 
			CASE
				WHEN (@showUnconfirmed = 1 AND @isFirstBusyUnConfirmed = 1) OR @isFirstBusyConfirmed = 1 THEN 1
				ELSE 0
			END, 
			CASE
				WHEN (@showUnconfirmed = 1 AND @isSecondBusyUnConfirmed = 1) OR @isSecondBusyConfirmed = 1 THEN 1
				ELSE 0
			END,
			CASE
				WHEN (@showUnconfirmed = 1 AND @isLastBusyUnConfirmed = 1) OR @isLastBusyConfirmed = 1 THEN 1
				ELSE 0
			END,
			@maxCapacity,
			CASE
				WHEN (@showUnconfirmed = 1) THEN @freeCapacityUnConfirmed
				ELSE @freeCapacityConfirmed
			END
			) 
	END
	SET @currentDate = DATEADD(DAY, 1, @currentDate)		
	END

SELECT * FROM @res

RETURN

