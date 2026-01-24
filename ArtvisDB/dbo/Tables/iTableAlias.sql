CREATE TABLE [dbo].[iTableAlias] (
    [storedProcedureID] INT          NOT NULL,
    [position]          TINYINT      CONSTRAINT [DF_aliasTables_ordinal_position] DEFAULT (1) NOT NULL,
    [name]              VARCHAR (64) CONSTRAINT [DF_aliasTables_alias_table_name] DEFAULT ('data') NOT NULL,
    CONSTRAINT [PK_aliasTables] PRIMARY KEY CLUSTERED ([storedProcedureID] ASC, [position] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_aliasTables_TO_storedProcedures] FOREIGN KEY ([storedProcedureID]) REFERENCES [dbo].[iStoredProcedure] ([storedProcedureID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[iTableAlias] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_aliasTables_alias]
    ON [dbo].[iTableAlias]([storedProcedureID] ASC, [name] ASC) WITH (FILLFACTOR = 90);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_aliasTables_position]
    ON [dbo].[iTableAlias]([storedProcedureID] ASC, [position] ASC) WITH (FILLFACTOR = 90);

