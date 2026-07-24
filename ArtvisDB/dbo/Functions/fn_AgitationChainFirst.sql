-- Первое окно цепочки связанных окон (windowPrevId); для одиночного окна - оно само.
-- Используется авто-обвязкой политической агитации (AgitationFraming).
CREATE FUNCTION [dbo].[fn_AgitationChainFirst] (@windowID int)
RETURNS int
AS
BEGIN
	DECLARE @first int = @windowID, @prev int
	WHILE 1 = 1
	BEGIN
		SELECT @prev = windowPrevId FROM TariffWindow WHERE windowId = @first
		IF @prev IS NULL BREAK
		SET @first = @prev
	END
	RETURN @first
END
