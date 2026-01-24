CREATE	PROC [dbo].[DeleteUnusedDummyRollers]
AS
Declare @count int

Delete 
	r
From 
	Roller r Left Join Issue i on r.rollerID = i.rollerID
Where 
	isMute=1
	And i.issueID is Null

Set @count = @@ROWCOUNT

insert into Announcement (toUserID, fromUserID, [subject]) 
Select userID, 0, 'Удалено неиспользуемых роликов-пустышек: ' + Str(@count) From [User] Where IsAdmin = 1 

delete 
	r
From 
	Roller r Left Join Issue i on r.rollerID = i.rollerID
Where 
	r.parentID Is Not Null
	And i.issueID is Null
 
Set @count = @@ROWCOUNT

insert into Announcement (toUserID, fromUserID, [subject]) 
Select userID, 0, 'Удалено неиспользуемых роликов-клонов: ' + Str(@count) From [User] Where IsAdmin = 1 