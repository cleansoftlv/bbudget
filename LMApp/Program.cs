using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LMApp;
using LMApp.Models.Account;
using Microsoft.AspNetCore.Components;
using LMApp.Models.Categories;
using LMApp.Models.Transactions;
using LMApp.Models.UI;
using BlazorApplicationInsights;
using LMApp.Models.Configuration;
using BlazorApplicationInsights.Models;
using LMApp.Models.Context;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using LMApp.Models;
using System.Reflection;
using System;
using LMApp.Models.Extensions;
using Shared.Login;
using LMApp.Models.Licenses;
using LMApp.Models.Reports;
using Shared.License;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<LocalStorageService>();

builder.Services.AddSingleton((sp) =>
{
    return new CascadingValueSource<UserContext>((UserContext)null, isFixed: false);
});
builder.Services.AddSingleton((sp) =>
{
    return new CascadingValueSource<AuthUserInfo>((AuthUserInfo)null, isFixed: false);
});

builder.Services.AddCascadingValue(sp => sp.GetRequiredService<CascadingValueSource<UserContext>>());
builder.Services.AddCascadingValue(sp => sp.GetRequiredService<CascadingValueSource<AuthUserInfo>>());

var assembly = System.Reflection.Assembly.GetExecutingAssembly();
AssemblyName thisAssemName = assembly.GetName();
Version ver = thisAssemName.Version;
string appVersion = ver.ToString();

builder.Services.AddCascadingValue("AppVersion", x => appVersion);
builder.Services.AddSingleton<UserContextService>();
builder.Services.AddSingleton<FormatService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<TransactionsService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<LicenseService>();
builder.Services.AddScoped<ReportsService>();
builder.Services.AddSingleton<LicenseCheckService>();
builder.Services.AddScoped<Utils>();
builder.Services.AddPWAUpdater();
builder.Services.AddViewTransition();


builder.Services.AddTransient<AuthorizationMessageHandler>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

//add named http client to call lunchmoney api

builder.Services.AddHttpClient("LM", client =>
{
    client.BaseAddress = new Uri("https://dev.lunchmoney.app/v1/");
    client.DefaultRequestHeaders.Add(ClientConstants.XClientAppHeaderKey, String.Concat(ClientConstants.XClientAppHeaderValue, '/', appVersion));
}).ConfigurePrimaryHttpMessageHandler(
    serviceProvider => new AuthorizationMessageHandler(serviceProvider.GetRequiredService<UserContextService>())
    {
        InnerHandler = new HttpClientHandler()
    });

builder.Services.AddHttpClient("LMAuth", client =>
{
    client.BaseAddress = new Uri("https://dev.lunchmoney.app/v1/");
    client.DefaultRequestHeaders.Add(ClientConstants.XClientAppHeaderKey, String.Concat(ClientConstants.XClientAppHeaderValue, '/', appVersion));
});


builder.Services.AddScoped<CookieHandler>();
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(string.Concat(builder.HostEnvironment.BaseAddress.TrimEnd('/'), "/api/"));
    client.DefaultRequestHeaders.Add(ClientConstants.XClientAppHeaderKey, String.Concat(ClientConstants.XClientAppHeaderValue, '/', appVersion));
}).AddHttpMessageHandler<CookieHandler>();


builder.Services.AddBootstrapBlazor();

if (!String.IsNullOrEmpty(builder.Configuration[nameof(LocalOptions.ApplicationInsightsConnectionString)]))
{
    builder.Services.AddBlazorApplicationInsights(config =>
    {
        config.ConnectionString = builder.Configuration[nameof(LocalOptions.ApplicationInsightsConnectionString)];
    },
        async applicationInsights =>
    {
        var telemetryItem = new TelemetryItem()
        {
            Tags = new Dictionary<string, object>()
                {
                { "appVersion", appVersion },
                { "ai.cloud.role", "LMApp" },
                { "ai.cloud.roleInstance", builder.Configuration[nameof(LocalOptions.Enviroment)] ?? "Dev" },
                }
        };
        await applicationInsights.AddTelemetryInitializer(telemetryItem);
    }, addWasmLogger: true, o =>
    {
        o.MinLogLevel = LogLevel.Warning;
    });

}
#if DEBUG
builder.Logging.SetMinimumLevel(LogLevel.Information);
#else
builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

builder.Services.Configure<LocalOptions>(builder.Configuration);

await builder.Build().RunAsync();
