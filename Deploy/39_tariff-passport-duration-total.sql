-- Паспорт тарифа: «Полная продолжительность» — на вкладку «Общие», сразу под «Продолжительность» (предложение Миши 12.10.2026:
-- полная продолжительность становится обязательной и важной, а на вкладке «DJin» её не видно). Три паспорта:
--   iEntity 81 «Тариф» (Свойства, Добавить тариф), iPassport TariffMass («Добавить тариф массово»),
--   iPassport TariffMassEdit («Изменить похожие тарифы»).
-- Правка точечная: строка поля переносится, остальной текст паспорта не меняется. Десктоп и веб читают те же паспорта.
-- Только метаданные, клиент не нужен; после наката перезапустить qd2 и веб (паспорта читаются при старте). Идемпотентен:
-- повторный запуск напишет «уже перенесено». Если паспорт на базе не такой, как ожидалось, — «Остановлено», ничего не изменено.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 39_tariff-passport-duration-total.sql

SET NOCOUNT ON;
GO
SET XACT_ABORT ON;

DECLARE @fld  nvarchar(200) = N'<field caption="Полная продолжительность:" name="duration_total"/>';
DECLARE @dur  nvarchar(100) = N'name="duration"/>';
DECLARE @djin nvarchar(100) = N'caption="DJin"';

-- Все поиски и замены — в двоичной колляции: в колляции базы символ CR при сравнении игнорируется. Концы строк в паспортах
-- смешанные (CRLF и LF), поэтому строка поля вырезается вместе со своим переводом строки и отступом и вставляется как есть.
DECLARE @p TABLE (k nvarchar(50) PRIMARY KEY, p nvarchar(max) COLLATE Latin1_General_BIN2,
    posFld int, posDur int, posDjin int, lf int, cut int, seg nvarchar(400) COLLATE Latin1_General_BIN2, np nvarchar(max) COLLATE Latin1_General_BIN2);
INSERT @p (k, p) SELECT N'iEntity 81', CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 81;
INSERT @p (k, p) SELECT N'iPassport ' + codeName, passport FROM dbo.iPassport WHERE codeName IN ('TariffMass', 'TariffMassEdit');
UPDATE @p SET posFld = CHARINDEX(@fld, p), posDur = CHARINDEX(@dur, p), posDjin = CHARINDEX(@djin, p);

IF (SELECT COUNT(*) FROM @p) <> 3
BEGIN
    RAISERROR(N'Остановлено: не найден один из паспортов (iEntity 81, iPassport TariffMass, TariffMassEdit). Ничего не изменено.', 16, 1);
    RETURN;
END

-- уже перенесено: поле стоит до вкладки «DJin» во всех трёх
IF NOT EXISTS (SELECT 1 FROM @p WHERE posFld = 0 OR posFld > posDjin)
BEGIN
    PRINT N'Полная продолжительность уже на вкладке «Общие» во всех трёх паспортах — ничего не делаем.';
    RETURN;
END

-- строка поля: от перевода строки перед ним (LF, с CR перед ним, если есть) до конца тега
UPDATE @p SET lf = CASE WHEN posFld > 1 AND CHARINDEX(NCHAR(10), REVERSE(LEFT(p, posFld - 1))) > 0
                        THEN posFld - CHARINDEX(NCHAR(10), REVERSE(LEFT(p, posFld - 1))) ELSE 0 END;
UPDATE @p SET cut = CASE WHEN lf > 1 AND SUBSTRING(p, lf - 1, 1) = NCHAR(13) THEN lf - 1 ELSE lf END;
UPDATE @p SET seg = SUBSTRING(p, cut, posFld + LEN(@fld) - cut) WHERE lf > 0;

-- ожидаемый вид: поле и «Продолжительность» — по одному разу, «Продолжительность» на «Общие», поле на «DJin»
-- отдельной строкой (перед ним на строке только отступ)
IF EXISTS (SELECT 1 FROM @p
           WHERE (DATALENGTH(p) - DATALENGTH(REPLACE(p, @fld, N''))) / DATALENGTH(@fld) <> 1
              OR (DATALENGTH(p) - DATALENGTH(REPLACE(p, @dur, N''))) / DATALENGTH(@dur) <> 1
              OR posDjin = 0 OR posDur > posDjin OR posFld < posDjin OR lf = 0
              OR REPLACE(REPLACE(SUBSTRING(p, lf + 1, posFld - lf - 1), NCHAR(9), N''), N' ', N'') <> N'')
BEGIN
    RAISERROR(N'Остановлено: паспорт тарифа на этой базе не такой, как ожидалось (поле «Полная продолжительность» не найдено на вкладке «DJin» отдельной строкой). Ничего не изменено.', 16, 1);
    RETURN;
END

-- вырезать строку поля (она после «Продолжительность», позиция той не сдвигается) и вставить её сразу после «Продолжительность»
UPDATE @p SET np = STUFF(STUFF(p, cut, LEN(seg), N''), posDur + LEN(@dur), 0, seg);

BEGIN TRANSACTION;

UPDATE e SET passport = x.np FROM dbo.iEntity e JOIN @p x ON x.k = N'iEntity 81' WHERE e.entityID = 81;
UPDATE ip SET passport = x.np FROM dbo.iPassport ip JOIN @p x ON x.k = N'iPassport ' + ip.codeName WHERE ip.codeName IN ('TariffMass', 'TariffMassEdit');

-- проверка: поле один раз, сразу после «Продолжительность», до вкладки «DJin»
IF EXISTS (SELECT 1 FROM (
        SELECT CAST(passport AS nvarchar(max)) COLLATE Latin1_General_BIN2 p FROM dbo.iEntity WHERE entityID = 81
        UNION ALL SELECT CAST(passport AS nvarchar(max)) COLLATE Latin1_General_BIN2 FROM dbo.iPassport WHERE codeName IN ('TariffMass', 'TariffMassEdit')) v
    WHERE (DATALENGTH(p) - DATALENGTH(REPLACE(p, @fld, N''))) / DATALENGTH(@fld) <> 1
       OR CHARINDEX(@fld, p) < CHARINDEX(@dur, p)
       OR CHARINDEX(@fld, p) > CHARINDEX(@djin, p)
       OR CHARINDEX(N'Полная продолжительность', p) = 0)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(N'Остановлено: после правки поле стоит не там — всё откачено, ничего не изменено.', 16, 1);
    RETURN;
END

COMMIT TRANSACTION;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT). «Полная продолжительность» — на вкладке «Общие» в трёх паспортах тарифа.';
GO
