-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	╧юыєўшЄ№ эрёЄЁющъє ю ёююс∙хэшш юс єфрыхээ√ї т√яєёърї
-- =============================================
CREATE FUNCTION f_SysParamsDaysLog
(
)
returns tinyint 
AS
BEGIN
	declare @daysLog tinyint 
    select top 1 @daysLog = cast([value] as int) from iInternalVariable where [name] = 'DaysLog'
	return coalesce(@daysLog, 5)
END
