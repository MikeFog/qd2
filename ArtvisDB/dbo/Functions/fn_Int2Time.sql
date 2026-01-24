CREATE  FUNCTION [dbo].[fn_Int2Time]
(
@duration int
)
RETURNS VARCHAR(8)
AS
BEGIN

DECLARE @hour smallint, @min smallint, @sec smallint, @sHour varchar(10),
				@sSec varchar(2), @sMin varchar(2), @secus varchar(1)

IF (@duration < 0)	
	SET @secus = '-'
ELSE 
	SET @secus = ''
				
SET @duration = ABS(@duration)	

SET @hour = @duration / 3600
SET @min = (@duration % 3600) / 60
SET @sec = @duration % 60

IF	@hour > 9 SET @sHour = RTRIM(@hour)
ELSE SET @sHour = '0' + RTRIM(@hour)

IF	@min > 9 SET @sMin = RTRIM(@min)
ELSE SET @sMin = '0' + RTRIM(@min)

IF	@sec > 9 SET @sSec = RTRIM(@sec)
ELSE SET @sSec = '0' + RTRIM(@sec)

RETURN @secus + @sHour + ':' + @sMin + ':' + @sSec
END
