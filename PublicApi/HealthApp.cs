using FunctionCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services.Health;
using Services.Helpers;
using Shared;

namespace PublicApi
{
    public class HealthApp
    {

        public HealthApp(
            HealthService healthService,
            ILoggerFactory loggerFactory
            )
        {
            _healthService = healthService;
            _loggerFactory = loggerFactory;
        }

        private readonly HealthService _healthService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Throttler _throttler;

        [Function(nameof(HowAreYou))]
        public async Task<IActionResult> HowAreYou(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/howareyou")]
            HttpRequest req)
        {
            try
            {
                var ok = await _healthService.CheckDbConnection();
                if (!ok)
                {
                    return new ObjectResultWithStatus(StatusCodes.Status503ServiceUnavailable, new ApiError
                    {
                        error = "Database connection failed",
                    });
                }
                return new OkObjectResult(new OkOnly());
            }
            catch (Exception ex)
            {
                _loggerFactory.CreateLogger<HealthApp>().LogError(ex, "Health check failed");
                return new ObjectResultWithStatus(StatusCodes.Status503ServiceUnavailable, new ApiError
                {
                    error = "Internal server error",
                });
            }
        }

        //[Function(nameof(Stats))]
        //public async Task<IActionResult> Stats(
        //      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/stats")]
        //    HttpRequest req)
        //{
        //    var userCnt = await _healthService.GetAppUserCount();
        //    return new OkObjectResult(new
        //    {
        //        uc = userCnt
        //    });
        //}



    }
}
