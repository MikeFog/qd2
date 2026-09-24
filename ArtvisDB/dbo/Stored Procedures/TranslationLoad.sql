-- Все переводы одного языка — веб грузит их целиком в память (docs/tasks/web-i18n.md).
CREATE PROCEDURE [dbo].[TranslationLoad]
(
    @lang VARCHAR(10)
)
AS
SET NOCOUNT ON;

SELECT [context], [source], [text]
FROM   [dbo].[iTranslation]
WHERE  [lang] = @lang;
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[TranslationLoad] TO PUBLIC
    AS [dbo];
