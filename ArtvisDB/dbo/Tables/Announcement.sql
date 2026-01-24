CREATE TABLE [dbo].[Announcement] (
    [announcementID]         INT                  IDENTITY (1, 1) NOT NULL,
    [dateCreated]            [dbo].[DF_DATETIME]  CONSTRAINT [DF_Announcement_dateCreated] DEFAULT (getdate()) NOT NULL,
    [isConfirmationRequired] BIT                  CONSTRAINT [DF_Announcement_isConfirmationRequired] DEFAULT (0) NOT NULL,
    [dateConfirmed]          [dbo].[DF_DATETIME]  NULL,
    [fromUserID]             SMALLINT             NULL,
    [toUserID]               SMALLINT             NULL,
    [subject]                [dbo].[doubleString] NOT NULL,
    CONSTRAINT [PK_Announcement] PRIMARY KEY CLUSTERED ([announcementID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_AnnouncementFrom_User] FOREIGN KEY ([fromUserID]) REFERENCES [dbo].[User] ([userID]),
    CONSTRAINT [FK_AnnouncementTo_User] FOREIGN KEY ([toUserID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[Announcement] SET (LOCK_ESCALATION = AUTO);

