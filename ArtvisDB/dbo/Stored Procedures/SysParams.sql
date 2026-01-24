-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	Получить системные настройки
-- =============================================
CREATE procedure [dbo].[SysParams] 
as 
begin 
	set nocount on;
    
    select 
		dbo.f_SysParamsDaysLog() as daysLog, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'DaysHistorySave'), 365) as daysHistorySave, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'DeletedActionsLifetime'), 365) as DeletedActionsLifetime, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'UnconfirmedActionsLifetime'), 365) as UnconfirmedActionsLifetime, 
		'Системные настройки' as [name]

end
