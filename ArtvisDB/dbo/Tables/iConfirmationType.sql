CREATE TABLE [dbo].[iConfirmationType] (
    [confirmationTypeID] TINYINT       IDENTITY (1, 1) NOT NULL,
    [name]               NVARCHAR (64) NOT NULL,
    CONSTRAINT [PK_ConfirmationType] PRIMARY KEY CLUSTERED ([confirmationTypeID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iConfirmationType] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UIX_ConfirmationType_name]
    ON [dbo].[iConfirmationType]([name] ASC) WITH (FILLFACTOR = 90);

