# Задача: обучающий проект — MetadataAdmin (просмотр/редактирование iEntity-метаданных)

## Статус

Планирование. Задача 1 расписана полностью и готова к самостоятельной реализации.
Задачи 2+ будут сформулированы после ревью задачи 1 — сознательно не планируются
заранее в деталях.

## 0. Контекст и правила

- Цель — не тренировочный туториал, а реальная задача: часть функциональности
  редактора метаданных `iEntity`/`iEntityAttribute`, которой в проекте не хватает
  (сейчас есть только редактирование XML `passport`/`filter`, см.
  `MetadataEditor/MetadataEditor/MainForm.cs` — старый прототип, не трогаем и не
  расширяем, оставляем как есть).
- Метаданные `iEntity`/`iTableAlias`/`iStoredProcedure`/`iModuleProcedure` — это то,
  на чём держится резолвинг хранимых процедур (`DataAccessor.DoAction`) и
  генерация паспортов сущностей по всему приложению (см. `docs/ARCHITECTURE.md`,
  раздел «Metadata-driven passport forms»). Ошибка в них может незаметно сломать
  не связанные между собой экраны — отсюда все ограничения ниже.
- Работа только на **копии базы**, не на проде и не на общей dev-базе.
- Отдельная ветка `learning/metadata-editor`, созданная от `master` (не от
  `feature/political-agitation` — там сейчас незакоммиченные продакшн-правки).
- Ревью каждого шага перед мержем куда-либо — обязательно.
- Задача 1 — **только чтение**, никаких `INSERT`/`UPDATE`/`DELETE`. Это сознательно:
  сначала должен появиться работающий скелет и привычка использовать
  `DataAccessor`, а не голый `SqlConnection`.

## 1. Задача 1 — скелет приложения + 2 экрана на чтение

### 1.1 Definition of done

- Новое решение `MetadataAdmin.sln` собирается в Visual Studio без ошибок.
- Запуск показывает `MainForm` с меню из двух пунктов: **Entity** и
  **EntityAttribute**.
- **Entity** открывает окно со списком всех записей `iEntity` (грид, только чтение).
- **EntityAttribute** открывает окно со списком всех записей `iEntityAttribute`
  (грид, только чтение).
- Доступ к БД — только через `DataAccessor`, нигде нет прямого `SqlConnection`/
  `SqlCommand`.
- Для каждого экрана есть свой класс, представляющий строку данных (`EntityInfo`,
  `EntityAttributeInfo`) — грид не биндится напрямую на `DataRow`/`DataTable`.
- Подключение — к копии базы (в `app.config`), не к продовой строке подключения.

### 1.2 Создать проект

Через мастер Visual Studio (не писать `.csproj` руками):
`File → New → Project → Windows Forms App (.NET Framework)`.
- Framework: **.NET Framework 4.8** (как у `Client`).
- Имя: `MetadataAdmin`.
- Расположение: корень репозитория, рядом с `Client`, `FogSoft.WinForm`, `ArtvisDB`
  (то есть путь получится `MetadataAdmin\MetadataAdmin.csproj`).

Переименовать `Form1` → `MainForm` через Solution Explorer (F2, согласиться на
переименование всех ссылок) — `Program.cs` подхватит новое имя автоматически.

### 1.3 Добавить ссылки (References)

Project References (Add Reference → Projects → Browse):
- `..\FogSoft.WinForm\FogSoft.WinForm.csproj` — здесь лежит `DataAccessor`.
- `..\Microsoft.ApplicationBlocks.Data\Microsoft.ApplicationBlocks.Data.csproj` —
  от него зависит `DataAccessor` внутри.

Assembly References (Add Reference → Browse):
- `..\Lib\log4net.dll` — `FogSoft.WinForm` использует log4net напрямую,
  и `Client` тоже подключает его явно (не транзитивно) — повторяем тот же паттерн.
- `System.Configuration` (Add Reference → Assemblies) — нужен для чтения
  `app.config` (`ConnectionStringSettings` и т.п.).

### 1.4 app.config

Заменить содержимое `App.config` на:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
  </configSections>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
  <connectionStrings>
    <add name="Main" connectionString="user id=AdvertAgUser; password=AdvertAg; server=.\sqlexpress; database=ЗАМЕНИТЬ_НА_ИМЯ_КОПИИ_БАЗЫ" />
  </connectionStrings>
  <log4net>
    <appender name="FileAppender" type="log4net.Appender.RollingFileAppender">
      <file value="logs/metadata-admin.log" />
      <appendToFile value="true" />
      <rollingStyle value="Size" />
      <maximumFileSize value="5MB" />
      <maxSizeRollBackups value="5" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date{yyyy-MM-dd HH:mm:ss.fff} %-5p [%thread] - %m%n" />
      </layout>
    </appender>
    <root>
      <level value="Info" />
      <appender-ref ref="FileAppender" />
    </root>
  </log4net>
</configuration>
```

Имя строки подключения обязательно `Main` — именно его читает
`ConfigurationUtil.ConnectionStringMain` (см.
`FogSoft.WinForm/Classes/ConfigurationUtil.cs:43-46`), который использует
`DataAccessor`.

### 1.5 Хранимые процедуры (в `ArtvisDB/dbo/Stored Procedures/`)

В проекте уже есть `EntityInfoRetrieve.sql`, которая возвращает и `iEntity`, и
`iEntityAttribute`, но заодно ещё `iEntityAction` с проверкой прав
(`IsActionEnabled(@userID, ...)`) и колонки `INFORMATION_SCHEMA` — то есть
обязательный параметр `@userID`, не нужный для простого просмотра, и лишние
result set'ы. Для задачи 1 проще и чище завести две небольшие процедуры по
образцу того же файла (стиль `COALESCE(@entityID, entityID)` — оттуда же):

`EntityRetrieve.sql`:
```sql
CREATE PROCEDURE [dbo].[EntityRetrieve]
(
	@entityID int = NULL
)
AS
SET NOCOUNT ON

SELECT
	entityID, name, tableName, className, pkColumn, codeName, parentId, isObsolete
FROM
	iEntity
WHERE
	entityID = COALESCE(@entityID, entityID)
ORDER BY
	entityID
```

`EntityAttributeRetrieve.sql`:
```sql
CREATE PROCEDURE [dbo].[EntityAttributeRetrieve]
(
	@entityID int = NULL
)
AS
SET NOCOUNT ON

SELECT
	entityID, alias, name, ordinal_position, selector, dataType
FROM
	iEntityAttribute
WHERE
	entityID = COALESCE(@entityID, entityID)
ORDER BY
	entityID, ordinal_position, selector
```

Столбцы `passport`/`filter` из `iEntity` сюда сознательно не включены — это
большие XML-блобы, для грида не подходят (их редактирование — то, что уже делает
старый прототип, вне рамок задачи 1).

Обе процедуры создать на копии базы (через SSMS или `sqlcmd`), файлы добавить в
`ArtvisDB`-проект.

### 1.6 MainForm — меню

Меню проще собрать кодом, а не дизайнером — 2 пункта, и код нагляднее в ревью,
чем сгенерированный дизайнером XML-подобный C#:

```csharp
public MainForm()
{
    InitializeComponent();
    InitializeMenu();
}

private void InitializeMenu()
{
    var menu = new MenuStrip();

    var entityItem = new ToolStripMenuItem("Entity");
    entityItem.Click += (s, e) => new EntityListForm().Show();

    var attributeItem = new ToolStripMenuItem("EntityAttribute");
    attributeItem.Click += (s, e) => new EntityAttributeListForm().Show();

    menu.Items.Add(entityItem);
    menu.Items.Add(attributeItem);

    MainMenuStrip = menu;
    Controls.Add(menu);
}
```

`Show()`, а не `ShowDialog()` — оба окна не должны блокировать друг друга (в
задаче 2, если появится связь Entity → его атрибуты, это пригодится).

### 1.7 EntityListForm и EntityAttributeListForm

Требования к каждой из двух форм (структура одинаковая, это специально —
закрепляет один и тот же паттерн дважды):

- `DataGridView`, `Dock = Fill`, `ReadOnly = true`.
- Конструктор вызывает загрузку данных.
- Загрузка: `DataAccessor.LoadDataSet("EntityRetrieve", DataAccessor.CreateParametersDictionary())`
  (аналогично для `EntityAttributeRetrieve`) — параметр `entityID` не передавать,
  сработает `= NULL` в процедуре и вернутся все строки.
- Результат смаппить в список `EntityInfo`/`EntityAttributeInfo` (завести самому,
  по набору колонок из 1.5) и присвоить в `grid.DataSource` — не биндить
  `DataTable` напрямую.

Как именно мапить `DataRow → EntityInfo` (конструктор с параметрами, статический
фабричный метод, ручное присваивание после `new` — на выбор) и как разбить
методы внутри формы — сознательно не расписано, это и есть часть задачи.

Скелет для старта (второй файл зеркально):

```csharp
public class EntityListForm : Form
{
    private readonly DataGridView grid = new DataGridView
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true
    };

    public EntityListForm()
    {
        Text = "Entity";
        Width = 800;
        Height = 500;
        Controls.Add(grid);
        LoadData();
    }

    private void LoadData()
    {
        // TODO: DataAccessor.LoadDataSet("EntityRetrieve", ...) -> List<EntityInfo> -> grid.DataSource
    }
}
```

### 1.8 Проверка перед ревью

- [ ] `MetadataAdmin.sln` собирается без ошибок и предупреждений о недостающих
      сборках.
- [ ] `app.config` указывает на копию базы (проверить строку подключения глазами).
- [ ] Оба пункта меню открывают окна с данными без исключений.
- [ ] В коде нет `SqlConnection`/`SqlCommand` — только `DataAccessor`.
- [ ] Есть классы `EntityInfo`/`EntityAttributeInfo`, грид не биндится на `DataRow`.
- [ ] Нет никакого кода на запись (`ExecuteNonQuery`, `INSERT`/`UPDATE`/`DELETE`
      в SQL).
- [ ] `logs/metadata-admin.log` создаётся и пишется при запуске (проверка, что
      log4net-конфиг подхватился).

## 2. Что дальше (не для задачи 1, только ориентир)

Порядок по возрастанию риска — предлагается делать в этом порядке, но
конкретику каждой задачи фиксировать отдельно, после ревью предыдущей:

1. Запись (CRUD) по `iEntityAttribute` — дочерняя таблица, PK/уникальность по
   (`entityID`, `ordinal_position`, `selector`) сами страхуют от части ошибок,
   поломка задевает только один entity.
2. Связка экранов: выбор entity в списке фильтрует его атрибуты (master-detail).
3. Редактирование полей самого `iEntity` (`tableName`, `className`, `pkColumn`,
   `parentId` и т.п.) — то, что старый прототип не покрывал (он работал только с
   `passport`/`filter`).
4. `iTableAlias`/`iStoredProcedure`/`iModuleProcedure` — таблицы резолвинга под
   `DataAccessor.DoAction`, в `docs/ARCHITECTURE.md` прямо отмечены как источник
   риска («hidden proc coupling»). Возможно, стоит оставить read-only даже в
   финальной версии инструмента — решить по факту, когда дойдём.

Напоминание для тестирования любого из следующих шагов: клиентское приложение
кэширует `iEntity`/`iTableAlias` — эффект правки в основном `Client` виден только
после его перезапуска.
