-- Версии Word-шаблонов документов без самих файлов (экран «Шаблоны документов»).
-- isCurrent = 1 у версии, действующей сегодня. docs/tasks/web-reports.md §8.
CREATE PROCEDURE [dbo].[DocumentTemplates]
(
    @agencyID     SMALLINT = NULL,
    @reportTypeID SMALLINT = NULL
)
AS
SET NOCOUNT ON;

SELECT t.[documentTemplateID], t.[agencyID], a.[name] AS agencyName,
       t.[reportTypeID], rt.[name] AS reportTypeName,
       t.[startDate], t.[fileName], t.[comment], DATALENGTH(t.[content]) AS size,
       t.[createdBy], u.[userName] AS createdByName,
       t.[createDate],
       CAST(CASE WHEN t.[documentTemplateID] = cur.[documentTemplateID] THEN 1 ELSE 0 END AS BIT) AS isCurrent
FROM   [dbo].[DocumentTemplate] t
       JOIN [dbo].[Agency] a ON a.[agencyID] = t.[agencyID]
       JOIN [dbo].[ReportType] rt ON rt.[reportTypeID] = t.[reportTypeID]
       LEFT JOIN [dbo].[User] u ON u.[userID] = t.[createdBy]
       OUTER APPLY (
           SELECT TOP 1 c.[documentTemplateID]
           FROM   [dbo].[DocumentTemplate] c
           WHERE  c.[agencyID] = t.[agencyID] AND c.[reportTypeID] = t.[reportTypeID]
                  AND c.[startDate] <= CAST(GETDATE() AS DATE)
           ORDER BY c.[startDate] DESC, c.[documentTemplateID] DESC
       ) cur
WHERE  (@agencyID IS NULL OR t.[agencyID] = @agencyID)
       AND (@reportTypeID IS NULL OR t.[reportTypeID] = @reportTypeID)
ORDER BY a.[name], t.[reportTypeID], t.[startDate] DESC, t.[documentTemplateID] DESC;
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DocumentTemplates] TO PUBLIC
    AS [dbo];
