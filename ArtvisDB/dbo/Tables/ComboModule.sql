CREATE TABLE [dbo].[ComboModule] (
    [comboModuleID] SMALLINT      IDENTITY (1, 1) NOT NULL,
    [name]          NVARCHAR (64) NOT NULL,
    CONSTRAINT [PK_ComboModule] PRIMARY KEY NONCLUSTERED ([comboModuleID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [UIX_ComboModule_name] UNIQUE NONCLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[ComboModule] SET (LOCK_ESCALATION = AUTO);
