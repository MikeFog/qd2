CREATE TABLE [dbo].[iModules] (
    [moduleID]    INT          NOT NULL,
    [module_name] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_modules] PRIMARY KEY CLUSTERED ([moduleID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iModules] SET (LOCK_ESCALATION = AUTO);

