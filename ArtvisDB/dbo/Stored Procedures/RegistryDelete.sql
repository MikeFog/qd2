CREATE PROCEDURE dbo.RegistryDelete
(
@id int
)
AS
SET NOCOUNT ON
DELETE FROM registry WHERE id = @id

