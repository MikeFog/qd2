CREATE TABLE [dbo].[RollerBrand] (
    [rollerID] INT      NOT NULL,
    [brandID]  SMALLINT NOT NULL,
    CONSTRAINT [PK_RollerBrand] PRIMARY KEY CLUSTERED ([rollerID] ASC, [brandID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_RollerBrand_Brand] FOREIGN KEY ([brandID]) REFERENCES [dbo].[Brand] ([brandID]) ON DELETE CASCADE,
    CONSTRAINT [FK_RollerBrand_Roller] FOREIGN KEY ([rollerID]) REFERENCES [dbo].[Roller] ([rollerID]) ON DELETE CASCADE
);


GO
ALTER TABLE [dbo].[RollerBrand] SET (LOCK_ESCALATION = AUTO);

