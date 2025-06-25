using LMApp.Models.Extensions;
using Shared.License;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Licenses
{
    public class LicenseService(IHttpClientFactory httpFactory)
    {
        private readonly IHttpClientFactory _httpFactory = httpFactory;

        public async Task<LicensingInfoResponse> GetLicensingInfo()
        {
            var client = _httpFactory.CreateClient("Api");
            using var resp = await client.GetAsync($"license");

            resp.EnsureSuccessStatusCodeNamed("Api");
            var result = await resp.Content.ReadFromJsonAsync<LicensingInfoResponse>();
            if (result == null)
            {
                throw new Exception("Failed to parse licensing info");
            }
            return result;
        }

        public async Task<List<ProductInfo>> GetProducts()
        {
            var client = _httpFactory.CreateClient("Api");
            using var resp = await client.GetAsync($"license/products");
            resp.EnsureSuccessStatusCodeNamed("Api");
            var result = await resp.Content.ReadFromJsonAsync<List<ProductInfo>>();
            if (result == null)
            {
                throw new Exception("Failed to parse product prices");
            }
            return result;
        }

        public async Task<PaymentStatusInfo> GetOrder(int id)
        {
            var client = _httpFactory.CreateClient("Api");
            using var resp = await client.GetAsync($"license/order/{id}");
            if (!resp.IsSuccessStatusCode && resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            resp.EnsureSuccessStatusCodeNamed("Api");
            var result = await resp.Content.ReadFromJsonAsync<PaymentStatusInfo>();
            if (result == null)
            {
                throw new Exception("Failed to parse product prices");
            }
            return result;
        }

        public async Task<CreateOrderResponse> StartPayment(int priceId)
        {
            var client = _httpFactory.CreateClient("Api");
            using var resp = await client.PostAsync($"license/startpayment/{priceId}", null);
            resp.EnsureSuccessStatusCodeNamed("Api");
            var result = await resp.Content.ReadFromJsonAsync<CreateOrderResponse>();
            if (result == null)
            {
                throw new Exception("Failed to parse product prices");
            }
            return result;
        }
    }
}
