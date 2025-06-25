using System.Text.Json.Serialization;

namespace Shared.LMApi
{
    public class UserDto
    {
        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("user_email")]
        public string UserEmail { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("account_id")]
        public long AccountId { get; set; }

        [JsonPropertyName("budget_name")]
        public string BudgetName { get; set; }

        [JsonPropertyName("primary_currency")]
        public string PrimaryCurrency { get; set; }

        [JsonPropertyName("api_key_label")]
        public string ApiKeyLabel { get; set; }
    }
}