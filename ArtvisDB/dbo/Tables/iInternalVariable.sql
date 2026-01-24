CREATE TABLE [dbo].[iInternalVariable] (
    [name]  VARCHAR (50) NOT NULL,
    [value] BINARY (500) NOT NULL,
    CONSTRAINT [PK_InternalVariable] PRIMARY KEY CLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iInternalVariable] SET (LOCK_ESCALATION = AUTO);

