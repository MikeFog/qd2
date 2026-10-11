# Попутные дефекты и задачи на потом

Найдены по ходу других работ и отложены, чтобы не смешивать с текущими задачами. Раньше часть из них жила только в карточках
«Suggested task» приложения: такая карточка в репозиторий не попадает и пропадает, если её закрыть. Здесь записано всё,
включая восстановленные из журналов сессий 11.10.2026. У каждой задачи — суть, статус по коду на 11.10.2026 и, если была
карточка, полный текст задания: его можно вставить в новую сессию как есть.

## Открытые

### 1. Замена ролика другой длины обнуляет цену спонсорского выпуска; путаница флагов в `hlp_GetMainUserCredentials`

- `ArtvisDB/dbo/Stored Procedures/RollerSubstitute.sql`: `@newPrice` считается только для линейных (`@campaignTypeID = 1`), а при другой длине UPDATE переписывает `Issue.tariffPrice` из него — у спонсорского выпуска цена становится 0. Пример на ArtvisDev: выпуск 39345974 (кампания 376610, акция 171679) — 1 800 → 0. Нужно понять, участвует ли `Issue.tariffPrice` спонсорских в деньгах (деньги спонсорских идут по `ProgramIssue`), и не трогать цену у типов 2–4.
- `hlp_GetMainUserCredentials.sql:27`: в ветке с `@grantorID` присваивается `@isAdmin` вместо `@isTrafficManager`. Замена ролика `@grantorID` не передаёт; режим «грантор», по `docs/IMPROVEMENTS.md` [GRANT-01], не использовался ни в одной базе.
- Статус: открыто. Найдено при этапе 5 перехода на фактическое окно (11.10.2026).

<details><summary>Задание для отдельной сессии (карточка от 11.10.2026)</summary>

```text
qd2 repo C:\Work\AdvertAg\sources (WinForms + MS SQL; dev DB `.\sqlexpress` / ArtvisDev — Misha's working DB: read-only checks, any writes only inside short BEGIN TRAN … ROLLBACK, every query with a time limit (sqlcmd -t 60), never query prod). Two pre-existing defects found during stage 5 of the «actual window» plan (docs/tasks/window-actual-switch.md), deliberately NOT fixed there:
1) ArtvisDB/dbo/Stored Procedures/RollerSubstitute.sql: @newPrice is set to 0 before the loop body and computed only for @campaignTypeID = 1 (around `set @newPrice = 0` / `if @campaignTypeID = 1 and @msgError is null … fn_GetIssuePrice`), and later the UPDATE of Issue writes tariffPrice from @issues.newPrice when the duration differs (around lines 318-333). For a sponsor campaign (type 2) Issue row, substituting a roller of a different length overwrites Issue.tariffPrice with 0. Example on ArtvisDev: issue 39345974 (campaign 376610, action 171679) tariffPrice 1800 → 0. Reproduce in a rolled-back transaction, check how sponsor money is actually computed (ActionRecalculate type 2 uses ProgramIssue.tariffPrice for money and Issue rows for duration/bonus time — is Issue.tariffPrice of type 2 used anywhere for money? grep GetPriceByPeriod, rpt_GenericBill, stat_*), decide whether the right fix is to leave tariffPrice unchanged for type 2 (and 3/4) or to recompute; propose a minimal fix.
2) ArtvisDB/dbo/Stored Procedures/hlp_GetMainUserCredentials.sql around line 27: in the @grantorID branch @isAdmin is assigned where @isTrafficManager is meant. RollerSubstitute does not pass @grantorID, so it is not hit there — find all callers that do pass @grantorID (grep), check whether «grantor» mode is used at all (docs/IMPROVEMENTS.md [GRANT-01] says it was never used in any database) and recommend fix vs. leave.
Deliverable: findings with evidence, a proposed minimal patch and a Deploy script following Deploy/README.md conventions (UTF-8 BOM, CRLF, first batch only header + SET NOCOUNT ON, CREATE OR ALTER, final OBJECT_DEFINITION check printing «ГОТОВО», trailing SET NOEXEC OFF if a guard is used). Note: after Deploy/34 the scripts 18, 26, 27, 29 and ArtvisDB/Scripts issue-price-from-db / promo-position-forbidden / roller-substitute-same-duration-skip must not be re-run (they carry old texts of RollerSubstitute etc.) — build any change on the current working-copy text of RollerSubstitute.sql. Do not commit without asking Misha (address him in masculine form).
```
</details>

### 2. Удаление подтверждённой акции не пишет журнал удалённых выпусков и не уведомляет администраторов

- `ActionIUD.sql`: при удалении подтверждённой акции сначала вызывается `ActionDeactivate` (строка ~90), который снимает подтверждение со всех выпусков; запись в `LogDeletedIssue` (~160) и проверка `SayAdminThatIssuesDelete` (~171) идут после и с фильтром `i.isConfirmed = 1` — ничего не находят. Журнал «Удаленные рекламные выпуски» (сущность 193) остаётся пустым, уведомления нет. `CampaignIUD` пишет журнал до удаления — образец.
- Статус: открыто (порядок в `ActionIUD` на 11.10.2026 тот же). Найдено при шаге 1 перехода (07.10.2026).

<details><summary>Задание для отдельной сессии (карточка от 08.10.2026)</summary>

```text
Repo C:\Work\AdvertAg\sources (qd2: WinForms + MS SQL; web FogSoft.Web shares the procedures).

Defect found 07.10.2026 by code review (not reproduced live): `ArtvisDB/dbo/Stored Procedures/ActionIUD.sql` DeleteItem for a confirmed action first calls `ActionDeactivate` (around lines 88-91). `ActionDeactivate.sql` (around lines 106-107) sets `isConfirmed = 0` for all Issue rows of the action. After that, ActionIUD's insert into `LogDeletedIssue` (around lines 160-165, filter `i.isConfirmed = 1`) and the admin notification check `SayAdminThatIssuesDelete` (around lines 166-173, same filter) find no rows. Result: deleting a confirmed action leaves no rows in the «Удаленные рекламные выпуски» journal (entity 193) and admins get no «Удалил выпуски, дата выхода которых раньше чем через N дней…» notification. Compare CampaignIUD.sql, which logs before deleting.

Also note: ActionIUD continues after `EXEC ActionDeactivate` even if it raised an error (nested RAISERROR does not stop the caller); the client rolls back because isTransactionRequired = 1 (FogSoft.WinForm/DataAccess/DataAccessor.cs ~595-603), but check whether an explicit return is needed.

Tasks:
1. Confirm by reading ActionIUD.sql and ActionDeactivate.sql fully; check git log for when it broke.
2. Ask the user (Misha) before any DB writes. ArtvisDev is his dev DB (changes allowed, but keep runs short); never query prod. Reproduce in a rolled-back transaction on ArtvisDev: delete a confirmed test action and count LogDeletedIssue rows.
3. Fix minimally: capture the confirmed issues (or do the LogDeletedIssue insert and the notification check) BEFORE calling ActionDeactivate. Keep the join by `i.actualWindowID` for the notification day (changed 07.10.2026 in Deploy/24). The LogDeletedIssue time column is planned to switch to windowDateActual of the actual window separately ("пакет 1" in docs/tasks/window-actual-switch.md §5.1) — do not mix that in unless asked.
4. Make a Deploy/NN script following Deploy/README.md conventions (UTF-8 with BOM, first batch header + SET NOCOUNT ON, CREATE OR ALTER, OBJECT_DEFINITION check, COMMIT by default). Do not commit without the user's permission; commit only your own files (working copy is shared).
```
</details>

### 3. Генерация окон по шаблону: условие без BEGIN — окна создаются в неотмеченные дни и в занятые слоты

- `GenerateTariffWindowByTemplate.sql`: большое `IF` (день недели, слот занят, профилактика) без BEGIN/END охватывает только строку с `f_CheckLinkedTariffWindows`; вставка окна (`INSERT TariffWindow` / `InsideLinkedWindowError`) выполняется на каждый день цикла с устаревшим `@isInsideChain`. Последствия: окна на неотмеченных днях недели, в занятых слотах и в периодах профилактики; ложные `InsideLinkedWindowError`.
- **Пересекается с этапом 7** перехода на фактическое окно (`docs/tasks/window-actual-switch.md`): та же процедура, проверка «слот занят» (~:77) и склейки. Чинить отдельной правкой до или после этапа 7, не в одном деплое.
- Статус: открыто (код на 11.10.2026 тот же). Найдено 07.10.2026 чтением кода, вживую не воспроизведено.

<details><summary>Задание для отдельной сессии (карточка от 07.10.2026)</summary>

```text
Repo C:\Work\AdvertAg\sources (qd2: WinForms + MS SQL). Procedure `ArtvisDB/dbo/Stored Procedures/GenerateTariffWindowByTemplate.sql` (desktop: «Создать новое окно по шаблону…» / «Продлить окно», caller Frm/FrmWindowTariffTemplate.cs:74-82).

Suspected defect (found by code reading 07.10.2026, not reproduced live): inside the `While @currentDate <= @finishDate` loop (line ~45), the big `IF ((weekday filter) And Not Exists(slot taken: tw.windowDateOriginal = @actualDate) And Not Exists(DisabledWindow))` at lines ~57-82 has no BEGIN/END, so it only guards `set @isInsideChain = dbo.f_CheckLinkedTariffWindows(...)` (line ~84). The following `IF @isInsideChain = 0 ... INSERT TariffWindow ... ELSE INSERT @errors 'InsideLinkedWindowError'` block (lines ~86-94) runs on EVERY day of the loop with the stale @isInsideChain (declared at line 35 without a value, i.e. NULL). Expected consequences: before the first matching day — false 'InsideLinkedWindowError' rows; after it — windows inserted on unchecked weekdays, on days where the slot is already taken (bypassing the Not Exists check), and inside DisabledWindow periods.

Tasks:
1. Confirm by reading the procedure fully; check git history (`git log -p` on the file) whether BEGIN/END was lost in a recent change (e.g. commit 6a2afc0).
2. Ask the user (Misha) before any database query. ArtvisDev is his working DB; never query prod. If allowed, check on ArtvisDev for evidence: windows with tariffId IS NULL and price 0 created in a run on unexpected weekdays or duplicating a slot (same massmediaID + windowDateOriginal).
3. Fix: wrap the whole per-day body after the IF in BEGIN/END (and reset @isInsideChain per iteration). Keep the change minimal; follow docs/AI_AGENT_PLAYBOOK.md and Deploy conventions (UTF-8 with BOM, a Deploy/NN_*.sql script that COMMITs, first batch only header + SET NOCOUNT ON).
4. Note: a separate plan (docs/tasks/window-actual-switch.md, stage 7) will later also change line ~77 (add check on windowDateActual) and f_CheckLinkedTariffWindows — keep the fix independent and mention it there.
Do not commit without the user's permission; commit only your own files (the working copy is shared with other sessions).
```
</details>

### 4. Дни недели: в названии тарифа перепутаны «вт» и «ср»; отчёт о заполненности сдвигает дни недели

- `TariffPassport.sql:19-20`: `t.wednesday → ',вт'`, `t.tuesday → ',ср'` — подписи перепутаны (на 11.10.2026 так и есть).
- `stat_FillPercentage.sql`: сравнивает `DATEPART(WEEKDAY, ...)` как будто неделя с воскресенья, а `SET DATEFIRST` нет; у логина приложения русский язык (понедельник = 1) — фильтр по дням недели сдвинут на день.
- Статус: открыто. Найдено 24.09.2026 при инвентаризации кириллицы в SQL.

<details><summary>Задание для отдельной сессии (карточка от 24.09.2026)</summary>

```text
Repo C:\Work\AdvertAg\sources, SQL project ArtvisDB. Read CLAUDE.md and docs/AI_AGENT_PLAYBOOK.md first. Dev DB: sqlcmd -S "(local)\sqlexpress" -E -d ArtvisDev -f 65001 (Windows auth is sysadmin; use -I when deploying procs; deploy scripts must COMMIT by default, start with SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON batch).

Two suspected bugs:
1. ArtvisDB/dbo/Stored Procedures/TariffPassport.sql ~lines 19-20: `case t.wednesday when 1 then ',вт'` and `case t.tuesday when 1 then ',ср'` — Tuesday/Wednesday labels swapped in the tariff display name. Confirm with data (a tariff active on Tuesday only) and fix; check whether any other procedure has the same copy-paste (grep for ',вт').
2. ArtvisDB/dbo/Stored Procedures/stat_FillPercentage.sql ~lines 127-133 and ~157+: compares DATEPART(WEEKDAY, tw.dayActual) with 1=Sunday..7=Saturday, i.e. assumes DATEFIRST 7, but it has no SET DATEFIRST and the application login AdvertAgUser has default language Russian (verified on dev server: DATEFIRST=1, Monday=1). So day-of-week filters are shifted by one day. Most other procedures do `SET DATEFIRST 1` explicitly — follow that convention (or use @@DATEFIRST-independent expression as in AgitationFraming.sql). Verify with a before/after run on ArtvisDev for a known period, find the C# caller(s) and which report/menu uses it.
Produce a short plan, fix, write a deploy script under ArtvisDB/Scripts, validate on ArtvisDev, and give a validation checklist. Do not commit or touch prod without asking.
```
</details>

### 5. Веб: гонка в `MessageAccessor` — чужие подстановки в тексте ошибки

- `FogSoft.WinForm/DataAccess/MessageAccessor.cs` (общий с вебом): параметры сообщения — в статическом поле `parameters`, кэши — обычные `Dictionary`. В вебе (много пользователей) два запроса могут подставить в текст ошибки чужие значения или упасть на одновременной записи в словарь. Десктоп не затронут (один пользователь).
- Статус: открыто (на 11.10.2026 поле по-прежнему статическое). Найдено 24.09.2026.

<details><summary>Задание для отдельной сессии (карточка от 24.09.2026)</summary>

```text
Repo C:\Work\AdvertAg\sources (qd2: WinForms desktop + Blazor Server web FogSoft.Web sharing core via FogSoft.Core linked files). Read CLAUDE.md, docs/ARCHITECTURE.md, docs/AI_AGENT_PLAYBOOK.md first.

Problem: FogSoft.WinForm/DataAccess/MessageAccessor.cs (linked into FogSoft.Core and used by the web) has `private static Dictionary<string, object> parameters` set via `MessageAccessor.Parameters = parameters` from DataAccessor (FogSoft.WinForm/DataAccess/DataAccessor.cs lines ~207 and ~513) right before executing a procedure; later ErrorManager.GetErrorMessage → MessageAccessor.GetMessage(name) formats iMessage text placeholders {0..n} using that static dictionary. Also `messages` is a plain static Dictionary lazily filled in LoadMessage, and `_dsData` is lazily loaded — not thread-safe. In the multi-user Blazor Server web app, concurrent requests from different users can (a) substitute another user's parameters into an error message, (b) throw on concurrent Dictionary writes.

Task: make it safe for the web without changing desktop behavior. Preferred minimal approach: make the parameters flow per-call (e.g. AsyncLocal/ThreadStatic set by DataAccessor, or pass parameters explicitly through the exception path — check how ErrorManager and web ErrorPresenter (FogSoft.Web/Infrastructure/ErrorPresenter.cs) call GetMessage), and use ConcurrentDictionary / lock for the caches. Find all callers of MessageAccessor.Parameters and GetMessage in Client, FogSoft.WinForm, FogSoft.Web. Keep the change surgical; build both the desktop solution (see memory about which MSBuild) and `dotnet build FogSoft.Web`. Produce a short plan first, then implement, then a validation checklist. Do not commit without asking.
```
</details>

### 6. Журнал использования роликов строится ~3 с даже на один день одной станции

- `stat_RollerStatistic.sql`: план перебирает выпуски всех подтверждённых акций (~3 млн строк) и только потом отбирает окна периода; на месяце по всем станциям 6–7 с. Вариант «сначала окна периода» не ускорил — нужен разбор фактического плана. Выдачу не менять.
- Статус: открыто, производительность. Найдено 10.10.2026 при этапе 6.

<details><summary>Задание для отдельной сессии (карточка от 10.10.2026)</summary>

```text
qd2 (C:\Work\AdvertAg\sources), SQL Server Express, dev base `.\sqlexpress` / ArtvisDev (Misha's working DB: no long locks/index rebuilds without agreeing first; never query prod). Procedure `ArtvisDB/dbo/Stored Procedures/stat_RollerStatistic.sql` («Журнал использования роликов», desktop entity 139 and web /roller-statistic; metadata timeout 1800 s) takes ~2.9–3.5 s even for one day on one station (e.g. @startDate=@finishDate='20260720', @massmediaString='284,' — note the trailing comma required by fn_CreateTableFromString, @loggedUserID=3), and 6–7 s for a month over all stations. SET STATISTICS IO shows Issue ~35 000 seeks / 132 000 reads, Campaign ~11 000 seeks, Roller 14 000 reads, ~3 s CPU: the plan starts from all confirmed actions → campaigns → issues (~3 M rows) and filters the period's TariffWindow rows late. Since Deploy/28 (10.10.2026) the main insert joins `TariffWindow tw on i.actualWindowID = tw.windowID` (fact window, keep this semantics). A quick try — prefilling `#tw` (windows of the period on @massmedias) and joining Issue on actualWindowID — was NOT faster (2.6–7 s), so find the real cost driver with the actual plan (SET STATISTICS PROFILE / XML), e.g. the derived users table `x`, the late @massmedias filter (commented-out join at line ~74), cardinality of table variables, or the GROUP BY. Index `IX_Issue_ActualWindowID_isConfirmed_COVERING (actualWindowID, isConfirmed) INCLUDE (campaignID, rollerID)` exists. Goal: identical output (all 3 result sets, with @splitByDays/@splitByManager on and off; compare old vs new as temp procedures `#name` in one connection, best of several runs) and a clearly faster time; then a Deploy script by the conventions in Deploy/README.md (UTF-8 BOM, CRLF, first batch only header + SET NOCOUNT ON, separate SET ANSI_NULLS/QUOTED_IDENTIFIER batches, CREATE OR ALTER, final OBJECT_DEFINITION check printing «ГОТОВО»). Keep `ActionsForRollerStatistic` semantics in sync (same day/station rule). Do not commit without asking Misha (address him in masculine form).
```
</details>

### 7. Устаревший комментарий об очереди роликов Шаблона №3

- `Client/Controls/TariffWithRangeGrid.cs:~1174` (XML-doc над `AddIssuesRangeTimePeriodMultiRoller`): «случайно перемешанной очереди (отсортированных по убыванию длительности)» — после перехода `RollerAllocationQueue` на round-robin неверно; актуальное описание — `docs/scenarios/template3-roller-distribution.md`. Вторая половина карточки (комментарий в `FrmTemplate3.cs`) уже сделана.
- Статус: наполовину, только текст комментария. Карточка от 11.07.2026.

<details><summary>Задание для отдельной сессии (карточка от 11.07.2026)</summary>

```text
В Client/Forms/FrmTemplate3.cs на строках 318-319 комментарий над свойством SelectedRollers гласит "Пока не используется генератором выпусков — только UI-заготовка для нового бизнес-процесса." Это устарело: свойство активно используется в CampaignForm.tbbTemplate3_Click (строка ~1306) для построения rollerQuantities, передаваемых в генератор. Нужно убрать/поправить комментарий, чтобы не вводить в заблуждение.

В Client/Controls/TariffWithRangeGrid.cs на строках 540-545 (XML-doc над AddIssuesRangeTimePeriodMultiRoller) написано "...из общей случайно перемешанной очереди (отсортированных по убыванию длительности, см. RollerAllocationQueue)". После рефакторинга RollerAllocationQueue на round-robin по раундам (см. Client/Classes/RollerAllocationQueue.cs) это описание больше не точное — там теперь round-robin с anti-adjacent подстраховкой, а не сортировка по убыванию длительности. Нужно обновить формулировку.

Оба места — просто правка текста комментария, без изменения логики. Подробное актуальное описание алгоритма есть в docs/scenarios/template3-roller-distribution.md, можно сослаться на этот файл вместо повторного описания механики в комментарии.
```
</details>

## Найдено попутно при этапах 4–5 перехода на фактическое окно (без карточек)

Подробности с числами — `docs/tasks/window-actual-open-questions.md` §11 и разборы этапа 4.

- **НДС в счетах 2027 года.** В `AgencyTax` у всех текущих агентств ставка заканчивается 31.12.2026, у агентства 212 строк нет совсем. Без строк на 2027 год НДС в счетах 2027 года будет 0 — это справочник, не код; передать тому, кто его ведёт.
- **Порог объёмной скидки 0,71** в наборе 88 (станция 219, от 30 000) при 0,70 у 1 043 кампаний — похоже на правку 06.10.2026. Если она есть на проде, любой пересчёт старой акции станции 219 поднимет её цену (оценка до +571 тыс. ₽).
- **Даты кампаний не совпадают с выпусками** у 60 линейных кампаний в 18 акциях (следы старого broadcastStart и удаления выпусков без пересчёта: 176914…176952, 182386; у 185835 даты пустые при 293 выпусках); у 8 кампаний 28 выпусков (6 785,85 ₽) лежат за датами кампании и не попадают в акт.
- **Счёт падает с ошибкой 515** у кампании с пустыми датами (акция 185835: `GetPriceByPeriod` при обеих датах NULL не заполняет НДС); в счёте по месяцу такие кампании молча теряются, хотя комментарий в `rpt_GenericBill` обещает обратное.
- `rpt_GenericBill.sql:~154`: у пакетов в счёте по месяцу верхняя граница — `finishDate` кампании вместо `@endDate` (пропорции по станциям).
- `GetMonthes`: у пакетов лишний пустой месяц («день +1», пример — акция 160578: 02.2024 и пустой 03.2024).
- **Факторный анализ:** сравниваемый период = начало + (конец − начало), а не календарный месяц (октябрь против сентября даёт 01.09–01.10).
- `fn_Int2Time` (`VARCHAR(8)`) от 100 часов обрезает секунды («281:48:1» вместо 281:48:13) — «Объём реализации в секундах», факторный анализ.
- **Счётчики занятости окна** разошлись с выпусками (окно 13077994: в окне подтверждённый и неподтверждённый выпуск, `capacityInUseUnconfirmed = 0`) — нужен скрипт сверки счётчиков `TariffWindow` с `Issue`.
- `stat_SponsorBusiness`, режим «свободные и занятые»: последний день спонсорского прайс-листа после 00:00 отсекается (LEFT JOIN по дате выпуска при `finishDate` с 00:00) — поведение сохранено явным условием в шаге 3 broadcastStart.
- `GetPriceByPeriod`: план линейной ветки зависит от первого вызова — `docs/IMPROVEMENTS.md` [SQL-02] (отложено 11.10.2026).

## Закрытые карточки

- «Проверить и исправить дефекты создания кампаний» (03.10.2026): Д-4, Д-5 и Д-7 исправлены, Д-9 решено не исправлять — `docs/action-forms.md` §10.
- «Удалить мёртвые файлы в Client/Classes» и «Почини .gitignore, задавивший FogSoft.WinForm/Win32/» (21.08.2026) — сделаны в основной сессии 23.08.2026.
