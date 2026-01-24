CREATE TABLE [dbo].[TransferLog] (
    [transferLogID] INT      IDENTITY (1, 1) NOT NULL,
    [transferDate]  DATETIME CONSTRAINT [DF_TransferLog_transferDate] DEFAULT (getdate()) NOT NULL,
    [userID]        SMALLINT NOT NULL,
    [oldDate]       DATETIME NOT NULL,
    [newDate]       DATETIME NOT NULL,
    [actionID]      INT      NOT NULL,
    [issueID]       INT      NOT NULL,
    CONSTRAINT [PK_TransferLog] PRIMARY KEY CLUSTERED ([transferLogID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_TransferLog_Issue] FOREIGN KEY ([issueID]) REFERENCES [dbo].[Issue] ([issueID]) ON DELETE CASCADE,
    CONSTRAINT [FK_TransferLog_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[TransferLog] SET (LOCK_ESCALATION = AUTO);


GO
CREATE NONCLUSTERED INDEX [IX_TransferLog_Issue]
    ON [dbo].[TransferLog]([issueID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_TransferLog_TransferDate]
    ON [dbo].[TransferLog]([transferDate] ASC) WITH (FILLFACTOR = 90);

