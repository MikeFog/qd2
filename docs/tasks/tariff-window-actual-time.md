# Задача: время выхода рекламных окон в линейной кампании — оригинальное → актуальное

## 1. Проблема

В форме редактирования линейной (простой) рекламной кампании тарифная сетка
(`RollerIssuesGrid3`) в первой колонке показывает **время выхода рекламных окон**.

Каждое тарифное окно (`TariffWindow`) имеет **два** времени:
- `windowDateOriginal` — оригинальное: с каким временем окно было создано из тарифа;
- `windowDateActual` — актуальное: куда окно реально «уехало» после переноса
  (например, создано на 14:20, фактически выходит в 14:50).

Исторически сетка раскладывала и подписывала окна по **оригинальному** времени
(когда переносов ещё не было). Сейчас это неверно: пользователь должен видеть и
работать с **актуальным** временем выхода.

Перенос окна в рамках решаемой задачи возможен **только в пределах того же дня**
(меняется время суток, не дата).

### Почему это не «поменять одну подпись»

- `Issue` (выпуск) **не хранит время** — только `originalWindowID` + `actualWindowID`
  (при вставке оба = одно окно). Время выводится JOIN-ом к `TariffWindow`.
  Поэтому переезд окна в сетке — это вопрос **раскладки/отображения**, а не перезаписи выпусков.
- В C# свойство `TariffWindow.WindowDate` уже возвращает `windowDateActual` —
  объектная модель «актуальна», оригинал жил только в SQL-раскладке сетки.
- Раскладка строится по часу/минуте из процедуры `TariffWindowRetrieve`,
  которая проецировала `[hour]/[min]` из `windowDateOriginal`.

## 2. Предложенные механизмы решения и ограничения

### Что меняем

Раскладка сетки определяется двумя осями:
- **строка (время суток)** — C# фильтрует окна по `hour/min`, *приходящим из процедуры*
  (`TariffWindowGrid.AddWindowCells` → фильтр `price/hour/min`); C# не знает, оригинал это
  или актуал — джойнит по тому, что дала SP;
- **столбец (день недели)** — считается в C# от `windowDateOriginal.DayOfWeek`.

Поскольку перенос — в пределах одного дня, **день недели не меняется**, и достаточно
переключить только проекцию времени суток в процедуре. UI о тонкостях не узнаёт.

Реализация — **необязательный переключатель**, а не глобальная правка:

1. `TariffWindowRetrieve`: добавлен `@useActualTime BIT = 0`. При `1` колонки `[hour]/[min]`
   (а значит и «сетка часов», и список окон) строятся от `windowDateActual`.
2. `MassmediaPricelist.GetTariffWindows(...)`: новый необязательный `useActualTime = false`,
   прокидывается в процедуру.
3. `TariffWindowGrid`: виртуальное `UseActualTime => false`, `populateGrid` передаёт его в SP.
4. `RollerIssuesGrid3`: `override UseActualTime => module == null` — актуал **только** для линейной
   расстановки роликов; при выбранном модуле (модульная кампания) грид остаётся на оригинале.
5. `TrafficGrid` (наследник `RollerIssuesGrid3`): `override UseActualTime => false` — траффик-грид
   управляет окнами по оригинальному расписанию.

### Почему override не на всём `RollerIssuesGrid3` (правка по review)

`RollerIssuesGrid3` переиспользуется шире, чем «линейная расстановка»:
- **Модульные кампании** кладут модуль в тот же грид ([CampaignForm.cs:302](../../Client/Forms/CampaignForm.cs)),
  далее `RefreshGrid()` → SP. Глобальный `=> true` переключил бы и их — вне scope. Гейт `module == null`.
- **`TrafficGrid : RollerIssuesGrid3`** — управление тарифными окнами. Его операции берут время из
  строки грида и шлют в SQL, ищущие по `windowDateOriginal`:
  `TariffWindowMoveTime` ([:49](../../ArtvisDB/dbo/Stored%20Procedures/TariffWindowMoveTime.sql)),
  `TariffWindowChangeDuration` ([:45](../../ArtvisDB/dbo/Stored%20Procedures/TariffWindowChangeDuration.sql)),
  `TariffWindowMassDelete` ([:27](../../ArtvisDB/dbo/Stored%20Procedures/TariffWindowMassDelete.sql)).
  С актуальной подписью `14:50` операция искала бы оригинал `14:50` → промах/чужое окно. Поэтому
  `TrafficGrid` явно остаётся на оригинале.

### Чего НЕ меняем (ограничения и обоснование)

| Не трогаем | Почему |
|---|---|
| Контракт `TariffWindowRetrieve` для прочих вызовов | Параметр необязательный, дефолт `0` (оригинал) — обратная совместимость |
| `TariffWindowGenerationForm` (генерация/управление окнами) | Использует базовый `TariffWindowGrid` → `UseActualTime=false` → оригинал; там окна осмысленны по расписанию создания |
| Модульные кампании (тот же `RollerIssuesGrid3`, но `module != null`) | Гейт `module == null` оставляет их на оригинале |
| `TrafficGrid` (наследник `RollerIssuesGrid3`) | Явный `override => false`; операции переноса/длительности/удаления ищут по `windowDateOriginal` |
| Шаблоны (`FrmGenerator`) и модульные сетки | Зовут `GetTariffWindows` без флага → оригинал |
| Веерные кампании (`TariffWithRangeGrid` / `AddRangeIssues`) | Вынесено в **отдельный этап 2** |
| Столбец (день недели) в `AddWindowCells` | Перенос только в пределах дня → день не меняется |
| Счётчики выпусков по дням (`Grid` SP, `dayOriginal`) | То же — при сдвиге внутри дня остаются верными |
| Выпуски (`Issue`) и их запись | Время не хранится в выпуске; переезд — только раскладка |

### Сознательно отложенные случаи (вне рамок задачи)

- Перенос окна **через границу суток** (23:50 → 00:10) — потребовал бы ещё правки
  дня недели в `AddWindowCells` и счётчиков в `Grid`. В рамках задачи такой перенос невозможен.
- **Коллизия**: два окна, уехавшие в один и тот же актуал (то же время+цена+день),
  попадут в одну ячейку (одно затрёт другое). Вероятность повышается после переносов,
  но в модели оригинала коллизия так же возможна; отдельно не обрабатывается.

## 3. Ветка для review

**Ветка:** `feature/tariff-window-actual-time` (от `master`)

Изменённые исходники (без учёта `Client/app.config` — это постороннее локальное изменение):

| Файл | Суть |
|---|---|
| `ArtvisDB/dbo/Stored Procedures/TariffWindowRetrieve.sql` | `@useActualTime`; `[hour]/[min]` от `windowDateActual` при `1` |
| `Client/Classes/MassmediaPricelist.cs` | `GetTariffWindows(..., useActualTime=false)` + проброс в SP |
| `Client/Controls/TariffWindowGrid.cs` | virtual `UseActualTime => false`; передача в `GetTariffWindows` |
| `Client/Controls/RollerIssuesGrid3.cs` | `override UseActualTime => module == null` (актуал только для линейной расстановки) |
| `Client/Controls/TrafficGrid.cs` | `override UseActualTime => false` (управление окнами — по оригиналу) |

Сопутствующая документация (в этой же ветке):
- `docs/scenarios/campaign-edit-form-load.md` — карта загрузки формы редактирования кампании
- `docs/tasks/tariff-window-actual-time.md` — этот документ

### Статус

- Проект собирается без ошибок (`Merlin.exe`).
- Runtime-проверка: требует деплоя обновлённой `TariffWindowRetrieve` в БД и открытия
  кампании с «уехавшим» окном — убедиться, что окно стоит в строке актуального времени.
- Этап 2 (веер) — выполнен (см. ниже).

## 4. Этап 2 — веерное размещение (`TariffWithRangeGrid`)

### Отличие от линейного

В линейной кампании достаточно было поменять только проекцию времени в `TariffWindowRetrieve`:
добавление выпуска идёт по `WindowId` из ячейки (окно уже в объекте), привязки к времени нет.

В веере наоборот — добавление/удаление работают **по диапазону времени**: клик по слоту шлёт
`WindowDate` слота в SQL, которая ищет окно `windowDateOriginal BETWEEN @issueDate AND +30мин`
(своё окно на каждой радиостанции акции). Поэтому при раскладке грида по `windowDateActual`
обе операции тоже обязаны искать окно по `windowDateActual`, иначе клик/удаление промахнётся.

### Почему БЕЗ флага `@useActualTime` (в отличие от этапа 1)

В этапе 1 флаг нужен: `TariffWindowRetrieve` — **общая** процедура, её зовут и модульные
кампании, и `TrafficGrid`, и форма генерации окон, которым нужно остаться на оригинале.

Три процедуры веера (`TariffWindowWithRange`, `AddRangeIssues`, `MasterIssueDelete`) вызываются
**только** из веерного пути (`TariffWithRangeGrid` / `MasterIssue` / `EditIssuesForm`) — других
вызовов в коде и в SQL нет. Веер всегда должен показывать фактическое время, так что флаг был бы
параметром, всегда стоящим в `1`. Поэтому `windowDateActual` зашит в процедуры напрямую, C#-плоскость
не трогаем.

### Изменённые файлы

| Файл | Суть |
|---|---|
| `ArtvisDB/dbo/Stored Procedures/TariffWindowWithRange.sql` | Раскладка окон в получасовые слоты по `windowDateActual` (в `#tw`, прайм-день, префильтр, три BETWEEN-join'а) |
| `ArtvisDB/dbo/Stored Procedures/AddRangeIssues.sql` | Выбор окна (основной + проверка занятости) по `windowDateActual` |
| `ArtvisDB/dbo/Stored Procedures/MasterIssueDelete.sql` | Поиск выпуска по `windowDateActual` (предикат + `ORDER BY`) |

C#-код не менялся: грид уже передаёт `WindowDate` слота, который теперь = фактическое время.

`TariffWindowWithRange.WindowDate` читает столбец `date` (время слота), поэтому раскладка, `@issueDate`
для add/delete и подсветка `AddedIssues` остаются взаимно согласованными автоматически.

### Ограничения (как в этапе 1)

- Перенос предполагается в пределах того же дня → день недели и счётчики выпусков не меняются.
- Окно с переносом хранит `originalWindowID == actualWindowID` (модель окна-переезда), поэтому
  в `MasterIssueDelete` меняется только проекция времени, join по `originalWindowID` не трогаем.

### Статус этапа 2

- Проект собирается без ошибок (`Merlin.exe`).
- Runtime-проверка: задеплоить `TariffWindowWithRange`, `AddRangeIssues`, `MasterIssueDelete`,
  открыть веерную акцию с «уехавшим» окном — окно в строке актуального времени; клик добавляет,
  Del удаляет тот же слот.
