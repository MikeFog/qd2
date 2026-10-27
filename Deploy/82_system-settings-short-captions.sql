-- Deploy/82: короткие подписи в паспорте сущности 214 «Системные настройки».
-- Левая колонка паспорта ~240 px; длинные подписи переносились на 2–3 строки и обрезались
-- (хвост «(дни до сегодняшнего дня):» не виден). Меняется только текст caption в iEntity.passport
-- и добавляются испанские переводы новых подписей. Имена полей (name) не меняются.
-- Идемпотентен. Можно до и после Deploy/81. После наката перезапустить qd2/Протектор и веб.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @map TABLE (old nvarchar(200), new nvarchar(200), es nvarchar(200));
INSERT @map VALUES
 (N'Уведомление об удаленных выпусках (дни до сегодняшнего дня):', N'Удалённые выпуски, дней:',      N'Emisiones eliminadas, días:'),
 (N'Срок хранения истории (в днях):',                               N'Хранение истории, дней:',        N'Conservación del historial, días:'),
 (N'Срок хранения удалённых акций (в днях):',                       N'Хранение удал. акций, дней:',    N'Conserv. de campañas elim., días:'),
 (N'Срок хранения макетов акций (в днях):',                         N'Хранение макетов, дней:',        N'Conservación de borradores, días:'),
 (N'Дата активации нового алгоритма пакетной скидки:',              N'Новая пакетная скидка с даты:',  N'Nuevo desc. por paquete desde:'),
 (N'Реестр счетов: рабочих дней до эфира для оранжевого сигнала:',  N'Счета: оранжевый, раб. дней:',   N'Facturas: naranja, días háb.:');

IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214)
BEGIN
    RAISERROR(N'Сущности 214 «Системные настройки» нет — паспорт не правится.', 16, 1);
    RETURN;
END

DECLARE @p nvarchar(max) = (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 214);
DECLARE @orig nvarchar(max) = @p;

SELECT @p = REPLACE(@p, N'caption="' + old + N'"', N'caption="' + new + N'"') FROM @map;

IF @p <> @orig
BEGIN
    UPDATE dbo.iEntity SET passport = @p WHERE entityID = 214;
    PRINT N'Подписи паспорта 214 сокращены.';
END
ELSE
    PRINT N'Подписи паспорта 214 уже короткие (или паспорт другой) — не менялось.';

SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, new AS [source], es AS [text] FROM @map) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO

IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%caption="Хранение истории, дней:"%')
    PRINT N'ГОТОВО: подписи «Системных настроек» сокращены';
ELSE
    PRINT N'ОШИБКА: подписи не изменены';
GO
