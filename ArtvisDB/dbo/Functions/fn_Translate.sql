-- Перевод видимого текста для веба (docs/tasks/web-i18n.md): ключ — русский текст, как у Tr.T в C#.
-- Язык 'ru' или NULL — исходный текст без обращения к таблице (десктоп процедурам язык не передаёт).
-- Вызывать один раз в переменную в начале процедуры, а не в каждой строке результата.
CREATE FUNCTION [dbo].[fn_Translate]
(
    @lang   VARCHAR(10),
    @source NVARCHAR(4000)
)
RETURNS NVARCHAR(4000)
AS
BEGIN
    IF @lang IS NULL OR @lang = 'ru'
        RETURN @source;

    RETURN COALESCE(
        (SELECT [text]
         FROM   [dbo].[iTranslation]
         WHERE  [lang] = @lang
           AND  [context] = ''
           AND  [sourceHash] = CONVERT(binary(32), hashbytes('SHA2_256', @source))),
        @source);
END
