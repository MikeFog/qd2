CREATE TABLE [dbo].[LogDeletedIssue] (
    [logId]       INT      IDENTITY (1, 1) NOT NULL,
    [userId]      SMALLINT NOT NULL,
    [date]        DATETIME NOT NULL,
    [rollerId]    INT      NOT NULL,
    [actionId]    INT      NOT NULL,
    [issueDate]   DATETIME NOT NULL,
    [massmediaID] SMALLINT NULL,
    CONSTRAINT [PK_Log] PRIMARY KEY CLUSTERED ([logId] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_LogDeletedIssue_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]) ON DELETE SET NULL
);


GO
ALTER TABLE [dbo].[LogDeletedIssue] SET (LOCK_ESCALATION = AUTO);

