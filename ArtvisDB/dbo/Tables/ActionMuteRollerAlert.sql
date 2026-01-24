CREATE TABLE [dbo].[ActionMuteRollerAlert] (
    [actionID]  INT           NOT NULL,
    [alertDate] SMALLDATETIME NOT NULL,
    CONSTRAINT [PK_ActionMuteRollerAlert] PRIMARY KEY CLUSTERED ([actionID] ASC, [alertDate] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_ActionMuteRollerAlert_Action] FOREIGN KEY ([actionID]) REFERENCES [dbo].[Action] ([actionID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[ActionMuteRollerAlert] SET (LOCK_ESCALATION = AUTO);

