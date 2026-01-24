


CREATE   PROCEDURE [dbo].[ModulePricelistIUD]
(
@modulePriceListID smallint = NULL,
@sourceModulePriceListID SMALLINT = NULL,
@moduleID smallint = NULL,
@priceListID smallint = NULL,
@startDate DATETIME,
@finishDate DATETIME,
@price money = NULL,
@rollerID INT = NULL,
@isStandAlone BIT = 0,
@extraChargeFirstRoller tinyint = NULL,
@extraChargeSecondRoller tinyint = NULL,
@extraChargeLastRoller tinyint = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT on

if @actionName in ('AddItem', 'UpdateItem', 'Clone')
begin 
	DECLARE @priceListStartDate DATETIME
	DECLARE @priceListFinishDate DATETIME

	DECLARE @maxPriceListStartDate DATETIME

	select @priceListStartDate = startDate, @priceListFinishDate = finishDate FROM [Pricelist] WHERE [pricelistID] = @priceListID

	if not (@startDate between @priceListStartDate and @priceListFinishDate)
	BEGIN
		RAISERROR ( N'ModulePriceListBadStart', 16, 1)
		RETURN
	END

	if not (@finishDate between @priceListStartDate and @priceListFinishDate)
	BEGIN
		RAISERROR (N'ModulePriceListBadFinish', 16, 1)
		RETURN
	END

	IF (@finishDate < @startDate)
	BEGIN
		RAISERROR (N'ModulePriceListBadDays', 16, 1)
		RETURN
	END

	if exists(select * from ModulePriceList mpl 
				where mpl.moduleID = @moduleID 
					and mpl.startDate <= @finishDate and mpl.finishDate >= @startDate
					and mpl.modulePriceListID <> IsNull(@modulePriceListID, -1))
	begin 
		RAISERROR ('ModulePriceListTimeSpanExists', 16, 1)
		RETURN
	end 
end 

IF @actionName = 'Clone' AND NOT EXISTS(SELECT DISTINCT
											t2.[tariffID]
										FROM 
											ModuleTariff mt
											INNER JOIN [Tariff] t ON mt.[tariffID] = t.[tariffID]
											INNER JOIN [Pricelist] pl ON t.[pricelistID] = pl.[pricelistID]
											LEFT JOIN [Pricelist] pl2 ON pl.[massmediaID] = pl2.[massmediaID] 
												AND pl2.[pricelistID] = @priceListID
												AND pl2.[startDate] <= @startDate
												AND pl2.[finishDate] >= @finishDate
											LEFT JOIN [Tariff] t2 ON pl2.[pricelistID] = t2.[pricelistID]
												AND t.[time] = t2.[time]
												AND t.[monday] = t2.[monday]
												AND t.[tuesday] = t2.[tuesday]
												AND t.[wednesday] = t2.[wednesday]
												AND t.[thursday] = t2.[thursday]
												AND t.[sunday] = t2.[sunday]
												AND t.[saturday] = t2.[saturday]
										WHERE mt.modulePriceListID = @sourceModulePriceListID
										GROUP BY t2.[tariffID]
										HAVING t2.[tariffID] IS NOT NULL )
BEGIN 
	RAISERROR ('CopyPriceListFirst', 16, 1)
	RETURN
END 

IF (@actionName IN ('AddItem', 'Clone')) BEGIN
	INSERT INTO [ModulePricelist](moduleID, priceListID, startDate, finishDate, price, [rollerID], isStandAlone, [extraChargeFirstRoller], [extraChargeSecondRoller], [extraChargeLastRoller])
	VALUES(@moduleID, @priceListID, @startDate, @finishDate, @price, @rollerID, @isStandAlone, @extraChargeFirstRoller, @extraChargeSecondRoller, @extraChargeLastRoller)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @modulePriceListID = SCOPE_IDENTITY()
	
	IF (@actionName = 'Clone')
	BEGIN
		INSERT INTO ModuleTariff
			SELECT DISTINCT
				@modulePriceListID, t2.[tariffID]
			FROM 
				ModuleTariff mt
				INNER JOIN [Tariff] t ON mt.[tariffID] = t.[tariffID]
				INNER JOIN [Pricelist] pl ON t.[pricelistID] = pl.[pricelistID]
				LEFT JOIN [Pricelist] pl2 ON pl.[massmediaID] = pl2.[massmediaID] 
					AND pl2.[pricelistID] = @priceListID
					AND pl2.[startDate] <= @startDate
					AND pl2.[finishDate] >= @finishDate
				LEFT JOIN [Tariff] t2 ON pl2.[pricelistID] = t2.[pricelistID]
					AND t.[time] = t2.[time]
					AND t.[monday] = t2.[monday]
					AND t.[tuesday] = t2.[tuesday]
					AND t.[wednesday] = t2.[wednesday]
					AND t.[thursday] = t2.[thursday]
					AND t.[sunday] = t2.[sunday]
					AND t.[saturday] = t2.[saturday]
			WHERE mt.modulePriceListID = @sourceModulePriceListID
			GROUP BY t2.[tariffID]
			HAVING t2.[tariffID] IS NOT NULL 
	END
	
	EXEC ModulePriceLists @modulePricelistID = @modulePricelistID
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [ModulePricelist] WHERE modulePricelistID = @modulePricelistID
ELSE IF @actionName = 'UpdateItem' BEGIN

	-- Forbidden to change price if pricelist is in use
	Declare @oldPrice money, @oldStartDate datetime, @oldFinishDate datetime
	SELECT @oldPrice = price, @oldStartDate = startDate, @oldFinishDate = finishDate FROM ModulePricelist WHERE modulePricelistID = @modulePricelistID
	IF @price <> @oldPrice and exists (
		SELECT * FROM ModuleIssue WHERE modulePricelistID = @modulePricelistID
	) 
	BEGIN
		RAISERROR('ModulePricelistInUse', 16, 1)
		RETURN
	end
	
	if (@oldStartDate <> @startDate or @oldFinishDate <> @finishDate)
		and exists(select * 
					from ModuleIssue mi 
					where mi.modulePricelistID = @modulePriceListID
						and (mi.issueDate < @startDate or mi.issueDate > @finishDate) )
	begin 
		raiserror('ModulePricelistInUse', 16, 1)
		return
	end 
	
	if exists(select * from PackModuleContent pmc 
				inner join PackModulePriceList pmpl on pmc.pricelistID = pmpl.priceListID
				where pmc.modulePriceListID = @modulePriceListID 
					and (pmpl.startDate < @startDate or pmpl.finishDate > @finishDate))
	begin
		raiserror('ModulePricelistUserInPackModule', 16, 1)
		return
	end 
		
	UPDATE
		[ModulePriceList]
	SET 
		[startDate] = @startDate,
		[finishDate] = @finishDate,
		[price] = @price,
		rollerID = @rollerID,
		isStandAlone = @isStandAlone,
		extraChargeFirstRoller = @extraChargeFirstRoller,
		extraChargeSecondRoller = @extraChargeSecondRoller,
		extraChargeLastRoller = @extraChargeLastRoller
	WHERE
		modulePricelistID = @modulePricelistID
		
	EXEC ModulePriceLists @modulePricelistID = @modulePricelistID		
	
END







