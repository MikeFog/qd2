> Исследование проведено 21.09.2026. Ничего в коде и данных не менялось.

# `broadcastStart` — справочник и оценка удаления

`broadcastStart` («начало эфирного дня») — время, с которого начинаются эфирные сутки.
Всё, что звучит раньше него, относится к **предыдущему** эфирному дню: тариф на 01:00
понедельника — это ночь с понедельника на вторник, а не ночь с воскресенья на
понедельник. От такой модели суток в своё время отказались, но поле и вся обслуживающая
его арифметика остались и разошлись по системе.

**Краткий вывод:** поле мертво в данных на обеих доступных инсталляциях, выигрыша в
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

Проверено на `ArtvisDev` и `Artvis` (вторая локальная база). `Belgorod` и `Tumen` на
сервере есть, но **OFFLINE — не проверялись**.

| Проверка | ArtvisDev | Artvis |
|---|---|---|
| `Pricelist.broadcastStart` | **138 из 138 = 00:00** | **136 из 136 = 00:00** |
| `SponsorProgramPricelist.broadcastStart` | 39 = 03:00, 1 = 00:00 | 39 = 03:00, 1 = 00:00 |
| `SponsorTariff` с `time < 03:00` | **0** | — |
| `ProgramIssue` в зоне 00:00–03:00 | **0** из 2857 | — |
| `TariffWindow`: `dayActual` ≠ дате `windowDateActual` | **0 из 2 010 628** | — |
| `TariffWindow`: `dayOriginal` ≠ дате `windowDateOriginal` | **0 из 2 010 628** | — |
| `Campaign` с ненулевым временем в `startDate`/`finishDate` | **0 из 38 577** | — |

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
`CONVERT(varchar(5), pl.broadcastStart, 114)`. У спонсорского прайс-листа (сущность 12)
атрибута нет вообще.

В C# **нет ни одного места, которое пишет `broadcastStart`** в IUD-процедуру — только
чтение. Новый прайс-лист всегда получает `DEFAULT '19000101'`.

Демонтаж уже начинали руками:
- [`Massmedia.cs:256`](../Client/Classes/Massmedia.cs) — `broadcastStart` читается, обе
  строки применения закомментированы, переменная мёртвая;
- [`TariffWindowWithRange.sql:56`](../ArtvisDB/dbo/Stored%20Procedures/TariffWindowWithRange.sql) —
  минуты принудительно занулены с комментарием *«проблема из-за минут при редактировании
  веерной акции»*.

---

## 4. Полный список объектов по слоям

Всего: **49 объектов БД** (41 процедура + 8 функций) + `fn_GetTimeString`, **2 таблицы**,
**7 файлов C#**, **1 атрибут метаданных**. В веб-версии (`FogSoft.Web`) — **ноль**
упоминаний. В Crystal-отчётах (`.rpt`) — **ноль**.

### Слой 0 — кандидаты в мёртвые (удалять целиком, не править)

Ноль зависимостей в БД (`sys.sql_expression_dependencies`), ноль вызовов по имени из C#,
отсутствуют в `iStoredProcedure`.

| Объект | Примечание |
|---|---|
| `hlp_GetStartFinishFromIssueDateAndBroadcastStart` | процедура целиком про `broadcastStart` |
| `rpt_Grid_v2` | вытеснена `rpt_Grid_v3` |
| `MediaPlanRetrieve` (v1) | вытеснена `MediaPlanRetrieve_v2` |
| `fn_GetPriceByPeriod1` | |
| `fn_GetPrice` | вытеснена `stat_GetPrice_proc`? |
| `fn_GetPriceByPeriod` | |
| `fn_statGetPrice` | вытеснена `stat_GetPrice_proc`? |
| `fn_statGetPriceByMonth` | вытеснена `stat_GetPriceByMonth_proc`? |

> Перед удалением подтвердить, что их не зовут из динамического SQL — зависимости
> SQL Server его не видят.

### Слой 1 — тождественный сдвиг (приём A), правка механическая

**Спонсорский тракт** (`SponsorProgramPricelist`, `campaignTypeID = 2`):

`ActionRecalculate` · `GetIssuesPrice` · `GetPriceByPeriod` · `SetIssueRatio` ·
`stat_GetPrice_proc` · `stat_GetPriceByMonth_proc` · `stat_Bonuses` · `rpt_GenericBill` ·
`CampaignsForActJournalRetrieve` · `SponsorCampaignPrograms` ·
`SponsorCampaignProgramDelete` · `ProgramIssues` · `ProgramIssuesDays` ·
`SponsorPricelistByDate` · `sponsorPLIUD`

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
| `rpt_Grid_v3`, `rpt_Grid` | отдают `broadcastStart` **наружу колонкой**, `fn_GetTimeString` в предикате JOIN, `ORDER BY CASE WHEN Time < broadcastStart` |
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
| [`MassmediaPricelist.cs`](../Client/Classes/MassmediaPricelist.cs) | `ParamNames.BroadcastStart`, свойство `BroadcastStart`, передача в `GetTariffWindows`, сдвиг границ в `CheckLinkedWindows` |
| [`MassmediaPricelist.WinForms.cs:167`](../Client/Classes/MassmediaPricelist.WinForms.cs) | `new SpecialTariffWindow(BroadcastStart)` |
| [`SpecialTariffWindow.cs:34`](../Client/Classes/SpecialTariffWindow.cs) | `if (time < broadcastStart) WindowDate = WindowDate.AddDays(1)` |
| [`TariffWindowGrid.cs:393,426`](../Client/Controls/TariffWindowGrid.cs) | раскладка часов (`hour + 24`) и колонок по дням недели |
| [`TariffWithRangeGrid.cs`](../Client/Controls/TariffWithRangeGrid.cs) | `MinBroadCast`/`MaxBroadCast`: размер массива окон, `GetTimeString`, гард в `PopulateGridTable` |
| [`ExportDocument.cs:29,57`](../Client/Classes/GridExport/ExportDocument.cs) | `broadcastTime = BroadcastStart.ToString("HHmm")` → **выгрузка в DJin**, см. §5 |
| [`Massmedia.cs:256`](../Client/Classes/Massmedia.cs) | мёртвый код (применение закомментировано) |

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

### [SQL-BS-02] `sponsorPLIUD` должен падать при редактировании из UI

Процедура объявляет `@broadcastStart smalldatetime = null` и выполняет
`UPDATE SponsorProgramPricelist SET ... broadcastStart = @broadcastStart`, тогда как
колонка `NOT NULL`. У сущности 12 атрибута `broadcastStart` нет — значит клиент параметр
не передаёт. Требует отдельной проверки, к удалению поля отношения не имеет.

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
переносе в веб (куда его и не переносили).

### Риски

| Риск | Оценка |
|---|---|
| Другие инсталляции (`Belgorod`, `Tumen`) с ненулевым `Pricelist.broadcastStart` | **главный.** 137 063 окна раньше 06:00 уехали бы на сутки |
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
> `00:00`, `dbo.bak_SponsorProgramPricelist_broadcastStart` — 40 строк.

**Результат контрольного замера.** До и после применения снят снимок из 15 324 строк:
привязка выпуска к прайс-листу (JOIN по `broadcastStart`), приведение к эфирному дню,
ветвление по дням недели, `ProgramIssuesDays` + `SponsorCampaignPrograms` по всем
спонсорским кампаниям с выпусками, `GetIssuesPrice` / `GetPriceByPeriod` по каждой,
`stat_SponsorBusiness` за три года, `rpt_Grid_v3` на 12 датах со спонсорскими выпусками,
`Campaign.startDate`/`finishDate`.

Различий по существу — **ноль**. Единственное изменение: 12 строк `rpt_Grid_v3`, где
поменялось **само значение** колонки `broadcastStart` (`03:00` → `00:00`) при полностью
идентичных остальных полях. Эта колонка в клиенте не читается: `GridReportCreater`
отдаёт датасет в Crystal-отчёт `Grid`, а поиск по всем 10 файлам `.rpt` (ASCII и UTF-16)
её не находит. Выгрузка в DJin берёт `broadcastStart` из `Pricelist` (линейный прайс-лист),
а не из спонсорского, поэтому имена файлов не затронуты.

### Шаг 2 — удалить мёртвые объекты

Слой 0. Независимо от остального, чистая уборка.

### Шаг 3 — схлопнуть слой 1

Механическая замена сдвигов на `dbo.ToShortDate(...)`. Колонку **оставить**. Начинать с
`TariffWindowIUD` (write-path) и сверять `dayActual`/`dayOriginal` до и после.

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
