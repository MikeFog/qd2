-- Свойства спонсорского прайс-листа (iEntity 12): убрано поле «Начало вещания».
-- Поле было на форме и меняло значение, но sponsorPLIUD колонку не пишет (Deploy/30, SQL-BS-02), так что ввод молча терялся.
-- У обычного прайс-листа (iEntity 80) то же поле закомментировано в паспорте раньше — делаем так же, в том же виде.
-- Колонка SponsorProgramPricelist.broadcastStart остаётся (00:00), процедуры не меняются: broadcastStart закрыт на шаге 3.
SET NOCOUNT ON;
GO

DECLARE @old nvarchar(max) = N'<field caption="Начало вещания:" name="broadcastStart" default="0"/>';
DECLARE @new nvarchar(max) = N'<!--field caption="Начало вещания:" name="broadcastStart" default="0"/-->';
DECLARE @p nvarchar(max) = (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 12);

IF @p IS NULL
	RAISERROR(N'89: у iEntity 12 нет паспорта.', 16, 1);
ELSE IF CHARINDEX(@old, @p) > 0
	UPDATE dbo.iEntity SET passport = REPLACE(@p, @old, @new) WHERE entityID = 12;
GO

IF (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 12) LIKE N'%<!--field caption="Начало вещания:"%'
	AND (SELECT CAST(passport AS nvarchar(max)) FROM dbo.iEntity WHERE entityID = 12) LIKE N'%name="bonus"%'
	PRINT N'ГОТОВО: в свойствах спонсорского прайс-листа нет поля «Начало вещания».';
ELSE
	RAISERROR(N'89: паспорт iEntity 12 не изменён.', 16, 1);
GO
