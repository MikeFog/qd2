-- Журналы акций (макетов, подтверждённых, удалённых): колонка «Общее время» в таблице кампаний акции.
--
-- Таблицу кампаний под акцией показывает сущность 78 «Рекламная кампания» (selector 0, сценарии 9, 27, 29), данные даёт
-- процедура Campaigns, которая уже отдаёт Campaign.issuesDurationString (ЧЧ:ММ:СС из issuesDuration — суммы длительностей
-- выпусков кампании, её ведёт ActionRecalculate); не хватало только строки iEntityAttribute. Колонка ставится после «Типа»
-- (ordinal 4, перед «Ценой по тарифам»). Процедуры, таблицы и права не меняются. Название — как в карточке кампании. Испанский
-- «Общее время» → «Tiempo total» уже есть (Deploy/08).
-- Зачем: длительность кампании теперь решает, пройдёт ли она порог «процента заполнения» пакетной скидки (Deploy/76).
-- У спонсорских кампаний число включает расход бонусного времени (f_GetSponsorDuration), а не голые секунды.
--
-- Ранняя редакция скрипта записывала колонку в сущности 91, 92, 93 и 171 (там таблицы кампаний других экранов; журнал
-- их не использует) — скрипт эти строки убирает. Selector 1 (формы акции) и 2 (список с фирмой) не меняются.
--
-- В любое время, независимо от остальных; после наката перезапустить qd2 и веб (метаданные читаются при старте). Идемпотентен.
-- Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 77_campaign-duration-column.sql

SET NOCOUNT ON;
GO
DELETE FROM dbo.iEntityAttribute WHERE entityID IN (91, 92, 93, 171) AND name = 'issuesDurationString' AND selector = 0;
PRINT N'Убрано строк ранней редакции (91, 92, 93, 171): ' + CAST(@@ROWCOUNT AS nvarchar(10));

IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 78)
   AND NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 78 AND name = 'issuesDurationString' AND selector = 0)
   AND NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 78 AND ordinal_position = 4 AND selector = 0)
BEGIN
    INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (78, N'Общее время', 'issuesDurationString', 4, 0, NULL);
    PRINT N'Колонка добавлена.';
END
ELSE IF EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 78 AND name = 'issuesDurationString' AND selector = 0 AND alias <> N'Общее время')
BEGIN
    -- ранняя редакция называла колонку «Продолжительность»
    UPDATE dbo.iEntityAttribute SET alias = N'Общее время' WHERE entityID = 78 AND name = 'issuesDurationString' AND selector = 0;
    PRINT N'Колонка переименована.';
END
GO
SELECT entityID, alias, name, ordinal_position, selector FROM dbo.iEntityAttribute WHERE entityID = 78 AND selector = 0 ORDER BY ordinal_position;
IF EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 78 AND name = 'issuesDurationString' AND selector = 0)
   AND NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID IN (91, 92, 93, 171) AND name = 'issuesDurationString')
    PRINT N'ГОТОВО: колонка «Общее время» в таблице кампаний журналов акций (78) на месте.';
ELSE
    RAISERROR(N'77: колонка «Общее время» в сущности 78 не записана.', 16, 1);
GO
