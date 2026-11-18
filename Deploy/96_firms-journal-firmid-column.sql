-- Журнал «Фирмы-заказчики»: колонка «Идентификатор» (FirmID) первой (18.11.2026, Миша: в карточке фирмы номер есть, в журнале нет).
--
-- Колонки задаёт iEntityAttribute сущности 16 «Фирмы» (selector 0); процедура Firms уже отдаёт firmID (f.*), не хватало
-- только строки метаданных. Ставится перед «Именем»: ordinal 0 (у «Имени» 1). Подпись — как в карточке фирмы
-- («Идентификатор:»); испанский «Identificador» уже есть (column, Deploy/08).
-- Тот же набор колонок показывают все таблицы фирм, поэтому номер появится и там (решение Миши 18.11.2026): «Группы компаний»
-- (фирмы группы), выбор фирмы при создании акции и в калькуляторе, выбор фирм в группу, пикеры фирмы в вебе.
-- Порядок строк не меняется: сортировки по умолчанию нет ни в десктопе, ни в вебе, строки идут как отдаёт Firms (ORDER BY name).
-- Быстрый поиск десктопа раньше стоял на первой колонке; с числовой FirmID набор названия давал бы «Поиск работает только
-- для колонок, которые содержат текстовую информацию!». Новый qd2 (SmartGrid) ставит его на первую текстовую колонку — «Имя».
-- Скрипт без нового qd2 тоже работает, но до обновления клиента в десктопе быстрый поиск надо переключать щелчком по «Имени».
--
-- В любое время, на Artvis и Tumen; после наката перезапустить qd2 и веб (метаданные читаются при старте). Идемпотентен.
-- Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 96_firms-journal-firmid-column.sql

SET NOCOUNT ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 16)
BEGIN
    RAISERROR(N'96: нет сущности 16 «Фирмы» — не та база. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND name = 'firmID' AND selector = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND ordinal_position = 0 AND selector = 0)
        RAISERROR(N'96: место 0 в колонках сущности 16 занято другой колонкой. Ничего не изменено.', 16, 1);
    ELSE
    BEGIN
        INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
        VALUES (16, N'Идентификатор', 'firmID', 0, 0, NULL);
        PRINT N'Колонка добавлена.';
    END
END
GO
SELECT entityID, alias, name, ordinal_position, selector FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 ORDER BY ordinal_position;
IF (SELECT TOP (1) name FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 ORDER BY ordinal_position) = 'firmID'
    PRINT N'ГОТОВО: колонка «Идентификатор» в таблицах фирм (16) первая.';
ELSE
    RAISERROR(N'96: колонка «Идентификатор» в сущности 16 не первая.', 16, 1);
GO
SET NOEXEC OFF;
GO
