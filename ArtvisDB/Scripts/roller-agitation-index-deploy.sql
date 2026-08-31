/*
    Развёртывание: индекс IX_Roller_rolActionTypeID.
    (ветка feature/recalc-join-fix)

    Зачем: проверка AgitationMixError в IssueIUD / ModuleIssueIUD спрашивает
    «есть ли в акции ролик политагитации (тип 6)». Roller кластеризован по
    firmID, а PK_Roller — некластерный по rollerID, поэтому проверка тянула
    rolActionTypeID для КАЖДОГО выпуска акции через seek в PK_Roller плюс key
    lookup в кластерный индекс: ~1000 seek'ов ради ответа «да/нет».

    Замер на восстановленном проде (акция 541 выпуск, 9 кампаний, тёплый кеш):
        запрос проверки        7,10 -> 2,14 мс
        тело IssueIUD         11-12 -> 5,0-5,3 мс
        полный вызов AddItem   17,6 -> 10,4 мс

    Код процедур НЕ меняется — исходный текст запроса с этим индексом уже
    оптимален. Зеркальный вариант Roller(rollerID) INCLUDE(rolActionTypeID)
    проверен и НЕ помогает (7,4 мс) — нужен именно порядок ниже.

    Roller — 21,5 тыс. строк, индекс занимает сотни килобайт; влияние на
    вставку/правку ролика пренебрежимо.

    Скрипт идемпотентен. Откат: DROP INDEX [IX_Roller_rolActionTypeID] ON [dbo].[Roller];
*/
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Roller') AND name = 'IX_Roller_rolActionTypeID')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Roller_rolActionTypeID]
        ON [dbo].[Roller]([rolActionTypeID] ASC)
        INCLUDE([rollerID]);
    PRINT 'IX_Roller_rolActionTypeID — создан.';
END
ELSE
    PRINT 'IX_Roller_rolActionTypeID — уже есть.';
