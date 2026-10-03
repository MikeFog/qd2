/*
    ДЕПЛОЙ: текст отказа UIX_Campaign без «Операция прервана.»

    ЗАЧЕМ   Сообщение показывается, когда кампания такого типа, с таким типом оплаты,
            станцией и агентством уже есть в акции. С 03.10.2026 «Добавить рекламную
            кампанию» пишет станции по одной (отказ по одной не мешает остальным), и
            массовая смена типа оплаты тоже идёт по кампаниям - операция целиком не
            прерывается, хвост «Операция прервана.» вводил в заблуждение.

    ЧТО     iMessage.UIX_Campaign - новый русский текст;
            iTranslation (es) - перевод под новым текстом (ключ перевода - сам русский
            текст), строка старого текста удаляется. Источник правды испанского -
            ArtvisDB/Scripts/i18n/es.tsv (web-i18n-es-seed.sql уже пересобран).

    ИДЕМПОТЕНТНОСТЬ повторный запуск ничего не меняет.
    КЛИЕНТ  qd2 и веб перезапустить: словарь iMessage и переводы читаются при старте.
    ЗАПУСК  скрипт применяется сразу (COMMIT в конце); перед запуском - резервная копия.
            sqlcmd -S <сервер>\<инстанс> -d <база> -E -f 65001 -I -b -i uix-campaign-message-deploy.sql
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

DECLARE @old NVARCHAR(4000) = N'Рекламная кампания такого типа, с таким типом оплаты и радиостанцией уже присутствует в данной рекламной акции. Операция прервана.';
DECLARE @new NVARCHAR(4000) = N'Рекламная кампания такого типа, с таким типом оплаты и радиостанцией уже присутствует в данной рекламной акции.';
DECLARE @es  NVARCHAR(4000) = N'Ya existe en esta campaña publicitaria una pauta de este tipo, con este tipo de pago y esta emisora.';

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'UIX_Campaign')
BEGIN
    RAISERROR(N'Остановлено: в iMessage нет ключа UIX_Campaign.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

UPDATE dbo.iMessage SET message = @new WHERE name = 'UIX_Campaign' AND message <> @new;
PRINT CONCAT(N'iMessage обновлено строк: ', @@ROWCOUNT);

-- переводы есть только там, где накачена многоязычность веба (web-i18n-translation-deploy.sql)
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
BEGIN
    DELETE FROM dbo.iTranslation
    WHERE lang = 'es' AND context = '' AND sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', @old));
    PRINT CONCAT(N'iTranslation удалено старых строк: ', @@ROWCOUNT);

    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @new AS [source], @es AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
    PRINT CONCAT(N'iTranslation вставлено или обновлено: ', @@ROWCOUNT);
END

COMMIT TRANSACTION;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT). Перезапустить qd2 и веб.';
GO
