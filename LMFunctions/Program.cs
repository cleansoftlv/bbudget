using LMFunctions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;

var host = new HostBuilder()
    .ConfigureAppConfiguration(ConfigureAppConfiguration)
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<AuthMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddServices(context.Configuration);
    })
    .Build();

host.Run();

static void ConfigureAppConfiguration(IConfigurationBuilder builder)
{
    builder
        .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("servicesSettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();
}