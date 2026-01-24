CREATE TABLE [dbo].[MassmediaGroup] (
    [massmediaGroupID] INT           IDENTITY (1, 1) NOT NULL,
    [name]             VARCHAR (250) NOT NULL,
    CONSTRAINT [PK_MassmediaGroup] PRIMARY KEY CLUSTERED ([massmediaGroupID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[MassmediaGroup] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UK_MassmediaGroup_Name]
    ON [dbo].[MassmediaGroup]([name] ASC) WITH (FILLFACTOR = 90);

