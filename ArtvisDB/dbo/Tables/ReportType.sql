CREATE TABLE [dbo].[ReportType] (
    [reportTypeID] SMALLINT      IDENTITY (1, 1) NOT NULL,
    [name]         VARCHAR (128) NOT NULL,
    CONSTRAINT [PK_ReportType] PRIMARY KEY CLUSTERED ([reportTypeID] ASC)
);


GO
ALTER TABLE [dbo].[ReportType] SET (LOCK_ESCALATION = AUTO);

