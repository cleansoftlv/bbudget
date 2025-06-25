using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;
using Services.Licensing.Revolut;

var builder = FunctionsApplication.CreateBuilder(args);


ConfigureAppConfiguration(builder.Configuration);

builder
    .ConfigureFunctionsWebApplication();


builder.Services.AddScoped<RevolutService>();
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();
builder.Services.AddServices(builder.Configuration);

builder.Build().Run();

static void ConfigureAppConfiguration(IConfigurationBuilder builder)
{
    builder
        .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("servicesSettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();
}