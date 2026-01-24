CREATE TABLE [dbo].[GroupRight] (
    [groupID]        SMALLINT NOT NULL,
    [entityActionID] SMALLINT NOT NULL,
    CONSTRAINT [PK_GroupRight] PRIMARY KEY CLUSTERED ([groupID] ASC, [entityActionID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_GroupRight_entityAction] FOREIGN KEY ([entityActionID]) REFERENCES [dbo].[iEntityAction] ([entityActionID]) ON DELETE CASCADE,
    CONSTRAINT [FK_GroupRight_Group] FOREIGN KEY ([groupID]) REFERENCES [dbo].[Group] ([groupID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[GroupRight] SET (LOCK_ESCALATION = AUTO);

