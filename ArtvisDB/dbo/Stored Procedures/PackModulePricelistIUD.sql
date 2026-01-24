



CREATE    PROCEDURE [dbo].[PackModulePricelistIUD]
(
@pricelistID smallint OUT,
@sourcePricelistID SMALLINT = NULL,
@packModuleID smallint = NULL,
@startDate datetime = NULL,
@finishDate datetime = NULL,
@price MONEY = NULL,
@extraChargeFirstRoller TINYINT,
@extraChargeSecondRoller TINYINT,
@extraChargeLastRoller TINYINT,
@rollerID INT = NULL, 
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @msg Varchar(4000)

IF @actionName IN('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('StartFinishDateError', 16, 1)
		RETURN
	END

	IF EXISTS(
		SELECT * FROM PackModulePricelist	
		WHERE 
			(@startDate between startDate and finishDate Or 
			@finishDate between startDate and finishDate) and
			(IsNull(@pricelistID, 0) <> pricelistID OR @actionName <> 'UpdateItem') and
			packModuleID = @packModuleID
		) BEGIN
		RAISERROR('PLPeriodIntersection', 16, 1)
		RETURN
	END
END

if @actionName = 'Clone'
begin 
	declare @sourceCount int, @cloneCount int 
	select @sourceCount = count(*) from PackModuleContent where pricelistID = @sourcePricelistID
		
	SELECT @cloneCount = count(*)
	FROM
		[PackModuleContent] pmc
		inner join ModulePriceList mpl on pmc.moduleID = mpl.moduleID
			and mpl.startDate <= @startDate and mpl.finishDate >= @finishDate
	WHERE 
		pmc.[pricelistID] = @sourcePricelistID
	
	if @sourceCount <> @cloneCount
	begin 
		RAISERROR('CannotClonePackModulePriceList', 16, 1)
		RETURN
	end 
end 

IF @actionName IN('AddItem', 'Clone') BEGIN
	INSERT INTO [PackModulePricelist]
		(packModuleID, startDate, finishDate, [price], [extraChargeFirstRoller],
			[extraChargeSecondRoller],[extraChargeLastRoller], [rollerID])
	VALUES(@packModuleID, @startDate, @finishDate, @price, @extraChargeFirstRoller,
			@extraChargeSecondRoller, @extraChargeLastRoller, @rollerID)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @pricelistID = SCOPE_IDENTITY()
	
	if (@actionName = 'Clone') begin
		INSERT INTO [PackModuleContent] ([pricelistID],[moduleID],[modulePriceListID]) 
		SELECT @pricelistID, pmc.[moduleID], mpl.modulePriceListID
		FROM
			[PackModuleContent] pmc
			inner join ModulePriceList mpl on pmc.moduleID = mpl.moduleID
				and mpl.startDate <= @startDate and mpl.finishDate >= @finishDate
		WHERE 
			pmc.[pricelistID] = @sourcePricelistID
	end
	
	EXEC PackModulePricelists @pricelistID = @pricelistID 
END
ELSE IF @actionName = 'DeleteItem' 
	DELETE FROM [PackModulePricelist] WHERE pricelistID = @pricelistID
ELSE IF @actionName = 'UpdateItem' BEGIN
	IF EXISTS(SELECT 1 FROM [dbo].[PackModuleIssue] WHERE pricelistID = @pricelistID) BEGIN
		-- Если прайс-лист используется в рекламной кампании, то менять можно только дату окончания
		Declare 
			@priceCurrent MONEY,
			@extraChargeFirstRollerCurrent TINYINT,
			@extraChargeSecondRollerCurrent TINYINT,
			@extraChargeLastRollerCurrent TINYINT,
			@rollerIDCurrent INT

		Select 
			@priceCurrent = price,
			@extraChargeFirstRollerCurrent = extraChargeFirstRoller,
			@extraChargeSecondRollerCurrent = extraChargeSecondRoller,
			@extraChargeLastRollerCurrent = extraChargeLastRoller,
			@rollerIDCurrent = rollerID
		From 
			[PackModulePricelist]
		WHERE		
			pricelistID = @pricelistID

		If	@priceCurrent != @price Or
			@extraChargeFirstRollerCurrent != @extraChargeFirstRoller Or
			@extraChargeSecondRollerCurrent != @extraChargeSecondRoller Or
			@extraChargeLastRollerCurrent != @extraChargeLastRoller Or
			IsNull(@rollerIDCurrent, 0) != IsNull(@rollerID, 0) Or
			exists(select * 
					from PackModuleIssue mi 
					where mi.pricelistID = @priceListID
						and (mi.issueDate < @startDate or mi.issueDate > @finishDate) )
	begin 
		raiserror('ModulePricelistInUse', 16, 1)
		return
	end 
			Begin
			RAISERROR('PackModulePriceListInUse', 16, 1)
			RETURN
		End
	END

	UPDATE	
		[PackModulePricelist]
	SET			
		startDate = @startDate, 
		finishDate = @finishDate,
		[price] = @price,
		[extraChargeFirstRoller] = @extraChargeFirstRoller,
		[extraChargeSecondRoller] = @extraChargeSecondRoller,			
		[extraChargeLastRoller] = @extraChargeLastRoller,
		[rollerID] = @rollerID
	WHERE		
		pricelistID = @pricelistID

	EXEC PackModulePricelists @pricelistID = @pricelistID 
END









