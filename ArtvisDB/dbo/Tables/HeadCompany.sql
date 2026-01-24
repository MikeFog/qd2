CREATE TABLE [dbo].[HeadCompany] (
    [headCompanyID] INT                  IDENTITY (1, 1) NOT NULL,
    [name]          [dbo].[doubleString] NOT NULL,
    CONSTRAINT [PK__HeadComp] PRIMARY KEY CLUSTERED ([headCompanyID] ASC)
);

