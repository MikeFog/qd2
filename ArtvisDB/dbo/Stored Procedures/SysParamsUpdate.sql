-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	Обновить параметры системы
-- =============================================
CREATE procedure [dbo].[SysParamsUpdate] 
(
	@daysLog tinyint = null,
	@daysHistorySave smallint = null,
	@deletedActionsLifetime smallint,
	@unconfirmedActionsLifetime smallint,
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
end
