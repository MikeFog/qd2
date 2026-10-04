-- Убираем многоточие в названиях трёх пунктов меню, добавленных с «...»: в остальном меню
-- (434 пункта) его нет, и ни одно правило «пункт открывает диалог» там не выдерживалось.
--   прайс-лист (80): «Сменить цену...»; тариф (81): «Изменить похожие тарифы...», «Клонировать массово...».
-- Переводы (iTranslation) лежат по русскому оригиналу, поэтому новые названия переводятся
-- web-i18n-es-seed.sql (прогнать после этого скрипта). Скрипт идемпотентен.
-- После прогона перезапустить qd2 (кэш метаданных).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i menu-remove-ellipsis-deploy.sql

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE [dbo].[iEntityAction] SET alias = N'Сменить цену'
WHERE entityID = 80 AND name = 'ChangeTariffPrices' AND alias <> N'Сменить цену';

UPDATE [dbo].[iEntityAction] SET alias = N'Изменить похожие тарифы'
WHERE entityID = 81 AND name = 'EditSimilarTariffs' AND alias <> N'Изменить похожие тарифы';

UPDATE [dbo].[iEntityAction] SET alias = N'Клонировать массово'
WHERE entityID = 81 AND name = 'CloneTariffsMass' AND alias <> N'Клонировать массово';

COMMIT TRANSACTION;

SELECT entityID, alias, name FROM [dbo].[iEntityAction]
WHERE name IN ('ChangeTariffPrices', 'EditSimilarTariffs', 'CloneTariffsMass')
ORDER BY entityID, ordinal_position;
