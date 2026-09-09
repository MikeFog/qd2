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

## 3. Слабые места

### Термины (соглашение)

- **цепочка окон** (звено, голова, хвост) — структура `windowPrevId`/`windowNextId`.
  Слово несёт *порядок*, а именно порядок здесь и ломается.
- **склейка / склеить окна** — действие оператора, создающее цепочку.
- **объединение тарифов / тариф-продолжение** — `TariffUnion`. Отдельное слово,
  чтобы в речи не путать с оконной цепочкой.

### Корень проблемы

**Цепочка окон задаётся по оригинальному времени, эфир идёт по фактическому, и
между ними ничего не синхронизирует.**

- Склейка в гриде трафика ([`TrafficGrid`](../Client/Controls/TrafficGrid.cs),
  `UseActualTime => false`) связывает два окна, соседних по **оригинальному** времени
  в колонке дня.
- [`TariffWindowMoveTime`](../ArtvisDB/dbo/Stored Procedures/TariffWindowMoveTime.sql)
  меняет **только** `windowDateActual`. Не трогает `windowPrevId`/`windowNextId`, не
  перепроверяет порядок, не зовёт `CheckLinkedWindows`.
- Грид трафика по-прежнему рисует цепочку целой (он на оригинальном времени) —
  расхождение **невидимо и в гриде**. Вылезает только в DJin-выгрузке и в обвязке
  политагитации.
- `windowDateOriginal` после рождения окна неизменяемо (ни `TariffWindowIUD`
  `UpdateItem`, ни `MoveTime` его не трогают). Двигать позицию можно только по
  фактической оси — а её никто не сверяет с цепочкой.

### Список

| # | Сценарий | Что ломается | Где | Защита / статус |
|---|---|---|---|---|
| 1 | **`MoveTime` инвертирует цепочку**: фактическое время хвоста делают раньше головы | DJin-выгрузка: пара выходит **двумя отдельными блоками в обратном порядке**, хвост — без заголовка `B…T` и входного джингла, суммирование длительности (`GetNextWindowsDuration` идёт вперёд по строкам) партнёра не находит. `AgitationFraming`: лок. идентификатор СМИ (44) в голове, фед. (55) в хвосте — в эфире звучат в обратном порядке. Зона политагитации. | [TariffWindowMoveTime.sql](../ArtvisDB/dbo/Stored Procedures/TariffWindowMoveTime.sql), [TariffWindowIUD.sql](../ArtvisDB/dbo/Stored Procedures/TariffWindowIUD.sql), [ExportDocument.cs:63-235](../Client/Classes/GridExport/ExportDocument.cs), [AgitationFraming.sql:88-102](../ArtvisDB/dbo/Stored Procedures/AgitationFraming.sql) | **закрыто** (deploy `window-chain-guards`): `MoveTime` и `TariffWindowIUD.UpdateItem` (паспорт «Свойства») проверяют порядок цепочки по будущему факт. времени → `LinkedWindowsWrongOrder`. Полусвязи (#6) обходят проверку |
| 2 | `MoveTime` — **массовая** операция (диапазон дат + маска дней недели) | цепочка, склеенная на конкретный день, попадает под общий сдвиг без всякого учёта | [TrafficGrid.cs:544-568](../Client/Controls/TrafficGrid.cs) | нет |
| 3 | **Нет инварианта непрерывности** | `windowPrevId`/`windowNextId` нигде не проверяет, что голова кончается там, где начинается хвост. `ChangeDuration` растягивает/сжимает звено → «непрерывный кусок эфира» разъезжается (наложение или дыра). `CheckWindowOverflow` в гриде — на окно, не на цепочку | [TariffWindowChangeDuration.sql](../ArtvisDB/dbo/Stored Procedures/TariffWindowChangeDuration.sql) | нет. Запрет менять продолжительность объединённого окна был реализован (`CannotChangeDurationOfLinkedWindow`), но **снят** — заказчик не подтвердил потребность (обсуждается) |
| 4 | `ChangeDuration` / `ChangeDurationInDay` **не знают о цепочках**, не зовут `CheckLinkedWindows`. `ChangeDurationInDay` вдобавок не трогает `duration_total` | рассинхрон `duration` vs `duration_total`, а выгрузка суммирует `duration_total` | [TariffWindowChangeDurationInDay.sql](../ArtvisDB/dbo/Stored Procedures/TariffWindowChangeDurationInDay.sql) | нет (см. #3) |
| 5 | **Регенерация окон стирает цепочки** (не `TariffUnion`) | `CheckLinkedWindows` обнуляет обе стороны + шлёт уведомление трафику; оператор склеивает заново после каждой регенерации. Легко забыть → цепочка тихо исчезает перед эфиром | [CheckLinkedWindows.sql](../ArtvisDB/dbo/Stored Procedures/CheckLinkedWindows.sql), [GenerateTariffWindows.sql](../ArtvisDB/dbo/Stored Procedures/GenerateTariffWindows.sql) (проверки цепочек нет) | уведомление, не блокировка |
| 6 | **`[C#-01]` Не-транзакционная запись двух сторон.** `GroupWithWindow`/`UngroupWindows` пишут `windowPrevId` и `windowNextId` двумя round-trip без транзакции | сбой второго → **полусвязь** (`w.windowNextId → v`, но `v.windowPrevId ≠ w`). Уникального ограничения/CHECK нет | [TariffWindowWithRollerIssues.WinForms.cs:39-89](../Client/Classes/TariffWindowWithRollerIssues.WinForms.cs) | `CheckLinkedWindows` — костыль-ремонт; фикс: транзакция либо один `TariffWindowIUD`. `docs/IMPROVEMENTS.md` `[C#-01]` |
| 7 | **Расхождение обхода вперёд/назад** при несимметричных ссылках | множество «окон цепочки вперёд» ≠ «назад». В `AgitationFraming.CleanupWindow` давало осиротевшую обвязку политагитации | `docs/tasks/political-agitation-review-findings.md` §2.2 | закрыто в `hotfix/agitation-cleanup-perf` (оба направления одним forward-проходом); остаточный дефект «не-вычищаемая обвязка за полуразрывом» отслеживается |
| 8 | **Нет защиты от кольца.** Все обходы (`fn_AgitationChainFirst`, `AgitationFraming`, `GetNextWindowsDuration`) — `WHILE 1=1` по `prev`/`next` без visited-множества | кольцо `windowPrevId`/`windowNextId` → вечный цикл | `docs/tasks/political-agitation-review-findings.md` §193 | нет |
| 9 | **Две разные склейки сходятся в одном `isExtension`** | и `TariffUnion`, и `windowPrevId` дают `isExtension = true` и кормят `GetNextWindowsDuration` — но ветка выбирается по наличию `tariffUnionID`, поэтому **оконная цепочка молча игнорируется**, если у тарифа есть объединение | [ExportDocument.cs:196-232](../Client/Classes/GridExport/ExportDocument.cs) | нет. Запрет объединять окна при `IsTariffUnited` был реализован в C#, но **снят** — заказчик не подтвердил потребность (обсуждается) |
| 10 | **Цепочка почти невидима в UI** | только зелёная ячейка в гриде трафика (`LightSeaGreen`). Диалоги `MoveTime` / `ChangeDuration` / удаления не показывают «это окно склеено» — оператор действует вслепую | [TrafficGrid.cs:197](../Client/Controls/TrafficGrid.cs) | нет |
| 11 | Время/капасити считается **по окну, не по цепочке** (похоже by design — подтвердить у заказчика) | цепочка — чисто отображательно-выгрузочный концепт; `timeInUseConfirmed`/`maxCapacity` в каждом звене отдельно. Оператор может считать «блок большой» и переполнить одно звено | весь размещающий код | — |
| 12 | Удаление среднего звена `A→B→C` | FK `A.windowNextId = B` мешает прямому удалению; путь через регенерацию / `CheckLinkedWindows` оставляет полуразорванные хвосты → осиротевшая обвязка политагитации | [TariffWindowIUD.sql:43-60](../ArtvisDB/dbo/Stored Procedures/TariffWindowIUD.sql) | детектор `political-agitation-orphan-framing-check.sql` |
| 13 | **Тариф между объединёнными тарифами** | новый/изменённый тариф вклинивается между двумя `TariffUnion`-связанными | [TariffIUD.sql:83-132](../ArtvisDB/dbo/Stored Procedures/TariffIUD.sql) | `TariffChainDamage` — **есть**, но на хрупкой логике дней недели / `broadcastStart` (та же, что в `fn_FindTariffIDForChain`); крайние случаи (переход через сутки, совпадение времени) вероятны |
| 14 | **Окно между звеньями цепочки по оригинальному времени** | новое окно встаёт внутрь цепочки | [f_CheckLinkedTariffWindows.sql](../ArtvisDB/dbo/Functions/f_CheckLinkedTariffWindows.sql) (guard `InsideLinkedWindowError` в `TariffWindowIUD AddItem`, `GenerateTariffWindowByTemplate`) | проверяет **только непосредственного предшественника**; **обходится обратной полусвязью** (`B.windowNextId IS NULL`, но `C.windowPrevId = B`); в `GenerateTariffWindows` отсутствует |
| 15 | **Окно в эфирный промежуток цепочки** | окно физически звучит посреди склеенного блока | — | **нет защиты**: (а) `AddItem` с `windowDateOriginal` вне цепочки, но `windowDateActual` внутри — guard смотрит только на original; (б) `MoveTime` — слепой `UPDATE windowDateActual`, ноль проверок |
| 16 | **Каскад `TariffUnion` односторонний** | `ON DELETE CASCADE` только на `FK_TariffUnion_Tariff` (`tariffID`) | [TariffUnion.sql](../ArtvisDB/dbo/Tables/TariffUnion.sql) | удаление тарифа-продолжения спасает `TariffIUD DeleteItem` (чистит по `tariffUnionID` до `DELETE Tariff`); прямой `DELETE FROM Tariff` упрётся в FK |

Самые острые — **#1, #3, #9, #15**: молча портят файл, уходящий в эфирную
автоматизацию; #1 и частично #12 — в политагитации.

### Внесённые ограничения (deploy `window-chain-guards`)

Реализован **только запрет #1** — перенос объединённого окна в неправильный
порядок. `ArtvisDB/Scripts/window-chain-guards-deploy.sql` (2 процедуры
`CREATE OR ALTER` + 1 строка `iMessage`). C# не трогали. Тестирование:
`docs/tasks/window-chain-guards-testing.md`.

| Что | Как | Отказ |
|---|---|---|
| `TariffWindowMoveTime` (шаблонный перенос из сетки трафика) | перед переносом проверяет, что порядок каждой затронутой цепочки по **будущему** факт. времени сохранится (голова строго раньше хвоста) | `LinkedWindowsWrongOrder` |
| `TariffWindowIUD` `UpdateItem` (паспорт окна «Свойства» → «Время выхода реальное»; там же пишутся связи при объединении/отмене) | та же проверка порядка. Срабатывает только при реальном изменении `windowDateActual` ИЛИ при установке связи; правки `isDisabled`/`price`/продолжительности объединение не задевают | `LinkedWindowsWrongOrder` |

**Обсуждается с заказчиком (не реализовано):**
- #3 — запрет менять продолжительность объединённого окна (был сделан, снят);
- #9 — запрет объединять окна, если тариф уже «тариф-продолжение» (был сделан
  в C#, снят; вместе с ним откачена и null-safe правка геттера `IsTariffUnited`).

**Что осталось непокрытым (кроме #3/#9 выше):** полусвязи (#6) обходят проверку
порядка (если `a.windowNextId → b`, но `b.windowPrevId ≠ a` — пара проверяется
только в одну сторону); #15 (в эфирный промежуток цепочки можно поставить
**чужое** окно: `AddItem` с расхождением original/actual, либо `MoveTime`
не входящего в цепочку окна); инвариант непрерывности как таковой; #2, #5, #7, #8,
#10–14, #16.

### Направления решения (на обсуждение, не выбрано)

- **A.** Запретить `MoveTime` / `ChangeDuration` на звене цепочки (просто; оператору
  придётся расклеить → подвинуть → склеить).
- **B.** `MoveTime` двигает всю цепочку целиком, сохраняя относительные смещения;
  `ChangeDuration` пересчитывает/сдвигает соседей.
- **C.** Убрать оконную цепочку вовсе — расширять одно окно через уже существующий
  `Extend` ([`FrmWindowTariffTemplate`](../Client/Classes/TariffWindowWithRollerIssues.WinForms.cs:28)).
  Радикально, снимает целый класс багов.
- **D.** Хранить цепочку как одно «составное окно» с явным порядком и суммарной
  длительностью, звенья — подчинённые. Большая переделка.

Общие предпосылки для A–D: транзакционная запись обеих сторон (#6), симметрия
`prev`/`next` через CHECK или единый `TariffWindowIUD`-вызов, счётчик в обходах (#8).

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
