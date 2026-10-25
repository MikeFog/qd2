CREATE PROCEDURE [dbo].[sp_CheckFirmINN]
	(
		@inn varchar(20),
		@firmID int = null 
	)
AS
BEGIN
	SET NOCOUNT ON;

	-- пробелы и знаки препинания не считаются (' 770' = '770', '333-555' = '333555'), как в FirmIUD
	set @inn = dbo.fn_FirmKey(@inn);

	select [name]
	from Firm
	where (((@inn is null or @inn = space(0)) and inn is null)
		or (@inn is not null and @inn != space(0) and dbo.fn_FirmKey(inn) like @inn + '%')) and (@firmID is null or firmID <> @firmID)
END
