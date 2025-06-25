using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Health
{
    public class HealthService(IDbContextFactory<CommonContext> dbFactory, ILoggerFactory logFactory)
    {
        private readonly IDbContextFactory<CommonContext> _dbFactory = dbFactory;
        private readonly ILoggerFactory _loggerFactory = logFactory;


        public async Task<bool> CheckDbConnection()
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Database.CanConnectAsync();
        }

        public void CheckLogging()
        {
            _loggerFactory.CreateLogger<HealthService>().LogWarning("Logging is working");
        }

        public async Task<int> GetAppUserCount()
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.AppUsers.CountAsync();
        }
    }
}
