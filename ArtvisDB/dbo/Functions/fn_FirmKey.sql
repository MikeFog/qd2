-- Ключ для сравнения реквизитов фирмы (ИНН, КПП, счёт): без пробелов и знаков препинания, чтобы
-- '333-555-6666', '333 555 6666' и '3335556666' считались одним значением. Хранится в базе то, что ввёл
-- пользователь; ключ нужен только для проверки дублей (FirmIUD, sp_CheckFirmINN).
-- Заглушки из одних знаков ('-', '.', '...') ключом не стираются (без пробелов остаются как есть),
-- иначе все фирмы с заглушкой стали бы дублями друг друга.
CREATE FUNCTION [dbo].[fn_FirmKey] (@value VARCHAR(20))
RETURNS VARCHAR(20)
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @noSpaces VARCHAR(20) =
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@value, ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), '');

    DECLARE @key VARCHAR(20) =
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@noSpaces,
            '-', ''), '.', ''), ',', ''), '/', ''), '\', ''), '_', ''), '(', ''), ')', ''), '+', ''), ':', ''), ';', ''), '*', '');

    RETURN CASE WHEN @key = '' THEN @noSpaces ELSE @key END;
END
