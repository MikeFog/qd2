-- Веб: параметры «Экспорта сеток вещания» — именованным паспортом, как отбор своих экранов
-- (правило docs/AI_AGENT_PLAYBOOK.md, «Отбор своего веб-экрана — тоже метаданными»).
--
-- iPassport «BroadcastGridExport»: станции (выбор нескольких, значение «id,id,»), дата, что
-- выгружать — файлы для эфира (DJin) и сетки в Word. Десктоп паспорт не читает (ExportGridForm
-- строит форму сама).
--
-- Идемпотентен. После наката — перезапуск веба (метаданные кэшируются).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i web-grid-export-seed.sql

SET NOCOUNT ON;

DECLARE @passport VARCHAR(4000) = '<filter>
	<page caption="Общие">
		<selector caption="Радиостанции: " name="massmediaString" entity="massmedia" multiselect="true" mandatory="true"/>
		<field caption="Дата: " name="theDate" type="df_date" value="TODAY" mandatory="true"/>
		<field caption="Файлы для эфира (DJin)" name="exportDJin" type="boolean" value="true"/>
		<field caption="Сетки в Word" name="exportWord" type="boolean" value="true"/>
	</page>
</filter>';

IF EXISTS (SELECT 1 FROM [dbo].[iPassport] WHERE codeName = 'BroadcastGridExport')
	UPDATE [dbo].[iPassport] SET passport = @passport WHERE codeName = 'BroadcastGridExport';
ELSE
	INSERT INTO [dbo].[iPassport] (codeName, passport) VALUES ('BroadcastGridExport', @passport);

PRINT N'iPassport BroadcastGridExport записан.';
