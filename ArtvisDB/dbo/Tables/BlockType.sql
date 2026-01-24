CREATE TABLE [dbo].[BlockType] (
    [blockTypeID] SMALLINT     NOT NULL,
    [name]        VARCHAR (50) NOT NULL,
    [code]        VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_BlockType] PRIMARY KEY CLUSTERED ([blockTypeID] ASC)
);


GO
ALTER TABLE [dbo].[BlockType] SET (LOCK_ESCALATION = AUTO);

