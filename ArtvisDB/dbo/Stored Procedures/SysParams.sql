-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	Получить системные настройки
-- =============================================
CREATE procedure [dbo].[SysParams] (@languageCode VARCHAR(10) = 'ru') -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
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
		@tSysParams as [name]

end
