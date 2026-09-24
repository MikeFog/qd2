/*
    ДЕПЛОЙ: хранилище переводов веб-версии (многоязычность, этап 2).
    Задача: docs/tasks/web-i18n.md.

    ЧТО ДЕЛАЕТ
      1. Таблица iTranslation(lang, context, source, text) — перевод по русскому тексту.
      2. Процедура TranslationLoad @lang — все переводы языка (веб грузит их в память).
      3. Переносит готовые испанские названия пунктов меню из iMenu.name_es в iTranslation.
         Колонка iMenu.name_es и процедуры меню НЕ меняются: десктоп и Protector работают как раньше.

    КЛИЕНТ          десктоп не затронут. Веб после наката — перезапустить (переводы кэшируются).
    ИДЕМПОТЕНТНОСТЬ повторный запуск безопасен (перенос не перезаписывает уже заданные переводы).
    ОТКАТ           DROP PROCEDURE dbo.TranslationLoad; DROP TABLE dbo.iTranslation.
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
-- sqlcmd по умолчанию создаёт процедуры с QUOTED_IDENTIFIER OFF — задаём явно
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
BEGIN
    CREATE TABLE [dbo].[iTranslation] (
        [lang]       VARCHAR (10)    NOT NULL,
        [context]    VARCHAR (32)    CONSTRAINT [DF_iTranslation_context] DEFAULT ('') NOT NULL,
        [source]     NVARCHAR (4000) NOT NULL,
        [text]       NVARCHAR (4000) NOT NULL,
        [sourceHash] AS (CONVERT([binary](32), hashbytes('SHA2_256', [source]))) PERSISTED NOT NULL,
        CONSTRAINT [PK_iTranslation] PRIMARY KEY CLUSTERED ([lang] ASC, [context] ASC, [sourceHash] ASC)
    );
    PRINT 'Создана таблица iTranslation';
END
GO

CREATE OR ALTER PROCEDURE [dbo].[TranslationLoad]
(
    @lang VARCHAR(10)
)
AS
SET NOCOUNT ON;

SELECT [context], [source], [text]
FROM   [dbo].[iTranslation]
WHERE  [lang] = @lang;
GO
GRANT EXECUTE ON OBJECT::[dbo].[TranslationLoad] TO PUBLIC AS [dbo];
GO

-- Меню: name_es → iTranslation. Разделители «-» и совпадающие с русским не переносим;
-- если у одного русского названия разные испанские — берём любое (меньшее).
BEGIN TRANSACTION;

INSERT INTO [dbo].[iTranslation] ([lang], [context], [source], [text])
SELECT 'es', '', m.[name], MIN(m.[name_es])
FROM   [dbo].[iMenu] m
WHERE  m.[name_es] IS NOT NULL
  AND  LTRIM(RTRIM(m.[name_es])) <> ''
  AND  m.[name] <> '-'
  AND  m.[name_es] <> m.[name] COLLATE Latin1_General_BIN
  AND  NOT EXISTS (SELECT 1 FROM [dbo].[iTranslation] t
                   WHERE t.[lang] = 'es' AND t.[context] = ''
                     AND t.[sourceHash] = CONVERT(binary(32), hashbytes('SHA2_256', m.[name])))
GROUP BY m.[name];

PRINT CONCAT('Перенесено переводов меню: ', @@ROWCOUNT);

COMMIT TRANSACTION;
GO
