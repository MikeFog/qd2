CREATE TABLE [dbo].[Module] (
    [moduleID]    SMALLINT       IDENTITY (1, 1) NOT NULL,
    [massmediaID] SMALLINT       NOT NULL,
    [name]        NVARCHAR (32)  NOT NULL,
    [path]        NVARCHAR (255) NULL,
    CONSTRAINT [PK_Module] PRIMARY KEY NONCLUSTERED ([moduleID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Module_MassMedia] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]),
    CONSTRAINT [UIX_Module_massmedia_name] UNIQUE CLUSTERED ([massmediaID] ASC, [name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[Module] SET (LOCK_ESCALATION = AUTO);

