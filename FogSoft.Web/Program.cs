using System.Reflection;
using FogSoft.Web.Components;
using FogSoft.Web.Infrastructure;
using FogSoft.WinForm.Classes;
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
