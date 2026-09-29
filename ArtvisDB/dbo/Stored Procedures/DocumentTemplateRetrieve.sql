-- Файл Word-шаблона: по ID версии либо действующий на дату документа
-- (@agencyID, @reportTypeID, @date). Нет шаблона — пустой результат. docs/tasks/web-reports.md §8.
CREATE PROCEDURE [dbo].[DocumentTemplateRetrieve]
(
    @documentTemplateID INT      = NULL,
    @agencyID           SMALLINT = NULL,
    @reportTypeID       SMALLINT = NULL,
    @date               DATETIME = NULL
)
AS
SET NOCOUNT ON;

SELECT TOP 1 t.[documentTemplateID], t.[agencyID], t.[reportTypeID], t.[startDate], t.[fileName], t.[content]
FROM   [dbo].[DocumentTemplate] t
WHERE  (@documentTemplateID IS NOT NULL AND t.[documentTemplateID] = @documentTemplateID)
       OR (@documentTemplateID IS NULL
           AND t.[agencyID] = @agencyID AND t.[reportTypeID] = @reportTypeID
           AND t.[startDate] <= CAST(@date AS DATE))
ORDER BY t.[startDate] DESC, t.[documentTemplateID] DESC;
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DocumentTemplateRetrieve] TO PUBLIC
    AS [dbo];
