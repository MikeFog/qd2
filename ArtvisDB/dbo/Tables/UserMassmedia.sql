CREATE TABLE [dbo].[UserMassmedia] (
    [userID]      SMALLINT NOT NULL,
    [massmediaID] SMALLINT NOT NULL,
    [canWork]     BIT      CONSTRAINT [DF_UserMassmedia_canWork] DEFAULT ((0)) NOT NULL,
    [canAdd]      BIT      CONSTRAINT [DF_UserMassmedia_canAdd] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_UserMassmedia] PRIMARY KEY CLUSTERED ([userID] ASC, [massmediaID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_UserMassmedia_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserMassmedia_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[UserMassmedia] SET (LOCK_ESCALATION = AUTO);

