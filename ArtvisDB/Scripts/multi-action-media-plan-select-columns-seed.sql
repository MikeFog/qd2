-- Колонки списка акций в «График размещения по нескольким акциям»
-- (Рекламный отдел, FrmActionsSelector).
--
-- Селектор 1 сущности «Рекламная акция» (77): те же колонки, что в селекторе 0,
-- но после номера акции добавлены «Дата начала» и «Дата окончания»
-- (startDate/finishDate отдаёт Actions1). Селектор 0 — журналы и остальные
-- экраны — не меняется. Форма ставит селектор 1 на клон сущности
-- (FrmActionsSelector.ColumnsSelector).
--
-- Строки селектора пересоздаются целиком: MERGE по ordinal_position при сдвиге
-- позиций упёрся бы в PK (entityID, alias, selector) и UIX (ordinal_position).
--
-- Метаданные клиент читает при входе — после прогона перезапустить qd2.
-- Скрипт идемпотентен, повторный прогон безопасен.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i multi-action-media-plan-select-columns-seed.sql

SET NOCOUNT ON;
GO

DECLARE @entAction INT = 77;
DECLARE @selector TINYINT = 1;

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entAction)
BEGIN
    RAISERROR('Сущность 77 («Рекламная акция») не найдена — проверьте базу', 16, 1);
    RETURN;
END

BEGIN TRAN;

DELETE FROM [dbo].[iEntityAttribute] WHERE entityID = @entAction AND selector = @selector;

-- Копия селектора 0 с сохранением позиций, кроме двух новых колонок сразу после
-- номера акции: фирма-заказчик (позиция 2) сдвигается на 4. Позиции 3 и 4 в
-- селекторе 0 свободны (занято 1, 2, 5, 15, 35, 40, 45, 50).
INSERT INTO [dbo].[iEntityAttribute] (entityID, alias, name, ordinal_position, selector, dataType)
SELECT entityID, alias, name, ordinal_position, @selector, dataType
FROM [dbo].[iEntityAttribute]
WHERE entityID = @entAction AND selector = 0;

UPDATE [dbo].[iEntityAttribute] SET ordinal_position = 4
WHERE entityID = @entAction AND selector = @selector AND name = 'firmName';

INSERT INTO [dbo].[iEntityAttribute] (entityID, alias, name, ordinal_position, selector)
VALUES
    (@entAction, N'Дата начала',    'startDate',  2, @selector),
    (@entAction, N'Дата окончания', 'finishDate', 3, @selector);

COMMIT;

-------------------------------------------------------------------------------
-- Отчёт
-------------------------------------------------------------------------------

PRINT '--- Выбор акций для сводного медиаплана: колонки (iEntityAttribute 77, селектор 1) ---';

SELECT ordinal_position AS [позиция], name AS [колонка], alias AS [заголовок]
FROM [dbo].[iEntityAttribute]
WHERE entityID = @entAction AND selector = @selector
ORDER BY ordinal_position;
