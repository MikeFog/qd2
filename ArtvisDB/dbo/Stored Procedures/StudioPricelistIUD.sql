










CREATE            PROCEDURE [dbo].[StudioPricelistIUD]
(
@pricelistID smallint = NULL,
@studioID smallint = NULL,
@startDate datetime = NULL,
@finishDate datetime = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS

SET NOCOUNT ON
-- Validate parameters
IF @actionName IN('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('StartFinishDateError', 16, 1)
		RETURN
	END

	IF EXISTS(
		SELECT * FROM StudioPricelist	
		WHERE 
			(@startDate between startDate and finishDate Or 
			@finishDate between startDate and finishDate) and
			(IsNull(@pricelistID, 0) <> pricelistID OR @actionName <> 'UpdateItem') and
			studioID = @studioID
		) BEGIN
		RAISERROR('PLPeriodIntersection', 16, 1)
		RETURN
	END
END
IF @actionName IN ('AddItem', 'Clone') BEGIN
	INSERT INTO [StudioPricelist](studioID, startDate, finishDate)
	VALUES(@studioID, @startDate, @finishDate)

	DECLARE @basePricelistID smallint
	SET	@basePricelistID = @pricelistID
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @pricelistID = SCOPE_IDENTITY()

	IF @actionName = 'Clone' Begin
		-- Clone tariff list
		INSERT INTO [StudioTariff]([pricelistID], [rolStyleID], [summa], [tariffTypeID])
		SELECT @pricelistID, [rolStyleID], [summa], [tariffTypeID]
		FROM StudioTariff WHERE pricelistID = @basePricelistID

	End
	EXEC StudioPricelists @pricelistID = @pricelistID
END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [StudioPricelist] WHERE pricelistID = @pricelistID
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[StudioPricelist]
	SET			
		studioID = @studioID, 
		startDate = @startDate, 
		finishDate = @finishDate
	WHERE		
		pricelistID = @pricelistID

	EXEC StudioPricelists @pricelistID = @pricelistID
END









