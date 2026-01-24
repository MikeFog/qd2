CREATE TABLE [dbo].[DisabledWindow] (
    [disabledWindowID] INT                 IDENTITY (1, 1) NOT NULL,
    [massmediaID]      SMALLINT            NOT NULL,
    [startDate]        [dbo].[DF_DATETIME] NOT NULL,
    [finishdate]       [dbo].[DF_DATETIME] NOT NULL,
    CONSTRAINT [PK_DisabledWindow] PRIMARY KEY NONCLUSTERED ([disabledWindowID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_DisabledWindow_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[DisabledWindow] SET (LOCK_ESCALATION = AUTO);

