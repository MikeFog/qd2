CREATE TABLE [dbo].[ComboModuleContent] (
    [comboModuleContentID] SMALLINT IDENTITY (1, 1) NOT NULL,
    [comboModuleID]        SMALLINT NOT NULL,
    [moduleID]             SMALLINT NOT NULL,
    CONSTRAINT [PK_ComboModuleContent] PRIMARY KEY CLUSTERED ([comboModuleContentID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_ComboModuleContent_ComboModule] FOREIGN KEY ([comboModuleID]) REFERENCES [dbo].[ComboModule] ([comboModuleID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ComboModuleContent_Module] FOREIGN KEY ([moduleID]) REFERENCES [dbo].[Module] ([moduleID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[ComboModuleContent] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_ComboModuleContent]
    ON [dbo].[ComboModuleContent]([comboModuleID] ASC, [moduleID] ASC) WITH (FILLFACTOR = 90);
