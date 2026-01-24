CREATE PROCEDURE [dbo].[SponsorTariffIUD]
(
@tariffID int = NULL,
@pricelistID smallint = NULL,
@time smalldatetime = NULL,
@monday tinyint = NULL,
@tuesday tinyint = NULL,
@wednesday tinyint = NULL,
@thursday tinyint = NULL,
@friday tinyint = NULL,
@saturday tinyint = NULL,
@sunday tinyint = NULL,
@price money = NULL,
@duration smallint = NULL,
@comment nvarchar(32) = NULL,
@isAlive BIT = 0,
@needExt BIT = 1,
@needInJingle BIT = 1,
@needOutJingle BIT = 1,
@suffix NVARCHAR(32) = NULL,
@path NVARCHAR(255) = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
-- Check, may be tariff with such attributes has been already created
IF @actionName In ('AddItem', 'UpdateItem') And
	Exists(
		Select	* From SponsorTariff
						Where	tariffID <> IsNull(@tariffID, 0) and
						pricelistId = @pricelistID and
						[time] = @time and 
						(
 					   (monday = @monday and @monday = 1) or
						(tuesday = @tuesday and @tuesday = 1) or
						(wednesday = @Wednesday and @wednesday = 1) or
						(thursday = @thursday and @thursday = 1) or
						(friday = @friday and @friday = 1) or
						(saturday = @saturday and @saturday = 1) or
						(sunday = @sunday and @sunday = 1) 
						)
	)
	BEGIN
	RAISERROR('TariffAlreadyExists', 16, 1)
	RETURN
	END	
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [SponsorTariff](pricelistID, time, monday, tuesday, wednesday, thursday, friday, saturday, sunday, price, duration, comment, isAlive, needExt, needInJingle, needOutJingle, suffix, path)
	VALUES(@pricelistID, @time, @monday, @tuesday, @wednesday, @thursday, @friday, @saturday, @sunday, @price, @duration, @comment, @isAlive, @needExt, @needInJingle, @needOutJingle, @suffix, @path)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @tariffID = SCOPE_IDENTITY()
	Exec SponsorTariffList @tariffID = @tariffID
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [SponsorTariff] WHERE tariffID = @tariffID
ELSE IF @actionName = 'UpdateItem' BEGIN
	-- Check if Tariff already in use and attributes can't be changed
	Declare	
		@oldTime datetime, 
		@oldPrice money,
		@oldMonday bit,
		@oldTuesday bit,
		@oldWednesday bit,
		@oldThursday bit,
		@oldFriday bit,
		@oldSaturday bit,
		@oldSunday bit

	SELECT 
		@oldTime = time, 
		@oldPrice = price,
		@oldMonday = monday,
		@oldTuesday = tuesday,
		@oldWednesday = wednesday,
		@oldThursday = thursday,
		@oldFriday = friday,
		@oldSaturday = saturday,
		@oldSunday = sunday
	FROM
		[SponsorTariff]
	WHERE		
		tariffID = @tariffID		
	
	IF (@oldTime <> @time Or @oldPrice <> @price Or @oldMonday <> @monday Or
		@oldTuesday <> @tuesday	Or @oldWednesday <> @wednesday Or
		@oldThursday <> @thursday Or @oldFriday <> @friday Or 
		@oldSaturday <> @saturday Or @oldSunday <> @sunday) And
		EXISTS(SELECT * FROM ProgramIssue WHERE tariffID = @tariffID)
		Begin
		RAISERROR('SponsorTariffInUse', 16, 1)
		RETURN	
		End

	UPDATE	
		[SponsorTariff]
	SET			
		[time] = @time, 
		monday = @monday, 
		tuesday = @tuesday, 
		wednesday = @wednesday, 
		thursday = @thursday, 
		friday = @friday, 
		saturday = @saturday, 
		sunday = @sunday, 
		price = @price, 
		duration = @duration, 
		comment = @comment,
		isAlive = @isAlive,
		needExt = @needExt, 
		needInJingle = @needInJingle, 
		needOutJingle = @needOutJingle, 
		suffix = @suffix, 
		path = @path
	WHERE		
		tariffID = @tariffID

	Exec SponsorTariffList @tariffID = @tariffID
	END
