-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 28.10.2008
-- Description:	Получить пустой ролик
-- =============================================
CREATE procedure [dbo].[GetMuteRoller]
(
@duration timeDuration,
@rollerID int = null out,
@firmID int,
@advertTypeID int = null,
@withShow bit = 1
)
as 
begin 
	set nocount on;

Select @rollerID = r.rollerID From Roller r 
Where r.firmID = @firmID And r.duration = @duration And r.isMute = 1 And IsNull(r.advertTypeID, 0) = IsNull(@advertTypeID, 0)
--And Exists(Select 1 From Issue i Where i.rollerID = r.rollerID And i.campaignID = @campaignID)

If @rollerID Is Null
Begin
	-- всегда теперь создаём новый ролик-пустышку и присваиваем конкретной фирме
	declare @name nvarchar(64)
		
	set @name = 'Ролик - ' + dbo.fn_Int2Time(@duration)
	insert into Roller ([name],	duration,isEnabled,rolActionTypeID,isCommon,isMute, firmID, advertTypeID) 
	values (@name,@duration, 1, 1, 0, 1, @firmID, @advertTypeID) 
		
	set @rollerID = scope_identity()
End	
	if @withShow = 1
	begin 
		SELECT 
			r.*, 
			dbo.fn_Int2Time(r.[duration]) as durationString, 
			null as firmName
		FROM 
			[Roller] r
		where r.rollerID = @rollerID
	end 
end
