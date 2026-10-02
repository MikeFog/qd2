-- Паспорт «Изменение продолжительности» (трафик-менеджмент, TrafficChangeDuration):
-- убраны серые поля «Продолжительность по тарифу» / «Полная продолжительность по тарифу».
-- Тариф искался по (цена + время) и после перебивки цены окон не находился — ошибка
-- «Не найден тариф для выполнения данной операции!». Процедура TariffWindowChangeDuration
-- тариф не использует, поля были только подсказкой. Нужен новый Merlin.exe.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
DECLARE @p nvarchar(max) = N'<passport>
	<page caption="Общие">
		<field caption="Текущее время выхода:" name="time" type="DF_TIME" disabled="true"/>
		<field caption="Новая продолжительность:" name="newduration" type="timeDuration"/>
		<field caption="Новая полная продолжительность:" name="newduration_total" type="timeDuration"/>
		<separator /> 
		<field caption="Начиная с:" name="startDate" type="DF_DATE"/>
		<field caption="Оканчивая:" name="finishDate" type="DF_DATE"/>
		<separator /> 
 		<field caption="Понедельник" name="monday" type="boolean" value="true"/>
		<field caption="Вторник" name="tuesday" type="boolean" value="true"/>
		<field caption="Среда" name="wednesday" type="boolean" value="true"/>
		<field caption="Четверг" name="thursday" type="boolean" value="true"/>
		<field caption="Пятница" name="friday" type="boolean" value="true"/>
		<field caption="Суббота" name="saturday" type="boolean" value="true"/>
		<field caption="Воскресенье" name="sunday" type="boolean" value="true"/>
	</page>
</passport>';
IF EXISTS (SELECT 1 FROM [dbo].[iPassport] WHERE codeName = 'TrafficChangeDuration')
	UPDATE [dbo].[iPassport] SET passport = @p WHERE codeName = 'TrafficChangeDuration';
ELSE
	RAISERROR('Паспорт TrafficChangeDuration не найден', 16, 1);
GO
