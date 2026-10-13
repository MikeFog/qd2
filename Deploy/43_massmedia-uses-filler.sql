-- Станция: признак «Радиостанция использует добивщик» (MassMedia.usesFiller, вкладка «DJin настройки» паспорта станции).
--
-- Добивщик (Broadcast Schedule, schedule.exe) — отдельная программа: забирает файл выгрузки сетки, добавляет анонс/шифт/песню
-- в блоки и перед DJin убирает свои служебные метки (AudioBlock.save: строки «fake-in» пропускаются, c-type-4/5/9 → c).
-- Но работает он не со всеми станциями (Авторадио, Monte Carlo и др. — файл идёт прямо на передающий компьютер).
-- Теперь qd2 и веб пишут метки только в файл станций с признаком; у остальных файл чистый — то же, что получилось бы после добивщика
-- (BlockManager.Process(…, usesFiller)). Имя файла тоже изменено: «Станция (город)_дд_мм_гггг_с_добивщиком.txt» /
-- «…_без_добивщика.txt» вместо «…_дд_мм_гггг_0000-2359.txt» (broadcastStart у всех прайс-листов 00:00, время в имени — константа;
-- добивщик берёт из имени станцию, город и дату, остаток имени не разбирает).
--
-- Что делает скрипт:
--   1. колонка MassMedia.usesFiller bit NOT NULL DEFAULT 0 («по умолчанию нет» — для новых станций);
--   2. при ПЕРВОМ добавлении колонки — выставляет 1 станциям, чьё имя добивщик знает (GetAudioManager/process_file: Европа Плюс,
--      Дорожное радио, Русское радио, Ретро FM, Радио Любовь, Love Radio, Русский Хит, Радио Дача, Радио Шансон, Новое радио, Радио 7,
--      Бизнес ФМ, Хит FM, Гордость, Наше Радио): до наката их файл получал метки, и поведение остаётся прежним, пока вы не снимете
--      галочку. Остальным — 0 (добивщик их не обрабатывает вовсе: «Неизвестный тип радиостанции»). ЭТО ДОГАДКА ПО ИМЕНИ: какие
--      города реально настроены в config.json добивщика, видно только там — проверьте список в конце вывода и поправьте галочки
--      в паспорте станции. Повторный запуск галочки НЕ трогает;
--   3. vMassmedia и MassmediaIUD отдают/пишут колонку;
--   4. паспорт станции (iEntity 9): поле «Радиостанция использует добивщик» первой строкой вкладки «DJin настройки» + испанский.
--
-- Нужны новые Merlin.exe и веб (старый клиент пишет метки всем и старое имя файла; новый клиент на базе без скрипта — как старый).
-- После наката — перезапустить qd2 и веб (паспорта читаются при старте). Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 43_massmedia-uses-filler.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

-- =====================================================================
-- 1-2. Колонка и начальные значения (только при первом добавлении колонки)
-- =====================================================================
IF COL_LENGTH('dbo.MassMedia', 'usesFiller') IS NULL
BEGIN
    ALTER TABLE dbo.MassMedia ADD usesFiller bit NOT NULL CONSTRAINT DF_MassMedia_usesFiller DEFAULT (0);

    -- имена, которые знает добивщик (process_file): сравнение без регистра, по имени без пробелов по краям
    EXEC (N'UPDATE dbo.MassMedia SET usesFiller = 1
            WHERE LTRIM(RTRIM(name)) IN (N''Европа Плюс'', N''Дорожное радио'', N''Русское радио'', N''Ретро FM'', N''Радио Любовь'',
                N''Love Radio'', N''Русский Хит'', N''Радио Дача'', N''Радио Шансон'', N''Новое радио'', N''Радио 7'',
                N''Бизнес ФМ'', N''Хит FM'', N''Гордость'', N''Наше Радио'')');
    PRINT N'Колонка MassMedia.usesFiller добавлена, начальные значения выставлены по именам станций.';
END
ELSE
    PRINT N'Колонка MassMedia.usesFiller уже есть — значения не трогаем.';
GO

-- =====================================================================
-- 3. Вид и процедура станции
-- =====================================================================
CREATE OR ALTER VIEW [dbo].[vMassmedia]
AS
SELECT        mm.massmediaID, mm.roltypeID, mm.nationalBonus, mm.deadLine, mm.isCheckDeadline, mm.isActive, mm.rollerEnterPath, mm.rollerExitPath, mm.rollerEtcPath, mm.rollerPath, mm.rollerEnterMax, mm.rollerExitMax, 
                         mm.rollerEtcMax, mm.massmediaGroupID, mm.exportName, mm.rollerEnterMin, mm.rollerExitMin, mm.rollerEtcMin, mm.mediaPlusMassmediaID, mm.grammofonMistake, mm.name, mm.director, mm.prefix, mm.fullPrefix, 
                         mm.reportString, mmg.name AS groupName, mm.name + CASE WHEN mm.massmediaGroupID IS NULL THEN '' ELSE ' (' + mmg.Name + ')' END AS nameWithGroup, mm.certificateIssued, mm.volume_c, mm.volume_n,
                         mm.volume_p, mm.volume_m, mm.volume_j, mm.agitationLocalRollerID, mm.agitationAnnounceRollerID, mm.agitationFederalRollerID, mm.agitationExcludeIntervals, mm.usesFiller
FROM            dbo.MassMedia AS mm LEFT OUTER JOIN
                         dbo.MassmediaGroup AS mmg ON mm.massmediaGroupID = mmg.massmediaGroupID
GO

CREATE OR ALTER PROCEDURE [dbo].[MassmediaIUD]
(
@massmediaID smallint = NULL,
@name nvarchar(32) = NULL,
@prefix nvarchar(256) = NULL,
@roltypeID smallint = NULL,
@deadLine datetime = NULL,
@isActive bit = NULL,
@director nvarchar(256) = NULL,
@fullPrefix nvarchar(256) = NULL,
@reportString nvarchar(256) = NULL,
@actionName varchar(32),
@rollerEnterPath NVARCHAR(255) = NULL,
@rollerExitPath NVARCHAR(255) = NULL,
@rollerEtcPath NVARCHAR(255) = NULL,
@rollerPath NVARCHAR(255) = NULL,
@rollerEnterMax smallint = NULL,
@rollerExitMax smallint = NULL,
@rollerEtcMax smallint = null,
@rollerEnterMin smallint = NULL,
@rollerExitMin smallint = NULL,
@rollerEtcMin smallint = null,
@massmediaGroupID int = null,
@exportName nvarchar(255) = null,
@loggedUserID smallint,
@mediaPlusMassmediaID smallint = null,
@painting image = null,
@certificateIssued nvarchar(256),
@volume_c decimal(5,2),
@volume_n decimal(5,2),
@volume_p decimal(5,2),
@volume_m decimal(5,2),
@volume_j decimal(5,2),
@agitationLocalRollerID int = null,
@agitationAnnounceRollerID int = null,
@agitationFederalRollerID int = null,
@agitationExcludeIntervals nvarchar(256) = null,
@usesFiller bit = null
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on

if @mediaPlusMassmediaID = 0
	set @mediaPlusMassmediaID = null

-- Интервалы-исключения политической агитации: проверяем формат строки
-- (необязательные дни недели + ЧЧ:ММ-ЧЧ:ММ через ';'), см. fn_AgitationExcludeIntervals
if @actionName in ('AddItem', 'UpdateItem')
	and exists (select 1 from dbo.fn_AgitationExcludeIntervals(@agitationExcludeIntervals) where startMin is null)
begin
	raiserror('AgitationIntervalsInvalid', 16, 1)
	return
end

if @actionName in ('AddItem', 'UpdateItem') and @mediaPlusMassmediaID is not null
begin 
	if exists(select * from MassMedia mm where (@massmediaID is null or @massmediaID <> mm.massmediaID) and mm.mediaPlusMassmediaID = @mediaPlusMassmediaID)
	begin 
		raiserror('MassmediaWithSameMediaPlusIdExist', 16, 1)
		return
	end 
end 

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Massmedia](roltypeID, deadLine, isActive, rollerEnterPath, 
		rollerExitPath, rollerEtcPath, rollerPath, rollerEnterMax, rollerExitMax, rollerEtcMax, rollerEnterMin, rollerExitMin, rollerEtcMin, massmediaGroupID, 
		exportName,mediaPlusMassmediaID, [name], director, painting, prefix, [fullPrefix], [reportString], certificateIssued,
		volume_c, volume_n, volume_p, volume_m, volume_j,
		agitationLocalRollerID, agitationAnnounceRollerID, agitationFederalRollerID, agitationExcludeIntervals, usesFiller)
	VALUES(@roltypeID, @deadLine, @isActive, @rollerEnterPath, @rollerExitPath,
		@rollerEtcPath, @rollerPath, @rollerEnterMax, @rollerExitMax, @rollerEtcMax, @rollerEnterMin, @rollerExitMin, @rollerEtcMin, @massmediaGroupID,
		@exportName,@mediaPlusMassmediaID, @name, @director, @painting, @prefix, @fullPrefix, @reportString, @certificateIssued,
		@volume_c, @volume_n, @volume_p, @volume_m, @volume_j,
		@agitationLocalRollerID, @agitationAnnounceRollerID, @agitationFederalRollerID, @agitationExcludeIntervals, isnull(@usesFiller, 0))
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @massmediaID = SCOPE_IDENTITY()

	--Exec massmediaList @massmediaID = @massmediaID, @loggedUserID = @loggedUserID

	insert into UserMassmedia (userID,	massmediaID, canWork, canAdd) 
	values (@loggedUserID, @massmediaID, 1, 1) 

	select * from MassMedia where massmediaID = @massmediaID
END
ELSE IF @actionName = 'DeleteItem' begin
	DELETE FROM [Massmedia] WHERE massmediaID = @massmediaID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[Massmedia]
	SET
		[name] = @name,
		roltypeID = @roltypeID, 
		deadLine = @deadLine, 
		isActive = Coalesce(@isActive, isActive),
		rollerEnterPath = @rollerEnterPath, 
		rollerExitPath = @rollerExitPath, 
		rollerEtcPath = @rollerEtcPath, 
		rollerPath = @rollerPath, 
		rollerEnterMax = @rollerEnterMax, 
		rollerExitMax = @rollerExitMax, 
		rollerEtcMax = @rollerEtcMax,
		rollerEnterMin = @rollerEnterMin, 
		rollerExitMin = @rollerExitMin, 
		rollerEtcMin = @rollerEtcMin,
		massmediaGroupID = @massmediaGroupID, 
		exportName = @exportName,
		mediaPlusMassmediaID = @mediaPlusMassmediaID,
		director = @director,
		painting = @painting,
		prefix=@prefix,
		[fullPrefix] = @fullPrefix,
		[reportString] = @reportString,
		certificateIssued = @certificateIssued,
		volume_c = @volume_c,
		volume_n = @volume_n,
		volume_p = @volume_p,
		volume_m = @volume_m,
		volume_j = @volume_j,
		agitationLocalRollerID = @agitationLocalRollerID,
		agitationAnnounceRollerID = @agitationAnnounceRollerID,
		agitationFederalRollerID = @agitationFederalRollerID,
		agitationExcludeIntervals = @agitationExcludeIntervals,
		usesFiller = Coalesce(@usesFiller, usesFiller)
	WHERE
		massmediaID = @massmediaID

	Exec massmediaList @massmediaID = @massmediaID, @loggedUserID = @loggedUserID
END
GO

-- =====================================================================
-- 4. Паспорт станции (iEntity 9): поле — первой строкой вкладки «DJin настройки»
-- =====================================================================
DECLARE @tab  nvarchar(100) = N'<page caption="DJin настройки">';
DECLARE @name nvarchar(100) = N'name="usesFiller"';
DECLARE @p    nvarchar(max) = (SELECT CAST(passport AS nvarchar(max)) COLLATE Latin1_General_BIN2 FROM dbo.iEntity WHERE entityID = 9);
DECLARE @pos  int = CHARINDEX(@tab COLLATE Latin1_General_BIN2, @p);

IF @p IS NULL OR @pos = 0 OR (DATALENGTH(@p) - DATALENGTH(REPLACE(@p, @tab COLLATE Latin1_General_BIN2, N''))) / DATALENGTH(@tab) <> 1
    RAISERROR(N'Остановлено: в паспорте станции (iEntity 9) нет единственной вкладки «DJin настройки». Ничего не изменено.', 16, 1);
ELSE IF CHARINDEX(@name COLLATE Latin1_General_BIN2, @p) > 0
    PRINT N'Поле «Радиостанция использует добивщик» в паспорте станции уже есть.';
ELSE
BEGIN
    -- перевод строки после тега такой же, как в паспорте (CRLF или LF)
    DECLARE @at int = @pos + LEN(@tab);
    DECLARE @eol nvarchar(2) = CASE WHEN SUBSTRING(@p, @at, 2) = NCHAR(13) + NCHAR(10) THEN NCHAR(13) + NCHAR(10) ELSE NCHAR(10) END;
    DECLARE @np nvarchar(max) = STUFF(@p, @at, 0,
        @eol + NCHAR(9) + NCHAR(9) + N'<field caption="Радиостанция использует добивщик" name="usesFiller"/>'
             + @eol + NCHAR(9) + NCHAR(9) + N'<separator />');

    IF TRY_CAST(@np AS xml) IS NULL
        RAISERROR(N'Остановлено: после вставки поля XML паспорта стал невалидным. Ничего не изменено.', 16, 1);
    ELSE
    BEGIN
        UPDATE dbo.iEntity SET passport = @np WHERE entityID = 9;
        PRINT N'Поле «Радиостанция использует добивщик» добавлено в паспорт станции.';
    END
END
GO

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (VALUES ('es', '', N'Радиостанция использует добивщик', N'La emisora usa el rellenador de bloques')) AS src (lang, context, [source], [text])
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO

-- =====================================================================
-- Проверка
-- =====================================================================
IF COL_LENGTH('dbo.vMassmedia', 'usesFiller') IS NOT NULL
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.MassmediaIUD')) LIKE N'%@usesFiller%'
   AND (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 9) LIKE N'%name="usesFiller"%'
    PRINT N'ГОТОВО: признак «Радиостанция использует добивщик» — колонка, вид, процедура, паспорт.';
ELSE
    RAISERROR(N'43: признак применён не полностью (колонка/вид/процедура/паспорт).', 16, 1);
GO

-- Кому поставлена галочка (проверьте по config.json добивщика и поправьте в паспорте станции — вкладка «DJin настройки»)
SELECT m.massmediaID AS [№], m.name AS [Станция], g.name AS [Город/группа], m.isActive AS [Активна], m.usesFiller AS [Использует добивщик]
FROM dbo.MassMedia m
    LEFT JOIN dbo.MassmediaGroup g ON g.massmediaGroupID = m.massmediaGroupID
WHERE m.isActive = 1
ORDER BY m.usesFiller DESC, m.name, g.name;
GO
