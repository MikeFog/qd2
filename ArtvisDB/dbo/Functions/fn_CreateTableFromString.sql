CREATE FUNCTION [dbo].[fn_CreateTableFromString]
(
@IDString varchar(8000)
)
RETURNS @IDTable TABLE (ID int, INDEX ID CLUSTERED (ID))
AS
BEGIN

DECLARE @startLocation int,	@position int
SELECT @startLocation = 0, @position = -1

WHILE @position <> 0 BEGIN
	SET @position = CHARINDEX(',', @IDString, @startLocation)
	IF @position <> 0 BEGIN
		INSERT INTO @IDTable VALUES(SUBSTRING(@IDString, @startLocation, @position - @startLocation))
		SET @startLocation = @position + 1
	END
END

RETURN
END
