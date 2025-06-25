using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Services.Helpers;
using Services.Licensing.Revolut.Dto;
using Services.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Services.Licensing.Revolut
{
    public class RevolutService(IHttpClientFactory httpFactory, IOptions<ServicesOptions> options)
    {
        private readonly IHttpClientFactory _httpFactory = httpFactory;
        private readonly ServicesOptions _options = options.Value;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        const string HttpClientName = "Revolut";

        public async Task<RevOrder> CreateOrder(CreateOrderRequest req)
        {
            using var response = await CreateClient().PostAsJsonAsync("orders", req, _jsonOptions);
            await response.EnsureSuccessStatusCodeIncludeBody();
            return await response.Content.ReadFromJsonAsync<RevOrder>();
        }

        private HttpClient CreateClient()
        {
            return _httpFactory.CreateClient(HttpClientName);
        }

        public static void AddHttpClient(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient(HttpClientName, client =>
            {
                client.BaseAddress = new Uri(configuration.GetValue<string>("Services:Revolut:BaseUrl"));
                client.DefaultRequestHeaders.Add("Revolut-Api-Version", configuration.GetValue<string>("Services:Revolut:ApiVersion"));
                client.DefaultRequestHeaders.Add("User-Agent", "bbudget_api/1.0");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration.GetValue<string>("Services:Revolut:SecretKey"));
            });
        }

        private const string SignCheckVersion = "v1";

        public async Task EnqueueWebhookEvent(string payload, string enviroment)
        {
            var queue = CreateQueueClient(enviroment);
            try
            {
                await queue.SendMessageAsync(payload);
            }
            //Handle queue does not exists and retry
            catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "QueueNotFound")
            {
                await queue.CreateIfNotExistsAsync();
                await queue.SendMessageAsync(payload);
            }
        }

        public async Task<RevOrder> GetOrder(string orderId)
        {
            return await CreateClient().GetFromJsonAsync<RevOrder>($"orders/{HttpUtility.UrlEncode(orderId)}");
        }

        private QueueClient CreateQueueClient(string enviroment)
        {
            var queueName = enviroment == "sandbox"
                ? _options.Revolut.WebhookQueueNameTest
                : _options.Revolut.WebhookQueueName;

            return new Azure.Storage.Queues.QueueClient(
                _options.Revolut.WebhookQueueStorageConnectionString,
                queueName,
                new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });
        }

        public (bool ok, string error) VerifyWebhookSignature(
            string signatureHeader,
            string timestampHeader,
            string requestBody,
            DateTimeOffset utcNow,
            string enviroment)
        {
            if (!long.TryParse(timestampHeader, out long timestamp))
            {
                return (false, "timestamp is not int64");
            }

            var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            var timeDiff = utcNow - date;
            if (Math.Abs(timeDiff.TotalMinutes) > 10)
            {
                return (false, "timestamp is too new or too old (+10 min diff)");
            }

            string payloadToSign = $"{SignCheckVersion}.{timestampHeader}.{requestBody}";

            var secret = enviroment == "sandbox"
                ? _options.Revolut.WebhookSecretTest
                : _options.Revolut.WebhookSecret;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadToSign));
                string computedSignature = $"{SignCheckVersion}={BitConverter.ToString(hash).Replace("-", "").ToLower()}";

                // Revolut may send multiple signatures separated by commas
                var providedSignatures = signatureHeader.Split(',').Select(s => s.Trim());

                if (!providedSignatures.Contains(computedSignature))
                {
                    return (false, "signature does not match");
                }
            }

            return (true, null);
        }

        public async Task<RevWebhook> SetupWebhook(string webhookUrl)
        {
            var req = new CreateWebhookRequest
            {
                url = webhookUrl,
                events = [WebhookEventTypes.OrderCompleted, WebhookEventTypes.OrderCancelled]
            };
            using var response = await CreateClient().PostAsJsonAsync("1.0/webhooks", req, _jsonOptions);
            await response.EnsureSuccessStatusCodeIncludeBody();
            return await response.Content.ReadFromJsonAsync<RevWebhook>();
        }
    }
}
