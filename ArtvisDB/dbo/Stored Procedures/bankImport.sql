create PROCEDURE [dbo].[bankImport]
	@bankId int = NULL OUT,
	@bik VARCHAR(10),
	@name VARCHAR(255),
	@corAccount VARCHAR(32)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @count INT
	
	SET @count = (SELECT COUNT(*) FROM [Bank] WHERE [Bank].[name] = @name)
    
    IF (@count = 1) 
    BEGIN
		UPDATE [Bank] SET 
			[bik] = @bik,
			[corAccount] = @corAccount
		WHERE
			name = @name

		select @bankId = bankId from Bank where name = @name
	END
	else
	begin if (@count = 0)
		INSERT INTO [Bank] (
			[name],
			[bik],
			[corAccount]
		) VALUES ( 
			@name,
			@bik,
			@corAccount 
		) 

		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @bankId = SCOPE_IDENTITY()
	END
	
END
