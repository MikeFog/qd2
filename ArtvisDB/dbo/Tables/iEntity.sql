CREATE TABLE [dbo].[iEntity] (
    [entityID]          INT           IDENTITY (1, 1) NOT NULL,
    [name]              NVARCHAR (64) NOT NULL,
    [passport]          NTEXT         NULL,
    [tableName]         VARCHAR (64)  NULL,
    [className]         VARCHAR (64)  NULL,
    [assemblyName]      VARCHAR (64)  NULL,
    [pkColumn]          VARCHAR (64)  NULL,
    [codeName]          VARCHAR (64)  NULL,
    [filter]            NTEXT         NULL,
    [isGrantingAllowed] BIT           CONSTRAINT [DF_entities_isGrantingAllowsd] DEFAULT (0) NOT NULL,
    [iconName]          VARCHAR (32)  NULL,
    [entityClassName]   VARCHAR (64)  NULL,
    [parentId]          INT           NULL,
    [isObsolete]        BIT           CONSTRAINT [DF_iEntity_isObsolete] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_entities] PRIMARY KEY CLUSTERED ([entityID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_Entity_Entity] FOREIGN KEY ([parentId]) REFERENCES [dbo].[iEntity] ([entityID])
);


GO
ALTER TABLE [dbo].[iEntity] SET (LOCK_ESCALATION = AUTO);

