CREATE TABLE [dbo].[PackModule] (
    [packModuleID] SMALLINT       IDENTITY (1, 1) NOT NULL,
    [name]         NVARCHAR (64)  NOT NULL,
    [path]         NVARCHAR (255) NULL,
    CONSTRAINT [PK_PackModule] PRIMARY KEY NONCLUSTERED ([packModuleID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [UIX_PackModule_name] UNIQUE NONCLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[PackModule] SET (LOCK_ESCALATION = AUTO);

