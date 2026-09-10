using System.Reflection;
using FogSoft.Web.Components;
using FogSoft.Web.Infrastructure;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Контекст пользователя ---------------------------------------------
// Сеанс живёт столько же, сколько circuit (вкладка браузера). Подробности,
// почему именно так, — в комментарии к CircuitServicesAccessor.
builder.Services.AddScoped<UserSession>();
// Scoped — то есть свой на circuit: диалог одного пользователя не должен
// быть виден другому.
builder.Services.AddScoped<DialogService>();
builder.Services.AddSingleton<CircuitServicesAccessor>();
builder.Services.AddScoped<CircuitHandler, CircuitServicesHandler>();
// Кэш метаданных сущностей — тоже на circuit, и это вопрос не скорости, а прав:
// в метаданные вшито dbo.IsActionEnabled(@userID, …), см. WebEntityCache.
builder.Services.AddScoped<CircuitEntityCacheState>();
// Меню пользователя и вытекающий из него доступ к экранам — тоже на circuit.
builder.Services.AddScoped<MenuAccess>();

var app = builder.Build();

// --- Инициализация ядра -------------------------------------------------
// log4net: на .NET Core секция app.config не читается, конфигурация грузится
// явно из файла. Формат строки и путь к логу те же, что у десктопа.
log4net.Config.XmlConfigurator.Configure(
    log4net.LogManager.GetRepository(Assembly.GetEntryAssembly()),
    new FileInfo(Path.Combine(AppContext.BaseDirectory, "log4net.config")));

// Метаданные называют сборку доменных классов "Merlin" (это Merlin.exe,
// десктоп). В вебе те же классы лежат в FogSoft.Core — заявляем соответствие,
// иначе Entity.CreateObject не поднимет класс сущности. Регистрировать нужно
// до первого обращения к метаданным.
DomainAssemblyResolver.Register();

// Подстановка хранилища пользователя — тот самый шов из этапа 0.2.
// С этого момента весь код ядра, спрашивающий SecurityManager.LoggedUser,
// получает пользователя текущего circuit, а не общего на всех.
SecurityManager.SetLoggedUserStorage(
    new WebLoggedUserStorage(app.Services.GetRequiredService<CircuitServicesAccessor>()));

// Тот же шов, но для кэша сущностей. Без него проверка прав ниже читала бы
// права первого вошедшего в процесс пользователя: EntityInfoRetrieve снимает
// метаданные под конкретный @userID, а кэш в ядре — статический.
EntityManager.SetEntityCache(
    new WebEntityCache(app.Services.GetRequiredService<CircuitServicesAccessor>()));

// Права на действия начинают проверяться на исполнении, а не только гасить
// кнопки, как в десктопе: в вебе адрес вызывается напрямую, минуя меню.
// docs/tasks/web-migration.md, раздел 7 п.1.
DataAccessor.SetActionAuthorization(new WebActionAuthorization());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
