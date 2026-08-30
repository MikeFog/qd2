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
- `Grid`, `MediaPlanRetrieve` / `MediaPlanRetrieve_v2`, `IssuesDays`
- `stat_GetPrice_proc`, `stat_GetPriceByMonth_proc`, `stat_RollerStatistic`, `statFactorAnalysis`
- `RollerSubstitutionPassport`, `RollerSubstitute`, `CampaignsIssueDelete`, `TariffWindowWithAdvertTypeRetrieve`
- `ActionRecalculate` (фаза 1A) и `hlp_CampaignRecalc` — там агрегат по всем кампаниям акции сразу, hash может быть и оправдан; 12 мс, низкий приоритет

**Возможное направление:** Не переписывать вслепую. Для каждого кандидата снять фактический план и сравнить оценку строк на стороне `TariffWindow` с реальным числом обрабатываемых выпусков; переписывать только там, где виден `Hash Match` с широким срезом по `dayOriginal` и узкой стороной выпусков. Проверять эквивалентность так же, как в `ArtvisDB/Scripts/issues-join-fix-check.sql`.
