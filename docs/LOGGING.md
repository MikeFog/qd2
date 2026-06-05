# Логирование в qd2

## Стек

- **log4net** — единственный логер в проекте
- Конфигурация: `Client\app.config`, секция `<log4net>`
- Лог-файл: `logs/qd2.log` (RollingFileAppender, 5 МБ × 5 файлов)
- Формат строки: `2026-06-05 14:32:01.123 INFO  [1] [user] [cid] - сообщение`

## Уровни (root по умолчанию)

| Уровень | Когда пишется | Примеры |
|---------|--------------|---------|
| `INFO`  | Всегда (root = Info) | Итог генерации по шаблону, медленные SP |
| `DEBUG` | Только если логгер явно выставлен в Debug | Время C#-операций (OperationScope), DAL-детали |
| `ERROR` | Всегда | Необработанные исключения через ErrorManager |

## Два слоя измерения времени

### Слой 1 — SQL (готов, всегда активен)

Класс: `FogSoft.WinForm\DataAccess\DbExecutionScope.cs`  
Логгер: `FogSoft.WinForm.DataAccess.DbExecutionScope`  
Уровень: `INFO`  
Порог: `StoredProcExecutionTimeThresholdMS` из `app.config` (сейчас `1` мс — пишет всё)

Покрытие: каждый вызов `DataAccessor.LoadDataSet` / `ExecuteNonQuery` / `DoAction`.  
Пример записи:
```
INFO  - AddRangeIssues 87ms timeout=30 rows=3
INFO  - ActionRecalculate 12ms timeout=30
```

Чтобы видеть только медленные SP, поставить порог:
```xml
<add key="StoredProcExecutionTimeThresholdMS" value="1000" />
```

---

### Слой 2 — C#-операции (добавлен 2026-06-05)

Класс: `FogSoft.WinForm\Classes\OperationScope.cs`  
Логгер: `FogSoft.WinForm.Classes.OperationScope`  
Уровень: `DEBUG` — **по умолчанию не пишется**

Покрытие — точки входа в бизнес-операции:

| Метод | Что измеряет |
|-------|-------------|
| `RollerIssuesGrid3.AddIssue` | Линейный клик: BeginTx → IssueIUD → Recalculate → CommitTx → RefreshSingleCell |
| `TariffWithRangeGrid.AddIssuesRange` | Веерный клик: AddRangeIssues → (опц.) Recalculate |
| `TariffWithRangeGrid.AddIssuesRangeTimePeriod` | Веерный TimePeriod-слот: фильтр слотов + N × AddIssuesRange |

Пример записи:
```
DEBUG - AddIssue date=2026-06-10 09:00 143ms
DEBUG - AddIssuesRange date=2026-06-10 09:00 recalc=True 95ms
DEBUG - AddIssuesRangeTimePeriod date=2026-06-10 q=0/2p/1np 312ms
```

---

### Слой 2b — итоговое время генерации по шаблону

Класс: `FrmGenerator`  
Логгер: `Merlin.Forms.FrmGenerator`  
Уровень: `INFO` — **пишется всегда**

```
INFO  - Generate completed in 4821ms success=42 fail=0
```

Записывается в `finally` после полного прохода шаблона, до `RecalculateAction`.

---

## Как включить OperationScope.Debug

В `app.config`, внутри секции `<log4net>`:

```xml
<!-- Детальное время C#-операций (включать при профилировании) -->
<logger name="FogSoft.WinForm.Classes.OperationScope">
  <level value="DEBUG" />
</logger>
```

После этого в лог будут попадать все `OperationScope` записи без какого-либо порога.

## Как добавить логирование в новый метод

Паттерн полностью аналогичен `DbExecutionScope`:

```csharp
// 1. Поле класса (один раз)
private static readonly ILog Log = LogManager.GetLogger(typeof(МойКласс));

// 2. Оборачиваем метод
using (OperationScope.Start("ИмяОперации param=значение"))
{
    // ... тело метода
    return result;  // return внутри using работает корректно
}
```

Для одиночных `Log.Info` без измерения времени — просто:
```csharp
Log.Info($"Сообщение {переменная}");
```

## Существующие логгеры в проекте

| Логгер | Файл | Уровень |
|--------|------|---------|
| `FogSoft.WinForm.DataAccess.DbExecutionScope` | DbExecutionScope.cs | INFO (с порогом) |
| `FogSoft.WinForm.Classes.ConfigurationUtil` | ConfigurationUtil.cs | ERROR |
| `Merlin.Forms.FrmGenerator` | FrmGenerator.cs | INFO |
| `FogSoft.WinForm.Classes.OperationScope` | OperationScope.cs | DEBUG (выключен) |
| `Merlin.Forms.GridReport.*` | GridReportCreater.cs | INFO |
| `DAL` | (конфиг app.config) | DEBUG — особый логгер |

## Файлы

```
Client\
  app.config                 — log4net конфигурация
FogSoft.WinForm\
  DataAccess\
    DbExecutionScope.cs      — SQL-layer timing scope
  Classes\
    ConfigurationUtil.cs     — StoredProcExecutionTimeThresholdMS
    OperationScope.cs        — C#-layer timing scope
logs\
  qd2.log                    — текущий лог (в папке рядом с exe)
  qd2.log.1 … qd2.log.5     — ротированные копии
```
