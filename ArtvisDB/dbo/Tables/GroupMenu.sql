CREATE TABLE [dbo].[GroupMenu] (
    [groupID] SMALLINT NOT NULL,
    [menuID]  SMALLINT NOT NULL,
    CONSTRAINT [PK_GroupMenu] PRIMARY KEY CLUSTERED ([groupID] ASC, [menuID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_GroupMenu_Group] FOREIGN KEY ([groupID]) REFERENCES [dbo].[Group] ([groupID]) ON DELETE CASCADE,
    CONSTRAINT [FK_GroupMenu_menu] FOREIGN KEY ([menuID]) REFERENCES [dbo].[iMenu] ([menuID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[GroupMenu] SET (LOCK_ESCALATION = AUTO);

