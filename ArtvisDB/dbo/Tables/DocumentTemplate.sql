-- Word-шаблоны документов для клиента (docs/tasks/web-reports.md §6.0, §8 этап 2).
-- Свой шаблон у каждого агентства на каждый вид документа (ReportType), с версиями:
-- действует версия с наибольшей startDate не позже даты документа, при равных датах —
-- загруженная последней. Старые версии не удаляются при загрузке новой: перепечатка
-- документа прошлой даты берёт текст, действовавший тогда. Десктоп таблицу не читает.
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


GO
CREATE NONCLUSTERED INDEX [IX_DocumentTemplate_Agency_Type]
    ON [dbo].[DocumentTemplate]([agencyID] ASC, [reportTypeID] ASC, [startDate] DESC);

