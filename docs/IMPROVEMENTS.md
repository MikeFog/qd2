# Кандидаты на улучшение

Здесь фиксируются наблюдения, которые **не являются ошибками**, но заслуживают внимания при рефакторинге или оптимизации.
Каждый пункт содержит: область, суть, обоснование, указатели на код.

Для настоящих багов (не улучшений) — отдельные файлы в `docs/tasks/`, ссылка здесь как указатель:

- [`stat_VolumesByPaymentTypes` падает на акциях дороже ~1 млн ₽](tasks/stat-volumes-by-payment-types-overflow.md) — переполнение `decimal(8,2)` при `@groupByPaymentType = 0`; найдено 11.09.2026, не исправлено.

---

## UI / Performance

### [RANGE-01] Полный RefreshGrid() после каждого клика в веерном гриде

**Область:** `TariffWithRangeGrid` / веерное размещение  
**Суть:** После добавления выпуска вызывается `RefreshGrid()` — полный перезаброс грида из БД (`TariffWindowWithRange`). В линейном сценарии (`RollerIssuesGrid3`) обновляется только одна ячейка (`RefreshSingleCell`).  
**Почему важно:** При большом числе радиостанций в акции или широком диапазоне дат полный перечёт блокирует UI-поток на каждый клик.  
**Где смотреть:**
- `Client\Controls\TariffWithRangeGrid.cs` — метод `AddIssuesRange(DataGridViewCell)`, строка `RefreshGrid()`
- `Client\Controls\RollerIssuesGrid3.cs` — метод `RefreshSingleCell` (образец точечного обновления)
- `ArtvisDB\dbo\Stored Procedures\TariffWindowWithRange.sql` — SP, которую вызывает `populateGrid`

**Возможное направление:** После `AddRangeIssues` перечитывать только затронутую ячейку (строку/колонку), аналогично `RefreshSingleCell` в линейном сценарии.

---

### [UI-02] ~290 мс на клик добавления выпуска уходит в перебиндинг двух SmartGrid деталей окна

**Область:** `CampaignForm.ShowWindowIssues` / `TariffGrid.FireCellClicked`
**Суть:** После оптимизаций ветки `feature/recalc-join-fix` тёплый клик добавления выпуска в простой линейной акции = ~335 мс, из которых SQL-часть (`IssueIUD` + `ActionRecalculate` + `Campaigns` + `WindowIssuesRetrieve`) — суммарно ~40 мс, а `CampaignStatusChanged` после фикса `ReloadData` — ~25 мс. Оставшиеся ~290 мс — чисто клиентский рендеринг в `FireCellClicked`:
- `grdIssues.Clear()` + `grdCurrentCampaignIssues.Clear()` + пересоздание `Entity` (Clone) + перебиндинг двух `SmartGrid` в `ShowWindowIssues`;
- два подряд `Application.DoEvents()` — в `TariffGrid.FireCellClicked` и в `CampaignForm.grid_CellClicked` — прокачивают всю очередь перерисовки грида;
- в логе видно окном ~260 мс между закрытием `UpdateDB` и первым `WindowIssuesRetrieve`, где нет ни одного вызова БД.

**Почему важно:** Это теперь доминирующая доля клика. При этом окно деталей часто даже не смотрят во время расстановки. Дешёвых и безрисковых вариантов нет — правка лезет в биндинг `SmartGrid` и в порядок `DoEvents`.
**Где смотреть:**
- `Client\Controls\TariffGrid.cs` — `FireCellClicked` (стр. ~463), `GetCell(ITariffWindow)` — O(строк×колонок) скан
- `Client\Forms\CampaignForm.cs` — `grid_CellClicked` (стр. ~491), `ShowWindowIssues` (стр. ~551)

**Возможное направление:** (а) не перестраивать `grdIssues`/`grdCurrentCampaignIssues` на каждый клик добавления, а только когда пользователь реально смотрит панель деталей / по отдельному действию; (б) убрать лишний `Application.DoEvents()`; (в) кэшировать клонированные `Entity`. Браться только если заказчик после отгрузки `feature/recalc-join-fix` всё ещё жалуется на отклик расстановки.

---

### [UI-03] Комбо-модули: ~2 с клиентского рендеринга после каждой операции в гриде

**Область:** `ComboModuleGrid` / форма размещения комбо-модулями
**Суть:** Замер 30.08.2026 на восстановленном проде (лог sveta): один клик добавления модульного выпуска в комбо-модулях = ~2 с, из которых SQL — <150 мс (`ModuleIssueIUD` + `ActionRecalculate` 20–30 мс после join-фикса + `Actions1` + `IsModuleExist` + `CampaignIUD`). Оставшиеся ~1,9 с — окно в логе без единого вызова БД между `Actions1` и следующим `ComboModuleFreeTimeRetrieve`. После каждой операции (и просто навигации по гриду) форма перечитывает три источника — `ComboModuleContentRetrieve` + `ComboModuleFreeTimeRetrieve` + `ComboModuleIssuesRetrieve` — и полностью перестраивает грид на клиенте. Те же ~2 с повторяются на каждом шаге.
**Почему важно:** Комбо-модули — активная зона разработки, и это ощущается как «тормозит» даже сейчас, когда SQL уже дешёвый. Родственно [RANGE-01] и [UI-02]: узкое место — полная клиентская перестройка грида вместо точечного обновления.
**Где смотреть:**
- `Client\Controls\ComboModuleGrid.cs` — `OnGridCellClick` (стр. ~626), путь обновления после `ModuleIssueIUD`
- SP `ComboModuleFreeTimeRetrieve` — отдельно кандидат [SQL-02] (до 21 с в пике 29.08.2026), но в этом замере <100 мс
- `Client\Classes` — где `campaign.Action.Refresh()` (`Actions1`) зовётся по каждому модульному выпуску (аналог убранного в `RollerIssuesGrid3.UpdateDB`)

**Возможное направление:** после добавления/удаления одного модульного выпуска обновлять только затронутую ячейку/строку, а полный `ComboModule*Retrieve` + перестройку грида делать один раз в конце пакетной операции и при явной смене контекста. Убрать пер-элементный `campaign.Action.Refresh()`. Браться после отгрузки join-фикса, если жалобы на отклик комбо-модулей останутся.

---

## C# / Domain

### [ISSUE-01] ModuleIssue переопределяет Delete(), но не Delete(bool) — теряется пересчёт акции

**Область:** `Merlin.Classes.ModuleIssue`, `FogSoft.WinForm.Classes.PresentationObject`  
**Суть:** В `PresentationObject` два разных виртуальных метода удаления: `Delete()` и `Delete(bool silenceFlag)`. `Delete()` просто зовёт `Delete(false)`, то есть переопределение одного из них **не** влияет на другой. `ModuleIssue` переопределяет только `Delete()` — и именно там после удаления пересчитывает акцию (`campaign.Action.Recalculate()`).  
**Почему важно:** Пересчёт выполняется только на путях, которые зовут `Delete()` без параметров — это удаление через контекстное меню (`PresentationObject.DoAction` → `case Delete: Delete();`). А `SmartGrid.DeleteSelectedObjects` и любой код массового удаления зовут `Delete(true)` — там пересчёта нет, и суммы акции остаются старыми, пока её не пересчитает кто-то ещё. Симптом: удалил выпуски по Delete — стоимость акции не изменилась. В форме размещения комбо-модулями это обошли явным вызовом `_action.Recalculate()` в общем хвосте удаления (как это давно делает `CampaignForm.ProcessCurrentCampaignIssuesDelete`), но сама ловушка осталась и может выстрелить в других местах, работающих с `ModuleIssue`.  
**Где смотреть:**
- `FogSoft.WinForm\Classes\PresentationObject.cs` — `Delete()` и `Delete(bool)`, `DoAction` (ветка `EntityActions.Delete`)
- `Client\Classes\ModuleIssue.cs` — `override bool Delete()` с пересчётом
- `FogSoft.WinForm\Controls\SmartGrid.cs` — `DeleteSelectedObjects` (зовёт `Delete(true)`), `DeleteCurrentObject` (зовёт `DoAction`)
- `Client\Forms\CreateActionMaster\ComboModulePlacementForm.cs` — `AfterIssuesDeleted` как пример обхода

**Возможное направление:** Перенести пересчёт из `Delete()` в `Delete(bool)` — тогда он отработает на всех путях, потому что `Delete()` делегирует туда же. Изменение затрагивает все экраны модульных кампаний, поэтому требует проверки, не появится ли двойной пересчёт там, где вызывающий код уже пересчитывает акцию сам.

---

### [ISSUE-02] `ReloadData()` обходит переопределения `Refresh()` — развилка в сбросе кэшей дочерних объектов

**Область:** `FogSoft.WinForm.Classes.PresentationObject`
**Суть:** `ReloadData()` (добавлен в ветке `feature/recalc-join-fix` для перезагрузки объекта без `ObjectChanged`) зовёт **приватный** `Refresh(InterfaceObjects, bool notify)`, минуя `public virtual bool Refresh()`. А `Refresh()` переопределяют семь классов, и все — чтобы сбросить кэш дочерних объектов: `ActionOnMassmedia` (`user`), `ModuleIssue` (`_roller`), `ModulePricelist`, `PackModuleIssue`, `PackModulePricelist`, `StudioOrder`, `StudioOrderAction`. Для этих типов `ReloadData()` перезагрузит строку данных, но оставит протухший кэш — тихая ошибка.
**Почему важно:** Сейчас не стреляет (`Campaign` не переопределяет `Refresh()`, единственный вызыватель — `CampaignForm.CampaignStatusChanged`). Но это публичный метод базового класса фреймворка, и его позовут для других типов. Doc-комментарий сейчас предупреждает словами — этого мало.
**Где смотреть:**
- `FogSoft.WinForm\Classes\PresentationObject.cs` — `ReloadData()`, приватный `Refresh(InterfaceObjects, bool)`, `public virtual bool Refresh()`
- семь `override bool Refresh()` (grep по решению)

**Возможное направление:** Вынести сброс кэшей в `protected virtual void OnDataReloaded()`, вызывать его из общего приватного `Refresh` после `Init(...)`, а семь переопределений `Refresh()` заменить на переопределения этого хука. Тогда `ReloadData()` и `Refresh()` идут через одну точку сброса, развилки нет. Проверить, что ни одно из семи переопределений не делает в `Refresh()` ничего, кроме сброса кэша + `base.Refresh()`.

---

### [GRANT-01] Режим «грантор» не использовался ни в одной базе — кандидат на удаление целиком

**Область:** `CampaignForm` (кнопка `toolStripButtonGrantor`, свойство `Grantor`), `Utils.AskConfirmation` / `FrmConfirmation`, гриды (`IRollerGrid.Grantor`), SQL-параметр `@grantorID`
**Суть:** режим позволяет менеджеру работать с выпусками под правами грантора или администратора (тот вводит свой логин и пароль): `AskConfirmation` → `Grantor` → `grantorID` в `IssueIUD` / `ModuleIssueIUD` / `PackModuleIssueID` / `AddRangeIssues` / `MasterIssueDelete`; `hlp_GetMainUserCredentials` берёт права (`isAdmin`, `rightToGoBack`, `rightForMinus`) у грантора; выпуск хранит `Issue.grantorID`; `ActionActivate` смотрит `fn_IsRightForMinus(i.grantorID)`. Журнал `ConfirmationHistory` удалён 2026-09-20 (ветка `cleanup/confirmation-history`); след «кто подтвердил» остался только в `Issue.grantorID` самого выпуска.
**Данные** (копии ArtvisDev, Artvis, Artvis2, Belgorod, Tumen на 2026-09-20): `Issue.grantorID IS NOT NULL` — **0 выпусков из ~10,5 млн** (3,44 / 3,39 / 3,39 / 3,39 / 0,27 млн). Это вся история, а не месячное окно `DeleteHistory`. Значит, режим не использовался никогда или не пережил ни один выпуск. При этом 4 из 40 активных пользователей ArtvisDev помечены `isGrantor = 1`.
**Живой остаток — это другая ветка «подтверждения»:** `ManagerDiscountForm.cs:132` зовёт `AskConfirmation`, чтобы администратор авторизовал скидку сверх лимита (`ActionForm.cs:303` → `SetFinalPrice(..., grantor, reasonId)`). Её судьба решается отдельно; решение 2026-08-21 — в веб не переносится (`docs/tasks/web-migration-dialogs.md`, §8 п.1).
**Почему важно:** веб режима не получит, то есть это функция десктопа, которой в вебе не будет. Параметр `@grantorID` и логика прав грантора лежат в самых горячих процедурах выпусков. Это мёртвая (по данным) ветка в критичном коде.
**Где смотреть:**
- C#: `Client\Forms\CampaignForm.cs` (~стр. 1336, 1654–1665), `Client\Classes\Utils.WinForms.cs` (`AskConfirmation`), `Client\Forms\FrmConfirmation.cs`, `Client\Controls\{IRollerGrid,RollerIssuesGrid3,PackModuleGrid,TariffWithRangeGrid}.cs`, `Client\Classes\Campaign.cs`, `FogSoft.WinForm\Classes\SecurityManager.cs` (`IsGrantor`)
- SQL: `hlp_GetMainUserCredentials`, `IssueIUD`, `ModuleIssueIUD`, `PackModuleIssueID`, `AddRangeIssues`, `MasterIssueDelete`, `ActionActivate`, `CampaignImportGrammofon`, `CampaignImportMediaPlus`, `GetUserData`, `UserIUD`, `vUser`, `f_IsGrantor`, колонки `User.isGrantor` и `Issue.grantorID` (с FK)

**Возможное направление:** решение владельца продукта — режим не нужен. Тогда убрать кнопку и `Grantor` в гридах, `@grantorID` из процедур выпусков и импорта, ветку грантора в `hlp_GetMainUserCredentials`, `f_IsGrantor`/`isGrantor`, затем колонку `Issue.grantorID`; `AskConfirmation` для скидок оставить или убрать отдельным решением. Технически дёшево, но задевает горячие процедуры: делать по образцу `cleanup/*` (скрипт правит процедуры из `OBJECT_DEFINITION`, а не из репозитория) и проверять на ArtvisDev и Belgorod. Перед этим стоит спросить у пользователей с `isGrantor = 1`, не нужна ли им эта возможность.

---

## SQL / Architecture

### [SQL-01] Дублирование логики расчёта цены за период (GetPriceByPeriod) внутри stat_Bonuses

**Область:** `dbo.GetPriceByPeriod`, `dbo.stat_Bonuses`  
**Суть:** `GetPriceByPeriod` — краеугольная функция расчёта стоимости кампании за период (используется во многих процессах). Раньше `stat_Bonuses` вызывала её построчно через курсор (RBAR): ~500+ вызовов `EXEC` на один отчёт, из-за чего процедура работала 10–99 секунд. Чтобы исправить производительность, логику `GetPriceByPeriod` (ветки по `campaignTypeID` 1–4, `isSpecial`, `showBlack`) пришлось **скопировать и переписать в set-based виде прямо внутри `stat_Bonuses`** — теперь она нигде не вызывает `GetPriceByPeriod`, а держит собственную копию той же математики.  
**Почему важно:** Теперь есть два места с одной и той же бизнес-логикой расчёта цены. Если `GetPriceByPeriod` изменится (новый `campaignTypeID`, другая формула, новый параметр), `stat_Bonuses` это изменение не подхватит автоматически — нужно будет руками синхронизировать копию. Риск рассинхронизации будет расти с каждым изменением тарифной логики.  
**Где смотреть:**
- `ArtvisDB\dbo\Stored Procedures\GetPriceByPeriod.sql` — оригинальная scalar-процедура (по одной кампании за вызов)
- `dbo.stat_Bonuses` (не в системе контроля версий, живёт прямо в БД) — set-based копия той же логики, ветка `@selectByCreateDate = 0`

**Возможное направление:** Превратить `GetPriceByPeriod` в inline table-valued function (один `SELECT` с `CASE`/`UNION ALL` по `campaignTypeID`, без временных таблиц — ветку типа 4 с `#tmp` переписать через оконные функции `SUM(...) OVER (PARTITION BY campaignID)`). Inline TVF SQL Server встраивает в план запроса, поэтому:
- одиночные вызыватели (UI, пересчёт при клике) продолжают дёргать `GetPriceByPeriod` — она становится тонкой обёрткой над той же функцией;
- массовые/отчётные вызыватели (`stat_Bonuses` и подобные) используют `CROSS APPLY` по множеству кампаний за один set-based проход.

Так расчётная математика будет жить в одном месте. Перед тем как браться — надо найти все текущие вызовы `GetPriceByPeriod` в коде и метаданных, оценить масштаб и риски переписывания multi-statement-логики.

---

### [SQL-02] Аудит: тот же паттерн join `Issue` × `TariffWindow` по диапазону дат в других процедурах

**Область:** процедуры, соединяющие выпуски с окнами по `originalWindowID` + фильтр `TariffWindow.dayOriginal BETWEEN ...`
**Суть:** В `GetIssuesPrice` и `SetIssueRatio` (исправлено в ветке `feature/recalc-join-fix`) прямой `INNER JOIN Issue → TariffWindow` с диапазоном по `dayOriginal` оптимизатор строил как `Hash Match`, вычитывая в build-фазу **весь срез `TariffWindow` за период по всем СМИ** (~165 тыс. строк на месяц, ~2 млн на год), чтобы сматчить его с десятками выпусков одной кампании. Замер на восстановленном проде: 30 мс против 0,2 мс на вызов; на кампании в 4380 выпусков за год — 164 мс против 16 мс. Логических чтений при этом мало (индекс `UX_TariffWindow_dayOriginal_windowID` узкий), поэтому по `STATISTICS IO` проблема не видна — она видна только по CPU. Лечится переписыванием на `CROSS APPLY (SELECT TOP 1 ...)`: `windowId` — PK `TariffWindow`, совпадение не более одного, семантика не меняется, хинты не нужны.
**Почему важно:** Паттерн почти наверняка повторяется. `GetIssuesPrice` была №2 в топе прода по суммарному времени — то есть цена такой формы плана измеряется часами процессорного времени в месяц. Кандидаты по grep (`dayOriginal` + `originalWindowID` в одном теле), в порядке приоритета по известным жалобам:
- `ComboModuleFreeTimeRetrieve` — до 21 с в логе 29.08.2026
- `Grid`, `IssuesDays`
- `stat_GetPrice_proc`, `stat_GetPriceByMonth_proc`, `stat_RollerStatistic`, `statFactorAnalysis`
- `RollerSubstitutionPassport`, `RollerSubstitute`, `CampaignsIssueDelete`, `TariffWindowWithAdvertTypeRetrieve`
- `ActionRecalculate` (фаза 1A) и `hlp_CampaignRecalc` — там агрегат по всем кампаниям акции сразу, hash может быть и оправдан; 12 мс, низкий приоритет

**Возможное направление:** Не переписывать вслепую. Для каждого кандидата снять фактический план и сравнить оценку строк на стороне `TariffWindow` с реальным числом обрабатываемых выпусков; переписывать только там, где виден `Hash Match` с широким срезом по `dayOriginal` и узкой стороной выпусков. Проверять эквивалентность так же, как в `ArtvisDB/Scripts/issues-join-fix-check.sql`.

**Разобранные кандидаты:**
- `MediaPlanRetrieve_v2` — **закрыто, ветка `hotfix/mediaplan-v2-perf`.** Проверено: к этому паттерну процедура отношения не имеет. Её join `Issue → TariffWindow` идёт по `windowId` (PK, равенство), а не по диапазону `dayOriginal`, поэтому `CROSS APPLY` тут ничего не меняет. Настоящая причина — другой класс бага, тот же, что был у `dbo.Firms`: «универсальные» предикаты `i.campaignId = ISNULL(@campaignId, i.campaignID)` / `c.actionID = ISNULL(@actionID, c.actionID)` / `c.agencyID = ISNULL(@agencyId, c.agencyID)`. Оценка селективности вырождается в догадку 10% (`EstimateRows=344423` при `Issue` в 3,44 млн), и один закешированный план — `Hash Match` поверх полного скана `Issue` **и** полного скана `TariffWindow` — обслуживал все три режима вызова. Лечится `OPTION (RECOMPILE)` на обеих ветках заполнения `#issue`: parameter embedding сворачивает NULL-ветки `ISNULL`, предикат становится sargable, план — seek по `Campaign` → seek по `Issue.campaignID` → seek по `TariffWindow.windowId`. Замеры на ArtvisDev: 3,4×–818× по 35 сценариям, худший случай 39,6 с → 0,33 с; цена — ~25 мс компиляции на вызов. Эквивалентность: 0 расхождений на 125 590 строках `#issue`. См. `ArtvisDB/Scripts/mediaplan-v2-perf-deploy.sql` и `mediaplan-v2-perf-check.sql`.
- `MediaPlanRetrieve` (v1) — из кода не вызывается (`Client/Classes/MediaPlan.cs` зовёт только `_v2`), трогать нечего.
- `dbo.Campaigns` — **тот же catch-all баг** (`Campaigns.sql`: `cm.actionID = COALESCE(@actionID, cm.actionID) AND cm.campaignID = COALESCE(@campaignID, cm.campaignID)` и т.п.), ~22 мс/вызов по логу инцидента. C#-фикс N+1 в `MediaPlan.PrintFooter` (ветка `hotfix/mediaplan-v2-perf`, коммит `2e36d7d`) убрал 544 вызова из сводного медиаплана, так что здесь она больше не горячая, но `Campaigns` — core-процедура журналов, используется повсюду. Следующий кандидат на `OPTION (RECOMPILE)` — снять план, подтвердить `Hash Match` / скан, проверить, что параметры не переприсваиваются в теле (иначе parameter embedding не сработает).

**Отдельный кандидат, вынесенный из разбора `MediaPlanRetrieve_v2`:** покрывающий индекс `Issue(campaignID) INCLUDE (actualWindowID, rollerID, positionId)` убирает key lookup при выборке выпусков кампании — логические чтения `Issue` в медиаплане падают с 53 126 до 58. Но на холодном кеше он экономит всего ~900 физических страниц (~10 мс), а стоит 93 МБ на самой горячей на запись таблице системы (у `Issue` уже ~1,09 ГБ индексов, и по ней идёт шторм `ActionRecalculate`). Не окупается в одиночку; имеет смысл вернуться к нему, только если найдётся ещё несколько читателей с тем же профилем — тогда дешевле расширить существующий `IX_Issue_campaignID_covering` двумя INCLUDE-столбцами, чем заводить новый индекс.

---

### [SQL-03] `ActionDeactivate` снимает обвязку политагитации построчным курсором

**Область:** `dbo.ActionDeactivate`, `dbo.AgitationFraming` (`CleanupWindow`)
**Суть:** `ActionDeactivate` в конце крутит курсор по всем агит-окнам акции (`@agitWindows`) и на каждое зовёт `EXEC AgitationFraming @actionName='CleanupWindow'` — в одной неявной транзакции, держа X-блокировки на `Issue`/`TariffWindow` весь срок. После `hotfix/agitation-cleanup-perf` один `CleanupWindow` ≈ 2 мс (было ~276), но при сотнях окон это всё равно RBAR под общей блокировкой. Все цепочки окон в данных имеют длину ≤ 2 (ArtvisDev: 414 цепочек, все длины 2, ветвлений/колец 0), поэтому `CleanupWindow` полностью переписывается в set-based: один проход по `@agitWindows` + их цепочкам, групповая проверка «осталась ли подтверждённая агитация» и групповое удаление.
**Почему важно:** блокировочный след `ActionDeactivate` — та же зона, что в открытой паре дедлоков `Issue`/`stat_GetPrice` ([[project_deadlocks_prod]]). Перф-инцидент 31.08.2026 (таймаут деактивации) закрыт хотфиксом, но архитектурно операция осталась построчной.
**Где смотреть:**
- `ArtvisDB\dbo\Stored Procedures\ActionDeactivate.sql` — курсор `cur_agit_deact` (~стр. 148–162)
- `ArtvisDB\dbo\Stored Procedures\AgitationFraming.sql` — ветка `CleanupWindow`
- 7 других вызывающих `CleanupWindow` (`ActionIUD`, `CampaignIUD`, `CampaignsIssueDelete`, `CampaignTransferDay`, `IssueIUD`, `IssueTransfer`) — при переписывании проверить все

**Возможное направление:** `CleanupWindow` принимает набор окон (TVP или temp-таблица), обрабатывает их цепочки одним set-based проходом; `ActionDeactivate` зовёт её один раз со всем `@agitWindows`.

---

### [SQL-04] «Время профилактики» (`DisabledWindow`): таблица пуста, но её читают 9 объектов

**Область:** `dbo.DisabledWindow` и её читатели
**Суть:** экран ввода «времени профилактики» вырезан 2026-09-19 (ветка `cleanup/dead-menu-branches`: пункт `miDisabledWindows`, сущность 10, действие `AddDisabledWindow`, процедуры `disabledWindows` / `DisabledWindowIUD`). Таблица осталась и **пуста в пяти базах** (ArtvisDev, Artvis, Artvis2, Belgorod, Tumen; в Univer таблицы нет); у действий сущности 10 не было ни одного права у групп — то есть вводить данные было некому. Но читают её:
- проверки при работе с выпусками и окнами (горячий путь): `hlp_IssueVerify` (сообщение `DisabledWindowInsert`), `IssueTransfer` (`DisabledWindowTransfer`), `ProgramIssueIUD` через `fn_IsDisabledWindow` (`DisabledWindowInsertProgram`), `TariffWindowIUD`;
- генерация окон: `GenerateTariffWindowByTemplate`, `sl_GenerateTariffWindowsDay`;
- импорт медиапланов: `CampaignImportGrammofon`, `CampaignImportMediaPlus`;

**Не путать (это живая функция, не трогать):** `TariffWindow.isDisabled` — флаг «запретить вносить выпуски в окна». Он используется массово (на Belgorod ~81 тыс. окон с `isDisabled = 1`). Таблицу `DisabledWindow` он **не** использует: ни `ShowDisabledWindows` (действие «Показать заблокированные окна» у прайс-листа, сущность 80), ни `TariffWindowRetrieve` (параметр `@showDisabledWindows`) её не читают.
**Почему не удалили сразу:** часть читателей — горячие процедуры проверки выпусков и окон; правка каждой требует аккуратной хирургии и проверки, а веб SQL не переносит, так что выигрыш только в чистоте и в паре лишних запросов на каждую проверку выпуска.
**Возможное направление:** спросить владельца: «время профилактики» не нужно? Если нет — по образцу `cleanup/*` (правка процедур из `OBJECT_DEFINITION`, проверка формы вырезаемого фрагмента): убрать проверки из перечисленных процедур, затем `fn_IsDisabledWindow`, три сообщения `DisabledWindow*` и таблицу. `ShowDisabledWindows` и действие «Показать заблокированные окна» остаются. Проверять на ArtvisDev и Belgorod.

---

### [C#-01] Связывание окон трафика — два не-транзакционных `TariffWindowIUD`

**Область:** `Client\Classes\TariffWindowWithRollerIssues.WinForms.cs` (`GroupWithWindow`, `UngroupWindows`)
**Суть:** линковка/раслинковка окон (`windowPrevId`/`windowNextId`) пишет обе стороны связи двумя отдельными round-trip'ами `window.Update()` без транзакции. Сбой второго оставляет **полусвязь** (`w.windowNextId → v`, но `v.windowPrevId ≠ w`). Уникального ограничения/CHECK нет; существование `dbo.CheckLinkedWindows` (детект+ремонт) — прямое свидетельство, что полусвязи в проде есть. Из-за них расходятся forward/backward обходы цепочки — см. [SQL-03] и `docs/tasks/political-agitation-review-findings.md` §2.2 (осиротевшая обвязка политагитации).
**Возможное направление:** обернуть `GroupWithWindow`/`UngroupWindows` в `DataAccessor.BeginTransaction()`/`Commit` (или писать обе стороны одним вызовом `TariffWindowIUD`). Тогда полусвязи станут невозможны, `CheckLinkedWindows` можно вывести из регулярного прогона, а эквивалентность forward/backward обхода — безусловной.

---

### [C#-02] `Action.GetCampaigns` может отдавать `null`-элементы, которые уходят в медиаплан без фильтра

**Область:** `Client\Classes\Action.cs` (`GetCampaigns`, ~стр. 272–279), `Client\Classes\Action.WinForms.cs` (медиаплан по акции), `Client\Classes\MediaPlan.cs`
**Суть:** `Action.GetCampaigns` строит список `Campaign` через `Campaign.GetCampaignById`, который возвращает `null` для удалённой кампании. `BuildAddedIssuesTable` (`Action.cs:384–388`) эти `null` явно отфильтровывает — но `Action.WinForms.cs` (вызовы `MediaPlan.CreateInstance(IList<Campaign>, ...)`) передаёт список как есть, а `MediaPlan.PrintMediaPlan` / `PrintCampaignInfo` разыменовывают элементы без проверки. NRE, если в акции есть удалённая кампания, попавшая в список. На практике маловероятно (`dbo.Campaigns` перечисляет кампании существующей акции), но контракт «список без `null`» нигде не держится.
**Возможное направление:** фильтровать `null` в `Action.GetCampaigns` (или в точке передачи в `MediaPlan`), как это уже делает `BuildAddedIssuesTable`. Предсуществующее, к ветке `hotfix/mediaplan-v2-perf` отношения не имеет (её N+1-фикс `null` кэширует и поведение не меняет).

---

## Веб (FogSoft.Web)

### [WEB-01] Сценарии связей кэшируются статически, а метаданные сущности в вебе — персональные

**Область:** `FogSoft.WinForm\Classes\RelationManager.cs`, древовидные экраны веба (`Browser.razor`)

**Суть:** `RelationManager.scenarios` — статический словарь, заполняемый один раз при первом
`GetScenario` (`Load()` читает `as_relationScenarios`). Каждый `RelationScenario` при построении
резолвит `StartingEntity = EntityManager.GetEntity(StartingEntityID)` и **держит эту ссылку
навсегда**. В десктопе это безопасно: один процесс — один пользователь. В вебе один процесс
обслуживает всех, а объект `Entity` персонален: `EntityInfoRetrieve` вшивает в метаданные права
конкретного пользователя (`dbo.IsActionEnabled(@userID, …)`), ради чего и сделан кэш сущностей
на circuit (`FogSoft.Web\Infrastructure\CircuitEntityCache.cs`). Значит `StartingEntity` во всех
сценариях — это объект того пользователя, чей circuit первым открыл дерево, и он достаётся
всем остальным до перезапуска приложения.

**Почему важно:** сегодня это **латентно, а не активный дефект**: в вебе `StartingEntity`
используется только там, где нужны поля отбора, — `PassportSchema.Parse(scenario.XmlFilter,
scenario.StartingEntity, PageTypes.Filter)` и `Globals.PrepareForFilter(scenario.StartingEntity)`
в `Browser.razor`, — а состав полей фильтра от прав не зависит. Но `Entity` несёт и
`Action.IsEnabled`, то есть права. Первое же обращение к правам через `RootEntity` (например,
если гашение пункта в меню корня начнут решать по нему, а не по контейнеру) молча ответит за
чужого пользователя. Это ровно тот класс дефекта, который уже ловили дважды: общий кэш
сущностей на процесс и кэш «на circuit» вместо «на пользователя» (раздел 7 п.1
`docs/tasks/web-migration.md`).

**Где смотреть:**
- `FogSoft.WinForm\Classes\RelationManager.cs` — статический `scenarios`, `Load()`, `ClearHash()`;
  конструктор `RelationScenario` (поле `StartingEntity`)
- `FogSoft.WinForm\Classes\RelationManager.cs` — для контраста `EntityRelation.ChildEntity`:
  он резолвит сущность **на каждое обращение**, поэтому этой проблемы не имеет
- `FogSoft.Web\Infrastructure\CircuitEntityCache.cs` — как в вебе решена та же задача для
  `EntityManager`
- `FogSoft.Web\Components\Pages\Browser.razor` — единственные сегодняшние читатели
  `scenario.StartingEntity`

**Возможное направление:** самое дешёвое и полное — сделать `StartingEntity` вычисляемым
свойством (`EntityManager.GetEntity(StartingEntityID)` на каждое обращение), как уже устроен
`EntityRelation.ChildEntity`; тогда замороженной ссылки не останется вовсе, а десктоп не
заметит разницы (у него кэш сущностей и так один). Более тяжёлый путь — хранилище сценариев на
circuit по образцу `EntityManager.IEntityCache`. Временной мерой сгодился бы
`RelationManager.ClearHash()` при смене пользователя в `WebLoggedUserStorage`, но он лечит
только вход-выход в одной вкладке, а не одновременную работу двух пользователей.

**Когда браться:** до того, как в вебе появится хоть один читатель прав или селектора атрибутов
через `RelationScenario.StartingEntity`. Правка в ядре, поэтому отдельной задачей, не попутно.

---

### [WEB-02] Обновление узла дерева читает процедуру дважды

**Область:** `FogSoft.Web\Components\Pages\Browser.razor` (`ReloadNode`, `Select`, `LoadChildren`)

**Суть:** у раскрытого и одновременно выбранного узла обновление читает содержимое дважды:

```
ReloadNode(node):
    node.Container.ClearCache();
    node.Children = null;
    if (node.Expanded) node.Children = LoadChildren(node);   // перечисление контейнера → GetContent (чтение №1)
    if (node == _selected) Select(node);                     // ClearCache() + GetContent()  (чтение №2)
```

`LoadChildren` перечисляет контейнер, и `ObjectsIterator` по дороге кэширует прочитанное
(`lastContentFilter`), но `Select` тут же сбрасывает кэш и читает заново. Параметры у обоих
вызовов одинаковые, результат тоже.

**Как обнаружено:** живой прогон журналов рекламных акций 2026-09-20. В
`FogSoft.Web\bin\Debug\net10.0\logs\qd2.log` — две подряд `Actions1` с одинаковыми параметрами,
разница 142 мс, один и тот же `cid` (`bbf0825a`), обе `rows=8`.

**Почему важно:** задевает не журналы акций, а любое дерево и любой путь, ведущий в
`ReloadNode`, — кнопку «Обновить», действие с `ActionEffect.Changed` (в том числе переключатели
вида в журналах акций), удаление узла. Среди процедур, которые при этом удваиваются, —
`HeadCompaniesWithActions`: та самая, с которой начинались таймауты журнала акций 27.07.2026
(`docs/tasks/hcwa-perf.md` в заметках, кандидат в этом же файле по соседству). Удвоение не
меняет результат, поэтому само себя не проявляет: видно только в логе.

**Где смотреть:**
- `FogSoft.Web\Components\Pages\Browser.razor` — `ReloadNode` (~стр. 382), `Select` (~стр. 272),
  `LoadChildren` (~стр. 231)
- `FogSoft.WinForm\Classes\ObjectsIterator.cs` / `ObjectContainer.cs` — кэш `lastContentFilter`
  и его сброс в `ClearCache`

**Возможное направление:** читать один раз и переиспользовать. Самое простое — развести
«перечитать» и «показать»: `Select` не должен звать `ClearCache`, когда вызывающий только что
сам сбросил кэш и загрузил детей (например, параметром `refresh: false`). Тогда `ReloadNode`
делает одно чтение, а одиночный клик по узлу — как и раньше, одно.

**Когда браться:** отдельной небольшой правкой; риск низкий, но она трогает общий движок
деревьев, поэтому не попутно с переносом очередного экрана. Проверять по логу: после правки на
переключение вида в журнале акций должна приходиться одна `Actions1`, а не две.
