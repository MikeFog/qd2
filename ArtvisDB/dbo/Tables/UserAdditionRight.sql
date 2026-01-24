CREATE TABLE [dbo].[UserAdditionRight] (
    [userID]         SMALLINT NOT NULL,
    [entityActionID] SMALLINT NOT NULL,
    [isGrant]        BIT      NOT NULL,
    CONSTRAINT [PK_UserAdditionRight] PRIMARY KEY CLUSTERED ([userID] ASC, [entityActionID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_UserAdditionRight_iEntityAction] FOREIGN KEY ([entityActionID]) REFERENCES [dbo].[iEntityAction] ([entityActionID]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserAdditionRight_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[UserAdditionRight] SET (LOCK_ESCALATION = AUTO);

