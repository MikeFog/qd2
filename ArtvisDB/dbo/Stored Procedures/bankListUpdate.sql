CREATE PROCEDURE [dbo].[bankListUpdate]
	@bik VARCHAR(10),
	@name VARCHAR(255),
	@corAccount VARCHAR(32)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @count INT
	
	SET @count = (SELECT COUNT(*) FROM [Bank] WHERE [Bank].[bik] = @bik)
    
    IF (@count = 1) 
    BEGIN
		UPDATE [Bank] SET 
			[name] = @name,
			[corAccount] = @corAccount
		WHERE
			bik = @bik
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
	END
	
END
