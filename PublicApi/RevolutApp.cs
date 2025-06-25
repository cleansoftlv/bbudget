using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Health;
using Services.Licensing.Revolut;
using Services.Options;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PublicApi
{
    public class RevolutApp
    {
        private readonly ILogger<RevolutApp> _logger;
        private readonly RevolutService _revolutService;

        public RevolutApp(
            ILogger<RevolutApp> logger,
            RevolutService revolutService)
        {
            _logger = logger;
            _revolutService = revolutService;
        }


        [Function("Revolut_OrderStatus")]
        public async Task<IActionResult> OrderStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "revolut/updatestatus")]
            HttpRequest req)
        {
            string? signatureHeader = req.Headers["Revolut-Signature"];
            string? timestampHeader = req.Headers["Revolut-Request-Timestamp"];
            var env = req.Query["env"];

            if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(timestampHeader))
            {
                return new BadRequestObjectResult("Missing required headers");
            }
            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult("Request body is empty");
            }
            var (ok, error) = _revolutService.VerifyWebhookSignature(signatureHeader, timestampHeader, requestBody, DateTimeOffset.UtcNow, env);

            if (!ok)
            {
                _logger.LogError($"Signature verification failed: {error}");
                return new UnauthorizedResult();
            }
            await _revolutService.EnqueueWebhookEvent(requestBody, env);
            return new StatusCodeResult(204);
        }
    }
}
