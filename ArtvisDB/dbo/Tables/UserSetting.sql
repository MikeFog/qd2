CREATE TABLE [dbo].[UserSetting] (
    [userID]       SMALLINT       NOT NULL,
    [settingName]  VARCHAR (128)  NOT NULL,
    [settingValue] NVARCHAR (500) NULL,
    CONSTRAINT [PK_UserSetting] PRIMARY KEY CLUSTERED ([userID] ASC, [settingName] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_UserSetting_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[UserSetting] SET (LOCK_ESCALATION = AUTO);
