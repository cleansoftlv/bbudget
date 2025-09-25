using System.Text.Json.Serialization;

namespace LMApp.Models.Categories
{
    public class UpsertBudgetResponse
    {
        [JsonPropertyName("category_group")]
        public CategoryGroupBudget category_group { get; set; }
    }

    public class CategoryGroupBudget
    {
        [JsonPropertyName("category_id")]
        public long category_id { get; set; }

        [JsonPropertyName("amount")]
        public decimal amount { get; set; }

        [JsonPropertyName("currency")]
        public string currency { get; set; }

        [JsonPropertyName("start_date")]
        public string start_date { get; set; }
    }
}
