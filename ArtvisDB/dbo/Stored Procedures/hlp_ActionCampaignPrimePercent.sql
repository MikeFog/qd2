-- Доля прайма линейных и модульных кампаний, % с округлением до 0,1 (docs/tasks/package-discount-prime-limit.md).
-- Правило — только здесь:
--   прайм — окно, цена которого равна максимальной цене окон станции в этот день (как генератор шаблонов, GetMaxPriceByDay);
--   день, где все окна станции по одной цене, — прайм целиком; окно и день — фактические (Issue.actualWindowID, dayActual);
--   доля = секунды роликов в прайм-окнах / все секунды роликов кампании; кампании без выпусков в результат не попадают.
-- Вход — #ActionIssue вызывающего (выпуски с окном выхода: campaignID, massmediaID, dayActual, price, duration), выход —
-- #ActionPrime (campaignID, primePercent). Выпуски собирает вызывающий: ActionRecalculate (тем же проходом считает суммы
-- кампаний — второй проход по выпускам и окнам на большой акции стоил +250 мс) и заполнение в Deploy/97.
-- Максимум цены — один раз на станцию и день (индекс IX_TariffWindow_MassmediaID_DayActual_Price).
CREATE PROCEDURE [dbo].[hlp_ActionCampaignPrimePercent]
AS
SET NOCOUNT ON;

DECLARE @dayMax TABLE (massmediaID SMALLINT NOT NULL, dayActual SMALLDATETIME NOT NULL, maxPrice DECIMAL(18,2) NULL,
    PRIMARY KEY (massmediaID, dayActual));

INSERT INTO @dayMax (massmediaID, dayActual)
SELECT DISTINCT massmediaID, dayActual FROM #ActionIssue;

UPDATE k
SET k.maxPrice = (SELECT MAX(d.price) FROM dbo.TariffWindow d WHERE d.massmediaID = k.massmediaID AND d.dayActual = k.dayActual)
FROM @dayMax k;

INSERT INTO #ActionPrime (campaignID, primePercent)
SELECT
    x.campaignID,
    CAST(ROUND(100.0 * SUM(CASE WHEN x.price = k.maxPrice THEN x.duration ELSE 0 END) / SUM(x.duration), 1) AS DECIMAL(4,1))
FROM #ActionIssue x
    INNER JOIN @dayMax k ON k.massmediaID = x.massmediaID AND k.dayActual = x.dayActual
GROUP BY x.campaignID
HAVING SUM(x.duration) > 0;
