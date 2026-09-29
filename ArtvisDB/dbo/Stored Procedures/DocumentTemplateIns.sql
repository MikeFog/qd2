-- Новая версия Word-шаблона. Шаблон проверен в вебе до вызова (DocxTemplate.Validate).
-- Возвращает ID версии. docs/tasks/web-reports.md §8.
CREATE PROCEDURE [dbo].[DocumentTemplateIns]
(
    @agencyID     SMALLINT,
    @reportTypeID SMALLINT,
    @startDate    DATETIME,
    @content      VARBINARY(MAX),
    @fileName     NVARCHAR(255),
    @comment      NVARCHAR(255) = NULL,
    @loggedUserId SMALLINT
)
AS
SET NOCOUNT ON;

INSERT INTO [dbo].[DocumentTemplate]
       ([agencyID], [reportTypeID], [startDate], [content], [fileName], [comment], [createdBy])
VALUES (@agencyID, @reportTypeID, CAST(@startDate AS DATE), @content, @fileName, NULLIF(LTRIM(RTRIM(@comment)), ''), @loggedUserId);

SELECT CAST(SCOPE_IDENTITY() AS INT) AS documentTemplateID;
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DocumentTemplateIns] TO PUBLIC
    AS [dbo];
