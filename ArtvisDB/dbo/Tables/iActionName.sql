CREATE TABLE [dbo].[iActionName] (
    [actionNameID] INT          IDENTITY (1, 1) NOT NULL,
    [name]         VARCHAR (64) NOT NULL,
    CONSTRAINT [PK_ACTIONNAMES] PRIMARY KEY CLUSTERED ([actionNameID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iActionName] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_actionNames]
    ON [dbo].[iActionName]([name] ASC) WITH (FILLFACTOR = 90);

