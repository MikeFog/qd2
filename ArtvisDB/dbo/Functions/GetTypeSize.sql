
CREATE FUNCTION [dbo].[GetTypeSize]
(
@type AS VARCHAR(50),
@size AS INTEGER
)
RETURNS INTEGER
AS
BEGIN
	-- Declare variable
	DECLARE @new_size AS INTEGER

	IF (@size IS NULL)
	BEGIN
		SET @new_size = (CASE LOWER(@type)
					WHEN 'uniqueidentifier' THEN 36
					WHEN 'tinyint' THEN 1
					WHEN 'smallint' THEN 2
					WHEN 'int' THEN 4
					WHEN 'smalldatetime' THEN 4
					WHEN 'real' THEN 4
					WHEN 'money' THEN 4
					WHEN 'datetime' THEN 8
					WHEN 'float' THEN 4
					WHEN 'sql_variant' THEN 8016
					WHEN 'bit' THEN 1
					WHEN 'decimal' THEN 5
					WHEN 'numeric' THEN 5
					WHEN 'smallmoney' THEN 4
					WHEN 'bigint' THEN 8
					WHEN 'varbinary' THEN 1
					WHEN 'varchar' THEN 1
					WHEN 'binary' THEN 1
					WHEN 'char' THEN 1
					WHEN 'timestamp' THEN 8
					WHEN 'nvarchar' THEN 1
					WHEN 'nchar' THEN 1
					WHEN 'sysname' THEN 128
					ELSE 1
				    END)
	END
	ELSE SET @new_size = @size
	
	RETURN @new_size
END









