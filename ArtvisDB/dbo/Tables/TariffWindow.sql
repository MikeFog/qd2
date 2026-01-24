CREATE TABLE [dbo].[TariffWindow] (
    [windowId]                   INT                  IDENTITY (1, 1) NOT NULL,
    [tariffId]                   INT                  NULL,
    [windowDateOriginal]         DATETIME             NOT NULL,
    [windowDateActual]           DATETIME             NOT NULL,
    [duration]                   [dbo].[timeDuration] NOT NULL,
    [timeInUseConfirmed]         [dbo].[timeDuration] CONSTRAINT [DF_TariffWindow_durationInUse] DEFAULT (0) NOT NULL,
    [timeInUseUnconfirmed]       [dbo].[timeDuration] CONSTRAINT [DF_TariffWindow_timeInUseConfirmer1] DEFAULT (0) NOT NULL,
    [price]                      MONEY                NOT NULL,
    [isFirstPositionOccupied]    BIT                  CONSTRAINT [DF_TariffWindow_isFirstPositionOccupied] DEFAULT (0) NOT NULL,
    [isSecondPositionOccupied]   BIT                  CONSTRAINT [DF_TariffWindow_isSecondPositionOccupied] DEFAULT (0) NOT NULL,
    [isLastPositionOccupied]     BIT                  CONSTRAINT [DF_TariffWindow_isLastPositionOccupied] DEFAULT (0) NOT NULL,
    [firstPositionsUnconfirmed]  SMALLINT             CONSTRAINT [DF_TariffWindow_firstPositionsUnconfirmed] DEFAULT (0) NOT NULL,
    [secondPositionsUnconfirmed] SMALLINT             CONSTRAINT [DF_TariffWindow_secondPositionsUnconfirmed] DEFAULT (0) NOT NULL,
    [lastPositionsUnconfirmed]   SMALLINT             CONSTRAINT [DF_TariffWindow_lastPositionsUnconfirmed] DEFAULT (0) NOT NULL,
    [massmediaID]                SMALLINT             NOT NULL,
    [maxCapacity]                SMALLINT             CONSTRAINT [DF_TariffWindow_maxCapacity] DEFAULT (0) NOT NULL,
    [capacityInUseConfirmed]     SMALLINT             CONSTRAINT [DF_TariffWindow_capacityInUseConfirmed] DEFAULT (0) NOT NULL,
    [capacityInUseUnconfirmed]   SMALLINT             CONSTRAINT [DF_TariffWindow_capacityInUseConfirmed1] DEFAULT (0) NOT NULL,
    [isDisabled]                 BIT                  CONSTRAINT [DF_TariffWindow_isDisabled] DEFAULT ((0)) NOT NULL,
    [dayActual]                  SMALLDATETIME        NOT NULL,
    [dayOriginal]                SMALLDATETIME        NOT NULL,
    [windowPrevId]               INT                  NULL,
    [windowNextId]               INT                  NULL,
    [isMarked]                   BIT                  CONSTRAINT [DF_TariffWindow_isMarked] DEFAULT ((0)) NOT NULL,
    [duration_total]             [dbo].[timeDuration] CONSTRAINT [DF_TariffWindow_duration_total] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_TariffWindow] PRIMARY KEY NONCLUSTERED ([windowId] ASC),
    CONSTRAINT [FK_TariffWindow_Massmedia_OrigDate] FOREIGN KEY ([massmediaID]) REFERENCES [dbo].[MassMedia] ([massmediaID]),
    CONSTRAINT [FK_TariffWindow_Tariff] FOREIGN KEY ([tariffId]) REFERENCES [dbo].[Tariff] ([tariffID]),
    CONSTRAINT [FK_TariffWindow_TariffWindowNext] FOREIGN KEY ([windowNextId]) REFERENCES [dbo].[TariffWindow] ([windowId]),
    CONSTRAINT [FK_TariffWindow_TariffWindowUnion] FOREIGN KEY ([windowPrevId]) REFERENCES [dbo].[TariffWindow] ([windowId])
);


GO
ALTER TABLE [dbo].[TariffWindow] SET (LOCK_ESCALATION = AUTO);


GO
CREATE UNIQUE CLUSTERED INDEX [UX_TariffWindow_WindowID_DayOriginal]
    ON [dbo].[TariffWindow]([windowId] ASC, [dayOriginal] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_MassmediaID]
    ON [dbo].[TariffWindow]([massmediaID] ASC, [dayOriginal] ASC, [tariffId] ASC, [windowDateOriginal] ASC, [price] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_TariffID]
    ON [dbo].[TariffWindow]([tariffId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_WindowDateOriginal]
    ON [dbo].[TariffWindow]([windowDateOriginal] ASC, [massmediaID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_Next]
    ON [dbo].[TariffWindow]([windowNextId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_Prev]
    ON [dbo].[TariffWindow]([windowPrevId] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_TariffWindow_dayOriginal_windowID]
    ON [dbo].[TariffWindow]([dayOriginal] ASC, [windowId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_TariffID_WindowDateOriginal]
    ON [dbo].[TariffWindow]([tariffId] ASC, [windowDateOriginal] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_massmediaID_maxCapacity_isDisabled_tariffId__]
    ON [dbo].[TariffWindow]([massmediaID] ASC, [maxCapacity] ASC, [isDisabled] ASC, [tariffId] ASC)
    INCLUDE([windowDateOriginal], [duration], [timeInUseConfirmed], [timeInUseUnconfirmed], [isFirstPositionOccupied], [isSecondPositionOccupied], [isLastPositionOccupied], [firstPositionsUnconfirmed], [secondPositionsUnconfirmed], [lastPositionsUnconfirmed]);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_mm_date]
    ON [dbo].[TariffWindow]([massmediaID] ASC, [windowDateOriginal] ASC)
    INCLUDE([tariffId], [duration], [timeInUseConfirmed], [timeInUseUnconfirmed], [isFirstPositionOccupied], [isSecondPositionOccupied], [isLastPositionOccupied], [firstPositionsUnconfirmed], [secondPositionsUnconfirmed], [lastPositionsUnconfirmed], [maxCapacity], [isDisabled]) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_TariffWindow_mm_day]
    ON [dbo].[TariffWindow]([massmediaID] ASC, [dayActual] ASC)
    INCLUDE([tariffId], [duration], [windowId], [windowNextId], [windowPrevId], [duration_total], [maxCapacity]);

