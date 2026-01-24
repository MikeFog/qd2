CREATE TABLE [dbo].[iModuleProcedure] (
    [moduleProcedureID] INT IDENTITY (1, 1) NOT NULL,
    [storedProcedureID] INT NOT NULL,
    [entityID]          INT NOT NULL,
    [moduleID]          INT NOT NULL,
    [actionNameID]      INT NOT NULL,
    [connectionTimeout] INT CONSTRAINT [DF_connect_timeout] DEFAULT (15) NOT NULL,
    CONSTRAINT [PK_moduleProcedures] PRIMARY KEY CLUSTERED ([moduleProcedureID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_moduleProcedures_actionNames] FOREIGN KEY ([actionNameID]) REFERENCES [dbo].[iActionName] ([actionNameID]),
    CONSTRAINT [FK_moduleProcedures_entity] FOREIGN KEY ([entityID]) REFERENCES [dbo].[iEntity] ([entityID]),
    CONSTRAINT [FK_moduleProcedures_modules] FOREIGN KEY ([moduleID]) REFERENCES [dbo].[iModules] ([moduleID]),
    CONSTRAINT [FK_moduleProcedures_storedProcedures] FOREIGN KEY ([storedProcedureID]) REFERENCES [dbo].[iStoredProcedure] ([storedProcedureID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[iModuleProcedure] SET (LOCK_ESCALATION = AUTO);

