CREATE TABLE [dbo].[iCampaignType] (
    [campaignTypeID] TINYINT       NOT NULL,
    [name]           NVARCHAR (32) NOT NULL,
    CONSTRAINT [PK_CampaignType] PRIMARY KEY CLUSTERED ([campaignTypeID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [UIX_CampaignType_name] UNIQUE NONCLUSTERED ([name] ASC) WITH (FILLFACTOR = 90)
);


GO
ALTER TABLE [dbo].[iCampaignType] SET (LOCK_ESCALATION = AUTO);

