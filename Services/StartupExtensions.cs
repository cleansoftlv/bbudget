using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Services.Auth;
using Services.DAL;
using Services.Health;
using Services.Helpers;
using Services.Licensing;
using Services.Licensing.Revolut;
using Services.Options;
using Shared.License;
using System.IdentityModel.Tokens.Jwt;

namespace Services
{
    public static class StartupExtensions
    {

        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.Configure<ServicesOptions>(configuration.GetSection("Services"));

            services.AddScoped<AuthService>();
            services.AddScoped<HealthService>();
            services.AddScoped<LicenseCheckService>();
            services.AddScoped<LicenseService>();


            services.AddSingleton<LocalCache>();
            services.AddSingleton<Throttler>();


            RevolutService.AddHttpClient(services, configuration);
            services.AddScoped<RevolutService>();

            services.AddHttpClient("LMAuth", client =>
            {
                client.BaseAddress = new Uri("https://dev.lunchmoney.app/v1/");
                client.DefaultRequestHeaders.Add("User-Agent", String.Concat("bbudget_api/1.0"));
            });

            services.AddDbContextFactory<CommonContext>((services, options) =>
                   {
                       var servicesOptions = services.GetRequiredService<IOptions<ServicesOptions>>().Value;
                       if (servicesOptions.IsAzureSql)
                       {
                           options.UseAzureSql(servicesOptions.DbConnectionString, sqlOptions =>
                           {
                               if (servicesOptions.SqlCompatabilityLevel != null)
                               {
                                   sqlOptions.UseCompatibilityLevel(
                                       servicesOptions.SqlCompatabilityLevel.Value);
                               }
                           });
                       }
                       else
                       {
                           options.UseSqlServer(servicesOptions.DbConnectionString, sqlOptions =>
                           {
                               if (servicesOptions.SqlCompatabilityLevel != null)
                               {
                                   sqlOptions.UseCompatibilityLevel(
                                     servicesOptions.SqlCompatabilityLevel.Value);
                               }
                           });
                       }
                   }
                   );


            return services;
        }
    }
}
