CREATE TABLE [dbo].[iEntityAction] (
    [entityActionID]    SMALLINT       IDENTITY (1, 1) NOT NULL,
    [entityID]          INT            NOT NULL,
    [alias]             NVARCHAR (128) NOT NULL,
    [name]              VARCHAR (64)   NULL,
    [ordinal_position]  SMALLINT       NOT NULL,
    [isHidden]          BIT            NOT NULL,
    [isGrantingAllowed] BIT            CONSTRAINT [DF_entityAction_isGrantingAllowed] DEFAULT (1) NOT NULL,
    [imgResourceName]   VARCHAR (50)   NULL,
    [parentID]          SMALLINT       NULL,
    CONSTRAINT [PK_entityAction] PRIMARY KEY NONCLUSTERED ([entityActionID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_entityAction_entities] FOREIGN KEY ([entityID]) REFERENCES [dbo].[iEntity] ([entityID]) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT [UIX_entityAction_position] UNIQUE CLUSTERED ([entityID] ASC, [ordinal_position] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iEntityAction] SET (LOCK_ESCALATION = AUTO);

