CREATE TABLE [dbo].[ManagerDiscountReason] (
    [managerDiscountReasonID] SMALLINT       IDENTITY (1, 1) NOT NULL,
    [name]                    NVARCHAR (100) NOT NULL,
    CONSTRAINT [PK_ManagerDiscountReason] PRIMARY KEY CLUSTERED ([managerDiscountReasonID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ManagerDiscountReason_Name]
    ON [dbo].[ManagerDiscountReason]([name] ASC);

