










CREATE            PROCEDURE [dbo].[StudioIUD]
(
@studioID smallint = NULL,
@prefix nvarchar(16) = NULL,
@name nvarchar(64) = NULL,
@address nvarchar(128) = NULL,
@phone varchar(64) = NULL,
@fax varchar(64) = NULL,
@email varchar(256) = NULL,
@bankID smallint = NULL,
@account varchar(32) = NULL,
@inn varchar(32) = NULL,
@kpp varchar(16) = null,
@okonh varchar(32) = NULL,
@okpo varchar(32) = NULL,
@egrn varchar(16) = null,
@okved varchar(16) = NULL,
@director varchar(32) = NULL,
@bookkeeper varchar(32) = NULL,
@fullPrefix varchar(256) = NULL,
@reportString varchar(256) = NULL,
@registration varchar(256) = NULL,
@actionName varchar(32),
@isActive bit = 0
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	-- First create agency
	Exec AgencyIUD @name = @name, @address = @address, @phone = @phone, @fax = @fax, @email = @email,
		@account = @account, @inn = @inn, @okpo = @okpo, @okonh = @okonh, @kpp = @kpp, @egrn = @egrn, @okved = @okved,
		@bankID = @bankID, @prefix = @prefix, @director = @director, @bookkeeper = @bookkeeper, @fullPrefix = @fullPrefix, @reportString = @reportString, @registration = @registration,
		@actionName = @actionName, @withResultset = 0, @agencyID = @studioID out

	INSERT INTO [Studio](StudioID, IsActive)
	VALUES(@StudioId, @IsActive)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	EXEC Studios @studioID = @studioID
	END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM [Studio] WHERE studioID = @studioID
	DELETE FROM [Agency] WHERE agencyID = @studioID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[Studio]
	SET			
		isActive = @isActive
	WHERE		
		studioID = @studioID

	Exec AgencyIUD @agencyID = @studioID,
		@name = @name, @address = @address, @phone = @phone, @fax = @fax, @email = @email,
		@account = @account, @inn = @inn, @okpo = @okpo, @okonh = @okonh, @kpp = @kpp, @egrn = @egrn, @okved = @okved,
		@bankID = @bankID, @prefix = @prefix, @director = @director, @bookkeeper = @bookkeeper, @fullPrefix = @fullPrefix, @reportString = @reportString, @registration = @registration,
		@actionName = @actionName, @withResultset = 0

	EXEC Studios @studioID = @studioID
	END















