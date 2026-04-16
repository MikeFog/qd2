
CREATE   PROCEDURE [dbo].[GetMaxPriceByDay]
    @MassMediaId SMALLINT,
    @DateFrom    DATE,
    @DateTo      DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(tw.dayActual AS DATE) AS [Date],
        MAX(tw.price) AS [Price]
    FROM dbo.TariffWindow tw
    WHERE tw.massmediaID = @MassMediaId
      AND tw.dayActual >= @DateFrom
      AND tw.dayActual < DATEADD(DAY, 1, @DateTo)
    GROUP BY CAST(tw.dayActual AS DATE)
END