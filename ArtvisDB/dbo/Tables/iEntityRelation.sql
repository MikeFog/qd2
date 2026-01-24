CREATE TABLE [dbo].[iEntityRelation] (
    [relationScenarioID]    INT     NOT NULL,
    [parentEntityID]        INT     NOT NULL,
    [childEntityID]         INT     NOT NULL,
    [selector]              TINYINT CONSTRAINT [DF_EntityRelation_selector] DEFAULT (0) NOT NULL,
    [isChildNodeExpandable] BIT     CONSTRAINT [DF_EntityRelation_isEndPoint] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_entityRelation] PRIMARY KEY CLUSTERED ([relationScenarioID] ASC, [parentEntityID] ASC, [childEntityID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_entityRelation_entities_parent] FOREIGN KEY ([parentEntityID]) REFERENCES [dbo].[iEntity] ([entityID]),
    CONSTRAINT [FK_entityRelation_entity_child] FOREIGN KEY ([childEntityID]) REFERENCES [dbo].[iEntity] ([entityID]),
    CONSTRAINT [FK_entityRelation_relationScenario] FOREIGN KEY ([relationScenarioID]) REFERENCES [dbo].[iRelationScenario] ([relationScenarioID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[iEntityRelation] SET (LOCK_ESCALATION = AUTO);

