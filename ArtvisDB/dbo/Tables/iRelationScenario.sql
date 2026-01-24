CREATE TABLE [dbo].[iRelationScenario] (
    [relationScenarioID] INT           IDENTITY (1, 1) NOT NULL,
    [name]               NVARCHAR (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [startingEntityID]   INT           NOT NULL,
    CONSTRAINT [PK_relationScenario] PRIMARY KEY CLUSTERED ([relationScenarioID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_relationScenario_entity] FOREIGN KEY ([startingEntityID]) REFERENCES [dbo].[iEntity] ([entityID]) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT [UIX_relationScenario_name] UNIQUE NONCLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iRelationScenario] SET (LOCK_ESCALATION = AUTO);

