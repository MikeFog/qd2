CREATE TABLE [dbo].[UserAdditionMenu] (
    [userID]  SMALLINT NOT NULL,
    [menuID]  SMALLINT NOT NULL,
    [isGrant] BIT      NOT NULL,
    CONSTRAINT [PK_UserAdditionMenu] PRIMARY KEY CLUSTERED ([userID] ASC, [menuID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_UserAdditionMenu_iMenu] FOREIGN KEY ([menuID]) REFERENCES [dbo].[iMenu] ([menuID]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserAdditionMenu_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[UserAdditionMenu] SET (LOCK_ESCALATION = AUTO);

