# Политическая агитация: развёртывание на продуктивной базе

Инструкция по обновлению прода. Реализация описана в
`political-agitation-ads.md`, программа тестирования — в
`political-agitation-testing.md`, находки ревью — в
`political-agitation-review-findings.md`.

Всё, что меняется в базе, делится на две части:

1. **Схема, справочные данные и метаданные форм** — один идемпотентный скрипт
   `ArtvisDB/Scripts/political-agitation-seed.sql`.
2. **Программные объекты** (процедуры, функции, представление) — деплоятся из
   файлов репозитория, список ниже.

Плюс третья часть вне базы: **свежая сборка клиента** (`Merlin.exe`).

---

## 0. Перед началом

- Сделать бэкап продуктивной базы. Скрипт добавляет колонки и правит XML
  паспорта — операции обратимые, но откат руками неприятен.
- Проверить, что деплой идёт **не** через dacpac с опцией «удалять объекты,
  которых нет в проекте». Три новых объекта добавлены в `ArtvisDB.sqlproj`
  (`AgitationFraming`, `fn_AgitationChainFirst`, `fn_AgitationExcludeIntervals`),
  так что dacpac их создаст, но опцию всё равно стоит проверить.
- Развёртывание лучше делать вне эфирного пика: `IssueIUD`, `ActionActivate`,
  `CampaignTransferDay` — процедуры, которыми пользуются постоянно.

---

## 1. Скрипт схемы и данных

```
sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i ArtvisDB\Scripts\political-agitation-seed.sql
```

**Флаг `-I` обязателен** (`QUOTED_IDENTIFIER ON`) — без него `UPDATE` в
`IssueIUD` падает на индексированных представлениях. Это касается и скрипта, и
деплоя процедур.

Что делает скрипт:

| Раздел | Содержание |
|---|---|
| Типы роликов | `iRollerActionType`: 6 (агитация), 7 (анонс), 44 (локальное СМИ), 55 (федеральное СМИ) |
| Сообщения | 5 записей в `iMessage` + 1 в `iMessageToActivate` |
| Схема | 4 колонки в `MassMedia`: три ссылки на `Roller` (с FK) и `agitationExcludeIntervals` |
| Метаданные | 3 алиаса result set в `iTableAlias` для `massmediaPassport` |
| Форма | Страница «Политическая агитация» в XML паспорта станции (`iEntity`, entityID = 9) |

Скрипт **идемпотентен и конвергентен**: повторный прогон безопасен, а из
промежуточных отменённых редакций (были в ходе разработки) он сам приводит базу
к финальному состоянию. Страница паспорта каждый раз вырезается по позиции и
вставляется заново, поэтому дублей не возникает.

В конце скрипт печатает **таблицу проверки** — все строки должны быть в
состоянии `OK`. Строки про программные объекты покажут `НЕ ЗАДЕПЛОЕН`, пока не
выполнен шаг 2, это нормально.

---

## 2. Программные объекты

13 объектов. В репозитории они лежат как `CREATE`, поэтому для существующих
объектов при деплое нужно заменить `CREATE` на `ALTER` (либо использовать
`CREATE OR ALTER`, если версия SQL Server позволяет).

### Новые (создаются впервые, `CREATE` как есть)

| Файл | Что это |
|---|---|
| `dbo/Stored Procedures/AgitationFraming.sql` | ядро: вставка и снятие обвязки |
| `dbo/Functions/fn_AgitationChainFirst.sql` | первое окно цепочки `windowPrevId` |
| `dbo/Functions/fn_AgitationExcludeIntervals.sql` | разбор интервалов-исключений |

### Изменённые (нужен `ALTER`)

| Файл | Что изменилось |
|---|---|
| `dbo/Views/vMassmedia.sql` | +4 колонки агитации |
| `dbo/Stored Procedures/MassmediaIUD.sql` | +4 параметра, запись колонок, валидация интервалов |
| `dbo/Stored Procedures/massmediaPassport.sql` | +3 result set со списками роликов |
| `dbo/Stored Procedures/hlp_IssueVerify.sql` | запреты: позиционирование, смешение, семейства 4/44 и 5/55, анонс, проверка полей станции |
| `dbo/Stored Procedures/IssueIUD.sql` | запрет смешения, хуки вставки и снятия обвязки |
| `dbo/Stored Procedures/ModuleIssueIUD.sql` | запрет смешения |
| `dbo/Stored Procedures/PackModuleIssueID.sql` | запрет смешения |
| `dbo/Stored Procedures/RollerSubstitute.sql` | запрет подмены класса ролика при замене |
| `dbo/Stored Procedures/ActionActivate.sql` | блокирующая проверка полей станции + вставка обвязки |
| `dbo/Stored Procedures/ActionDeactivate.sql` | снятие обвязки при деактивации |
| `dbo/Stored Procedures/ActionIUD.sql` | снятие обвязки при окончательном удалении акции |
| `dbo/Stored Procedures/CampaignIUD.sql` | снятие обвязки при удалении кампании |
| `dbo/Stored Procedures/CampaignsIssueDelete.sql` | снятие обвязки при удалении строки ролика/дня |
| `dbo/Stored Procedures/CampaignTransferDay.sql` | перенос обвязки при переносе дня |
| `dbo/Stored Procedures/IssueTransfer.sql` | перенос обвязки при переносе выпуска |

Порядок внутри шага 2 не важен: `AgitationFraming` вызывается по имени, а
отложенное разрешение имён в SQL Server позволяет создавать процедуры в любой
последовательности. Но **функции лучше задеплоить первыми** — они используются
в `WHERE`, и запрос с несуществующей функцией не скомпилируется.

### Готовая команда (PowerShell)

Разворачивает все объекты по списку, автоматически подставляя `ALTER` вместо
`CREATE` для уже существующих:

```powershell
$server = ".\sqlexpress"; $db = "<база>"; $root = "C:\Work\AdvertAg\sources\ArtvisDB"
$files = @(
  "dbo\Functions\fn_AgitationChainFirst.sql",
  "dbo\Functions\fn_AgitationExcludeIntervals.sql",
  "dbo\Views\vMassmedia.sql",
  "dbo\Stored Procedures\AgitationFraming.sql",
  "dbo\Stored Procedures\MassmediaIUD.sql",
  "dbo\Stored Procedures\massmediaPassport.sql",
  "dbo\Stored Procedures\hlp_IssueVerify.sql",
  "dbo\Stored Procedures\IssueIUD.sql",
  "dbo\Stored Procedures\ModuleIssueIUD.sql",
  "dbo\Stored Procedures\PackModuleIssueID.sql",
  "dbo\Stored Procedures\RollerSubstitute.sql",
  "dbo\Stored Procedures\ActionActivate.sql",
  "dbo\Stored Procedures\ActionDeactivate.sql",
  "dbo\Stored Procedures\ActionIUD.sql",
  "dbo\Stored Procedures\CampaignIUD.sql",
  "dbo\Stored Procedures\CampaignsIssueDelete.sql",
  "dbo\Stored Procedures\CampaignTransferDay.sql",
  "dbo\Stored Procedures\IssueTransfer.sql"
)
foreach ($f in $files) {
    $sql = [System.IO.File]::ReadAllText((Join-Path $root $f))
    # у представления берём только тело, без диаграммных extended properties
    if ($f -like "*Views*") { $sql = ($sql -split "(?m)^GO\s*$")[0] }
    $name = [System.IO.Path]::GetFileNameWithoutExtension($f)
    $exists = (sqlcmd -S $server -d $db -E -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID('dbo.$name') IS NULL THEN 0 ELSE 1 END") -join ""
    if ($exists.Trim() -eq "1") {
        $sql = ([regex]'CREATE(\s+)(PROC(?:EDURE)?|FUNCTION|VIEW)').Replace($sql, 'ALTER$1$2', 1)
    }
    $tmp = Join-Path $env:TEMP "dep_$name.sql"
    [System.IO.File]::WriteAllText($tmp, $sql, (New-Object System.Text.UTF8Encoding($true)))
    sqlcmd -S $server -d $db -E -f 65001 -I -i $tmp
    if ($LASTEXITCODE -eq 0) { Write-Host "OK:   $name" } else { Write-Host "FAIL: $name" -ForegroundColor Red }
}
```

---

## 3. Клиент

Пересобрать и разложить пользователям `Merlin.exe`. В ветке есть правки C#:

- порядок обвязки в выгрузке для DJin (`BlockManager`, `DJinExportDocument`);
- фикс GDI+ при сохранении паспорта с картинкой (`PageFieldImage`);
- поиск подписи по всем страницам паспорта станции (`MassmediaPassport`).

**Клиент кэширует метаданные форм**, поэтому после шага 1 его обязательно нужно
перезапустить — иначе новая вкладка в карточке станции не появится даже при
верно выполненном скрипте.

---

## 4. Проверка после развёртывания

1. Повторно прогнать скрипт шага 1 — таблица проверки должна быть полностью
   `OK`, включая три программных объекта.
2. Открыть карточку любой радиостанции — должна быть вкладка «Политическая
   агитация» с тремя списками и полем интервалов; сохранение проходит.
3. Дымовой тест по `political-agitation-testing.md`, раздел 7 (10 минут).
4. Убедиться, что **обычная** (неполитическая) акция активируется как раньше и
   в её окнах не появилось ничего лишнего.
5. Сформировать выгрузку для DJin на день без агитации и сравнить с прежней —
   файл должен совпадать.

---

## 5. Порядок отката

Если что-то пошло не так, откат такой:

1. Вернуть предыдущую сборку клиента.
2. Вернуть тела процедур из `master` тем же способом (шаг 2 со старыми файлами).
3. Колонки в `MassMedia` и записи справочников можно **не** удалять: без
   изменённых процедур они не используются и ни на что не влияют. Если всё же
   нужно убрать вкладку из карточки станции — вырезать из XML паспорта
   (`iEntity`, entityID = 9) блок `<page caption="Политическая агитация">…</page>`
   и перезапустить клиент.

Полное удаление колонок потребует сначала снять три FK
(`FK_MassMedia_AgitationLocalRoller`, `…AgitationAnnounceRoller`,
`…AgitationFederalRoller`), а также удалить выпуски служебной акции — так что
это крайняя мера, не штатный откат.
