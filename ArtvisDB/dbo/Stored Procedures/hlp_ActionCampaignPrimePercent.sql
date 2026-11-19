-- Доля прайма линейных кампаний, % с округлением до 0,1 (docs/tasks/package-discount-prime-limit.md).
-- Правило — только здесь (уточнения заказчика 19.11.2026):
--   только линейные кампании (тип 1); у модульных, спонсорских и пакетных доли нет;
--   прайм — окно, цена которого равна максимальной цене обычных платных окон станции в этот день (как генератор шаблонов
--   и подсветка ₽); если у обычных платных окон станции в этот день одна цена, прайма в этот день нет. Обычные — окна тарифов
--   не «только для модулей» (у станций Переславля модульные окна по 0); платные — с ценой больше 0: окна с нулевой ценой
--   заказчик просил не учитывать — их 1–5 в день у Авторадио, Европы Плюс, Ретро FM, Русского радио и Хит FM, с ними
--   1 460 станций-дней 2026 года не были бы однотарифными;
--   окно и день — фактические (Issue.actualWindowID, dayActual);
--   доля = секунды роликов в прайм-окнах / секунды роликов кампании в платных окнах (выпуски в нулевых окнах не считаются
--   вовсе — иначе бесплатными выходами можно было бы разбавить прайм); кампании без таких выпусков в результат не попадают.
-- Вход — #ActionIssue вызывающего (выпуски с окном выхода: campaignID, campaignTypeID, massmediaID, dayActual, price,
-- duration), выход — #ActionPrime (campaignID, primePercent). Выпуски собирает вызывающий: ActionRecalculate (тем же
-- проходом считает суммы кампаний — второй проход по выпускам и окнам на большой акции стоил +250 мс) и заполнение в Deploy/97.
-- Скорость (ArtvisDev 19.11.2026, цены дня): окна дня — поиском по индексу IX_TariffWindow_ActualDate (станция,
-- windowDateActual; цена и тариф в нём есть), день окна dayActual — дата его windowDateActual (0 расхождений на 2 011 204
-- окнах; broadcastStart закрыт); тариф окна — поиском по ключу Tariff. Не годится (замерено): по (станция, dayActual) —
-- до 325 мс на акции в 18 тыс. выпусков; без FORCESEEK и LOOP JOIN — просмотр индекса окон, 0,7–1,5 с на любой акции;
-- модульные тарифы в табличной переменной — оптимизатор считает её почти пустой и просматривает целиком на каждый день,
-- +170 мс на акции в 300 выпусков; условие на цену в WHERE — FORCESEEK уходит на индекс с поиском только по станции.
CREATE PROCEDURE [dbo].[hlp_ActionCampaignPrimePercent]
AS
SET NOCOUNT ON;

DECLARE @day TABLE (massmediaID SMALLINT NOT NULL, dayActual SMALLDATETIME NOT NULL,
    maxPrice DECIMAL(18,2) NULL, minPrice DECIMAL(18,2) NULL, PRIMARY KEY (massmediaID, dayActual));

INSERT INTO @day (massmediaID, dayActual)
SELECT DISTINCT massmediaID, dayActual FROM #ActionIssue WHERE campaignTypeID = 1 AND price > 0;

UPDATE k
SET k.maxPrice = p.maxPrice, k.minPrice = p.minPrice
FROM @day k
    CROSS APPLY
    (
        -- нулевые окна — в CASE, а не в WHERE: с условием на цену FORCESEEK выбирал индекс, где поиск только по станции,
        -- и пересчёт большой акции дорожал на 1,1 с. Без NULL в CASE — иначе предупреждение «Значение NULL исключено» на
        -- каждом пересчёте; день только из нулевых окон: максимум 0, минимум «бесконечность» — прайма нет
        SELECT maxPrice = MAX(CASE WHEN d.price > 0 THEN d.price ELSE 0 END),
            minPrice = MIN(CASE WHEN d.price > 0 THEN d.price ELSE 9999999999999999.99 END)
        FROM dbo.TariffWindow d WITH (FORCESEEK)
            INNER JOIN dbo.Tariff t ON t.tariffID = d.tariffId
        WHERE d.massmediaID = k.massmediaID
            AND d.windowDateActual >= k.dayActual AND d.windowDateActual < DATEADD(DAY, 1, k.dayActual)
            AND t.isForModuleOnly = 0
    ) p
OPTION (LOOP JOIN);

INSERT INTO #ActionPrime (campaignID, primePercent)
SELECT
    x.campaignID,
    CAST(ROUND(100.0 * SUM(CASE WHEN x.price = k.maxPrice AND k.maxPrice > k.minPrice THEN x.duration ELSE 0 END)
        / SUM(x.duration), 1) AS DECIMAL(4,1))
FROM #ActionIssue x
    INNER JOIN @day k ON k.massmediaID = x.massmediaID AND k.dayActual = x.dayActual
WHERE x.campaignTypeID = 1 AND x.price > 0
GROUP BY x.campaignID
HAVING SUM(x.duration) > 0;
