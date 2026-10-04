/*
    ДЕПЛОЙ: удаление процедуры ComboModuleActionModulesRetrieve.

    ЗАЧЕМ   С правкой Д-7 (04.10.2026, docs/action-forms.md §10) строки грида «Размещение
            модулями» строит сама форма - из выпусков (ComboModuleIssuesRetrieve) и кампаний
            акции. Процедуру вызывал только удалённый ComboModule.LoadActionModules; в
            метаданных (iStoredProcedure) её нет, других объектов-зависимостей нет.

    КЛИЕНТ  Удалять ПОСЛЕ раскладки нового Merlin.exe: старый клиент вызывает процедуру при
            открытии «Размещения модулями» из карточки акции с ответом «Нет».

    ИДЕМПОТЕНТНОСТЬ повторный запуск ничего не меняет.
    ЗАПУСК  скрипт применяется сразу (COMMIT в конце); перед запуском - резервная копия.
            sqlcmd -S <сервер>\<инстанс> -d <база> -E -f 65001 -I -b -i combo-module-action-modules-drop-deploy.sql
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;

-- защита: процедуру не должен вызывать никто, кроме удалённого клиентского метода
IF EXISTS (SELECT 1 FROM sys.sql_expression_dependencies WHERE referenced_entity_name = 'ComboModuleActionModulesRetrieve')
   OR EXISTS (SELECT 1 FROM dbo.iStoredProcedure WHERE name = 'ComboModuleActionModulesRetrieve')
BEGIN
    RAISERROR(N'Остановлено: на ComboModuleActionModulesRetrieve есть ссылки (объекты базы или iStoredProcedure).', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

IF OBJECT_ID('dbo.ComboModuleActionModulesRetrieve', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.ComboModuleActionModulesRetrieve;
    PRINT N'Процедура ComboModuleActionModulesRetrieve удалена.';
END
ELSE
    PRINT N'Процедуры ComboModuleActionModulesRetrieve нет - удалять нечего.';

COMMIT TRANSACTION;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT).';
GO
