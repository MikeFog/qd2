-- Удаление версии Word-шаблона (ошибочная загрузка). docs/tasks/web-reports.md §8.
CREATE PROCEDURE [dbo].[DocumentTemplateDel]
(
    @documentTemplateID INT
)
AS
SET NOCOUNT ON;

DELETE FROM [dbo].[DocumentTemplate]
WHERE  [documentTemplateID] = @documentTemplateID;
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DocumentTemplateDel] TO PUBLIC
    AS [dbo];
