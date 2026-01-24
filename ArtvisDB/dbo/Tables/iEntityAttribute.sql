CREATE TABLE [dbo].[iEntityAttribute] (
    [entityID]         INT          NOT NULL,
    [alias]            VARCHAR (32) NOT NULL,
    [name]             VARCHAR (32) NOT NULL,
    [ordinal_position] SMALLINT     CONSTRAINT [DF_entityAttribute_ordinal_position] DEFAULT (1) NOT NULL,
    [selector]         TINYINT      CONSTRAINT [DF_entityAttribute_selector] DEFAULT (0) NOT NULL,
    [dataType]         VARCHAR (32) NULL,
    CONSTRAINT [PK_entityAttribute] PRIMARY KEY NONCLUSTERED ([entityID] ASC, [alias] ASC, [selector] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_entityAttribute_entity] FOREIGN KEY ([entityID]) REFERENCES [dbo].[iEntity] ([entityID]) ON DELETE CASCADE,
    CONSTRAINT [UIX_entityAttribute] UNIQUE CLUSTERED ([entityID] ASC, [ordinal_position] ASC, [selector] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iEntityAttribute] SET (LOCK_ESCALATION = AUTO);

