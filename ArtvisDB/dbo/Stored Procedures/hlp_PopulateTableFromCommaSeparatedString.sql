
CREATE  PROC [dbo].[hlp_PopulateTableFromCommaSeparatedString]
(
@tableName as varchar(64),
@string as varchar(1024)
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on
Declare	@start int, @start2 int, @sql varchar(1024)
Set @start = 1
Set @start2 = 1
While @start2 > 0 Begin
	Set @start2 = CharIndex(',', @string, @start)
	If @start2 > 0 Begin
		Set @sql = 'Insert Into ' + @tableName + 
			' Values(Substring(''' + @string +''', ' + LTRIM(@start) +',' + LTRIM(@start2 - @start) +'))'

		Exec(@sql)
		Set @start = @start2 + 1
	End
End


