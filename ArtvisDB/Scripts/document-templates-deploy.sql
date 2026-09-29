/*
    ДЕПЛОЙ: хранилище Word-шаблонов документов (документы из шаблонов, этап 2).
    Задача: docs/tasks/web-reports.md §8.

    ЧТО ДЕЛАЕТ
      1. Таблица DocumentTemplate: шаблон на агентство × вид документа (ReportType)
         × «действует с», файл .docx, кто и когда загрузил. Версии не перезаписываются.
      2. Процедуры DocumentTemplates (список версий), DocumentTemplateRetrieve (файл по ID
         или действующий на дату), DocumentTemplateIns (новая версия), DocumentTemplateDel.

    КЛИЕНТ          десктоп не затронут (печатает через Crystal). Веб читает таблицу с этапа 5.
    ИДЕМПОТЕНТНОСТЬ повторный запуск безопасен: таблица создаётся, только если её нет.
    ОТКАТ           DROP PROCEDURE dbo.DocumentTemplates, dbo.DocumentTemplateRetrieve,
                    dbo.DocumentTemplateIns, dbo.DocumentTemplateDel; DROP TABLE dbo.DocumentTemplate.
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
-- sqlcmd по умолчанию создаёт процедуры с QUOTED_IDENTIFIER OFF — задаём явно
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.DocumentTemplate') IS NULL
BEGIN
    CREATE TABLE [dbo].[DocumentTemplate] (
        [documentTemplateID] INT             IDENTITY (1, 1) NOT NULL,
        [agencyID]           SMALLINT        NOT NULL,
        [reportTypeID]       SMALLINT        NOT NULL,
        [startDate]          DATE            NOT NULL,
        [content]            VARBINARY (MAX) NOT NULL,
        [fileName]           NVARCHAR (255)  NOT NULL,
        [comment]            NVARCHAR (255)  NULL,
        [createdBy]          SMALLINT        NOT NULL,
        [createDate]         DATETIME        CONSTRAINT [DF_DocumentTemplate_createDate] DEFAULT (getdate()) NOT NULL,
        CONSTRAINT [PK_DocumentTemplate] PRIMARY KEY CLUSTERED ([documentTemplateID] ASC),
        CONSTRAINT [FK_DocumentTemplate_Agency] FOREIGN KEY ([agencyID]) REFERENCES [dbo].[Agency] ([agencyID]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentTemplate_ReportType] FOREIGN KEY ([reportTypeID]) REFERENCES [dbo].[ReportType] ([reportTypeID]),
        CONSTRAINT [FK_DocumentTemplate_User] FOREIGN KEY ([createdBy]) REFERENCES [dbo].[User] ([userID])
    );

    CREATE NONCLUSTERED INDEX [IX_DocumentTemplate_Agency_Type]
        ON [dbo].[DocumentTemplate]([agencyID] ASC, [reportTypeID] ASC, [startDate] DESC);
    PRINT 'Создана таблица DocumentTemplate';
END
GO

-- Версии Word-шаблонов документов без самих файлов (экран «Шаблоны документов»).
-- isCurrent = 1 у версии, действующей сегодня. docs/tasks/web-reports.md §8.
CREATE OR ALTER PROCEDURE [dbo].[DocumentTemplates]
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
GRANT EXECUTE ON OBJECT::[dbo].[DocumentTemplates] TO PUBLIC AS [dbo];
GO

-- Файл Word-шаблона: по ID версии либо действующий на дату документа
-- (@agencyID, @reportTypeID, @date). Нет шаблона — пустой результат. docs/tasks/web-reports.md §8.
CREATE OR ALTER PROCEDURE [dbo].[DocumentTemplateRetrieve]
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
GRANT EXECUTE ON OBJECT::[dbo].[DocumentTemplateRetrieve] TO PUBLIC AS [dbo];
GO

-- Новая версия Word-шаблона. Шаблон проверен в вебе до вызова (DocxTemplate.Validate).
-- Возвращает ID версии. docs/tasks/web-reports.md §8.
CREATE OR ALTER PROCEDURE [dbo].[DocumentTemplateIns]
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
GRANT EXECUTE ON OBJECT::[dbo].[DocumentTemplateIns] TO PUBLIC AS [dbo];
GO

-- Удаление версии Word-шаблона (ошибочная загрузка). docs/tasks/web-reports.md §8.
CREATE OR ALTER PROCEDURE [dbo].[DocumentTemplateDel]
(
    @documentTemplateID INT
)
AS
SET NOCOUNT ON;

DELETE FROM [dbo].[DocumentTemplate]
WHERE  [documentTemplateID] = @documentTemplateID;
GO
GRANT EXECUTE ON OBJECT::[dbo].[DocumentTemplateDel] TO PUBLIC AS [dbo];
GO
