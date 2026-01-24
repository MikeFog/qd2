CREATE TABLE [dbo].[iMessage] (
    [messageID] INT             IDENTITY (1, 1) NOT NULL,
    [name]      VARCHAR (64)    NOT NULL,
    [message]   NVARCHAR (4000) NOT NULL,
    CONSTRAINT [PK_message] PRIMARY KEY CLUSTERED ([messageID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [UK_message_name] UNIQUE NONCLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iMessage] SET (LOCK_ESCALATION = AUTO);

