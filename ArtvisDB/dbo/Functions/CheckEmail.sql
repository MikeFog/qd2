CREATE   FUNCTION [dbo].[CheckEmail]
   (
	@Email VARCHAR(100) --Адрес электронной почты
   )
   RETURNS BIT
   AS
   BEGIN

	DECLARE @Result BIT;
	SET @Result = 1 -- NULL считаем разрешенным адресом
	
	--Начинаем проверку, только если есть данные
	IF @Email IS NOT NULL
	BEGIN	
		IF @Email LIKE '_%@__%.__%'
			SET @Result = 1;
		ELSE
			SET @Result = 0;
	END
	
	RETURN @Result;
   END