-- Веб: отбор «Журнала использования роликов» и «Сетки вещания» — метаданными, как у журналов.
--
-- До этого оба веб-экрана рисовали отбор своей разметкой (как десктопные формы
-- RollerStatisticForm и FrmGridReport). Теперь — общей панелью отбора (FilterPanel) по XML:
--   1. iEntity.filter сущности 139 «Использование роликов» — был пуст; десктоп его не читает
--      (RollerStatisticForm строит отбор сама), так что для десктопа ничего не меняется.
--      Имена полей = параметры stat_RollerStatistic. «Радиостанции» — selector с multiselect:
--      в веб-фильтре это выбор нескольких станций, значение «id,id,» (massmediaString).
--   2. iPassport «BroadcastGridFilter» — именованный отбор сетки вещания (своей сущности у неё
--      нет). Имена полей = параметры rpt_Grid_v3. mandatory="true" — поле отбирает всегда.
--
-- Идемпотентен: повторный прогон перезаписывает XML тем же. После наката — перезапуск веба
-- (метаданные кэшируются).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i web-filters-roller-statistic-print-grid-seed.sql

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = 139 AND codeName = 'rollerStatistic')
BEGIN
	RAISERROR(N'Сущность 139 (rollerStatistic) не найдена - согласуйте с Merlin.Entities.RollerStatistic', 16, 1);
	RETURN;
END

BEGIN TRANSACTION;

UPDATE [dbo].[iEntity]
SET filter = N'<filter>
	<page caption="Общие">
		<field caption="Начало интервала: " name="startDate" type="df_date" value="TODAY"/>
		<field caption="Окончание интервала: " name="finishDate" type="df_date" value="TODAY"/>
		<selector caption="Радиостанции: " name="massmediaString" entity="massmedia" multiselect="true" mandatory="true"/>
		<objectPicker caption="Менеджер: " name="userID" entity="user"/>
		<objectPicker caption="Фирма-заказчик: " name="firmID" entity="Firm"/>
		<objectPicker caption="Группа компаний: " name="headCompanyID" entity="HeadCompany"/>
		<objectPicker caption="Предмет рекламы:" name="advertTypeID" entity="adverttype" isCreateNewAllowed="false" relationScenario="Предметы рекламы"/>
		<field caption="Показывать с оплатой" name="showWhite" type="boolean" value="true"/>
		<field caption="Показывать без оплаты" name="showBlack" type="boolean" value="true"/>
	</page>
	<page caption="Разбивка">
		<field caption="С разбивкой по менеджерам" name="splitByManager" type="boolean" value="false"/>
		<field caption="С разбивкой по дням" name="splitByDays" type="boolean" value="false"/>
	</page>
</filter>'
WHERE entityID = 139;

DECLARE @grid VARCHAR(4000) = '<filter>
	<page caption="Общие">
		<objectPicker caption="Радиостанция: " name="massmediaID" entity="massmedia" mandatory="true"/>
		<field caption="Дата: " name="theDate" type="df_date" value="TODAY" mandatory="true"/>
		<objectPicker caption="Менеджер: " name="userID" entity="user"/>
	</page>
</filter>';

IF EXISTS (SELECT 1 FROM [dbo].[iPassport] WHERE codeName = 'BroadcastGridFilter')
	UPDATE [dbo].[iPassport] SET passport = @grid WHERE codeName = 'BroadcastGridFilter';
ELSE
	INSERT INTO [dbo].[iPassport] (codeName, passport) VALUES ('BroadcastGridFilter', @grid);

COMMIT TRANSACTION;

PRINT N'Отбор сущности 139 и iPassport BroadcastGridFilter записаны.';
