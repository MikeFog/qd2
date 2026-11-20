-- Журнал «Фирмы-заказчики»: «Идентификатор» (FirmID) только в самом журнале (20.11.2026, просьба заказчика от 19.11.2026:
-- «отображать его только в журнале фирм заказчиков»).
--
-- 96 и 98 добавили «Идентификатор» в общий набор колонок сущности 16 «Фирмы» (selector 0) — его показывают все таблицы фирм:
-- журнал, «Группы компаний», выбор фирмы при создании акции и в калькуляторе, выбор фирм в группу, пикеры в вебе.
-- Теперь у журнала свой набор — selector 1 (Firm.JournalAttributeSelector; десктоп MDIForm.ShowFirms, веб MenuRoutes «miFirm»):
-- копия общего, «Идентификатор» четвёртый, перед «Телефоном», как после 98. Из общего набора «Идентификатор» убирается.
-- Процедуры, подписи и испанские строки не меняются.
--
-- После 96 и 98. В любое время, на Artvis и Tumen. Нужны новые qd2 и веб: старый клиент открывает журнал с общим набором —
-- «Идентификатора» в журнале не будет, пока клиент не обновят. Новый клиент без этого скрипта журнал фирм не откроет
-- (набора 1 нет) — сначала скрипт, потом клиент. После наката перезапустить qd2 и веб (метаданные читаются при старте).
-- Идемпотентен. После 100 не перезапускать 96 (вернёт «Идентификатор» в общий набор; его итоговая проверка всё равно даст
-- ошибку) и 98 (проверка набора 0 даст ошибку, без вреда). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 100_firms-journal-own-columns.sql

SET NOCOUNT ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 16)
BEGIN
    RAISERROR(N'100: нет сущности 16 «Фирмы» — не та база. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 1)
   AND NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 AND name = 'firmID' AND ordinal_position = 4)
BEGIN
    RAISERROR(N'100: в общем наборе сущности 16 нет «Идентификатора» на месте 4 — сначала накатить 96 и 98. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
BEGIN TRANSACTION;
-- набор журнала: копия общего (вместе с «Идентификатором»)
IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 1)
BEGIN
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    SELECT entityID, alias, name, ordinal_position, 1, dataType
    FROM dbo.iEntityAttribute
    WHERE entityID = 16 AND selector = 0;
    PRINT N'Набор колонок журнала (selector 1) создан.';
END
-- общий набор — без «Идентификатора»
DELETE FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 AND name = 'firmID';
IF @@ROWCOUNT > 0 PRINT N'«Идентификатор» убран из общего набора (selector 0).';
COMMIT;
GO
SELECT selector, ordinal_position, name, alias, dataType FROM dbo.iEntityAttribute WHERE entityID = 16 ORDER BY selector, ordinal_position;
IF NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 AND name = 'firmID')
   AND EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 1 AND name = 'firmID' AND ordinal_position = 4)
   AND (SELECT TOP (1) name FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 1 ORDER BY ordinal_position) = 'name'
   AND (SELECT TOP (1) name FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 ORDER BY ordinal_position) = 'name'
   AND NOT EXISTS (
        SELECT name FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0
        EXCEPT
        SELECT name FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 1)
    PRINT N'ГОТОВО: «Идентификатор» только в журнале фирм (набор 1), в остальных таблицах фирм (набор 0) его нет.';
ELSE
    RAISERROR(N'100: наборы колонок сущности 16 не те: ожидался «Идентификатор» только в наборе 1 на месте 4.', 16, 1);
GO
SET NOEXEC OFF;
GO
