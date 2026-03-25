CREATE FUNCTION [dbo].[fn_IsAcceptRatioForUser]
(
	@userID INT,
	@ratio decimal(18,10),
	@startDate datetime,
	@finishDate datetime 
)
RETURNS BIT
AS
BEGIN
	IF dbo.[f_IsAdmin](@userID) = 1
			return 1

	DECLARE @maxratio decimal(18,10)
	select @startDate = dbo.ToShortDate(@startDate), @finishDate = dbo.ToShortDate(@finishDate)
	SELECT @maxratio = dbo.fn_GetMaxUserDiscount(@userID, @startDate, @finishDate)

	IF @maxratio - @ratio < 0.002
		return 1
	return 0
END