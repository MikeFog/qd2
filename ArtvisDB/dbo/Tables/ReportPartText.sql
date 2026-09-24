CREATE TABLE [dbo].[ReportPartText] (
    [reportPartTextID] SMALLINT      IDENTITY (1, 1) NOT NULL,
    [reportTypeID]     SMALLINT      NOT NULL,
    [reportText]       NVARCHAR (MAX)         NOT NULL,
    [codeName]         VARCHAR (64)  NOT NULL,
    [description]      NVARCHAR (128) NULL,
    CONSTRAINT [PK_ReportPartText] PRIMARY KEY CLUSTERED ([reportPartTextID] ASC)
);


GO
ALTER TABLE [dbo].[ReportPartText] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ReportPartText_CodeName]
    ON [dbo].[ReportPartText]([codeName] ASC);

