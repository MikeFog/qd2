CREATE TABLE [dbo].[iMessageToActivate] (
    [name]    VARCHAR (64)    NOT NULL,
    [message] NVARCHAR (4000) NOT NULL,
    CONSTRAINT [PK_iMessageToActivate] PRIMARY KEY CLUSTERED ([name] ASC)
);


GO
ALTER TABLE [dbo].[iMessageToActivate] SET (LOCK_ESCALATION = AUTO);

