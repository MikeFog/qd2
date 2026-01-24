CREATE TABLE [dbo].[iMessageToSubtitute] (
    [msgError]  VARCHAR (50)  NOT NULL,
    [message]   VARCHAR (128) NOT NULL,
    [messageID] INT           IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_iMessageToSubtitute] PRIMARY KEY CLUSTERED ([messageID] ASC)
);


GO
ALTER TABLE [dbo].[iMessageToSubtitute] SET (LOCK_ESCALATION = AUTO);

