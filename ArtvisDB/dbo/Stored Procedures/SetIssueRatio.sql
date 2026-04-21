CREATE PROC [dbo].[SetIssueRatio]
(
    @campaignID int,
    @campaignTypeID int,
    @startDate datetime,
    @finishDate datetime,
    @ratio float
)
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #issue
    (
        issueID int NOT NULL PRIMARY KEY
    );

    SET @startDate  = CONVERT(datetime, CONVERT(varchar(8), @startDate, 112), 112);
    SET @finishDate = CONVERT(datetime, CONVERT(varchar(8), @finishDate, 112), 112);

    INSERT INTO #issue (issueID)
    SELECT i.issueID
    FROM Issue i
        INNER JOIN TariffWindow tw ON tw.windowId = i.originalWindowID
    WHERE
        i.campaignId = @campaignID
        AND tw.dayOriginal BETWEEN @startDate AND @finishDate;

    UPDATE i WITH (ROWLOCK)
    SET i.ratio = @ratio
    FROM Issue i
        INNER JOIN #issue x ON x.issueID = i.issueID;

    IF @campaignTypeID = 2
        UPDATE i
        SET i.Ratio = @ratio
        FROM ProgramIssue i
            INNER JOIN Campaign c
                ON i.campaignId = @campaignID
               AND c.campaignID = i.campaignID
            INNER JOIN SponsorTariff st
                ON i.tariffID = st.tariffID
            INNER JOIN SponsorProgramPriceList pl
                ON st.pricelistID = pl.pricelistID
        WHERE
            i.issueDate BETWEEN
                DATEADD(mi, DATEPART(mi, pl.broadcastStart),
                    DATEADD(hh, DATEPART(hh, pl.broadcastStart), @startDate))
                AND
                DATEADD(mi, DATEPART(mi, pl.broadcastStart),
                    DATEADD(hh, DATEPART(hh, pl.broadcastStart), @finishDate));

    IF @campaignTypeID = 3
        UPDATE ModuleIssue
        SET ratio = @ratio
        WHERE
            campaignId = @campaignID
            AND issueDate BETWEEN @startDate AND @finishDate;

    IF @campaignTypeID = 4
        UPDATE [PackModuleIssue]
        SET [ratio] = @ratio
        WHERE
            [campaignID] = @campaignID
            AND [issueDate] BETWEEN @startDate AND @finishDate;
END