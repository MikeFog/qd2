


CREATE    PROCEDURE [dbo].[StudioTariffIUD]
(
@tariffID int = NULL,
@pricelistID smallint = NULL,
@rolStyleID smallint = NULL,
@summa money = NULL,
@tariffTypeID smallint = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [StudioTariff](pricelistID, rolStyleID, summa, tariffTypeID)
	VALUES(@pricelistID, @rolStyleID, @summa, @tariffTypeID)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @tariffID = SCOPE_IDENTITY()
	EXEC StudioTariffList @tariffID = @tariffID
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [StudioTariff] WHERE tariffID = @tariffID
ELSE IF @actionName = 'UpdateItem' BEGIN
	-- Check which colums are really updated
	Declare @oldSumma money, @oldRolStyleID smallint, @oldTariffTypeID smallint

	SELECT
		@oldSumma = summa,
		@oldRolStyleID = rolStyleID,
		@oldTariffTypeID = tariffTypeID
	FROM 
		[StudioTariff]
	WHERE		
		tariffID = @tariffID	

	If (@oldSumma <> @summa Or @oldRolStyleID <> @rolStyleID Or @oldTariffTypeID <> @tariffTypeID)
		And Exists (Select * From StudioOrder Where tariffID = @tariffID)
		BEGIN
		RAISERROR('StudioTariffInUse', 16, 1)
		RETURN
	END

	UPDATE	
		[StudioTariff]
	SET			
		pricelistID = @pricelistID, 
		rolStyleID = @rolStyleID, 
		summa = @summa, 
		tariffTypeID = @tariffTypeID
	WHERE		
		tariffID = @tariffID

	EXEC StudioTariffList @tariffID = @tariffID
	END






