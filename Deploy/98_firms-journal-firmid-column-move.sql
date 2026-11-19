-- Журнал «Фирмы-заказчики»: «Идентификатор» (FirmID) с первого места на четвёртое, перед «Телефоном» (19.11.2026, просьба заказчика:
-- первой колонкой должно быть название).
--
-- Деплой 96 поставил «Идентификатор» первым (ordinal 0). Теперь порядок колонок сущности 16 «Фирмы» (selector 0):
-- Имя (1), ИНН (2), Группа компаний (3), Идентификатор (4), Телефон (5), дальше как было. Меняется одна строка iEntityAttribute:
-- ordinal_position у firmID 0 → 4 (место 4 свободно, остальные колонки не трогаем). Процедуры, подписи и испанские строки не меняются.
-- Как и 96, порядок виден во всех таблицах фирм: журнал, «Группы компаний», выбор фирмы при создании акции и в калькуляторе,
-- выбор фирм в группу, пикеры фирмы в вебе.
-- Быстрый поиск десктопа с этим порядком снова стоит на «Имени» (первая колонка текстовая); правка SmartGrid из 96 остаётся и не мешает.
--
-- После 96 (без 96 колонки «Идентификатор» нет — скрипт остановится). В любое время, на Artvis и Tumen; после наката перезапустить
-- qd2 и веб (метаданные читаются при старте). Идемпотентен. После 98 96 не перезапускать: колонку он не тронет (она есть), но его
-- итоговая проверка «первая колонка — Идентификатор» выдаст ошибку.
-- Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 98_firms-journal-firmid-column-move.sql

SET NOCOUNT ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 16)
BEGIN
    RAISERROR(N'98: нет сущности 16 «Фирмы» — не та база. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND name = 'firmID' AND selector = 0)
BEGIN
    RAISERROR(N'98: в сущности 16 нет колонки «Идентификатор» (firmID) — сначала накатить 96. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
IF EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND name = 'firmID' AND selector = 0 AND ordinal_position <> 4)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND ordinal_position = 4 AND selector = 0)
        RAISERROR(N'98: место 4 в колонках сущности 16 занято другой колонкой. Ничего не изменено.', 16, 1);
    ELSE
    BEGIN
        UPDATE dbo.iEntityAttribute SET ordinal_position = 4
        WHERE entityID = 16 AND name = 'firmID' AND selector = 0;
        PRINT N'Колонка «Идентификатор» перенесена на место 4.';
    END
END
GO
SELECT entityID, alias, name, ordinal_position, selector FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 ORDER BY ordinal_position;
IF (SELECT TOP (1) name FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 ORDER BY ordinal_position) = 'name'
   AND (SELECT ordinal_position FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 AND name = 'firmID') = 4
   AND (SELECT ordinal_position FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 AND name = 'phone') = 5
    PRINT N'ГОТОВО: в таблицах фирм (16) первая колонка «Имя», «Идентификатор» четвёртая, перед «Телефоном».';
ELSE
    RAISERROR(N'98: порядок колонок сущности 16 не тот: ожидалось «Имя» первой и «Идентификатор» (4) перед «Телефоном» (5).', 16, 1);
GO
SET NOEXEC OFF;
GO
