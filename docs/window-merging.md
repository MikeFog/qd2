# Склейка окон — справочник

В системе есть **два независимых механизма «склейки»**, которые часто путают, потому
что в эфире и в DJin-выгрузке они дают один и тот же результат — несколько подряд
идущих рекламных окон звучат как один непрерывный блок (общая длительность, одно
обрамление джинглами). Но живут они на разных уровнях и настраиваются по-разному.

| | `TariffUnion` | `TariffWindow.windowPrevId` / `windowNextId` |
|---|---|---|
| Уровень | тариф прайс-листа (постоянная настройка) | конкретное рекламное окно конкретного дня |
| Кто настраивает | трафик-менеджер в паспорте тарифа | трафик-менеджер в форме управления трафиком, по клику на ячейке дня |
| Область действия | все окна, сгенерированные из этих тарифов, на весь срок прайс-листа | ровно эти `windowId`, разово |
| Структура | односвязный список вперёд (`tariffID → tariffUnionID`) | двусвязный список (`prev` ⇄ `next`), симметрия ничем не гарантирована |
| Доля размещений | штатная фича прайс-листа | ~1% окон (подтверждено заказчиком 2026-07-24) |
| Клонируется с прайс-листом | да (`PricelistIUD` `Clone`) | нет (привязка к `windowId`, теряется при регенерации окон) |

Комментарий в коде, где обе ветки сходятся —
[ExportDocument.cs:196](../Client/Classes/GridExport/ExportDocument.cs): *«there're 2
possibilities how to union windows — through tariffs and through windows»*.

---

## 1. `TariffUnion` — «тариф-продолжение» на уровне прайс-листа

### Схема

[`ArtvisDB/dbo/Tables/TariffUnion.sql`](../ArtvisDB/dbo/Tables/TariffUnion.sql)

```
tariffID       int  -- «этот» тариф (более ранний по времени выхода)
tariffUnionID  int  -- его ПРОДОЛЖЕНИЕ: следующий тариф, звучащий тем же куском эфира
PK (tariffID, tariffUnionID)
FK tariffID      → Tariff  ON DELETE CASCADE
FK tariffUnionID → Tariff  (без каскада)
```

- Направление: строка `(A, B)` означает «B — продолжение A», причём `B.time > A.time`
  (в шкале с учётом `broadcastStart`, см. `fn_GetTariffTimesWithBroadcast`).
- «Есть ли у тарифа продолжение» → `EXISTS (… tu.tariffID = X)`.
- «Является ли тариф чьим-то продолжением» → `EXISTS (… tu.tariffUnionID = X)`.
- Цепочка строится транзитивно: `A.tariffUnionID = B`, `B.tariffUnionID = C`. В данных
  ArtvisDev все цепочки длиной ≤ 2 (см. IMPROVEMENTS `[SQL-03]`).
- Один тариф может быть продолжением только одного (`TariffUnionErrorAlreadyUsed`).

### Как настраивается

Паспорт тарифа: чекбокс `isUnionEnable` + лукап `tariffUnionID`
([`Client/Forms/TariffPassport.cs`](../Client/Forms/TariffPassport.cs) — только
показывает/прячет поле).

Значение лукапа отдаёт [`TariffPassport.sql`](../ArtvisDB/dbo/Stored Procedures/TariffPassport.sql):
он через [`fn_FindTariffIDForChain`](../ArtvisDB/dbo/Functions/fn_FindTariffIDForChain.sql)
вычисляет **единственный допустимый** следующий тариф — ближайший по времени, с
полным совпадением набора дней недели. То есть оператор не выбирает произвольный
тариф, а лишь подтверждает механически определённого соседа.

Запись — [`TariffIUD.sql`](../ArtvisDB/dbo/Stored Procedures/TariffIUD.sql),
`AddItem` / `UpdateItem` / `Clone` / `DeleteItem`:

- `insert into TariffUnion (tariffID, tariffUnionID) values (@tariffID, @tariffUnionID)`
- `DeleteItem`: `delete from TariffUnion where tariffID = @tariffID or tariffUnionID = @tariffID`
  (обе стороны — каскада по `tariffUnionID` в схеме нет).
- Проверки целостности цепочки:
  - `TariffAlreadyExists`, `TariffConflictsWithSponsorTariff` — общие.
  - `TariffChainDamage` — новый/изменённый тариф не должен «влезать» между двумя
    уже объединёнными тарифами (проверка по всем 7 дням недели через `UNION` семи
    `TOP 1`, [TariffIUD.sql:92-131](../ArtvisDB/dbo/Stored Procedures/TariffIUD.sql)).
  - `TariffChainWrongUpdate` — после правки тарифа из цепочки механический «next»
    (`fn_FindTariffIDForChain`) обязан совпасть с сохранённым `tariffUnionID`.
- `fn_GetTariffTimesWithBroadcast(@time, @broadcastStart)` — вспомогательная: время
  ДО начала вещания сдвигается на +1 день, чтобы «ночной» хвост суток сортировался
  после вечера.

### Клонирование прайс-листа

`PricelistIUD` `@actionName='Clone'` переносит `TariffUnion` через промежуточную
`@tariffMap (oldTariffID → newTariffID)` — сопоставление по `(time + дни недели)`
сломалось после доработки клона (см. [[project_pricelist_clone_window_overrides]]),
поэтому теперь `MERGE … OUTPUT`.
[PricelistIUD.sql:53-97](../ArtvisDB/dbo/Stored Procedures/PricelistIUD.sql).

### Кто читает

| Место | Что делает |
|---|---|
| [`TariffWindowRetrieve.sql`](../ArtvisDB/dbo/Stored Procedures/TariffWindowRetrieve.sql):155 | флаг `IsTariffUnited` (1, если тариф участвует в союзе **любой** стороной: `f.tariffId = tu.tariffID OR f.tariffId = tu.tariffUnionID`) |
| [`rpt_Grid.sql`](../ArtvisDB/dbo/Stored Procedures/rpt_Grid.sql) / `rpt_Grid_v2` / `rpt_Grid_v3` | `LEFT JOIN … ON t.tariffID = tu.tariffID`, прокидывают колонку `tariffUnionID` в грид/выгрузку; `ORDER BY … windowPrevId` |
| [`ModuleTariffs.sql`](../ArtvisDB/dbo/Stored Procedures/ModuleTariffs.sql), [`sl_TariffRetrieve.sql`](../ArtvisDB/dbo/Stored Procedures/sl_TariffRetrieve.sql) | отдают `isUnionEnable` / `tariffUnionID` для списков тарифов модуля |
| [`TrafficGrid.cs`](../Client/Controls/TrafficGrid.cs):198 | `IsTariffUnited` → красит ячейку в бледно-зелёный (`MarkCellAsUnited`, `Color.FromArgb(217,242,208)`) |
| [`ExportDocument.cs`](../Client/Classes/GridExport/ExportDocument.cs) | **основной потребитель** — см. ниже |

### Семантика в DJin-выгрузке

[`ExportDocument.ExportBlocks`](../Client/Classes/GridExport/ExportDocument.cs:63) идёт
по строкам грида (отсортированным `tariffTime`, затем `windowPrevId`) и держит
`lastTarrifID` / `lastTarrifUnionID`:

- строка считается **продолжением** (`isExtension = true`), если
  `lastTarrifUnionID == tariffID` текущей строки (**или** у строки есть `windowPrevId`
  — вторая ветка склейки, ниже);
- для продолжения не печатается заголовок блока `B…T`, не печатается входной джингл;
- длительность блока = своя (`durationTotal`) + `GetNextWindowsDuration(...)`, которая
  идёт вперёд по строкам и суммирует `durationTotal` тех, чей `tariffID` равен
  текущему `tariffUnionId`, переходя по цепочке
  ([ExportDocument.cs:190-235](../Client/Classes/GridExport/ExportDocument.cs));
- выходной джингл / `ext` (`NeedOutJingle`, `NeedExt`) откладываются на конец
  всей связки.

---

## 2. `windowPrevId` / `windowNextId` — склейка конкретных окон дня

### Схема

[`ArtvisDB/dbo/Tables/TariffWindow.sql`](../ArtvisDB/dbo/Tables/TariffWindow.sql):23-24

```
windowPrevId  int NULL  FK → TariffWindow.windowId   -- предыдущее окно
windowNextId  int NULL  FK → TariffWindow.windowId   -- следующее окно
IX_TariffWindow_Prev (windowPrevId), IX_TariffWindow_Next (windowNextId)
```

Двусвязный список: в норме `w.windowNextId = v ⇔ v.windowPrevId = w`. **Симметрия
ничем не обеспечена** — ни уникальным индексом, ни CHECK, ни триггером (см. дефекты).

Смысл: несколько строк `TariffWindow` **одного дня**, которые физически являются
одним непрерывным куском эфира. Это тот самый ~1%, который `TariffUnion` (уровень
прайс-листа) не покрывает, потому что склейка нужна не каждый день.

### Как настраивается

Форма управления трафиком → [`TrafficGrid`](../Client/Controls/TrafficGrid.cs)
(`TrafficManagementForm.grdTariffWindow`). Действия метаданных
`GroupWithNext` / `GroupWithPrev` / `UngroupNext` / `UngroupPrev` →
[`TariffWindowWithRollerIssues.DoAction`](../Client/Classes/TariffWindowWithRollerIssues.WinForms.cs):13
→ `GroupWithWindow(isWithPrev)` / `UngroupWindows(isWithPrev)`.

- Соседнее окно ищется в той же колонке (дне) грида: `FindPrevWindowInDay` /
  `FindNextWindowInDay`. Действие доступно только если строка не первая/не последняя
  (`IsGroupWithPrevEnabled` / `NextEnabled`,
  [TrafficGrid.cs:194-195](../Client/Controls/TrafficGrid.cs)).
- `GroupWithWindow(true)`: у найденного предыдущего окна `windowNextId = this.WindowId`,
  у текущего `windowPrevId = prev.WindowId`.
- Запись — **два отдельных** `TariffWindowIUD` `UpdateItem` (по одному на сторону),
  **без транзакции** (`window.Update()` … `this.Update()`).
  → см. дефект [C#-01].
- `UngroupWindows` — то же, но с `NULL` и подтверждением `UserInteraction.Confirm`.

[`TariffWindowIUD.sql`](../ArtvisDB/dbo/Stored Procedures/TariffWindowIUD.sql):

- `UpdateItem` пишет `windowPrevId` / `windowNextId` как есть из параметров.
- `AddItem`: `InsideLinkedWindowError`, если новое окно попадает «внутрь» связанной
  цепочки — [`f_CheckLinkedTariffWindows`](../ArtvisDB/dbo/Functions/f_CheckLinkedTariffWindows.sql)
  (есть ли более раннее по `windowDateOriginal` окно с непустым `windowNextId`).
  Та же ошибка в `GenerateTariffWindowByTemplate`.
- `DeleteItem` связи не чистит; FK не даст удалить окно, на которое ещё ссылается
  сосед.

### Регенерация окон рвёт цепочки

`GenerateTariffWindows` колонки `windowPrevId` / `windowNextId` не заполняет — новые
окна получают новые `windowId`, старые ссылки повисают.
[`CheckLinkedWindows`](../ArtvisDB/dbo/Stored Procedures/CheckLinkedWindows.sql)
вызывается из
[`MassmediaPricelist.CheckLinkedWindows`](../Client/Classes/MassmediaPricelist.cs:161)
после операций с диапазоном дат: курсором по окнам с непустым `windowNextId` ищет
«оборванный» конец (нет более позднего окна с `windowPrevId`) и в этом случае
обнуляет обе стороны + шлёт уведомление трафик-менеджеру
(`SayTrafficThatWindowLinkDeleted`). **Само существование этой процедуры (детект +
ремонт) — прямое свидетельство, что полусвязи в проде есть.**

### Кто читает

| Место | Что делает |
|---|---|
| [`fn_AgitationChainFirst`](../ArtvisDB/dbo/Functions/fn_AgitationChainFirst.sql) | первое окно цепочки (идти по `windowPrevId` до NULL) |
| [`AgitationFraming.sql`](../ArtvisDB/dbo/Stored Procedures/AgitationFraming.sql) `InsertForWindow` | идёт к началу цепочки (`windowPrevId`) и к концу (`windowNextId`): идентификатор локального СМИ (тип 44) — в первое окно, федерального (55) — в последнее, анонс агитации (7) — в само окно |
| `AgitationFraming` `CleanupWindow` | обходит цепочку от `@cFirst` по `windowNextId`, собирая `@chainWindows`; обвязку снимает только если во всей цепочке не осталось подтверждённой агитации (тип 6) |
| `rpt_Grid*` | прокидывают `windowNextId` / `windowPrevId` клиенту; `ORDER BY … windowPrevId` (продолжение сортируется после родителя) |
| [`ExportDocument.GetNextWindowsDuration`](../Client/Classes/GridExport/ExportDocument.cs:214) (ветка `else`) | идёт вперёд по строкам, для строк с непустым `windowPrevId` суммирует `durationTotal` по различным `tariffId`; `isExtension` тоже включается при наличии `windowPrevId` |
| [`TrafficGrid.cs`](../Client/Controls/TrafficGrid.cs):197 | `window.IsInGroup` → красит ячейку в `LightSeaGreen` (`MarkCellAsGroup`) |

---

## 3. Известные дефекты и грабли

1. **`[C#-01]` Не-транзакционная запись двух сторон связи.**
   [`GroupWithWindow`/`UngroupWindows`](../Client/Classes/TariffWindowWithRollerIssues.WinForms.cs)
   пишут `windowPrevId` и `windowNextId` двумя отдельными round-trip без транзакции.
   Сбой второго → **полусвязь** (`w.windowNextId → v`, но `v.windowPrevId ≠ w`).
   Уникального ограничения/CHECK нет. `CheckLinkedWindows` — костыль-ремонт.
   Направление фикса: обернуть в `DataAccessor.BeginTransaction()` / `Commit` либо
   писать обе стороны одним `TariffWindowIUD`. См. `docs/IMPROVEMENTS.md` `[C#-01]`.

2. **Расхождение обхода вперёд/назад.** При несимметричных ссылках множество
   «окон цепочки вперёд» ≠ «назад». В `AgitationFraming.CleanupWindow` это давало
   осиротевшую обвязку политагитации; закрыто в `hotfix/agitation-cleanup-perf`
   (оба направления теперь по одному forward-проходу), но остаточный дефект —
   «не-вычищаемая» обвязка за полуразрывом — отслеживается.
   См. `docs/tasks/political-agitation-review-findings.md` §2.2.

3. **Нет защиты от кольца.** Все циклы обхода (`fn_AgitationChainFirst`,
   `AgitationFraming`, `GetNextWindowsDuration`) — `WHILE 1=1` по `prev`/`next` без
   счётчика/visited-множества. Кольцо `windowPrevId`/`windowNextId` → вечный цикл.
   `docs/tasks/political-agitation-review-findings.md` §193.

4. **`GenerateTariffWindows` затирает цепочки окон** (не `TariffUnion`). После
   регенерации связи `windowPrevId`/`windowNextId` повисают, `CheckLinkedWindows`
   их обнуляет постфактум с уведомлением.

5. **Две разные склейки сходятся в одном `isExtension`.** В DJin-выгрузке и
   `TariffUnion`, и `windowPrevId` дают `isExtension = true` и обе кормят
   `GetNextWindowsDuration` (разными ветками). Правя одну — проверять обе.

6. **Каскад `TariffUnion` односторонний.** `ON DELETE CASCADE` только на
   `FK_TariffUnion_Tariff` (`tariffID`); удаление тарифа-продолжения защищено тем,
   что `TariffIUD` `DeleteItem` сам чистит строку по `tariffUnionID` до `DELETE Tariff`.
   Прямой `DELETE FROM Tariff` в обход процедуры упрётся в FK.

---

## Точки входа — краткий индекс

**`TariffUnion` пишут:** `TariffIUD` (Add/Update/Delete/Clone), `PricelistIUD` (Clone).
**`TariffUnion` читают:** `TariffWindowRetrieve`, `rpt_Grid*`, `ModuleTariffs`,
`sl_TariffRetrieve`, `ExportDocument`, `TrafficGrid`.

**`windowPrevId`/`windowNextId` пишут:** `TariffWindowIUD` (`UpdateItem`) через
`TariffWindowWithRollerIssues.GroupWithWindow`/`UngroupWindows`; `CheckLinkedWindows`
(обнуление при ремонте).
**читают:** `fn_AgitationChainFirst`, `AgitationFraming`, `rpt_Grid*`,
`ExportDocument`, `TrafficGrid`.
