CREATE TABLE [dbo].[MassMedia] (
    [massmediaID]          SMALLINT             IDENTITY (247, 1) NOT NULL,
    [roltypeID]            SMALLINT             NULL,
    [nationalBonus]        [dbo].[timeDuration] CONSTRAINT [DF_MassMedia_nationalBonus] DEFAULT ((0)) NOT NULL,
    [deadLine]             DATETIME             NULL,
    [isCheckDeadline]      BIT                  NOT NULL,
    [isActive]             BIT                  CONSTRAINT [DF__massmedia__isAct__7FCBCD04] DEFAULT ((1)) NOT NULL,
    [rollerEnterPath]      NVARCHAR (255)       NULL,
    [rollerExitPath]       NVARCHAR (255)       NULL,
    [rollerEtcPath]        NVARCHAR (255)       NULL,
    [rollerPath]           NVARCHAR (255)       NULL,
    [rollerEnterMax]       SMALLINT             NULL,
    [rollerExitMax]        SMALLINT             NULL,
    [rollerEtcMax]         SMALLINT             NULL,
    [massmediaGroupID]     INT                  NULL,
    [exportName]           NVARCHAR (255)       NULL,
    [rollerEnterMin]       SMALLINT             NULL,
    [rollerExitMin]        SMALLINT             NULL,
    [rollerEtcMin]         SMALLINT             NULL,
    [mediaPlusMassmediaID] SMALLINT             NULL,
    [grammofonMistake]     TINYINT              CONSTRAINT [DF_MassMedia_grammofonMistake] DEFAULT ((0)) NOT NULL,
    [name]                 VARCHAR (64)         NOT NULL,
    [director]             [dbo].[doubleString] NULL,
    [painting]             IMAGE                NULL,
    [prefix]               [dbo].[doubleString]         NULL,
    [fullPrefix]           [dbo].[doubleString] NULL,
    [reportString]         [dbo].[doubleString] NULL,
    [certificateIssued]    [dbo].[doubleString] NULL,
    [volume_c]             DECIMAL (5, 2)       NULL,
    [volume_n]             DECIMAL (5, 2)       NULL,
    [volume_p]             DECIMAL (5, 2)       NULL,
    [volume_m]             DECIMAL (5, 2)       NULL,
    [volume_j]             DECIMAL (5, 2)       NULL,
    CONSTRAINT [PK_MassMedia] PRIMARY KEY NONCLUSTERED ([massmediaID] ASC),
    CONSTRAINT [FK_MassMedia_MassmediaGroup] FOREIGN KEY ([massmediaGroupID]) REFERENCES [dbo].[MassmediaGroup] ([massmediaGroupID]),
    CONSTRAINT [FK_MassMedia_RolType] FOREIGN KEY ([roltypeID]) REFERENCES [dbo].[iRolType] ([rolTypeID])
);


GO
ALTER TABLE [dbo].[MassMedia] SET (LOCK_ESCALATION = AUTO);


GO
CREATE CLUSTERED INDEX [UX_MassMedia_massmediaID_massmediaGroupID]
    ON [dbo].[MassMedia]([massmediaID] ASC, [massmediaGroupID] ASC);

