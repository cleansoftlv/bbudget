using LMFunctions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Services.Auth;
using Services.Licensing;
using Services.Licensing.Revolut.Dto;

namespace LMFunctions
{
    public class LicenseApp(
        LicenseService licenseService,
        AuthService authService)
    {
        private readonly LicenseService _licenseService = licenseService;
        private readonly AuthService _authService = authService;

        [Function("License_GetLicensingInfo")]
        public async Task<IActionResult> GetLicensingInfo(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "license")]
            HttpRequest req,
          FunctionContext fcontext)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }
            var resp = await _licenseService.GetUserLicensingInfo(userId.Value);
            return new OkObjectResult(resp);
        }

        [Function("License_GetProductPrices")]
        public async Task<IActionResult> GetProductPrices(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "license/products")]
            HttpRequest req)
        {
            var resp = await _licenseService.GetActiveProducts();
            return new OkObjectResult(resp);
        }

        [Function("License_StartPayment")]
        public async Task<IActionResult> StartPayment(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "license/startpayment/{priceId:int}")]
            HttpRequest req,
            FunctionContext fcontext,
            int priceId)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }
            var resp = await _licenseService.CreateOrder(userId.Value, priceId);
            return new OkObjectResult(resp);
        }

        [Function("License_GetOrderStatus")]
        public async Task<IActionResult> GetOrderStatus(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "license/order/{id:int}")]
            HttpRequest req,
            FunctionContext fcontext,
            int id)
        {
            var userId = fcontext.GetUserId();
            if (userId == null)
            {
                return new UnauthorizedResult();
            }
            var resp = await _licenseService.GetOrderPaymentStatus(id, userId.Value);
            if (resp == null)
            {
                return new NotFoundResult();
            }
            resp.NewSignature = await _authService.LoadAuthInfoSignature(userId.Value);
            return new OkObjectResult(resp);
        }

        [Function("License_UpdateOrderStatus")]
        public async Task UpdateOrderStatus(
             [QueueTrigger("%Services:Revolut:WebhookQueueName%",
                Connection = "Services:Revolut:WebhookQueueStorageConnectionString")] OrderWebhookEvent orderEvent)
        {
            await _licenseService.ProcessOrderStatusUpdate(orderEvent);
        }
    }
}
