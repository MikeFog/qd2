> Исследование проведено 21.09.2026. Шаг 1 (обнуление спонсорских `03:00`) — на проде
> с 22.09.2026, закрытие сентября прошло без проблем. Инвентарь сверен с кодом и базами
> заново 06.10.2026. Шаг 2 (удаление мёртвых объектов) — `Deploy/20`, на проде с 06.10.2026
> без ошибок; шаг 3 (схлопнуть слой 1) — `Deploy/29`, на ArtvisDev с 10.10.2026, замер «до/после» — 0 различий. Очерёдность с переходом на фактическое окно
> выпуска — §7, «Порядок с переходом на фактическое окно» (08.10.2026).

# `broadcastStart` — справочник и оценка удаления

`broadcastStart` («начало эфирного дня») — время, с которого начинаются эфирные сутки.
Всё, что звучит раньше него, относится к **предыдущему** эфирному дню: тариф на 01:00
понедельника — это ночь с понедельника на вторник, а не ночь с воскресенья на
понедельник. От такой модели суток в своё время отказались, но поле и вся обслуживающая
его арифметика остались и разошлись по системе.

**Краткий вывод:** поле мертво в данных на всех проверенных инсталляциях, выигрыша в
быстродействии от его удаления практически нет, а основная выгода — минус ~50 объектов
нечитаемой арифметики и закрытие целого класса багов «сетка уехала на день». Риск
низкий при условии, что проверены все инсталляции (см. [Шаг 0](#шаг-0--проверка-обязательно)).

---

## 1. Схема

| Таблица | Колонка | Кого обслуживает |
|---|---|---|
| [`Pricelist`](../ArtvisDB/dbo/Tables/Pricelist.sql) | `broadcastStart [DF_TIME] NOT NULL DEFAULT '01.01.1900'` | линейная и модульная реклама |
| [`SponsorProgramPricelist`](../ArtvisDB/dbo/Tables/SponsorProgramPricelist.sql) | то же | спонсорские программы |

Механика реализована двумя приёмами, и это различие важно — цена удаления у них разная:

**A. Сдвиг** — `issueDate ± broadcastStart` перед приведением к дате:
```sql
CONVERT(datetime, CONVERT(varchar(8),
    DATEADD(mi, -DATEPART(mi, pl.broadcastStart),
        DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)), 112), 112)
```
При `broadcastStart = 00:00` это тождественно `dbo.ToShortDate(i.issueDate)`.

**B. Ветвление** — «если время раньше начала суток, взять день недели на одну позицию назад»:
```sql
(t.[time] >= @broadcastStart AND ((t.monday = 1 And @weekday = 1) Or ...))
OR
(t.[time] <  @broadcastStart AND ((t.monday = 1 And @weekday = 7) Or ...))
```
При `00:00` вторая ветка недостижима.

Две служебные функции инкапсулируют оба приёма:
- [`fn_GetTariffTimesWithBroadcast`](../ArtvisDB/dbo/Functions/fn_GetTariffTimesWithBroadcast.sql) — `+1 день`, если время < `broadcastStart`;
- [`fn_GetTimeString`](../ArtvisDB/dbo/Functions/fn_GetTimeString.sql) — формат `HH:mm`, причём час **+24** для «ночной» части суток (даёт `25:30`).

---

## 2. Состояние данных

Проверено на `ArtvisDev` и `Artvis` (копия прода) 21.09.2026, на `Tumen` (локальная
копия, доведённая до уровня ArtvisDev) 06.10.2026. Значения — до шага 1.
`Belgorod` на сервере есть, но **OFFLINE — не проверялась**; `Univer` и `Artvis2` на
локальном сервере больше нет.

| Проверка | ArtvisDev | Artvis | Tumen |
|---|---|---|---|
| `Pricelist.broadcastStart` | **138 из 138 = 00:00** | **136 из 136 = 00:00** | **12 из 12 = 00:00** |
| `SponsorProgramPricelist.broadcastStart` | 39 = 03:00, 1 = 00:00 | 39 = 03:00, 1 = 00:00 | 6 = 03:00 |
| `SponsorTariff` с `time < 03:00` | **0** | — | **0** |
| `ProgramIssue` в зоне 00:00–03:00 | **0** из 2857 | — | **0** (спонсорских выпусков нет вообще) |
| `TariffWindow`: `dayActual` ≠ дате `windowDateActual` | **0 из 2 010 628** | — | **0 из 229 389** |
| `TariffWindow`: `dayOriginal` ≠ дате `windowDateOriginal` | **0 из 2 010 628** | — | **0 из 229 389** |
| `Campaign` с ненулевым временем в `startDate`/`finishDate` | **0 из 38 577** | — | **0** |

Последние три строки — решающие. `dayActual` / `dayOriginal` **персистентны** и пишутся
формулой «минус `broadcastStart`» ([`TariffWindowIUD`](../ArtvisDB/dbo/Stored%20Procedures/TariffWindowIUD.sql)).
Ноль расхождений на двух миллионах строк означает: сдвига суток в данных нет и в
сохранившейся истории не было.

### Почему спонсорские `03:00` не считаются живым использованием

Значение выглядит рабочим — оно есть даже в прайс-листах на 2027 год. Но:

- **Ветвление (приём B) не срабатывает**: нет ни одного `SponsorTariff` раньше 03:00.
- **Сдвиг (приём A) эквивалентен нулю**: он двигает границы отбора на 3 часа, но в зоне
  00:00–03:00 нет ни одного выпуска, поэтому множество отобранных строк не меняется.
- **В сохраняемые данные 3 часа не попадают**: единственное такое место —
  [`ActionRecalculate`](../ArtvisDB/dbo/Stored%20Procedures/ActionRecalculate.sql) (`startDate = MIN(issueDate − 3ч)`) —
  обёрнуто в `dbo.ToShortDate(...)`, и факт это подтверждает (нулевое время у всех кампаний).

`03:00` держится исключительно на копировании при клонировании прайс-листа.

---

## 3. Поле нельзя задать из интерфейса

В метаданных ровно один атрибут — сущность **80 «Прайс-лист»**, `broadcastStartString`
(«Начало эфирного дня»), и он **только для показа**: вычисляется в
[`Pricelists`](../ArtvisDB/dbo/Stored%20Procedures/Pricelists.sql) как
`CONVERT(varchar(5), pl.broadcastStart, 114)`. Атрибут виден и в вебе (журнал строится по
метаданным; есть испанский перевод в `iTranslation`). У спонсорского прайс-листа
(сущность 12) атрибута нет вообще.

В C# **нет ни одного места, которое пишет `broadcastStart`** в IUD-процедуру — только
чтение. Новый прайс-лист всегда получает `DEFAULT '19000101'`.

Демонтаж уже начинали руками:
- [`Massmedia.cs:256`](../Client/Classes/Massmedia.cs) — `broadcastStart` читается, обе
  строки применения закомментированы, переменная мёртвая;
- [`TariffWindowWithRange.sql:59`](../ArtvisDB/dbo/Stored%20Procedures/TariffWindowWithRange.sql) —
  минуты принудительно занулены с комментарием *«проблема из-за минут при редактировании
  веерной акции»*.

---

## 4. Полный список объектов по слоям

Всего: **49 объектов БД** (40 процедур + 9 функций, включая `fn_GetTimeString`),
**2 таблицы**, **11 файлов C#** (из них 2 не компилируются), **1 атрибут метаданных**. Список объектов сверен
06.10.2026 по `sys.sql_modules` на `ArtvisDev`, `Artvis` и `Tumen` — во всех трёх базах
он один и тот же и совпадает с файлами в `ArtvisDB/dbo`. В Crystal-отчётах (`.rpt`) —
**ноль**.

Веб (`FogSoft.Web`) своего кода с `broadcastStart` не имеет, но **использует общий код
десктопа** — файлы подключены ссылками в `FogSoft.Core.csproj`: `MassmediaPricelist.cs` и
`SpecialTariffWindow.cs` с 21.08.2026 (в исследовании 21.09 это было упущено),
`GridExport/ExportDocument.cs` и `DJinExportDocument.cs` с 25.09.2026. Через последние
идёт веб-выгрузка в эфир (`ExportGrid.razor` → `BroadcastGridExport.DJin` →
`ExportDocument.ExportToMemory`). Правка этих файлов меняет поведение обеих версий сразу.

### Слой 0 — кандидаты в мёртвые (удалять целиком, не править)

> **Удалены 06.10.2026** скриптом [`Deploy/20_broadcast-start-dead-objects-drop.sql`](../Deploy/20_broadcast-start-dead-objects-drop.sql)
> (ArtvisDev и прод — 06.10.2026; Tumen — по `Deploy/README.md`). Файлы убраны из
> `ArtvisDB/dbo` и `ArtvisDB.sqlproj`. Таблица ниже оставлена как обоснование.

Проверено 06.10.2026 на `ArtvisDev`: ноль зависимостей в БД
(`sys.sql_expression_dependencies`), имя не встречается в тексте других модулей (это
ловит и динамический SQL), ни одного вызова из компилируемого C#
(`Client`, `FogSoft.Core`, `FogSoft.Web`, `FogSoft.WinForm`), нет ни в `iStoredProcedure`,
ни в одной строковой колонке таблиц метаданных `i*`.

| Объект | Примечание |
|---|---|
| `hlp_GetStartFinishFromIssueDateAndBroadcastStart` | процедура целиком про `broadcastStart` |
| `rpt_Grid_v2` | вытеснена `rpt_Grid_v3` |
| `rpt_Grid` | вытеснена `rpt_Grid_v3`. Единственный вызов — [`GridReportCreater.cs`](../Client/Forms/GridReport/GridReportCreater.cs), файл не входит в `Client.csproj` (живой — `GridReportCreator.cs` → `rpt_Grid_v3`); удалить вместе с ним |
| `fn_GetPrice` | |
| `fn_GetPriceByPeriod` | |
| `fn_GetPriceByPeriod1` | |
| `fn_statGetPrice` | зовётся **динамическим SQL** из `stat_VolumeOfRealization2`, а та сама мертва (нет в `iStoredProcedure` и в C#; работает `stat_VolumeOfRealization3`). Удалять парой |
| `fn_statGetPriceByMonth` | то же с `stat_VolumeOfRealizationByMonth2` (работает `…ByMonth3`) |

Сами `stat_VolumeOfRealization2` / `stat_VolumeOfRealizationByMonth2` `broadcastStart` не
содержат и в счёт 49 не входят, но без них функции не удалить.

`MediaPlanRetrieve` (v1) из этого списка уже удалён — 28.09.2026 в этапе 1 медиаплана
(`230db73`).

### Слой 1 — тождественный сдвиг (приём A), правка механическая

**Спонсорский тракт** (`SponsorProgramPricelist`, `campaignTypeID = 2`):

`ActionRecalculate` · `GetIssuesPrice` · `GetPriceByPeriod` · `SetIssueRatio` ·
`stat_GetPrice_proc` · `stat_GetPriceByMonth_proc` · `stat_Bonuses` · `rpt_GenericBill` ·
`CampaignsForActJournalRetrieve` · `SponsorCampaignPrograms` ·
`SponsorCampaignProgramDelete` · `ProgramIssues` · `ProgramIssuesDays` ·
`SponsorPricelistByDate` · `sponsorPLIUD` · `stat_SponsorBusiness` · `ProgramIssueIUD`

`ProgramIssueIUD` — **write-path спонсорского выпуска** (добавлен в список 06.10.2026):
проверка `ProgramNotExists` подбирает тариф по дню недели с ветвлением приёма B
(`@issueDate < ToShortDate(@issueDate) + broadcastStart` → день недели назад). У
`stat_SponsorBusiness` то же ветвление плюс сдвиг границ периода (приём A). В спонсорском
тракте после шага 1 все значения `00:00`, поэтому вторая ветка здесь недостижима и
правка остаётся механической, несмотря на приём B.

**Линейный/модульный тракт** (`Pricelist`):

| Объект | Что делает |
|---|---|
| `TariffWindowIUD` | пишет `dayActual` / `dayOriginal` — **это write-path, проверять первым** |
| `MediaPlanRetrieve_v2` | `OUTER APPLY` к `Pricelist` + колонка в `GROUP BY` |
| `Pricelists` | `broadcastStartString` + `fn_GetTariffWindowDateRangeStr` |
| `ModulePriceLists` | отдаёт колонку наружу |
| `TariffPassport`, `CampaignDaysTreePassport`, `RollerSubstitutionPassport` | `fn_GetTimeString` |
| `PricelistIUD` | параметр, запись, валидация `UpdatePriceListBroadcastFaild` |

### Слой 2 — ветвление и структура (приём B), правка осмысленная

| Объект | Чем опасен |
|---|---|
| `TariffWindowWithRange` | `@minBroadcast`/`@maxBroadcast` задают **размерность сетки веера** |
| `TariffWindowRetrieve` | параметр `@broadcastStart` + флаг сортировки часов |
| `rpt_Grid_v3` | отдаёт `broadcastStart` **наружу колонкой**, `fn_GetTimeString` в предикате JOIN, `ORDER BY CASE WHEN Time < broadcastStart`, `isToday` для выгрузки DJin. Читают и десктоп, и веб (`BroadcastGrid`, `BroadcastGridExport`) |
| `sl_GenerateTariffWindowsDay` | ветвление дня недели + сдвиг даты при генерации окон |
| `GenerateTariffWindows` | пробрасывает параметр в предыдущую |
| `GenerateTariffWindowByTemplate` | ветвление при генерации по шаблону |
| `TariffIUD` | `fn_GetTariffTimesWithBroadcast` в 14 подзапросах — логика цепочек `TariffUnion` |
| `fn_FindTariffIDForChain` | то же, поиск продолжения цепочки |
| `TariffWindowMoveTime`, `TariffWindowChangePrice`, `TariffWindowChangeDuration` | флаг `@needaddday` |
| `stat_ModuleLoading`, `stat_PackModuleLoading` | ветвление по дням недели + **дефект**, см. §5 |
| `fn_GetTariffTimesWithBroadcast`, `fn_GetTimeString`, `fn_GetTariffWindowDateRangeStr` | сами функции |

### Слой 3 — C#

| Файл | Что делает |
|---|---|
Строки сверены 06.10.2026. «Веб» — файл подключён в `FogSoft.Core` и работает в обеих версиях.

| Файл | Веб | Что делает |
|---|---|---|
| [`MassmediaPricelist.cs`](../Client/Classes/MassmediaPricelist.cs) | да | `ParamNames.BroadcastStart` (:27), свойство `BroadcastStart` (:73), передача в `TariffWindowRetrieve` из `GetTariffWindows` (:136), сдвиг границ в `CheckLinkedWindows` (:178–179) |
| [`MassmediaPricelist.WinForms.cs:259`](../Client/Classes/MassmediaPricelist.WinForms.cs) | — | `new SpecialTariffWindow(BroadcastStart)` |
| [`SpecialTariffWindow.cs:34`](../Client/Classes/SpecialTariffWindow.cs) | да | `if (time < broadcastStart) WindowDate = WindowDate.AddDays(1)` |
| [`TariffWindowGrid.cs:439,472`](../Client/Controls/TariffWindowGrid.cs) | — | раскладка часов (`hour + 24`) и колонок по дням недели |
| [`TariffWithRangeGrid.cs`](../Client/Controls/TariffWithRangeGrid.cs) | — | `MinBroadCast`/`MaxBroadCast` (:172–203): размер массива окон, `GetTimeString` (:792), гард в `PopulateGridTable` |
| [`ExportDocument.cs:48`](../Client/Classes/GridExport/ExportDocument.cs) | да | `ExportToMemory`: `broadcastTime = BroadcastStart.ToString("HHmm")` → **имя файла DJin**, см. §5 |
| [`ExportDocument.WinForms.cs:22`](../Client/Classes/GridExport/ExportDocument.WinForms.cs) | — | то же для десктопной выгрузки в папку (вынесено из `ExportDocument.cs` при переносе в веб) |
| [`DJinExportDocument.cs:21,29`](../Client/Classes/GridExport/DJinSerializer/DJinExportDocument.cs) | да | собирает имена файлов, см. [DJin-01] |
| [`VectorBoxExportDocument.cs`](../Client/Classes/GridExport/VectorBoxSerializer/VectorBoxExportDocument.cs), [`VideoDJExportDocument.cs`](../Client/Classes/GridExport/VideoDJSerializer/VideoDJExportDocument.cs) | — | параметр `broadcastTime` принимают, но не используют; **оба файла не входят в `Client.csproj`** (мёртвые, см. `docs/roller-types.md` Н-12) |
| [`Massmedia.cs:256`](../Client/Classes/Massmedia.cs) | — | мёртвый код (применение закомментировано) |

Новый код уже опирается на календарные сутки: [`TariffWindowWeek.cs:32`](../Client/Classes/TariffWindowWeek.cs)
(04.10.2026, генерация и трафик) прямо пишет в комментарии, что `broadcastStart` у всех
00:00, и сдвиг не учитывает.

---

## 5. Попутные находки

### [DJin-01] Имя файла выгрузки содержит `broadcastTime` — внешний интерфейс

[`DJinExportDocument.cs`](../Client/Classes/GridExport/DJinSerializer/DJinExportDocument.cs)
формирует имена файлов для эфирной системы:

```csharp
string fileFirst  = string.Format("{0}_{1}_{2}-2359.txt", fileName, date, broadcastTime);
string fileSecond = string.Format("{0}_{1}_0000-{2}.txt", fileName, date.AddDays(1), broadcastTime);
```

При `broadcastStart = 00:00` первый файл всегда называется `..._0000-2359.txt`, а второй
**не создаётся никогда**: он требует строк с `isToday = 0`, а `isToday` вычисляется в
`rpt_Grid_v3` как `tariffTime < 24`, и час ≥ 24 появляется только через `fn_GetTimeString`
при ненулевом `broadcastStart`.

> **Это самое ответственное место при удалении.** Формат имени `0000-2359` уходит наружу,
> в DJin. Его нужно сохранить буквально (захардкодить), а не «упростить». Ветку второго
> файла можно удалять — она недостижима.

С 25.09.2026 тот же код формирует имена и в веб-выгрузке (`ExportDocument.ExportToMemory`),
так что одна правка закрывает обе версии. Веб-вариант дополнительно при отсутствии
прайс-листа на дату подставляет пустую строку (`..._-2359.txt`); на практике недостижимо —
без прайс-листа нет окон и выпусков, файл не пишется, — но захардкоженный `0000` убирает
и это.

### [SQL-BS-01] Тариф ровно в полночь выпадает из отчётов загрузки

`stat_ModuleLoading` и `stat_PackModuleLoading` используют **строгие** неравенства:

```sql
(t.[time] < pl.broadcastStart and (...дни недели со сдвигом...))
or (t.[time] > pl.broadcastStart and (...дни недели без сдвига...))
```

При `broadcastStart = 00:00` тариф со временем ровно `00:00` не удовлетворяет ни одному
условию и в отчёт не попадает. В `ArtvisDev` таких тарифов **10 из 7099, из них 7 —
модульные** (`tariffID` 39071, 39132, 39191, 140538, 142605, 142663, 142719).

Дефект существует сегодня, независимо от удаления поля; удаление `broadcastStart` его
попутно закрывает (условие схлопывается в «всегда истина»).

### [SQL-BS-02] `sponsorPLIUD` падает при редактировании из UI — ПОДТВЕРЖДЕНО

Процедура объявляет `@broadcastStart smalldatetime = null` и выполняет
`UPDATE SponsorProgramPricelist SET ... broadcastStart = @broadcastStart`, тогда как
колонка `NOT NULL`. У сущности 12 атрибута `broadcastStart` нет — значит клиент параметр
не передаёт.

**Подтверждено 06.10.2026 замером шага 3 на ArtvisDev:** `UpdateItem` с теми же значениями
падает у **всех 40 из 40** спонсорских прайс-листов: «Не удалось вставить значение NULL в
столбец "broadcastStart"». То есть редактировать спонсорский прайс-лист из интерфейса
сейчас нельзя вообще. Создание (`AddItem`, `INSERT … VALUES (…, @broadcastStart)`) по коду
должно падать так же — замером не проверялось.
Шаг 3 всё равно трогает `sponsorPLIUD` — перестать писать колонку там же (колонка
получит `DEFAULT`); в замере это будет ожидаемое различие `ERR` → успех.

### [SQL-BS-03] `TariffWindowIUD AddItem` не находит прайс-лист в его последний день

`AddItem` берёт прайс-лист условием `@windowDateActual between pl.startDate and pl.finishDate`,
а `finishDate` хранится с временем `00:00`. Окно в последний день прайс-листа в любое время,
кроме ровно `00:00`, прайс-лист не находит → `InternalError`. Подтверждено замером на 5
радиостанциях (31.12.2026 10:00 → `InternalError`, 31.12.2026 00:00 → успех). Проверка выше в
той же процедуре (`< finishDate + 1`) окно пропускает, так что пользователь видит «внутреннюю
ошибку». Кто зовёт `AddItem` — особое окно (`MassmediaPricelist.CreateSpecialTariffWindow`).
К `broadcastStart` не относится, при шаге 3 **не исправлять** (сравнение должно остаться
тождественным) — отдельной правкой.

---

## 6. Оценка

### Выгода по быстродействию — практически нулевая

Замер `rpt_Grid_v3` (самый нагруженный день, `massmediaID = 238`) — **78 мс**, из них на
логику `broadcastStart` приходятся единицы мс. Причины:

- все тяжёлые фильтры с `broadcastStart` сидят в **спонсорских ветках**, а это 2857
  выпусков и 590 кампаний — мизер на фоне 2 млн окон;
- линейный тракт читает уже посчитанные `dayActual` / `dayOriginal`, а не считает на лету;
- объёмы мелкие: 96 окон на день/СМИ.

Скромный выигрыш есть в трёх местах — скалярные UDF в предикатах
(`fn_GetTariffTimesWithBroadcast` в 14 подзапросах `TariffIUD`, `fn_GetTimeString` в JOIN
`rpt_Grid_v3`) и лишний `OUTER APPLY` в `MediaPlanRetrieve_v2`. Это единицы-десятки
миллисекунд.

**Настоящая выгода — упрощение**: минус ~50 объектов с трёхэтажными `DATEADD`, закрытие
класса багов «сетка уехала на день» / «пустая сетка веера», и одним понятием меньше при
переносе в веб (своего кода там нет, но через общие файлы `FogSoft.Core` поле уже
просочилось — см. §4).

### Риски

| Риск | Оценка |
|---|---|
| Другие инсталляции с ненулевым `Pricelist.broadcastStart` | **главный.** 137 063 окна раньше 06:00 уехали бы на сутки. `Tumen` проверена 06.10.2026 — чисто; открытой осталась только `Belgorod` |
| Слой 1 | минимальный — арифметическое тождество |
| Слой 2–3 | средний, но ломает **экран**, а не данные: видно сразу, откатывается пересборкой |
| Формат имени файла DJin | средний — внешний интерфейс, см. [DJin-01] |
| Порча данных | низкий — при `00:00` `dayActual`/`dayOriginal` пишутся одинаково |

---

## 7. План удаления

### Шаг 0 — проверка (обязательно)

Выполнить на **каждой** инсталляции, включая `Belgorod` и `Tumen`:

```sql
SELECT 'PL' src, CONVERT(varchar(8), broadcastStart, 108) bs, COUNT(*) c
FROM Pricelist GROUP BY CONVERT(varchar(8), broadcastStart, 108)
UNION ALL
SELECT 'SPL', CONVERT(varchar(8), broadcastStart, 108), COUNT(*)
FROM SponsorProgramPricelist GROUP BY CONVERT(varchar(8), broadcastStart, 108);
```

Любое ненулевое значение в `Pricelist` — **стоп**, поле живое, дальше не идти.

> **Статус:** прод `Artvis` — пройден 22.09.2026; `Tumen` — пройден 06.10.2026 на
> локальной копии (результаты в §2); `Belgorod` — **не проверена** (OFFLINE). Пока она не
> проверена, изменения шагов 3–5 в Белгород не выкладывать.

### Шаг 1 — главный тест ценой одной строки

Обнулить спонсорские `03:00`:

- применение: [`Scripts/broadcast-start-neutralize-deploy.sql`](../ArtvisDB/Scripts/broadcast-start-neutralize-deploy.sql)
- откат: [`Scripts/broadcast-start-neutralize-rollback.sql`](../ArtvisDB/Scripts/broadcast-start-neutralize-rollback.sql)

Деплой-скрипт перед обнулением сохраняет **полный снимок** `broadcastStart` в таблицу
`dbo.bak_SponsorProgramPricelist_broadcastStart`, а откат восстанавливает значения
из неё. Никаких захардкоженных списков `pricelistID` — берутся фактические значения той
инсталляции, где выполнялся деплой.

Свойства, проверенные на `ArtvisDev` прогоном полного цикла:

| Проверка | Результат |
|---|---|
| deploy создаёт снимок и обнуляет | снимок 40 строк, обнулено 39 |
| **повторный** deploy не затирает снимок | «снимок НЕ перезаписан», обнулено 0, в бэкапе по-прежнему 39 × `03:00` |
| rollback восстанавливает | 39 строк, состояние совпало с исходным построчно (0 расхождений) |
| **повторный** rollback | 0 строк, идемпотентен |
| deploy без снимка | транзакция откатывается, данные не трогаются |
| прайс-листы, созданные после деплоя | в снимке отсутствуют, откат их не трогает — остаются с `00:00` |

Таблицу-бэкап скрипт отката намеренно **не удаляет** (откат можно повторить). Удалять
вручную, когда решение станет окончательным. После этого **весь код становится доказуемо
тождественным**. Дать поработать пару недель.

Смысл шага: он эмпирически проверяет всю теорию из §2, не тронув ни строчки кода. Если
что-то всплывёт — откатили и закрыли тему. Если нет — дальнейшие шаги становятся чистой
механикой.

> **Статус: применено на `ArtvisDev` 21.09.2026** (39 строк) и на **проде 22.09.2026**
> (39 строк). На проде подтверждено запросом: `SponsorProgramPricelist` — 40 из 40 =
> `00:00`, `dbo.bak_SponsorProgramPricelist_broadcastStart` — 40 строк. На проде две
> недели без замечаний, **закрытие сентября по спонсорским кампаниям прошло без проблем**
> (Миша, 06.10.2026) — условие перехода к шагам 2–3 выполнено.
>
> На `Tumen` шаг 1 **не применялся** (6 × `03:00`). Спонсорских выпусков там нет вообще,
> поэтому на результат это не влияет; применить можно для единообразия.
> **Применён 10.10.2026** (Миша) перед шагом 3: `Deploy/29` на Tumen без него отказался работать, как задумано.

**Результат контрольного замера.** До и после применения снят снимок из 15 324 строк:
привязка выпуска к прайс-листу (JOIN по `broadcastStart`), приведение к эфирному дню,
ветвление по дням недели, `ProgramIssuesDays` + `SponsorCampaignPrograms` по всем
спонсорским кампаниям с выпусками, `GetIssuesPrice` / `GetPriceByPeriod` по каждой,
`stat_SponsorBusiness` за три года, `rpt_Grid_v3` на 12 датах со спонсорскими выпусками,
`Campaign.startDate`/`finishDate`.

Различий по существу — **ноль**. Единственное изменение: 12 строк `rpt_Grid_v3`, где
поменялось **само значение** колонки `broadcastStart` (`03:00` → `00:00`) при полностью
идентичных остальных полях. Эта колонка в клиенте не читается: `GridReportCreator`
отдаёт датасет в Crystal-отчёт `Grid`, а поиск по всем 10 файлам `.rpt` (ASCII и UTF-16)
её не находит. Выгрузка в DJin берёт `broadcastStart` из `Pricelist` (линейный прайс-лист),
а не из спонсорского, поэтому имена файлов не затронуты.

### Шаг 2 — удалить мёртвые объекты

Слой 0. Независимо от остального, чистая уборка. Состав на 06.10.2026: 8 объектов
слоя 0 + `stat_VolumeOfRealization2` / `stat_VolumeOfRealizationByMonth2` (единственные
вызывающие `fn_statGetPrice*`) + файл `Client/Forms/GridReport/GridReportCreater.cs`
(не компилируется, единственный вызов `rpt_Grid`).

> **Статус: сделано 06.10.2026** — [`Deploy/20_broadcast-start-dead-objects-drop.sql`](../Deploy/20_broadcast-start-dead-objects-drop.sql).
> Скрипт сам повторяет проверку ссылок, вырезая комментарии (`/* */`, `--`): имена
> встречаются только в комментариях `stat_GetPrice_proc`, `stat_GetPriceByMonth_proc`,
> `stat_VolumeOfRealization3`, `…ByMonth3`, `rpt_Grid_v3`. Проверен отрицательным тестом
> (процедура с настоящим вызовом `rpt_Grid_v2` — остановка, комментарии — пропуск) и на
> ArtvisDev: удалено 10, повторный запуск — без изменений; `rpt_Grid_v3`,
> `stat_VolumeOfRealization3`, `stat_VolumeOfRealizationByMonth3` после удаления
> выполняются, неразрешённых ссылок на удалённые имена в базе нет.

### Шаг 3 — схлопнуть слой 1

Механическая замена сдвигов на `dbo.ToShortDate(...)`, а в спонсорском тракте — снятие
недостижимой второй ветки дня недели. Колонку **оставить**. Начинать с write-path:
`TariffWindowIUD` (сверять `dayActual`/`dayOriginal` до и после) и `ProgramIssueIUD`.
Остальное сверять тем же контрольным замером, что и шаг 1.

> **Статус: сделано 10.10.2026** — [`Deploy/29_broadcast-start-layer1-collapse.sql`](../Deploy/29_broadcast-start-layer1-collapse.sql),
> 19 процедур: спонсорский тракт (`ActionRecalculate`, `GetIssuesPrice`, `GetPriceByPeriod`, `SetIssueRatio`,
> `stat_GetPrice_proc`, `stat_GetPriceByMonth_proc`, `stat_Bonuses`, `rpt_GenericBill`, `CampaignsForActJournalRetrieve`,
> `SponsorCampaignPrograms`, `SponsorCampaignProgramDelete`, `ProgramIssues`, `ProgramIssuesDays`, `stat_SponsorBusiness`,
> `ProgramIssueIUD`), `TariffWindowIUD` и паспорта `TariffPassport`, `CampaignDaysTreePassport`,
> `RollerSubstitutionPassport` (`fn_GetTimeString(broadcastStart, x)` → `CONVERT(varchar(5), x, 108)`).
> Замер на ArtvisDev: «до» снят заново 10.10.2026 (10 мин), «после» — 38 457 секций, 252 438 строк, **0 различий**.
> Скрипт отказывается работать, если где-то начало эфирного дня не 00:00 (у спонсорских прайс-листов — всё значение
> `'19000101'`, они участвовали в арифметике и датой), — на Tumen сначала шаг 1. **Tumen:** шаг 1 и `Deploy/29` накатаны
> 10.10.2026, «ГОТОВО».
>
> **Не вошло в шаг 3** (не тождественно или не сдвиг):
> - `stat_SponsorBusiness`, режим «свободные и занятые»: прайс-лист присоединён `LEFT JOIN` по дате выпуска, и сдвиг
>   заодно отсекал строки без прайс-листа — последний день спонсорского прайс-листа после 00:00 (`finishDate` хранится
>   с 00:00). Отсечение сохранено явным условием `pl.pricelistID is not null`; похоже на дефект — отдельно.
> - `MediaPlanRetrieve_v2`: начало дня берётся `OUTER APPLY` к прайс-листу; если прайс-листа на день окна нет,
>   сдвиг даёт `NULL` и выпуск выпадает из «Графика размещения». Тождественно не упростить — вместе с чисткой мёртвой
>   второй ветки.
> - Выдача колонки наружу и запись (`Pricelists`, `ModulePriceLists`, `SponsorPricelistByDate`, `PricelistIUD`,
>   `sponsorPLIUD`) — до шага 5. [SQL-BS-02] (`sponsorPLIUD` падает) — отдельной правкой со своим ожидаемым различием.

**Правило правки — только буквальное удаление прибавки `00:00`**: `DATEADD(mi, ±DATEPART(mi, x.broadcastStart), DATEADD(hh, ±DATEPART(hh, x.broadcastStart), E))` → `E`,
`E - spp.broadcastStart` → `E`, `ToShortDate(@d) + sppl.broadcastStart` → `ToShortDate(@d)`,
недостижимая вторая ветка дня недели — убрать. Сравнения (`BETWEEN … AND @finishDate + 1`,
`<`, `>=`) **не переписывать** на «приведение к дате»: границы у процедур включительные и
разные (у `GetIssuesPrice` конец — полночь следующего дня включительно, у `SetIssueRatio` —
полночь `@finishDate`), и любая «унификация» поменяет результат на выпусках ровно в 00:00.

**Замер (собран 06.10.2026):** [`ArtvisDB/Scripts/broadcast-start-snapshot.sql`](../ArtvisDB/Scripts/broadcast-start-snapshot.sql)
+ сравнение [`broadcast-start-snapshot-compare.ps1`](../ArtvisDB/Scripts/broadcast-start-snapshot-compare.ps1).
Вызывает все процедуры слоя 1 на реальных данных ArtvisDev (590 спонсорских кампаний по
дням, 350 акций, отчёты за периоды, окна, прайс-листы), пишущие — в точках сохранения с
откатом; второй проход переносит часть выпусков ровно на 00:00 (в данных таких нет), чтобы
проверить включительные границы. Около 13–14 минут; **всё это время qd2 на ArtvisDev не
открывается** (блокировки до конца транзакции) — запускать по согласованию. Два прогона
«до» 06.10.2026 совпали полностью (38 457 секций, 253 тыс. строк) — замер детерминирован;
сравнение ловит изменение одной цифры (проверено).

Попутно замер показал: `CampaignsForActJournalRetrieve` за квартал по одному агентству идёт
до 6 мин (в среднем 112 с) — тот же тормоз, что в прод-логе в конце месяца; в замере журнал
урезан до однодневных периодов и двух месяцев по 4 агентствам со спонсорскими кампаниями.

#### Порядок с переходом на фактическое окно (решено 08.10.2026)

Переход на фактическое окно выпуска (`docs/tasks/window-actual-switch.md` §5.5) правит часть тех же процедур:
- шаг 3: деньги (`ActionRecalculate`, `GetIssuesPrice`, `SetIssueRatio`, `GetPriceByPeriod`, `rpt_GenericBill`,
  `CampaignsForActJournalRetrieve`, `stat_GetPrice_proc`, `stat_GetPriceByMonth_proc`, `stat_Bonuses`),
  `TariffWindowIUD`, `RollerSubstitutionPassport`;
- шаг 4: `TariffWindowRetrieve`, `TariffWindowMoveTime`, `GenerateTariffWindowByTemplate`, `TariffIUD`,
  `SpecialTariffWindow.cs`.

Правило: одна правка процедуры за раз. Тождественная правка шага 3 и смысловая правка перехода идут разными
деплоями, у каждой свой замер. Порядок:
1. Шаг 3 — раньше этапов перехода «Деньги», Р-2 (`TariffWindowIUD`) и замены ролика. Перед ним заново снять «до»:
   снимок от 06.10 устарел. Уже сделанные `Deploy/24` и `Deploy/25` (10 процедур) процедур замера не трогают.
2. Шаг 4 — после этапа 7 перехода (окна) или не делать.

### Шаг 4 — слой 2 и 3

По одному объекту, с проверкой экрана. Здесь же чинится хрупкость веерной сетки
(`PopulateGridTable` без `MaxBroadCast`) и закрывается [SQL-BS-01]. Формат имени файла
DJin — захардкодить, см. [DJin-01].

### Шаг 5 — удалить колонку

`DROP COLUMN` в обеих таблицах, убрать атрибут метаданных `broadcastStartString` (сущность 80)
и параметры процедур.

> Шаги 0–1 отвечают на вопрос «рискованно или нет» ценой одного `UPDATE`.
> Шаги 2–3 дают бо́льшую часть выигрыша в читаемости при минимальном риске.
> Шаги 4–5 добавляют немного, а риска несут больше всех — **остановиться после шага 3
> совершенно нормально**.
