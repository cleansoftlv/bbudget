using System.Text.Json.Serialization;

namespace LMApp.Models.Categories
{
    public class UpsertBudgetRequest
    {
        [JsonPropertyName("start_date")]
        public string start_date { get; set; }

        [JsonPropertyName("category_id")]
        public long category_id { get; set; }

        [JsonPropertyName("amount")]
        public decimal amount { get; set; }

        [JsonPropertyName("currency")]
        public string currency { get; set; }
    }
}
