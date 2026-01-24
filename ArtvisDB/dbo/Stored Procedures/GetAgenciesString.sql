CREATE PROCEDURE [dbo].[GetAgenciesString]
(
	@actionID int, 
	@massmediaIDString VARCHAR(8000) = null,
	@agencies nvarchar(max) out
)	
AS
BEGIN
	SET NOCOUNT ON;

	CREATE TABLE #mm(massmediaID int)
	INSERT INTO #mm
	SELECT * FROM fn_CreateTableFromString(@massmediaIDString)

	set @agencies = ''
	select @agencies = @agencies + case when @agencies != '' then ', ' else '' end + a.prefix + space(1) + a.[name] from Agency a where a.agencyID in (
		select distinct c.agencyID
		from 
			Campaign c 
			inner join Issue i on i.campaignID = c.campaignID
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			inner join [#mm] mm on tw.massmediaID = mm.massmediaID
		where 
			c.actionID = @actionID)
END
