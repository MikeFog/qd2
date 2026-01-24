CREATE TABLE [dbo].[User] (
    [userID]           SMALLINT      IDENTITY (1, 1) NOT NULL,
    [firstName]        NVARCHAR (32) NOT NULL,
    [secondName]       NVARCHAR (32) NULL,
    [lastName]         NVARCHAR (32) NOT NULL,
    [loginName]        NVARCHAR (32) NOT NULL,
    [birthday]         DATETIME      NULL,
    [passwordHash]     BINARY (16)   NOT NULL,
    [isActive]         BIT           CONSTRAINT [DF_User_isActive] DEFAULT ((0)) NOT NULL,
    [email]            NVARCHAR (64) NULL,
    [phone]            NVARCHAR (32) NULL,
    [userName]         AS            (([firstName]+space((1)))+[lastName]),
    [isAdmin]          BIT           CONSTRAINT [DF_User_isAdmin] DEFAULT ((0)) NOT NULL,
    [isTrafficManager] BIT           CONSTRAINT [DF_User_isTrafficManager] DEFAULT ((0)) NOT NULL,
    [isGrantor]        BIT           CONSTRAINT [DF_User_isGrantor] DEFAULT ((0)) NOT NULL,
    [isBookKeeper]     BIT           CONSTRAINT [DF_User_isBookKeeper] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([userID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[User] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_UserLoginName]
    ON [dbo].[User]([loginName] ASC) WITH (FILLFACTOR = 90);

