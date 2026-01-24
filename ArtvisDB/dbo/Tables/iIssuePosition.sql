CREATE TABLE [dbo].[iIssuePosition] (
    [positionId]       SMALLINT      NOT NULL,
    [description]      NVARCHAR (64) NOT NULL,
    [shortDescription] NVARCHAR (64) NOT NULL,
    CONSTRAINT [PK_issuePosition] PRIMARY KEY CLUSTERED ([positionId] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [UX_issuePosition_description] UNIQUE NONCLUSTERED ([description] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iIssuePosition] SET (LOCK_ESCALATION = AUTO);

