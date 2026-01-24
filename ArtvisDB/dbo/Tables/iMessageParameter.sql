CREATE TABLE [dbo].[iMessageParameter] (
    [messageID] INT          NOT NULL,
    [ordinal]   TINYINT      NOT NULL,
    [name]      VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_messageParameter] PRIMARY KEY CLUSTERED ([messageID] ASC, [ordinal] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_messageParameter_message] FOREIGN KEY ([messageID]) REFERENCES [dbo].[iMessage] ([messageID]) ON DELETE CASCADE,
    CONSTRAINT [UK_messageParameter_messageID_name] UNIQUE NONCLUSTERED ([messageID] ASC, [name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iMessageParameter] SET (LOCK_ESCALATION = AUTO);

