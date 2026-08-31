# Кандидаты на улучшение

Здесь фиксируются наблюдения, которые **не являются ошибками**, но заслуживают внимания при рефакторинге или оптимизации.
Каждый пункт содержит: область, суть, обоснование, указатели на код.

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

### [C#-01] Связывание окон трафика — два не-транзакционных `TariffWindowIUD`

**Область:** `Client\Classes\TariffWindowWithRollerIssues.WinForms.cs` (`GroupWithWindow`, `UngroupWindows`)
**Суть:** линковка/раслинковка окон (`windowPrevId`/`windowNextId`) пишет обе стороны связи двумя отдельными round-trip'ами `window.Update()` без транзакции. Сбой второго оставляет **полусвязь** (`w.windowNextId → v`, но `v.windowPrevId ≠ w`). Уникального ограничения/CHECK нет; существование `dbo.CheckLinkedWindows` (детект+ремонт) — прямое свидетельство, что полусвязи в проде есть. Из-за них расходятся forward/backward обходы цепочки — см. [SQL-03] и `docs/tasks/political-agitation-review-findings.md` §2.2 (осиротевшая обвязка политагитации).
**Возможное направление:** обернуть `GroupWithWindow`/`UngroupWindows` в `DataAccessor.BeginTransaction()`/`Commit` (или писать обе стороны одним вызовом `TariffWindowIUD`). Тогда полусвязи станут невозможны, `CheckLinkedWindows` можно вывести из регулярного прогона, а эквивалентность forward/backward обхода — безусловной.
