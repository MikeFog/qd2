-- Переводы видимого текста веб-версии. docs/tasks/web-i18n.md.
-- Ключ — сам русский текст (как в gettext): подписи метаданных, captions паспортов и
-- фильтров, тексты iMessage, строки кода (Tr.T). Десктоп таблицу не читает.
-- В первичном ключе — хэш текста: сам текст (до 4000) не помещается в 900 байт ключа.
CREATE TABLE [dbo].[iTranslation] (
    [lang]       VARCHAR (10)    NOT NULL,
    [context]    VARCHAR (32)    CONSTRAINT [DF_iTranslation_context] DEFAULT ('') NOT NULL,
    [source]     NVARCHAR (4000) NOT NULL,
    [text]       NVARCHAR (4000) NOT NULL,
    [sourceHash] AS (CONVERT([binary](32), hashbytes('SHA2_256', [source]))) PERSISTED NOT NULL,
    CONSTRAINT [PK_iTranslation] PRIMARY KEY CLUSTERED ([lang] ASC, [context] ASC, [sourceHash] ASC)
);
