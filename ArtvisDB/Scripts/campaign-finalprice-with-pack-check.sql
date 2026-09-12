/*
    СВЕРКА к campaign-finalprice-with-pack-deploy.sql
    (Campaign.finalPrice начинает хранить цену со всеми скидками, включая пакетную)

    КАК ПОЛЬЗОВАТЬСЯ
      Скрипт сам определяет, мигрирована база или нет, по телу dbo.ActionRecalculate.

        -- на НЕмигрированной базе  -> раздел "ДО":  снимает эталон и оценивает риски
        -- на мигрированной базе    -> раздел "ПОСЛЕ": сверяет факт с эталоном

      Поэтому порядок такой:
        1. sqlcmd ... -i campaign-finalprice-with-pack-check.sql     (раздел ДО)
        2. sqlcmd ... -i campaign-finalprice-with-pack-deploy.sql
        3. sqlcmd ... -i campaign-finalprice-with-pack-check.sql     (раздел ПОСЛЕ)

      Эталон складывается в dbo._FinalPriceMigrationBaseline. После успешной
      сверки таблицу можно удалить:  DROP TABLE dbo._FinalPriceMigrationBaseline;

    ЧТО ИМЕННО ДОКАЗЫВАЕТСЯ
      Каждая правка в читающих процедурах -- это замена выражения
      "finalPrice * Action.discount" на "finalPrice". Значит, вывод всех отчётов
      останется прежним тогда и только тогда, когда для каждой кампании
      выполнено:  новое finalPrice = CAST(старое finalPrice * a.discount AS DECIMAL(18,2)).
      Раздел ПОСЛЕ проверяет ровно это, построчно, по всей таблице.

      Отдельно (раздел 3) оценивается единственное место, где старый код
      домножал БЕЗ округления: там расхождение в копейку возможно и ожидаемо.

    ЗАПУСК
      sqlcmd -S <сервер> -d <база> -E -b -I -i campaign-finalprice-with-pack-check.sql
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO

DECLARE @migrated BIT = CASE
    WHEN CHARINDEX(N'finalPrice = @estimatedPrice', OBJECT_DEFINITION(OBJECT_ID('dbo.ActionRecalculate'))) > 0
    THEN 1 ELSE 0 END;

PRINT 'БД      : ' + DB_NAME();
PRINT 'Сервер  : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
PRINT 'Режим   : ' + CASE WHEN @migrated = 1 THEN 'ПОСЛЕ (база мигрирована)' ELSE 'ДО (база не мигрирована)' END;
PRINT '';

IF @migrated = 0
BEGIN
    ----------------------------------------------------------------------
    PRINT '=== 1. Масштаб изменения ===';
    ----------------------------------------------------------------------
    SELECT
        [всего кампаний]        = COUNT(*),
        [будет изменено]        = SUM(CASE WHEN c.campaignTypeID <> 4 AND a.discount <> 1 THEN 1 ELSE 0 END),
        [пакетные модульные]    = SUM(CASE WHEN c.campaignTypeID = 4 THEN 1 ELSE 0 END),
        [без пакетной скидки]   = SUM(CASE WHEN c.campaignTypeID <> 4 AND a.discount = 1 THEN 1 ELSE 0 END),
        [сумма finalPrice до]   = CAST(SUM(c.finalPrice) AS DECIMAL(18,2)),
        [сумма finalPrice после]= CAST(SUM(CASE WHEN c.campaignTypeID = 4 THEN c.finalPrice
                                               ELSE CAST(c.finalPrice * a.discount AS DECIMAL(18,2)) END) AS DECIMAL(18,2))
    FROM dbo.Campaign c
         INNER JOIN dbo.[Action] a ON a.actionID = c.actionID;

    ----------------------------------------------------------------------
    PRINT '';
    PRINT '=== 2. Снятие эталона в dbo._FinalPriceMigrationBaseline ===';
    ----------------------------------------------------------------------
    IF OBJECT_ID('dbo._FinalPriceMigrationBaseline') IS NOT NULL
        DROP TABLE dbo._FinalPriceMigrationBaseline;

    -- expectedFinalPrice -- это ровно то, что все читающие процедуры показывали
    -- ДО правки. После миграции Campaign.finalPrice обязан совпасть с ним.
    SELECT
        c.campaignID,
        c.actionID,
        c.campaignTypeID,
        oldFinalPrice      = c.finalPrice,
        packDiscount       = a.discount,
        expectedFinalPrice = CASE WHEN c.campaignTypeID = 4 THEN c.finalPrice
                                  ELSE CAST(c.finalPrice * a.discount AS DECIMAL(18,2)) END,
        -- прежний смысл Action.priceSumByCampaigns: со всеми скидками, кроме пакетной
        expectedPrePack    = CAST(c.price * c.managerDiscount AS DECIMAL(18,2)),
        actionTotalPrice   = a.totalPrice,
        actionPrePackSum   = a.priceSumByCampaigns
    INTO dbo._FinalPriceMigrationBaseline
    FROM dbo.Campaign c
         INNER JOIN dbo.[Action] a ON a.actionID = c.actionID;

    ALTER TABLE dbo._FinalPriceMigrationBaseline ADD PRIMARY KEY (campaignID);

    DECLARE @baselineRows int;
    SELECT @baselineRows = COUNT(*) FROM dbo._FinalPriceMigrationBaseline;
    PRINT '  строк в эталоне: ' + CONVERT(varchar(12), @baselineRows);

    ----------------------------------------------------------------------
    PRINT '';
    PRINT '=== 3. Копеечный риск: где старый код НЕ округлял ===';
    PRINT '    3a. ActionsForPaymentCommon -- условие "есть долг" (HAVING)';
    ----------------------------------------------------------------------
    -- Старое: SUM(finalPrice * discount) без округления, затем *100 и CAST к int.
    -- Новое : SUM(округлённых finalPrice), затем *100 и CAST к int.
    -- Интересуют акции, у которых целочисленный результат разошёлся.
    -- Оценка приблизительная: настоящий paidUp в процедуре считается на платёж,
    -- агентство и признак "чёрный/белый", здесь -- суммарно по акции. Для порядка
    -- величины этого хватает.
    ;WITH s AS (
        SELECT c.actionID,
               oldSum = SUM(CASE WHEN c.campaignTypeID = 4 THEN c.finalPrice
                                 ELSE c.finalPrice * a.discount END),
               newSum = SUM(CASE WHEN c.campaignTypeID = 4 THEN c.finalPrice
                                 ELSE CAST(c.finalPrice * a.discount AS DECIMAL(18,2)) END)
        FROM dbo.Campaign c
             INNER JOIN dbo.[Action] a ON a.actionID = c.actionID
        GROUP BY c.actionID
    ),
    p AS (
        SELECT pa.actionID, paid = SUM(pa.summa)
        FROM dbo.PaymentAction pa
        GROUP BY pa.actionID
    ),
    v AS (
        SELECT s.actionID,
               oldDebt = CAST(s.oldSum * 100 AS int) - CAST(p.paid * 100 AS int),
               newDebt = CAST(s.newSum * 100 AS int) - CAST(p.paid * 100 AS int)
        FROM s INNER JOIN p ON p.actionID = s.actionID
    )
    SELECT [акций с платежами]      = COUNT(*),
           [появится долг]          = SUM(CASE WHEN oldDebt <= 0 AND newDebt >  0 THEN 1 ELSE 0 END),
           [исчезнет долг]          = SUM(CASE WHEN oldDebt >  0 AND newDebt <= 0 THEN 1 ELSE 0 END),
           [макс. сдвиг долга, коп] = MAX(ABS(newDebt - oldDebt))
    FROM v;

    PRINT '    Все такие долги -- 1-2 копейки. Причина: старый код суммировал';
    PRINT '    НЕокруглённые произведения и обрезал дробь через CAST(... AS int),';
    PRINT '    новый складывает копеечно округлённые суммы кампаний.';
    PRINT '    Список акций, у которых вердикт поменяется (первые 100):';

    ;WITH s AS (
        SELECT c.actionID,
               oldSum = SUM(CASE WHEN c.campaignTypeID = 4 THEN c.finalPrice
                                 ELSE c.finalPrice * a.discount END),
               newSum = SUM(CASE WHEN c.campaignTypeID = 4 THEN c.finalPrice
                                 ELSE CAST(c.finalPrice * a.discount AS DECIMAL(18,2)) END)
        FROM dbo.Campaign c
             INNER JOIN dbo.[Action] a ON a.actionID = c.actionID
        GROUP BY c.actionID
    ),
    p AS (
        SELECT pa.actionID, paid = SUM(pa.summa)
        FROM dbo.PaymentAction pa
        GROUP BY pa.actionID
    )
    SELECT TOP 100
        s.actionID,
        [сумма старая] = s.oldSum,
        [сумма новая]  = s.newSum,
        [оплачено]     = p.paid,
        [долг был, коп]   = CAST(s.oldSum * 100 AS int) - CAST(p.paid * 100 AS int),
        [долг станет, коп]= CAST(s.newSum * 100 AS int) - CAST(p.paid * 100 AS int)
    FROM s INNER JOIN p ON p.actionID = s.actionID
    WHERE (CASE WHEN CAST(s.oldSum * 100 AS int) - CAST(p.paid * 100 AS int) > 0 THEN 1 ELSE 0 END)
       <> (CASE WHEN CAST(s.newSum * 100 AS int) - CAST(p.paid * 100 AS int) > 0 THEN 1 ELSE 0 END)
    ORDER BY s.actionID;

    PRINT '    3b. stat_Balance -- свёрнутые (завершившиеся) кампании';
    SELECT [кампаний с расхождением]      = COUNT(*),
           [суммарное расхождение, р.]    = ISNULL(CAST(SUM(ABS(c.finalPrice * a.discount
                                                - CAST(c.finalPrice * a.discount AS DECIMAL(18,2)))) AS DECIMAL(18,2)), 0)
    FROM dbo.Campaign c
         INNER JOIN dbo.[Action] a ON a.actionID = c.actionID
    WHERE c.campaignTypeID <> 4
      AND c.finalPrice * a.discount <> CAST(c.finalPrice * a.discount AS DECIMAL(18,2));

    ----------------------------------------------------------------------
    PRINT '';
    PRINT '=== 4. Кампании с нарушенным инвариантом finalPrice = ROUND(price*managerDiscount, 2) ===';
    PRINT '    (их первый же ActionRecalculate "вылечит" -- это не следствие миграции,';
    PRINT '     но полезно знать число заранее, чтобы потом не списать его на неё)';
    ----------------------------------------------------------------------
    SELECT [кампаний] = COUNT(*)
    FROM dbo.Campaign c
         INNER JOIN dbo.[Action] a ON a.actionID = c.actionID
    WHERE a.isSpecial = 0
      AND ABS(c.finalPrice - CAST(c.price * c.managerDiscount AS DECIMAL(18,2))) > 0.005;

    ----------------------------------------------------------------------
    PRINT '';
    PRINT '=== 5. Сходимость ДО миграции: Action.totalPrice против суммы кампаний ===';
    PRINT '    (это же число печатает деплой в конце -- сравнивать надо с ним,';
    PRINT '     иначе устаревшие totalPrice спишутся на миграцию)';
    ----------------------------------------------------------------------
    SELECT [акций всего]                  = COUNT(*),
           [расходится больше копейки]    = SUM(CASE WHEN ABS(a.totalPrice - x.s) > 0.01 THEN 1 ELSE 0 END),
           [максимальное расхождение, р.] = MAX(ABS(a.totalPrice - x.s))
    FROM dbo.[Action] a
         CROSS APPLY (SELECT ISNULL(SUM(CASE WHEN c.campaignTypeID = 4 THEN c.finalPrice
                                             ELSE CAST(c.finalPrice * aa.discount AS DECIMAL(18,2)) END), 0) AS s
                      FROM dbo.Campaign c
                           INNER JOIN dbo.[Action] aa ON aa.actionID = c.actionID
                      WHERE c.actionID = a.actionID) x
    WHERE a.isSpecial = 0;

    PRINT '';
    PRINT 'Эталон снят. Теперь запускайте campaign-finalprice-with-pack-deploy.sql,';
    PRINT 'затем этот же скрипт повторно -- он перейдёт в режим ПОСЛЕ.';
END
ELSE
BEGIN
    ----------------------------------------------------------------------
    PRINT '=== 6. Построчная сверка Campaign.finalPrice с эталоном ===';
    ----------------------------------------------------------------------
    IF OBJECT_ID('dbo._FinalPriceMigrationBaseline') IS NULL
    BEGIN
        RAISERROR('Нет dbo._FinalPriceMigrationBaseline: эталон до миграции не снимался, сверить не с чем.', 16, 1);
        RETURN;
    END

    SELECT [кампаний в эталоне]      = (SELECT COUNT(*) FROM dbo._FinalPriceMigrationBaseline),
           [кампаний сейчас]         = (SELECT COUNT(*) FROM dbo.Campaign),
           [расхождений finalPrice]  = (SELECT COUNT(*)
                                        FROM dbo.Campaign c
                                             INNER JOIN dbo._FinalPriceMigrationBaseline b ON b.campaignID = c.campaignID
                                        WHERE c.finalPrice <> b.expectedFinalPrice);

    PRINT '  Ожидается: расхождений finalPrice = 0.';
    PRINT '  Первые 50 расхождений, если они есть:';

    SELECT TOP 50
        c.campaignID, c.campaignTypeID,
        [было]   = b.oldFinalPrice,
        [пакетная] = b.packDiscount,
        [ожидалось] = b.expectedFinalPrice,
        [стало]  = c.finalPrice,
        [дельта] = c.finalPrice - b.expectedFinalPrice
    FROM dbo.Campaign c
         INNER JOIN dbo._FinalPriceMigrationBaseline b ON b.campaignID = c.campaignID
    WHERE c.finalPrice <> b.expectedFinalPrice
    ORDER BY ABS(c.finalPrice - b.expectedFinalPrice) DESC;

    ----------------------------------------------------------------------
    PRINT '';
    PRINT '=== 7. Итоги по акциям ===';
    ----------------------------------------------------------------------
    -- Сами поля Action трогать не должно было: миграция меняет только Campaign.
    SELECT [акций, где totalPrice изменился]         = SUM(CASE WHEN a.totalPrice <> b.actionTotalPrice THEN 1 ELSE 0 END),
           [акций, где priceSumByCampaigns изменился]= SUM(CASE WHEN a.priceSumByCampaigns <> b.actionPrePackSum THEN 1 ELSE 0 END)
    FROM dbo.[Action] a
         INNER JOIN (SELECT DISTINCT actionID, actionTotalPrice, actionPrePackSum
                     FROM dbo._FinalPriceMigrationBaseline) b ON b.actionID = a.actionID;

    PRINT '  Ожидается: оба нуля (деплой поля Action не трогает).';

    ----------------------------------------------------------------------
    PRINT '';
    PRINT '=== 8. Сходимость: SUM(finalPrice) по кампаниям против Action.totalPrice ===';
    ----------------------------------------------------------------------
    SELECT [акций всего]                   = COUNT(*),
           [расходится больше копейки]     = SUM(CASE WHEN ABS(a.totalPrice - x.s) > 0.01 THEN 1 ELSE 0 END),
           [максимальное расхождение, р.]  = MAX(ABS(a.totalPrice - x.s))
    FROM dbo.[Action] a
         CROSS APPLY (SELECT ISNULL(SUM(c.finalPrice), 0) AS s
                      FROM dbo.Campaign c WHERE c.actionID = a.actionID) x
    WHERE a.isSpecial = 0;

    PRINT '  Это главный смысловой критерий: теперь итог акции = простая сумма';
    PRINT '  finalPrice её кампаний. Остаточные расхождения -- акции с устаревшим';
    PRINT '  totalPrice; лечатся EXEC dbo.ActionRecalculate @actionID.';

    ----------------------------------------------------------------------
    PRINT '';
    PRINT '=== 9. Процедуры ===';
    ----------------------------------------------------------------------
    SELECT o.name AS [ещё домножает на пакетную скидку]
    FROM sys.sql_modules m
           JOIN sys.objects o ON o.object_id = m.object_id
    WHERE  CHARINDEX(N'finalPrice', m.definition) > 0
       AND (CHARINDEX(N'finalPrice] * a.[discount]', m.definition) > 0
         OR CHARINDEX(N'finalPrice * a.discount',    m.definition) > 0
         OR CHARINDEX(N'FinalPrice * @actiondiscount', m.definition) > 0
         OR CHARINDEX(N'actionDiscount * @finalPrice', m.definition) > 0
         OR CHARINDEX(N'finalPrice * discount',      m.definition) > 0
         OR CHARINDEX(N'campaignPrice * @aDiscount', m.definition) > 0
         OR CHARINDEX(N'campaignFinalPrice * @campaignAdiscount', m.definition) > 0)
    ORDER BY o.name;

    PRINT '  Ожидается пустой список.';
    PRINT '';
    PRINT 'Если разделы 6-9 чистые -- миграция прошла корректно.';
    PRINT 'Эталон можно удалить: DROP TABLE dbo._FinalPriceMigrationBaseline;';
END
GO
