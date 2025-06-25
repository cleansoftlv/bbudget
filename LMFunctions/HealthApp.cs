using FunctionCommon;
using LMFunctions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services.Health;
using Services.Helpers;
using Shared;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LMFunctions
{
    public class HealthApp
    {

        public HealthApp(
            HealthService healthService,
            ILoggerFactory loggerFactory,
            Throttler throttler
            )
        {
            _healthService = healthService;
            _loggerFactory = loggerFactory;
            _throttler = throttler;
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


        //[Function(nameof(LoggingTest))]
        //public IActionResult LoggingTest(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/logging/warn")]
        //    HttpRequest req)
        //{
        //   _healthService.CheckLogging();
        //    return new OkObjectResult(new OkOnly());
        //}

        //[Function(nameof(ErrorTest))]
        //public IActionResult ErrorTest(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/logging/error")]
        //    HttpRequest req)
        //{
        //    throw new Exception("This is a test exception for logging purposes");
        //}

        //[Function(nameof(IpTest))]
        //public IActionResult IpTest(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/iptest")]
        //    HttpRequest req,
        //    FunctionContext fcontext)
        //{
        //    if (!_throttler.TryRegisterRequest("test", 5))
        //    {
        //        return new ObjectResultWithStatus(StatusCodes.Status429TooManyRequests, new ApiError
        //        {
        //            error = "Too many requests",
        //        });
        //    }


        //    var funIp = fcontext.GetClientIp(req);

        //    return new OkObjectResult(new
        //    {
        //        xforwarded = req.Headers["X-Forwarded-For"].ToString(),
        //        ip = req.HttpContext.Connection.RemoteIpAddress?.ToString(),
        //        sxForwardedFor = funIp
        //    });
        //}



    }
}
