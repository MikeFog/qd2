CREATE PROC [dbo].[PassportRetrieve]
(
@codeName varchar(32) = null
)
as

SET NOCOUNT ON
SELECT passport,codeName FROM iPassport WHERE codeName = coalesce(@codeName, codeName)

