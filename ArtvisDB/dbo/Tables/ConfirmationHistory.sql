CREATE TABLE [dbo].[ConfirmationHistory] (
    [confirmationID]     INT             IDENTITY (1, 1) NOT NULL,
    [confirmationTypeID] TINYINT         NOT NULL,
    [userID]             SMALLINT        NOT NULL,
    [grantorID]          SMALLINT        NOT NULL,
    [description]        NVARCHAR (2000) NOT NULL,
    [dateCreated]        DATETIME        CONSTRAINT [DF_ConfirmationHistory_dateCreated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ConfirmationHistory] PRIMARY KEY CLUSTERED ([confirmationID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_ConfirmationHistory_ConfirmationType] FOREIGN KEY ([confirmationTypeID]) REFERENCES [dbo].[iConfirmationType] ([confirmationTypeID]),
    CONSTRAINT [FK_ConfirmationHistory_Grantor] FOREIGN KEY ([grantorID]) REFERENCES [dbo].[User] ([userID]),
    CONSTRAINT [FK_ConfirmationHistory_User] FOREIGN KEY ([userID]) REFERENCES [dbo].[User] ([userID])
);


GO
ALTER TABLE [dbo].[ConfirmationHistory] SET (LOCK_ESCALATION = AUTO);

