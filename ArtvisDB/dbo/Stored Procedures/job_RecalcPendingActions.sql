CREATE PROC [dbo].[job_RecalcPendingActions]
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    /*
        Досчитывает акции, у которых полный ActionRecalculate отложен на закрытие
        формы редактирования (Action.needsRecalc = 1), но так и не выполнился —
        процесс/RDP упал до CampaignForm.FormClosing.
        См. расследование storm ActionRecalculate.

        У SQL Server Express нет Agent — ставится в Планировщик задач Windows,
        см. Scripts/campaign-recalc-deploy.sql.

        Прогон дёшев: обычно needsRecalc = 1 у нуля-единиц акций; ActionRecalculate
        идемпотентен, повторный досчёт открытой прямо сейчас акции безвреден.
        TOP (1000) — предохранитель от аварийного разрастания набора.
    */

    DECLARE @actionID INT, @done INT = 0, @failed INT = 0, @p DECIMAL(18,2),
            @firstErr NVARCHAR(2048) = NULL;

    -- у удалённых акций тоталы никого не интересуют — просто гасим флаг
    UPDATE dbo.[Action] SET needsRecalc = 0 WHERE needsRecalc = 1 AND deleteDate IS NOT NULL;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT TOP (1000) actionID
        FROM dbo.[Action]
        WHERE needsRecalc = 1 AND deleteDate IS NULL
        ORDER BY actionID;

    OPEN cur;
    FETCH NEXT FROM cur INTO @actionID;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            EXEC dbo.ActionRecalculate @actionID = @actionID, @totalPrice = @p OUTPUT;
            SET @done += 1;
        END TRY
        BEGIN CATCH
            SET @failed += 1;
            IF @firstErr IS NULL
                SET @firstErr = CONCAT(N'actionID ', @actionID, N': ', ERROR_MESSAGE());
        END CATCH
        FETCH NEXT FROM cur INTO @actionID;
    END
    CLOSE cur;
    DEALLOCATE cur;

    SELECT @done AS recalculated, @failed AS failed, @firstErr AS first_error;

    IF @failed > 0
        RAISERROR(N'job_RecalcPendingActions: %d акций не досчитано. Первая ошибка: %s', 16, 1, @failed, @firstErr);
END
