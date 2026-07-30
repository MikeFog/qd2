# Задача: обучающий проект — MetadataAdmin (просмотр/редактирование iEntity-метаданных)

## Статус

Планирование. Задача 1 расписана полностью и готова к самостоятельной реализации.
Задачи 2+ будут сформулированы после ревью задачи 1 — сознательно не планируются
заранее в деталях.

## 1. Задача 1 — скелет приложения + 2 экрана на чтение

### 1.1 Definition of done

- Новое решение `MetadataAdmin.sln` собирается в Visual Studio без ошибок.
- Проект ни на что в остальном репозитории не ссылается (см. 1.3) и работает на
  копии базы, не на проде.
- Запуск показывает `MainForm` с меню из двух пунктов: **Сущности** и
  **Атрибуты сущностей**.
- **Сущности** открывает окно со списком всех записей `iEntity` (грид, только
  чтение).
- **Атрибуты сущностей** открывает окно со списком всех записей
  `iEntityAttribute` (грид, только чтение).
- Доступ к БД — только через собственный класс `DataAccessor` (написать с нуля,
  статический класс поверх `SqlConnection`/`SqlCommand` — заодно повод
  разобраться, что такое статические классы). Это не класс из `FogSoft.WinForm`
  — проект на него не ссылается вообще. `SqlConnection`/`SqlCommand`
  используются только внутри этого одного класса, не в формах.
- SQL — обычные `SELECT`, без хранимых процедур (см. 1.5).
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

Никаких Project References на другие проекты решения `qd2.sln` — ни на
`FogSoft.WinForm`, ни на `Microsoft.ApplicationBlocks.Data`. Проект должен
собираться и работать сам по себе, без единой ссылки на остальной репозиторий.
Существующий код (`FogSoft.WinForm/DataAccess/DataAccessor.cs`,
`FogSoft.WinForm/Classes/ConfigurationUtil.cs`, старый прототип
`MetadataEditor/MetadataEditor/MainForm.cs`) можно открыть и почитать для идей,
но не подключать как зависимость — все классы переписываются самостоятельно.

Единственная дополнительная ссылка:
- `System.Configuration` (Add Reference → Assemblies) — стандартная сборка
  .NET Framework для чтения `app.config` (`ConfigurationManager`,
  `ConnectionStringSettings`). Это часть платформы, а не связь с другим
  проектом решения.

log4net не подключаем — он был нужен только внутри `FogSoft.WinForm`. Для ошибок
в задаче 1 достаточно `try/catch` + `MessageBox.Show(ex.Message)` (как, кстати,
сделано и в старом прототипе).

### 1.4 app.config

Заменить содержимое `App.config` на:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
  <connectionStrings>
    <add name="Main" connectionString="user id=AdvertAgUser; password=AdvertAg; server=.\sqlexpress; database=ЗАМЕНИТЬ_НА_ИМЯ_КОПИИ_БАЗЫ" />
  </connectionStrings>
</configuration>
```

Имя строки подключения (`Main`) — просто пример, не обязательное требование:
здесь его читает не общий `ConfigurationUtil` (как в исходном варианте плана), а
свой класс конфигурации, который предстоит написать, так что имя ключа выбирает
сам.

Написать свой класс чтения конфигурации — например, статический метод, который
через `ConfigurationManager.ConnectionStrings["Main"]` возвращает готовую строку
подключения. Для вдохновения можно посмотреть (не копировать и не подключать)
`FogSoft.WinForm/Classes/ConfigurationUtil.cs` — там тот же принцип на
хранилище connection string.

### 1.5 SQL

Не использовать хранимые процедуры — написать свой SQL прямо в C#-коде (обычный
текст запроса, выполняемый через `SqlCommand`). Никаких изменений в проекте
`ArtvisDB` для задачи 1 не требуется.

Задача 1 показывает все строки без фильтра, поэтому запросы — самые простые, без
`WHERE` и без параметров:

```sql
SELECT
	entityID, name, tableName, className, pkColumn, codeName, parentId, isObsolete
FROM
	iEntity
ORDER BY
	entityID
```

```sql
SELECT
	entityID, alias, name, ordinal_position, selector, dataType
FROM
	iEntityAttribute
ORDER BY
	entityID, ordinal_position, selector
```

Столбцы `passport`/`filter` из `iEntity` сюда сознательно не включены — это
большие XML-блобы, для грида не подходят (их редактирование — то, что уже делает
старый прототип, вне рамок задачи 1).

На будущее (когда в задаче 2 понадобится фильтр по `entityID`): значение
подставлять только через `SqlParameter`, никогда через конкатенацию строк —
именно конкатенацией собран SQL в старом прототипе
(`MetadataEditor/MetadataEditor/MainForm.cs`, методы `GetPassport()`/
`SavePassport()`) — это пример, как делать не надо.

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

    var entityItem = new ToolStripMenuItem("Сущности");
    entityItem.Click += (s, e) => new EntityListForm().Show();

    var attributeItem = new ToolStripMenuItem("Атрибуты сущностей");
    attributeItem.Click += (s, e) => new EntityAttributeListForm().Show();

    menu.Items.Add(entityItem);
    menu.Items.Add(attributeItem);

    MainMenuStrip = menu;
    Controls.Add(menu);
}
```

`Show()`, а не `ShowDialog()` — оба окна не должны блокировать друг друга (в
задаче 2, если появится связь «сущность → её атрибуты», это пригодится).

### 1.7 EntityListForm и EntityAttributeListForm

Требования к каждой из двух форм (структура одинаковая, это специально —
закрепляет один и тот же паттерн дважды):

- `DataGridView`, `Dock = Fill`, `ReadOnly = true`.
- Конструктор вызывает загрузку данных.
- Загрузка — через свой класс `DataAccessor` (SQL из 1.5; внутри —
  `SqlDataAdapter`/`DataTable` или ручной перебор `SqlDataReader`, на выбор).
- Результат смаппить в список `EntityInfo`/`EntityAttributeInfo` (завести самому,
  по набору колонок из 1.5) и присвоить в `grid.DataSource` — не биндить
  `DataTable` напрямую.

Как именно мапить `DataRow`/`SqlDataReader` → `EntityInfo` (конструктор с
параметрами, статический фабричный метод, ручное присваивание после `new` — на
выбор), какую сигнатуру дать методам `DataAccessor` и как разбить методы внутри
формы — сознательно не расписано, это и есть часть задачи.

Скелет для старта (второй файл зеркально, с `EntityAttributeInfo` и
«Атрибуты сущностей»):

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
        Text = "Сущности";
        Width = 800;
        Height = 500;
        Controls.Add(grid);
        LoadData();
    }

    private void LoadData()
    {
        // TODO: свой DataAccessor (SQL из 1.5) -> List<EntityInfo> -> grid.DataSource
    }
}
```

### 1.8 Проверка перед ревью

- [ ] `MetadataAdmin.sln` собирается без ошибок и предупреждений о недостающих
      сборках.
- [ ] Проект не ссылается ни на один другой проект решения `qd2.sln` (нет
      `FogSoft.WinForm`, нет `Microsoft.ApplicationBlocks.Data` в ссылках).
- [ ] `app.config` указывает на копию базы, не на продовую строку подключения
      (проверить глазами).
- [ ] Оба пункта меню открывают окна с данными без исключений.
- [ ] `SqlConnection`/`SqlCommand` используются только внутри своего класса
      `DataAccessor`, не в формах напрямую.
- [ ] Нет хранимых процедур — SQL прямо в коде, изменений в `ArtvisDB` нет.
- [ ] Есть классы `EntityInfo`/`EntityAttributeInfo`, грид не биндится на `DataRow`.
- [ ] Нет никакого кода на запись (`ExecuteNonQuery`, `INSERT`/`UPDATE`/`DELETE`
      в SQL).
