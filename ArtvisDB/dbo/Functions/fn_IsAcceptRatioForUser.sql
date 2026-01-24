CREATE FUNCTION [dbo].[fn_IsAcceptRatioForUser]
(
	@userID INT,
	@ratio float,
	@startDate datetime,
	@finishDate datetime 
)
RETURNS BIT
AS
BEGIN
	DECLARE @maxratio float

	if @userID is not null
	begin 
		IF dbo.[f_IsAdmin](@userID) = 1
			return 1

		select @startDate = dbo.ToShortDate(@startDate), @finishDate = dbo.ToShortDate(@finishDate)

		select @maxratio = max(ud.maxRatio) 
		from UserDiscount ud 
		where ud.userID = @userID
			and ud.startDate <= @finishDate
				and ud.finishDate >= @startDate
		
		if @maxratio is null or 
			exists( -- Нету промежутков между датами
				select ud.startDate, ud.finishDate
				from UserDiscount ud
					left join UserDiscount ud2 on ud.userID = ud2.userID
						and ud2.startDate > ud.finishDate
				where ud.userID = @userID
					and ud.startDate <= @finishDate
						and ud.finishDate >= @startDate
				group by ud.startDate, ud.finishDate
				having datediff(day, ud.finishDate, min(ud2.startDate)) > 1)
			or not exists( -- Нет скидки на дату начала
				select * from UserDiscount ud 
					where  ud.userID = @userID
						and @startDate between ud.startDate and ud.finishDate
			)
			or not exists( -- Нет скидки на дату окончания
				select * from UserDiscount ud 
					where  ud.userID = @userID
						and @finishDate between ud.startDate and ud.finishDate
			)
			set @maxratio = 1
		
		IF @maxratio - @ratio < 0.005
			return 1
	end 
	
	return 0
END
