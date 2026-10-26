-- «Реестр счетов»: настройка «за сколько рабочих дней до эфира» в «Протектор → Администрирование → Настройки...».
-- Оранжевый кружок («Внимание») у активированной, но не оплаченной акции появляется за N РАБОЧИХ дней до её первого дня
-- (суббота и воскресенье не считаются; праздничного календаря в системе нет). N хранится в iInternalVariable,
-- имя 'BillUnpaidWarnDays'; не задана — 4. Читает f_SysParamsBillUnpaidWarnDays (Deploy/80, BillRegister).
--
-- Что меняется:
--   1. SysParams — новая колонка billUnpaidWarnDays (значение или 4);
--   2. SysParamsUpdate — новый необязательный параметр @billUnpaidWarnDays (NULL — настройка удаляется, действует 4);
--   3. паспорт сущности 214 «Системные настройки» — поле «Реестр счетов: рабочих дней до эфира для оранжевого сигнала».
-- Без этого скрипта реестр работает с N = 4; поле в «Настройках...» появится только после него.
--
-- Порядок: после 76 (SysParams и SysParamsUpdate с датой пакетной скидки; без неё скрипт отказывается: «Ничего не
-- изменено») и после 80 (f_SysParamsBillUnpaidWarnDays). Не перезапускать после 81: 76, 08 и ArtvisDB/Scripts
-- web-i18n-sql-deploy.sql (несут старые тексты SysParams / SysParamsUpdate); если перезапустили — повторить 81.
-- Идемпотентен (CREATE OR ALTER, IF NOT EXISTS). Ожидается «ГОТОВО». После наката перезапустить Протектор (паспорт
-- читается при старте); клиент qd2 и веб не нужны.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 81_bill-unpaid-warn-days.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID(N'dbo.f_SysParamsBillUnpaidWarnDays') IS NULL
   OR OBJECT_ID(N'dbo.f_SysParamsPackageDiscountMaxBasisFrom') IS NULL
   OR CHARINDEX(N'packageDiscountMaxBasisFrom', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.SysParamsUpdate')), N'')) = 0
BEGIN
    RAISERROR(N'81: сначала 76 (дата пакетной скидки в SysParams) и 80 (f_SysParamsBillUnpaidWarnDays). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO

-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	Получить системные настройки
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SysParams] (@languageCode VARCHAR(10) = 'ru') -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
as 
begin 
	set nocount on;
	DECLARE @tSysParams NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Системные настройки');
    
    select 
		dbo.f_SysParamsDaysLog() as daysLog, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'DaysHistorySave'), 365) as daysHistorySave, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'DeletedActionsLifetime'), 365) as DeletedActionsLifetime, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'UnconfirmedActionsLifetime'), 365) as UnconfirmedActionsLifetime, 
		-- в паспорте у даты нет пустого значения (при «ОК» она всегда сохраняется): «не задана» показываем как 31.12.2099 = выключено
		coalesce(dbo.f_SysParamsPackageDiscountMaxBasisFrom(), '20991231') as packageDiscountMaxBasisFrom, 
		-- «Реестр счетов»: за сколько рабочих дней до эфира оранжевый кружок у не оплаченной акции (по умолчанию 4)
		dbo.f_SysParamsBillUnpaidWarnDays() as billUnpaidWarnDays, 
		@tSysParams as [name]

end
GO

-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	Обновить параметры системы
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SysParamsUpdate] 
(
	@daysLog tinyint = null,
	@daysHistorySave smallint = null,
	@deletedActionsLifetime smallint,
	@unconfirmedActionsLifetime smallint,
	@packageDiscountMaxBasisFrom datetime = null, -- NULL — настройка не задана (удаляется)
	@billUnpaidWarnDays smallint = null, -- NULL — настройка не задана (удаляется), действует 4
	@actionName varchar(16) = null 
)
as 
begin 
	set nocount on;

	if @actionName <> 'UpdateItem'
		return

    declare @PARAM_NAME_DAYS_LOG char(7)
	select @PARAM_NAME_DAYS_LOG = 'DaysLog'
	
	declare @PARAM_NAME_DAYS_HISTORY_SAVE char(15)
	select @PARAM_NAME_DAYS_HISTORY_SAVE = 'DaysHistorySave'
	
	declare @daysLogB binary(500), @daysHistorySaveB binary(500)
	if @daysLog is not null 
		set @daysLogB = cast ((@daysLog) as binary(500))
	else 
		set @daysLogB = cast (5 as binary(500))
		
	if @daysHistorySave is not null 
		set @daysHistorySaveB = cast ((@daysHistorySave) as binary(500))
	else 
		set @daysHistorySaveB = cast (365 as binary(500))
	
	if exists(select * from iInternalVariable where [name] = @PARAM_NAME_DAYS_LOG)
		update iInternalVariable set [value] = @daysLogB where [name] = @PARAM_NAME_DAYS_LOG
	else 
		insert into iInternalVariable ([name],[value]) 	values (@PARAM_NAME_DAYS_LOG,@daysLogB) 
	
	if exists(select * from iInternalVariable where [name] = @PARAM_NAME_DAYS_HISTORY_SAVE)
		update iInternalVariable set [value] = @daysHistorySaveB where [name] = @PARAM_NAME_DAYS_HISTORY_SAVE
	else 
		insert into iInternalVariable ([name],[value]) 	values (@PARAM_NAME_DAYS_HISTORY_SAVE,@daysHistorySaveB) 

	update iInternalVariable set [value] = Cast(@deletedActionsLifetime as binary(500)) where [name] = 'DeletedActionsLifetime'

	update iInternalVariable set [value] = Cast(@unconfirmedActionsLifetime as binary(500)) where [name] = 'UnconfirmedActionsLifetime'

	-- Дата начала акции, с которой «процент заполнения» пакетной скидки считается от максимальной длительности
	-- (hlp_ActionDiscountCalculate, pc_PackageDiscountCalculateModel); не задана — везде от средней
	if @packageDiscountMaxBasisFrom is null
		delete from iInternalVariable where [name] = 'PackageDiscountMaxBasisFrom'
	else if exists(select * from iInternalVariable where [name] = 'PackageDiscountMaxBasisFrom')
		update iInternalVariable set [value] = Cast(Cast(Cast(@packageDiscountMaxBasisFrom as date) as datetime) as binary(500)) where [name] = 'PackageDiscountMaxBasisFrom'
	else
		insert into iInternalVariable ([name],[value]) values ('PackageDiscountMaxBasisFrom', Cast(Cast(Cast(@packageDiscountMaxBasisFrom as date) as datetime) as binary(500)))

	-- «Реестр счетов»: за сколько рабочих дней до первого дня акции не оплаченная активированная акция получает оранжевый кружок
	-- (f_SysParamsBillUnpaidWarnDays, BillRegister); не задана — 4
	if @billUnpaidWarnDays is null
		delete from iInternalVariable where [name] = 'BillUnpaidWarnDays'
	else if exists(select * from iInternalVariable where [name] = 'BillUnpaidWarnDays')
		update iInternalVariable set [value] = Cast(@billUnpaidWarnDays as binary(500)) where [name] = 'BillUnpaidWarnDays'
	else
		insert into iInternalVariable ([name],[value]) values ('BillUnpaidWarnDays', Cast(@billUnpaidWarnDays as binary(500)))
end

GO

-- ===== паспорт сущности 214 «Системные настройки»: поле с числом дней =====
DECLARE @field nvarchar(400) = N'<field caption="Реестр счетов: рабочих дней до эфира для оранжевого сигнала:" name="billUnpaidWarnDays" type="int" min="1" max="30"/>';
IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214)
    RAISERROR(N'Сущности 214 «Системные настройки» нет — паспорт не правится.', 16, 1);
ELSE IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%name="billUnpaidWarnDays"%')
    PRINT N'Паспорт «Системных настроек» уже содержит поле дней.';
ELSE IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%</page>%')
BEGIN
    UPDATE dbo.iEntity
    SET passport = REPLACE(CAST(passport AS nvarchar(max)), N'</page>', NCHAR(9) + @field + NCHAR(13) + NCHAR(10) + NCHAR(9) + N'</page>')
    WHERE entityID = 214;
    PRINT N'В паспорт «Системных настроек» добавлено поле дней.';
END
ELSE
    RAISERROR(N'В паспорте 214 нет </page> — поле не добавлено.', 16, 1);

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (VALUES ('es', '', N'Реестр счетов: рабочих дней до эфира для оранжевого сигнала:',
                             N'Registro de facturas: días hábiles antes de la emisión para la señal naranja:')) AS src (lang, context, [source], [text])
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO
SET NOEXEC OFF;
GO
IF CHARINDEX(N'billUnpaidWarnDays', OBJECT_DEFINITION(OBJECT_ID(N'dbo.SysParams'))) > 0
   AND CHARINDEX(N'billUnpaidWarnDays', OBJECT_DEFINITION(OBJECT_ID(N'dbo.SysParamsUpdate'))) > 0
   AND EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%billUnpaidWarnDays%')
    PRINT N'ГОТОВО: настройка «рабочих дней до эфира» для реестра счетов (по умолчанию 4)';
ELSE
    PRINT N'ОШИБКА: что-то не накатилось';
GO
