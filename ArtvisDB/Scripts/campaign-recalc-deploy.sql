/*
    Развёртывание: перенос пер-кликового пересчёта акции на закрытие формы.
    (расследование storm ActionRecalculate, ветка feature/campaign-recalc-defer)

    Порядок на проде:
      1. Этот скрипт — схема (колонка Action.needsRecalc). Идемпотентен.
      2. Процедуры (обычным деплоем / dacpac; при ручном деплое в SSMS — с
         SET QUOTED_IDENTIFIER ON, иначе UPDATE TariffWindow внутри упадёт с
         ошибкой 1934: у TariffWindow индекс IX_TariffWindow_mm_day_windowTime
         по вычисляемому столбцу windowTime):
           dbo/Stored Procedures/hlp_CampaignRecalc.sql        (новая)
           dbo/Stored Procedures/IssueIUD.sql                  (+ @skipCampaignRecalc, вызов hlp_CampaignRecalc, needsRecalc=1)
           dbo/Stored Procedures/ModuleIssueIUD.sql            (то же)
           dbo/Stored Procedures/ActionRecalculate.sql         (needsRecalc=0 в конце)
           dbo/Stored Procedures/job_RecalcPendingActions.sql  (новая — лечащий джоб)
      3. Проверка: ArtvisDB/Scripts/campaign-recalc-equivalence-check.sql
      4. Планировщик задач Windows на сервере БД (у Express нет SQL Agent) —
         см. блок в конце этого скрипта.

    Существующие акции консистентны (исторически пересчитывались на каждом клике) —
    поэтому DEFAULT 0 и никакого backfill не нужно.
*/
SET NOCOUNT ON;

-- Индекса на needsRecalc НЕТ намеренно: фильтрованный индекс требует
-- SET QUOTED_IDENTIFIER ON у ЛЮБОГО DML по Action, а легаси-процедуры
-- (ActionIUD, ActionActivate, MergeActions...) скомпилированы с QI OFF —
-- получили бы ошибку 1934. Таблица Action маленькая (~12k строк), скан
-- WHERE needsRecalc = 1 для лечащего джоба / открытия журнала стоит копейки.

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Action') AND name = 'needsRecalc')
BEGIN
    ALTER TABLE dbo.[Action]
        ADD [needsRecalc] BIT NOT NULL CONSTRAINT [DF_Action_needsRecalc] DEFAULT ((0));
    PRINT 'Action.needsRecalc — добавлена.';
END
ELSE
    PRINT 'Action.needsRecalc — уже есть.';
GO

/*
    ЛЕЧАЩИЙ ДЖОБ (Планировщик задач Windows на сервере БД).

    job_RecalcPendingActions досчитывает акции, у которых полный ActionRecalculate
    отложен на закрытие формы, но не выполнился (падение процесса / обрыв RDP).

    Разовая настройка задачи (запуск от имени учётки с правами на инстанс;
    -E = Windows-аутентификация; при SQL-логине заменить на -U/-P):

        schtasks /Create /TN "Artvis\RecalcPendingActions" /SC DAILY /ST 03:30 ^
          /RU SYSTEM ^
          /TR "sqlcmd -S localhost\SQLEXPRESS -E -d Artvis -b -Q \"EXEC dbo.job_RecalcPendingActions\""

    Ночной запуск — минимум. Прогон дёшев (обычно 0-1 акций), можно ставить
    /SC HOURLY, если журнал акций не должен показывать устаревший тотал
    брошенной в середине дня акции дольше часа.

    Проверить вручную:  EXEC dbo.job_RecalcPendingActions;
*/
