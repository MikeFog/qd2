CREATE PROCEDURE dbo.RegistryDelete
(
@id int
)
AS
SET NOCOUNT ON
DELETE FROM registry WHERE id = @id
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[RegistryDelete] TO PUBLIC
    AS [dbo];

