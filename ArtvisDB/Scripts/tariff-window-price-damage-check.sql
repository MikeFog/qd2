-- ТОЛЬКО ЧТЕНИЕ. Окна, которые могла испортить старая TariffWindowChangePrice («Изменить цену...» в
-- генерации окон меняла цену окон ВСЕХ станций с тем же временем и ценой; исправлено Deploy/22, 07.10.2026).
-- Процедура в базе с 09.07.2024 (create_date) — ищем с этой даты.
--
-- Признак: цена окна отличается от цены его тарифа. С июля 2024 по копии прода от 28.09.2026 таких окон
-- ровно два набора, оба — «Дорожное радио (Переславль)», 06:15, тариф 15 ₽, в окне 10 ₽:
--   35 окон 04–07.03.2025 и 01–31.12.2025 (выпусков нет);
--   103 окна 20.09–31.12.2026 — вызов agv 20.09.2026 18:33 «15 -> 10, 20.09–31.12.2026» по прайс-листу
--   RFM (Переславль), подтверждено сравнением копий (выпусков на 28.09 нет).
-- Расхождение бывает и намеренным: «Изменить цену...» на своей станции. Признак ошибки — у другой станции в то же
-- время с той же старой ценой окна поменялись так же. Tumen (07.10.2026): расхождения только у 10 станций Кургана —
-- прайм-часы 09/13/19 с 01.06.2026 переведены на цену обычных часов (у каждой станции свои цена и минуты) —
-- сделано намеренно, ущерба нет.
-- Запрос показывает текущее состояние: новые расхождения (например, от пары вызовов agv 06.10.2026 по 2027 году)
-- и выпуски, поставленные в такие окна (их цена посчитана от испорченной цены окна).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -W -s "|" -i tariff-window-price-damage-check.sql

SET NOCOUNT ON;
GO

-- 1) Наборы окон, где цена окна <> цене тарифа (с 09.07.2024).
SELECT tw.massmediaID, mm.[name] + N' (' + ISNULL(g.[name], N'') + N')' AS station,
    CONVERT(varchar(5), tw.windowDateOriginal, 108) AS [time], t.price AS tariff_price, tw.price AS window_price,
    COUNT(*) AS windows, CONVERT(varchar(10), MIN(tw.dayOriginal), 104) AS from_day, CONVERT(varchar(10), MAX(tw.dayOriginal), 104) AS to_day,
    SUM(x.issues) AS issues, t.tariffID, t.pricelistID
FROM dbo.TariffWindow tw
    JOIN dbo.Tariff t ON t.tariffID = tw.tariffId
    JOIN dbo.MassMedia mm ON mm.massmediaID = tw.massmediaID
    LEFT JOIN dbo.MassmediaGroup g ON g.massmediaGroupID = mm.massmediaGroupID
    CROSS APPLY (SELECT COUNT(*) AS issues FROM dbo.Issue i WHERE i.actualWindowID = tw.windowId) x
WHERE tw.dayOriginal >= '20240709' AND tw.price <> t.price
GROUP BY tw.massmediaID, mm.[name], g.[name], CONVERT(varchar(5), tw.windowDateOriginal, 108), t.price, tw.price, t.tariffID, t.pricelistID
ORDER BY tw.massmediaID, [time], from_day;
GO

-- 2) Выпуски в таких окнах: акция, кампания, цена выпуска (tariffPrice).
SELECT c.actionID, i.campaignID, i.issueID, CONVERT(varchar(16), tw.windowDateActual, 120) AS window_time,
    t.price AS tariff_price, tw.price AS window_price, i.tariffPrice, i.isConfirmed
FROM dbo.TariffWindow tw
    JOIN dbo.Tariff t ON t.tariffID = tw.tariffId
    JOIN dbo.Issue i ON i.actualWindowID = tw.windowId
    JOIN dbo.Campaign c ON c.campaignID = i.campaignID
WHERE tw.dayOriginal >= '20240709' AND tw.price <> t.price
ORDER BY c.actionID, tw.windowDateActual;
GO
