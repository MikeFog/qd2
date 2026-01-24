CREATE TABLE [dbo].[GroupMember] (
    [groupID] SMALLINT NOT NULL,
    [userID]  SMALLINT NOT NULL,
    CONSTRAINT [PK_GroupMember] PRIMARY KEY CLUSTERED ([groupID] ASC, [userID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_GroupMember_Group] FOREIGN KEY ([groupID]) REFERENCES [dbo].[Group] ([groupID]) ON DELETE CASCADE,
    CONSTRAINT [FK_GroupMember_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[GroupMember] SET (LOCK_ESCALATION = AUTO);

