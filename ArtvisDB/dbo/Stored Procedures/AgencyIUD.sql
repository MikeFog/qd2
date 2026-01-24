

CREATE           PROCEDURE [dbo].[AgencyIUD]
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
@director varchar(32) = NULL,
@bookkeeper varchar(32) = NULL,
@prefix varchar(64) = NULL,
@isActive tinyint = 1,
@directorSignature varchar(255) = NULL,
@bookkeeperSignature varchar(255) = NULL,
@fullPrefix varchar(256) = NULL,
@reportString varchar(256) = NULL,
@registration varchar(256) = NULL,
@actionName varchar(32),
@withResultset bit = 1,
@painting image = null,
@reportPlace varchar(64)
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Agency](name, address, phone, fax, email, account, inn, okpo, okonh, bankID, director, bookkeeper, prefix, isActive, directorSignature, bookkeeperSignature, fullPrefix, reportString, registration, kpp, egrn, okved, painting, reportPlace)
	VALUES(@name, @address, @phone, @fax, @email, @account, @inn, @okpo, @okonh, @bankID, @director, @bookkeeper, @prefix, @isActive, @directorSignature, @bookkeeperSignature, @fullPrefix, @reportString, @registration, @kpp, @egrn, @okved, @painting, @reportPlace)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @agencyID = SCOPE_IDENTITY()	
	IF @withResultset = 1 EXEC agencies @agencyID = @agencyID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	DELETE FROM [Studio] WHERE StudioID = @agencyID
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
		reportPlace = @reportPlace
	WHERE		
		AgencyID = @AgencyID

	IF @withResultset = 1 EXEC agencies @agencyID = @agencyID
END
