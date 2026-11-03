

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
@director nvarchar(32) = NULL,
@bookkeeper nvarchar(32) = NULL,
@prefix nvarchar(64) = NULL,
@isActive tinyint = 1,
@directorSignature nvarchar(255) = NULL,
@bookkeeperSignature nvarchar(255) = NULL,
@fullPrefix nvarchar(256) = NULL,
@reportString nvarchar(256) = NULL,
@registration nvarchar(256) = NULL,
@actionName varchar(32),
@withResultset bit = 1,
@painting image = null,
@reportPlace nvarchar(64),
@path2proposalTemplate varchar(128) = null
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON

-- Расчётный счёт — счёт агентства в выбранном банке (AgencyBank), без банка не хранится.
SET @account = NULLIF(LTRIM(RTRIM(@account)), '')
IF @actionName IN ('AddItem', 'UpdateItem') AND @bankID IS NOT NULL AND @account IS NULL
begin
	raiserror('AgencyBankAccountRequired', 16, 1)
	return
end

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Agency](name, address, phone, fax, email, inn, okpo, okonh, bankID, director,
		bookkeeper, prefix, isActive, directorSignature, bookkeeperSignature, fullPrefix, reportString, 
		registration, kpp, egrn, okved, painting, reportPlace, path2proposalTemplate)
	VALUES(@name, @address, @phone, @fax, @email, @inn, @okpo, @okonh, @bankID, @director,
		@bookkeeper, @prefix, @isActive, @directorSignature, @bookkeeperSignature, @fullPrefix, @reportString, 
		@registration, @kpp, @egrn, @okved, @painting, @reportPlace, @path2proposalTemplate)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @agencyID = SCOPE_IDENTITY()

	IF @bankID IS NOT NULL
		INSERT INTO [AgencyBank](agencyID, bankID, account) VALUES(@agencyID, @bankID, @account)

	IF @withResultset = 1 EXEC agencies @agencyID = @agencyID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
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
		reportPlace = @reportPlace,
		path2proposalTemplate = @path2proposalTemplate
	WHERE
		AgencyID = @AgencyID

	-- счета в других банках остаются: вернут банк — вернётся и счёт
	IF @bankID IS NOT NULL
	BEGIN
		UPDATE [AgencyBank] SET account = @account WHERE agencyID = @agencyID AND bankID = @bankID
		IF @@ROWCOUNT = 0
			INSERT INTO [AgencyBank](agencyID, bankID, account) VALUES(@agencyID, @bankID, @account)
	END

	IF @withResultset = 1 EXEC agencies @agencyID = @agencyID
END
