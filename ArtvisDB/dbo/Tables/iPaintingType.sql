CREATE TABLE [dbo].[iPaintingType] (
    [typeID] TINYINT       NOT NULL,
    [name]   NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_PaintingType] PRIMARY KEY CLUSTERED ([typeID] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iPaintingType] SET (LOCK_ESCALATION = AUTO);

